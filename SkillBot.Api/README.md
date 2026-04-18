# SkillBot REST API Implementation Guide

## 📋 Overview

This guide walks you through setting up the SkillBot REST API with Swagger documentation. The API is designed to be **OAuth-ready** for future authentication implementation.

## 🏗️ What We're Building

```
SkillBot REST API
├── Chat Endpoints (Single-agent)
├── Multi-Agent Endpoints  
├── Plugin Management
├── Conversation Management
├── Health Checks
└── Swagger UI Documentation
```

## 🚀 Setup Instructions

### Step 1: Create the API Project

```bash
cd SkillBot

# Create Web API project
dotnet new webapi -n SkillBot.Api -f net10.0

# Add to solution
dotnet sln add SkillBot.Api/SkillBot.Api.csproj

# Add project references
cd SkillBot.Api
dotnet add reference ../SkillBot.Core/SkillBot.Core.csproj
dotnet add reference ../SkillBot.Infrastructure/SkillBot.Infrastructure.csproj
dotnet add reference ../SkillBot.Plugins/SkillBot.Plugins.csproj
```

### Step 2: Install NuGet Packages

```bash
# Swagger/OpenAPI
dotnet add package Swashbuckle.AspNetCore

# Logging
dotnet add package Serilog.AspNetCore
dotnet add package Serilog.Sinks.Console
dotnet add package Serilog.Sinks.File
```

### Step 3: Copy the Files

Place all the provided files in the following structure:

```
SkillBot.Api/
├── Controllers/
│   ├── ChatController.cs
│   ├── MultiAgentController.cs
│   └── PluginsController.cs
├── Middleware/
│   ├── ErrorHandlingMiddleware.cs
│   └── RequestLoggingMiddleware.cs
├── Models/
│   ├── Requests/
│   │   └── ChatRequest.cs
│   └── Responses/
│       └── ApiResponses.cs
├── Services/
│   └── ConversationService.cs
├── Program.cs
└── appsettings.json
```

### Step 4: Configure API Key

```bash
# Use user secrets (recommended for development)
dotnet user-secrets init
dotnet user-secrets set "SkillBot:ApiKey" "your-openai-api-key-here"

# Or edit appsettings.json directly (not recommended)
```

### Step 5: Build and Run

```bash
# Build
dotnet build

# Run
dotnet run

# API will be available at:
# - HTTP: http://localhost:5000
# - HTTPS: https://localhost:5001
# - Swagger UI: https://localhost:5001 (root)
```

## 📚 API Endpoints

### Chat Endpoints

#### POST /api/chat
Send a message to single-agent mode.

**Request**:
```json
{
  "message": "What's 25 * 17?",
  "conversationId": "conv_123abc"  // optional
}
```

**Response**:
```json
{
  "message": "25 multiplied by 17 equals 425.",
  "conversationId": "conv_123abc",
  "toolCalls": [
    {
      "pluginName": "Calculator",
      "functionName": "multiply",
      "arguments": { "a": 25, "b": 17 },
      "executionTimeMs": 45.2
    }
  ],
  "executionTimeMs": 1250.5,
  "tokensUsed": 156,
  "timestamp": "2026-04-16T10:30:00Z"
}
```

#### GET /api/chat/{conversationId}
Get conversation history.

**Response**:
```json
{
  "conversationId": "conv_123abc",
  "messages": [
    {
      "role": "user",
      "content": "What's 25 * 17?",
      "timestamp": "2026-04-16T10:30:00Z"
    },
    {
      "role": "assistant",
      "content": "25 multiplied by 17 equals 425.",
      "timestamp": "2026-04-16T10:30:01Z"
    }
  ],
  "createdAt": "2026-04-16T10:30:00Z",
  "lastActivityAt": "2026-04-16T10:30:01Z",
  "messageCount": 2
}
```

#### DELETE /api/chat/{conversationId}
Delete a conversation.

**Response**: 204 No Content

### Multi-Agent Endpoints

#### POST /api/multi-agent/chat
Execute task with multi-agent coordination.

**Request**:
```json
{
  "task": "Research Python and write a summary",
  "conversationId": "conv_456def"  // optional
}
```

**Response**:
```json
{
  "finalResponse": "Python is a high-level programming language...",
  "conversationId": "conv_456def",
  "strategy": "sequential",
  "agentsUsed": [
    {
      "agentId": "research-agent",
      "agentName": "Research Specialist",
      "result": "Python was created by Guido van Rossum...",
      "executionTimeMs": 2500,
      "success": true
    },
    {
      "agentId": "writing-agent",
      "agentName": "Writing Specialist",
      "result": "Summary: Python is...",
      "executionTimeMs": 1800,
      "success": true
    }
  ],
  "totalExecutionTimeMs": 4350.2,
  "timestamp": "2026-04-16T10:35:00Z"
}
```

#### GET /api/multi-agent/agents
List available specialized agents.

