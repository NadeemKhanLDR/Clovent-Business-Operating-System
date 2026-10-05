$ErrorActionPreference = "Stop"
$cap = "D:\Clovent Business Operating System\Tools\ScreenCaptureUtil\bin\Release\net10.0-windows\ScreenCaptureUtil.exe"
$exePath = "D:\Clovent Business Operating System\artifacts\release\Clovent.BusinessOperatingSystem-1.0.7-win-x64\Clovent.Desktop.exe"

Get-Process -Name "*Clovent*" -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Seconds 1

Write-Host "Starting 1.0.7..."
$p = Start-Process $exePath -PassThru

Write-Host "Waiting for Sign in window..."
& $cap wait-win "Sign in" 30
Start-Sleep -Seconds 1

Write-Host "Setting credentials with set-text..."
& $cap set-text "Sign in" "Username" "admin_acceptance"
Start-Sleep -Milliseconds 400
& $cap set-text "Sign in" "Password" "Password@2026!"
Start-Sleep -Milliseconds 400

Write-Host "Clicking POS with click-btn..."
& $cap click-btn "Sign in" "POS"
Start-Sleep -Seconds 5

Write-Host "Windows currently visible:"
& $cap list-wins

Stop-Process -Id $p.Id -Force
Write-Host "Finished test_login_107"
