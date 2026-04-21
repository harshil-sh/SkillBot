# SkillBot Web UI — Troubleshooting Guide

This document covers the most common issues users and developers encounter with the SkillBot Blazor WebAssembly frontend.

---

## Table of Contents

1. [Login Issues](#1-login-issues)
2. [Blank Page / App Fails to Load](#2-blank-page--app-fails-to-load)
3. [API Connection Issues](#3-api-connection-issues)
4. [Performance Issues](#4-performance-issues)
5. [Browser Compatibility](#5-browser-compatibility)
6. [Settings Not Saved](#6-settings-not-saved)
7. [Chat Issues](#7-chat-issues)

---

## 1. Login Issues

### 1.1 CORS Error on Login

| | Detail |
|-|--------|
| **Symptom** | Browser console shows: `Access to fetch at 'https://localhost:7101/api/auth/login' from origin 'http://localhost:5000' has been blocked by CORS policy` |
| **Cause** | The SkillBot API does not have the web UI's origin in its CORS allowed origins list |
| **Solution** | In `SkillBot.Api/Program.cs`, add the web UI origin to the CORS policy: `policy.WithOrigins("http://localhost:5000", "https://localhost:7288")`. Restart the API. |
| **Prevention** | In production, set the `CORS:AllowedOrigins` configuration key to your production web UI URL. Do not use `AllowAnyOrigin()` in production. |

### 1.2 Wrong API URL

| | Detail |
|-|--------|
| **Symptom** | Login button spins indefinitely; no error in the UI; browser console shows a `net::ERR_CONNECTION_REFUSED` or `net::ERR_NAME_NOT_RESOLVED` error |
| **Cause** | `ApiSettings:BaseUrl` in `wwwroot/appsettings.json` points to the wrong host or port |
| **Solution** | Open browser DevTools → Network tab, click the failing request, and check the URL. Update `wwwroot/appsettings.json` to match the running API's address. |
| **Prevention** | Maintain separate `appsettings.json` and `appsettings.Production.json` files for different environments. |

### 1.3 Token Expiry — Redirected to Login Unexpectedly

| | Detail |
|-|--------|
| **Symptom** | User is navigated to `/login` after being previously authenticated |
| **Cause** | The JWT stored in `localStorage` has expired (default: 24 hours after login) |
| **Solution** | Log in again. If this is happening too frequently, ask the administrator to increase `JwtSettings:ExpirationMinutes` in `appsettings.json`. |
| **Prevention** | Implement token refresh logic in `CustomAuthStateProvider` to silently renew the token before it expires. |

### 1.4 "Invalid credentials" on Correct Password

| | Detail |
|-|--------|
| **Symptom** | `401 Unauthorized` despite entering the correct email/password |
| **Cause** | Account may be deactivated by an administrator, or the email is registered under a different casing |
| **Solution** | Try logging in via the API directly with `curl` to confirm. Contact an administrator to check account status. |

### 1.5 Admin Pages Show "Not Authorized"

| | Detail |
|-|--------|
| **Symptom** | Navigating to `/admin` shows "You are not authorized to access this resource" |
| **Cause** | The user's JWT was issued before their role was promoted to Admin, so the token still contains `role: User` |
| **Solution** | Log out and log back in. The new JWT will include the updated `Admin` role claim. |

---

## 2. Blank Page / App Fails to Load

### 2.1 White Screen on First Load

| | Detail |
|-|--------|
| **Symptom** | Browser shows a white/blank page; no content appears even after 10+ seconds |
| **Cause** | Multiple possible causes: JavaScript error, WASM download failure, missing `index.html` routing |
| **Solution** | 1. Open **DevTools → Console** — look for red errors. 2. Open **DevTools → Network** — look for failed requests (red rows). 3. Hard-refresh with `Ctrl+Shift+R` to bypass service worker cache. |
| **Prevention** | Test deployment with `dotnet publish -c Release` locally before pushing to production. |

### 2.2 WASM Download Failures

| | Detail |
|-|--------|
| **Symptom** | Network tab shows `dotnet.wasm` or `SkillBot.Web.dll` with status 404 or 403 |
| **Cause** | The web server is not configured to serve the `_framework/` directory, or MIME types for `.wasm` and `.dll` are missing |
| **Solution** | For nginx: ensure the `root` directive points to `wwwroot/`. For IIS: add `.wasm` and `.dll` MIME types (see [DEPLOYMENT_WEB.md](DEPLOYMENT_WEB.md)). |
| **Prevention** | Include `web.config` (IIS) or `nginx.conf` with explicit MIME type mappings in your deployment pipeline. |

### 2.3 "Loading…" Spinner Never Resolves

| | Detail |
|-|--------|
| **Symptom** | The Blazor loading progress bar appears but never completes |
| **Cause** | Network timeout downloading the WASM runtime (slow connection) or a JavaScript error after download |
| **Solution** | Wait up to 60 seconds on a slow connection. If it never resolves, check the console for errors after the spinner disappears. Check that `blazor.webassembly.js` loaded successfully. |
| **Prevention** | Enable Brotli compression on your web server to reduce WASM download size by ~70%. |

### 2.4 Blank Page After Navigation (Deep Link / Refresh)

| | Detail |
|-|--------|
| **Symptom** | Navigating directly to `https://skillbot.example.com/chat` or refreshing returns a 404 |
| **Cause** | The web server returned a 404 for `/chat` instead of serving `index.html` — SPA fallback not configured |
| **Solution** | Add the SPA fallback rule to your web server. nginx: `try_files $uri $uri/ /index.html;`. IIS: `web.config` URL Rewrite rule. Netlify: `netlify.toml` redirect. See [DEPLOYMENT_WEB.md](DEPLOYMENT_WEB.md). |
| **Prevention** | Always test deep-link navigation after deploying to a new hosting environment. |

---

## 3. API Connection Issues

### 3.1 `BaseAddress` Misconfiguration

| | Detail |
|-|--------|
| **Symptom** | All API calls fail; console shows requests going to `http://localhost:5000/api/...` instead of `https://localhost:7101/api/...` |
| **Cause** | `ApiSettings:BaseUrl` is not set in `wwwroot/appsettings.json` — the default `HttpClient.BaseAddress` is the web UI's own origin |
| **Solution** | Update `wwwroot/appsettings.json` to set `BaseUrl` to the API's full URL including scheme and port. Rebuild and redeploy. |
| **Prevention** | The `SkillBotApiClient` constructor should throw an `InvalidOperationException` if `BaseAddress` is not set to a valid absolute URI. |

### 3.2 Self-Signed Certificate Warning

| | Detail |
|-|--------|
| **Symptom** | `net::ERR_CERT_AUTHORITY_INVALID` errors when making API calls to `https://localhost:7101` |
| **Cause** | The ASP.NET development HTTPS certificate is not trusted by the browser |
| **Solution** | Run `dotnet dev-certs https --trust` and restart the browser. Alternatively, use `http://localhost:5188` (HTTP) in `appsettings.json` during development. |
| **Prevention** | In CI/CD, configure the API to use a real certificate or run behind a trusted reverse proxy. |

### 3.3 API Returns 500 for All Requests

| | Detail |
|-|--------|
| **Symptom** | Every API call returns HTTP 500; the web UI shows generic error messages |
| **Cause** | The API may have failed to start correctly (missing LLM API key, database connection failure) |
| **Solution** | Check the API logs: `SkillBot.Api/logs/skillbot-*.log`. Look for `Fatal` or `Error` level entries at startup. |
| **Prevention** | Add a health check call on web UI startup. If `/health` returns non-healthy, show a maintenance banner. |

---

## 4. Performance Issues

### 4.1 Slow First Load

| | Detail |
|-|--------|
| **Symptom** | The app takes 5–15 seconds to show on first visit |
| **Cause** | WASM runtime download (~2.5 MB compressed) on first visit; no cached WASM |
| **Solution** | This is normal on first load over a slow connection. Subsequent loads use the service worker cache and are near-instant. Enable Brotli compression to reduce download size. |
| **Prevention** | Use a CDN with edge caching for the `_framework/` directory. Set long `Cache-Control` TTLs (e.g., 1 week) for WASM and DLL files. |

### 4.2 Large Bundle Size

| | Detail |
|-|--------|
| **Symptom** | Published `_framework/` directory is over 10 MB uncompressed |
| **Cause** | MudBlazor and the .NET runtime are included in the bundle |
| **Solution** | Ensure you publish with `-c Release` (enables IL trimming). Add `<PublishTrimmed>true</PublishTrimmed>` to `SkillBot.Web.csproj`. Check that unused NuGet packages are removed. |
| **Prevention** | Run `dotnet publish --self-contained false -c Release` and inspect output sizes in CI. |

### 4.3 Slow API Responses (First Message After Idle)

| | Detail |
|-|--------|
| **Symptom** | The first chat message after a period of inactivity takes 10–30 seconds; subsequent messages are fast |
| **Cause** | The LLM provider's model has a cold-start latency; or the API container was in a low-CPU state |
| **Solution** | Enable the two-tier cache (`SkillBot:Caching:Enabled: true`). For repeated questions the cache returns near-instantly. Consider using a "warmer" Hangfire job that sends a dummy message on startup. |

### 4.4 Memory Usage Grows Over Time

| | Detail |
|-|--------|
| **Symptom** | Browser tab memory usage increases steadily over a long chat session |
| **Cause** | All messages are held in `_messages` list; very long conversations can accumulate thousands of items |
| **Solution** | Implement virtual scrolling for the message list using MudBlazor's `MudVirtualize` component to render only visible messages. |

---

## 5. Browser Compatibility

### Supported Browsers

Blazor WebAssembly requires a modern browser with WebAssembly support:

| Browser | Minimum version | Notes |
|---------|----------------|-------|
| Chrome / Chromium | 90+ | Fully supported |
| Firefox | 90+ | Fully supported |
| Edge (Chromium) | 90+ | Fully supported |
| Safari | 15+ | Supported; some CSS animation differences |
| Safari iOS | 15.4+ | Supported |
| Chrome Android | 90+ | Supported |
| IE 11 | ❌ | Not supported — WebAssembly not available |

### 5.1 Safari-Specific Issues

| | Detail |
|-|--------|
| **Symptom** | App loads but `localStorage` calls fail silently in Safari private mode |
| **Cause** | Safari blocks `localStorage` in private browsing mode |
| **Solution** | Use a normal Safari window. If `localStorage` is unavailable, `CustomAuthStateProvider` falls back to session-only memory state (user will need to log in again on next tab open). |

---

## 6. Settings Not Saved

| | Detail |
|-|--------|
| **Symptom** | Changes to settings appear saved (snackbar confirmation shows) but revert after page refresh |
| **Cause** | The PUT request to `/api/settings` returned an error that was silently swallowed, or the JWT expired between the settings fetch and save |
| **Solution** | Open DevTools → Network, click the failed PUT request, and inspect the response body. Common cause: expired JWT → log out and back in, then retry. |
| **Prevention** | Settings components should display inline error messages on failed saves, not just a success snackbar. |

---

## 7. Chat Issues

### 7.1 "No LLM provider configured" Error

| | Detail |
|-|--------|
| **Symptom** | Sending a chat message returns an error: "No LLM provider is configured for your account" |
| **Cause** | Neither a system-level API key (set by admin) nor a personal API key (set by user) is configured for any provider |
| **Solution** | Go to **Settings → API Keys** and enter your OpenAI, Claude, or Gemini API key. |

### 7.2 Responses Cut Off Mid-Sentence

| | Detail |
|-|--------|
| **Symptom** | The assistant's reply ends abruptly in the middle of a sentence |
| **Cause** | The `max_tokens` limit for the configured model was reached |
| **Solution** | Ask the question again with instructions to be more concise, or break it into smaller questions. Administrators can increase the `MaxTokens` setting in `appsettings.json`. |

### 7.3 Conversation Not Saved After Refresh

| | Detail |
|-|--------|
| **Symptom** | After a page refresh, the conversation appears empty even though messages were sent |
| **Cause** | The "Save Conversation History" setting is disabled in Privacy settings |
| **Solution** | Go to **Settings → Privacy** and enable "Save Conversation History". Future conversations will be saved. Previously unsaved conversations are lost. |

---

*See also: [USER_GUIDE_WEB.md](USER_GUIDE_WEB.md) · [FAQ_WEB.md](FAQ_WEB.md) · [DEPLOYMENT_WEB.md](DEPLOYMENT_WEB.md)*
