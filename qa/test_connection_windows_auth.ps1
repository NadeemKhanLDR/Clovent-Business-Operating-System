$p = Start-Process "C:\CloventClient103\Clovent.Desktop.exe" -PassThru
Write-Host "Started PID: $($p.Id)"

# Wait for window to appear
& "Tools\ScreenCaptureUtil\bin\Release\net10.0-windows\ScreenCaptureUtil.exe" wait-win "First-Run" 15

# Click Next to go to Step 2
& "Tools\ScreenCaptureUtil\bin\Release\net10.0-windows\ScreenCaptureUtil.exe" click-btn "First-Run" "Next >"
Start-Sleep -Seconds 2

# Click Test Connection
& "Tools\ScreenCaptureUtil\bin\Release\net10.0-windows\ScreenCaptureUtil.exe" click-btn "First-Run" "Test Connection"
Start-Sleep -Seconds 3

# Capture Step 2 with connection result
& "Tools\ScreenCaptureUtil\bin\Release\net10.0-windows\ScreenCaptureUtil.exe" title "First-Run" "qa\acceptance_test_103\02b_test_connection_windows_auth.png"

# Dump Step 2 controls
& "Tools\ScreenCaptureUtil\bin\Release\net10.0-windows\ScreenCaptureUtil.exe" dump "First-Run"

Stop-Process -Id $p.Id -Force -ErrorAction SilentlyContinue
