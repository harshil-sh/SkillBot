using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using SkillBot.Infrastructure.Plugins;

namespace SkillBot.Plugins.Search;

/// <summary>
/// Web search plugin using SerpAPI (provides Google search results)
/// </summary>
[Plugin(Name = "WebSearch", Description = "Search the web for current information using Google via SerpAPI")]
public class SerpApiPlugin
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly ILogger<SerpApiPlugin>? _logger;
    private const string SerpApiEndpoint = "https://serpapi.com/search";
    private const int MaxLoggedJsonLength = 4000;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public SerpApiPlugin(IConfiguration configuration, ILogger<SerpApiPlugin>? logger = null)
    {
        _apiKey = configuration["SerpApi:ApiKey"] 
            ?? throw new InvalidOperationException("SerpAPI key not configured");
        
        _logger = logger;
        _httpClient = new HttpClient();
    }

    [KernelFunction("search_web")]
    [Description("REQUIRED for current information: Search Google for recent news, latest developments, current events, or any time-sensitive information. Use this whenever the user asks about 'latest', 'recent', 'current', 'news', or requests up-to-date information.")]
    public async Task<string> SearchWebAsync(
        [Description("Search query - be specific and clear")] string query,
        [Description("Number of results to return (1-10, default 5)")] int count = 5)
    {
        try
        {
            _logger?.LogInformation("Searching web via SerpAPI for: {Query}", query);

            var numResults = Math.Min(Math.Max(count, 1), 10);
            var requestUrl = $"{SerpApiEndpoint}?engine=google&q={Uri.EscapeDataString(query)}&num={numResults}&api_key={_apiKey}";
            
            var response = await _httpClient.GetAsync(requestUrl);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger?.LogError("SerpAPI error: {StatusCode} - {Error}", response.StatusCode, error);
                
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    return "Search failed: Invalid SerpAPI key. Please check your configuration.";
                }
                
                return $"Search failed: {response.StatusCode}";
            }

            var content = await response.Content.ReadAsStringAsync();
            LogApiResponse("google", query, content);

            SerpApiResponse? searchResult;
            try
            {
                searchResult = JsonSerializer.Deserialize<SerpApiResponse>(content, JsonOptions);
            }
            catch (JsonException ex)
            {
                _logger?.LogError(ex,
                    "Failed to parse SerpAPI web search JSON for query '{Query}'. Raw response: {RawResponse}",
                    query,
                    TruncateForLogs(content));
                return "Search failed: Invalid JSON response format from SerpAPI.";
            }

            if (searchResult == null)
            {
                _logger?.LogWarning("SerpAPI web search JSON deserialized to null for query '{Query}'. Raw response: {RawResponse}",
                    query,
                    TruncateForLogs(content));
                return "Search failed: Empty or unsupported JSON response from SerpAPI.";
            }

            if (searchResult?.OrganicResults == null || searchResult.OrganicResults.Count == 0)
            {
                return $"No results found for: {query}";
            }

            var results = new System.Text.StringBuilder();
            results.AppendLine($"Search results for '{query}':");
            results.AppendLine();

            foreach (var result in searchResult.OrganicResults.Take(numResults))
            {
                results.AppendLine($"**{result.Title}**");
                if (!string.IsNullOrEmpty(result.Snippet))
                {
                    results.AppendLine(result.Snippet);
                }
                results.AppendLine($"URL: {result.Link}");
                
                if (!string.IsNullOrEmpty(result.DisplayedLink))
                {
                    results.AppendLine($"Source: {result.DisplayedLink}");
                }
                
                results.AppendLine();
            }

            _logger?.LogInformation("Found {Count} results for: {Query}", searchResult.OrganicResults.Count, query);

            return results.ToString();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error searching web for: {Query}", query);
            return $"Search error: {ex.Message}";
        }
    }

    [KernelFunction("search_news")]
    [Description("Search for recent news articles and breaking stories. Use this specifically for news, current events, or recent developments.")]
    public async Task<string> SearchNewsAsync(
        [Description("News search query")] string query,
        [Description("Number of results (1-10, default 5)")] int count = 5)
    {
        try
        {
            _logger?.LogInformation("Searching news via SerpAPI for: {Query}", query);

            var numResults = Math.Min(Math.Max(count, 1), 10);
            var requestUrl = $"{SerpApiEndpoint}?engine=google_news&q={Uri.EscapeDataString(query)}&num={numResults}&api_key={_apiKey}";
            
            var response = await _httpClient.GetAsync(requestUrl);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger?.LogError("SerpAPI news error: {StatusCode} - {Error}", response.StatusCode, error);
                return $"News search failed: {response.StatusCode}";
            }

            var content = await response.Content.ReadAsStringAsync();
            LogApiResponse("google_news", query, content);

            SerpApiNewsResponse? searchResult;
            try
            {
                searchResult = JsonSerializer.Deserialize<SerpApiNewsResponse>(content, JsonOptions);
            }
            catch (JsonException ex)
            {
                _logger?.LogError(ex,
                    "Failed to parse SerpAPI news JSON for query '{Query}'. Raw response: {RawResponse}",
                    query,
                    TruncateForLogs(content));
                return "News search failed: Invalid JSON response format from SerpAPI.";
            }

            if (searchResult == null)
            {
                _logger?.LogWarning("SerpAPI news JSON deserialized to null for query '{Query}'. Raw response: {RawResponse}",
                    query,
                    TruncateForLogs(content));
                return "News search failed: Empty or unsupported JSON response from SerpAPI.";
            }

            if (searchResult?.NewsResults == null || searchResult.NewsResults.Count == 0)
            {
                return $"No news found for: {query}";
            }

            var newsItems = FlattenNewsResults(searchResult.NewsResults)
                .Take(numResults)
                .ToList();

            if (newsItems.Count == 0)
            {
                return $"No news found for: {query}";
            }

            var results = new System.Text.StringBuilder();
            results.AppendLine($"Recent news for '{query}':");
            results.AppendLine();

            foreach (var item in newsItems)
            {
                results.AppendLine($"**{item.Title}**");
                if (!string.IsNullOrEmpty(item.Snippet))
                {
                    results.AppendLine(item.Snippet);
                }
                if (!string.IsNullOrWhiteSpace(item.Source))
                {
                    results.AppendLine($"Source: {item.Source}");
                }
                if (!string.IsNullOrWhiteSpace(item.Date))
                {
                    results.AppendLine($"Published: {item.Date}");
                }
                results.AppendLine($"URL: {item.Link}");
                results.AppendLine();
            }

            return results.ToString();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error searching news for: {Query}", query);
            return $"News search error: {ex.Message}";
        }
    }

    [KernelFunction("search_images")]
    [Description("Search for images. Returns image URLs and descriptions.")]
    public async Task<string> SearchImagesAsync(
        [Description("Image search query")] string query,
        [Description("Number of results (1-10, default 5)")] int count = 5)
    {
        try
        {
            _logger?.LogInformation("Searching images via SerpAPI for: {Query}", query);

            var numResults = Math.Min(Math.Max(count, 1), 10);
            var requestUrl = $"{SerpApiEndpoint}?engine=google_images&q={Uri.EscapeDataString(query)}&num={numResults}&api_key={_apiKey}";
            
            var response = await _httpClient.GetAsync(requestUrl);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger?.LogError("SerpAPI image error: {StatusCode} - {Error}", response.StatusCode, error);
                return $"Image search failed: {response.StatusCode}";
            }

            var content = await response.Content.ReadAsStringAsync();
            LogApiResponse("google_images", query, content);

            SerpApiImageResponse? searchResult;
            try
            {
                searchResult = JsonSerializer.Deserialize<SerpApiImageResponse>(content, JsonOptions);
            }
            catch (JsonException ex)
            {
                _logger?.LogError(ex,
                    "Failed to parse SerpAPI image JSON for query '{Query}'. Raw response: {RawResponse}",
                    query,
                    TruncateForLogs(content));
                return "Image search failed: Invalid JSON response format from SerpAPI.";
            }

            if (searchResult == null)
            {
                _logger?.LogWarning("SerpAPI image JSON deserialized to null for query '{Query}'. Raw response: {RawResponse}",
                    query,
                    TruncateForLogs(content));
                return "Image search failed: Empty or unsupported JSON response from SerpAPI.";
            }

            if (searchResult?.ImagesResults == null || searchResult.ImagesResults.Count == 0)
            {
                return $"No images found for: {query}";
            }

            var results = new System.Text.StringBuilder();
            results.AppendLine($"Image search results for '{query}':");
            results.AppendLine();

            foreach (var image in searchResult.ImagesResults.Take(numResults))
            {
                results.AppendLine($"**{image.Title}**");
                results.AppendLine($"Image URL: {image.Original}");
                if (!string.IsNullOrEmpty(image.Thumbnail))
                {
                    results.AppendLine($"Thumbnail: {image.Thumbnail}");
                }
                results.AppendLine($"Source: {image.Source}");
                results.AppendLine();
            }

            return results.ToString();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error searching images for: {Query}", query);
            return $"Image search error: {ex.Message}";
        }
    }

    private void LogApiResponse(string engine, string query, string responseJson)
    {
        _logger?.LogInformation(
            "SerpAPI response received. Engine: {Engine}, Query: {Query}, Length: {Length}",
            engine,
            query,
            responseJson.Length);

        _logger?.LogDebug(
            "SerpAPI raw JSON response. Engine: {Engine}, Query: {Query}, Response: {Response}",
            engine,
            query,
            TruncateForLogs(responseJson));
    }

    private static string TruncateForLogs(string value)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= MaxLoggedJsonLength)
        {
            return value;
        }

        return value[..MaxLoggedJsonLength] + "... [truncated]";
    }

    private static IEnumerable<NewsDisplayItem> FlattenNewsResults(IEnumerable<NewsResult> newsResults)
    {
        var seenLinks = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var article in newsResults)
        {
            if (!string.IsNullOrWhiteSpace(article.Link) && seenLinks.Add(article.Link))
            {
                yield return new NewsDisplayItem
                {
                    Title = article.Title,
                    Snippet = article.Snippet,
                    Source = ExtractSourceName(article.Source),
                    Date = article.Date,
                    Link = article.Link
                };
            }

            if (article.Stories == null)
            {
                continue;
            }

            foreach (var story in article.Stories)
            {
                if (string.IsNullOrWhiteSpace(story.Link) || !seenLinks.Add(story.Link))
                {
                    continue;
                }

                yield return new NewsDisplayItem
                {
                    Title = story.Title,
                    Snippet = story.Snippet,
                    Source = ExtractSourceName(story.Source),
                    Date = story.Date,
                    Link = story.Link
                };
            }
        }
    }

    private static string ExtractSourceName(JsonElement source)
    {
        if (source.ValueKind == JsonValueKind.String)
        {
            return source.GetString() ?? "";
        }

        if (source.ValueKind == JsonValueKind.Object &&
            source.TryGetProperty("name", out var nameElement) &&
            nameElement.ValueKind == JsonValueKind.String)
        {
            return nameElement.GetString() ?? "";
        }

        return "";
    }

    // DTOs for SerpAPI responses
    private class SerpApiResponse
    {
        [JsonPropertyName("organic_results")]
        public List<OrganicResult>? OrganicResults { get; set; }
    }

    private class OrganicResult
    {
        [JsonPropertyName("title")]
        public string Title { get; set; } = "";

        [JsonPropertyName("link")]
        public string Link { get; set; } = "";

        [JsonPropertyName("snippet")]
        public string Snippet { get; set; } = "";

        [JsonPropertyName("displayed_link")]
        public string DisplayedLink { get; set; } = "";
    }

    private class SerpApiNewsResponse
    {
        [JsonPropertyName("news_results")]
        public List<NewsResult>? NewsResults { get; set; }
    }

    private class NewsResult
    {
        [JsonPropertyName("title")]
        public string Title { get; set; } = "";

        [JsonPropertyName("link")]
        public string Link { get; set; } = "";

        [JsonPropertyName("snippet")]
        public string Snippet { get; set; } = "";

        [JsonPropertyName("source")]
        public JsonElement Source { get; set; }

        [JsonPropertyName("date")]
        public string Date { get; set; } = "";

        [JsonPropertyName("stories")]
        public List<NewsStory>? Stories { get; set; }
    }

    private class NewsStory
    {
        [JsonPropertyName("title")]
        public string Title { get; set; } = "";

        [JsonPropertyName("link")]
        public string Link { get; set; } = "";

        [JsonPropertyName("snippet")]
        public string Snippet { get; set; } = "";

        [JsonPropertyName("source")]
        public JsonElement Source { get; set; }

        [JsonPropertyName("date")]
        public string Date { get; set; } = "";
    }

    private class NewsDisplayItem
    {
        public string Title { get; set; } = "";
        public string Link { get; set; } = "";
        public string Snippet { get; set; } = "";
        public string Source { get; set; } = "";
        public string Date { get; set; } = "";
    }

    private class SerpApiImageResponse
    {
        [JsonPropertyName("images_results")]
        public List<ImageResult>? ImagesResults { get; set; }
    }

    private class ImageResult
    {
        [JsonPropertyName("title")]
        public string Title { get; set; } = "";

        [JsonPropertyName("original")]
        public string Original { get; set; } = "";

        [JsonPropertyName("thumbnail")]
        public string Thumbnail { get; set; } = "";

        [JsonPropertyName("source")]
        public string Source { get; set; } = "";
    }
}
