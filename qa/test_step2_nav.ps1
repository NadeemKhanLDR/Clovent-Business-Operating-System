$p = Start-Process "C:\CloventClient103\Clovent.Desktop.exe" -PassThru
Write-Host "Started PID: $($p.Id)"

# Wait for window to appear
& "Tools\ScreenCaptureUtil\bin\Release\net10.0-windows\ScreenCaptureUtil.exe" wait-win "First-Run" 15

# Capture Step 1
& "Tools\ScreenCaptureUtil\bin\Release\net10.0-windows\ScreenCaptureUtil.exe" title "First-Run" "qa\acceptance_test_103\01_wizard_step1_welcome.png"

# Click Next to go to Step 2
& "Tools\ScreenCaptureUtil\bin\Release\net10.0-windows\ScreenCaptureUtil.exe" click-btn "First-Run" "Next >"
Start-Sleep -Seconds 2

# Capture Step 2
& "Tools\ScreenCaptureUtil\bin\Release\net10.0-windows\ScreenCaptureUtil.exe" title "First-Run" "qa\acceptance_test_103\02_step2_db_connection.png"

# Dump Step 2 controls
& "Tools\ScreenCaptureUtil\bin\Release\net10.0-windows\ScreenCaptureUtil.exe" dump "First-Run"

Stop-Process -Id $p.Id -Force -ErrorAction SilentlyContinue
