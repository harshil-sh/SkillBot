<#
.SYNOPSIS
    Prepares the SkillBot environment and guides you through recording a demo GIF.

.DESCRIPTION
    Starts SkillBot with docker-compose, waits for services to become healthy,
    opens the browser, and displays a step-by-step recording guide.

.EXAMPLE
    .\prepare-demo-recording.ps1
    .\prepare-demo-recording.ps1 -SkipDocker     # skip docker-compose (already running)
    .\prepare-demo-recording.ps1 -NoOpen         # don't auto-open the browser
#>

param(
    [switch]$SkipDocker,
    [switch]$NoOpen,
    [string]$BaseUrl   = "http://localhost:8080",
    [string]$HealthUrl = "http://localhost:8080/health"
)

# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------

function Write-Header {
    param([string]$Text)
    $line = "-" * 68
    Write-Host ""
    Write-Host "  $line" -ForegroundColor Cyan
    $pad = [Math]::Max(0, [Math]::Floor((68 - $Text.Length) / 2))
    Write-Host ("  " + (" " * $pad) + $Text) -ForegroundColor Cyan
    Write-Host "  $line" -ForegroundColor Cyan
    Write-Host ""
}

function Write-Step {
    param([int]$Number, [string]$Text, [string]$Duration = "", [ConsoleColor]$Color = "White")
    $dur = if ($Duration) { "  [$Duration]" } else { "" }
    Write-Host "  [Step $Number]  $Text$dur" -ForegroundColor $Color
}

function Write-Tip {
    param([string]$Text)
    Write-Host "  TIP  $Text" -ForegroundColor Yellow
}

function Write-Reminder {
    param([string]$Text)
    Write-Host "  NOTE $Text" -ForegroundColor Magenta
}

function Write-Ok {
    param([string]$Text)
    Write-Host "  OK   $Text" -ForegroundColor Green
}

function Write-Info {
    param([string]$Text)
    Write-Host "  -->  $Text" -ForegroundColor DarkCyan
}

function Wait-ForHealth {
    param([string]$Url, [int]$TimeoutSec = 120, [int]$IntervalSec = 3)

    Write-Info "Waiting for $Url to become healthy..."
    $deadline = (Get-Date).AddSeconds($TimeoutSec)
    $attempt  = 0

    while ((Get-Date) -lt $deadline) {
        $attempt++
        try {
            $resp = Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec 4 -ErrorAction Stop
            if ($resp.StatusCode -in 200, 204) {
                Write-Ok "Service healthy after $attempt attempt(s)."
                return $true
            }
        } catch {
            # not ready yet
        }
        Write-Host "." -NoNewline -ForegroundColor DarkGray
        Start-Sleep -Seconds $IntervalSec
    }

    Write-Host ""
    Write-Warning "Service did not become healthy within ${TimeoutSec}s."
    return $false
}

# ---------------------------------------------------------------------------
# Banner
# ---------------------------------------------------------------------------

Clear-Host
Write-Host ""
Write-Host "  ================================================" -ForegroundColor Cyan
Write-Host "    SkillBot -- Demo GIF Recording Preparation    " -ForegroundColor Cyan
Write-Host "  ================================================" -ForegroundColor Cyan
Write-Host ""

# ---------------------------------------------------------------------------
# Pre-flight: recording tools
# ---------------------------------------------------------------------------

Write-Header "PRE-FLIGHT CHECKLIST"

Write-Tip "Install a screen recorder before continuing:"
Write-Host "     ScreenToGif  ->  https://www.screentogif.com  (Windows - recommended)" -ForegroundColor Gray
Write-Host "     LICEcap      ->  https://www.cockos.com/licecap  (Windows / macOS)" -ForegroundColor Gray
Write-Host "     Peek         ->  https://github.com/phw/peek  (Linux)" -ForegroundColor Gray
Write-Host "     Gifox        ->  https://gifox.io  (macOS)" -ForegroundColor Gray
Write-Host ""

Write-Reminder "Recorder settings to configure:"
Write-Host "     Resolution : 1280 x 720" -ForegroundColor DarkGray
Write-Host "     Frame rate : 15 FPS" -ForegroundColor DarkGray
Write-Host "     Max size   : 5 MB" -ForegroundColor DarkGray
Write-Host "     Output     : docs/assets/demo.gif" -ForegroundColor DarkGray
Write-Host ""

$null = Read-Host "  Press ENTER when your recorder is open and configured (or Ctrl+C to abort)"

# ---------------------------------------------------------------------------
# Step 1 -- Start Docker services
# ---------------------------------------------------------------------------

Write-Header "STEP 1 -- STARTING SKILLBOT SERVICES"

if ($SkipDocker) {
    Write-Ok "Skipping docker-compose (SkipDocker flag set)."
} else {
    if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
        Write-Warning "docker not found in PATH. Is Docker Desktop running?"
        Write-Info "Start Docker Desktop, then re-run this script."
        exit 1
    }

    if (-not (Test-Path "docker-compose.yml")) {
        Write-Warning "docker-compose.yml not found in the current directory."
        Write-Info "Run this script from the SkillBot repository root."
        exit 1
    }

    Write-Info "Running: docker compose up -d"
    docker compose up -d
    if ($LASTEXITCODE -ne 0) {
        Write-Warning "docker compose up returned exit code $LASTEXITCODE"
        Write-Info "Check 'docker compose logs' for details."
        exit 1
    }
    Write-Ok "Containers started."
}

# ---------------------------------------------------------------------------
# Step 2 -- Wait for health
# ---------------------------------------------------------------------------

