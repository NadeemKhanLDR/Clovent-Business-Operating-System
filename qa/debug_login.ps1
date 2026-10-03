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

Write-Host "Sending ENTER key to trigger login..."
& $cap key "Sign in" "enter"
Start-Sleep -Seconds 4

Write-Host "Capturing active screen/windows..."
& $cap list-wins
& $cap screen "qa\acceptance_test_103\debug_after_enter.png"

$wins = & $cap list-wins 2>&1 | Out-String
Write-Host "Visible windows:`n$wins"

# Check if Sign in is still open and dump it
$hSignIn = & $cap wait-win "Sign in" 2
if ($LASTEXITCODE -eq 0) {
    Write-Host "Sign in is still open. Dumping elements..."
    & $cap dump "Sign in" > "qa\acceptance_test_103\debug_signin_dump.txt"
    & $cap title "Sign in" "qa\acceptance_test_103\debug_signin_error.png"
}
