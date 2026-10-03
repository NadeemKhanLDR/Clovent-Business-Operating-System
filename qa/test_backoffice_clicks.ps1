$ErrorActionPreference = "Stop"

$cap = "Tools\ScreenCaptureUtil\bin\Release\net10.0-windows\ScreenCaptureUtil.exe"

Get-Process -Name "*Clovent*" -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Seconds 2

Write-Host "1. Launching Clovent.Desktop.exe..."
$p = Start-Process "C:\CloventClient105\Clovent.Desktop.exe" -PassThru
Start-Sleep -Seconds 3

# Wait for Sign in
$signInHwnd = 0
for ($i = 0; $i -lt 30; $i++) {
    Start-Sleep -Seconds 1
    $procWins = Get-Process -Id $p.Id -ErrorAction SilentlyContinue
    if ($procWins -and $procWins.MainWindowHandle -ne 0) {
        $signInHwnd = $procWins.MainWindowHandle
        break
    }
}

Write-Host "Sign in HWND: $signInHwnd"
& $cap set-text "Sign in" "Username" "admin_acceptance"
Start-Sleep -Milliseconds 400
& $cap set-text "Sign in" "Password" "Password@2026!"
Start-Sleep -Milliseconds 400

# Click Back Office
& $cap click-btn "Sign in" "BACK OFFICE"
Start-Sleep -Seconds 5

$mainTitle = "Clovent Business Operating System"
Write-Host "Dumping Back Office controls..."
& $cap list-wins

# Capture main dashboard
& $cap title $mainTitle "qa\acceptance_test_103\test_main.png"

# Test tab select
Write-Host "Selecting Reports tab..."
& $cap select-tab $mainTitle "Reports"
Start-Sleep -Seconds 2
& $cap title $mainTitle "qa\acceptance_test_103\test_reports_tab.png"

# Check what buttons are on Reports tab
Write-Host "Clicking Sales Summary..."
& $cap click-btn $mainTitle "Sales Summary"
Start-Sleep -Seconds 3
& $cap title $mainTitle "qa\acceptance_test_103\test_sales_summary.png"

# Stop process
Stop-Process -Id $p.Id -Force
