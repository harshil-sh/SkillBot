# SkillBot Web UI — User Guide

Welcome to SkillBot! This guide walks you through every feature of the web interface, from creating your account to using advanced multi-agent mode.

---

## Table of Contents

1. [Getting Started](#1-getting-started)
2. [Using the Chat](#2-using-the-chat)
3. [Multi-Agent Mode](#3-multi-agent-mode)
4. [Conversation Management](#4-conversation-management)
5. [Search](#5-search)
6. [Settings](#6-settings)
7. [Account Management](#7-account-management)
8. [Keyboard Shortcuts](#8-keyboard-shortcuts)

---

## 1. Getting Started

### 1.1 Register an Account

1. Open your browser and go to `http://localhost:5000` (or your deployment URL).
2. Click **Register** in the navigation bar (or on the home page).
3. Fill in:
   - **Username** — 3–50 characters, letters and numbers only
   - **Email** — a valid email address
   - **Password** — at least 8 characters, one uppercase letter, one digit
   - **Confirm Password** — must match Password
4. Click **Create Account**.
5. You are automatically logged in and redirected to the Chat page.

### 1.2 Log In

1. Click **Login** in the navigation bar.
2. Enter your **Email** and **Password**.
3. Click **Sign In**.
4. You are redirected to the Chat page (or to the page you were trying to access).

**Forgot password?** Contact your SkillBot administrator to reset your password. Self-service password reset requires an email provider to be configured.

### 1.3 Your First Conversation

1. After logging in you land on the **Chat** page.
2. Click the text box at the bottom of the page (labelled "Message SkillBot…").
3. Type a question, for example: `What is the capital of France?`
4. Press **Enter** or click the **Send** button (▶).
5. SkillBot replies in the message area. The first reply may take 2–5 seconds while the model warms up.

> **Tip:** If SkillBot responds with an error about an API key, visit **Settings → API Keys** and enter your OpenAI (or Claude/Gemini) API key.

---

## 2. Using the Chat

### 2.1 Sending Messages

- Type your message in the input box at the bottom and press **Enter** to send.
- Press **Shift + Enter** to add a line break without sending.
- The **Send** button is also clickable.

### 2.2 Message Formatting

SkillBot's replies are rendered as Markdown:

| Markdown syntax | Rendered as |
|-----------------|-------------|
| `**bold**` | **bold** |
| `*italic*` | *italic* |
| `` `inline code` `` | `inline code` |
| ` ```python ... ``` ` | Syntax-highlighted code block |
| `# Heading` | Section heading |
| `- item` | Bullet list |

You can copy any assistant message by clicking the **Copy** icon that appears on hover.

### 2.3 Token Usage

Each message shows token usage (input + output tokens) in small text below the assistant's reply. This helps you track consumption if you are using personal API keys with a provider that bills per token.

### 2.4 Stopping a Response

If SkillBot is taking too long or producing an unwanted response, click the **Stop** button (■) that appears in the toolbar while a response is being generated.

---

## 3. Multi-Agent Mode

Multi-agent mode routes your task to a team of specialised AI agents: **Research**, **Coding**, **Data Analysis**, and **Writing**. The router automatically decides whether to run agents in parallel or sequentially based on your task.

### 3.1 Enabling Multi-Agent Mode

1. In the Chat toolbar, find the **Multi-Agent** toggle switch.
2. Click it to turn it on (it turns blue).
3. The input placeholder changes to "Describe your task…" as a reminder.

### 3.2 Sending a Multi-Agent Task

Multi-agent mode works best for complex, compound requests:

- *"Research the latest news about quantum computing and write a professional summary."*
- *"Analyse this CSV data and produce a bar chart description: [data]"*
- *"Write a Python function to parse JSON and include unit tests."*

Send the task the same way as a regular message (Enter or Send button).

### 3.3 Understanding Multi-Agent Responses

The response includes a header indicating which routing strategy was used:

| Strategy | Description |
|----------|-------------|
| `single` | Task handled by the single most appropriate agent |
| `parallel` | Multiple agents worked simultaneously; results merged |
| `sequential` | Agents worked in a pipeline (output of one fed into next) |

Each agent's contribution is labelled in the response (e.g., **[Research Agent]**, **[Writing Agent]**).

### 3.4 Switching Back to Single-Agent Mode

Toggle the **Multi-Agent** switch off to return to the default single-agent chat.

---

## 4. Conversation Management

### 4.1 Conversation Sidebar

The left sidebar lists all your saved conversations, newest first. Each item shows:
- The first message (truncated to 60 characters) as the title
- The date of the last message

Click any conversation to load it in the main area.

### 4.2 Starting a New Conversation

Click the **New Chat** button (✏️) at the top of the sidebar. This clears the current conversation and starts a fresh one. Your previous conversation remains saved and accessible in the sidebar.

### 4.3 Searching Conversations

Type in the search box at the top of the sidebar to filter conversations by title. The filter is case-insensitive and matches partial words.

### 4.4 Deleting a Conversation

Hover over a conversation in the sidebar to reveal the **Delete** icon (🗑). Click it, then confirm in the dialog. Deleted conversations cannot be recovered.

### 4.5 Conversation History Settings

By default, SkillBot saves all conversations. To control this, go to **Settings → Privacy**:
- Toggle "Save Conversation History" off to stop saving new conversations.
- Set a retention period (7 days, 30 days, 1 year, or Forever).
- Click "Clear All History" to delete all existing conversations.

---

## 5. Search

SkillBot can search the web using the SerpAPI integration (requires the administrator to have configured a SerpAPI key).

### 5.1 Using Search in Chat

Type your question naturally. If SkillBot determines a web search would help, it performs one automatically via the Search plugin.

### 5.2 Explicit Search Command

You can force a web search by prefixing your message with `/search`:

```
/search latest .NET 10 release notes
```

The response includes source URLs so you can verify the information.

---

## 6. Settings

Access settings by clicking **Settings** in the navigation bar (or pressing `Alt+S`).

### 6.1 General Settings

| Setting | Description |
|---------|-------------|
| Preferred Model | The AI model to use (e.g., `gpt-4`, `claude-3-opus`, `gemini-1.5-pro`) |
| Response Language | Language for assistant replies (default: English) |
| System Prompt | A custom persona or standing instruction for the assistant |

**Example system prompt:** *"You are a helpful senior software engineer. Always respond with concise, idiomatic code examples."*

Click **Save** to apply changes.

### 6.2 API Keys

SkillBot can use your personal API keys from each provider, so your usage is billed to your own accounts.

**Setting up an OpenAI key:**
1. Go to [platform.openai.com/api-keys](https://platform.openai.com/api-keys) and create a key.
2. Paste the key (starts with `sk-`) into the **OpenAI API Key** field.
3. Select **OpenAI** as your preferred provider.
4. Click **Save**.

**Setting up a Claude key:**
1. Go to [console.anthropic.com](https://console.anthropic.com) → API Keys.
2. Paste the key (starts with `sk-ant-`) into the **Anthropic API Key** field.
3. Select **Claude** as your preferred provider.
4. Click **Save**.

**Setting up a Gemini key:**
1. Go to [aistudio.google.com/app/apikey](https://aistudio.google.com/app/apikey).
2. Paste the key (starts with `AIzaSy`) into the **Google API Key** field.
3. Select **Gemini** as your preferred provider.
4. Click **Save**.

> **Security:** API keys are stored encrypted on the server and are never sent back to the browser in plain text (the UI shows only the last 4 characters).

### 6.3 Appearance

| Setting | Options |
|---------|---------|
| Theme | Dark / Light (saved in browser localStorage) |
| Font Size | 12–20 px slider |

Changes apply immediately. The theme preference persists across browser sessions.

### 6.4 Privacy

| Setting | Description |
|---------|-------------|
| Save Conversation History | Toggle on/off |
| History Retention | 7 days / 30 days / 1 year / Forever |
| Clear All History | Deletes all conversations permanently |

### 6.5 About

Shows the current API version, web client version, and .NET runtime version. Includes links to the GitHub repository, documentation, and the MIT license.

---

## 7. Account Management

Access your account by clicking **Profile** in the navigation bar.

### 7.1 View Profile

Your profile page shows:
- Username and email
- Account creation date
- Total messages sent
- Total tokens consumed

### 7.2 Change Display Name

1. Click **Edit** next to your username.
2. Enter a new username (3–50 characters, alphanumeric).
3. Click **Save**.

### 7.3 Change Password

1. On the Profile page, scroll to the **Security** section.
2. Click **Change Password**.
3. Enter your **Current Password**.
4. Enter and confirm your **New Password** (min 8 characters, 1 uppercase, 1 digit).
5. Click **Update Password**.

You will be asked to log in again with the new password.

### 7.4 Delete Account

> ⚠️ **This action is permanent and cannot be undone.** All your conversations, settings, and API keys will be deleted.

1. On the Profile page, scroll to the **Danger Zone** section.
2. Click **Delete Account**.
3. In the confirmation dialog, type your username to confirm.
4. Click **Permanently Delete**.

You are immediately logged out and your data is deleted.

---

## 8. Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| `Enter` | Send message |
| `Shift+Enter` | New line in message input |
| `Alt+N` | New conversation |
| `Alt+S` | Open Settings |
| `Alt+P` | Open Profile |
| `Escape` | Close dialog / modal |
| `Alt+D` | Toggle dark/light mode |
| `Alt+M` | Toggle multi-agent mode |

---

*See also: [FAQ_WEB.md](FAQ_WEB.md) · [TROUBLESHOOTING_WEB.md](TROUBLESHOOTING_WEB.md) · [ADMIN_GUIDE.md](ADMIN_GUIDE.md)*
