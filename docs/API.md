# REST API Reference

Base URL (development): `https://localhost:7101`

Interactive documentation (development only): open `https://localhost:7101/` in a browser for the Swagger UI.

All authenticated endpoints require a JWT token in the `Authorization` header:
```
Authorization: Bearer <token>
```

Tokens are obtained from the `POST /api/auth/login` endpoint.

---

## Table of Contents

- [Authentication](#authentication)
  - [POST /api/auth/register](#post-apiauthregister)
  - [POST /api/auth/login](#post-apiauthlogin)
- [Chat](#chat)
  - [POST /api/chat](#post-apichat)
  - [GET /api/chat/history](#get-apichathistory)
  - [GET /api/chat/{conversationId}](#get-apichatconversationid)
  - [DELETE /api/chat/{conversationId}](#delete-apichatconversationid)
- [Multi-Agent](#multi-agent)
  - [POST /api/multi-agent/chat](#post-apimulti-agentchat)
  - [GET /api/multi-agent/agents](#get-apimulti-agentagents)
- [Settings](#settings)
  - [GET /api/settings](#get-apisettings)
  - [PUT /api/settings/api-key](#put-apisettingsapi-key)
  - [PUT /api/settings/provider](#put-apisettingsprovider)
- [Plugins](#plugins)
  - [GET /api/plugins](#get-apiplugins)
  - [GET /api/plugins/{pluginName}](#get-apipluginspluginname)
- [Usage](#usage)
  - [GET /api/usage/stats](#get-apiusagestats)
  - [GET /api/usage/stats/{conversationId}](#get-apiusagestatsconversationid)
  - [GET /api/usage/top-conversations](#get-apiusagetop-conversations)
  - [DELETE /api/usage/stats](#delete-apiusagestats)
- [Tasks](#tasks)
  - [POST /api/tasks/schedule](#post-apitasksschedule)
  - [POST /api/tasks/recurring](#post-apitasksrecurring)
  - [GET /api/tasks](#get-apitasks)
  - [GET /api/tasks/{taskId}](#get-apitaskstaskid)
  - [DELETE /api/tasks/{taskId}](#delete-apitaskstaskid)
- [Cache](#cache)
  - [GET /api/cache/stats](#get-apicachestats)
  - [GET /api/cache/health](#get-apicachehealth)
  - [DELETE /api/cache](#delete-apicache)
  - [DELETE /api/cache/invalidate/{pattern}](#delete-apicacheinvalidatepattern)
- [Webhooks](#webhooks)
  - [POST /api/webhook/telegram](#post-apiwebhooktelegram)
- [Health](#health)
  - [GET /health](#get-health)

---

## Authentication

### POST /api/auth/register

Register a new user account.

**Auth required:** No

**Request body:**
```json
{
  "email": "user@example.com",
  "username": "alice",
  "password": "Secret123!"
}
```

**Response 200:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "userId": "abc123",
  "username": "alice",
  "email": "user@example.com",
  "expiresAt": "2025-01-02T10:00:00Z"
}
```

**Response 400:** Email already in use or validation error.

```bash
curl -k -X POST https://localhost:7101/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{"email":"user@example.com","username":"alice","password":"Secret123!"}'
```

---

### POST /api/auth/login

Authenticate and receive a JWT token.

**Auth required:** No

**Request body:**
```json
{
  "email": "user@example.com",
  "password": "Secret123!"
}
```

**Response 200:** Same shape as `/register`.

**Response 400:** Invalid credentials.

```bash
curl -k -X POST https://localhost:7101/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"user@example.com","password":"Secret123!"}'
```

---

## Chat

### POST /api/chat

Send a message to the single-agent AI assistant.

**Auth required:** ✅ JWT

**Rate limited:** ✅

**Request body:**
```json
{
  "message": "What is the capital of France?",
  "conversationId": "optional-existing-id"
}
```

| Field | Type | Required | Description |
|---|---|---|---|
| `message` | string | ✅ | The user's message. Validated and checked for safe content. |
| `conversationId` | string | No | Resume an existing conversation. Omit to start a new one. |

**Response 200:**
```json
{
  "message": "The capital of France is Paris.",
  "conversationId": "conv-abc123",
  "executionTimeMs": 842.5,
  "tokensUsed": 47,
  "toolCalls": [
    {
      "pluginName": "Calculator",
      "functionName": "add",
      "arguments": {"a": 2, "b": 3},
      "executionTimeMs": 1.2
    }
  ]
}
```

**Response 400:** Empty message, input validation failure, or content safety failure.  
**Response 429:** Rate limit exceeded. Check the `Retry-After` hint in the error body.

```bash
curl -k -X POST https://localhost:7101/api/chat \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"message":"What is 17 * 23?"}'
```

---

### GET /api/chat/history

Retrieve the authenticated user's conversation history.

**Auth required:** ✅ JWT

**Query parameters:**

| Parameter | Default | Description |
|---|---|---|
| `limit` | `50` | Maximum number of records to return. |

**Response 200:** Array of conversation records (message, response, tokens, timestamp).

```bash
curl -k https://localhost:7101/api/chat/history \
  -H "Authorization: Bearer $TOKEN"
```

---

### GET /api/chat/{conversationId}

Retrieve messages for a specific conversation (from the in-memory/cache store, 24-hour TTL).

**Auth required:** ✅ JWT

**Response 200:**
```json
{
  "conversationId": "conv-abc123",
  "messages": [
    {"role": "user", "content": "Hello"},
    {"role": "assistant", "content": "Hi! How can I help you?"}
  ]
}
```

**Response 404:** Conversation not found or expired.

```bash
curl -k https://localhost:7101/api/chat/conv-abc123 \
  -H "Authorization: Bearer $TOKEN"
```

---

### DELETE /api/chat/{conversationId}

Delete a conversation from the session store.

**Auth required:** ✅ JWT

**Response 204:** Deleted.  
**Response 404:** Conversation not found.

```bash
curl -k -X DELETE https://localhost:7101/api/chat/conv-abc123 \
  -H "Authorization: Bearer $TOKEN"
```

---

## Multi-Agent

### POST /api/multi-agent/chat

Execute a task using multi-agent orchestration. The `LlmTaskRouter` selects a strategy (`single`, `parallel`, or `sequential`) and dispatches the task to one or more specialised agents.

**Auth required:** No

**Request body:**
```json
{
  "task": "Research quantum computing trends and write a 3-paragraph summary",
  "conversationId": "optional-existing-id"
}
```

**Response 200:**
```json
{
  "finalResponse": "Quantum computing ...",
  "conversationId": "conv-xyz",
  "strategy": "sequential",
  "totalExecutionTimeMs": 4200.0,
  "agentsUsed": [
    {
      "agentId": "research-agent",
      "agentName": "research-agent",
      "result": "Research findings ...",
      "executionTimeMs": 2100.0,
      "success": true
    },
    {
      "agentId": "writing-agent",
      "agentName": "writing-agent",
      "result": "Polished summary ...",
      "executionTimeMs": 1800.0,
      "success": true
    }
  ]
}
```

```bash
curl -k -X POST https://localhost:7101/api/multi-agent/chat \
  -H "Content-Type: application/json" \
  -d '{"task":"Analyse the pros and cons of Python vs JavaScript for backend development"}'
```

---

### GET /api/multi-agent/agents

List all registered specialised agents.

**Auth required:** No

**Response 200:**
```json
[
  {
    "agentId": "research-agent",
    "name": "Research Specialist",
    "description": "Expert at gathering and synthesising information",
    "specializations": ["research", "information", "facts"],
    "status": "Idle"
  }
]
```

```bash
curl -k https://localhost:7101/api/multi-agent/agents
```

---

## Settings

### GET /api/settings

Get the authenticated user's current settings.

**Auth required:** ✅ JWT

**Response 200:**
```json
{
  "preferredProvider": "openai",
  "hasOpenAiKey": true,
  "hasClaudeKey": false,
  "hasGeminiKey": false,
  "hasSerpApiKey": false
}
```

> Note: API key values are never returned; only whether they are set.

```bash
curl -k https://localhost:7101/api/settings \
  -H "Authorization: Bearer $TOKEN"
```

---

### PUT /api/settings/api-key

Store a provider API key for the authenticated user.

**Auth required:** ✅ JWT

**Request body:**
```json
{
  "provider": "openai",
  "apiKey": "sk-proj-..."
}
```

| `provider` value | Provider |
|---|---|
| `openai` | OpenAI |
| `claude` | Anthropic Claude |
| `gemini` | Google Gemini |
| `serpapi` | SerpAPI (web search) |

**Response 200:** Key updated.  
**Response 400:** Invalid provider or empty key.

```bash
curl -k -X PUT https://localhost:7101/api/settings/api-key \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"provider":"openai","apiKey":"sk-proj-..."}'
```

---

### PUT /api/settings/provider

Switch the LLM provider used for subsequent chat requests.

**Auth required:** ✅ JWT

**Request body:**
```json
{
  "provider": "claude"
}
```

**Response 200:** Provider updated.  
**Response 400:** Unsupported provider.

```bash
curl -k -X PUT https://localhost:7101/api/settings/provider \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"provider":"gemini"}'
```

---

## Plugins

### GET /api/plugins

List all registered plugins and their functions.

**Auth required:** No

**Response 200:**
```json
[
  {
    "name": "Calculator",
    "description": "Performs arithmetic operations",
    "functions": [
      {
        "name": "add",
        "description": "Adds two numbers",
        "parameters": [
          {"name": "a", "type": "Double", "description": "First number", "isRequired": true},
          {"name": "b", "type": "Double", "description": "Second number", "isRequired": true}
        ]
      }
    ]
  }
]
```

```bash
curl -k https://localhost:7101/api/plugins
```

---

### GET /api/plugins/{pluginName}

Get details for a single plugin by name (case-insensitive).

**Auth required:** No

**Response 200:** Single plugin object (same shape as list item above).  
**Response 404:** Plugin not found.

```bash
curl -k https://localhost:7101/api/plugins/Calculator
```

---

## Usage

### GET /api/usage/stats

Get overall token usage statistics.

**Auth required:** No

**Query parameters:**

| Parameter | Description |
|---|---|
| `since` | ISO 8601 date-time. Only include usage after this time. |

**Response 200:**
```json
{
  "totalTokens": 12345,
  "totalRequests": 89,
  "averageTokensPerRequest": 138.7,
  "byModel": {
    "gpt-4": 12345
  }
}
```

```bash
curl -k "https://localhost:7101/api/usage/stats?since=2025-01-01"
```

---

### GET /api/usage/stats/{conversationId}

Usage statistics for a specific conversation.

**Auth required:** No

```bash
curl -k https://localhost:7101/api/usage/stats/conv-abc123
```

---

### GET /api/usage/top-conversations

Top conversations by token usage.

**Auth required:** No

**Query parameters:**

| Parameter | Default | Description |
|---|---|---|
| `limit` | `10` | Number of conversations to return (max 100). |

```bash
curl -k "https://localhost:7101/api/usage/top-conversations?limit=5"
```

---

### DELETE /api/usage/stats

Reset all usage statistics.

**Auth required:** No

**Response 204:** Stats reset.

```bash
curl -k -X DELETE https://localhost:7101/api/usage/stats
```

---

## Tasks

Background tasks are powered by Hangfire. The Hangfire Dashboard is available at `/hangfire`.

### POST /api/tasks/schedule

Schedule a one-time agent task.

**Auth required:** No

**Request body:**
```json
{
  "task": "Generate a weekly performance summary",
  "executeAt": "2025-06-01T09:00:00Z",
  "isMultiAgent": false
}
```

**Response 200:**
```json
{
  "taskId": "task-abc123",
  "message": "Task scheduled to execute at 2025-06-01 09:00:00 UTC"
}
```

```bash
curl -k -X POST https://localhost:7101/api/tasks/schedule \
  -H "Content-Type: application/json" \
  -d '{"task":"Summarise news","executeAt":"2025-06-01T09:00:00Z","isMultiAgent":false}'
```

---

### POST /api/tasks/recurring

Schedule a recurring agent task using a cron expression.

**Auth required:** No

**Request body:**
```json
{
  "task": "Generate daily report",
  "cronExpression": "0 9 * * *",
  "isMultiAgent": true
}
```

Common cron expressions:

| Expression | Meaning |
|---|---|
| `0 9 * * *` | Daily at 09:00 UTC |
| `0 */6 * * *` | Every 6 hours |
| `0 0 * * 0` | Weekly on Sunday midnight |
| `*/5 * * * *` | Every 5 minutes |

**Response 200:**
```json
{
  "taskId": "task-xyz789",
  "message": "Recurring task scheduled with cron expression: 0 9 * * *"
}
```

---

### GET /api/tasks

List all scheduled tasks.

**Auth required:** No

**Response 200:** Array of task info objects.

---

### GET /api/tasks/{taskId}

Get information about a specific task.

**Auth required:** No

**Response 200:** Task info object.  
**Response 404:** Task not found.

---

### DELETE /api/tasks/{taskId}

Cancel a scheduled task.

**Auth required:** No

**Response 204:** Cancelled.  
**Response 404:** Task not found.

---

## Cache

### GET /api/cache/stats

Cache performance statistics for both L1 (in-memory) and L2 (SQLite) tiers.

**Auth required:** No

**Response 200:**
```json
{
  "l1Hits": 420,
  "l1Misses": 80,
  "l2Hits": 55,
  "l2Misses": 25,
  "totalEntries": 312,
  "totalSizeBytes": 5242880,
  "hitRatePercentage": 81.25,
  "totalRequests": 580,
  "totalHits": 475,
  "totalMisses": 105,
  "oldestEntry": "2025-05-01T00:00:00Z",
  "newestEntry": "2025-05-15T12:00:00Z"
}
```

```bash
curl -k https://localhost:7101/api/cache/stats
```

---

### GET /api/cache/health

Cache health status.

**Auth required:** No

**Response 200:**
```json
{
  "isHealthy": true,
  "hitRate": 81.25,
  "totalEntries": 312,
  "message": null
}
```

---

### DELETE /api/cache

Clear all cached entries from both L1 and L2.

**Auth required:** No

**Response 204:** Cache cleared.

```bash
curl -k -X DELETE https://localhost:7101/api/cache
```

---

### DELETE /api/cache/invalidate/{pattern}

Invalidate cache entries matching a pattern (supports `*` wildcard).

**Auth required:** No

**Examples:**

```bash
# Clear all LLM response cache entries
curl -k -X DELETE "https://localhost:7101/api/cache/invalidate/llm_response_*"

# Clear all search cache entries
curl -k -X DELETE "https://localhost:7101/api/cache/invalidate/search:*"
```

**Response 204:** Entries invalidated.  
**Response 400:** Empty pattern.

---

## Webhooks

### POST /api/webhook/telegram

Receives incoming updates from the Telegram Bot API. This endpoint is called by Telegram's servers — do not call it directly.

**Auth required:** No (anonymous, called by Telegram)

**Request body:** Telegram `Update` object (see [Telegram Bot API docs](https://core.telegram.org/bots/api#update)).

**Response 200:** Always returns 200 to acknowledge receipt.

See [TELEGRAM.md](TELEGRAM.md) for setup instructions.

---

## Health

### GET /health

Application health check endpoint.

**Auth required:** No

**Response 200:**
```json
{
  "status": "Healthy",
  "results": {
    "skillbot_health": {
      "status": "Healthy",
      "description": "SkillBot API is healthy"
    }
  }
}
```

```bash
curl -k https://localhost:7101/health
```

---

## Error Responses

All error responses follow this shape:

```json
{
  "error": "ErrorCode",
  "message": "Human-readable description",
  "details": "Optional additional details"
}
```

Common error codes:

| Code | HTTP Status | Meaning |
|---|---|---|
| `InvalidRequest` | 400 | Malformed request body or missing required field |
| `ValidationFailed` | 400 | Input failed validation rules |
| `UnsafeContent` | 400 | Content safety check rejected the input |
| `InvalidProvider` | 400 | Unknown LLM provider name |
| `NotFound` | 404 | Resource does not exist |
| `RateLimitExceeded` | 429 | Too many requests; includes `RetryAfter` info |
| `RequestCancelled` | 499 | Client cancelled the request |
| `InternalError` | 500 | Unhandled server error |
