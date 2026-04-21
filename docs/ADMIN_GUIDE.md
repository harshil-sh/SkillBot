# SkillBot — Administrator Guide

This guide covers the SkillBot admin dashboard, user management, system monitoring, and configuration options available to administrators.

---

## Table of Contents

1. [Getting Admin Access](#1-getting-admin-access)
2. [Admin Dashboard](#2-admin-dashboard)
3. [User Management](#3-user-management)
4. [System Monitoring](#4-system-monitoring)
5. [Configuration Management](#5-configuration-management)
6. [Troubleshooting Common Admin Issues](#6-troubleshooting-common-admin-issues)

---

## 1. Getting Admin Access

### First Admin User

The first admin account must be created by directly updating the database or using `dotnet user-secrets` during initial setup.

**Option A — Database update:**

```sql
-- Run against skillbot.db (SQLite)
UPDATE Users SET Role = 'Admin' WHERE Email = 'admin@example.com';
```

**Option B — Seed via environment variable:**

```json
// SkillBot.Api/appsettings.json
{
  "AdminSeed": {
    "Email": "admin@yourdomain.com",
    "Username": "admin",
    "Password": "AdminSecret123!"
  }
}
```

When `AdminSeed` is configured and no admin user exists, the API creates the admin account on startup.

### Promoting Existing Users

Any admin can promote other users to admin via the web UI (see §3.3) or via the API:

```bash
curl -k -X PUT https://localhost:7101/api/admin/users/{userId} \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"role": "Admin"}'
```

---

## 2. Admin Dashboard

Access: **Navigation bar → Admin** (only visible to users with the Admin role).

### 2.1 Stats Cards

The dashboard displays four top-level metric cards:

| Card | Metric | Refresh interval |
|------|--------|-----------------|
| Total Users | Count of all registered accounts | On page load |
| Active Today | Users who sent at least one message today | On page load |
| Total Messages | Sum of all messages ever sent | On page load |
| Cache Hit Rate | % of LLM requests served from cache (L1 + L2) | On page load |

Click **Refresh** (↻ icon) to reload all stats.

### 2.2 Recent Activity Feed

Below the stat cards, a feed shows the last 20 system events, newest first:

- User registrations
- Login events
- Admin actions (role changes, account deactivation)
- System errors and warnings

Each entry shows: username, action type, timestamp, and IP address (if logging is enabled).

### 2.3 Quick Actions

The dashboard provides shortcut buttons:
- **View All Users** → `/admin/users`
- **View Analytics** → `/admin/analytics`
- **Check API Health** → opens `/health` in a new tab

---

## 3. User Management

Access: **Admin → Users** (`/admin/users`)

### 3.1 User List

The user list is a sortable, filterable data grid with the following columns:

| Column | Description |
|--------|-------------|
| Username | Clickable link to user detail |
| Email | User's email address |
| Role | `User` or `Admin` |
| Status | Active / Inactive |
| Joined | Registration date |
| Messages | Total messages sent by this user |
| Last Active | Date of last activity |
| Actions | Edit / Deactivate / Delete buttons |

**Sorting:** Click any column header to sort ascending; click again for descending.

**Filtering:** Use the search box above the grid to filter by username or email. The filter applies instantly as you type.

**Pagination:** 20 users per page. Use the pagination controls at the bottom to navigate.

### 3.2 View User Details

Click a username to open the user detail panel:
- Full profile (username, email, role, creation date)
- Message count and token consumption
- Conversation list (view-only)
- API key status (shows which providers have keys configured — not the keys themselves)

### 3.3 Edit User Role

1. In the Actions column, click **Edit** (✏️) for the target user.
2. Change the **Role** dropdown from `User` to `Admin` (or vice versa).
3. Click **Save**.

> The affected user must log out and back in for the role change to take effect (their existing JWT retains the old role until it expires or they re-authenticate).

### 3.4 Deactivate / Reactivate a User

Deactivating a user prevents them from logging in and invalidates all their active sessions.

1. Click **Deactivate** (🚫) in the Actions column.
2. Confirm in the dialog.

To reactivate, find the user (deactivated users have a grey status badge) and click **Activate** (✅).

### 3.5 Delete a User

> ⚠️ **This permanently deletes the user and all their conversations.** This action cannot be undone.

1. Click **Delete** (🗑) in the Actions column.
2. In the confirmation dialog, type the username to confirm.
3. Click **Permanently Delete**.

---

## 4. System Monitoring

Access: **Admin → Analytics** (`/admin/analytics`)

### 4.1 Usage Analytics

The analytics page provides charts for the selected date range (default: last 30 days):

**Messages Per Day** — Bar chart of daily message volume. Use this to identify traffic spikes and plan capacity.

**Token Consumption by Provider** — Pie chart showing what percentage of token usage is on OpenAI vs Claude vs Gemini. Useful for cost attribution.

**Cache Hit Rate Trend** — Line chart of the daily L1+L2 cache hit rate. A rate above 20% indicates effective caching; below 10% may suggest the cache TTL is too short.

**Export:** Click **Export CSV** to download the raw data for the selected period.

### 4.2 Health Checks

SkillBot.Api exposes a `/health` endpoint that returns the status of all subsystems:

```bash
curl -k https://localhost:7101/health
```

Example response:

```json
{
  "status": "Healthy",
  "checks": {
    "database": { "status": "Healthy", "duration": "2ms" },
    "cache": { "status": "Healthy", "duration": "0ms" },
    "hangfire": { "status": "Healthy" }
  }
}
```

The admin dashboard links to this endpoint via the **Check API Health** button.

### 4.3 Logs

SkillBot uses Serilog with rolling file logging. Log files are located at:

```
SkillBot.Api/logs/skillbot-YYYYMMDD.log
```

Log levels (configured in `appsettings.json`):

| Level | Includes |
|-------|---------|
| `Verbose` | All internal detail (development only) |
| `Debug` | Detailed diagnostics |
| `Information` | Normal operation events (default) |
| `Warning` | Non-critical issues, rate limit hits |
| `Error` | Exceptions, failed requests |
| `Fatal` | Startup failures, critical errors |

To change the log level:

```json
// SkillBot.Api/appsettings.json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning"
      }
    }
  }
}
```

### 4.4 Background Jobs (Hangfire)

Hangfire's dashboard is available at `https://localhost:7101/hangfire` (admin authentication required).

It shows:
- **Enqueued** jobs waiting to run
- **Processing** currently executing jobs
- **Succeeded** completed jobs with execution history
- **Failed** jobs with full stack traces and retry controls

Common recurring jobs:
- `CleanExpiredCacheEntries` — runs every hour, purges L2 SQLite cache entries past their TTL
- `PurgeOldConversations` — runs daily, deletes conversations past the user's retention setting

---

## 5. Configuration Management

### 5.1 Feature Flags

Feature flags are configured in `SkillBot.Api/appsettings.json`:

```json
{
  "Features": {
    "Registration": true,        // Allow new user registration
    "GuestChat": false,          // Allow unauthenticated chat (rate-limited)
    "SerpApiSearch": true,       // Enable web search plugin
    "MultiAgent": true,          // Enable multi-agent mode
    "TelegramBot": false         // Enable Telegram channel
  }
}
```

Changes require an API restart.

### 5.2 Rate Limiting

Rate limits are per-user and reset per time window:

```json
{
  "RateLimit": {
    "RequestsPerMinute": 20,
    "RequestsPerHour": 200,
    "RequestsPerDay": 1000
  }
}
```

Admin users are exempt from rate limiting.

### 5.3 System Message (Global Persona)

Set a system-level persona that applies to all users (overridden by per-user system prompts when set):

```json
{
  "SkillBot": {
    "SystemMessage": "You are SkillBot, a helpful AI assistant. Be concise and accurate."
  }
}
```

### 5.4 JWT Settings

```json
{
  "JwtSettings": {
    "Secret": "your-super-secret-key-min-32-chars-long",
    "ExpirationMinutes": 1440,   // 24 hours default
    "Issuer": "SkillBot",
    "Audience": "SkillBotUsers"
  }
}
```

Changing `Secret` invalidates all existing tokens. All users will be logged out.

---

## 6. Troubleshooting Common Admin Issues

### ❌ Admin menu not visible

**Cause:** User's JWT does not contain the `Admin` role claim (role was added after last login).  
**Fix:** Ask the user to log out and log back in.

---

### ❌ Analytics page shows no data

**Cause:** The analytics endpoint requires at least 24 hours of data, or the date range is set to a period before any messages were sent.  
**Fix:** Change the date range to include the current day. Check API logs for analytics query errors.

---

### ❌ Cannot deactivate a user — "Operation failed"

**Cause:** Attempting to deactivate yourself or the last admin account.  
**Fix:** You cannot deactivate your own account or the system's only admin. Promote another user to admin first, then deactivate.

---

### ❌ Hangfire dashboard returns 403

**Cause:** The Hangfire dashboard is restricted to Admin-role users.  
**Fix:** Ensure you are logged in with an Admin account and that the JWT has not expired. If the token is expired, log out and back in.

---

### ❌ High error rate in logs — `429 Too Many Requests` from provider

**Cause:** The shared (system-level) API key has hit the provider's rate limit.  
**Fix:** Encourage users to set their own personal API keys in Settings. Alternatively, increase the provider-level rate limit by upgrading the API key tier.

---

### ❌ Database growing too large

**Cause:** Conversation history and L2 cache are accumulating.  
**Fix:**
1. Reduce `HistoryRetentionDays` in `appsettings.json` and trigger `PurgeOldConversations` immediately via the Hangfire dashboard.
2. Reduce `CacheExpirationHours` to evict L2 cache entries sooner.
3. Run `VACUUM` on the SQLite database to reclaim disk space:
   ```sql
   -- Connect to skillbot.db with any SQLite client
   VACUUM;
   ```

---

*See also: [USER_GUIDE_WEB.md](USER_GUIDE_WEB.md) · [DEPLOYMENT_WEB.md](DEPLOYMENT_WEB.md) · [TROUBLESHOOTING_WEB.md](TROUBLESHOOTING_WEB.md)*
