# SkillBot Web — Component Reference

This document describes every significant Razor component in `SkillBot.Web`. Each entry covers **purpose**, **parameters**, **events**, and a **usage example**.

---

## Table of Contents

- [Layout Components](#layout-components)
  - [MainLayout](#mainlayout)
  - [NavMenu](#navmenu)
- [Page Components](#page-components)
  - [Home](#home)
  - [Login](#login)
  - [Register](#register)
  - [Chat](#chat)
  - [Profile](#profile)
  - [Settings](#settings)
  - [Admin/Dashboard](#admindashboard)
  - [Admin/Users](#adminusers)
  - [Admin/Analytics](#adminanalytics)
- [Chat Components](#chat-components)
  - [ChatMessage](#chatmessage)
  - [ConversationList](#conversationlist)
  - [TypingIndicator](#typingindicator)
- [Shared Components](#shared-components)
  - [MarkdownRenderer](#markdownrenderer)
  - [LoadingIndicator](#loadingindicator)
- [Settings Components](#settings-components)
  - [ApiKeysSettings](#apikeyssettings)
  - [GeneralSettings](#generalsettings)
  - [AppearanceSettings](#appearancesettings)
  - [PrivacySettings](#privacysettings)
  - [AboutSettings](#aboutsettings)

---

## Layout Components

### MainLayout

**File:** `Layout/MainLayout.razor`

**Purpose:** Shell component that provides the two-column layout: a collapsible sidebar containing `NavMenu` and a main content area that renders the active page via `@Body`. Also hosts the `MudThemeProvider` and `MudSnackbarProvider` at the root so every child component can trigger snackbar notifications.

**Inherits:** `LayoutComponentBase`

**Parameters:** None (receives `Body` via `LayoutComponentBase`).

**Injects:**
- `ThemeService` — reads `IsDarkMode` to configure `MudThemeProvider`

**Usage:** Applied automatically as `DefaultLayout` in `App.razor`. Not used directly.

```razor
<!-- App.razor -->
<AuthorizeRouteView RouteData="@routeData"
                    DefaultLayout="@typeof(MainLayout)" />
```

---

### NavMenu

**File:** `Layout/NavMenu.razor`

**Purpose:** Collapsible sidebar navigation rendered inside `MainLayout`. Displays navigation links that change based on authentication status (guests see Login/Register; authenticated users see Chat, Settings, Profile; admins additionally see the Admin section).

**Parameters:** None.

**Injects:**
- `NavigationManager` — for `href` resolution
- `AuthenticationStateProvider` — to show/hide auth-gated links

**Key behavior:**
- On mobile (< 960 px), the menu collapses and shows a hamburger toggle button.
- Active route is highlighted using `NavLink` with `NavLinkMatch.Prefix`.

**Example nav items rendered for authenticated users:**

```razor
<MudNavLink Href="/chat" Icon="@Icons.Material.Filled.Chat">Chat</MudNavLink>
<MudNavLink Href="/settings" Icon="@Icons.Material.Filled.Settings">Settings</MudNavLink>
<MudNavLink Href="/profile" Icon="@Icons.Material.Filled.Person">Profile</MudNavLink>
<AuthorizeView Roles="Admin">
    <MudNavLink Href="/admin" Icon="@Icons.Material.Filled.AdminPanelSettings">Admin</MudNavLink>
</AuthorizeView>
```

---

## Page Components

### Home

**File:** `Pages/Home.razor`
**Route:** `@page "/"`

**Purpose:** Landing page. Unauthenticated visitors see a marketing summary with links to Login and Register. Authenticated users are redirected to `/chat` automatically on load.

**Parameters:** None.

**Injects:** `NavigationManager`, `AuthenticationStateProvider`

---

### Login

**File:** `Pages/Login.razor`
**Route:** `@page "/login"`

**Purpose:** Renders a MudBlazor form for email + password login. On success, delegates to `CustomAuthStateProvider` to persist the JWT and navigates to the `returnUrl` query parameter (defaulting to `/chat`).

**Parameters:** None.

**Query parameters:**
- `returnUrl` — redirect destination after successful login (e.g., `/settings`)

**Injects:** `ISkillBotApiClient`, `CustomAuthStateProvider`, `NavigationManager`

**Form fields:**

| Field | Type | Validation |
|-------|------|-----------|
| Email | `MudTextField<string>` | Required, email format |
| Password | `MudTextField<string>` (InputType.Password) | Required, min 6 chars |

**Usage example:**

```razor
<!-- Navigating to login with a returnUrl -->
NavigationManager.NavigateTo($"/login?returnUrl=/settings");
```

---

### Register

**File:** `Pages/Register.razor`
**Route:** `@page "/register"`

**Purpose:** New user registration form. Collects username, email, and password (with confirmation). On success logs the user in immediately and navigates to `/chat`.

**Parameters:** None.

**Injects:** `ISkillBotApiClient`, `CustomAuthStateProvider`, `NavigationManager`

**Form fields:**

| Field | Type | Validation |
|-------|------|-----------|
| Username | `MudTextField<string>` | Required, 3–50 chars, alphanumeric |
| Email | `MudTextField<string>` | Required, email format |
| Password | `MudTextField<string>` | Required, min 8 chars, 1 upper, 1 digit |
| Confirm Password | `MudTextField<string>` | Must match Password |

---

### Chat

**File:** `Pages/Chat.razor`
**Route:** `@page "/chat"`
**Auth:** `[Authorize]`

**Purpose:** Main chat interface. Contains a three-panel layout: sidebar with `ConversationList`, message scroll area with `ChatMessage` items, and a bottom input bar. Supports both single-agent and multi-agent modes, toggled by a switch in the toolbar.

**Parameters:** None.

**Injects:** `ISkillBotApiClient`, `IJSRuntime` (for scroll-to-bottom)

**Key state fields:**

| Field | Type | Description |
|-------|------|-------------|
| `_messages` | `List<ChatMessageDto>` | Current conversation messages |
| `_conversations` | `List<ConversationDto>` | All conversations for sidebar |
| `_isLoading` | `bool` | True while waiting for API response |
| `_multiAgentMode` | `bool` | Toggle for multi-agent routing |
| `_currentConversationId` | `string?` | Active conversation ID |
| `_inputText` | `string` | Current value of the input field |

**Key methods:**

| Method | Description |
|--------|-------------|
| `SendMessageAsync()` | Posts message; calls single or multi-agent endpoint based on `_multiAgentMode` |
| `LoadConversationAsync(id)` | Fetches and displays an existing conversation |
| `NewConversationAsync()` | Clears `_messages` and resets `_currentConversationId` |
| `DeleteConversationAsync(id)` | Removes conversation from sidebar and API |
| `ScrollToBottomAsync()` | JS interop call to scroll the message list to the last item |

---

### Profile

**File:** `Pages/Profile.razor`
**Route:** `@page "/profile"`
**Auth:** `[Authorize]`

**Purpose:** Displays the current user's profile (username, email, joined date). Allows changing display name and password. Includes an account deletion section with confirmation dialog.

**Injects:** `ISkillBotApiClient`, `IDialogService`

---

### Settings

**File:** `Pages/Settings.razor`
**Route:** `@page "/settings"`
**Auth:** `[Authorize]`

**Purpose:** Container page for all user-configurable settings. Uses a `MudTabs` layout with one tab per settings component.

**Tab order:**

1. General (`GeneralSettings`)
2. API Keys (`ApiKeysSettings`)
3. Appearance (`AppearanceSettings`)
4. Privacy (`PrivacySettings`)
5. About (`AboutSettings`)

---

### Admin/Dashboard

**File:** `Pages/Admin/Dashboard.razor`
**Route:** `@page "/admin"`
**Auth:** `[Authorize(Roles = "Admin")]`

**Purpose:** Admin overview with stat cards (total users, active conversations, total tokens consumed, cache hit rate) and a recent-activity feed.

**Injects:** `ISkillBotApiClient`

---

### Admin/Users

**File:** `Pages/Admin/Users.razor`
**Route:** `@page "/admin/users"`
**Auth:** `[Authorize(Roles = "Admin")]`

**Purpose:** Paginated `MudDataGrid` of all registered users. Supports inline editing of roles, deactivating accounts, and viewing per-user stats.

---

### Admin/Analytics

**File:** `Pages/Admin/Analytics.razor`
**Route:** `@page "/admin/analytics"`
**Auth:** `[Authorize(Roles = "Admin")]`

**Purpose:** Usage charts (messages per day, token consumption by provider, cache hit rate trend). Data fetched from `/api/admin/analytics`.

---

## Chat Components

### ChatMessage

**File:** `Components/Chat/ChatMessage.razor`

**Purpose:** Renders a single message bubble with role-appropriate styling (user messages right-aligned in primary color; assistant messages left-aligned in surface color). Passes the message content through `MarkdownRenderer` for assistant messages.

**Parameters:**

| Parameter | Type | Required | Description |
|-----------|------|:--------:|-------------|
| `Message` | `ChatMessageDto` | ✅ | The message to display |
| `ShowTimestamp` | `bool` | No (default: `true`) | Whether to show the timestamp below the bubble |

**`ChatMessageDto` shape:**

```csharp
public record ChatMessageDto(
    string Role,          // "user" | "assistant" | "system"
    string Content,
    DateTime Timestamp,
    int? TokenCount
);
```

**Usage example:**

```razor
@foreach (var msg in _messages)
{
    <ChatMessage Message="msg" ShowTimestamp="true" />
}
```

---

### ConversationList

**File:** `Components/Chat/ConversationList.razor`

**Purpose:** Displays the list of past conversations in the chat sidebar. Each item shows the conversation title (derived from the first message) and date. Clicking an item fires `OnConversationSelected`.

**Parameters:**

| Parameter | Type | Required | Description |
|-----------|------|:--------:|-------------|
| `Conversations` | `IEnumerable<ConversationDto>` | ✅ | Conversations to display |
| `ActiveConversationId` | `string?` | No | Highlights the currently active item |
| `OnConversationSelected` | `EventCallback<string>` | ✅ | Raised when user clicks a conversation |
| `OnConversationDeleted` | `EventCallback<string>` | No | Raised when user clicks the delete icon |

**Usage example:**

```razor
<ConversationList
    Conversations="_conversations"
    ActiveConversationId="_currentConversationId"
    OnConversationSelected="LoadConversationAsync"
    OnConversationDeleted="DeleteConversationAsync" />
```

---

### TypingIndicator

**File:** `Components/Chat/TypingIndicator.razor`

**Purpose:** Displays an animated three-dot "typing" animation to indicate that the assistant is generating a response. Visibility is controlled by the parent page.

**Parameters:**

| Parameter | Type | Required | Description |
|-----------|------|:--------:|-------------|
| `Visible` | `bool` | ✅ | Whether the indicator is shown |
| `Label` | `string` | No (default: `"SkillBot is thinking…"`) | Screen-reader accessible label |

**Usage example:**

```razor
<TypingIndicator Visible="_isLoading" />
```

---

## Shared Components

### MarkdownRenderer

**File:** `Components/Shared/MarkdownRenderer.razor`

**Purpose:** Renders a markdown string as safe HTML. Uses a lightweight in-browser markdown parser (via JS interop or a .NET WASM library) to convert headings, bold, italic, inline code, fenced code blocks, and lists. Code blocks receive syntax highlighting.

**Parameters:**

| Parameter | Type | Required | Description |
|-----------|------|:--------:|-------------|
| `Content` | `string` | ✅ | Raw markdown string to render |
| `AllowHtml` | `bool` | No (default: `false`) | If `false`, strips raw HTML from input before rendering |

**Security note:** When `AllowHtml` is `false` (default), all raw HTML tags in the input are stripped before rendering, preventing XSS.

**Usage example:**

```razor
<MarkdownRenderer Content="@message.Content" />
```

---

### LoadingIndicator

**File:** `Components/Shared/LoadingIndicator.razor`

**Purpose:** Flexible loading state component. In `FullPage` mode it covers the viewport with a centered `MudProgressCircular`. In `Inline` mode it renders a small spinner inline.

**Parameters:**

| Parameter | Type | Required | Description |
|-----------|------|:--------:|-------------|
| `Visible` | `bool` | ✅ | Controls visibility |
| `Mode` | `LoadingMode` | No (default: `Inline`) | `FullPage` or `Inline` |
| `Message` | `string?` | No | Optional text shown below the spinner |

**Usage example:**

```razor
<!-- Full-page overlay while fetching initial data -->
<LoadingIndicator Visible="_initializing" Mode="LoadingMode.FullPage" Message="Loading conversations…" />

<!-- Inline spinner in a button -->
<MudButton Disabled="_saving">
    <LoadingIndicator Visible="_saving" Mode="LoadingMode.Inline" />
    Save
</MudButton>
```

---

## Settings Components

All settings components follow the same pattern:
1. `OnInitializedAsync` fetches the current setting value(s) from `ISkillBotApiClient`.
2. The user edits values in MudBlazor form controls.
3. Clicking **Save** calls the appropriate update endpoint and shows a snackbar confirmation.

### ApiKeysSettings

**File:** `Components/Settings/ApiKeysSettings.razor`

**Purpose:** Allows the user to enter personal API keys for each supported LLM provider (OpenAI, Anthropic Claude, Google Gemini). Keys are stored server-side on the user record and sent in the `Authorization` header of LLM requests when per-user keys are enabled.

**Fields:**

| Field | Provider | Placeholder |
|-------|----------|-------------|
| OpenAI API Key | OpenAI | `sk-…` |
| Anthropic API Key | Claude | `sk-ant-…` |
| Google API Key | Gemini | `AIza…` |
| Preferred Provider | Dropdown | `openai` / `claude` / `gemini` |

**Parameters:** None (fetches and saves via `ISkillBotApiClient`).

**Usage:**

```razor
<!-- In Settings.razor -->
<MudTabPanel Text="API Keys">
    <ApiKeysSettings />
</MudTabPanel>
```

---

### GeneralSettings

**File:** `Components/Settings/GeneralSettings.razor`

**Purpose:** Configures model name (e.g., `gpt-4`, `claude-3-opus`), response language, and the system prompt / persona string.

**Fields:** Model selector (dropdown), Language (dropdown), System Prompt (multiline text area).

---

### AppearanceSettings

**File:** `Components/Settings/AppearanceSettings.razor`

**Purpose:** Toggles dark/light mode and adjusts font size. Changes are applied immediately via `ThemeService` and persisted to `localStorage`.

**Injects:** `ThemeService`

**Fields:** Dark Mode toggle (`MudSwitch`), Font Size slider (`MudSlider<int>`, 12–20 px).

---

### PrivacySettings

**File:** `Components/Settings/PrivacySettings.razor`

**Purpose:** Controls data retention preferences: whether to save conversation history and how long to keep it. Includes a **Clear All History** button that opens a confirmation dialog before calling `DELETE /api/conversations`.

**Fields:** Save History toggle, Retention period (dropdown: 7 days / 30 days / 1 year / Forever), Clear History button.

---

### AboutSettings

**File:** `Components/Settings/AboutSettings.razor`

**Purpose:** Read-only panel showing version information (API version, web client version, .NET runtime), links to GitHub, documentation, and the MIT license.

**Parameters:** None. Fetches API version from `GET /api/health/version`.

---

*See also: [ARCHITECTURE.md](ARCHITECTURE.md) · [STATE_MANAGEMENT.md](STATE_MANAGEMENT.md)*
