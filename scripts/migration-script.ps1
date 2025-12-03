param(
    [Parameter(Mandatory=$true)]
    [ValidateSet('add', 'update', 'remove', 'list')]
    [string]$Operation,

    [Parameter(Mandatory=$false)]
    [string]$MigrationName
)

# Get project root (one level up from scripts folder)
$projectPath = Split-Path -Parent $PSScriptRoot
Set-Location $projectPath

# Create build-logs directory if it doesn't exist
if (-not (Test-Path 'build-logs')) {
    New-Item -ItemType Directory -Path 'build-logs' | Out-Null
}

# Generate timestamp
$timestamp = Get-Date -Format 'yyyyMMdd-HHmmss'

switch ($Operation) {
    'add' {
        if (-not $MigrationName) {
            Write-Host "Error: MigrationName is required for 'add' operation" -ForegroundColor Red
            exit 1
        }

        $logfileTemp = "build-logs/${timestamp}-migration-add-temp.log"

        Write-Host "Adding migration: $MigrationName..." -ForegroundColor Cyan
        dotnet ef migrations add $MigrationName --project PersonalFinancesApp *> $logfileTemp

        $success = $LASTEXITCODE -eq 0

        if ($success) {
            $logfileFinal = "build-logs/${timestamp}-migration-add-success.log"
            Rename-Item -Path $logfileTemp -NewName "${timestamp}-migration-add-success.log"

            Write-Host "✓ MIGRATION ADDED SUCCESSFULLY" -ForegroundColor Green
            Write-Host "Migration: $MigrationName"
            Write-Host "Log: $logfileFinal"
            exit 0
        } else {
            $logfileFinal = "build-logs/${timestamp}-migration-add-failed.log"
            Rename-Item -Path $logfileTemp -NewName "${timestamp}-migration-add-failed.log"

            $content = Get-Content $logfileFinal -Raw
            $errorLine = ($content -split "`n") | Select-String -Pattern "error|fail" -CaseSensitive:$false | Select-Object -First 1

            Write-Host "✗ MIGRATION ADD FAILED" -ForegroundColor Red
            if ($errorLine) {
                Write-Host $errorLine.Line -ForegroundColor Yellow
            }
            Write-Host "Log: $logfileFinal"
            exit 1
        }
    }

    'update' {
        $logfileTemp = "build-logs/${timestamp}-migration-update-temp.log"

        Write-Host "Updating database..." -ForegroundColor Cyan

        if ($MigrationName) {
            # Update to specific migration
            dotnet ef database update $MigrationName --project PersonalFinancesApp *> $logfileTemp
        } else {
            # Update to latest
            dotnet ef database update --project PersonalFinancesApp *> $logfileTemp
        }

        $success = $LASTEXITCODE -eq 0

        if ($success) {
            $logfileFinal = "build-logs/${timestamp}-migration-update-success.log"
            Rename-Item -Path $logfileTemp -NewName "${timestamp}-migration-update-success.log"

            Write-Host "✓ DATABASE UPDATED SUCCESSFULLY" -ForegroundColor Green
            if ($MigrationName) {
                Write-Host "Target Migration: $MigrationName"
            } else {
                Write-Host "Updated to: Latest migration"
            }
            Write-Host "Log: $logfileFinal"
            exit 0
        } else {
            $logfileFinal = "build-logs/${timestamp}-migration-update-failed.log"
            Rename-Item -Path $logfileTemp -NewName "${timestamp}-migration-update-failed.log"

            $content = Get-Content $logfileFinal -Raw
            $errorLine = ($content -split "`n") | Select-String -Pattern "error|fail" -CaseSensitive:$false | Select-Object -First 1

            Write-Host "✗ DATABASE UPDATE FAILED" -ForegroundColor Red
            if ($errorLine) {
                Write-Host $errorLine.Line -ForegroundColor Yellow
            }
            Write-Host "Log: $logfileFinal"
            exit 1
        }
    }

    'remove' {
        $logfileTemp = "build-logs/${timestamp}-migration-remove-temp.log"

        Write-Host "Removing last migration..." -ForegroundColor Cyan
        dotnet ef migrations remove --project PersonalFinancesApp --force *> $logfileTemp

        $success = $LASTEXITCODE -eq 0

        if ($success) {
            $logfileFinal = "build-logs/${timestamp}-migration-remove-success.log"
            Rename-Item -Path $logfileTemp -NewName "${timestamp}-migration-remove-success.log"

            Write-Host "✓ MIGRATION REMOVED SUCCESSFULLY" -ForegroundColor Green
            Write-Host "Log: $logfileFinal"
            exit 0
        } else {
            $logfileFinal = "build-logs/${timestamp}-migration-remove-failed.log"
            Rename-Item -Path $logfileTemp -NewName "${timestamp}-migration-remove-failed.log"

            $content = Get-Content $logfileFinal -Raw
            $errorLine = ($content -split "`n") | Select-String -Pattern "error|fail" -CaseSensitive:$false | Select-Object -First 1

            Write-Host "✗ MIGRATION REMOVE FAILED" -ForegroundColor Red
            if ($errorLine) {
                Write-Host $errorLine.Line -ForegroundColor Yellow
            }
            Write-Host "Log: $logfileFinal"
            exit 1
        }
    }

    'list' {
        Write-Host "Listing migrations..." -ForegroundColor Cyan
        dotnet ef migrations list --project PersonalFinancesApp
        exit $LASTEXITCODE
    }
}
