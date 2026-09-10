Param([int]$X, [int]$Y, [int]$Delay=350)
Add-Type -MemberDefinition '[DllImport("user32.dll")] public static extern bool SetProcessDPIAware(); [DllImport("user32.dll")] public static extern void mouse_event(uint f,uint dx,uint dy,uint data,int ex); [DllImport("user32.dll")] public static extern bool SetCursorPos(int x,int y); [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);' -Name U -Namespace W
[W.U]::SetProcessDPIAware() | Out-Null
[System.Windows.Forms.Cursor]::Position | Out-Null
Add-Type -AssemblyName System.Windows.Forms
[W.U]::SetCursorPos($X, $Y) | Out-Null
Start-Sleep -Milliseconds 250
[W.U]::mouse_event(2,0,0,0,0)
Start-Sleep -Milliseconds 60
[W.U]::mouse_event(4,0,0,0,0)
Start-Sleep -Milliseconds $Delay
Write-Output "CLICKED $X,$Y"
