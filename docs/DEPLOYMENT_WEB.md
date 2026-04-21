# SkillBot Web UI — Deployment Guide

This guide covers deploying the `SkillBot.Web` Blazor WebAssembly frontend alongside the `SkillBot.Api` backend.

---

## Table of Contents

1. [Build Overview](#1-build-overview)
2. [Docker Compose (API + nginx)](#2-docker-compose-api--nginx)
3. [Static File Hosting](#3-static-file-hosting)
4. [IIS](#4-iis)
5. [ASP.NET Core Static File Hosting](#5-aspnet-core-static-file-hosting)
6. [Production Checklist](#6-production-checklist)

---

## 1. Build Overview

Blazor WebAssembly compiles to static files. The published output is a directory of HTML, CSS, JavaScript, and WebAssembly blobs that can be served by **any** static file host.

### Build command

```bash
dotnet publish SkillBot.Web/SkillBot.Web.csproj \
    -c Release \
    -o ./publish/web
```

### Output structure

```
publish/web/wwwroot/
├── index.html                   # SPA shell — serve for ALL routes
├── _framework/                  # WASM runtime + .NET DLLs (Brotli compressed)
│   ├── blazor.webassembly.js
│   ├── dotnet.wasm.br
│   └── SkillBot.Web.dll.br
├── css/
│   ├── app.css
│   └── bootstrap/
├── SkillBot.Web.styles.css      # Scoped CSS from .razor.css files
├── manifest.webmanifest
├── service-worker.js
└── appsettings.json             # ← Update this before publishing!
```

### Update `appsettings.json` before publishing

```json
{
  "ApiSettings": {
    "BaseUrl": "https://api.skillbot.example.com"
  }
}
```

The API base URL is read by the browser at runtime, so this is the only configuration change required for a new deployment target.

---

## 2. Docker Compose (API + nginx)

This is the recommended production setup: **nginx** serves the Blazor static files and **reverse-proxies** API calls to the `SkillBot.Api` container.

### `docker-compose.yml`

```yaml
services:
  skillbot-api:
    build:
      context: .
      dockerfile: Dockerfile
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - JwtSettings__Secret=${JWT_SECRET}
      - SkillBot__ApiKey=${OPENAI_API_KEY}
      - ConnectionStrings__SkillBot=Data Source=/data/skillbot.db
    volumes:
      - skillbot-data:/data
    expose:
      - "8080"
    restart: unless-stopped

  skillbot-web:
    image: nginx:1.27-alpine
    ports:
      - "80:80"
      - "443:443"
    volumes:
      - ./publish/web/wwwroot:/usr/share/nginx/html:ro
      - ./nginx.conf:/etc/nginx/conf.d/default.conf:ro
      - ./certs:/etc/nginx/certs:ro   # TLS certificates
    depends_on:
      - skillbot-api
    restart: unless-stopped

volumes:
  skillbot-data:
```

### `nginx.conf`

```nginx
server {
    listen 80;
    server_name skillbot.example.com;
    return 301 https://$host$request_uri;
}

server {
    listen 443 ssl http2;
    server_name skillbot.example.com;

    # TLS
    ssl_certificate     /etc/nginx/certs/fullchain.pem;
    ssl_certificate_key /etc/nginx/certs/privkey.pem;
    ssl_protocols       TLSv1.2 TLSv1.3;
    ssl_ciphers         ECDHE-ECDSA-AES128-GCM-SHA256:ECDHE-RSA-AES128-GCM-SHA256;

    # Security headers
    add_header Strict-Transport-Security "max-age=31536000; includeSubDomains" always;
    add_header X-Frame-Options "DENY" always;
    add_header X-Content-Type-Options "nosniff" always;
    add_header Referrer-Policy "strict-origin-when-cross-origin" always;
    add_header Content-Security-Policy "default-src 'self'; script-src 'self' 'wasm-unsafe-eval'; style-src 'self' 'unsafe-inline' https://fonts.googleapis.com; font-src 'self' https://fonts.gstatic.com; connect-src 'self' https://api.skillbot.example.com; img-src 'self' data:;" always;

    root /usr/share/nginx/html;
    index index.html;

    # Serve Brotli-compressed WASM
    location ~* \.wasm$ {
        gzip_static off;
        brotli_static on;
        types { application/wasm wasm; }
        add_header Cache-Control "public, max-age=604800, immutable";
    }

    # Serve Brotli-compressed DLLs
    location ~* \.dll$ {
        brotli_static on;
        types { application/octet-stream dll; }
        add_header Cache-Control "public, max-age=604800, immutable";
    }

    # Cache static assets aggressively
    location ~* \.(js|css|png|ico|webmanifest)$ {
        add_header Cache-Control "public, max-age=86400";
    }

    # Proxy API calls
    location /api/ {
        proxy_pass         http://skillbot-api:8080;
        proxy_http_version 1.1;
        proxy_set_header   Host $host;
        proxy_set_header   X-Real-IP $remote_addr;
        proxy_set_header   X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header   X-Forwarded-Proto $scheme;
    }

    # Proxy health and swagger
    location ~ ^/(health|swagger|hangfire) {
        proxy_pass http://skillbot-api:8080;
    }

    # SPA fallback — all other routes serve index.html
    location / {
        try_files $uri $uri/ /index.html;
    }
}
```

> **The `try_files ... /index.html` directive is critical.** Without it, navigating directly to `/chat` or refreshing the page returns a 404 from nginx.

### Build and deploy

```bash
# 1. Publish the web project
dotnet publish SkillBot.Web/SkillBot.Web.csproj -c Release -o ./publish/web

# 2. Start all services
docker compose up -d

# 3. Verify
curl -I https://skillbot.example.com
curl https://skillbot.example.com/health
```

---

## 3. Static File Hosting

Because the output is pure static files, you can host `SkillBot.Web` on any CDN or static hosting service. The API must be hosted separately (see `SkillBot.Api` deployment) and CORS must be configured.

### Netlify

1. Connect your GitHub repository to Netlify.
2. Set build settings:
   - **Build command:** `dotnet publish SkillBot.Web/SkillBot.Web.csproj -c Release -o publish/web`
   - **Publish directory:** `publish/web/wwwroot`
3. Add a `netlify.toml` in the repo root for SPA routing:

```toml
[[redirects]]
  from = "/*"
  to = "/index.html"
  status = 200
```

4. Set environment-specific `appsettings.json` values via Netlify's build inject or update `appsettings.json` in a pre-build script.

### GitHub Pages

GitHub Pages does not support custom 404 → index.html redirects natively. Use a `404.html` workaround:

```bash
# After publish, copy index.html as 404.html
cp publish/web/wwwroot/index.html publish/web/wwwroot/404.html
```

Add to `index.html` a script that restores the URL from the 404.html redirect. See [rafrex/spa-github-pages](https://github.com/rafrex/spa-github-pages) for the canonical approach.

### Azure Static Web Apps

Azure Static Web Apps natively supports SPA routing via `staticwebapp.config.json`:

```json
{
  "navigationFallback": {
    "rewrite": "/index.html",
    "exclude": ["/_framework/*", "/css/*", "*.{png,ico,webmanifest}"]
  },
  "globalHeaders": {
    "Cache-Control": "public, max-age=0, must-revalidate",
    "X-Frame-Options": "DENY"
  }
}
```

Place this file in `SkillBot.Web/wwwroot/` so it is included in the published output.

---

## 4. IIS

### `web.config` for SPA routing

Create `SkillBot.Web/wwwroot/web.config`:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <system.webServer>
    <rewrite>
      <rules>
        <!-- Serve existing files and directories directly -->
        <rule name="Static Assets" stopProcessing="true">
          <match url="([\S]+[.](html|htm|svg|js|css|wasm|dll|png|ico|webmanifest|json))" />
          <action type="None" />
        </rule>
        <!-- SPA fallback: rewrite all other URLs to index.html -->
        <rule name="SPA Fallback" stopProcessing="true">
          <match url=".*" />
          <conditions>
            <add input="{REQUEST_FILENAME}" matchType="IsFile" negate="true" />
            <add input="{REQUEST_FILENAME}" matchType="IsDirectory" negate="true" />
          </conditions>
          <action type="Rewrite" url="/index.html" />
        </rule>
      </rules>
    </rewrite>
    <!-- Serve Brotli-compressed WASM correctly -->
    <staticContent>
      <mimeMap fileExtension=".wasm" mimeType="application/wasm" />
      <mimeMap fileExtension=".dll" mimeType="application/octet-stream" />
    </staticContent>
    <httpCompression>
      <dynamicTypes>
        <add mimeType="application/wasm" enabled="false" />
      </dynamicTypes>
    </httpCompression>
  </system.webServer>
</configuration>
```

### IIS Site configuration

1. Create a new IIS website pointing to `publish/web/wwwroot/`.
2. Set the application pool to **No Managed Code** (Blazor WASM is purely static — no .NET runtime needed in IIS).
3. Install the **URL Rewrite** IIS module if not present.

---

## 5. ASP.NET Core Static File Hosting

You can serve the Blazor WASM output directly from `SkillBot.Api` without a separate static file server. This simplifies deployment to a single process.

In `SkillBot.Api/Program.cs`:

```csharp
// Serve Blazor WASM files from wwwroot
app.UseDefaultFiles();
app.UseStaticFiles();

// SPA fallback for client-side routing
app.MapFallbackToFile("index.html");
```

Add the published web files to the API's `wwwroot/` directory during the build pipeline:

```bash
dotnet publish SkillBot.Web/SkillBot.Web.csproj -c Release -o ./publish/web
dotnet publish SkillBot.Api/SkillBot.Api.csproj -c Release -o ./publish/api
cp -r ./publish/web/wwwroot/* ./publish/api/wwwroot/
```

The API then serves both the REST endpoints and the Blazor SPA from the same origin, eliminating CORS issues.

---

## 6. Production Checklist

### Security

- [ ] `JwtSettings:Secret` is at least 64 random characters (use `openssl rand -base64 64`)
- [ ] HTTPS is enforced (HTTP redirects to HTTPS)
- [ ] `Content-Security-Policy` header is set (see nginx.conf example)
- [ ] `X-Frame-Options: DENY` is set
- [ ] `X-Content-Type-Options: nosniff` is set
- [ ] CORS `AllowedOrigins` lists only your production web UI URL
- [ ] `appsettings.json` in `wwwroot/` contains no secrets (API URL only)

### Performance

- [ ] Brotli compression is enabled for `_framework/` files
- [ ] `Cache-Control: immutable` is set for versioned assets (WASM, DLLs)
- [ ] `Cache-Control: no-cache` is set for `index.html` (ensures new deployments are loaded)
- [ ] PWA service worker is enabled for offline support

### Availability

- [ ] Health check endpoint `/health` is monitored
- [ ] Log file rotation is configured (Serilog rolling file)
- [ ] Database backup is scheduled (SQLite file copy)
- [ ] Docker container restart policy is `unless-stopped`

### Configuration

- [ ] `ApiSettings:BaseUrl` in `wwwroot/appsettings.json` points to the production API URL
- [ ] `Features:Registration` is set appropriately (disable if invite-only)
- [ ] Rate limits are tuned for expected traffic
- [ ] LLM provider API keys are set via environment variables (not in `appsettings.json`)

---

*See also: [DEVELOPMENT.md](frontend/DEVELOPMENT.md) · [TROUBLESHOOTING_WEB.md](TROUBLESHOOTING_WEB.md) · [docs/DEPLOYMENT.md](DEPLOYMENT.md)*
