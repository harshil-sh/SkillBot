# SkillBot Web — State Management

Blazor WebAssembly has no framework-mandated state management solution. SkillBot.Web uses a deliberate three-tier approach, each tier appropriate for a different scope and lifetime of state.

---

## Table of Contents

1. [Overview](#1-overview)
2. [Tier 1 — Component-Local State](#2-tier-1--component-local-state)
3. [Tier 2 — Scoped Service State](#3-tier-2--scoped-service-state)
4. [Tier 3 — Persisted State (localStorage)](#4-tier-3--persisted-state-localstorage)
5. [Authentication State](#5-authentication-state)
6. [Theme State](#6-theme-state)
7. [Chat State](#7-chat-state)
8. [Decision Guide — Which Tier to Use?](#8-decision-guide--which-tier-to-use)
9. [Cascading Parameters](#9-cascading-parameters)
10. [Avoiding Common Pitfalls](#10-avoiding-common-pitfalls)

---

## 1. Overview

```
┌──────────────────────────────────────────────────────────────┐
│ Tier 3 — localStorage                                        │
│ JWT token · theme preference · console preferences          │
│ Survives full page reload / tab close / app restart         │
├──────────────────────────────────────────────────────────────┤
│ Tier 2 — Scoped Services (singleton in WASM lifetime)        │
│ CustomAuthStateProvider · ThemeService                       │
│ Survives navigation between pages                            │
├──────────────────────────────────────────────────────────────┤
│ Tier 1 — Component @code fields                              │
│ Loading flags · form values · current conversation           │
│ Destroyed when component is removed from render tree         │
└──────────────────────────────────────────────────────────────┘
```

> **Note:** In Blazor WebAssembly, `Scoped` services are effectively singletons — there is only one DI scope per WASM instance. This makes them safe to use as in-memory application state stores.

---

## 2. Tier 1 — Component-Local State

### When to use

- Transient UI state: loading spinners, error messages, open/closed dialogs.
- Form input values that have not yet been saved.
- State that is only meaningful to a single component and its direct children.

### Pattern

Declare private fields in the `@code` block. Blazor automatically re-renders the component when a synchronous assignment is followed by a UI interaction or `StateHasChanged()`.

```razor
@* Chat.razor *@
@code {
    private bool _isLoading;
    private string _inputText = string.Empty;
    private string? _errorMessage;
    private List<ChatMessageDto> _messages = new();

    private async Task SendMessageAsync()
    {
        _isLoading = true;
        _errorMessage = null;
        // StateHasChanged() is NOT needed here — Blazor re-renders
        // automatically after an awaited task completes in an event handler.

        try
        {
            var response = await ApiClient.SendMessageAsync(new ChatRequest(_inputText));
            _messages.Add(new ChatMessageDto("assistant", response.Reply, DateTime.UtcNow, response.TokenCount));
            _inputText = string.Empty;
        }
        catch (HttpRequestException ex)
        {
            _errorMessage = $"Could not reach the API: {ex.Message}";
        }
        finally
        {
            _isLoading = false;
        }
    }
}
```

### Re-render triggers

| Situation | Re-render automatic? |
|-----------|:-------------------:|
| Event handler (`@onclick`, `@onchange`) completes | ✅ |
| `EventCallback` invoked by child component | ✅ |
| `await` in `OnInitializedAsync` / `OnParametersSetAsync` | ✅ |
| Background `Task` outside Blazor's event loop | ❌ — call `InvokeAsync(StateHasChanged)` |
| Timer callback | ❌ — call `InvokeAsync(StateHasChanged)` |

---

## 3. Tier 2 — Scoped Service State

### When to use

- State that must survive navigation to a different page.
- State consumed by multiple unrelated components (e.g., both `NavMenu` and `Chat.razor` need to know if the user is authenticated).
- State that would be wasteful to reload from the API on every page visit.

### Registration

```csharp
// Program.cs
builder.Services.AddScoped<ISkillBotApiClient, SkillBotApiClient>();
builder.Services.AddScoped<CustomAuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(
    sp => sp.GetRequiredService<CustomAuthStateProvider>());
builder.Services.AddScoped<ThemeService>();
```

### Consuming a service

```razor
@inject ThemeService ThemeService

<MudSwitch T="bool"
           Checked="ThemeService.IsDarkMode"
           CheckedChanged="async _ => await ThemeService.ToggleThemeAsync()"
           Label="Dark mode" />
```

### Change notification pattern

When a service mutates state that components need to react to, use the `event Action?` pattern:

```csharp
public class ThemeService
{
    public bool IsDarkMode { get; private set; }
    public event Action? OnThemeChanged;

    public async Task ToggleThemeAsync()
    {
        IsDarkMode = !IsDarkMode;
        await PersistToStorageAsync();
        OnThemeChanged?.Invoke();
    }
}
```

Components subscribe in `OnInitialized` and unsubscribe in `Dispose`:

```razor
@implements IDisposable
@inject ThemeService ThemeService

@code {
    protected override void OnInitialized()
    {
        ThemeService.OnThemeChanged += StateHasChanged;
    }

    public void Dispose()
    {
        ThemeService.OnThemeChanged -= StateHasChanged;
    }
}
```

---

## 4. Tier 3 — Persisted State (localStorage)

### When to use

- State that must survive a full browser reload or tab close.
- User preferences (theme, font size).
- The JWT token (so the user stays logged in across sessions).

### Access via JS Interop

```csharp
// Inject IJSRuntime
@inject IJSRuntime JS

// Write
await JS.InvokeVoidAsync("localStorage.setItem", "skillbot_theme", "dark");

// Read
var theme = await JS.InvokeAsync<string?>("localStorage.getItem", "skillbot_theme");

// Delete
await JS.InvokeVoidAsync("localStorage.removeItem", "skillbot_theme");
```

### Keys used by SkillBot.Web

| Key | Value | Set by |
|-----|-------|--------|
| `skillbot_jwt` | JWT string | `CustomAuthStateProvider` |
| `skillbot_theme` | `"dark"` or `"light"` | `ThemeService` |
| `skillbot_fontsize` | Integer string (px) | `AppearanceSettings` |

---

## 5. Authentication State

Authentication state is managed by `CustomAuthStateProvider`, which extends Blazor's `AuthenticationStateProvider`.

### Flow

```
App start
    │
    ▼
CustomAuthStateProvider.GetAuthenticationStateAsync()
    │
    ├── Read "skillbot_jwt" from localStorage
    │
    ├── [No token / expired] ──► return AnonymousPrincipal
    │
    └── [Valid token] ──► parse claims ──► return ClaimsPrincipal
```

### Implementation sketch

```csharp
public class CustomAuthStateProvider : AuthenticationStateProvider
{
    private readonly IJSRuntime _js;
    private readonly ISkillBotApiClient _api;

    public CustomAuthStateProvider(IJSRuntime js, ISkillBotApiClient api)
    {
        _js = js;
        _api = api;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = await _js.InvokeAsync<string?>("localStorage.getItem", "skillbot_jwt");

        if (string.IsNullOrWhiteSpace(token) || IsTokenExpired(token))
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));

        var claims = ParseClaimsFromJwt(token);
        var identity = new ClaimsIdentity(claims, "jwt");
        return new AuthenticationState(new ClaimsPrincipal(identity));
    }

    public async Task MarkUserAsAuthenticated(string token)
    {
        await _js.InvokeVoidAsync("localStorage.setItem", "skillbot_jwt", token);
        var claims = ParseClaimsFromJwt(token);
        var identity = new ClaimsIdentity(claims, "jwt");
        var user = new ClaimsPrincipal(identity);
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(user)));
    }

    public async Task MarkUserAsLoggedOut()
    {
        await _js.InvokeVoidAsync("localStorage.removeItem", "skillbot_jwt");
        NotifyAuthenticationStateChanged(
            Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()))));
    }

    private static bool IsTokenExpired(string token)
    {
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);
        return jwt.ValidTo < DateTime.UtcNow;
    }

    private static IEnumerable<Claim> ParseClaimsFromJwt(string token)
    {
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);
        return jwt.Claims;
    }
}
```

### Consuming authentication state in components

**Option A — `<AuthorizeView>`** (declarative, preferred for UI sections):

```razor
<AuthorizeView>
    <Authorized>
        <p>Welcome, @context.User.Identity?.Name!</p>
    </Authorized>
    <NotAuthorized>
        <p><a href="/login">Please log in</a></p>
    </NotAuthorized>
</AuthorizeView>
```

**Option B — `[CascadingParameter]`** (imperative, for logic in `@code`):

```razor
@code {
    [CascadingParameter]
    private Task<AuthenticationState> AuthStateTask { get; set; } = null!;

    private string? _username;

    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthStateTask;
        _username = authState.User.Identity?.Name;
    }
}
```

**Option C — `AuthenticationStateProvider` injection** (for services):

```csharp
public class SkillBotApiClient : ISkillBotApiClient
{
    private readonly HttpClient _http;
    private readonly AuthenticationStateProvider _authProvider;

    public async Task<string?> GetTokenAsync()
    {
        var state = await _authProvider.GetAuthenticationStateAsync();
        // retrieve token from localStorage if needed
    }
}
```

---

## 6. Theme State

`ThemeService` is the single source of truth for the current color scheme.

```csharp
public class ThemeService
{
    private readonly IJSRuntime _js;

    public bool IsDarkMode { get; private set; }
    public event Action? OnThemeChanged;

    public ThemeService(IJSRuntime js) => _js = js;

    /// <summary>Called once during app startup to restore preference.</summary>
    public async Task LoadFromStorageAsync()
    {
        var stored = await _js.InvokeAsync<string?>("localStorage.getItem", "skillbot_theme");
        IsDarkMode = stored == "dark";
        OnThemeChanged?.Invoke();
    }

    public async Task ToggleThemeAsync()
    {
        IsDarkMode = !IsDarkMode;
        await _js.InvokeVoidAsync(
            "localStorage.setItem", "skillbot_theme", IsDarkMode ? "dark" : "light");
        OnThemeChanged?.Invoke();
    }
}
```

`MainLayout.razor` loads the preference in `OnAfterRenderAsync` (because JS interop is not available during pre-render):

```razor
@code {
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await ThemeService.LoadFromStorageAsync();
            StateHasChanged();
        }
    }
}
```

---

## 7. Chat State

Chat state lives entirely in `Chat.razor` as component-local (Tier 1) state. It is reset each time the user navigates away from `/chat` and back, which loads the conversation list fresh from the API.

```razor
@code {
    // --- Current conversation ---
    private string? _currentConversationId;
    private List<ChatMessageDto> _messages = new();

    // --- Sidebar ---
    private List<ConversationDto> _conversations = new();

    // --- Input ---
    private string _inputText = string.Empty;
    private bool _multiAgentMode;

    // --- UI state ---
    private bool _initializing = true;
    private bool _isLoading;
    private string? _errorMessage;

    protected override async Task OnInitializedAsync()
    {
        _conversations = (await ApiClient.GetConversationsAsync()).ToList();
        _initializing = false;
    }
}
```

**Why not a service?** Conversation data is user-specific, server-authoritative, and inexpensive to reload. Keeping it local avoids stale-state bugs and simplifies reasoning about when data is fresh.

---

## 8. Decision Guide — Which Tier to Use?

| Question | Tier |
|----------|------|
| Is this a form field or transient flag (loading, error)? | **Tier 1** — component field |
| Does it need to survive navigation to another page? | **Tier 2** — scoped service |
| Is it user authentication or a token? | **Tier 2 + 3** — `CustomAuthStateProvider` (service + localStorage) |
| Is it a UI preference that must survive reload? | **Tier 3** — localStorage (via a service) |
| Does it come fresh from the API on every page visit? | **Tier 1** — load in `OnInitializedAsync` |

---

## 9. Cascading Parameters

MudBlazor's `MudThemeProvider` is a cascading parameter source. You don't need to pass the theme down manually — MudBlazor components pick it up automatically.

Custom cascading values can be added in `MainLayout.razor` with `<CascadingValue>`:

```razor
<!-- Example: cascading the current user's display name -->
<CascadingValue Value="_username">
    @Body
</CascadingValue>
```

Consumed as:

```razor
@code {
    [CascadingParameter]
    private string? Username { get; set; }
}
```

Use this sparingly. Prefer injection of services over cascading parameters for non-UI data.

---

## 10. Avoiding Common Pitfalls

### ❌ Calling `StateHasChanged()` in synchronous event handlers

Blazor calls `StateHasChanged` automatically after every event handler. Calling it yourself is redundant and can cause double renders.

```csharp
// ❌ Redundant
private void ToggleMenu()
{
    _menuOpen = !_menuOpen;
    StateHasChanged(); // not needed
}

// ✅ Correct
private void ToggleMenu() => _menuOpen = !_menuOpen;
```

### ❌ Mutating state from a background Thread without `InvokeAsync`

```csharp
// ❌ Will throw or silently fail
_ = Task.Run(() => {
    _messages.Add(newMsg);
    StateHasChanged(); // Wrong thread
});

// ✅ Marshal back to the UI thread
_ = Task.Run(async () => {
    await InvokeAsync(() => {
        _messages.Add(newMsg);
        StateHasChanged();
    });
});
```

### ❌ Storing server-fetched data in a global service

Server data (conversation history, user profiles) changes on the server. Cache it in a service only if you implement an explicit invalidation strategy.

### ✅ Dispose event subscriptions

Always unsubscribe from service events in `IDisposable.Dispose()` to prevent memory leaks and phantom re-renders on disposed components.

---

*See also: [ARCHITECTURE.md](ARCHITECTURE.md) · [COMPONENTS.md](COMPONENTS.md)*
