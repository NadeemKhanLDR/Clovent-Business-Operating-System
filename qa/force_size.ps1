Param([int]$ProcId,[int]$W,[int]$H)
Add-Type -TypeDefinition 'using System;using System.Runtime.InteropServices;namespace Q4{public static class U{[DllImport("user32.dll")] public static extern bool SetProcessDpiAwarenessContext(IntPtr v);[DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr h,IntPtr a,int x,int y,int cx,int cy,uint f);[DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h,int c);[DllImport("user32.dll")] public static extern bool GetClientRect(IntPtr h,out R r);[DllImport("user32.dll")] public static extern int GetDpiForWindow(IntPtr h);[DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr h);public struct R{public int L,T,Rt,B;}}}' | Out-Null
[Q4.U]::SetProcessDpiAwarenessContext([IntPtr](-4)) | Out-Null
$p = Get-Process -Id $ProcId; $p.Refresh(); $h = $p.MainWindowHandle
[Q4.U]::ShowWindow($h, 1) | Out-Null
Start-Sleep -Milliseconds 600
$dpi = [Q4.U]::GetDpiForWindow($h)
$rw = [int][math]::Round($W * $dpi / 96.0); $rh = [int][math]::Round($H * $dpi / 96.0)
[Q4.U]::SetWindowPos($h, [IntPtr]::Zero, 0, 0, $rw, $rh, 0x0004) | Out-Null
Start-Sleep -Milliseconds 1200
$r = New-Object Q4.U+R; [Q4.U]::GetClientRect($h, [ref]$r) | Out-Null
Write-Output ("CLIENT_PHYS=" + ($r.Rt-$r.L) + "x" + ($r.B-$r.T) + " LOGICAL=" + [int](($r.Rt-$r.L)*96.0/$dpi) + "x" + [int](($r.B-$r.T)*96.0/$dpi) + " DPI=" + $dpi)
