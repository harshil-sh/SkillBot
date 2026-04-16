# SkillBot Deployment Guide

## Table of Contents
- [Deployment Options](#deployment-options)
- [Local Development](#local-development)
- [Windows Deployment](#windows-deployment)
- [Linux Deployment](#linux-deployment)
- [Docker Deployment](#docker-deployment)
- [Cloud Deployment](#cloud-deployment)
- [Configuration](#configuration)
- [Monitoring](#monitoring)
- [Troubleshooting](#troubleshooting)

## Deployment Options

SkillBot can be deployed in several ways depending on your needs:

| Method | Best For | Pros | Cons |
|--------|----------|------|------|
| **Local Console** | Development, Testing | Easy setup, Full control | Not scalable |
| **Windows Service** | Production Windows | Auto-start, Background | Windows-only |
| **Linux Systemd** | Production Linux | Auto-start, Logs | Linux-only |
| **Docker** | Cloud, Containerized | Portable, Scalable | Requires Docker |
| **Azure/AWS** | Enterprise, Scale | Managed, HA | Cost, Complexity |

## Local Development

### Prerequisites

```bash
# Required
- .NET 10 SDK or later
- OpenAI API key

# Optional
- Visual Studio 2022 / VS Code / Rider
- SQLite browser (for database inspection)
- Git
```

### Setup

```bash
# 1. Clone the repository
git clone <your-repo-url>
cd SkillBot

# 2. Restore dependencies
dotnet restore

# 3. Configure API key
cd SkillBot.Console
dotnet user-secrets init
dotnet user-secrets set "SkillBot:ApiKey" "your-openai-api-key-here"

# 4. Build
dotnet build

# 5. Run
dotnet run

# Run in multi-agent mode
dotnet run -- --multi-agent
```

### Development Configuration

Create `appsettings.Development.json` (gitignored):

```json
{
  "SkillBot": {
    "Model": "gpt-4",
    "VerboseLogging": true,
    "MemoryProvider": "InMemory",
    "MaxHistoryMessages": 50
  },
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft": "Information"
    }
  }
}
```

## Windows Deployment

### Option 1: Self-Contained Executable

```bash
# Publish as standalone executable
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true

# Output location
cd SkillBot.Console/bin/Release/net10.0/win-x64/publish/

# Run
SkillBot.Console.exe
```

**Distribution**:
- Copy the `publish` folder to target machine
- No .NET installation required on target
- Include `appsettings.json` with the executable

### Option 2: Windows Service

#### Step 1: Install as Windows Service

```bash
# Publish
dotnet publish -c Release -r win-x64

# Create service (run as Administrator)
sc create SkillBot binPath="C:\Apps\SkillBot\SkillBot.Console.exe" start=auto

# Start service
sc start SkillBot

# Stop service
sc stop SkillBot

# Delete service
sc delete SkillBot
```

#### Step 2: Modify Program.cs for Service

Add this NuGet package:
```bash
dotnet add package Microsoft.Extensions.Hosting.WindowsServices
```

Update `Program.cs`:
```csharp
var host = Host.CreateDefaultBuilder(args)
    .UseWindowsService() // Add this line
    .ConfigureServices(...)
    .Build();
```

### Configuration Location (Windows)

```
C:\ProgramData\SkillBot\appsettings.json
```

Or use environment variables:
```cmd
setx SkillBot__ApiKey "your-key-here" /M
setx SkillBot__Model "gpt-4" /M
```

## Linux Deployment

### Option 1: Self-Contained Binary

```bash
# Publish for Linux
dotnet publish -c Release -r linux-x64 --self-contained

# Copy to server
scp -r publish/ user@server:/opt/skillbot/

# On server, make executable
chmod +x /opt/skillbot/SkillBot.Console

# Run
/opt/skillbot/SkillBot.Console
```

### Option 2: Systemd Service

#### Step 1: Create Service File

Create `/etc/systemd/system/skillbot.service`:

```ini
[Unit]
Description=SkillBot AI Agent Service
After=network.target

[Service]
Type=notify
WorkingDirectory=/opt/skillbot
ExecStart=/opt/skillbot/SkillBot.Console
Restart=always
RestartSec=10
KillSignal=SIGINT
SyslogIdentifier=skillbot
User=skillbot
Environment="ASPNETCORE_ENVIRONMENT=Production"
Environment="SkillBot__ApiKey=your-api-key-here"

[Install]
WantedBy=multi-user.target
```

#### Step 2: Enable and Start Service

```bash
# Reload systemd
sudo systemctl daemon-reload

# Enable service (start on boot)
sudo systemctl enable skillbot

# Start service
sudo systemctl start skillbot

# Check status
sudo systemctl status skillbot

# View logs
sudo journalctl -u skillbot -f

# Stop service
sudo systemctl stop skillbot

# Restart service
sudo systemctl restart skillbot
```

#### Step 3: Modify Program.cs for Systemd

Add this NuGet package:
```bash
dotnet add package Microsoft.Extensions.Hosting.Systemd
```

Update `Program.cs`:
```csharp
var host = Host.CreateDefaultBuilder(args)
    .UseSystemd() // Add this line
    .ConfigureServices(...)
    .Build();
```

### Configuration Location (Linux)

```bash
# Option 1: Application directory
/opt/skillbot/appsettings.json

# Option 2: System config
/etc/skillbot/appsettings.json

# Option 3: User config
~/.config/skillbot/appsettings.json

# Option 4: Environment variables
export SkillBot__ApiKey="your-key"
export SkillBot__Model="gpt-4"
```

## Docker Deployment

### Dockerfile

Create `Dockerfile` in solution root:

```dockerfile
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files
COPY ["SkillBot.Core/SkillBot.Core.csproj", "SkillBot.Core/"]
COPY ["SkillBot.Infrastructure/SkillBot.Infrastructure.csproj", "SkillBot.Infrastructure/"]
COPY ["SkillBot.Plugins/SkillBot.Plugins.csproj", "SkillBot.Plugins/"]
COPY ["SkillBot.Console/SkillBot.Console.csproj", "SkillBot.Console/"]

# Restore dependencies
RUN dotnet restore "SkillBot.Console/SkillBot.Console.csproj"

# Copy source code
COPY . .

# Build
WORKDIR "/src/SkillBot.Console"
RUN dotnet build "SkillBot.Console.csproj" -c Release -o /app/build

# Publish
FROM build AS publish
RUN dotnet publish "SkillBot.Console.csproj" -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/runtime:10.0 AS final
WORKDIR /app

# Copy published app
COPY --from=publish /app/publish .

# Create data directory for SQLite
RUN mkdir -p /app/data

# Set environment
ENV ASPNETCORE_ENVIRONMENT=Production

# Run
ENTRYPOINT ["dotnet", "SkillBot.Console.dll"]
```

### docker-compose.yml

```yaml
version: '3.8'

services:
  skillbot:
    build:
      context: .
      dockerfile: Dockerfile
    container_name: skillbot
    restart: unless-stopped
    environment:
      - SkillBot__ApiKey=${OPENAI_API_KEY}
      - SkillBot__Model=gpt-4
      - SkillBot__MemoryProvider=SQLite
      - SkillBot__SqliteDatabasePath=/app/data/skillbot.db
    volumes:
      - ./data:/app/data
    stdin_open: true
    tty: true
```

### .env File

Create `.env` (gitignored):

```bash
OPENAI_API_KEY=your-api-key-here
```

### Build and Run

```bash
# Build image
docker build -t skillbot:latest .

# Run with docker-compose
docker-compose up -d

# View logs
docker-compose logs -f

# Stop
docker-compose down

# Rebuild after code changes
docker-compose up -d --build
```

### Docker Hub Deployment

```bash
# Tag image
docker tag skillbot:latest yourusername/skillbot:latest

# Push to Docker Hub
docker push yourusername/skillbot:latest

# Pull and run on any machine
docker pull yourusername/skillbot:latest
docker run -e SkillBot__ApiKey="your-key" yourusername/skillbot:latest
```

## Cloud Deployment

### Azure Container Instances

```bash
# Login to Azure
az login

# Create resource group
az group create --name skillbot-rg --location eastus

# Create container instance
az container create \
  --resource-group skillbot-rg \
  --name skillbot \
  --image yourusername/skillbot:latest \
  --dns-name-label skillbot-demo \
  --ports 80 \
  --environment-variables \
    SkillBot__ApiKey="your-key" \
    SkillBot__Model="gpt-4"

# View logs
az container logs --resource-group skillbot-rg --name skillbot

# Delete
az container delete --resource-group skillbot-rg --name skillbot
```

### AWS EC2

```bash
# 1. Launch EC2 instance (Ubuntu)
# 2. SSH into instance

# Install .NET
wget https://dot.net/v1/dotnet-install.sh
chmod +x dotnet-install.sh
./dotnet-install.sh --channel 10.0

# Clone and setup
git clone <your-repo>
cd SkillBot
dotnet restore
dotnet build

# Configure
export SkillBot__ApiKey="your-key"

# Run with systemd (see Linux section above)
```

### AWS ECS (Fargate)

```bash
# 1. Push Docker image to ECR
aws ecr create-repository --repository-name skillbot

# Tag and push
docker tag skillbot:latest <account-id>.dkr.ecr.us-east-1.amazonaws.com/skillbot:latest
docker push <account-id>.dkr.ecr.us-east-1.amazonaws.com/skillbot:latest

# 2. Create ECS task definition (use AWS Console or CLI)

# 3. Create ECS service
aws ecs create-service \
  --cluster skillbot-cluster \
  --service-name skillbot-service \
  --task-definition skillbot:1 \
  --desired-count 1 \
  --launch-type FARGATE
```

## Configuration

### Production Configuration

Create `appsettings.Production.json`:

```json
{
  "SkillBot": {
    "Model": "gpt-4",
    "VerboseLogging": false,
    "MemoryProvider": "SQLite",
    "SqliteDatabasePath": "/app/data/skillbot.db",
    "MaxHistoryMessages": 100
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "System": "Warning"
    }
  }
}
```

### Environment-Specific Settings

```bash
# Development
export ASPNETCORE_ENVIRONMENT=Development

# Staging
export ASPNETCORE_ENVIRONMENT=Staging

# Production
export ASPNETCORE_ENVIRONMENT=Production
```

### Secrets Management

#### Option 1: Environment Variables (Recommended)

```bash
# Linux/Mac
export SkillBot__ApiKey="your-key"

# Windows
setx SkillBot__ApiKey "your-key"

# Docker
docker run -e SkillBot__ApiKey="your-key" skillbot:latest
```

#### Option 2: Azure Key Vault

```bash
# Install package
dotnet add package Azure.Extensions.AspNetCore.Configuration.Secrets

# In Program.cs
var keyVaultEndpoint = new Uri(Environment.GetEnvironmentVariable("VaultUri"));
builder.Configuration.AddAzureKeyVault(keyVaultEndpoint, new DefaultAzureCredential());
```

#### Option 3: AWS Secrets Manager

```bash
# Install AWS SDK
dotnet add package AWSSDK.SecretsManager

# Retrieve secret in code
var client = new AmazonSecretsManagerClient();
var response = await client.GetSecretValueAsync(new GetSecretValueRequest 
{ 
    SecretId = "skillbot/api-key" 
});
```

## Monitoring

### Logging

SkillBot uses Microsoft.Extensions.Logging. Configure output:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "SkillBot": "Debug"
    },
    "Console": {
      "IncludeScopes": true
    }
  }
}
```

### Log Locations

- **Console**: stdout/stderr
- **Windows Service**: Windows Event Log
- **Linux Systemd**: journalctl
- **Docker**: `docker logs <container>`

### Health Checks

Add health check endpoint (if building REST API):

```csharp
builder.Services.AddHealthChecks()
    .AddCheck("database", () => 
        /* check SQLite connection */);

app.MapHealthChecks("/health");
```

### Metrics

Monitor these metrics:
- API call count
- Token usage
- Response time
- Error rate
- Memory usage

## Troubleshooting

### Issue: API Key Not Found

**Symptoms**: "OpenAI API key not found" error

**Solutions**:
```bash
# Verify environment variable
echo $SkillBot__ApiKey  # Linux/Mac
echo %SkillBot__ApiKey%  # Windows

# Check user secrets
dotnet user-secrets list

# Check appsettings.json (not recommended for production)
cat appsettings.json | grep ApiKey
```

### Issue: SQLite Database Locked

**Symptoms**: "Database is locked" error

**Solutions**:
1. Ensure only one instance is running
2. Check file permissions
3. Enable WAL mode (should be automatic)
4. Restart application

```bash
# Check if file is locked
lsof skillbot.db  # Linux
```

### Issue: Port Already in Use

**Symptoms**: (If running REST API) "Address already in use"

**Solutions**:
```bash
# Find process using port
netstat -ano | findstr :5000  # Windows
lsof -i :5000                  # Linux

# Kill process
taskkill /PID <pid> /F  # Windows
kill -9 <pid>           # Linux
```

### Issue: Out of Memory

**Symptoms**: Application crashes, slow performance

**Solutions**:
1. Reduce `MaxHistoryMessages` in configuration
2. Use SQLite instead of in-memory provider
3. Increase container memory limits
4. Implement conversation pruning

```json
{
  "SkillBot": {
    "MaxHistoryMessages": 50  // Reduce from 100
  }
}
```

### Issue: Permission Denied

**Symptoms**: Cannot write to database or logs

**Solutions**:
```bash
# Fix file permissions
chmod 755 /opt/skillbot
chown -R skillbot:skillbot /opt/skillbot

# Create required directories
mkdir -p /app/data
chmod 777 /app/data
```

## Performance Tuning

### Database Optimization

```bash
# Vacuum SQLite database periodically
sqlite3 skillbot.db "VACUUM;"

# Check database size
du -h skillbot.db
```

### Memory Management

```json
{
  "SkillBot": {
    "MaxHistoryMessages": 50,  // Lower for less memory
    "MemoryProvider": "SQLite"  // Use SQLite instead of in-memory
  }
}
```

### Concurrency

For multi-user scenarios:
- Use separate database per user
- Implement connection pooling
- Consider Redis for session state

## Backup and Recovery

### Backup SQLite Database

```bash
# Manual backup
cp skillbot.db skillbot.db.backup

# Automated backup script
#!/bin/bash
DATE=$(date +%Y%m%d_%H%M%S)
cp skillbot.db "backups/skillbot_$DATE.db"

# Keep only last 7 days
find backups/ -name "skillbot_*.db" -mtime +7 -delete
```

### Restore Database

```bash
# Stop application
systemctl stop skillbot

# Restore backup
cp backups/skillbot_20260416.db skillbot.db

# Start application
systemctl start skillbot
```

## Security Best Practices

1. ✅ **Never commit API keys** to version control
2. ✅ **Use environment variables** or secret managers
3. ✅ **Run as non-root user** (Linux)
4. ✅ **Enable firewall** rules
5. ✅ **Keep .NET runtime updated**
6. ✅ **Use HTTPS** (if exposing API)
7. ✅ **Rotate API keys** regularly
8. ✅ **Monitor API usage** for anomalies

## Scaling

### Horizontal Scaling

For multiple instances:
1. Use separate database per instance
2. Implement load balancer
3. Use shared state (Redis)
4. Consider message queue for tasks

### Vertical Scaling

Increase resources:
- More CPU cores (parallel processing)
- More RAM (larger conversation history)
- Faster disk (SQLite performance)

---

## Quick Reference

### Common Commands

```bash
# Development
dotnet run
dotnet run -- --multi-agent

# Build
dotnet build -c Release

# Publish
dotnet publish -c Release -r linux-x64

# Docker
docker build -t skillbot .
docker run -e SkillBot__ApiKey="key" skillbot

# Systemd
sudo systemctl start skillbot
sudo systemctl status skillbot
sudo journalctl -u skillbot -f
```

---

**Document Version**: 1.0  
**Last Updated**: 2026-04-16  
**For Issues**: Open an issue on GitHub
