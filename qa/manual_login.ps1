Add-Type -AssemblyName System.Windows.Forms
Add-Type -MemberDefinition '[DllImport("user32.dll")] public static extern bool SetProcessDPIAware(); [DllImport("user32.dll")] public static extern bool mouse_event(uint f, uint dx, uint dy, uint data, int ex); [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);' -Name U -Namespace W
[W.U]::SetProcessDPIAware() | Out-Null
$p = Get-Process Clovent.Desktop | Select-Object -First 1
[W.U]::SetForegroundWindow($p.MainWindowHandle) | Out-Null
Start-Sleep -Milliseconds 800
function Click { param([int]$x,[int]$y)
  [System.Windows.Forms.Cursor]::Position = New-Object System.Drawing.Point($x,$y)
  Start-Sleep -Milliseconds 250
  [W.U]::mouse_event(2,0,0,0,0); Start-Sleep -Milliseconds 80; [W.U]::mouse_event(4,0,0,0,0)
  Start-Sleep -Milliseconds 400
}
# username (center ~1920, y~720), password (~1920, y~960)
Click 1920 720
[System.Windows.Forms.SendKeys]::SendWait('^a')
Start-Sleep -Milliseconds 200
[System.Windows.Forms.SendKeys]::SendWait('admin')
Start-Sleep -Milliseconds 300
Click 1920 960
[System.Windows.Forms.SendKeys]::SendWait('^a')
Start-Sleep -Milliseconds 200
[System.Windows.Forms.SendKeys]::SendWait('Admin123!')
Start-Sleep -Milliseconds 300
[System.Windows.Forms.SendKeys]::SendWait('{ENTER}')
Write-Output "TYPED AND ENTERED"
