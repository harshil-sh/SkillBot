#!/usr/bin/env bash
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$REPO_ROOT"

echo "============================================"
echo "  SkillBot Setup"
echo "============================================"
echo ""

# --- Prerequisites ---
check_cmd() {
  if ! command -v "$1" &>/dev/null; then
    echo "ERROR: '$1' is not installed or not on PATH."
    exit 1
  fi
}

echo "Checking prerequisites..."
check_cmd dotnet
check_cmd docker
echo "  dotnet: $(dotnet --version)"
echo "  docker: $(docker --version | head -1)"
echo ""

# --- .env setup ---
if [ ! -f ".env" ]; then
  cp .env.example .env
  echo "Created .env from .env.example"
fi

# --- OpenAI API Key ---
current_key=$(grep -E '^OPENAI_API_KEY=' .env | cut -d'=' -f2-)
if [ "$current_key" = "sk-your-openai-key-here" ] || [ -z "$current_key" ]; then
  read -rp "Enter your OpenAI API key (sk-...): " openai_key
  if [ -n "$openai_key" ]; then
    sed -i "s|^OPENAI_API_KEY=.*|OPENAI_API_KEY=${openai_key}|" .env
    echo "  OpenAI API key saved."
  else
    echo "  WARNING: No OpenAI API key set. SkillBot will not function without an LLM key."
  fi
else
  echo "  OpenAI API key already configured."
fi

# --- JWT Secret ---
current_jwt=$(grep -E '^JWT_SECRET=' .env | cut -d'=' -f2-)
if [ "$current_jwt" = "your-super-secret-jwt-key-min-32-chars-change-this" ] || [ -z "$current_jwt" ]; then
  read -rp "Enter JWT secret (leave blank to auto-generate): " jwt_secret
  if [ -z "$jwt_secret" ]; then
    jwt_secret=$(openssl rand -hex 32 2>/dev/null || cat /proc/sys/kernel/random/uuid 2>/dev/null | tr -d '-' || echo "$(date +%s)-$(hostname)-skillbot-secret-$(id -u)")
    echo "  Auto-generated JWT secret."
  fi
  sed -i "s|^JWT_SECRET=.*|JWT_SECRET=${jwt_secret}|" .env
  echo "  JWT secret saved."
else
  echo "  JWT secret already configured."
fi

# --- Telegram (optional) ---
read -rp "Configure Telegram bot integration? [y/N]: " setup_telegram
if [[ "${setup_telegram,,}" == "y" ]]; then
  read -rp "  Telegram Bot Token: " tg_token
  read -rp "  Telegram Bot Username (without @): " tg_username
  read -rp "  Webhook URL (e.g. https://your-domain.com/api/webhook/telegram): " tg_webhook
  sed -i "s|^TELEGRAM_ENABLED=.*|TELEGRAM_ENABLED=true|" .env
  sed -i "s|^TELEGRAM_BOT_TOKEN=.*|TELEGRAM_BOT_TOKEN=${tg_token}|" .env
  sed -i "s|^TELEGRAM_BOT_USERNAME=.*|TELEGRAM_BOT_USERNAME=${tg_username}|" .env
  sed -i "s|^TELEGRAM_WEBHOOK_URL=.*|TELEGRAM_WEBHOOK_URL=${tg_webhook}|" .env
  echo "  Telegram configuration saved."
fi

echo ""
echo "Starting SkillBot with Docker Compose..."
docker compose up -d --build

echo ""
echo "============================================"
echo "  SkillBot is running!"
echo "  API:    http://localhost:8080"
echo "  Health: http://localhost:8080/health"
echo "  Swagger: http://localhost:8080/swagger"
echo "============================================"
