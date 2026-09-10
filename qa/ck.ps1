Param([int]$X, [int]$Y, [string]$Keys = "", [int]$Delay = 1200)
Add-Type -AssemblyName System.Windows.Forms
Add-Type -MemberDefinition '[DllImport("user32.dll")] public static extern bool SetProcessDPIAware(); [DllImport("user32.dll")] public static extern void mouse_event(uint f,uint dx,uint dy,uint data,int ex); [DllImport("user32.dll")] public static extern bool SetCursorPos(int x,int y);' -Name U -Namespace W
[W.U]::SetProcessDPIAware() | Out-Null
[W.U]::SetCursorPos($X, $Y) | Out-Null
Start-Sleep -Milliseconds 250
[W.U]::mouse_event(2,0,0,0,0)
Start-Sleep -Milliseconds 60
[W.U]::mouse_event(4,0,0,0,0)
Start-Sleep -Milliseconds 400
if ($Keys -ne "") {
    [System.Windows.Forms.SendKeys]::SendWait($Keys)
}
Start-Sleep -Milliseconds $Delay
Write-Output "DONE $X,$Y keys='$Keys'"
