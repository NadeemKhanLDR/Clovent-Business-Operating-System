Param([int]$ProcId)
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
Add-Type -AssemblyName System.Windows.Forms
Add-Type -MemberDefinition '[DllImport("user32.dll")] public static extern bool SetProcessDPIAware();' -Name D -Namespace W
[W.D]::SetProcessDPIAware() | Out-Null
Add-Type -MemberDefinition '[DllImport("user32.dll")] public static extern bool mouse_event(uint f, uint dx, uint dy, uint data, int ex); [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h); [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h, int cmd);' -Name U -Namespace W

$root = [System.Windows.Automation.AutomationElement]::RootElement
$cond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ProcessIdProperty, $ProcId)
$wins = $root.FindAll([System.Windows.Automation.TreeScope]::Children, $cond)
$login = $null
foreach ($w in $wins) { if ($w.Current.Name -like 'Sign in*') { $login = $w } }
if (-not $login) { Write-Output "NO LOGIN WINDOW"; exit 1 }

[W.U]::ShowWindow([IntPtr]$login.Current.NativeWindowHandle, 9) | Out-Null
[W.U]::SetForegroundWindow([IntPtr]$login.Current.NativeWindowHandle) | Out-Null
Start-Sleep -Milliseconds 800

function Click { param([int]$x, [int]$y)
  [System.Windows.Forms.Cursor]::Position = New-Object System.Drawing.Point($x, $y)
  Start-Sleep -Milliseconds 250
  [W.U]::mouse_event(2,0,0,0,0); Start-Sleep -Milliseconds 80; [W.U]::mouse_event(4,0,0,0,0)
  Start-Sleep -Milliseconds 350
}

# Login fields (physical px, DPI-aware): username, password, POS card
Click 2245 760
[System.Windows.Forms.SendKeys]::SendWait('^aadmin')
Start-Sleep -Milliseconds 400
Click 2245 905
[System.Windows.Forms.SendKeys]::SendWait('^aAdmin123!')
Start-Sleep -Milliseconds 400
Click 1945 1667
Write-Output "LOGIN SUBMITTED"
