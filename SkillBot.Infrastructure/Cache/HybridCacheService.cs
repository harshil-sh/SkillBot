using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using SkillBot.Core.Interfaces;
using SkillBot.Infrastructure.Configuration;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text;
using System.Collections.Generic;
using System.Linq;

namespace SkillBot.Infrastructure.Cache;

/// <summary>
/// Two-tier cache implementation with in-memory L1 and SQLite L2.
/// </summary>
public class HybridCacheService : ICacheService, IDisposable
{
    private readonly IMemoryCache _l1Cache;
    private readonly string _databasePath;
    private readonly ILogger<HybridCacheService> _logger;
    private readonly SemaphoreSlim _writeLock = new(1, 1);
    private readonly CacheStatistics _statistics = new();
    private readonly object _statsLock = new();
    private bool _disposed;

    public HybridCacheService(
        IMemoryCache memoryCache,
        CachingOptions options,
        ILogger<HybridCacheService> logger)
    {
        _l1Cache = memoryCache;
        _databasePath = options.CacheDatabasePath;
        _logger = logger;

        InitializeDatabase();
    }

    private void InitializeDatabase()
    {
        using var connection = new SqliteConnection($"Data Source={_databasePath}");
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS cache_entries (
                key TEXT PRIMARY KEY,
                value TEXT NOT NULL,
                type TEXT NOT NULL,
                created_at TEXT NOT NULL,
                expires_at TEXT NOT NULL,
                access_count INTEGER DEFAULT 0
            );
            CREATE INDEX IF NOT EXISTS idx_expires_at ON cache_entries(expires_at);
            CREATE INDEX IF NOT EXISTS idx_type ON cache_entries(type);
        ";
        command.ExecuteNonQuery();

        _logger.LogInformation("Cache database initialized at {Path}", _databasePath);
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        // Check L1 cache
        if (_l1Cache.TryGetValue(key, out T? value))
        {
            lock (_statsLock)
            {
                _statistics.L1Hits++;
            }
            _logger.LogDebug("L1 cache hit for key: {Key}", key);
            return value;
        }

        lock (_statsLock)
        {
            _statistics.L1Misses++;
        }

        // Check L2 cache
        var l2Value = await GetFromL2Async<T>(key, cancellationToken);
        if (l2Value != null)
        {
            lock (_statsLock)
            {
                _statistics.L2Hits++;
            }
            _logger.LogDebug("L2 cache hit for key: {Key}, promoting to L1", key);

            // Promote to L1
            var ttl = await GetRemainingTtlAsync(key, cancellationToken);
            if (ttl > TimeSpan.Zero)
            {
                var l1Options = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(ttl)
                    .SetSize(CalculateEntrySize(l2Value));

                _l1Cache.Set(key, l2Value, l1Options);
            }

            return l2Value;
        }

        lock (_statsLock)
        {
            _statistics.L2Misses++;
        }
        _logger.LogDebug("Cache miss for key: {Key}", key);
        return null;
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan ttl, string type, CancellationToken cancellationToken = default) where T : class
    {
        // Set in L1
        var l1Options = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(ttl)
            .SetSize(CalculateEntrySize(value));

        _l1Cache.Set(key, value, l1Options);

        // Set in L2
        await SetInL2Async(key, value, ttl, type, cancellationToken);

        _logger.LogDebug("Cached value for key: {Key}, type: {Type}, TTL: {TTL}", key, type, ttl);
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        _l1Cache.Remove(key);

        await _writeLock.WaitAsync(cancellationToken);
        try
        {
            using var connection = new SqliteConnection($"Data Source={_databasePath}");
            await connection.OpenAsync(cancellationToken);

            var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM cache_entries WHERE key = @key";
            command.Parameters.AddWithValue("@key", key);
            await command.ExecuteNonQueryAsync(cancellationToken);

            _logger.LogDebug("Removed cache entry: {Key}", key);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        // IMemoryCache doesn't have a Clear method, so we need to recreate it
        // For now, we just clear L2 and let L1 entries expire naturally
        // Alternatively, track all keys in L1 and remove them individually

        await _writeLock.WaitAsync(cancellationToken);
        try
        {
            using var connection = new SqliteConnection($"Data Source={_databasePath}");
            await connection.OpenAsync(cancellationToken);

            var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM cache_entries";
            await command.ExecuteNonQueryAsync(cancellationToken);

            _logger.LogInformation("Cleared all L2 cache entries (L1 will expire naturally)");
        }
        finally
        {
            _writeLock.Release();
        }
    }

    public async Task InvalidateByPatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        // Convert pattern to SQL LIKE pattern
        var likePattern = pattern.Replace("*", "%");

        await _writeLock.WaitAsync(cancellationToken);
        try
        {
            using var connection = new SqliteConnection($"Data Source={_databasePath}");
            await connection.OpenAsync(cancellationToken);

            // Get keys to remove
            var keysCommand = connection.CreateCommand();
            keysCommand.CommandText = "SELECT key FROM cache_entries WHERE key LIKE @pattern";
            keysCommand.Parameters.AddWithValue("@pattern", likePattern);

            var keys = new List<string>();
            using (var reader = await keysCommand.ExecuteReaderAsync(cancellationToken))
            {
                while (await reader.ReadAsync(cancellationToken))
                {
                    keys.Add(reader.GetString(0));
                }
            }

            // Remove from L1
            foreach (var key in keys)
            {
                _l1Cache.Remove(key);
            }

            // Remove from L2
            var deleteCommand = connection.CreateCommand();
            deleteCommand.CommandText = "DELETE FROM cache_entries WHERE key LIKE @pattern";
            deleteCommand.Parameters.AddWithValue("@pattern", likePattern);
            var deleted = await deleteCommand.ExecuteNonQueryAsync(cancellationToken);

            _logger.LogInformation("Invalidated {Count} cache entries matching pattern: {Pattern}", deleted, pattern);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    public async Task CleanupExpiredAsync(CancellationToken cancellationToken = default)
    {
        await _writeLock.WaitAsync(cancellationToken);
        try
        {
            using var connection = new SqliteConnection($"Data Source={_databasePath}");
            await connection.OpenAsync(cancellationToken);

            var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM cache_entries WHERE expires_at < @now";
            command.Parameters.AddWithValue("@now", DateTime.UtcNow.ToString("O"));
            var deleted = await command.ExecuteNonQueryAsync(cancellationToken);

            if (deleted > 0)
            {
                _logger.LogInformation("Cleaned up {Count} expired cache entries", deleted);
            }
        }
        finally
        {
            _writeLock.Release();
        }
    }

    public CacheStatistics GetStatistics()
    {
        using var connection = new SqliteConnection($"Data Source={_databasePath}");
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT
                COUNT(*) as total_entries,
                SUM(LENGTH(value)) as total_size,
                MIN(created_at) as oldest_entry,
                MAX(created_at) as newest_entry
            FROM cache_entries
            WHERE expires_at > @now
        ";
        command.Parameters.AddWithValue("@now", DateTime.UtcNow.ToString("O"));

        using var reader = command.ExecuteReader();
        if (reader.Read())
        {
            _statistics.TotalEntries = reader.GetInt32(0);
            _statistics.TotalSizeBytes = reader.IsDBNull(1) ? 0 : reader.GetInt64(1);

            if (!reader.IsDBNull(2))
                _statistics.OldestEntry = DateTime.Parse(reader.GetString(2));

            if (!reader.IsDBNull(3))
                _statistics.NewestEntry = DateTime.Parse(reader.GetString(3));
        }

        return _statistics;
    }

    private async Task<T?> GetFromL2Async<T>(string key, CancellationToken cancellationToken) where T : class
    {
        using var connection = new SqliteConnection($"Data Source={_databasePath}");
        await connection.OpenAsync(cancellationToken);

        var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT value FROM cache_entries
            WHERE key = @key AND expires_at > @now
        ";
        command.Parameters.AddWithValue("@key", key);
        command.Parameters.AddWithValue("@now", DateTime.UtcNow.ToString("O"));

        var json = await command.ExecuteScalarAsync(cancellationToken) as string;
        if (json == null)
            return null;

        // Update access count
        await IncrementAccessCountAsync(key, cancellationToken);

        return JsonSerializer.Deserialize<T>(json);
    }

    private async Task SetInL2Async<T>(string key, T value, TimeSpan ttl, string type, CancellationToken cancellationToken) where T : class
    {
        var json = JsonSerializer.Serialize(value);
        var now = DateTime.UtcNow;
        var expiresAt = now.Add(ttl);

        await _writeLock.WaitAsync(cancellationToken);
        try
        {
            using var connection = new SqliteConnection($"Data Source={_databasePath}");
            await connection.OpenAsync(cancellationToken);

            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT OR REPLACE INTO cache_entries (key, value, type, created_at, expires_at, access_count)
                VALUES (@key, @value, @type, @created_at, @expires_at, 0)
            ";
            command.Parameters.AddWithValue("@key", key);
            command.Parameters.AddWithValue("@value", json);
            command.Parameters.AddWithValue("@type", type);
            command.Parameters.AddWithValue("@created_at", now.ToString("O"));
            command.Parameters.AddWithValue("@expires_at", expiresAt.ToString("O"));

            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    private async Task<TimeSpan> GetRemainingTtlAsync(string key, CancellationToken cancellationToken)
    {
        using var connection = new SqliteConnection($"Data Source={_databasePath}");
        await connection.OpenAsync(cancellationToken);

        var command = connection.CreateCommand();
        command.CommandText = "SELECT expires_at FROM cache_entries WHERE key = @key";
        command.Parameters.AddWithValue("@key", key);

        var expiresAtStr = await command.ExecuteScalarAsync(cancellationToken) as string;
        if (expiresAtStr == null)
            return TimeSpan.Zero;

        var expiresAt = DateTime.Parse(expiresAtStr);
        var remaining = expiresAt - DateTime.UtcNow;
        return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
    }

    private async Task IncrementAccessCountAsync(string key, CancellationToken cancellationToken)
    {
        await _writeLock.WaitAsync(cancellationToken);
        try
        {
            using var connection = new SqliteConnection($"Data Source={_databasePath}");
            await connection.OpenAsync(cancellationToken);

            var command = connection.CreateCommand();
            command.CommandText = "UPDATE cache_entries SET access_count = access_count + 1 WHERE key = @key";
            command.Parameters.AddWithValue("@key", key);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    private static long CalculateEntrySize<T>(T value) where T : class
    {
        var json = JsonSerializer.Serialize(value);
        var byteCount = Encoding.UTF8.GetByteCount(json);
        return Math.Max(1, byteCount);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _writeLock.Dispose();
        _disposed = true;
    }
}
