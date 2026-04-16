// SkillBot.Infrastructure/Memory/SqliteMemoryProvider.cs
using System.Data;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using SkillBot.Core.Exceptions;
using SkillBot.Core.Interfaces;
using SkillBot.Core.Models;
using System.Text.Json;

namespace SkillBot.Infrastructure.Memory;

/// <summary>
/// SQLite-based implementation of chat history storage.
/// Provides persistent storage across sessions.
/// </summary>
public class SqliteMemoryProvider : IMemoryProvider, IAsyncDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ILogger<SqliteMemoryProvider> _logger;
    private readonly string _databasePath;
    private bool _initialized;

    public SqliteMemoryProvider(
        string databasePath,
        ILogger<SqliteMemoryProvider> logger)
    {
        _databasePath = databasePath ?? throw new ArgumentNullException(nameof(databasePath));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Create connection string
        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = _databasePath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared
        }.ToString();

        _connection = new SqliteConnection(connectionString);
    }

    /// <summary>
    /// Initialize the database (create tables if needed)
    /// </summary>
    private async Task EnsureInitializedAsync(CancellationToken cancellationToken = default)
    {
        if (_initialized) return;

        if (_connection.State != ConnectionState.Open)
        {
            await _connection.OpenAsync(cancellationToken);
        }

        await CreateSchemaAsync(cancellationToken);
        _initialized = true;

        _logger.LogInformation("SQLite database initialized at: {Path}", _databasePath);
    }

    /// <summary>
    /// Create database schema
    /// </summary>
    private async Task CreateSchemaAsync(CancellationToken cancellationToken)
    {
        var createTableSql = """
            CREATE TABLE IF NOT EXISTS messages (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                role TEXT NOT NULL,
                content TEXT NOT NULL,
                timestamp TEXT NOT NULL,
                metadata TEXT,
                created_at TEXT NOT NULL DEFAULT (datetime('now'))
            );

            CREATE INDEX IF NOT EXISTS idx_messages_timestamp 
            ON messages(timestamp DESC);

            CREATE INDEX IF NOT EXISTS idx_messages_role 
            ON messages(role);
            """;

        using var command = _connection.CreateCommand();
        command.CommandText = createTableSql;
        await command.ExecuteNonQueryAsync(cancellationToken);

        _logger.LogDebug("Database schema created/verified");
    }

    public async Task AddMessageAsync(
        AgentMessage message,
        CancellationToken cancellationToken = default)
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));

        await EnsureInitializedAsync(cancellationToken);

        try
        {
            var insertSql = """
                INSERT INTO messages (role, content, timestamp, metadata)
                VALUES (@role, @content, @timestamp, @metadata)
                """;

            using var command = _connection.CreateCommand();
            command.CommandText = insertSql;
            command.Parameters.AddWithValue("@role", message.Role);
            command.Parameters.AddWithValue("@content", message.Content);
            command.Parameters.AddWithValue("@timestamp", message.Timestamp.ToString("O")); // ISO 8601
            command.Parameters.AddWithValue("@metadata",
                message.Metadata != null ? JsonSerializer.Serialize(message.Metadata) : DBNull.Value);

            await command.ExecuteNonQueryAsync(cancellationToken);

            _logger.LogDebug("Added {Role} message to SQLite database", message.Role);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add message to database");
            throw new MemoryException("Failed to save message to database", ex);
        }
    }

    public async Task<IReadOnlyList<AgentMessage>> GetHistoryAsync(
        int? count = null,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);

        try
        {
            var selectSql = count.HasValue
                ? """
                  SELECT role, content, timestamp, metadata 
                  FROM messages 
                  ORDER BY timestamp DESC 
                  LIMIT @count
                  """
                : """
                  SELECT role, content, timestamp, metadata 
                  FROM messages 
                  ORDER BY timestamp DESC
                  """;

            using var command = _connection.CreateCommand();
            command.CommandText = selectSql;

            if (count.HasValue)
            {
                command.Parameters.AddWithValue("@count", count.Value);
            }

            var messages = new List<AgentMessage>();

            using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var role = reader.GetString(0);
                var content = reader.GetString(1);
                var timestamp = DateTimeOffset.Parse(reader.GetString(2));
                var metadataJson = reader.IsDBNull(3) ? null : reader.GetString(3);

                Dictionary<string, object>? metadata = null;
                if (!string.IsNullOrEmpty(metadataJson))
                {
                    metadata = JsonSerializer.Deserialize<Dictionary<string, object>>(metadataJson);
                }

                messages.Add(new AgentMessage
                {
                    Role = role,
                    Content = content,
                    Timestamp = timestamp,
                    Metadata = metadata
                });
            }

            // Reverse to get chronological order
            messages.Reverse();

            _logger.LogDebug("Retrieved {Count} messages from database", messages.Count);

            return messages;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve message history from database");
            throw new MemoryException("Failed to retrieve message history", ex);
        }
    }

    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);

        try
        {
            var deleteSql = "DELETE FROM messages";

            using var command = _connection.CreateCommand();
            command.CommandText = deleteSql;
            var rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken);

            _logger.LogInformation("Cleared {RowCount} messages from database", rowsAffected);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear message history");
            throw new MemoryException("Failed to clear message history", ex);
        }
    }

    public async Task<int> GetMessageCountAsync(CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);

        try
        {
            var countSql = "SELECT COUNT(*) FROM messages";

            using var command = _connection.CreateCommand();
            command.CommandText = countSql;
            var result = await command.ExecuteScalarAsync(cancellationToken);

            return Convert.ToInt32(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get message count");
            throw new MemoryException("Failed to get message count", ex);
        }
    }

    public async Task SaveAsync(CancellationToken cancellationToken = default)
    {
        // SQLite auto-commits by default, but we can optimize with WAL mode
        await EnsureInitializedAsync(cancellationToken);

        try
        {
            // Enable WAL mode for better concurrent performance
            var walSql = "PRAGMA journal_mode=WAL;";

            using var command = _connection.CreateCommand();
            command.CommandText = walSql;
            await command.ExecuteNonQueryAsync(cancellationToken);

            _logger.LogDebug("Database save/optimization completed");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to optimize database");
            // Don't throw - this is not critical
        }
    }

    /// <summary>
    /// Additional helper methods for SQLite-specific operations
    /// </summary>
    public async Task VacuumAsync(CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);

        try
        {
            var vacuumSql = "VACUUM;";

            using var command = _connection.CreateCommand();
            command.CommandText = vacuumSql;
            await command.ExecuteNonQueryAsync(cancellationToken);

            _logger.LogInformation("Database vacuum completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to vacuum database");
            throw new MemoryException("Failed to vacuum database", ex);
        }
    }

    public async Task<long> GetDatabaseSizeAsync(CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);

        try
        {
            if (File.Exists(_databasePath))
            {
                var fileInfo = new FileInfo(_databasePath);
                return fileInfo.Length;
            }

            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get database size");
            return 0;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection != null)
        {
            await _connection.CloseAsync();
            await _connection.DisposeAsync();
        }

        _logger.LogInformation("SQLite connection disposed");
    }
}
