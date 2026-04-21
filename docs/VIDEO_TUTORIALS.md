# SkillBot Web UI — Video Tutorial Scripts

This document contains scripts and shot-by-shot outlines for the planned video tutorial series. These outlines are used by content creators to produce screencasts. Each video targets a specific user audience and has a defined runtime.

---

## Tutorial 1: Quick Start (3 minutes)

**Title:** "SkillBot in 3 Minutes — Register, Chat, and Set Your Theme"  
**Target audience:** New users who just deployed SkillBot  
**Goal:** Get from zero to first AI reply in under 3 minutes

---

### Script

**[0:00 – 0:20] Introduction**

> "Hi! In this video I'll show you how to get started with SkillBot's web interface in under 3 minutes. By the end you'll have an account, sent your first message, and personalised your experience."

*[Screen: SkillBot home page at `http://localhost:5000`]*

---

**[0:20 – 0:55] Registration**

> "First, let's create an account. Click the Register button in the top navigation."

*[Click: Register button]*

> "Fill in a username — I'll use 'demo-user'. Enter your email address. Choose a strong password with at least 8 characters, one uppercase letter, and one number. Confirm the password and click Create Account."

*[Type: username, email, password × 2]*  
*[Click: Create Account button]*

> "You're now automatically logged in and taken to the Chat page."

*[Screen: Chat page with empty conversation]*

---

**[0:55 – 1:40] First Conversation**

> "Let's send our first message. Click in the message box at the bottom and type a question. I'll ask: 'Explain what Blazor WebAssembly is in two sentences.'"

*[Type: "Explain what Blazor WebAssembly is in two sentences."]*  
*[Press: Enter]*

> "SkillBot is thinking — you can see the animated dots while it generates a reply."

*[Screen: TypingIndicator visible]*

> "And here's our reply — rendered as clean text. You can see the token count at the bottom of the reply: 12 input tokens and 47 output tokens."

*[Screen: Reply visible with token count]*

---

**[1:40 – 2:10] API Key Setup**

> "By default, SkillBot uses the system API key configured by your administrator. You can also use your own personal OpenAI key. Let me show you how."

*[Click: Settings in nav]*  
*[Click: API Keys tab]*

> "Paste your OpenAI key here, select OpenAI as your preferred provider, and click Save. SkillBot will now use your personal key for all future requests."

*[Type: masked key placeholder]*  
*[Click: Save button]*  
*[Screen: Snackbar "API keys saved" appears]*

---

**[2:10 – 2:45] Dark Mode**

> "Finally, let's switch to dark mode for a more comfortable look. Click Settings, then the Appearance tab."

*[Click: Settings → Appearance tab]*

> "Toggle the Dark Mode switch. The theme changes immediately and is saved in your browser."

*[Click: Dark Mode toggle]*  
*[Screen: Entire UI switches to dark palette]*

---

**[2:45 – 3:00] Summary**

> "That's it! You've registered, sent your first message, configured your API key, and personalised your theme. Check the links in the description for a full user guide and more tutorials."

*[Screen: Chat page in dark mode with conversation visible]*

---

## Tutorial 2: Multi-Agent Mode (3 minutes)

**Title:** "Using SkillBot's Multi-Agent Mode for Complex Tasks"  
**Target audience:** Users who understand basic chat and want to tackle more complex requests  
**Goal:** Understand what multi-agent mode does, when to use it, and how to read the response

---

### Script

**[0:00 – 0:20] Introduction**

> "SkillBot has a multi-agent mode that routes complex tasks to a team of specialised AI agents. In this video I'll show you how to enable it, what kinds of tasks it excels at, and how to interpret the results."

---

**[0:20 – 0:55] What is Multi-Agent Mode?**

> "In single-agent mode, one AI model handles your entire request. In multi-agent mode, a router decides which specialised agents to use: Research, Coding, Data Analysis, or Writing — and whether to run them in parallel or in sequence."

*[Screen: Diagram showing single vs multi-agent flow (use a simple slide or annotated screenshot)]*

> "Multi-agent mode is best for compound tasks that span multiple domains — for example: 'Research recent AI breakthroughs and write a structured report' combines research and writing."

---

**[0:55 – 1:30] Enabling Multi-Agent Mode**

> "To enable it, find the Multi-Agent toggle in the chat toolbar — it's in the top-right of the input area. Click it to turn it on."

*[Click: Multi-Agent toggle — it turns blue]*

> "Notice the input placeholder changes to 'Describe your task…' — this reminds you that multi-agent mode works best with compound, multi-step requests."

---

**[1:30 – 2:30] Sending a Multi-Agent Task**

> "Let me send a real compound task. I'll type: 'Research the top 3 programming languages for AI development in 2024 and write a 200-word comparison.'"

*[Type: "Research the top 3 programming languages for AI development in 2024 and write a 200-word comparison."]*  
*[Press: Enter]*

> "The response takes a little longer than single-agent mode because multiple agents are working. You can see the typing indicator is still active."

*[Screen: TypingIndicator visible for 5-10 seconds]*

> "Here's the response. At the top you can see the routing strategy used — in this case 'sequential': the Research Agent gathered information, then the Writing Agent synthesised it into the comparison."

*[Screen: Multi-agent response with agent labels visible]*

> "The [Research Agent] section provides the raw findings; the [Writing Agent] section is the polished 200-word comparison."

---

**[2:30 – 2:50] When to Use Each Mode**

> "Use single-agent mode for direct questions and quick tasks. Use multi-agent mode for tasks that combine research + writing, data analysis + explanation, or code generation + documentation."