Write-Header "STEP 2 -- WAITING FOR SERVICES"

$healthy = Wait-ForHealth -Url $HealthUrl -TimeoutSec 120
Write-Host ""

if (-not $healthy) {
    $cont = Read-Host "  Service may not be ready. Continue anyway? [y/N]"
    if ($cont -notmatch "^[Yy]") { exit 1 }
}

# ---------------------------------------------------------------------------
# Step 3 -- Open browser
# ---------------------------------------------------------------------------

Write-Header "STEP 3 -- OPENING BROWSER"

if ($NoOpen) {
    Write-Info "Skipping browser open (NoOpen flag set)."
    Write-Info "Navigate manually to: $BaseUrl"
} else {
    Write-Info "Opening $BaseUrl in your default browser..."
    Start-Process $BaseUrl
    Start-Sleep -Seconds 2
    Write-Ok "Browser opened."
}

# ---------------------------------------------------------------------------
# Step 4 -- Recording guide
# ---------------------------------------------------------------------------

Write-Header "STEP 4 -- RECORDING GUIDE"

Write-Host "  Resize your browser window to 1280 x 720, then start your recorder." -ForegroundColor White
Write-Host "  Follow the script below. Suggested timing shown in [brackets]." -ForegroundColor DarkGray
Write-Host ""

Write-Step 1 "Show terminal with 'docker compose up -d' output" "~2 s" Cyan
Write-Host ""
Write-Step 2 "Switch to browser -- homepage / landing loads" "~1 s" Cyan
Write-Host ""
Write-Step 3 "Show Login / Register screen -- pause briefly" "~2 s" Yellow
Write-Tip    "Register a fresh account so the flow is obvious to viewers."
Write-Host ""
Write-Step 4 "Login succeeds -- Chat interface loads" "~1 s" Yellow
Write-Host ""
Write-Step 5 'Type message: "Explain quantum computing in simple terms"' "~2 s" Green
Write-Tip    "Type at a natural speed -- not too fast."
Write-Host ""
Write-Step 6 "Pause and show the AI response appearing (streaming text)" "~3 s" Green
Write-Tip    "If streaming is not yet enabled, scroll slowly through the response."
Write-Host ""
Write-Step 7 "Toggle multi-agent mode OR switch LLM provider in Settings" "~2 s" Magenta
Write-Tip    "Multi-agent: click the robot icon in the chat toolbar."
Write-Tip    "Provider switch: Settings -> General -> LLM Provider dropdown."
Write-Host ""
Write-Step 8 'Send message: "Compare Python and Rust for systems programming"' "~3 s" Magenta
Write-Tip    "Show the response appearing, then end the recording."
Write-Host ""

Write-Host "  ----------------------------------------------------------------" -ForegroundColor DarkGray
Write-Host "  Total target length: 15 - 20 seconds" -ForegroundColor White
Write-Host "  ----------------------------------------------------------------" -ForegroundColor DarkGray
Write-Host ""

# ---------------------------------------------------------------------------
# Step 5 -- Post-recording instructions
# ---------------------------------------------------------------------------

Write-Header "STEP 5 -- AFTER RECORDING"

Write-Info "Export / optimise your GIF:"
Write-Host ""
Write-Host "  Option A -- ScreenToGif built-in optimiser:" -ForegroundColor White
Write-Host "    File -> Save As -> GIF" -ForegroundColor Gray
Write-Host "    Encoder: FFmpeg  |  Quality: 85  |  FPS: 15" -ForegroundColor Gray
Write-Host ""
Write-Host "  Option B -- gifski (best quality):" -ForegroundColor White
Write-Host "    winget install gifski" -ForegroundColor DarkCyan
Write-Host "    gifski --fps 15 --quality 85 --width 1280 -o docs\assets\demo.gif frames\*.png" -ForegroundColor DarkCyan
Write-Host ""
Write-Host "  Option C -- ImageMagick:" -ForegroundColor White
Write-Host "    magick convert -delay 7 -loop 0 -layers Optimize frames\*.png docs\assets\demo.gif" -ForegroundColor DarkCyan
Write-Host ""

Write-Reminder "Check the file size after export:"
Write-Host "    (Get-Item docs\assets\demo.gif).Length / 1MB" -ForegroundColor DarkCyan
Write-Host "    Target: under 5 MB  (GitHub README inline GIF limit)" -ForegroundColor DarkGray
Write-Host ""

Write-Info "Save the file to:  docs\assets\demo.gif"
Write-Host ""
Write-Info "Then add it to README.md in the header section:"
Write-Host '    ![SkillBot Demo](docs/assets/demo.gif)' -ForegroundColor DarkCyan
Write-Host ""

# ---------------------------------------------------------------------------
# Done
# ---------------------------------------------------------------------------

Write-Header "ALL SET -- HAPPY RECORDING!"

Write-Host "  Quick links:" -ForegroundColor White
Write-Host "    Web UI      ->  $BaseUrl" -ForegroundColor DarkCyan
Write-Host "    Swagger     ->  http://localhost:8080/swagger" -ForegroundColor DarkCyan
Write-Host "    API health  ->  $HealthUrl" -ForegroundColor DarkCyan
Write-Host ""
Write-Host "  Useful docker commands:" -ForegroundColor White
Write-Host "    docker compose logs -f    # tail all logs" -ForegroundColor DarkGray
Write-Host "    docker compose ps         # check container status" -ForegroundColor DarkGray
Write-Host "    docker compose down       # stop everything" -ForegroundColor DarkGray
Write-Host ""