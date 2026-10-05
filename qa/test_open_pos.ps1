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
Write-Host "Visible windows after POS click:"
for ($i = 0; $i -lt 10; $i++) {
    & $cap list-wins
    Start-Sleep -Seconds 1
}

Stop-Process -Id $p.Id -Force
