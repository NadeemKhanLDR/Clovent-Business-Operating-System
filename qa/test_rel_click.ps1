$ErrorActionPreference = "Stop"
Add-Type -AssemblyName UIAutomationClient
$cap = "Tools\ScreenCaptureUtil\bin\Release\net10.0-windows\ScreenCaptureUtil.exe"

Get-Process -Name "*Clovent*" -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Seconds 2

Write-Host "1. Launching Clovent.Desktop.exe..."
$p = Start-Process "C:\CloventClient105\Clovent.Desktop.exe" -PassThru
$root = [System.Windows.Automation.AutomationElement]::RootElement

# Wait for Sign in
$signIn = $null
for ($i = 0; $i -lt 40; $i++) {
    Start-Sleep -Seconds 1
    $wins = $root.FindAll([System.Windows.Automation.TreeScope]::Children, [System.Windows.Automation.Condition]::TrueCondition)
    foreach ($w in $wins) {
        if ($w.Current.Name -like "*Sign in*") { $signIn = $w; break }
    }
    if ($signIn) { break }
}
if (!$signIn) { throw "Sign In window not found" }
Write-Host "Found Sign in window"

& $cap set-text "Sign in" "Username" "admin_acceptance"
Start-Sleep -Milliseconds 400
& $cap set-text "Sign in" "Password" "Password@2026!"
Start-Sleep -Milliseconds 400
& $cap click-btn "Sign in" "BACK OFFICE"

# Wait for Main Window
$mainTitle = "Clovent Business Operating System"
$main = $null
for ($i = 0; $i -lt 30; $i++) {
    Start-Sleep -Seconds 1
    $wins = $root.FindAll([System.Windows.Automation.TreeScope]::Children, [System.Windows.Automation.Condition]::TrueCondition)
    foreach ($w in $wins) {
        if ($w.Current.Name -like "*Clovent Business Operating System*" -and $w.Current.Name -notlike "*Sign in*") {
            $main = $w
            break
        }
    }
    if ($main) { break }
}
if (!$main) { throw "Main window not found" }
Write-Host "Main window rect: $($main.Current.BoundingRectangle)"

# Select Reports tab using UIA select-tab
Write-Host "Selecting Reports tab..."
& $cap select-tab $mainTitle "Reports"
Start-Sleep -Seconds 2

# Click Sales Summary at rel (85, 330)
Write-Host "Clicking Sales Summary button at rel (85, 330)..."
& $cap click-point-rel $mainTitle 85 330
Start-Sleep -Seconds 4

# Capture document
& $cap title $mainTitle "qa\acceptance_test_103\test_sales_doc.png"

Stop-Process -Id $p.Id -Force
Write-Host "Done!"
