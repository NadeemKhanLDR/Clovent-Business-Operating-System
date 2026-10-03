$ErrorActionPreference = "Stop"
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName System.Windows.Forms
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

# Switch to Reports
& $cap select-tab "Clovent Business Operating System" "Reports"
Start-Sleep -Seconds 2

# Dump all elements
$all = $main.FindAll([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.Condition]::TrueCondition)
$lines = [System.Collections.Generic.List[string]]::new()
foreach ($e in $all) {
    try {
        $lines.Add("NAME: '$($e.Current.Name)' | ID: '$($e.Current.AutomationId)' | TYPE: $($e.Current.ControlType.ProgrammaticName) | RECT: $($e.Current.BoundingRectangle)")
    } catch {}
}
[System.IO.File]::WriteAllLines("qa\acceptance_test_103\main_uia_dump.txt", $lines)
Write-Host "Dumped $($lines.Count) elements to qa\acceptance_test_103\main_uia_dump.txt"

Stop-Process -Id $p.Id -Force
