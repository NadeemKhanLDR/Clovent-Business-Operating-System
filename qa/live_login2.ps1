Param()
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
Add-Type -AssemblyName System.Windows.Forms
Add-Type -MemberDefinition '[DllImport("user32.dll")] public static extern bool SetProcessDPIAware();' -Name D -Namespace W
[W.D]::SetProcessDPIAware() | Out-Null
Add-Type -MemberDefinition '[DllImport("user32.dll")] public static extern bool mouse_event(uint f, uint dx, uint dy, uint data, int ex); [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h); [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h, int cmd);' -Name U -Namespace W

$procId = [int](Get-Content 'qa\v3_pid.txt')
$root = [System.Windows.Automation.AutomationElement]::RootElement
$cond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ProcessIdProperty, $procId)
$login = $null
foreach ($w in $root.FindAll([System.Windows.Automation.TreeScope]::Children, $cond)) {
  if ($w.Current.Name -like 'Sign in*') { $login = $w }
}
if (-not $login) { Write-Output 'NO LOGIN WINDOW'; exit 1 }
$h = [IntPtr]$login.Current.NativeWindowHandle
[W.U]::ShowWindow($h, 9) | Out-Null
[W.U]::SetForegroundWindow($h) | Out-Null
Start-Sleep -Milliseconds 900

function Click { param([int]$x, [int]$y)
  [System.Windows.Forms.Cursor]::Position = New-Object System.Drawing.Point($x, $y)
  Start-Sleep -Milliseconds 300
  [W.U]::mouse_event(2,0,0,0,0); Start-Sleep -Milliseconds 90; [W.U]::mouse_event(4,0,0,0,0)
  Start-Sleep -Milliseconds 400
}

# field rows from UIA dump: Username pane @1670,676 h33; Password @1670,821; POS card @1678,1638
Click 2245 670
[System.Windows.Forms.SendKeys]::SendWait('^aadmin')
Start-Sleep -Milliseconds 400
Click 2245 815
[System.Windows.Forms.SendKeys]::SendWait('^aAdmin123!')
Start-Sleep -Milliseconds 400
Click 1945 1670
Start-Sleep -Milliseconds 800
# press sign-in button (bottom of form) - find it via UIA
$btns = $login.FindAll([System.Windows.Automation.TreeScope]::Descendants, (New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ControlTypeProperty, [System.Windows.Automation.ControlType]::Button)))
foreach ($b in $btns) { Write-Output ("BTN '{0}'" -f $b.Current.Name) }
Write-Output 'LOGIN FILL DONE'
