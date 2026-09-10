Param([int]$X, [int]$Y, [string]$Text)
Add-Type -AssemblyName System.Windows.Forms
Add-Type -MemberDefinition '[DllImport("user32.dll")] public static extern bool SetProcessDPIAware(); [DllImport("user32.dll")] public static extern void mouse_event(uint f,uint dx,uint dy,uint data,int ex); [DllImport("user32.dll")] public static extern bool SetCursorPos(int x,int y); [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);' -Name U -Namespace WT
[WT.U]::SetProcessDPIAware() | Out-Null
$p = Get-Process -Id 26380
[WT.U]::SetForegroundWindow($p.MainWindowHandle) | Out-Null
Start-Sleep -Milliseconds 400
[WT.U]::SetCursorPos($X, $Y) | Out-Null
Start-Sleep -Milliseconds 250
[WT.U]::mouse_event(2,0,0,0,0); Start-Sleep -Milliseconds 60; [WT.U]::mouse_event(4,0,0,0,0)
Start-Sleep -Milliseconds 400
[System.Windows.Forms.SendKeys]::SendWait($Text)
Start-Sleep -Milliseconds 900
Write-Output "TYPED at $X,$Y"
