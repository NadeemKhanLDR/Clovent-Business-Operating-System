Param([int]$ProcId)
Add-Type -TypeDefinition 'using System;using System.Text;using System.Runtime.InteropServices;namespace Q3{public static class U{[DllImport("user32.dll")] public static extern bool EnumWindows(EnumWindowsProc cb,IntPtr l);public delegate bool EnumWindowsProc(IntPtr h,IntPtr l);[DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr h,out uint pid);[DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr h);[DllImport("user32.dll")] public static extern int GetClassName(IntPtr h,StringBuilder s,int n);[DllImport("user32.dll")] public static extern int GetWindowText(IntPtr h,StringBuilder s,int n);[DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h,out R r);public struct R{public int L,T,Rt,B;}}}' | Out-Null
$found = New-Object System.Collections.ArrayList
$cb = {
  param($h,$l)
  [uint32]$wpid = 0
  [Q3.U]::GetWindowThreadProcessId($h, [ref]$wpid) | Out-Null
  if ($wpid -eq $ProcId -and [Q3.U]::IsWindowVisible($h)) {
    $c = New-Object System.Text.StringBuilder 256
    [Q3.U]::GetClassName($h, $c, 256) | Out-Null
    $t = New-Object System.Text.StringBuilder 256
    [Q3.U]::GetWindowText($h, $t, 256) | Out-Null
    $r = New-Object Q3.U+R
    [Q3.U]::GetWindowRect($h, [ref]$r) | Out-Null
    [void]$found.Add("$h class=$($c.ToString()) text=$($t.ToString()) rect=$($r.L),$($r.T) - $($r.Rt),$($r.B)")
  }
  return $true
}
[Q3.U]::EnumWindows($cb, [IntPtr]::Zero) | Out-Null
$found | ForEach-Object { Write-Output $_ }
