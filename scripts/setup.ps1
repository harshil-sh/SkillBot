#Requires -Version 5.1
<#
.SYNOPSIS
    SkillBot Windows setup script.
.DESCRIPTION
    Checks prerequisites, configures .env, and launches SkillBot via Docker Compose.
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$RepoRoot = Split-Path $PSScriptRoot -Parent
Set-Location $RepoRoot

Write-Host "============================================" -ForegroundColor Cyan
Write-Host "  SkillBot Setup" -ForegroundColor Cyan
Write-Host "============================================"
Write-Host ""

# --- Prerequisites ---
function Assert-Command {
    param([string]$Name)
    if (-not (Get-Command $Name -ErrorAction SilentlyContinue)) {
        Write-Error "'$Name' is not installed or not on PATH. Please install it and try again."
        exit 1
    }
}

Write-Host "Checking prerequisites..." -ForegroundColor Yellow
Assert-Command "dotnet"
Assert-Command "docker"
$dotnetVer = (& dotnet --version 2>&1)
$dockerVer = (& docker --version 2>&1) -replace '\n.*',''
Write-Host "  dotnet: $dotnetVer"
Write-Host "  docker: $dockerVer"
Write-Host ""

# --- .env setup ---
if (-not (Test-Path ".env")) {
    Copy-Item ".env.example" ".env"
    Write-Host "Created .env from .env.example" -ForegroundColor Green
}

function Get-EnvValue {
    param([string]$Key)
    $line = Get-Content ".env" | Where-Object { $_ -match "^${Key}=" } | Select-Object -First 1
    if ($line) { return $line.Substring($Key.Length + 1) }
    return ""
}

function Set-EnvValue {
    param([string]$Key, [string]$Value)
    $content = Get-Content ".env" -Raw
    $content = $content -replace "(?m)^${Key}=.*", "${Key}=${Value}"
    Set-Content ".env" $content -NoNewline
}

# --- OpenAI API Key ---
$currentKey = Get-EnvValue "OPENAI_API_KEY"
if ($currentKey -eq "sk-your-openai-key-here" -or [string]::IsNullOrWhiteSpace($currentKey)) {
    $openaiKey = Read-Host "Enter your OpenAI API key (sk-...)"
    if (-not [string]::IsNullOrWhiteSpace($openaiKey)) {
        Set-EnvValue "OPENAI_API_KEY" $openaiKey
        Write-Host "  OpenAI API key saved." -ForegroundColor Green
    } else {
        Write-Warning "No OpenAI API key set. SkillBot will not function without an LLM key."
    }
} else {
    Write-Host "  OpenAI API key already configured." -ForegroundColor Green
}

# --- JWT Secret ---
$currentJwt = Get-EnvValue "JWT_SECRET"
if ($currentJwt -eq "your-super-secret-jwt-key-min-32-chars-change-this" -or [string]::IsNullOrWhiteSpace($currentJwt)) {
    $jwtSecret = Read-Host "Enter JWT secret (leave blank to auto-generate)"
    if ([string]::IsNullOrWhiteSpace($jwtSecret)) {
        # Generate a 64-char hex secret from 4 GUIDs
        $jwtSecret = (([System.Guid]::NewGuid().ToString("N")) + ([System.Guid]::NewGuid().ToString("N")))
        Write-Host "  Auto-generated JWT secret." -ForegroundColor Green
    }
    Set-EnvValue "JWT_SECRET" $jwtSecret
    Write-Host "  JWT secret saved." -ForegroundColor Green
} else {
    Write-Host "  JWT secret already configured." -ForegroundColor Green
}

# --- Telegram (optional) ---
$setupTelegram = Read-Host "Configure Telegram bot integration? [y/N]"
if ($setupTelegram -eq "y" -or $setupTelegram -eq "Y") {
    $tgToken    = Read-Host "  Telegram Bot Token"
    $tgUsername = Read-Host "  Telegram Bot Username (without @)"
    $tgWebhook  = Read-Host "  Webhook URL (e.g. https://your-domain.com/api/webhook/telegram)"
    Set-EnvValue "TELEGRAM_ENABLED"      "true"
    Set-EnvValue "TELEGRAM_BOT_TOKEN"    $tgToken
    Set-EnvValue "TELEGRAM_BOT_USERNAME" $tgUsername
    Set-EnvValue "TELEGRAM_WEBHOOK_URL"  $tgWebhook
    Write-Host "  Telegram configuration saved." -ForegroundColor Green
}

Write-Host ""
Write-Host "Starting SkillBot with Docker Compose..." -ForegroundColor Yellow
& docker compose up -d --build

Write-Host ""
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "  SkillBot is running!" -ForegroundColor Green
Write-Host "  API:     http://localhost:8080"
Write-Host "  Health:  http://localhost:8080/health"
Write-Host "  Swagger: http://localhost:8080/swagger"
Write-Host "============================================" -ForegroundColor Cyan
