Param([int]$ProcId, [int]$Cmd = 9)
Add-Type -MemberDefinition '[DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h); [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h, int cmd);' -Name U -Namespace W
$p = Get-Process -Id $ProcId
$h = (Get-Process -Id $ProcId).MainWindowHandle
if ($h -eq [IntPtr]::Zero) { Write-Output "no main window"; exit 1 }
[W.U]::ShowWindow($h, $Cmd) | Out-Null
[W.U]::SetForegroundWindow($h) | Out-Null
Start-Sleep -Milliseconds 500
Write-Output "foreground set: $($p.MainWindowTitle)"
