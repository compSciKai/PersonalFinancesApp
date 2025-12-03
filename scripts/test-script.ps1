# Get project root (one level up from scripts folder)
$projectPath = Split-Path -Parent $PSScriptRoot
Set-Location $projectPath

# Create build-logs directory if it doesn't exist
if (-not (Test-Path 'build-logs')) {
    New-Item -ItemType Directory -Path 'build-logs' | Out-Null
}

# Generate timestamp
$timestamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$logfileTemp = "build-logs/${timestamp}-test-temp.log"

# Run tests and capture output
Write-Host "Running tests for PersonalFinancesAppTests..." -ForegroundColor Cyan
dotnet test PersonalFinancesAppTests/PersonalFinancesAppTests.csproj --verbosity normal *> $logfileTemp

# Check if tests succeeded
$testSuccess = $LASTEXITCODE -eq 0

# Rename log file based on status
if ($testSuccess) {
    $logfileFinal = "build-logs/${timestamp}-test-success.log"
    Rename-Item -Path $logfileTemp -NewName "${timestamp}-test-success.log"

    # Parse success log for summary
    $content = Get-Content $logfileFinal -Raw

    # Extract test statistics
    $lines = $content -split "`n"

    # Parse test results (e.g., "Passed! - Failed: 0, Passed: 45, Skipped: 0, Total: 45")
    $resultLine = $lines | Select-String -Pattern "Failed:\s*(\d+),\s*Passed:\s*(\d+),\s*Skipped:\s*(\d+),\s*Total:\s*(\d+)" | Select-Object -First 1

    if ($resultLine) {
        $failed = $resultLine.Matches.Groups[1].Value
        $passed = $resultLine.Matches.Groups[2].Value
        $skipped = $resultLine.Matches.Groups[3].Value
        $total = $resultLine.Matches.Groups[4].Value
    } else {
        # Fallback parsing
        $passed = "unknown"
        $total = "unknown"
        $skipped = "0"
    }

    # Extract duration
    $durationMatch = $lines | Select-String -Pattern "Total time:\s*(\d+\.?\d*)" | Select-Object -First 1
    if ($durationMatch) {
        $duration = $durationMatch.Matches.Groups[1].Value
    } else {
        $duration = "unknown"
    }

    Write-Host "TESTS PASSED" -ForegroundColor Green
    Write-Host "Total: $total tests"
    Write-Host "Passed: $passed"
    if ($skipped -ne "0") {
        Write-Host "Skipped: $skipped"
    }
    Write-Host "Duration: ${duration}s"
    Write-Host "Log: $logfileFinal"

    exit 0
} else {
    $logfileFinal = "build-logs/${timestamp}-test-failed.log"
    Rename-Item -Path $logfileTemp -NewName "${timestamp}-test-failed.log"

    # Parse failed test details
    $content = Get-Content $logfileFinal -Raw
    $lines = $content -split "`n"

    # Extract test counts
    $resultLine = $lines | Select-String -Pattern "Failed:\s*(\d+),\s*Passed:\s*(\d+),\s*Skipped:\s*(\d+),\s*Total:\s*(\d+)" | Select-Object -First 1

    if ($resultLine) {
        $failed = $resultLine.Matches.Groups[1].Value
        $passed = $resultLine.Matches.Groups[2].Value
        $total = $resultLine.Matches.Groups[4].Value
    } else {
        $failed = "unknown"
        $total = "unknown"
    }

    Write-Host "TESTS FAILED ($failed/$total)" -ForegroundColor Red

    # Find failed test names
    $failedTests = $lines | Select-String -Pattern "Failed\s+(.+)\[" | Select-Object -First 3

    if ($failedTests) {
        Write-Host ""
        Write-Host "Failed Tests:" -ForegroundColor Yellow
        $count = 1
        foreach ($test in $failedTests) {
            $testName = $test.Matches.Groups[1].Value.Trim()
            Write-Host "$count. $testName"

            # Try to find assertion message
            $testIndex = $lines.IndexOf($test.Line)
            if ($testIndex -ge 0 -and $testIndex + 5 -lt $lines.Count) {
                $assertionLine = $lines[($testIndex+1)..($testIndex+5)] | Select-String -Pattern "Expected:|Assert\.|Message:" | Select-Object -First 1
                if ($assertionLine) {
                    $assertionText = $assertionLine.Line.Trim()
                    Write-Host "   $assertionText" -ForegroundColor DarkGray
                }
            }
            $count++
        }
    }

    Write-Host ""
    Write-Host "Log: $logfileFinal" -ForegroundColor Cyan

    exit 1
}
