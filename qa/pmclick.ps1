Param([int]$X, [int]$Y, [int]$Delay=400)
Add-Type -MemberDefinition '[DllImport("user32.dll")] public static extern bool SetProcessDpiAwarenessContext(IntPtr v); [DllImport("user32.dll")] public static extern void mouse_event(uint f,uint dx,uint dy,uint data,int ex); [DllImport("user32.dll")] public static extern bool SetCursorPos(int x,int y); [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);' -Name U -Namespace W
[W.U]::SetProcessDpiAwarenessContext([IntPtr](-4)) | Out-Null
[W.U]::SetCursorPos($X,$Y) | Out-Null
Start-Sleep -Milliseconds 200
[W.U]::mouse_event(2,0,0,0,0); Start-Sleep -Milliseconds 50; [W.U]::mouse_event(4,0,0,0,0)
Start-Sleep -Milliseconds $Delay
Write-Output "CLICKED $X,$Y"
