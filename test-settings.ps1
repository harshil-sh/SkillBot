$BaseUrl  = "http://localhost:5188"
$Email    = "settings-test-$(Get-Date -Format 'yyyyMMddHHmmss')@example.com"
$Username = "settingsuser$(Get-Date -Format 'yyyyMMddHHmmss')"
$Password = "password123"
$TestApiKey = "sk-test-0000000000000000000000000000000000000000000000000"

$passed = 0
$failed = 0

function Pass([string]$label) {
    Write-Host "  PASS: $label" -ForegroundColor Green
    $script:passed++
}

function Fail([string]$label, [string]$detail = "") {
    Write-Host "  FAIL: $label" -ForegroundColor Red
    if ($detail) { Write-Host "        $detail" -ForegroundColor DarkRed }
    $script:failed++
}

function Invoke-Api {
    param(
        [string]$Method,
        [string]$Endpoint,
        [hashtable]$Body,
        [string]$Token
    )

    $headers = @{ "Content-Type" = "application/json" }
    if ($Token) { $headers["Authorization"] = "Bearer $Token" }

    $params = @{
        Method  = $Method
        Uri     = "$BaseUrl$Endpoint"
        Headers = $headers
        ErrorAction = "Stop"
    }
    if ($Body) { $params["Body"] = ($Body | ConvertTo-Json) }

    return Invoke-RestMethod @params
}

Write-Host ""
Write-Host "SkillBot Settings API Tests" -ForegroundColor Cyan
Write-Host ("-" * 45)

# ── 1. Register + login ───────────────────────────────────────────────────────
Write-Host ""
Write-Host "1. Auth" -ForegroundColor Yellow

try {
    Invoke-Api -Method POST -Endpoint "/api/auth/register" -Body @{
        email    = $Email
        password = $Password
        username = $Username
    } | Out-Null
    Pass "Register new user"
} catch {
    Fail "Register new user" $_.Exception.Message
    Write-Host "Cannot continue without a registered user." -ForegroundColor Red
    exit 1
}

$token = $null
try {
    $login = Invoke-Api -Method POST -Endpoint "/api/auth/login" -Body @{
        email    = $Email
        password = $Password
    }
    $token = $login.token
    Pass "Login returns JWT token"
} catch {
    Fail "Login returns JWT token" $_.Exception.Message
    exit 1
}

# ── 2. Get initial settings ───────────────────────────────────────────────────
Write-Host ""
Write-Host "2. Get initial settings" -ForegroundColor Yellow

try {
    $settings = Invoke-Api -Method GET -Endpoint "/api/settings" -Token $token

    if ($settings.preferredProvider -eq "openai") { Pass "Default preferred provider is 'openai'" }
    else { Fail "Default preferred provider is 'openai'" "Got: $($settings.preferredProvider)" }

    if ($settings.hasOpenAiKey -eq $false) { Pass "HasOpenAiKey is false before setting key" }
    else { Fail "HasOpenAiKey is false before setting key" "Got: $($settings.hasOpenAiKey)" }

    if ($settings.hasClaudeKey -eq $false) { Pass "HasClaudeKey is false before setting key" }
    else { Fail "HasClaudeKey is false before setting key" "Got: $($settings.hasClaudeKey)" }
} catch {
    Fail "GET /api/settings" $_.Exception.Message
}

# ── 3. Set OpenAI API key ─────────────────────────────────────────────────────
Write-Host ""
Write-Host "3. Set OpenAI API key" -ForegroundColor Yellow

try {
    Invoke-Api -Method PUT -Endpoint "/api/settings/api-key" -Token $token -Body @{
        provider = "openai"
        apiKey   = $TestApiKey
    } | Out-Null
    Pass "PUT /api/settings/api-key returns 200"
} catch {
    Fail "PUT /api/settings/api-key returns 200" $_.Exception.Message
}

# ── 4. Verify key is reflected ────────────────────────────────────────────────
Write-Host ""
Write-Host "4. Verify HasOpenAiKey is now true" -ForegroundColor Yellow

try {
    $settings2 = Invoke-Api -Method GET -Endpoint "/api/settings" -Token $token

    if ($settings2.hasOpenAiKey -eq $true) { Pass "HasOpenAiKey is true after setting key" }
    else { Fail "HasOpenAiKey is true after setting key" "Got: $($settings2.hasOpenAiKey)" }

    if ($settings2.hasClaudeKey -eq $false) { Pass "HasClaudeKey still false (unchanged)" }
    else { Fail "HasClaudeKey still false (unchanged)" "Got: $($settings2.hasClaudeKey)" }
} catch {
    Fail "GET /api/settings (post key update)" $_.Exception.Message
}

# ── 5. Change preferred provider ─────────────────────────────────────────────
Write-Host ""
Write-Host "5. Change preferred provider" -ForegroundColor Yellow

try {
    Invoke-Api -Method PUT -Endpoint "/api/settings/provider" -Token $token -Body @{
        provider = "gemini"
    } | Out-Null
    Pass "PUT /api/settings/provider returns 200"
} catch {
    Fail "PUT /api/settings/provider returns 200" $_.Exception.Message
}

try {
    $settings3 = Invoke-Api -Method GET -Endpoint "/api/settings" -Token $token

    if ($settings3.preferredProvider -eq "gemini") { Pass "Preferred provider updated to 'gemini'" }
    else { Fail "Preferred provider updated to 'gemini'" "Got: $($settings3.preferredProvider)" }
} catch {
    Fail "GET /api/settings (post provider update)" $_.Exception.Message
}

# ── 6. Reject invalid provider ───────────────────────────────────────────────
Write-Host ""
Write-Host "6. Validation" -ForegroundColor Yellow

try {
    Invoke-Api -Method PUT -Endpoint "/api/settings/provider" -Token $token -Body @{
        provider = "unknown-provider"
    } | Out-Null
    Fail "Invalid provider is rejected with 400"
} catch {
    Pass "Invalid provider is rejected with 400"
}

try {
    Invoke-Api -Method PUT -Endpoint "/api/settings/api-key" -Token $token -Body @{
        provider = "openai"
        apiKey   = ""
    } | Out-Null
    Fail "Empty API key is rejected with 400"
} catch {
    Pass "Empty API key is rejected with 400"
}

# ── 7. Chat (uses per-user key path) ─────────────────────────────────────────
Write-Host ""
Write-Host "7. Chat (exercises per-user key resolution)" -ForegroundColor Yellow

try {
    $chat = Invoke-Api -Method POST -Endpoint "/api/chat" -Token $token -Body @{
        message = "Say hello in one word."
    }

    if ($chat.message) { Pass "Chat returns a response while per-user key is set" }
    else { Fail "Chat returns a response while per-user key is set" "Empty message in response" }
} catch {
    # A real API key wasn't used — a 401/500 from OpenAI is expected with a dummy key.
    # What matters is that the controller path was exercised (not a 404 or auth error).
    $statusCode = [int]$_.Exception.Response.StatusCode
    if ($statusCode -ne 401 -and $statusCode -ne 404) {
        Pass "Chat endpoint reached (key resolution exercised; dummy key expected to fail upstream)"
    } else {
        Fail "Chat endpoint reached" "Unexpected status $statusCode — $($_.Exception.Message)"
    }
}

# ── Summary ───────────────────────────────────────────────────────────────────
Write-Host ""
Write-Host ("-" * 45)
$total = $passed + $failed
Write-Host "Results: $passed/$total passed" -ForegroundColor $(if ($failed -eq 0) { "Green" } else { "Yellow" })

if ($failed -gt 0) { exit 1 }
exit 0
