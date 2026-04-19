param(
    [string]$ApiBaseUrl = "http://localhost:5188",
    [string]$Configuration = "Debug",
    [int]$ApiStartupTimeoutSec = 90,
    [switch]$SkipBuild,
    [switch]$SkipConsole,
    [switch]$TestRateLimit,
    [int]$RateLimitRequests = 105,
    [string]$ArtifactsDir = "test-artifacts"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# Windows PowerShell 5.1 may not auto-load System.Net.Http types.
try {
    Add-Type -AssemblyName System.Net.Http -ErrorAction SilentlyContinue
}
catch {
    # Ignore if already available.
}

$repoRoot = Split-Path -Parent $PSScriptRoot
Set-Location $repoRoot

$artifactsPath = Join-Path $repoRoot $ArtifactsDir
if (-not (Test-Path $artifactsPath)) {
    New-Item -ItemType Directory -Path $artifactsPath | Out-Null
}

$apiOutLog = Join-Path $artifactsPath "api.stdout.log"
$apiErrLog = Join-Path $artifactsPath "api.stderr.log"
$singleConsoleLog = Join-Path $artifactsPath "console.single.log"
$multiConsoleLog = Join-Path $artifactsPath "console.multi.log"
$singleConsoleErrLog = Join-Path $artifactsPath "console.single.err.log"
$multiConsoleErrLog = Join-Path $artifactsPath "console.multi.err.log"

$results = New-Object System.Collections.Generic.List[object]

function Add-TestResult {
    param(
        [string]$Name,
        [string]$Status,
        [string]$Details
    )

    $results.Add([pscustomobject]@{
        Test = $Name
        Status = $Status
        Details = $Details
    })

    $color = switch ($Status) {
        "PASS" { "Green" }
        "WARN" { "Yellow" }
        default { "Red" }
    }

    Write-Host ("[{0}] {1} - {2}" -f $Status, $Name, $Details) -ForegroundColor $color
}

function Invoke-DotNetStep {
    param(
        [string]$Name,
        [string[]]$DotnetArgs
    )

    try {
        Write-Host ("Running: dotnet {0}" -f ($DotnetArgs -join " ")) -ForegroundColor Cyan
        & dotnet @DotnetArgs
        if ($LASTEXITCODE -ne 0) {
            Add-TestResult -Name $Name -Status "FAIL" -Details ("dotnet exited with code {0}" -f $LASTEXITCODE)
            return $false
        }

        Add-TestResult -Name $Name -Status "PASS" -Details "Completed successfully"
        return $true
    }
    catch {
        Add-TestResult -Name $Name -Status "FAIL" -Details $_.Exception.Message
        return $false
    }
}

function New-HttpClient {
    $handler = [System.Net.Http.HttpClientHandler]::new()
    $handler.ServerCertificateCustomValidationCallback = { $true }
    $client = [System.Net.Http.HttpClient]::new($handler)
    $client.Timeout = [TimeSpan]::FromSeconds(45)
    return $client
}

function Invoke-Http {
    param(
        [Parameter(Mandatory = $true)][System.Net.Http.HttpClient]$Client,
        [Parameter(Mandatory = $true)][string]$Method,
        [Parameter(Mandatory = $true)][string]$Url,
        [hashtable]$Headers,
        [object]$Body
    )

    $request = [System.Net.Http.HttpRequestMessage]::new([System.Net.Http.HttpMethod]::new($Method), $Url)

    if ($Headers) {
        foreach ($k in $Headers.Keys) {
            [void]$request.Headers.TryAddWithoutValidation($k, [string]$Headers[$k])
        }
    }

    if ($null -ne $Body) {
        $json = $Body
        if ($Body -isnot [string]) {
            $json = $Body | ConvertTo-Json -Depth 20
        }
        $request.Content = [System.Net.Http.StringContent]::new($json, [System.Text.Encoding]::UTF8, "application/json")
    }

    try {
        $response = $Client.SendAsync($request).GetAwaiter().GetResult()
        $content = $response.Content.ReadAsStringAsync().GetAwaiter().GetResult()

        $parsedJson = $null
        if (-not [string]::IsNullOrWhiteSpace($content)) {
            try {
                if (Get-Command ConvertFrom-Json -ParameterName Depth -ErrorAction SilentlyContinue) {
                    $parsedJson = $content | ConvertFrom-Json -Depth 20
                }
                else {
                    $parsedJson = $content | ConvertFrom-Json
                }
            }
            catch {
                $parsedJson = $null
            }
        }

        return [pscustomobject]@{
            StatusCode = [int]$response.StatusCode
            IsSuccess = $response.IsSuccessStatusCode
            Content = $content
            Json = $parsedJson
        }
    }
    catch {
        return [pscustomobject]@{
            StatusCode = 0
            IsSuccess = $false
            Content = $_.Exception.Message
            Json = $null
        }
    }
    finally {
        $request.Dispose()
    }
}

function Get-JsonPropertyValue {
    param(
        [object]$Object,
        [string[]]$CandidateNames
    )

    if ($null -eq $Object) {
        return $null
    }

    foreach ($name in $CandidateNames) {
        $prop = $Object.PSObject.Properties | Where-Object { $_.Name -eq $name } | Select-Object -First 1
        if ($prop) {
            return $prop.Value
        }
    }

    return $null
}

function Invoke-ConsoleSmoke {
    param(
        [string]$Name,
        [string[]]$CommandArgs,
        [string]$LogPath,
        [string]$ErrorLogPath,
        [string[]]$StartupMarkers,
        [string[]]$NonInteractiveMarkers = @("Cannot read keys when either application does not have a console", "The handle is invalid."),
        [string[]]$FatalMarkers = @("Fatal Error", "Stack Trace", "appsettings.json' was not found"),
        [string]$WorkingDirectory = $repoRoot,
        [int]$StartupWaitSeconds = 8
    )

    try {
        if (Test-Path $LogPath) {
            Remove-Item $LogPath -Force
        }
        if (Test-Path $ErrorLogPath) {
            Remove-Item $ErrorLogPath -Force
        }

        $sanitizedArgs = @($CommandArgs | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
        if ($sanitizedArgs.Count -eq 0) {
            Add-TestResult -Name $Name -Status "FAIL" -Details "Console command arguments were empty"
            return
        }

        $argumentLine = ($sanitizedArgs -join " ")
        $proc = Start-Process -FilePath "dotnet" -ArgumentList $argumentLine -WorkingDirectory $WorkingDirectory -PassThru -NoNewWindow -RedirectStandardOutput $LogPath -RedirectStandardError $ErrorLogPath

        Start-Sleep -Seconds $StartupWaitSeconds

        if (-not $proc.HasExited) {
            Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue
            Start-Sleep -Milliseconds 400
        }

        $output = ""
        if (Test-Path $LogPath) {
            $output = Get-Content -Path $LogPath -Raw
        }

        $errorOutput = ""
        if (Test-Path $ErrorLogPath) {
            $errorOutput = Get-Content -Path $ErrorLogPath -Raw
        }

        $combinedOutput = "$output`n$errorOutput"

        foreach ($niMarker in $NonInteractiveMarkers) {
            if ($combinedOutput -match [regex]::Escape($niMarker)) {
                Add-TestResult -Name $Name -Status "WARN" -Details ("Console app hit non-interactive terminal limitation: {0}" -f $niMarker)
                return
            }
        }

        foreach ($fatal in $FatalMarkers) {
            if ($combinedOutput -match [regex]::Escape($fatal)) {
                Add-TestResult -Name $Name -Status "FAIL" -Details ("Fatal startup marker detected: {0}" -f $fatal)
                return
            }
        }

        $hasMarker = $false
        foreach ($marker in $StartupMarkers) {
            if ($output -match [regex]::Escape($marker)) {
                $hasMarker = $true
                break
            }
        }

        if ($hasMarker) {
            Add-TestResult -Name $Name -Status "PASS" -Details "Startup markers detected"
            return
        }

        if ($proc.HasExited -and $proc.ExitCode -ne 0) {
            Add-TestResult -Name $Name -Status "FAIL" -Details ("Exited with code {0}. See {1}" -f $proc.ExitCode, $LogPath)
            return
        }

        Add-TestResult -Name $Name -Status "WARN" -Details "Could not confirm startup markers"
    }
    catch {
        Add-TestResult -Name $Name -Status "FAIL" -Details $_.Exception.Message
    }
}

function Assert-Status {
    param(
        [string]$Name,
        [object]$Response,
        [int[]]$Expected,
        [switch]$WarningOnly
    )

    if ($Expected -contains $Response.StatusCode) {
        Add-TestResult -Name $Name -Status "PASS" -Details ("HTTP {0}" -f $Response.StatusCode)
        return $true
    }

    $snippet = $Response.Content
    if ($snippet.Length -gt 220) {
        $snippet = $snippet.Substring(0, 220)
    }

    if ($WarningOnly) {
        Add-TestResult -Name $Name -Status "WARN" -Details ("HTTP {0}. Response: {1}" -f $Response.StatusCode, $snippet)
    }
    else {
        Add-TestResult -Name $Name -Status "FAIL" -Details ("Expected [{0}], got {1}. Response: {2}" -f ($Expected -join ","), $Response.StatusCode, $snippet)
    }

    return $false
}

$apiProcess = $null
$client = $null

try {
    Write-Host "=== SkillBot End-to-End Feature Test Harness ===" -ForegroundColor Cyan
    Write-Host ("Repo Root: {0}" -f $repoRoot)
    Write-Host ("API Base URL: {0}" -f $ApiBaseUrl)
    Write-Host ("Artifacts: {0}" -f $artifactsPath)

    if (-not $SkipBuild) {
        $restoreOk = Invoke-DotNetStep -Name "Restore solution" -DotnetArgs @("restore", "SkillBot.slnx")
        if (-not $restoreOk) { throw "Stopping because restore failed." }

        $buildOk = Invoke-DotNetStep -Name "Build solution" -DotnetArgs @("build", "SkillBot.slnx", "-c", $Configuration, "--no-restore")
        if (-not $buildOk) { throw "Stopping because build failed." }
    }
    else {
        Add-TestResult -Name "Build step" -Status "WARN" -Details "Skipped via -SkipBuild"
    }

    Write-Host "Starting API project..." -ForegroundColor Cyan
    if (Test-Path $apiOutLog) { Remove-Item $apiOutLog -Force }
    if (Test-Path $apiErrLog) { Remove-Item $apiErrLog -Force }

    $apiArgs = @(
        "run",
        "--project", "SkillBot.Api/SkillBot.Api.csproj",
        "--configuration", $Configuration,
        "--urls", $ApiBaseUrl,
        "--no-build"
    )

    $apiProcess = Start-Process -FilePath "dotnet" -ArgumentList $apiArgs -WorkingDirectory $repoRoot -PassThru -NoNewWindow -RedirectStandardOutput $apiOutLog -RedirectStandardError $apiErrLog
    Add-TestResult -Name "Start API" -Status "PASS" -Details ("Started process PID {0}" -f $apiProcess.Id)

    $client = New-HttpClient

    $healthUrl = "$ApiBaseUrl/health"
    $healthy = $false
    $deadline = (Get-Date).AddSeconds($ApiStartupTimeoutSec)
    while ((Get-Date) -lt $deadline) {
        Start-Sleep -Milliseconds 750

        if ($apiProcess.HasExited) {
            throw "API process exited early with code $($apiProcess.ExitCode). Check $apiErrLog"
        }

        $health = Invoke-Http -Client $client -Method "GET" -Url $healthUrl
        if ($health.StatusCode -eq 200) {
            $healthy = $true
            break
        }
    }

    if (-not $healthy) {
        throw "API did not become healthy within $ApiStartupTimeoutSec seconds."
    }

    Add-TestResult -Name "API health during startup" -Status "PASS" -Details "Health endpoint returned 200"

    # Baseline API checks
    $swagger = Invoke-Http -Client $client -Method "GET" -Url "$ApiBaseUrl/swagger/v1/swagger.json"
    [void](Assert-Status -Name "Swagger JSON" -Response $swagger -Expected @(200))

    $plugins = Invoke-Http -Client $client -Method "GET" -Url "$ApiBaseUrl/api/plugins"
    [void](Assert-Status -Name "Get plugins" -Response $plugins -Expected @(200))

    $pluginName = "Calculator"
    $pluginsArray = @($plugins.Json)
    if ($plugins.StatusCode -eq 200 -and $plugins.Json -and $pluginsArray.Count -gt 0) {
        $firstPluginName = Get-JsonPropertyValue -Object $pluginsArray[0] -CandidateNames @("name", "Name")
        if ($firstPluginName) {
            $pluginName = [string]$firstPluginName
        }
        Add-TestResult -Name "Plugin count" -Status "PASS" -Details ("Found {0} plugin(s)" -f $pluginsArray.Count)
    }
    else {
        Add-TestResult -Name "Plugin count" -Status "WARN" -Details "Could not determine plugin count from response"
    }

    $pluginInfo = Invoke-Http -Client $client -Method "GET" -Url "$ApiBaseUrl/api/plugins/$pluginName"
    [void](Assert-Status -Name "Get plugin by name" -Response $pluginInfo -Expected @(200))

    $agents = Invoke-Http -Client $client -Method "GET" -Url "$ApiBaseUrl/api/multi-agent/agents"
    [void](Assert-Status -Name "Get multi-agent list" -Response $agents -Expected @(200))

    # Auth flow
    $email = "skillbot-test-{0}@example.com" -f ([Guid]::NewGuid().ToString("N").Substring(0, 10))
    $password = "P@ssw0rd!123"
    $username = "tester"

    $registerBody = @{
        email = $email
        password = $password
        username = $username
    }

    $register = Invoke-Http -Client $client -Method "POST" -Url "$ApiBaseUrl/api/auth/register" -Body $registerBody
    [void](Assert-Status -Name "Auth register" -Response $register -Expected @(200))

    $registerDuplicate = Invoke-Http -Client $client -Method "POST" -Url "$ApiBaseUrl/api/auth/register" -Body $registerBody
    [void](Assert-Status -Name "Auth duplicate register blocked" -Response $registerDuplicate -Expected @(400))

    $login = Invoke-Http -Client $client -Method "POST" -Url "$ApiBaseUrl/api/auth/login" -Body @{ email = $email; password = $password }
    [void](Assert-Status -Name "Auth login" -Response $login -Expected @(200))

    $loginBad = Invoke-Http -Client $client -Method "POST" -Url "$ApiBaseUrl/api/auth/login" -Body @{ email = $email; password = "wrong-password" }
    [void](Assert-Status -Name "Auth wrong password blocked" -Response $loginBad -Expected @(400))

    $token = $null
    $loginToken = Get-JsonPropertyValue -Object $login.Json -CandidateNames @("token", "Token")
    if ($login.StatusCode -eq 200 -and $loginToken) {
        $token = [string]$loginToken
        Add-TestResult -Name "JWT token issued" -Status "PASS" -Details "Token received"
    }
    else {
        Add-TestResult -Name "JWT token issued" -Status "FAIL" -Details "No token in login response"
    }

    # Chat + security checks
    $chatNoAuth = Invoke-Http -Client $client -Method "POST" -Url "$ApiBaseUrl/api/chat" -Body @{ message = "hello" }
    [void](Assert-Status -Name "Chat requires auth" -Response $chatNoAuth -Expected @(401, 403))

    if ($token) {
        $authHeaders = @{ Authorization = "Bearer $token" }

        $sqlPayload = Invoke-Http -Client $client -Method "POST" -Url "$ApiBaseUrl/api/chat" -Headers $authHeaders -Body @{ message = "SELECT * FROM users" }
        [void](Assert-Status -Name "Input validation blocks SQL patterns" -Response $sqlPayload -Expected @(400))

        $piiPayload = Invoke-Http -Client $client -Method "POST" -Url "$ApiBaseUrl/api/chat" -Headers $authHeaders -Body @{ message = "my email is test@example.com" }
        [void](Assert-Status -Name "Content safety blocks PII" -Response $piiPayload -Expected @(400))

        $toxicPayload = Invoke-Http -Client $client -Method "POST" -Url "$ApiBaseUrl/api/chat" -Headers $authHeaders -Body @{ message = "I want to kill this task" }
        [void](Assert-Status -Name "Content safety blocks toxic text" -Response $toxicPayload -Expected @(400))

        $missingConversation = Invoke-Http -Client $client -Method "GET" -Url "$ApiBaseUrl/api/chat/nonexistent-conversation" -Headers $authHeaders
        [void](Assert-Status -Name "Get missing conversation" -Response $missingConversation -Expected @(404))

        $deleteMissingConversation = Invoke-Http -Client $client -Method "DELETE" -Url "$ApiBaseUrl/api/chat/nonexistent-conversation" -Headers $authHeaders
        [void](Assert-Status -Name "Delete missing conversation" -Response $deleteMissingConversation -Expected @(404))

        $normalChat = Invoke-Http -Client $client -Method "POST" -Url "$ApiBaseUrl/api/chat" -Headers $authHeaders -Body @{ message = "What is 2 + 2?" }
        [void](Assert-Status -Name "Authorized chat (LLM path)" -Response $normalChat -Expected @(200) -WarningOnly)
    }

    # Cache endpoints
    $cacheStats = Invoke-Http -Client $client -Method "GET" -Url "$ApiBaseUrl/api/cache/stats"
    [void](Assert-Status -Name "Cache stats" -Response $cacheStats -Expected @(200))

    $cacheHealth = Invoke-Http -Client $client -Method "GET" -Url "$ApiBaseUrl/api/cache/health"
    [void](Assert-Status -Name "Cache health" -Response $cacheHealth -Expected @(200))

    $cacheInvalidate = Invoke-Http -Client $client -Method "DELETE" -Url "$ApiBaseUrl/api/cache/invalidate/llm_response_*"
    [void](Assert-Status -Name "Cache invalidate by pattern" -Response $cacheInvalidate -Expected @(204))

    $cacheClear = Invoke-Http -Client $client -Method "DELETE" -Url "$ApiBaseUrl/api/cache"
    [void](Assert-Status -Name "Cache clear" -Response $cacheClear -Expected @(204))

    # Usage endpoints
    $usageStats = Invoke-Http -Client $client -Method "GET" -Url "$ApiBaseUrl/api/usage/stats"
    [void](Assert-Status -Name "Usage stats" -Response $usageStats -Expected @(200))

    $topConversations = Invoke-Http -Client $client -Method "GET" -Url "$ApiBaseUrl/api/usage/top-conversations?limit=5"
    [void](Assert-Status -Name "Usage top conversations" -Response $topConversations -Expected @(200))

    $usageByConversation = Invoke-Http -Client $client -Method "GET" -Url "$ApiBaseUrl/api/usage/stats/nonexistent-conversation"
    [void](Assert-Status -Name "Usage stats by conversation" -Response $usageByConversation -Expected @(200))

    $usageReset = Invoke-Http -Client $client -Method "DELETE" -Url "$ApiBaseUrl/api/usage/stats"
    [void](Assert-Status -Name "Usage reset" -Response $usageReset -Expected @(204))

    # Task endpoints
    $pastSchedule = Invoke-Http -Client $client -Method "POST" -Url "$ApiBaseUrl/api/tasks/schedule" -Body @{
        task = "past task should fail"
        executeAt = (Get-Date).ToUniversalTime().AddMinutes(-1).ToString("o")
        isMultiAgent = $false
    }
    [void](Assert-Status -Name "Task schedule rejects past time" -Response $pastSchedule -Expected @(400))

    $futureTime = (Get-Date).ToUniversalTime().AddMinutes(2).ToString("o")
    $schedule = Invoke-Http -Client $client -Method "POST" -Url "$ApiBaseUrl/api/tasks/schedule" -Body @{
        task = "quick test task"
        executeAt = $futureTime
        isMultiAgent = $false
    }
    [void](Assert-Status -Name "Task schedule valid" -Response $schedule -Expected @(200))

    $scheduledTaskId = $null
    $scheduledTaskIdValue = Get-JsonPropertyValue -Object $schedule.Json -CandidateNames @("taskId", "TaskId")
    if ($schedule.StatusCode -eq 200 -and $scheduledTaskIdValue) {
        $scheduledTaskId = [string]$scheduledTaskIdValue

        $getTask = Invoke-Http -Client $client -Method "GET" -Url "$ApiBaseUrl/api/tasks/$scheduledTaskId"
        [void](Assert-Status -Name "Task get by id" -Response $getTask -Expected @(200))

        $cancelTask = Invoke-Http -Client $client -Method "DELETE" -Url "$ApiBaseUrl/api/tasks/$scheduledTaskId"
        [void](Assert-Status -Name "Task cancel" -Response $cancelTask -Expected @(204))

        $getCancelledTask = Invoke-Http -Client $client -Method "GET" -Url "$ApiBaseUrl/api/tasks/$scheduledTaskId"
        [void](Assert-Status -Name "Task state after cancel" -Response $getCancelledTask -Expected @(200, 404))
    }
    else {
        Add-TestResult -Name "Task id extraction" -Status "WARN" -Details "Could not extract scheduled task id"
    }

    $allTasks = Invoke-Http -Client $client -Method "GET" -Url "$ApiBaseUrl/api/tasks"
    [void](Assert-Status -Name "Task list" -Response $allTasks -Expected @(200))

    $recurringBad = Invoke-Http -Client $client -Method "POST" -Url "$ApiBaseUrl/api/tasks/recurring" -Body @{
        task = "recurring task"
        cronExpression = ""
        isMultiAgent = $false
    }
    [void](Assert-Status -Name "Recurring task rejects empty cron" -Response $recurringBad -Expected @(400))

    $recurringGood = Invoke-Http -Client $client -Method "POST" -Url "$ApiBaseUrl/api/tasks/recurring" -Body @{
        task = "recurring test"
        cronExpression = "*/5 * * * *"
        isMultiAgent = $false
    }
    [void](Assert-Status -Name "Recurring task schedule valid" -Response $recurringGood -Expected @(200))

    $recurringTaskIdValue = Get-JsonPropertyValue -Object $recurringGood.Json -CandidateNames @("taskId", "TaskId")
    if ($recurringGood.StatusCode -eq 200 -and $recurringTaskIdValue) {
        $recurringTaskId = [string]$recurringTaskIdValue
        $cancelRecurring = Invoke-Http -Client $client -Method "DELETE" -Url "$ApiBaseUrl/api/tasks/$recurringTaskId"
        [void](Assert-Status -Name "Recurring task cancel" -Response $cancelRecurring -Expected @(204))
    }

    # Multi-agent chat smoke (may require external API key)
    $multiChat = Invoke-Http -Client $client -Method "POST" -Url "$ApiBaseUrl/api/multi-agent/chat" -Body @{ task = "Summarize why automated tests are useful in one sentence." }
    [void](Assert-Status -Name "Multi-agent chat (LLM path)" -Response $multiChat -Expected @(200) -WarningOnly)

    # Optional explicit rate-limit test. Keep it at the end because it can block further requests.
    if ($TestRateLimit) {
        $rateLimited = $false
        for ($i = 1; $i -le $RateLimitRequests; $i++) {
            $rl = Invoke-Http -Client $client -Method "GET" -Url "$ApiBaseUrl/health"
            if ($rl.StatusCode -eq 429) {
                $rateLimited = $true
                break
            }
        }

        if ($rateLimited) {
            Add-TestResult -Name "Rate limiter returns 429" -Status "PASS" -Details ("429 observed within {0} requests" -f $RateLimitRequests)
        }
        else {
            Add-TestResult -Name "Rate limiter returns 429" -Status "FAIL" -Details ("No 429 after {0} requests" -f $RateLimitRequests)
        }
    }
    else {
        Add-TestResult -Name "Rate limiter stress test" -Status "WARN" -Details "Skipped (use -TestRateLimit to enable)"
    }

    # Console tests
    if (-not $SkipConsole) {
        Invoke-ConsoleSmoke -Name "Console single-agent smoke" -CommandArgs @(
            "run",
            "--project", "SkillBot.Console.csproj",
            "--configuration", $Configuration,
            "--no-build"
        ) -WorkingDirectory (Join-Path $repoRoot "SkillBot.Console") -LogPath $singleConsoleLog -ErrorLogPath $singleConsoleErrLog -StartupMarkers @(
            "Starting SkillBot",
            "SkillBot initialized successfully",
            "Type your message"
        )

        Invoke-ConsoleSmoke -Name "Console multi-agent smoke" -CommandArgs @(
            "run",
            "--project", "SkillBot.Console.csproj",
            "--configuration", $Configuration,
            "--no-build",
            "--",
            "--multi-agent"
        ) -WorkingDirectory (Join-Path $repoRoot "SkillBot.Console") -LogPath $multiConsoleLog -ErrorLogPath $multiConsoleErrLog -StartupMarkers @(
            "Multi-Agent Mode",
            "Multi-Agent System Active",
            "Type your message"
        )
    }
    else {
        Add-TestResult -Name "Console tests" -Status "WARN" -Details "Skipped via -SkipConsole"
    }
}
catch {
    Add-TestResult -Name "Harness runtime" -Status "FAIL" -Details $_.Exception.Message
}
finally {
    if ($client) {
        $client.Dispose()
    }

    if ($apiProcess -and -not $apiProcess.HasExited) {
        Write-Host "Stopping API process..." -ForegroundColor Cyan
        Stop-Process -Id $apiProcess.Id -Force -ErrorAction SilentlyContinue
    }
}

Write-Host ""
Write-Host "=== Test Summary ===" -ForegroundColor Cyan
$results | Format-Table -AutoSize

$passCount = @($results | Where-Object { $_.Status -eq "PASS" }).Count
$warnCount = @($results | Where-Object { $_.Status -eq "WARN" }).Count
$failCount = @($results | Where-Object { $_.Status -eq "FAIL" }).Count

Write-Host ""
Write-Host ("PASS: {0}  WARN: {1}  FAIL: {2}" -f $passCount, $warnCount, $failCount)
Write-Host ("API logs: {0}" -f $apiOutLog)
Write-Host ("API errors: {0}" -f $apiErrLog)
if (-not $SkipConsole) {
    Write-Host ("Console single-agent log: {0}" -f $singleConsoleLog)
    Write-Host ("Console single-agent error log: {0}" -f $singleConsoleErrLog)
    Write-Host ("Console multi-agent log: {0}" -f $multiConsoleLog)
    Write-Host ("Console multi-agent error log: {0}" -f $multiConsoleErrLog)
}

if ($failCount -gt 0) {
    exit 1
}

exit 0
