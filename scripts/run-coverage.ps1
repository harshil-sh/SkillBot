# Run tests with coverage and generate HTML report
$rootDir = Split-Path $PSScriptRoot -Parent
$coverageDir = Join-Path $rootDir "coverage"

# Clean previous coverage
if (Test-Path $coverageDir) { Remove-Item $coverageDir -Recurse -Force }
New-Item -ItemType Directory -Path $coverageDir | Out-Null

# Run tests with coverage
dotnet test $rootDir\SkillBot.slnx `
    --collect:"XPlat Code Coverage" `
    --results-directory $coverageDir `
    --no-build

# Check if reportgenerator is installed
$rg = Get-Command reportgenerator -ErrorAction SilentlyContinue
if ($rg) {
    reportgenerator `
        -reports:"$coverageDir\**\coverage.cobertura.xml" `
        -targetdir:"$coverageDir\report" `
        -reporttypes:"Html;TextSummary"
    
    Write-Host "Coverage report generated at: $coverageDir\report\index.html"
    Start-Process "$coverageDir\report\index.html"
} else {
    Write-Host "Install reportgenerator with: dotnet tool install --global dotnet-reportgenerator-globaltool"
    Write-Host "Then re-run this script"
}
