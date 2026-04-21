# SkillBot Web — Developer Setup Guide

This guide walks a new developer through setting up a local development environment for the `SkillBot.Web` Blazor WebAssembly frontend and its companion `SkillBot.Api` backend.

---

## Table of Contents

1. [Prerequisites](#1-prerequisites)
2. [Clone and Restore](#2-clone-and-restore)
3. [Configure the API](#3-configure-the-api)
4. [Configure the Web UI](#4-configure-the-web-ui)
5. [Run in Development](#5-run-in-development)
6. [Hot Reload](#6-hot-reload)
7. [Build for Production](#7-build-for-production)
8. [VS Code Setup](#8-vs-code-setup)
9. [Visual Studio Setup](#9-visual-studio-setup)
10. [Recommended Workflow](#10-recommended-workflow)
11. [Common Issues and Solutions](#11-common-issues-and-solutions)

---

## 1. Prerequisites

| Requirement | Minimum version | Download |
|-------------|----------------|---------|
| .NET SDK | **10.0** | https://dotnet.microsoft.com/download/dotnet/10.0 |
| Node.js | Not required | — |
| Browser | Chrome 90+ / Firefox 90+ / Edge 90+ / Safari 15+ | — |
| Git | 2.x | https://git-scm.com |

### Verify your .NET version

```powershell
dotnet --version
# Should print: 10.0.x
```

### Optional but recommended

- **VS Code** with the C# Dev Kit extension (see §8)
- **Visual Studio 2022** 17.13+ (see §9)

---

## 2. Clone and Restore

```bash
# Clone the repository
git clone https://github.com/harshil-sh/SkillBot.git
cd SkillBot

# Restore all NuGet packages for the entire solution
dotnet restore SkillBot.slnx
```

This restores packages for all five projects: `SkillBot.Core`, `SkillBot.Infrastructure`, `SkillBot.Api`, `SkillBot.Console`, and `SkillBot.Web`.

---

## 3. Configure the API

The web UI depends on `SkillBot.Api`. You must have the API running locally before the web UI can do anything useful.

### Set secrets (development)

```powershell
cd SkillBot.Api

# Required: JWT signing key (min 32 characters)
dotnet user-secrets set "JwtSettings:Secret" "dev-secret-key-change-in-production-abc123"

# At least one LLM provider key
dotnet user-secrets set "SkillBot:ApiKey" "sk-your-openai-key-here"

# Optional: Anthropic / Gemini keys
dotnet user-secrets set "SkillBot:ClaudeApiKey" "sk-ant-your-claude-key"
dotnet user-secrets set "SkillBot:GeminiApiKey" "AIzaSy-your-gemini-key"
```

### Verify API configuration

```powershell
cd SkillBot.Api
dotnet user-secrets list
```

### Run the API

```powershell
dotnet run --project SkillBot.Api/SkillBot.Api.csproj
```

The API starts on:
- **HTTPS:** `https://localhost:7101`
- **HTTP:** `http://localhost:5188`

Swagger UI is available at `https://localhost:7101/swagger`.

---

## 4. Configure the Web UI

`SkillBot.Web` reads its API base URL from `wwwroot/appsettings.json`:

```json
{
  "ApiSettings": {
    "BaseUrl": "https://localhost:7101"
  }
}
```

The default value (`https://localhost:7101`) matches the API's default HTTPS port. If you changed the API port in `SkillBot.Api/Properties/launchSettings.json`, update this file accordingly.

> **Note:** `wwwroot/appsettings.json` is a public file — it is served as-is to the browser. **Do not put secrets here.** Only put the API base URL.

### Development vs production config

For production, you can override `appsettings.json` with `appsettings.Production.json`:

```
SkillBot.Web/wwwroot/
├── appsettings.json             # dev defaults
└── appsettings.Production.json  # production overrides (not committed to git)
```

---

## 5. Run in Development

Open **two terminals** side by side.

**Terminal 1 — API:**

```powershell
dotnet run --project SkillBot.Api/SkillBot.Api.csproj
```

**Terminal 2 — Web UI:**

```powershell
dotnet run --project SkillBot.Web/SkillBot.Web.csproj
```

The web UI starts on:
- **HTTP:** `http://localhost:5000`
- **HTTPS:** `https://localhost:7288`

Open your browser to `http://localhost:5000`. The Blazor WASM runtime downloads (~2–5 MB on first load), and the app bootstraps.

### Accepting the dev certificate

On first run you may see a browser security warning for `https://localhost:7101`. Install the ASP.NET development certificate:

```powershell
dotnet dev-certs https --trust
```

---

## 6. Hot Reload

Blazor WebAssembly supports hot reload via `dotnet watch`. Changes to `.razor` files, C# code, and CSS are reflected in the browser without a full page reload.

```powershell
dotnet watch --project SkillBot.Web/SkillBot.Web.csproj
```

### What hot reload supports

| Change type | Hot-reloaded? | Notes |
|-------------|:------------:|-------|
| `.razor` markup changes | ✅ | Applied in < 1 s |
| `@code` block changes (method body) | ✅ | Applied in < 1 s |
| New class member added | ✅ | Applied with rebuild |
| New `.razor` file added | ✅ | Requires rebuild trigger |
| `wwwroot/css/app.css` | ✅ | CSS reloaded without WASM rebuild |
| `Program.cs` service registration | ⚠️ | Requires manual restart |
| `_Imports.razor` changes | ⚠️ | Requires manual restart |

Press `Ctrl+R` in the dotnet watch terminal to force a full rebuild.

---

## 7. Build for Production

### Publish

```powershell
dotnet publish SkillBot.Web/SkillBot.Web.csproj -c Release -o ./publish/web
```

The output in `./publish/web/wwwroot/` is a set of static files (HTML, CSS, JS, WASM blobs) that can be served by any static file host (nginx, IIS, Netlify, Azure Static Web Apps, etc.).

Key output files:

```
publish/web/wwwroot/
├── index.html
├── _framework/           # WASM runtime + app DLLs (compressed)
├── css/
└── SkillBot.Web.styles.css
```

### Verify bundle size

```powershell
# Check compressed sizes
Get-ChildItem ./publish/web/wwwroot/_framework -Recurse |
    Sort-Object Length -Descending |
    Select-Object -First 10 Name, @{N="KB";E={[math]::Round($_.Length/1KB,1)}}
```

Typical first-load size:
- WASM runtime: ~2.5 MB compressed
- App DLLs: ~300 KB compressed
- MudBlazor CSS: ~150 KB compressed

### Update production `appsettings.json`

Before publishing, update `wwwroot/appsettings.json` or `wwwroot/appsettings.Production.json`:

```json
{
  "ApiSettings": {
    "BaseUrl": "https://api.skillbot.example.com"
  }
}
```

---

## 8. VS Code Setup

### Required extensions

| Extension | ID | Purpose |
|-----------|-----|---------|
| **C# Dev Kit** | `ms-dotnettools.csdevkit` | Full C# language support, solution explorer |
| **C#** | `ms-dotnettools.csharp` | C# language server (IntelliSense, diagnostics) |
| **.NET MAUI** (optional) | `ms-dotnettools.dotnet-maui` | Includes Blazor snippets |
| **Prettier** (optional) | `esbenp.prettier-vscode` | Format CSS/JSON |

Install via command line:

```bash
code --install-extension ms-dotnettools.csdevkit
code --install-extension ms-dotnettools.csharp
```

### Recommended `settings.json`

```json
{
  "editor.formatOnSave": true,
  "editor.tabSize": 4,
  "files.associations": {
    "*.razor": "aspnetcorerazor"
  },
  "[razor]": {
    "editor.defaultFormatter": "ms-dotnettools.csdevkit"
  },
  "dotnet.defaultSolution": "SkillBot.slnx"
}
```

### `launch.json` for debugging

```json
{
  "version": "0.2.0",
  "configurations": [
    {
      "name": "Launch SkillBot.Web (WASM)",
      "type": "blazorwasm",
      "request": "launch",
      "cwd": "${workspaceFolder}/SkillBot.Web"
    },
    {
      "name": "Launch SkillBot.Api",
      "type": "coreclr",
      "request": "launch",
      "program": "${workspaceFolder}/SkillBot.Api/bin/Debug/net10.0/SkillBot.Api.dll",
      "cwd": "${workspaceFolder}/SkillBot.Api",
      "env": { "ASPNETCORE_ENVIRONMENT": "Development" }
    }
  ],
  "compounds": [
    {
      "name": "API + Web",
      "configurations": ["Launch SkillBot.Api", "Launch SkillBot.Web (WASM)"]
    }
  ]
}
```

---

## 9. Visual Studio Setup

1. Open `SkillBot.slnx` in Visual Studio 2022 (17.13+).
2. Set **Multiple Startup Projects**:
   - Right-click Solution → **Configure Startup Projects**
   - Set both `SkillBot.Api` and `SkillBot.Web` to **Start**
3. Press **F5** — both projects launch and the browser opens automatically.

Visual Studio automatically enables hot reload for Blazor WASM projects when running with the debugger attached.

---

## 10. Recommended Workflow

```
Daily development cycle:

1. git pull --rebase
2. dotnet restore SkillBot.slnx   (if packages changed)
3. Terminal 1: dotnet watch --project SkillBot.Api/SkillBot.Api.csproj
4. Terminal 2: dotnet watch --project SkillBot.Web/SkillBot.Web.csproj
5. Browser: http://localhost:5000
6. Edit .razor / .cs files → browser updates automatically
7. git commit -m "feat: ..."
```

---

## 11. Common Issues and Solutions

### ❌ "Unable to connect to the remote server"

**Symptom:** Browser shows a connection error on the login page.  
**Cause:** `SkillBot.Api` is not running, or is running on a different port.  
**Fix:**
1. Confirm the API is running: `curl -k https://localhost:7101/health`
2. Check `SkillBot.Web/wwwroot/appsettings.json` — `BaseUrl` must match the API's actual port.

---

### ❌ CORS error in browser console

**Symptom:** `Access to fetch at 'https://localhost:7101/api/...' from origin 'http://localhost:5000' has been blocked by CORS policy`  
**Cause:** The API's CORS policy does not include the web UI's origin.  
**Fix:** In `SkillBot.Api/Program.cs`, add `http://localhost:5000` to the allowed origins:

```csharp
policy.WithOrigins("http://localhost:5000", "https://localhost:7288")
```

---

### ❌ Blank page / app fails to load

**Symptom:** Browser shows a blank white page.  
**Fix:**
1. Open browser DevTools → Console tab.
2. Common causes:
   - `appsettings.json` has a JSON syntax error → fix and rebuild.
   - A required NuGet package failed to download → run `dotnet restore`.
   - Dev certificate not trusted → run `dotnet dev-certs https --trust`.

---

### ❌ Hot reload stops working

**Symptom:** Changes to `.razor` files are not reflected in the browser.  
**Fix:**
1. Press `Ctrl+R` in the `dotnet watch` terminal to force rebuild.
2. If that fails, stop `dotnet watch` and restart it.
3. Clear browser cache with `Ctrl+Shift+R`.

---

### ❌ `dotnet watch` fails with "file is being used by another process"

**Symptom:** `dotnet watch` exits immediately on Windows.  
**Cause:** A stale `dotnet.exe` process from a previous session is locking a DLL.  
**Fix:**

```powershell
Get-Process dotnet | Stop-Process
```

---

### ❌ Login succeeds but redirects back to /login

**Symptom:** After entering correct credentials, the page briefly shows chat then redirects to login.  
**Cause:** `localStorage` is unavailable (private/incognito mode with strict settings, or a browser extension blocking storage).  
**Fix:** Use a normal browser window, or check `Application → Local Storage` in DevTools to confirm `skillbot_jwt` was set.

---

### ❌ "The current user is not authorized"

**Symptom:** Admin pages show "Not authorized" even for admin users.  
**Cause:** The JWT does not contain the `role` claim, or the role is `"User"` not `"Admin"`.  
**Fix:**
1. Log out and log back in to refresh the JWT.
2. Verify the user has the Admin role: `GET /api/admin/users` with an existing admin token.

---

*See also: [ARCHITECTURE.md](ARCHITECTURE.md) · [DEPLOYMENT_WEB.md](../DEPLOYMENT_WEB.md)*
