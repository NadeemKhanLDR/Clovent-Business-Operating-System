$cap = "D:\Clovent Business Operating System\Tools\ScreenCaptureUtil\bin\Release\net10.0-windows\ScreenCaptureUtil.exe"
$exePath = "D:\Clovent Business Operating System\artifacts\release\Clovent.BusinessOperatingSystem-1.0.7-win-x64\Clovent.Desktop.exe"

Get-Process -Name "*Clovent*" -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Seconds 1

Write-Host "Starting 1.0.7..."
$p = Start-Process $exePath -PassThru

& $cap wait-win "Sign in" 30
Start-Sleep -Seconds 1

& $cap set-text "Sign in" "Username" "admin_acceptance"
Start-Sleep -Milliseconds 400
& $cap set-text "Sign in" "Password" "Password@2026!"
Start-Sleep -Milliseconds 500

Write-Host "Invoking inspect on elements..."
& $cap inspect "Sign in" "Username"
& $cap inspect "Sign in" "Password"

# Let's find rect of cardPos dynamically from UIAutomation
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName System.Windows.Forms
$root = [System.Windows.Automation.AutomationElement]::RootElement
$procCond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ProcessIdProperty, $p.Id)
$win = $root.FindFirst([System.Windows.Automation.TreeScope]::Children, $procCond)

$cardPos = $win.FindFirst([System.Windows.Automation.TreeScope]::Descendants, (New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::AutomationIdProperty, "cardPos")))
$r = $cardPos.Current.BoundingRectangle
Write-Host "cardPos BoundingRectangle: X=$($r.X) Y=$($r.Y) W=$($r.Width) H=$($r.Height)"
$cx = [int]($r.X + $r.Width / 2)
$cy = [int]($r.Y + $r.Height / 2)
Write-Host "Clicking physical cursor at ($cx, $cy)..."
& $cap click-xy $cx $cy

Start-Sleep -Seconds 3

Write-Host "Checking if error label has text..."
& $cap inspect "Sign in" "lblError"
& $cap title "Sign in" "D:\Clovent Business Operating System\qa\test_after_click.png"

Write-Host "Open windows:"
& $cap list-wins

Stop-Process -Id $p.Id -Force
