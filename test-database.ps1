Write-Host "Testing Database Persistence..." -ForegroundColor Cyan

$baseUrl = "http://localhost:5188"

# Test 1: Register user
Write-Host "`n1. Registering user..." -ForegroundColor Yellow
$registerBody = @{
    email = "dbtest@example.com"
    password = "Test123456"
    username = "dbtestuser"
} | ConvertTo-Json

try {
    $auth = Invoke-RestMethod -Uri "$baseUrl/api/auth/register" -Method Post -ContentType "application/json" -Body $registerBody
    Write-Host "✅ User registered" -ForegroundColor Green
    $token = $auth.token
} catch {
    Write-Host "⚠️ User might already exist, trying login..." -ForegroundColor Yellow
    $loginBody = @{
        email = "dbtest@example.com"
        password = "Test123456"
    } | ConvertTo-Json
    $auth = Invoke-RestMethod -Uri "$baseUrl/api/auth/login" -Method Post -ContentType "application/json" -Body $loginBody
    $token = $auth.token
}

$headers = @{
    "Authorization" = "Bearer $token"
    "Content-Type" = "application/json"
}

# Test 2: Send chat messages
Write-Host "`n2. Sending chat messages..." -ForegroundColor Yellow
$chatBody1 = '{"message": "What is 2+2?"}'
$response1 = Invoke-RestMethod -Uri "$baseUrl/api/chat" -Method Post -Headers $headers -Body $chatBody1
Write-Host "✅ Message 1 sent" -ForegroundColor Green

Start-Sleep -Seconds 2

$chatBody2 = '{"message": "What is the capital of France?"}'
$response2 = Invoke-RestMethod -Uri "$baseUrl/api/chat" -Method Post -Headers $headers -Body $chatBody2
Write-Host "✅ Message 2 sent" -ForegroundColor Green

# Test 3: Retrieve history
Write-Host "`n3. Retrieving conversation history..." -ForegroundColor Yellow
$history = Invoke-RestMethod -Uri "$baseUrl/api/chat/history?limit=10" -Headers $headers

Write-Host "✅ Found $($history.Count) conversations in history" -ForegroundColor Green

if ($history.Count -ge 2) {
    Write-Host "`n📝 Recent conversations:" -ForegroundColor Cyan
    foreach ($conv in $history | Select-Object -First 5) {
        Write-Host "  Q: $($conv.message)" -ForegroundColor White
        Write-Host "  A: $($conv.response.Substring(0, [Math]::Min(50, $conv.response.Length)))..." -ForegroundColor Gray
        Write-Host ""
    }
}

# Test 4: Check database file
Write-Host "`n4. Checking database file..." -ForegroundColor Yellow
$dbPath = "SkillBot.Api/skillbot.db"
if (Test-Path $dbPath) {
    $dbSize = (Get-Item $dbPath).Length
    Write-Host "✅ Database file exists ($dbSize bytes)" -ForegroundColor Green
} else {
    Write-Host "❌ Database file not found!" -ForegroundColor Red
}

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "Database Persistence Tests Complete!" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "`n💡 Restart the API and login again to verify users persist!" -ForegroundColor Yellow