$p = Start-Process "C:\CloventClient104\Clovent.Desktop.exe" -PassThru
Write-Host "Started PID: $($p.Id)"

# Wait for wizard to appear
& "Tools\ScreenCaptureUtil\bin\Release\net10.0-windows\ScreenCaptureUtil.exe" wait-win "First-Run" 15

# Step 1 -> Step 2
Write-Host "Navigating to Step 2..."
& "Tools\ScreenCaptureUtil\bin\Release\net10.0-windows\ScreenCaptureUtil.exe" click-btn "First-Run" "Next >"
Start-Sleep -Seconds 1

# Test Connection (Windows Auth)
Write-Host "Testing connection on Step 2..."
& "Tools\ScreenCaptureUtil\bin\Release\net10.0-windows\ScreenCaptureUtil.exe" click-btn "First-Run" "Test Connection"
Start-Sleep -Seconds 3

# Step 2 -> Step 3
Write-Host "Navigating to Step 3..."
& "Tools\ScreenCaptureUtil\bin\Release\net10.0-windows\ScreenCaptureUtil.exe" click-btn "First-Run" "Next >"
Start-Sleep -Seconds 2

# Capture initial Step 3 screen
& "Tools\ScreenCaptureUtil\bin\Release\net10.0-windows\ScreenCaptureUtil.exe" title "First-Run" "qa\acceptance_test_103\03_step3_schema_initialization.png"
Write-Host "Captured 03_step3_schema_initialization.png"

# Click Apply Database Migrations
Write-Host "Clicking Apply Database Migrations..."
& "Tools\ScreenCaptureUtil\bin\Release\net10.0-windows\ScreenCaptureUtil.exe" click-btn "First-Run" "Apply Database Migrations"

# Wait for migrations to finish (poll up to 90s for completion)
$sw = [System.Diagnostics.Stopwatch]::StartNew()
$migrated = $false
while ($sw.Elapsed.TotalSeconds -lt 90) {
    Start-Sleep -Seconds 3
    Write-Host "Waiting for migrations... ($([int]$sw.Elapsed.TotalSeconds)s)"
    
    $dumpOutput = & "Tools\ScreenCaptureUtil\bin\Release\net10.0-windows\ScreenCaptureUtil.exe" dump "First-Run" 2>&1 | Out-String
    if ($dumpOutput -match "migrated successfully" -or $dumpOutput -match "COMPLETED:") {
        $migrated = $true
        Write-Host "Migration completed successfully!"
        break
    }
}

Start-Sleep -Seconds 2
& "Tools\ScreenCaptureUtil\bin\Release\net10.0-windows\ScreenCaptureUtil.exe" title "First-Run" "qa\acceptance_test_103\03b_schema_migrations_complete.png"
Write-Host "Captured 03b_schema_migrations_complete.png"

Stop-Process -Id $p.Id -Force -ErrorAction SilentlyContinue
Write-Host "Finished Step 3 test."