**Response**:
```json
[
  {
    "agentId": "research-agent",
    "name": "Research Specialist",
    "description": "Expert in information gathering and research",
    "specializations": ["research", "information", "facts"],
    "status": "Ready"
  }
]
```

### Plugin Endpoints

#### GET /api/plugins
List all registered plugins.

**Response**:
```json
[
  {
    "name": "Calculator",
    "description": "Basic arithmetic operations",
    "functions": [
      {
        "name": "add",
        "description": "Add two numbers",
        "parameters": [
          {
            "name": "a",
            "type": "double",
            "description": "First number",
            "isRequired": true
          }
        ]
      }
    ]
  }
]
```

#### GET /api/plugins/{pluginName}
Get specific plugin details.

### Health Check

#### GET /health
Health check endpoint.

**Response**:
```json
{
  "status": "Healthy",
  "checks": {
    "skillbot_health": {
      "status": "Healthy",
      "description": "SkillBot API is healthy"
    }
  }
}
```

## 🧪 Testing with Swagger

1. **Start the API**:
   ```bash
   dotnet run
   ```

2. **Open Swagger UI**:
   ```
   https://localhost:5001
   ```

3. **Try the Chat Endpoint**:
   - Expand `POST /api/chat`
   - Click "Try it out"
   - Enter request body:
     ```json
     {
       "message": "What's 10 + 5?"
     }
     ```
   - Click "Execute"
   - See the response!

## 🧪 Testing with curl

```bash
# Chat endpoint
curl -X POST https://localhost:5001/api/chat \
  -H "Content-Type: application/json" \
  -d '{"message": "What is 2+2?"}'

# Multi-agent endpoint
curl -X POST https://localhost:5001/api/multi-agent/chat \
  -H "Content-Type: application/json" \
  -d '{"task": "Research AI and summarize"}'

# Get plugins
curl https://localhost:5001/api/plugins

# Health check
curl https://localhost:5001/health
```

## 🔐 OAuth Preparation (Phase 2)

The API is designed to be OAuth-ready. When you implement OAuth:

### 1. Add Authentication Packages

```bash
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
```

### 2. Update Program.cs

```csharp
// Add authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => {
        options.Authority = "https://your-auth-provider.com";
        options.Audience = "skillbot-api";
    });

// Enable in middleware
app.UseAuthentication();
app.UseAuthorization();
```

### 3. Add [Authorize] Attributes

```csharp
[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    // ...
}
```

### 4. Implement User Context

```csharp
public interface IUserContext
{
    string UserId { get; }
    string Email { get; }
}

public class UserContext : IUserContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    
    public string UserId => _httpContextAccessor.HttpContext?
        .User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";
}
```

### 5. Filter Conversations by User

```csharp
// In ConversationService
public async Task<ConversationResponse?> GetConversationAsync(
    string conversationId, 
    string userId)
{
    // Filter by userId
    if (conversation.UserId != userId)
        throw new UnauthorizedAccessException();
    
    // ...
}
```

## 📊 Project Structure

```
SkillBot.Api/
├── Controllers/          # API endpoints
│   ├── ChatController         - Single-agent chat
│   ├── MultiAgentController   - Multi-agent orchestration
│   └── PluginsController      - Plugin management
│
├── Services/            # Business logic
│   └── ConversationService    - Conversation management
│
├── Middleware/          # HTTP pipeline
│   ├── ErrorHandlingMiddleware  - Global error handling
│   └── RequestLoggingMiddleware - Request logging
│
├── Models/              # DTOs
│   ├── Requests/        - Request models
│   └── Responses/       - Response models
│
└── Program.cs           # Application configuration
```

## 🎯 Next Steps

### Phase 1 Complete ✅
- REST API with Swagger
- Chat endpoints
- Multi-agent endpoints
- Plugin management
- Health checks
- Error handling

### Phase 2: OAuth (Future)
- JWT authentication
- User management
- Per-user conversations
- API key management

### Phase 3: Blazor UI (Future)
- Web interface
- Real-time chat
- Conversation browser
- Agent visualization

## 🐛 Troubleshooting

### Port Already in Use
```bash
# Change ports in Properties/launchSettings.json or:
dotnet run --urls "http://localhost:5555;https://localhost:5556"
```

### Swagger Not Showing
- Ensure you're in Development mode
- Check `app.UseSwagger()` is called
- Navigate to root URL: `https://localhost:5001/`

### CORS Errors
- Update `appsettings.json` Cors:AllowedOrigins
- Add your Blazor app origin

### API Key Not Found
```bash
# Set via user secrets
dotnet user-secrets set "SkillBot:ApiKey" "sk-..."
```

## 📚 Documentation

The API is fully documented with:
- XML comments on all endpoints
- Swagger UI at root URL
- Request/response examples
- Error codes and messages

## 🚀 Deployment

See [DEPLOYMENT.md](../docs/DEPLOYMENT.md) for production deployment instructions.

---

**Status**: Phase 1 Complete ✅  
**Next**: Test the API, then move to OAuth (Phase 2) or Blazor UI (Phase 3)
