# Get project root (one level up from scripts folder)
$projectPath = Split-Path -Parent $PSScriptRoot
Set-Location $projectPath

# Create build-logs directory if it doesn't exist
if (-not (Test-Path 'build-logs')) {
    New-Item -ItemType Directory -Path 'build-logs' | Out-Null
}

# Generate timestamp
$timestamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$logfileTemp = "build-logs/${timestamp}-build-temp.log"

# Run build and capture output
Write-Host "Building PersonalFinancesApp.sln..." -ForegroundColor Cyan
dotnet build PersonalFinancesApp.sln *> $logfileTemp

# Check if build succeeded
$buildSuccess = $LASTEXITCODE -eq 0

# Rename log file based on status
if ($buildSuccess) {
    $logfileFinal = "build-logs/${timestamp}-build-success.log"
    Rename-Item -Path $logfileTemp -NewName "${timestamp}-build-success.log"

    # Parse success log for summary
    $content = Get-Content $logfileFinal -Raw

    # Extract key info
    $lines = $content -split "`n"
    $buildTimeMatch = $lines | Select-String -Pattern "(\d+\.?\d*) sec" | Select-Object -First 1
    if ($buildTimeMatch) {
        $buildTime = $buildTimeMatch.Matches.Groups[1].Value
    } else {
        $buildTime = "unknown"
    }

    # Count warnings
    $warningCount = ($lines | Select-String -Pattern "warning CS").Count

    Write-Host "BUILD SUCCESSFUL" -ForegroundColor Green
    Write-Host "Duration: $buildTime`s"
    Write-Host "Warnings: $warningCount"
    Write-Host "Log: build-logs/${timestamp}-build-success.log"

    exit 0
} else {
    $logfileFinal = "build-logs/${timestamp}-build-failed.log"
    Rename-Item -Path $logfileTemp -NewName "${timestamp}-build-failed.log"

    # Parse error details
    $content = Get-Content $logfileFinal -Raw
    $lines = $content -split "`n"

    # Find first error
    $errorLine = $lines | Select-String -Pattern "error CS" | Select-Object -First 1

    if ($errorLine) {
        Write-Host "BUILD FAILED" -ForegroundColor Red
        Write-Host $errorLine.Line
        Write-Host "Log: build-logs/${timestamp}-build-failed.log"
    } else {
        Write-Host "BUILD FAILED (details in log)" -ForegroundColor Red
        Write-Host "Log: build-logs/${timestamp}-build-failed.log"
    }

    exit 1
}
