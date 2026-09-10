Param([int]$X, [int]$Y)
Add-Type -AssemblyName System.Windows.Forms
Add-Type -MemberDefinition '[DllImport("user32.dll")] public static extern bool SetProcessDPIAware(); [DllImport("user32.dll")] public static extern void mouse_event(uint f, uint dx, uint dy, uint data, int ex);' -Name U -Namespace W
[W.U]::SetProcessDPIAware() | Out-Null
[System.Windows.Forms.Cursor]::Position = New-Object System.Drawing.Point($X, $Y)
Start-Sleep -Milliseconds 200
[W.U]::mouse_event(2,0,0,0,0); Start-Sleep -Milliseconds 60; [W.U]::mouse_event(4,0,0,0,0)
Write-Output "clicked $X,$Y"
