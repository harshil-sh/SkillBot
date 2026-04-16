# Contributing to SkillBot

Thank you for your interest in contributing to SkillBot! 🎉

This document provides guidelines for contributing to the project. Whether you're fixing bugs, adding features, improving documentation, or creating new plugins, we appreciate your help!

## Table of Contents
- [Code of Conduct](#code-of-conduct)
- [Getting Started](#getting-started)
- [How to Contribute](#how-to-contribute)
- [Development Workflow](#development-workflow)
- [Coding Standards](#coding-standards)
- [Testing Guidelines](#testing-guidelines)
- [Documentation](#documentation)
- [Pull Request Process](#pull-request-process)
- [Community](#community)

## Code of Conduct

### Our Pledge

We are committed to providing a welcoming and inclusive experience for everyone. We expect all contributors to:

- ✅ Be respectful and constructive
- ✅ Welcome newcomers and help them learn
- ✅ Focus on what's best for the community
- ✅ Show empathy towards others

### Unacceptable Behavior

- ❌ Harassment or discriminatory language
- ❌ Trolling or insulting comments
- ❌ Personal or political attacks
- ❌ Publishing others' private information

## Getting Started

### Prerequisites

Before contributing, ensure you have:

```bash
# Required
- .NET 10 SDK or later
- Git
- Your favorite IDE (Visual Studio, VS Code, or Rider)

# Recommended
- Docker (for testing containerization)
- SQLite browser (for database inspection)
```

### Fork and Clone

```bash
# 1. Fork the repository on GitHub

# 2. Clone your fork
git clone https://github.com/YOUR_USERNAME/skillbot.git
cd skillbot

# 3. Add upstream remote
git remote add upstream https://github.com/ORIGINAL_OWNER/skillbot.git

# 4. Create a branch
git checkout -b feature/your-feature-name
```

### Build and Run

```bash
# Restore dependencies
dotnet restore

# Build
dotnet build

# Run tests
dotnet test

# Run the application
cd SkillBot.Console
dotnet run
```

## How to Contribute

### Types of Contributions

We welcome various types of contributions:

#### 🐛 Bug Reports

Found a bug? Please report it!

**Before submitting:**
- Check if the issue already exists
- Try to reproduce it consistently
- Gather relevant information (error messages, logs)

**Include in your report:**
- Clear title and description
- Steps to reproduce
- Expected vs actual behavior
- Environment details (.NET version, OS)
- Relevant code snippets or logs

**Example:**
```markdown
**Bug**: SQLite database locks when running multiple instances

**To Reproduce**:
1. Start SkillBot in one terminal
2. Start another instance in a second terminal
3. Execute a command in the second instance

**Expected**: Both instances work independently
**Actual**: Second instance throws "database is locked" error

**Environment**:
- OS: Ubuntu 22.04
- .NET: 10.0.1
- SkillBot: v1.0.0
```

#### ✨ Feature Requests

Have an idea? We'd love to hear it!

**Before submitting:**
- Check if it's already been requested
- Consider if it fits the project's scope
- Think about implementation complexity

**Include in your request:**
- Clear use case description
- Proposed solution
- Alternative approaches considered
- Willingness to implement it yourself

#### 📝 Documentation Improvements

Documentation contributions are valuable!

- Fix typos or unclear explanations
- Add examples or tutorials
- Improve API documentation
- Translate documentation

#### 🔌 New Plugins

Creating plugins is a great way to contribute!

See our [Plugin Development Guide](PLUGIN-DEVELOPMENT.md) for details.

**Good plugin candidates:**
- Integrate with popular APIs (GitHub, Slack, etc.)
- Add useful utilities (file operations, data conversion)
- Provide domain-specific tools (finance, science, etc.)

## Development Workflow

### 1. Create a Branch

```bash
# Feature branch
git checkout -b feature/add-github-plugin

# Bug fix branch
git checkout -b fix/sqlite-lock-issue

# Documentation branch
git checkout -b docs/improve-readme
```

### 2. Make Changes

Follow our [Coding Standards](#coding-standards) when writing code.

```bash
# Make your changes
# ...

# Check status
git status

# Add changes
git add .

# Commit with clear message
git commit -m "Add GitHub integration plugin"
```

### 3. Keep Your Branch Updated

```bash
# Fetch upstream changes
git fetch upstream

# Rebase your branch
git rebase upstream/main

# Resolve any conflicts if needed
```

### 4. Push and Create PR

```bash
# Push to your fork
git push origin feature/add-github-plugin

# Create Pull Request on GitHub
```

## Coding Standards

### C# Style Guidelines

We follow standard C# conventions:

#### Naming Conventions

```csharp
// ✅ Classes: PascalCase
public class AgentEngine { }

// ✅ Interfaces: PascalCase with 'I' prefix
public interface IAgentEngine { }

// ✅ Methods: PascalCase
public void ExecuteAsync() { }

// ✅ Properties: PascalCase
public string Name { get; set; }

// ✅ Private fields: _camelCase with underscore
private readonly ILogger _logger;

// ✅ Parameters and locals: camelCase
public void Process(string userName) { }

// ✅ Constants: PascalCase
public const int MaxRetries = 3;
```

#### Code Organization

```csharp
// ✅ Order class members logically
public class MyClass
{
    // 1. Constants
    private const int DefaultTimeout = 30;
    
    // 2. Fields
    private readonly IService _service;
    
    // 3. Constructor
    public MyClass(IService service)
    {
        _service = service;
    }
    
    // 4. Public properties
    public string Name { get; set; }
    
    // 5. Public methods
    public void DoSomething() { }
    
    // 6. Private methods
    private void Helper() { }
}
```

#### Modern C# Features

```csharp
// ✅ Use records for immutable data
public record AgentMessage
{
    public required string Role { get; init; }
    public required string Content { get; init; }
}

// ✅ Use nullable reference types
public string? GetOptionalValue() { }

// ✅ Use pattern matching
if (response is { IsSuccess: true, Data: not null })
{
    ProcessData(response.Data);
}

// ✅ Use async/await (no .Result or .Wait())
public async Task<string> FetchDataAsync()
{
    return await _client.GetStringAsync(url);
}
```

#### Comments and Documentation

```csharp
// ✅ XML documentation for public APIs
/// <summary>
/// Executes the agent with the given message.
/// </summary>
/// <param name="message">User input message</param>
/// <param name="cancellationToken">Cancellation token</param>
/// <returns>Agent response</returns>
public async Task<AgentResponse> ExecuteAsync(
    string message,
    CancellationToken cancellationToken = default)
{
    // ✅ Comments explain WHY, not WHAT
    // Use cached response if available to save API costs
    if (TryGetCachedResponse(message, out var cached))
        return cached;
    
    return await ProcessNewMessageAsync(message, cancellationToken);
}
```

### Code Quality

#### Error Handling

```csharp
// ✅ Handle expected errors gracefully
public async Task<string> FetchDataAsync(string url)
{
    try
    {
        return await _client.GetStringAsync(url);
    }
    catch (HttpRequestException ex)
    {
        _logger.LogWarning(ex, "Failed to fetch data from {Url}", url);
        return "Error: Unable to fetch data";
    }
}

// ❌ Don't swallow exceptions
public void Process()
{
    try
    {
        // code
    }
    catch (Exception) { } // Bad: exception lost
}
```

#### Resource Management

```csharp
// ✅ Use 'using' for disposables
public async Task ProcessFileAsync(string path)
{
    using var stream = File.OpenRead(path);
    await ProcessStreamAsync(stream);
}

// ✅ Async disposal
public async Task ProcessAsync()
{
    await using var connection = new SqliteConnection(connectionString);
    await connection.OpenAsync();
}
```

#### Null Handling

```csharp
// ✅ Use null-conditional operator
var length = text?.Length ?? 0;

// ✅ Validate parameters
public void Process(string input)
{
    ArgumentException.ThrowIfNullOrEmpty(input);
    // ... process
}
```

### Project Structure

When adding new files, follow the existing structure:

```
SkillBot/
├── SkillBot.Core/              # Pure domain (interfaces, models)
├── SkillBot.Infrastructure/    # Implementations
├── SkillBot.Plugins/           # Plugin implementations
└── SkillBot.Console/           # Host application
```

## Testing Guidelines

### Writing Tests

We use xUnit for testing:

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
    public void Add_ShouldReturnCorrectSum()
    {
        // Arrange
        var a = 5;
        var b = 3;
        
        // Act
        var result = _calculator.Add(a, b);
        
        // Assert
        Assert.Equal(8, result);
    }
    
    [Theory]
    [InlineData(10, 2, 5)]
    [InlineData(100, 10, 10)]
    [InlineData(7, 2, 3.5)]
    public void Divide_ShouldReturnCorrectQuotient(
        double a, double b, double expected)
    {
        // Act
        var result = _calculator.Divide(a, b);
        
        // Assert
        Assert.Equal(expected, result);
    }
}
```

### Test Coverage

Aim for:
- ✅ 80%+ code coverage for new features
- ✅ All public methods tested
- ✅ Edge cases covered
- ✅ Error paths tested

### Running Tests

```bash
# Run all tests
dotnet test

# Run specific test
dotnet test --filter "FullyQualifiedName~CalculatorPluginTests"

# Run with coverage
dotnet test /p:CollectCoverage=true

# Run in watch mode
dotnet watch test
```

## Documentation

### When to Update Documentation

Update docs when you:
- Add or change public APIs
- Add new features
- Change configuration options
- Fix bugs that affect behavior
- Add new plugins

### Documentation Files

- **README.md**: Project overview, quick start
- **PLUGIN-DEVELOPMENT.md**: How to create plugins
- **DEPLOYMENT.md**: Deployment instructions
- **CHANGELOG.md**: Version history

### Writing Good Documentation

```markdown
# ✅ Good: Clear, with examples

## Using the Calculator Plugin

The Calculator plugin provides basic arithmetic operations.

**Example**:
\`\`\`csharp
pluginProvider.RegisterPlugin(new CalculatorPlugin());
\`\`\`

**Usage**:
\`\`\`
You: What's 5 + 3?
Assistant: 5 plus 3 equals 8.
\`\`\`

# ❌ Bad: Vague, no examples

## Calculator

Does math stuff.
```

## Pull Request Process

### Before Submitting

**Checklist**:
- [ ] Code follows style guidelines
- [ ] Tests added/updated and passing
- [ ] Documentation updated
- [ ] Commits are logical and well-described
- [ ] Branch is up to date with main

### PR Title Format

Use clear, descriptive titles:

```
✅ Good:
- "Add GitHub integration plugin"
- "Fix SQLite database locking issue"
- "Improve plugin error handling"

❌ Bad:
- "Update"
- "Fix bug"
- "Changes"
```

### PR Description Template

```markdown
## Description
Brief description of changes.

## Type of Change
- [ ] Bug fix
- [ ] New feature
- [ ] Breaking change
- [ ] Documentation update

## Testing
Describe testing done.

## Checklist
- [ ] Tests pass locally
- [ ] Documentation updated
- [ ] No breaking changes (or documented)
```

### Review Process

1. **Automated Checks**: CI/CD must pass
2. **Code Review**: At least one maintainer approval
3. **Discussion**: Address feedback constructively
4. **Approval**: Maintainer merges PR

### After Merge

```bash
# Update your local main
git checkout main
git pull upstream main

# Delete feature branch
git branch -d feature/your-feature

# Delete remote branch
git push origin --delete feature/your-feature
```

## Community

### Getting Help

- **GitHub Issues**: Report bugs or ask questions
- **Discussions**: General questions and ideas
- **Documentation**: Check guides first

### Recognition

Contributors are recognized in:
- README.md contributors section
- Release notes
- GitHub contributors page

### License

By contributing, you agree that your contributions will be licensed under the project's MIT License.

---

## Quick Contribution Checklist

- [ ] Fork and clone the repository
- [ ] Create a feature branch
- [ ] Make changes following coding standards
- [ ] Add/update tests
- [ ] Update documentation
- [ ] Commit with clear messages
- [ ] Push and create PR
- [ ] Address review feedback
- [ ] Celebrate when merged! 🎉

---

**Questions?** Open an issue or discussion on GitHub!

**Thank you for contributing to SkillBot!** 🚀
