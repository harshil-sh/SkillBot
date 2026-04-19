Write-Host "Testing Console Scenarios..." -ForegroundColor Cyan

$repoRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $repoRoot

$apiProcess = $null
$apiOut = Join-Path $env:TEMP "skillbot-api-out.log"
$apiErr = Join-Path $env:TEMP "skillbot-api-err.log"
$consoleSettingsFile = Join-Path $env:APPDATA "SkillBot\console-settings.json"

function Test-ApiAlive {
    try {
        $response = Invoke-WebRequest -Uri "http://localhost:5188/health" -UseBasicParsing -MaximumRedirection 0 -TimeoutSec 3 -ErrorAction Stop
        return ($response.StatusCode -eq 200 -or $response.StatusCode -eq 307 -or $response.StatusCode -eq 308)
    } catch {
        if ($_.Exception.Response) {
            $statusCode = [int]$_.Exception.Response.StatusCode
            return ($statusCode -eq 200 -or $statusCode -eq 307 -or $statusCode -eq 308)
        }

        return $false
    }
}

function Assert-Contains {
    param(
        [string]$Text,
        [string]$Pattern,
        [string]$Label
    )

    if ($Text -match [regex]::Escape($Pattern)) {
        Write-Host "PASS: $Label" -ForegroundColor Green
        return $true
    }

    Write-Host "FAIL: $Label" -ForegroundColor Red
    return $false
}

function Assert-ContainsAny {
    param(
        [string]$Text,
        [string[]]$Patterns,
        [string]$Label
    )

    foreach ($pattern in $Patterns) {
        if ($Text -match [regex]::Escape($pattern)) {
            Write-Host "PASS: $Label" -ForegroundColor Green
            return $true
        }
    }

    Write-Host "FAIL: $Label" -ForegroundColor Red
    return $false
}

try {
    if (Test-Path $consoleSettingsFile) {
        Remove-Item $consoleSettingsFile -Force
        Write-Host "Reset console settings cache." -ForegroundColor Yellow
    }

    if (-not (Test-ApiAlive)) {
        Write-Host "Starting API..." -ForegroundColor Yellow
        if (Test-Path $apiOut) { Remove-Item $apiOut -Force }
        if (Test-Path $apiErr) { Remove-Item $apiErr -Force }

        $apiProcess = Start-Process dotnet -ArgumentList "run --project SkillBot.Api" -PassThru -RedirectStandardOutput $apiOut -RedirectStandardError $apiErr

        $apiReady = $false
        for ($i = 0; $i -lt 60; $i++) {
            Start-Sleep -Seconds 1
            if (Test-ApiAlive) {
                $apiReady = $true
                break
            }

            if ($apiProcess.HasExited) {
                break
            }
        }

        if (-not $apiReady) {
            Write-Host "API failed to become ready." -ForegroundColor Red
            if (Test-Path $apiOut) {
                Write-Host "--- API STDOUT ---" -ForegroundColor DarkYellow
                Get-Content $apiOut | Select-Object -Last 40
            }
            if (Test-Path $apiErr) {
                Write-Host "--- API STDERR ---" -ForegroundColor DarkYellow
                Get-Content $apiErr | Select-Object -Last 40
            }
            exit 1
        }
    } else {
        Write-Host "API is already running." -ForegroundColor Yellow
    }

    $suffix = Get-Date -Format "yyyyMMddHHmmss"
    $email = "test-$suffix@example.com"
    $username = "testuser$suffix"
    $password = "password123"

    $commands = @(
        "login $email",
        $password,
        "register $email $password $username",
        "",
        "",
        "login $username",
        $password,
        "What is 2+2?",
        "search AI news",
        "settings set test-key test-value",
        "settings get test-key",
        "settings list",
        "stats",
        "health",
        "multi-agent Analyze the future of AI --agents researcher,writer,critic",
        "logout",
        "chat test",
        "login $username",
        $password,
        "chat '<script>alert(1)</script>'",
        "exit"
    )

    $inputScript = ($commands -join "`n") + "`n"

    Write-Host "Running console scenarios..." -ForegroundColor Yellow
    $consoleOutput = $inputScript | dotnet run --project SkillBot.Console/SkillBot.Console.csproj 2>&1 | Out-String

    Write-Host "--- Console Output (tail) ---" -ForegroundColor DarkYellow
    $consoleOutput.Split([Environment]::NewLine) | Select-Object -Last 60 | ForEach-Object { Write-Host $_ }

    $checks = @(
        (Assert-Contains -Text $consoleOutput -Pattern "Error: Invalid email or password" -Label "Login fails before registration"),
        (Assert-Contains -Text $consoleOutput -Pattern "Registered successfully." -Label "Register succeeds"),
        (Assert-Contains -Text $consoleOutput -Pattern "API key setup completed." -Label "API key onboarding shown"),
        (Assert-Contains -Text $consoleOutput -Pattern "Logged in successfully." -Label "Login succeeds"),
        (Assert-ContainsAny -Text $consoleOutput -Patterns @("Assistant:", "The sum of 2 and 2 is 4") -Label "Chat returns assistant response"),
        (Assert-Contains -Text $consoleOutput -Pattern "test-key = test-value" -Label "Settings set/get/list works"),
        (Assert-Contains -Text $consoleOutput -Pattern "Health: Healthy" -Label "Health command works"),
        (Assert-Contains -Text $consoleOutput -Pattern "Logged out successfully." -Label "Logout succeeds"),
        (Assert-Contains -Text $consoleOutput -Pattern "401 (Unauthorized)" -Label "Chat blocked after logout"),
        (Assert-Contains -Text $consoleOutput -Pattern "Input contains potentially malicious script content" -Label "Input safety check works")
    )

    $passed = ($checks | Where-Object { $_ }).Count
    $failed = $checks.Count - $passed

    Write-Host "" 
    Write-Host "Summary: $passed passed, $failed failed" -ForegroundColor Cyan

    if ($failed -gt 0) {
        exit 1
    }

    exit 0
}
finally {
    if ($apiProcess -and -not $apiProcess.HasExited) {
        Write-Host "Stopping API process..." -ForegroundColor Yellow
        Stop-Process -Id $apiProcess.Id -Force
    }
}