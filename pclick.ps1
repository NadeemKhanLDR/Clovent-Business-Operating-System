Param([long]$Hwnd, [int]$ClientX, [int]$ClientY)
Add-Type -AssemblyName System.Windows.Forms
Add-Type -MemberDefinition '[DllImport("user32.dll")] public static extern bool PostMessage(IntPtr h, uint m, IntPtr w, IntPtr l); [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);' -Name P -Namespace N
[N.P]::SetForegroundWindow([IntPtr]$Hwnd) | Out-Null
Start-Sleep -Milliseconds 150
$lParam = [IntPtr](($ClientY -shl 16) -bor ($ClientX -band 0xFFFF))
$MK_LBUTTON = 0x1
[N.P]::PostMessage([IntPtr]$Hwnd, 0x0201, [IntPtr]$MK_LBUTTON, $lParam) | Out-Null  # WM_LBUTTONDOWN
Start-Sleep -Milliseconds 120
[N.P]::PostMessage([IntPtr]$Hwnd, 0x0202, [IntPtr]::Zero, $lParam) | Out-Null       # WM_LBUTTONUP
Write-Output "posted click to $Hwnd at client $ClientX,$ClientY"
