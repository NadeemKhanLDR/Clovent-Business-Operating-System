$p = Start-Process "C:\CloventClient103\Clovent.Desktop.exe" -PassThru
Write-Host "Started PID: $($p.Id)"
Start-Sleep -Seconds 3

& "Tools\ScreenCaptureUtil\bin\Release\net10.0-windows\ScreenCaptureUtil.exe" dump "First-Run"
& "Tools\ScreenCaptureUtil\bin\Release\net10.0-windows\ScreenCaptureUtil.exe" title "First-Run" "qa\acceptance_test_103\01_wizard_step1_welcome.png"

Stop-Process -Id $p.Id -Force -ErrorAction SilentlyContinue
