$ErrorActionPreference = "Stop"
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName System.Windows.Forms
$cap = "Tools\ScreenCaptureUtil\bin\Release\net10.0-windows\ScreenCaptureUtil.exe"

Get-Process -Name "*Clovent*" -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Seconds 2

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
& $cap set-text "Sign in" "Username" "admin_acceptance"
& $cap set-text "Sign in" "Password" "Password@2026!"
& $cap click-btn "Sign in" "BACK OFFICE"
Start-Sleep -Seconds 5

$mainTitle = "Clovent Business Operating System"
# Try pressing Alt
& $cap key "%"
Start-Sleep -Seconds 1
& $cap title $mainTitle "qa\acceptance_test_103\test_keytips.png"

Stop-Process -Id $p.Id -Force
