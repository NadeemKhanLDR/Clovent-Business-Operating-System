$cap = "D:\Clovent Business Operating System\Tools\ScreenCaptureUtil\bin\Release\net10.0-windows\ScreenCaptureUtil.exe"
$exePath = "D:\Clovent Business Operating System\artifacts\release\Clovent.BusinessOperatingSystem-1.0.7-win-x64\Clovent.Desktop.exe"

Get-Process -Name "*Clovent*" -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Seconds 1

$p = Start-Process $exePath -PassThru
& $cap wait-win "Sign in" 30
Start-Sleep -Seconds 1

& $cap set-text "Sign in" "Username" "admin_acceptance"
& $cap set-text "Sign in" "Password" "Password@2026!"
& $cap click-btn "Sign in" "POS"
Start-Sleep -Seconds 3

& $cap click-btn "Long-Running Shift Active" "OK"
Start-Sleep -Seconds 3

Write-Host "Waiting for 'An error occurred'..."
& $cap wait-win "An error occurred" 10
& $cap click-btn "An error occurred" "Show Details"
Start-Sleep -Milliseconds 500

& $cap title "An error occurred" "D:\Clovent Business Operating System\qa\gdi_error_expanded.png"
& $cap dump "An error occurred" > "D:\Clovent Business Operating System\qa\gdi_error_dump.txt"

Stop-Process -Id $p.Id -Force
Write-Host "Done"
