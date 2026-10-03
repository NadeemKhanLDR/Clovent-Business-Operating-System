$ErrorActionPreference = "Stop"

$cap = "Tools\ScreenCaptureUtil\bin\Release\net10.0-windows\ScreenCaptureUtil.exe"

Get-Process -Name "*Clovent*" -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Seconds 1

Write-Host "Starting C:\CloventClient105\Clovent.Desktop.exe..."
$p = Start-Process "C:\CloventClient105\Clovent.Desktop.exe" -PassThru
Write-Host "Started PID: $($p.Id)"

& $cap wait-win "Sign in" 25
Start-Sleep -Seconds 2

Write-Host "Entering credentials..."
& $cap set-text "Sign in" "Username" "admin_acceptance"
Start-Sleep -Milliseconds 500
& $cap set-text "Sign in" "Password" "Password@2026!"
Start-Sleep -Milliseconds 500

& $cap title "Sign in" "qa\acceptance_test_103\09_login_form_filled.png"
Write-Host "Captured 09_login_form_filled.png"

Write-Host "Clicking POS card with HWND click..."
& $cap click-name "Sign in" "cardPos"
Start-Sleep -Seconds 3

# Check for next window
for ($i = 0; $i -lt 30; $i++) {
    $wins = & $cap list-wins 2>&1 | Out-String
    Write-Host "Tick $i windows:`n$wins"

    if ($wins -match "Punch In") {
        Write-Host "Punch In dialog detected. Capturing..."
        & $cap title "Punch In" "qa\acceptance_test_103\09b_punch_in.png"
        Write-Host "Clicking Punch In button..."
        & $cap click-btn "Punch In" "Punch In"
        Start-Sleep -Seconds 2
    }

    if ($wins -match "Open Cash Register Shift") {
        Write-Host "Open Shift dialog detected. Capturing 10_pos_shift_open.png..."
        & $cap title "Open Cash Register Shift" "qa\acceptance_test_103\10_pos_shift_open.png"
        Write-Host "Clicking Open Shift button..."
        & $cap click-btn "Open Cash Register Shift" "Open Shift"
        Start-Sleep -Seconds 3
    }

    if ($wins -match "Restaurant POS") {
        Write-Host "Restaurant POS window detected!"
        Start-Sleep -Seconds 2
        & $cap title "Restaurant POS" "qa\acceptance_test_103\10b_pos_main_ready.png"
        Write-Host "Captured 10b_pos_main_ready.png"
        break
    }

    Start-Sleep -Seconds 1
}

Write-Host "Final window list:"
& $cap list-wins