| Use single-agent | Use multi-agent |
|-----------------|----------------|
| "What is JWT?" | "Research JWT and write a tutorial" |
| "Fix this bug: …" | "Review my code and write unit tests" |
| "Translate this text" | "Analyse this data and create a report" |

---

**[2:50 – 3:00] Summary**

> "Multi-agent mode is a powerful tool for complex, multi-domain tasks. Enable it with the toggle, describe your task clearly, and let SkillBot orchestrate the rest."

---

## Tutorial 3: API Key Setup (2 minutes)

**Title:** "Setting Up Your Personal API Keys in SkillBot"  
**Target audience:** Users who want to use their own LLM provider keys  
**Goal:** Successfully configure and test all three supported providers

---

### Script

**[0:00 – 0:15] Introduction**

> "By default SkillBot uses the API key your administrator configured. In this video I'll show you how to set your own personal keys for OpenAI, Claude, and Gemini."

---

**[0:15 – 0:55] Getting API Keys**

> "First, you need keys from the providers. Here's where to get each one:"

- **OpenAI:** "Go to `platform.openai.com/api-keys`. Click Create new secret key. Copy the key — it starts with `sk-`."
- **Claude:** "Go to `console.anthropic.com`, click API keys, and create a new key. It starts with `sk-ant-`."
- **Gemini:** "Go to `aistudio.google.com/app/apikey`. Click Get API key. It starts with `AIzaSy`."

*[Screen: Show each provider's key page (use placeholder/blurred values)]*

---

**[0:55 – 1:35] Entering Keys in SkillBot**

> "Now go to SkillBot → Settings → API Keys tab."

*[Navigate: Settings → API Keys]*

> "Paste your OpenAI key in the first field, Claude key in the second, and Gemini key in the third. Then select your preferred provider from the dropdown — I'll choose OpenAI. Click Save."

*[Type: keys into each field, select provider, click Save]*  
*[Screen: Snackbar "API keys saved" appears]*

---

**[1:35 – 1:50] Testing the Key**

> "Let's test it. Go to the Chat page and send a message."

*[Navigate: Chat]*  
*[Type: "What model are you using?" → Enter]*

> "The response shows the provider and model. The token count is now tracked against your personal API key."

---

**[1:50 – 2:00] Summary**

> "Your API keys are stored encrypted on the server and never shown in plain text again. You can update or remove them at any time from the API Keys settings tab."

---

## Tutorial 4: Admin Dashboard (4 minutes)

**Title:** "SkillBot Admin Dashboard — Monitor Users and System Health"  
**Target audience:** Administrators managing a SkillBot deployment  
**Goal:** Understand all admin dashboard sections and perform common admin tasks

---

### Script

**[0:00 – 0:20] Introduction**

> "This tutorial is for SkillBot administrators. I'll walk through the admin dashboard, show you how to manage users, and demonstrate how to monitor system health."

*[Screen: Admin Dashboard page — logged in as admin]*

---

**[0:20 – 1:00] Stats Overview**

> "The dashboard shows four key metrics: Total Users, Active Today, Total Messages, and Cache Hit Rate."

*[Point to each stat card]*

> "Total Users is all registered accounts. Active Today counts users who sent at least one message today — useful for capacity planning. Cache Hit Rate shows the percentage of LLM requests served from cache — a high rate saves money and improves response times."

> "Click Refresh to reload the stats. They don't auto-refresh to keep API load low."

*[Click: Refresh button]*

---

**[1:00 – 1:45] Recent Activity Feed**

> "Below the stats is the recent activity feed — the last 20 events: user registrations, logins, admin actions, and system warnings. Use this to detect unusual activity like a sudden spike in login failures."

*[Scroll through activity feed]*

---

**[1:45 – 2:45] User Management**

> "Click Users in the left navigation to manage accounts."

*[Click: Admin → Users]*

> "The user list shows every registered account. I can sort by any column. Let me search for a specific user."

*[Type: username in search box]*

> "Here's the user. I can see their message count and last active date. Let me change their role to Admin."

*[Click: Edit icon for a user]*

> "Select Admin from the role dropdown and click Save. That user now has admin access. They'll need to log out and back in for the change to take effect."

*[Change role, click Save]*

> "To deactivate a user — for example, an employee who has left — click the Deactivate button. This prevents them logging in immediately."

*[Click: Deactivate button, confirm dialog]*

---

**[2:45 – 3:30] Analytics**

> "Click Analytics to see usage charts."

*[Click: Admin → Analytics]*

> "The Messages Per Day chart shows traffic patterns. This spike on Friday means users were active — useful for planning maintenance windows. The Token by Provider chart shows 80% of our usage is OpenAI, 20% Claude. The Cache Hit Rate trend is healthy at around 25%."

*[Point to each chart]*

> "Change the date range to zoom in on a specific period. Export CSV downloads the raw data."

*[Change date range picker]*

---

**[3:30 – 4:00] Health Check and Summary**

> "Click Check API Health to open the health endpoint. Everything shows Healthy: database, cache, and Hangfire background jobs are all running."

*[Click: Check API Health button — opens `/health` in new tab]*

> "For log files, check `SkillBot.Api/logs/` on the server. For background job history, open the Hangfire dashboard at `/hangfire`."

> "That covers the core admin features. Check the Admin Guide in the docs for more detail on configuration, rate limits, and troubleshooting."

---

*See also: [USER_GUIDE_WEB.md](USER_GUIDE_WEB.md) · [ADMIN_GUIDE.md](ADMIN_GUIDE.md)*
