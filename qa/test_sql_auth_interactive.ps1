$p = Start-Process "C:\CloventClient103\Clovent.Desktop.exe" -PassThru
Write-Host "Started PID: $($p.Id)"

# Wait for window to appear
& "Tools\ScreenCaptureUtil\bin\Release\net10.0-windows\ScreenCaptureUtil.exe" wait-win "First-Run" 15

# Go to Step 2
& "Tools\ScreenCaptureUtil\bin\Release\net10.0-windows\ScreenCaptureUtil.exe" click-btn "First-Run" "Next >"
Start-Sleep -Milliseconds 1500

# Open combo dropdown
& "Tools\ScreenCaptureUtil\bin\Release\net10.0-windows\ScreenCaptureUtil.exe" click-name "First-Run" "Open"
Start-Sleep -Milliseconds 600

# Click SQL Server Authentication list item
& "Tools\ScreenCaptureUtil\bin\Release\net10.0-windows\ScreenCaptureUtil.exe" click-name "First-Run" "SQL Server Authentication (Encrypted Credentials)"
Start-Sleep -Milliseconds 600

# Set Username and Password
& "Tools\ScreenCaptureUtil\bin\Release\net10.0-windows\ScreenCaptureUtil.exe" set-text "First-Run" "SQL Username:" "cbos_test_login"
Start-Sleep -Milliseconds 400
& "Tools\ScreenCaptureUtil\bin\Release\net10.0-windows\ScreenCaptureUtil.exe" set-text "First-Run" "SQL Password:" "CbosTest2026!#"
Start-Sleep -Milliseconds 400

# Test Connection with SQL Auth
& "Tools\ScreenCaptureUtil\bin\Release\net10.0-windows\ScreenCaptureUtil.exe" click-btn "First-Run" "Test Connection"
Start-Sleep -Seconds 3

# Capture SQL Auth test
& "Tools\ScreenCaptureUtil\bin\Release\net10.0-windows\ScreenCaptureUtil.exe" title "First-Run" "qa\acceptance_test_103\02c_test_connection_sql_auth.png"

# Dump status
& "Tools\ScreenCaptureUtil\bin\Release\net10.0-windows\ScreenCaptureUtil.exe" dump "First-Run"

Stop-Process -Id $p.Id -Force -ErrorAction SilentlyContinue
