#!/bin/bash
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(dirname "$SCRIPT_DIR")"
COVERAGE_DIR="$ROOT_DIR/coverage"

rm -rf "$COVERAGE_DIR"
mkdir -p "$COVERAGE_DIR"

dotnet test "$ROOT_DIR/SkillBot.slnx" \
    --collect:"XPlat Code Coverage" \
    --results-directory "$COVERAGE_DIR"

if command -v reportgenerator &> /dev/null; then
    reportgenerator \
        -reports:"$COVERAGE_DIR/**/coverage.cobertura.xml" \
        -targetdir:"$COVERAGE_DIR/report" \
        -reporttypes:"Html;TextSummary"
    echo "Coverage report at: $COVERAGE_DIR/report/index.html"
else
    echo "Install reportgenerator: dotnet tool install --global dotnet-reportgenerator-globaltool"
fi
