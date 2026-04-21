#!/bin/bash
# Build self-contained Linux x64 binary package

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(dirname "$SCRIPT_DIR")"
VERSION="1.0.0"
OUTPUT_DIR="$ROOT_DIR/dist"

echo "Building SkillBot v$VERSION for Linux x64..."

# Clean output
rm -rf "$OUTPUT_DIR"
mkdir -p "$OUTPUT_DIR/skillbot-linux-x64"

# Build self-contained
dotnet publish "$ROOT_DIR/SkillBot.Api/SkillBot.Api.csproj" \
    -c Release \
    -r linux-x64 \
    --self-contained \
    -p:PublishSingleFile=true \
    -o "$OUTPUT_DIR/skillbot-linux-x64"

# Copy config template
cp "$ROOT_DIR/.env.example" "$OUTPUT_DIR/skillbot-linux-x64/.env.example"

# Create quick-start README
cat > "$OUTPUT_DIR/skillbot-linux-x64/README.txt" << 'EOF'
SkillBot - Self-Hosted AI Assistant
=====================================

Quick Start:
1. Copy .env.example to .env and fill in your API keys
2. Run: ./SkillBot.Api
3. Open: http://localhost:8080

For full documentation: https://github.com/harshil-sh/SkillBot
EOF

# Create package
cd "$OUTPUT_DIR"
tar -czf "skillbot-linux-x64-v${VERSION}.tar.gz" skillbot-linux-x64/

echo "Package created: $OUTPUT_DIR/skillbot-linux-x64-v${VERSION}.tar.gz"
