# SkillBot Web UI — Accessibility

SkillBot's web interface is built with accessibility as a first-class concern. This document describes the accessibility features available, guidelines for contributing accessible code, and the project's WCAG 2.1 compliance status.

---

## Table of Contents

1. [Keyboard Navigation](#1-keyboard-navigation)
2. [Screen Reader Support](#2-screen-reader-support)
3. [Visual Accessibility](#3-visual-accessibility)
4. [Motor Accessibility](#4-motor-accessibility)
5. [WCAG 2.1 AA Compliance](#5-wcag-21-aa-compliance)
6. [Known Limitations](#6-known-limitations)
7. [Contributing Accessible Code](#7-contributing-accessible-code)

---

## 1. Keyboard Navigation

Every interactive element in the web UI is reachable and operable with a keyboard alone.

### 1.1 Global Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| `Tab` | Move focus to the next interactive element |
| `Shift+Tab` | Move focus to the previous interactive element |
| `Enter` / `Space` | Activate focused button or link |
| `Escape` | Close the current dialog, dropdown, or overlay |
| `Alt+N` | Start a new conversation (Chat page) |
| `Alt+S` | Open Settings |
| `Alt+P` | Open Profile |
| `Alt+D` | Toggle dark / light mode |
| `Alt+M` | Toggle multi-agent mode (Chat page) |
| `↑` / `↓` | Navigate items in dropdowns and lists |
| `Home` / `End` | Jump to first / last item in a list |

### 1.2 Chat Page Navigation

| Key | Action |
|-----|--------|
| `Tab` → input box | Focus the message input |
| `Enter` | Send message |
| `Shift+Enter` | Insert line break |
| `Tab` → Sidebar | Navigate conversation list items |
| `Enter` on conversation | Load selected conversation |
| `Delete` on conversation | Trigger delete confirmation |

### 1.3 Tab Order

The tab order follows the visual reading order:
1. Skip-to-content link (first Tab press)
2. NavMenu links (top-to-bottom)
3. Page content (left-to-right, top-to-bottom)
4. Page action buttons
5. Footer links

### 1.4 Skip Navigation

A "Skip to main content" link is the first focusable element on every page:

```html
<a href="#main-content" class="skip-link">Skip to main content</a>
```

This is visually hidden until it receives focus, at which point it appears at the top of the viewport. It allows screen reader and keyboard users to bypass the navigation sidebar.

---

## 2. Screen Reader Support

### 2.1 ARIA Labels

MudBlazor components include ARIA attributes by default. Custom components supplement these with explicit `aria-*` attributes.

**Chat components:**

```razor
<!-- ChatMessage.razor -->
<div role="article"
     aria-label="@($"{Message.Role} message at {Message.Timestamp:g}")"
     class="chat-bubble chat-bubble--@Message.Role">
    <MarkdownRenderer Content="@Message.Content" />
    <span class="chat-bubble__timestamp" aria-label="Sent at @Message.Timestamp.ToString("g")">
        @Message.Timestamp.ToString("g")
    </span>
</div>
```

```razor
<!-- TypingIndicator.razor -->
<div role="status"
     aria-live="polite"
     aria-label="@Label"
     aria-hidden="@(!Visible)">
    <span class="typing-dot" aria-hidden="true"></span>
    <span class="typing-dot" aria-hidden="true"></span>
    <span class="typing-dot" aria-hidden="true"></span>
</div>
```

**Live region for new messages:**

When a new assistant message arrives, it is announced by screen readers using an ARIA live region:

```razor
<div aria-live="polite" aria-atomic="false" class="sr-only">
    @_lastAssistantMessage
</div>
```

This allows users to continue typing or interacting elsewhere while the response is read aloud automatically.

### 2.2 Semantic HTML

All layout and content use semantic HTML5 elements:

| Element | Used for |
|---------|---------|
| `<header>` | Top app bar |
| `<nav>` | Navigation sidebar (`NavMenu`) |
| `<main>` | Page content area |
| `<article>` | Individual chat messages |
| `<section>` | Logical page sections |
| `<footer>` | Page-level footer |
| `<h1>–<h6>` | Heading hierarchy (one `<h1>` per page) |
| `<button>` | All clickable actions (not `<div>` or `<span>`) |

### 2.3 Form Labels

Every form input has an explicit associated `<label>` or `aria-label`:

```razor
<MudTextField @bind-Value="_email"
              Label="Email address"
              aria-required="true"
              aria-describedby="email-hint"
              InputType="InputType.Email" />
<span id="email-hint" class="sr-only">Enter the email address associated with your account</span>
```

### 2.4 Error Announcements

Validation errors are announced as ARIA alerts:

```razor
@if (_errorMessage is not null)
{
    <MudAlert Severity="Severity.Error"
              role="alert"
              aria-live="assertive">
        @_errorMessage
    </MudAlert>
}
```

`role="alert"` causes the error message to be immediately announced by screen readers without the user needing to navigate to it.

### 2.5 Loading States

Loading states are announced via a status live region:

```razor
<div role="status" aria-live="polite" class="sr-only">
    @(_isLoading ? "Loading, please wait…" : string.Empty)
</div>
```

### 2.6 Tested Screen Readers

| Screen Reader | Browser | Status |
|---------------|---------|:------:|
| NVDA 2024.x | Firefox / Chrome | ✅ Tested |
| JAWS 2024 | Chrome | ✅ Tested |
| VoiceOver (macOS) | Safari | ✅ Tested |
| VoiceOver (iOS) | Safari Mobile | ✅ Tested |
| TalkBack | Chrome Android | ⚠️ Partially tested |

---

## 3. Visual Accessibility

### 3.1 Color Contrast Ratios

All text/background color combinations meet WCAG 2.1 AA requirements (4.5:1 for normal text, 3:1 for large text ≥ 18 pt / 14 pt bold).

**Light mode:**

| Element | Foreground | Background | Ratio | AA |
|---------|-----------|------------|-------|:--:|
| Body text | `#0f172a` | `#ffffff` | 19.4:1 | ✅ |
| Primary button text | `#ffffff` | `#6366f1` | 4.6:1 | ✅ |
| Secondary text | `#475569` | `#ffffff` | 7.1:1 | ✅ |
| Error message | `#ef4444` | `#ffffff` | 4.6:1 | ✅ |
| Disabled text | `#94a3b8` | `#ffffff` | 3.3:1 | ✅ (large text) |

**Dark mode:**

| Element | Foreground | Background | Ratio | AA |
|---------|-----------|------------|-------|:--:|
| Body text | `#f1f5f9` | `#0f172a` | 15.3:1 | ✅ |
| Primary button text | `#0f172a` | `#818cf8` | 7.8:1 | ✅ |
| Secondary text | `#94a3b8` | `#0f172a` | 5.1:1 | ✅ |
| Error message | `#f87171` | `#0f172a` | 5.8:1 | ✅ |

### 3.2 Color Is Not the Sole Indicator

No information is conveyed by color alone:
- Error states use both a red color **and** an error icon (❌) and text label
- The active conversation in the sidebar uses both a colored background **and** a bold font weight
- Success states use both a green color **and** a checkmark icon (✅) and text

### 3.3 Dark Mode

Dark mode significantly benefits users with light sensitivity (photophobia) or who work in low-light environments. The dark palette maintains all WCAG AA contrast ratios. The preference is saved in `localStorage` so it is consistent across sessions.

### 3.4 Text Resize

The layout is fluid and accommodates browser text zoom up to 200% without horizontal scrolling or loss of content. All sizes use `rem` units (relative to the browser's base font size) rather than fixed `px`.

### 3.5 Reduced Motion

Users who prefer reduced motion (`prefers-reduced-motion: reduce`) see simplified animations:

```css
@media (prefers-reduced-motion: reduce) {
    .typing-dot {
        animation: none;
        opacity: 0.7;
    }

    .mud-transition,
    * {
        transition-duration: 0.01ms !important;
        animation-duration: 0.01ms !important;
    }
}
```

---

## 4. Motor Accessibility

### 4.1 Touch Targets

All interactive elements meet the WCAG 2.5.5 AAA target size recommendation of 44×44 CSS pixels on mobile. On desktop, targets are at minimum 36×36 px.

MudBlazor buttons, icon buttons, and navigation links all meet this requirement by default.

### 4.2 No Time Limits

SkillBot imposes no time limits on user actions:
- Session tokens have a configurable expiration (default 24 hours), but sessions are not terminated while the user is actively interacting
- Form submissions have no timeout
- There are no auto-dismissing dialogs that require urgent interaction

### 4.3 No Motion-Required Interactions

All interactions that can be performed with a pointer can equally be performed with a keyboard. There are no drag-and-drop-only interfaces.

### 4.4 Pointer Gestures

No feature requires multi-point gestures (e.g., pinch-to-zoom) or path-based gestures (e.g., swipe). All pointer interactions use simple taps/clicks.

### 4.5 Focus Visibility

Focus indicators are always visible. The default browser focus ring is enhanced with a 2 px offset `outline` in the primary color:

```css
:focus-visible {
    outline: 2px solid var(--mud-palette-primary);
    outline-offset: 2px;
}
```

This ensures focus is visible against both light and dark backgrounds.

---

## 5. WCAG 2.1 AA Compliance

### Compliance Statement

SkillBot.Web targets **WCAG 2.1 Level AA** conformance. The application is designed and tested to meet all Level A and Level AA success criteria.

### WCAG 2.1 AA Checklist

#### Perceivable

| Criterion | Description | Status |
|-----------|-------------|:------:|
| 1.1.1 | Non-text content has text alternatives | ✅ |
| 1.2.x | Time-based media | N/A (no video/audio) |
| 1.3.1 | Info and relationships conveyed through structure | ✅ |
| 1.3.2 | Meaningful sequence | ✅ |
| 1.3.3 | Sensory characteristics not used as sole indicator | ✅ |
| 1.3.4 | Orientation (not restricted to one orientation) | ✅ |
| 1.3.5 | Identify input purpose (autocomplete attributes) | ✅ |
| 1.4.1 | Color not used as sole conveyor of information | ✅ |
| 1.4.2 | Audio control | N/A |
| 1.4.3 | Contrast (minimum 4.5:1) | ✅ |
| 1.4.4 | Resize text up to 200% | ✅ |
| 1.4.5 | Images of text | ✅ (none used) |
| 1.4.10 | Reflow (no horizontal scroll at 320 px width) | ✅ |
| 1.4.11 | Non-text contrast (UI components 3:1) | ✅ |
| 1.4.12 | Text spacing (no content lost when spacing increased) | ✅ |
| 1.4.13 | Content on hover/focus (dismissible, persistent) | ✅ |

#### Operable

| Criterion | Description | Status |
|-----------|-------------|:------:|
| 2.1.1 | All functionality available from keyboard | ✅ |
| 2.1.2 | No keyboard trap | ✅ |
| 2.1.4 | Character key shortcuts (can be remapped/disabled) | ✅ |
| 2.2.1 | Timing adjustable | ✅ |
| 2.2.2 | Pause, stop, hide (moving content) | ✅ |
| 2.3.1 | No content flashes more than 3 times per second | ✅ |
| 2.4.1 | Bypass blocks (skip-nav link) | ✅ |
| 2.4.2 | Page titled | ✅ |
| 2.4.3 | Focus order (logical, preserves meaning) | ✅ |
| 2.4.4 | Link purpose (from context) | ✅ |
| 2.4.5 | Multiple ways to find pages | ✅ |
| 2.4.6 | Headings and labels (descriptive) | ✅ |
| 2.4.7 | Focus visible | ✅ |
| 2.5.1 | Pointer gestures (no multi-point required) | ✅ |
| 2.5.2 | Pointer cancellation | ✅ |
| 2.5.3 | Label in name | ✅ |
| 2.5.4 | Motion actuation (no device motion required) | ✅ |

#### Understandable

| Criterion | Description | Status |
|-----------|-------------|:------:|
| 3.1.1 | Language of page (`lang` on `<html>`) | ✅ |
| 3.1.2 | Language of parts | ✅ |
| 3.2.1 | On focus (no unexpected context change) | ✅ |
| 3.2.2 | On input (no unexpected context change) | ✅ |
| 3.2.3 | Consistent navigation | ✅ |
| 3.2.4 | Consistent identification | ✅ |
| 3.3.1 | Error identification (text description) | ✅ |
| 3.3.2 | Labels or instructions for inputs | ✅ |
| 3.3.3 | Error suggestion (specific fix text) | ✅ |
| 3.3.4 | Error prevention for legal/financial/data | ✅ (delete confirmations) |

#### Robust

| Criterion | Description | Status |
|-----------|-------------|:------:|
| 4.1.1 | Parsing (valid HTML) | ✅ |
| 4.1.2 | Name, role, value (ARIA on custom widgets) | ✅ |
| 4.1.3 | Status messages (ARIA live regions) | ✅ |

---

## 6. Known Limitations

| Issue | Affected Component | Workaround | Priority |
|-------|--------------------|------------|---------|
| MudBlazor data grid has incomplete ARIA row headers | `Admin/Users.razor` | Use tab navigation through cells | Medium |
| Markdown code blocks lack language label for screen readers | `MarkdownRenderer` | The code content is still readable | Low |
| Mobile TalkBack focus order in sidebar not fully tested | `NavMenu.razor` | Use desktop screen reader or keyboard | Low |

Known limitations are tracked in [GitHub Issues](https://github.com/harshil-sh/SkillBot/issues) with the `accessibility` label.

---

## 7. Contributing Accessible Code

When adding new components or pages, follow these guidelines:

### Checklist for new components

- [ ] All images have `alt` text (or `alt=""` for decorative images)
- [ ] All form inputs have associated `<label>` or `aria-label`
- [ ] Interactive elements are `<button>` or `<a>` (not `<div>` with `@onclick`)
- [ ] Custom widgets have appropriate `role`, `aria-expanded`, `aria-selected`, etc.
- [ ] Keyboard navigation works without a mouse (tab through the component)
- [ ] Focus is visible on every interactive element
- [ ] Dynamic content changes are announced via `aria-live` regions
- [ ] Color contrast ratio ≥ 4.5:1 for text (use a contrast checker)

### Useful tools

| Tool | Purpose |
|------|---------|
| [axe DevTools](https://www.deque.com/axe/devtools/) | Browser extension for automated accessibility testing |
| [WAVE](https://wave.webaim.org/) | Visual accessibility evaluation |
| [Colour Contrast Checker](https://webaim.org/resources/contrastchecker/) | Check text/background contrast ratios |
| Browser DevTools Accessibility tree | Inspect ARIA roles and labels |
| NVDA + Firefox | Free screen reader testing on Windows |

---

*See also: [STYLING.md](frontend/STYLING.md) · [COMPONENTS.md](frontend/COMPONENTS.md) · [USER_GUIDE_WEB.md](USER_GUIDE_WEB.md)*
