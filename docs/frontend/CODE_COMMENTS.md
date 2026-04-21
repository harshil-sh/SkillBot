# SkillBot Web — XML Documentation Comments Convention

All public types, public members, and non-trivial internal members in `SkillBot.Web` are documented with XML doc comments (`/// <summary>`). This enables IDE IntelliSense tooltips and supports future API documentation generation (e.g., via `docfx` or `xmldocmd`).

---

## Table of Contents

1. [Why XML Comments?](#1-why-xml-comments)
2. [Comment Elements Reference](#2-comment-elements-reference)
3. [Class-Level Comments](#3-class-level-comments)
4. [Method-Level Comments](#4-method-level-comments)
5. [Property and Parameter Comments](#5-property-and-parameter-comments)
6. [Blazor Component Conventions](#6-blazor-component-conventions)
7. [Interface Documentation](#7-interface-documentation)
8. [What NOT to Document](#8-what-not-to-document)
9. [XML Comment Linting](#9-xml-comment-linting)

---

## 1. Why XML Comments?

- **IDE tooltips:** When a developer hovers over `ApiClient.LoginAsync()`, the summary appears in the IntelliSense popup — no need to open the file.
- **Onboarding:** New contributors can understand a component's purpose without reading its implementation.
- **API surface clarity:** Documenting parameters and return values forces authors to think about the contract, often revealing design issues early.
- **Tooling compatibility:** XML docs can be extracted by `docfx`, `Sandcastle`, or shipped as `.xml` alongside a NuGet package.

---

## 2. Comment Elements Reference

| Element | Purpose |
|---------|---------|
| `<summary>` | One-sentence description of what a type or member does |
| `<param name="x">` | Describes a method/constructor parameter |
| `<returns>` | Describes the return value |
| `<exception cref="T">` | Documents a thrown exception |
| `<remarks>` | Additional context, caveats, implementation notes |
| `<example>` | Usage example (can include `<code>` blocks) |
| `<see cref="T"/>` | Cross-reference to another type/member |
| `<seealso cref="T"/>` | "See also" cross-reference (appears in the See Also section) |
| `<inheritdoc/>` | Inherit the comment from an overridden/implemented member |

---

## 3. Class-Level Comments

Summarise the responsibility of a class/component in one sentence. Add `<remarks>` for longer explanations.

```csharp
/// <summary>
/// Manages JWT authentication state for the Blazor WebAssembly application.
/// </summary>
/// <remarks>
/// Extends <see cref="AuthenticationStateProvider"/> and persists the token in
/// <c>localStorage</c> under the key <c>"skillbot_jwt"</c>. On app startup the
/// stored token is validated (expiry check) and, if still valid, the user is
/// automatically restored to an authenticated state without a round-trip to the API.
/// </remarks>
public class CustomAuthStateProvider : AuthenticationStateProvider
{ }
```

```csharp
/// <summary>
/// Manages the application's color palette (dark / light mode).
/// </summary>
/// <remarks>
/// Persists the user's preference in <c>localStorage</c> under the key
/// <c>"skillbot_theme"</c>. Fire <see cref="OnThemeChanged"/> to notify
/// subscribed components to re-render.
/// </remarks>
public class ThemeService
{ }
```

---

## 4. Method-Level Comments

Document every public method. Use `<param>` and `<returns>` for non-obvious signatures.

```csharp
/// <summary>
/// Authenticates the user and persists the returned JWT.
/// </summary>
/// <param name="token">The raw JWT string returned by <c>POST /api/auth/login</c>.</param>
/// <remarks>
/// Parses the token's claims to build a <see cref="ClaimsPrincipal"/>, stores the
/// token in <c>localStorage</c>, and calls
/// <see cref="AuthenticationStateProvider.NotifyAuthenticationStateChanged"/> so
/// all <see cref="Microsoft.AspNetCore.Components.Authorization.AuthorizeView"/>
/// components re-render with the authenticated state.
/// </remarks>
public async Task MarkUserAsAuthenticated(string token) { }
```

```csharp
/// <summary>
/// Sends a chat message to the single-agent endpoint and returns the reply.
/// </summary>
/// <param name="request">The chat request containing the user message and optional conversation ID.</param>
/// <param name="ct">Cancellation token; defaults to <see cref="CancellationToken.None"/>.</param>
/// <returns>
/// A <see cref="ChatResponse"/> containing the assistant reply, updated conversation ID,
/// and token usage statistics.
/// </returns>
/// <exception cref="HttpRequestException">
/// Thrown when the API returns a non-2xx status code.
/// The <see cref="HttpRequestException.StatusCode"/> property indicates the HTTP status.
/// </exception>
public async Task<ChatResponse> SendMessageAsync(ChatRequest request, CancellationToken ct = default) { }
```

---

## 5. Property and Parameter Comments

Document `[Parameter]` properties on Blazor components, and record constructor properties for model types.

### Blazor component parameters

```csharp
/// <summary>The message to render in this bubble.</summary>
[Parameter, EditorRequired]
public ChatMessageDto Message { get; set; } = null!;

/// <summary>
/// When <c>true</c>, displays the message timestamp below the bubble.
/// Defaults to <c>true</c>.
/// </summary>
[Parameter]
public bool ShowTimestamp { get; set; } = true;

/// <summary>
/// Raised when the user clicks the "Copy" icon on this message.
/// Carries the message content as the event argument.
/// </summary>
[Parameter]
public EventCallback<string> OnCopyClicked { get; set; }
```

### Record types

```csharp
/// <summary>Represents a single message in a chat conversation.</summary>
/// <param name="Role">The sender role: <c>"user"</c> or <c>"assistant"</c>.</param>
/// <param name="Content">The raw message text (may contain Markdown).</param>
/// <param name="Timestamp">UTC timestamp when the message was created.</param>
/// <param name="TokenCount">Number of tokens consumed by this message, if known.</param>
public record ChatMessageDto(
    string Role,
    string Content,
    DateTime Timestamp,
    int? TokenCount
);
```

---

## 6. Blazor Component Conventions

### `.razor` files

Because `.razor` files are compiled, XML doc comments go on the class defined by the `@code` block. For components that are entirely markup (no `@code`), add a `/// <summary>` comment at the top of the file inside a `@code {}` block:

```razor
@* ChatMessage.razor *@
@code {
    /// <summary>
    /// Renders a single chat message bubble with role-appropriate styling
    /// and markdown rendering for assistant messages.
    /// </summary>
}
```

Alternatively, use a code-behind partial class:

```csharp
// ChatMessage.razor.cs
/// <summary>
/// Renders a single chat message bubble with role-appropriate styling
/// and markdown rendering for assistant messages.
/// </summary>
public partial class ChatMessage { }
```

The code-behind approach is preferred for complex components because it keeps markup and C# documentation separate.

### `OnInitializedAsync` and lifecycle methods

```csharp
/// <summary>
/// Loads the conversation list from the API on initial render.
/// Sets <see cref="_initializing"/> to <c>false</c> when complete.
/// </summary>
protected override async Task OnInitializedAsync()
{
    _conversations = (await ApiClient.GetConversationsAsync()).ToList();
    _initializing = false;
}
```

---

## 7. Interface Documentation

Interfaces in `Services/` are the primary contracts and deserve the most thorough documentation. Implementations can use `/// <inheritdoc/>` to avoid duplication.

```csharp
/// <summary>
/// Abstracts all HTTP communication with the SkillBot REST API.
/// </summary>
/// <remarks>
/// Inject this interface in pages and components instead of <see cref="HttpClient"/>
/// directly. The default implementation (<see cref="SkillBotApiClient"/>) attaches
/// the JWT from <c>localStorage</c> automatically before each request.
/// </remarks>
public interface ISkillBotApiClient
{
    /// <summary>Authenticates with email and password; returns a JWT on success.</summary>
    /// <param name="request">Login credentials.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <exception cref="HttpRequestException">Thrown on 401 (wrong credentials) or network failure.</exception>
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default);
}

// Implementation — inherits docs from interface
public class SkillBotApiClient : ISkillBotApiClient
{
    /// <inheritdoc/>
    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        // ...
    }
}
```

---

## 8. What NOT to Document

Avoid noise comments that restate the obvious:

```csharp
// ❌ Adds no value
/// <summary>Gets or sets the value.</summary>
public string Value { get; set; }

// ❌ Restatement of the method name
/// <summary>This method sends the message.</summary>
private async Task SendMessageAsync() { }

// ❌ Implementation detail that belongs in a code comment, not XML doc
/// <summary>Uses a SHA-256 hash to build the cache key.</summary>
public async Task<string?> GetCachedResponseAsync(string prompt) { }
// Better: document the public contract; add an inline // comment for the SHA-256 detail.
```

Use plain `//` comments for implementation details, algorithm notes, and workarounds:

```csharp
// JS interop is not available during pre-render; defer to OnAfterRenderAsync.
protected override async Task OnAfterRenderAsync(bool firstRender)
{
    if (firstRender)
        await ThemeService.LoadFromStorageAsync();
}
```

---

## 9. XML Comment Linting

Enable XML documentation warnings in `SkillBot.Web.csproj` to catch missing comments on public APIs:

```xml
<PropertyGroup>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    <!-- CS1591: Missing XML comment for publicly visible type or member -->
    <NoWarn>$(NoWarn);CS1591</NoWarn>
    <!-- Remove CS1591 from NoWarn when you want strict enforcement -->
</PropertyGroup>
```

During active development `CS1591` is suppressed to avoid build noise. Before a public release, remove the `NoWarn` entry and fix all warnings.

---

*See also: [ARCHITECTURE.md](ARCHITECTURE.md) · [COMPONENTS.md](COMPONENTS.md) · [DEVELOPMENT.md](DEVELOPMENT.md)*
