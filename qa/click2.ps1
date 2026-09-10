Param([int]$X, [int]$Y, [int]$Delay=400)
Add-Type -AssemblyName System.Windows.Forms
Add-Type -MemberDefinition '[DllImport("user32.dll")] public static extern bool SetProcessDPIAware(); [DllImport("user32.dll")] public static extern void mouse_event(uint f,uint dx,uint dy,uint data,int ex); [DllImport("user32.dll")] public static extern bool SetCursorPos(int x,int y);' -Name U -Namespace WQ
[WQ.U]::SetProcessDPIAware() | Out-Null
[WQ.U]::SetCursorPos($X, $Y) | Out-Null
Start-Sleep -Milliseconds 250
[WQ.U]::mouse_event(2,0,0,0,0)
Start-Sleep -Milliseconds 60
[WQ.U]::mouse_event(4,0,0,0,0)
Start-Sleep -Milliseconds $Delay
Write-Output "CLICKED $X,$Y"
