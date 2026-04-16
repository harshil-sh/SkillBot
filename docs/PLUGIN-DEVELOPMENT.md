# Plugin Development Guide

## Table of Contents
- [Introduction](#introduction)
- [Plugin Basics](#plugin-basics)
- [Step-by-Step Tutorial](#step-by-step-tutorial)
- [Advanced Patterns](#advanced-patterns)
- [Best Practices](#best-practices)
- [Testing Plugins](#testing-plugins)
- [Deployment](#deployment)

## Introduction

Plugins (also called "Tools" or "Skills") extend SkillBot's capabilities. They are C# classes that the AI agent can call to perform specific tasks.

### What Makes a Good Plugin?

✅ **Focused**: Does one thing well  
✅ **Documented**: Clear descriptions for the LLM  
✅ **Reliable**: Handles errors gracefully  
✅ **Fast**: Responds quickly (< 5 seconds ideally)  
✅ **Stateless**: No side effects between calls  

### Plugin Lifecycle

```
1. Developer creates plugin class with attributes
2. Plugin registered with PluginProvider
3. PluginProvider registers with Semantic Kernel
4. LLM decides when to call plugin
5. Semantic Kernel invokes plugin method
6. Result returned to LLM
7. LLM incorporates result into response
```

## Plugin Basics

### Minimum Plugin Structure

```csharp
using Microsoft.SemanticKernel;
using System.ComponentModel;
using SkillBot.Infrastructure.Plugins;

namespace MyApp.Plugins;

[Plugin(Name = "MyPlugin", Description = "What it does")]
public class MyPlugin
{
    [KernelFunction("my_function")]
    [Description("What this function does")]
    public string MyFunction(
        [Description("What this parameter is")] string input)
    {
        return $"Result: {input}";
    }
}
```

### Required Attributes

**Class Level**:
```csharp
[Plugin(Name = "ShortName", Description = "Detailed description")]
```
- `Name`: Short identifier (no spaces)
- `Description`: Helps LLM understand when to use this plugin

**Method Level**:
```csharp
[KernelFunction("function_name")]
[Description("Clear description of what this function does")]
```
- Function name should be lowercase with underscores
- Description should explain purpose and use case

**Parameter Level**:
```csharp
[Description("What this parameter represents")]
```
- Helps LLM provide correct arguments
- Be specific about format, range, or examples

## Step-by-Step Tutorial

### Example 1: Simple Calculator

```csharp
using Microsoft.SemanticKernel;
using System.ComponentModel;
using SkillBot.Infrastructure.Plugins;

namespace SkillBot.Plugins.Examples;

[Plugin(
    Name = "Calculator",
    Description = "Performs basic arithmetic operations")]
public class CalculatorPlugin
{
    [KernelFunction("add")]
    [Description("Add two numbers together")]
    public double Add(
        [Description("The first number")] double a,
        [Description("The second number")] double b)
    {
        return a + b;
    }

    [KernelFunction("power")]
    [Description("Raise a number to a power")]
    public double Power(
        [Description("The base number")] double baseNum,
        [Description("The exponent")] double exponent)
    {
        return Math.Pow(baseNum, exponent);
    }
}
```

**Registration**:
```csharp
pluginProvider.RegisterPlugin(new CalculatorPlugin());
```

**Usage**:
```
User: What's 5 to the power of 3?
Assistant: 5 to the power of 3 equals 125.
```

### Example 2: File Operations

```csharp
using Microsoft.SemanticKernel;
using System.ComponentModel;
using SkillBot.Infrastructure.Plugins;

namespace SkillBot.Plugins.FileSystem;

[Plugin(
    Name = "FileOperations",
    Description = "Read and write files on the local system")]
public class FileOperationsPlugin
{
    private readonly string _baseDirectory;

    public FileOperationsPlugin(string baseDirectory = "./data")
    {
        _baseDirectory = baseDirectory;
        Directory.CreateDirectory(_baseDirectory);
    }

    [KernelFunction("read_file")]
    [Description("Read the contents of a text file")]
    public async Task<string> ReadFileAsync(
        [Description("The filename to read")] string filename)
    {
        try
        {
            var path = Path.Combine(_baseDirectory, filename);
            
            if (!File.Exists(path))
                return $"Error: File '{filename}' not found";

            return await File.ReadAllTextAsync(path);
        }
        catch (Exception ex)
        {
            return $"Error reading file: {ex.Message}";
        }
    }

    [KernelFunction("write_file")]
    [Description("Write content to a text file")]
    public async Task<string> WriteFileAsync(
        [Description("The filename to write")] string filename,
        [Description("The content to write")] string content)
    {
        try
        {
            var path = Path.Combine(_baseDirectory, filename);
            await File.WriteAllTextAsync(path, content);
            return $"Successfully wrote to '{filename}'";
        }
        catch (Exception ex)
        {
            return $"Error writing file: {ex.Message}";
        }
    }

    [KernelFunction("list_files")]
    [Description("List all files in the directory")]
    public string ListFiles()
    {
        try
        {
            var files = Directory.GetFiles(_baseDirectory)
                .Select(Path.GetFileName)
                .ToList();

            if (files.Count == 0)
                return "No files found";

            return string.Join(", ", files);
        }
        catch (Exception ex)
        {
            return $"Error listing files: {ex.Message}";
        }
    }
}
```

**Registration**:
```csharp
pluginProvider.RegisterPlugin(new FileOperationsPlugin("./my_files"));
```

### Example 3: HTTP API Integration

```csharp
using Microsoft.SemanticKernel;
using System.ComponentModel;
using System.Text.Json;
using SkillBot.Infrastructure.Plugins;

namespace SkillBot.Plugins.Api;

[Plugin(
    Name = "GitHub",
    Description = "Interact with GitHub repositories and issues")]
public class GitHubPlugin
{
    private readonly HttpClient _httpClient;
    private readonly string _token;

    public GitHubPlugin(string githubToken)
    {
        _token = githubToken;
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri("https://api.github.com/")
        };
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_token}");
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "SkillBot");
    }

    [KernelFunction("get_repo_info")]
    [Description("Get information about a GitHub repository")]
    public async Task<string> GetRepoInfoAsync(
        [Description("Repository in format: owner/repo")] string repository)
    {
        try
        {
            var response = await _httpClient.GetAsync($"repos/{repository}");
            
            if (!response.IsSuccessStatusCode)
                return $"Error: Repository '{repository}' not found";

            var json = await response.Content.ReadAsStringAsync();
            var repo = JsonSerializer.Deserialize<GitHubRepo>(json);

            return $"""
                Repository: {repo.FullName}
                Description: {repo.Description}
                Stars: {repo.StargazersCount}
                Forks: {repo.ForksCount}
                Language: {repo.Language}
                URL: {repo.HtmlUrl}
                """;
        }
        catch (Exception ex)
        {
            return $"Error: {ex.Message}";
        }
    }

    [KernelFunction("search_repos")]
    [Description("Search for GitHub repositories")]
    public async Task<string> SearchReposAsync(
        [Description("Search query")] string query,
        [Description("Number of results (1-10)")] int limit = 5)
    {
        try
        {
            var response = await _httpClient.GetAsync(
                $"search/repositories?q={Uri.EscapeDataString(query)}&per_page={limit}");

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<GitHubSearchResult>(json);

            var repos = result.Items
                .Take(limit)
                .Select(r => $"• {r.FullName} - {r.Description} ({r.StargazersCount} stars)");

            return string.Join("\n", repos);
        }
        catch (Exception ex)
        {
            return $"Error: {ex.Message}";
        }
    }

    // DTOs for deserialization
    private class GitHubRepo
    {
        public string FullName { get; set; } = "";
        public string Description { get; set; } = "";
        public int StargazersCount { get; set; }
        public int ForksCount { get; set; }
        public string Language { get; set; } = "";
        public string HtmlUrl { get; set; } = "";
    }

    private class GitHubSearchResult
    {
        public List<GitHubRepo> Items { get; set; } = new();
    }
}
```

**Registration**:
```csharp
var githubToken = configuration["GitHub:Token"];
pluginProvider.RegisterPlugin(new GitHubPlugin(githubToken));
```

## Advanced Patterns

### Pattern 1: Plugin with Dependency Injection

```csharp
[Plugin(Name = "Database", Description = "Query database")]
public class DatabasePlugin
{
    private readonly IConfiguration _config;
    private readonly ILogger<DatabasePlugin> _logger;
    private readonly string _connectionString;

    public DatabasePlugin(
        IConfiguration config,
        ILogger<DatabasePlugin> logger)
    {
        _config = config;
        _logger = logger;
        _connectionString = _config.GetConnectionString("Default");
    }

    [KernelFunction("query")]
    [Description("Execute a SQL query")]
    public async Task<string> QueryAsync(
        [Description("SQL query to execute")] string sql)
    {
        _logger.LogInformation("Executing query: {Query}", sql);
        
        // Implementation
        return "Results...";
    }
}

// Registration with DI
services.AddSingleton<DatabasePlugin>();

// Later, in Program.cs
var dbPlugin = services.GetRequiredService<DatabasePlugin>();
pluginProvider.RegisterPlugin(dbPlugin);
```

### Pattern 2: Stateful Plugin (Session-based)

```csharp
[Plugin(Name = "ShoppingCart", Description = "Manage shopping cart")]
public class ShoppingCartPlugin
{
    private readonly Dictionary<string, CartItem> _items = new();

    [KernelFunction("add_item")]
    [Description("Add item to cart")]
    public string AddItem(
        [Description("Item name")] string name,
        [Description("Price")] double price,
        [Description("Quantity")] int quantity = 1)
    {
        if (_items.ContainsKey(name))
        {
            _items[name] = _items[name] with { Quantity = _items[name].Quantity + quantity };
        }
        else
        {
            _items[name] = new CartItem(name, price, quantity);
        }

        return $"Added {quantity}x {name} to cart";
    }

    [KernelFunction("get_total")]
    [Description("Calculate total cost")]
    public string GetTotal()
    {
        var total = _items.Values.Sum(i => i.Price * i.Quantity);
        return $"Cart total: ${total:F2}";
    }

    private record CartItem(string Name, double Price, int Quantity);
}
```

### Pattern 3: Plugin with Validation

```csharp
[Plugin(Name = "Email", Description = "Send emails")]
public class EmailPlugin
{
    [KernelFunction("send_email")]
    [Description("Send an email")]
    public async Task<string> SendEmailAsync(
        [Description("Recipient email address")] string to,
        [Description("Email subject")] string subject,
        [Description("Email body")] string body)
    {
        // Validate email
        if (!IsValidEmail(to))
            return "Error: Invalid email address";

        // Validate length
        if (subject.Length > 200)
            return "Error: Subject too long (max 200 characters)";

        if (body.Length > 10000)
            return "Error: Body too long (max 10000 characters)";

        // Send email (implementation)
        await SendEmailInternalAsync(to, subject, body);
        
        return $"Email sent to {to}";
    }

    private bool IsValidEmail(string email)
    {
        return email.Contains("@") && email.Contains(".");
    }

    private async Task SendEmailInternalAsync(string to, string subject, string body)
    {
        // Implementation
    }
}
```

### Pattern 4: Plugin with Rate Limiting

```csharp
[Plugin(Name = "ApiCaller", Description = "Call external APIs")]
public class ApiCallerPlugin
{
    private readonly SemaphoreSlim _rateLimiter;
    private readonly int _maxCallsPerMinute = 60;

    public ApiCallerPlugin()
    {
        _rateLimiter = new SemaphoreSlim(_maxCallsPerMinute);
        
        // Reset rate limit every minute
        Task.Run(async () =>
        {
            while (true)
            {
                await Task.Delay(TimeSpan.FromMinutes(1));
                _rateLimiter.Release(_maxCallsPerMinute - _rateLimiter.CurrentCount);
            }
        });
    }

    [KernelFunction("call_api")]
    [Description("Make an API call")]
    public async Task<string> CallApiAsync(
        [Description("API endpoint URL")] string url)
    {
        if (!await _rateLimiter.WaitAsync(TimeSpan.FromSeconds(5)))
        {
            return "Error: Rate limit exceeded. Please try again later.";
        }

        try
        {
            using var client = new HttpClient();
            return await client.GetStringAsync(url);
        }
        finally
        {
            // Rate limit token is not released - consumed for this minute
        }
    }
}
```

## Best Practices

### 1. Error Handling

```csharp
// ✅ Good: Return error messages, don't throw
[KernelFunction("divide")]
public string Divide(double a, double b)
{
    if (b == 0)
        return "Error: Cannot divide by zero";
    
    return (a / b).ToString();
}

// ❌ Bad: Throwing exceptions
[KernelFunction("divide")]
public double Divide(double a, double b)
{
    if (b == 0)
        throw new ArgumentException("Division by zero");
    
    return a / b; // Exception propagates to LLM
}
```

### 2. Clear Descriptions

```csharp
// ✅ Good: Specific and actionable
[KernelFunction("convert_temperature")]
[Description("Convert temperature from Celsius to Fahrenheit. Input must be a number representing degrees Celsius.")]
public double ConvertToFahrenheit(
    [Description("Temperature in Celsius (e.g., 25.5)")] double celsius)

// ❌ Bad: Vague
[KernelFunction("convert")]
[Description("Converts stuff")]
public double Convert([Description("Input")] double input)
```

### 3. Return Format

```csharp
// ✅ Good: Structured, parseable
[KernelFunction("get_weather")]
public string GetWeather(string city)
{
    return $"""
        City: {city}
        Temperature: 22°C
        Condition: Sunny
        Humidity: 45%
        """;
}

// ✅ Also Good: JSON for complex data
[KernelFunction("get_user")]
public string GetUser(int id)
{
    var user = new { Id = id, Name = "John", Email = "john@example.com" };
    return JsonSerializer.Serialize(user);
}
```

### 4. Async Operations

```csharp
// ✅ Good: Async for I/O operations
[KernelFunction("fetch_url")]
public async Task<string> FetchUrlAsync(string url)
{
    using var client = new HttpClient();
    return await client.GetStringAsync(url);
}

// ❌ Bad: Blocking synchronous I/O
[KernelFunction("fetch_url")]
public string FetchUrl(string url)
{
    using var client = new HttpClient();
    return client.GetStringAsync(url).Result; // Blocks thread
}
```

### 5. Parameter Naming

```csharp
// ✅ Good: Clear, descriptive names
public string CreateUser(
    [Description("User's full name")] string fullName,
    [Description("User's email address")] string emailAddress,
    [Description("User's age in years")] int ageInYears)

// ❌ Bad: Ambiguous names
public string CreateUser(
    [Description("Name")] string name,
    [Description("Email")] string email,
    [Description("Age")] int age)
```

## Testing Plugins

### Unit Testing

```csharp
using Xunit;

public class CalculatorPluginTests
{
    private readonly CalculatorPlugin _calculator;

    public CalculatorPluginTests()
    {
        _calculator = new CalculatorPlugin();
    }

    [Fact]
    public void Add_ShouldReturnSum()
    {
        // Arrange
        double a = 5, b = 3;

        // Act
        var result = _calculator.Add(a, b);

        // Assert
        Assert.Equal(8, result);
    }

    [Theory]
    [InlineData(10, 2, 5)]
    [InlineData(100, 10, 10)]
    [InlineData(7, 2, 3.5)]
    public void Divide_ShouldReturnQuotient(double a, double b, double expected)
    {
        // Act
        var result = _calculator.Divide(a, b);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Divide_ByZero_ShouldReturnError()
    {
        // Act
        var result = _calculator.Divide(10, 0);

        // Assert
        Assert.Contains("Error", result);
    }
}
```

### Integration Testing

```csharp
public class PluginIntegrationTests
{
    [Fact]
    public async Task Plugin_ShouldBeCallableByEngine()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSkillBot(configuration);
        
        var provider = services.BuildServiceProvider();
        var engine = provider.GetRequiredService<IAgentEngine>();
        var pluginProvider = provider.GetRequiredService<IPluginProvider>();
        
        pluginProvider.RegisterPlugin(new CalculatorPlugin());

        // Act
        var response = await engine.ExecuteAsync("What's 5 + 3?");

        // Assert
        Assert.Contains("8", response.Content);
        Assert.True(response.ToolCalls.Any());
    }
}
```

## Deployment

### Package as Separate Assembly

```csharp
// Create new project
dotnet new classlib -n MyCustomPlugins

// Add reference to SkillBot.Core
dotnet add reference ../SkillBot.Core/SkillBot.Core.csproj

// Build
dotnet build

// Load at runtime
await pluginProvider.RegisterPluginsFromAssemblyAsync("./MyCustomPlugins.dll");
```

### Configuration

```json
{
  "SkillBot": {
    "PluginAssemblyPaths": [
      "./plugins/CustomPlugins.dll",
      "./plugins/EnterprisePlugins.dll"
    ]
  }
}
```

---

## Common Pitfalls

❌ **Don't**: Make plugins stateful across users  
✅ **Do**: Create new instance per user/session

❌ **Don't**: Block on I/O operations  
✅ **Do**: Use async/await

❌ **Don't**: Throw exceptions for expected errors  
✅ **Do**: Return error messages as strings

❌ **Don't**: Use vague parameter names  
✅ **Do**: Be specific and descriptive

❌ **Don't**: Forget to validate inputs  
✅ **Do**: Validate all parameters

---

**Document Version**: 1.0  
**Last Updated**: 2026-04-16  
**For Support**: See main README.md
