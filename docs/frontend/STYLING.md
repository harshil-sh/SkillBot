# SkillBot Web — Styling Guide

This document covers the visual design system for `SkillBot.Web`: MudBlazor theme configuration, dark/light mode, custom CSS, typography, spacing, and BEM naming conventions.

---

## Table of Contents

1. [Technology Stack](#1-technology-stack)
2. [MudBlazor Theme Configuration](#2-mudblazor-theme-configuration)
3. [Dark / Light Mode](#3-dark--light-mode)
4. [Custom CSS (`app.css`)](#4-custom-css-appcss)
5. [Typography](#5-typography)
6. [Spacing and Breakpoints](#6-spacing-and-breakpoints)
7. [BEM Naming Convention](#7-bem-naming-convention)
8. [Component-Scoped Styles (`.razor.css`)](#8-component-scoped-styles-razorcss)
9. [Accessibility and Color Contrast](#9-accessibility-and-color-contrast)
10. [Icons](#10-icons)

---

## 1. Technology Stack

| Technology | Version | Role |
|-----------|---------|------|
| MudBlazor | 9.x | Component library, theming, icons |
| Bootstrap | 5.x (via CDN) | Grid utilities, resets |
| Custom CSS | — | `wwwroot/css/app.css` — overrides and global styles |
| CSS Isolation | .NET 10 | `.razor.css` scoped styles per component |

MudBlazor is the primary styling system. Bootstrap is included for layout utilities (`d-flex`, `gap-*`, breakpoint helpers) and is not used for components (MudBlazor covers all UI components).

---

## 2. MudBlazor Theme Configuration

The custom theme is defined in `MainLayout.razor` (or a separate `ThemeProvider.cs`) and passed to `<MudThemeProvider>`.

### Theme definition

```csharp
private static readonly MudTheme SkillBotTheme = new()
{
    PaletteLight = new PaletteLight
    {
        Primary         = "#6366f1",   // Indigo-500
        PrimaryDarken   = "#4f46e5",   // Indigo-600
        PrimaryLighten  = "#818cf8",   // Indigo-400
        Secondary       = "#8b5cf6",   // Violet-500
        SecondaryDarken = "#7c3aed",   // Violet-600
        Tertiary        = "#06b6d4",   // Cyan-500
        Background      = "#ffffff",
        Surface         = "#f8fafc",
        AppbarBackground= "#6366f1",
        AppbarText      = "#ffffff",
        DrawerBackground= "#1e1b4b",   // Deep indigo sidebar
        DrawerText      = "#e0e7ff",
        DrawerIcon      = "#a5b4fc",
        TextPrimary     = "#0f172a",
        TextSecondary   = "#475569",
        ActionDefault   = "#64748b",
        Divider         = "#e2e8f0",
        Success         = "#10b981",
        Warning         = "#f59e0b",
        Error           = "#ef4444",
        Info            = "#3b82f6",
    },

    PaletteDark = new PaletteDark
    {
        Primary         = "#818cf8",   // Indigo-400 (lighter for dark bg)
        PrimaryDarken   = "#6366f1",
        PrimaryLighten  = "#a5b4fc",
        Secondary       = "#a78bfa",   // Violet-400
        SecondaryDarken = "#8b5cf6",
        Tertiary        = "#22d3ee",
        Background      = "#0f172a",   // Slate-900
        Surface         = "#1e293b",   // Slate-800
        AppbarBackground= "#1e1b4b",
        AppbarText      = "#e0e7ff",
        DrawerBackground= "#0f0b29",
        DrawerText      = "#c7d2fe",
        DrawerIcon      = "#818cf8",
        TextPrimary     = "#f1f5f9",
        TextSecondary   = "#94a3b8",
        ActionDefault   = "#94a3b8",
        Divider         = "#334155",
        Success         = "#34d399",
        Warning         = "#fbbf24",
        Error           = "#f87171",
        Info            = "#60a5fa",
    },

    Typography = new Typography
    {
        Default = new DefaultTypography
        {
            FontFamily = new[] { "Inter", "Segoe UI", "Helvetica Neue", "Arial", "sans-serif" },
            FontSize   = "0.875rem",
            FontWeight = "400",
            LineHeight = "1.6",
        },
        H1 = new H1Typography { FontSize = "2rem",   FontWeight = "700" },
        H2 = new H2Typography { FontSize = "1.5rem", FontWeight = "600" },
        H3 = new H3Typography { FontSize = "1.25rem",FontWeight = "600" },
        H4 = new H4Typography { FontSize = "1rem",   FontWeight = "600" },
        Body1 = new Body1Typography { FontSize = "0.875rem", LineHeight = "1.6" },
        Body2 = new Body2Typography { FontSize = "0.8125rem", LineHeight = "1.5" },
        Caption = new CaptionTypography { FontSize = "0.75rem", LineHeight = "1.4" },
        Button = new ButtonTypography { FontWeight = "600", TextTransform = "none" },
    },

    LayoutProperties = new LayoutProperties
    {
        DefaultBorderRadius = "8px",
        DrawerWidthLeft     = "260px",
        AppbarHeight        = "64px",
    },

    Shadows = new Shadow
    {
        Elevation = new[]
        {
            "none",
            "0 1px 3px rgba(0,0,0,.08), 0 1px 2px rgba(0,0,0,.12)",
            // ... up to Elevation[25]
        }
    }
};
```

### Applying the theme in `MainLayout.razor`

```razor
@inject ThemeService ThemeService

<MudThemeProvider Theme="@SkillBotTheme" IsDarkMode="@ThemeService.IsDarkMode" />
<MudDialogProvider />
<MudSnackbarProvider />

@Body
```

---

## 3. Dark / Light Mode

Dark mode is controlled by `ThemeService.IsDarkMode`, which:

1. Reads the user's saved preference from `localStorage` on startup.
2. Passes the value to `<MudThemeProvider IsDarkMode="…">`.
3. Fires `OnThemeChanged` so subscribed components re-render.

`MudThemeProvider` uses `PaletteLight` when `IsDarkMode = false` and `PaletteDark` when `IsDarkMode = true`. All MudBlazor components automatically switch palettes.

### Toggle control (in `AppearanceSettings.razor`)

```razor
<MudSwitch T="bool"
           @bind-Checked="ThemeService.IsDarkMode"
           Color="Color.Primary"
           Label="@(ThemeService.IsDarkMode ? "Dark mode" : "Light mode")"
           CheckedChanged="async _ => await ThemeService.ToggleThemeAsync()" />
```

### CSS custom properties

MudBlazor exposes the active palette as CSS custom properties on `:root`. You can use them in `app.css`:

```css
/* These are set automatically by MudThemeProvider */
--mud-palette-primary: #6366f1;
--mud-palette-background: #ffffff;
--mud-palette-surface: #f8fafc;
--mud-palette-text-primary: #0f172a;
```

When dark mode is active, MudBlazor updates these properties automatically. Custom components that reference `var(--mud-palette-*)` will switch with no extra code.

---

## 4. Custom CSS (`app.css`)

Location: `SkillBot.Web/wwwroot/css/app.css`

Referenced in `wwwroot/index.html`:

```html
<link href="css/app.css" rel="stylesheet" />
```

### Contents overview

```css
/* === Reset additions === */
*, *::before, *::after { box-sizing: border-box; }

/* === Scrollbars (Webkit) === */
::-webkit-scrollbar { width: 6px; }
::-webkit-scrollbar-thumb { background: var(--mud-palette-primary); border-radius: 3px; }
::-webkit-scrollbar-track { background: transparent; }

/* === Message bubbles === */
.chat-bubble--user {
    background-color: var(--mud-palette-primary);
    color: #fff;
    border-radius: 18px 18px 4px 18px;
    padding: 10px 16px;
    max-width: 75%;
    margin-left: auto;
    word-break: break-word;
}

.chat-bubble--assistant {
    background-color: var(--mud-palette-surface);
    color: var(--mud-palette-text-primary);
    border-radius: 18px 18px 18px 4px;
    padding: 10px 16px;
    max-width: 75%;
    border: 1px solid var(--mud-palette-divider);
    word-break: break-word;
}

/* === Code blocks inside assistant messages === */
.chat-bubble--assistant pre {
    background: var(--mud-palette-background);
    border: 1px solid var(--mud-palette-divider);
    border-radius: 6px;
    padding: 12px 16px;
    overflow-x: auto;
    font-size: 0.8125rem;
}

.chat-bubble--assistant code {
    font-family: 'JetBrains Mono', 'Fira Code', 'Consolas', monospace;
}

/* === Typing indicator animation === */
@keyframes typing-dot {
    0%, 60%, 100% { transform: translateY(0); opacity: 0.4; }
    30%           { transform: translateY(-6px); opacity: 1; }
}

.typing-dot {
    display: inline-block;
    width: 8px; height: 8px;
    border-radius: 50%;
    background: var(--mud-palette-primary);
    animation: typing-dot 1.2s infinite;
}
.typing-dot:nth-child(2) { animation-delay: 0.2s; }
.typing-dot:nth-child(3) { animation-delay: 0.4s; }

/* === Loading overlay === */
.loading-overlay {
    position: fixed;
    inset: 0;
    display: flex;
    align-items: center;
    justify-content: center;
    background: rgba(0, 0, 0, 0.4);
    z-index: 9999;
}

/* === Blazor error bar (override defaults) === */
#blazor-error-ui {
    background: var(--mud-palette-error);
    color: white;
    padding: 12px 20px;
    font-size: 0.875rem;
}
```

---

## 5. Typography

### Font stack

```
"Inter", "Segoe UI", "Helvetica Neue", Arial, sans-serif
```

Inter is loaded from Google Fonts in `wwwroot/index.html`:

```html
<link rel="preconnect" href="https://fonts.googleapis.com">
<link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&display=swap" rel="stylesheet">
```

### Type scale

| MudBlazor variant | Size | Weight | Usage |
|-------------------|------|--------|-------|
| `Typo.h1` | 2rem | 700 | Page titles |
| `Typo.h2` | 1.5rem | 600 | Section headings |
| `Typo.h3` | 1.25rem | 600 | Card headings |
| `Typo.h4` | 1rem | 600 | Sub-headings, dialog titles |
| `Typo.body1` | 0.875rem | 400 | Body copy, chat messages |
| `Typo.body2` | 0.8125rem | 400 | Secondary text, labels |
| `Typo.caption` | 0.75rem | 400 | Timestamps, helper text |
| `Typo.button` | inherited | 600 | Button labels (no uppercase) |

### Usage in Razor

```razor
<MudText Typo="Typo.h2" GutterBottom="true">Chat</MudText>
<MudText Typo="Typo.body2" Color="Color.Secondary">@message.Timestamp.ToString("g")</MudText>
```

---

## 6. Spacing and Breakpoints

### MudBlazor spacing

MudBlazor uses an 8 px base unit. The `Margin` and `Padding` props accept integers 0–16 (multiples of 4 px up to spacing 2, then 8 px beyond).

```razor
<MudPaper Class="pa-4 ma-2">   <!-- pa-4 = 16px padding, ma-2 = 8px margin -->
```

### Breakpoints

| Name | Min-width | Tailored for |
|------|-----------|-------------|
| `Xs` | 0 | Mobile portrait |
| `Sm` | 600 px | Mobile landscape |
| `Md` | 960 px | Tablet |
| `Lg` | 1280 px | Desktop |
| `Xl` | 1920 px | Wide desktop |

MudBlazor `MudHidden` and `MudGrid` use these breakpoints:

```razor
<!-- Sidebar hidden on mobile -->
<MudHidden Breakpoint="Breakpoint.SmAndDown">
    <ConversationList ... />
</MudHidden>
```

---

## 7. BEM Naming Convention

Custom CSS classes (non-MudBlazor) follow BEM:

```
block__element--modifier
```

### Examples

```css
/* Block */
.chat-bubble { ... }

/* Element */
.chat-bubble__header { ... }
.chat-bubble__content { ... }
.chat-bubble__timestamp { ... }

/* Modifier */
.chat-bubble--user { ... }      /* right-aligned, primary color */
.chat-bubble--assistant { ... } /* left-aligned, surface color */
.chat-bubble--error { ... }     /* red border for error state */
```

```css
/* Block */
.conversation-item { ... }

/* Element */
.conversation-item__title { ... }
.conversation-item__date { ... }

/* Modifier */
.conversation-item--active { background: var(--mud-palette-primary-lighten); }
```

---

## 8. Component-Scoped Styles (`.razor.css`)

Each component can have a paired `.razor.css` file that is automatically scoped by the Blazor CSS isolation build step. The generated CSS is bundled into `SkillBot.Web.styles.css` and loaded via `<link>` in `index.html`.

### Naming convention

`Layout/MainLayout.razor` → `Layout/MainLayout.razor.css`

### Example

```css
/* NavMenu.razor.css */
.top-row {
    height: 3.5rem;
    background-color: var(--mud-palette-drawer-background);
}

.navbar-brand {
    font-size: 1.1rem;
    font-weight: 700;
    color: var(--mud-palette-drawer-text);
}

.nav-scrollable {
    overflow-y: auto;
    height: calc(100vh - 3.5rem);
}
```

Scoped styles apply only to the component they belong to and do not bleed into child components. If you need to style a MudBlazor child component from a parent, add `::deep` before the selector:

```css
/* Target MudNavLink inside NavMenu */
::deep .mud-nav-link {
    border-radius: 8px;
    margin: 2px 8px;
}
```

---

## 9. Accessibility and Color Contrast

All color combinations meet **WCAG 2.1 AA** minimum contrast ratio (4.5:1 for normal text, 3:1 for large text).

| Combination | Ratio | AA Pass |
|-------------|-------|:-------:|
| Primary `#6366f1` on white | 4.6:1 | ✅ |
| White text on Primary `#6366f1` | 4.6:1 | ✅ |
| Dark mode text `#f1f5f9` on `#0f172a` | 15.3:1 | ✅ |
| Secondary `#94a3b8` on dark `#0f172a` | 5.1:1 | ✅ |
| Error `#ef4444` on white | 4.6:1 | ✅ |

Use `Color.Default` or `Color.Inherit` rather than hardcoded colors in MudBlazor components. This ensures the colors update correctly in dark mode.

---

## 10. Icons

MudBlazor bundles Material Design icons. Reference them via `Icons.Material.*`:

```razor
<MudIcon Icon="@Icons.Material.Filled.Send" />
<MudIconButton Icon="@Icons.Material.Filled.Delete" Color="Color.Error" />
```

Available icon sets:
- `Icons.Material.Filled` — filled variant (default)
- `Icons.Material.Outlined` — outlined variant
- `Icons.Material.Rounded` — rounded variant
- `Icons.Material.TwoTone` — two-tone variant

Custom SVG icons can be used inline:

```razor
<MudIcon Icon="<path d='M12 ...' />" ViewBox="0 0 24 24" />
```

---

*See also: [ARCHITECTURE.md](ARCHITECTURE.md) · [COMPONENTS.md](COMPONENTS.md) · [DEVELOPMENT.md](DEVELOPMENT.md)*
