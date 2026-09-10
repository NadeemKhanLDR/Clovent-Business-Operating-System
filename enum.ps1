Param([int]$ProcId)
Add-Type -MemberDefinition '[DllImport("user32.dll")] public static extern bool SetProcessDPIAware();' -Name D -Namespace W
[W.D]::SetProcessDPIAware() | Out-Null
Add-Type -MemberDefinition '
[DllImport("user32.dll")] public static extern bool EnumChildWindows(IntPtr h, EnumProc cb, IntPtr l);
public delegate bool EnumProc(IntPtr h, IntPtr l);
[DllImport("user32.dll")] public static extern int GetClassName(IntPtr h, System.Text.StringBuilder s, int n);
[DllImport("user32.dll")] public static extern int GetWindowText(IntPtr h, System.Text.StringBuilder s, int n);
[DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
[DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr h);
[DllImport("user32.dll")] public static extern IntPtr GetWindowLong(IntPtr h, int i);
public struct RECT { public int L, T, R, B; }
' -Name W -Namespace N
$targets = @()
$cb = {
  param($h, $l)
  $sb = New-Object System.Text.StringBuilder 256
  [N.W]::GetClassName($h, $sb, 256) | Out-Null
  $cls = $sb.ToString()
  $sb.Clear() | Out-Null
  [N.W]::GetWindowText($h, $sb, 256) | Out-Null
  $txt = $sb.ToString()
  $vis = [N.W]::IsWindowVisible($h)
  $r = New-Object N.W+RECT
  [N.W]::GetWindowRect($h, [ref]$r) | Out-Null
  $script:targets += [PSCustomObject]@{Handle=$h; Class=$cls; Text=$txt; Visible=$vis; L=$r.L; T=$r.T; R=$r.R; B=$r.B}
  return $true
}
# enumerate all top-level windows of the process, then children
Add-Type -MemberDefinition '
[DllImport("user32.dll")] public static extern bool EnumWindows(EnumProc cb, IntPtr l);
public delegate bool EnumProc(IntPtr h, IntPtr l);
[DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
[DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr h);
[DllImport("user32.dll")] public static extern int GetWindowText(IntPtr h, System.Text.StringBuilder s, int n);
[DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
public struct RECT { public int L, T, R, B; }
' -Name W2 -Namespace N
$tops = @()
$cb2 = {
  param($h, $l)
  $pid2 = 0
  [N.W2]::GetWindowThreadProcessId($h, [ref]$pid2) | Out-Null
  if ($pid2 -eq $ProcId -and [N.W2]::IsWindowVisible($h)) {
    $sb = New-Object System.Text.StringBuilder 256
    [N.W2]::GetWindowText($h, $sb, 256) | Out-Null
    $r = New-Object N.W2+RECT
    [N.W2]::GetWindowRect($h, [ref]$r) | Out-Null
    $script:tops += [PSCustomObject]@{Handle=$h; Text=$sb.ToString(); L=$r.L; T=$r.T; R=$r.R; B=$r.B}
  }
  return $true
}
[N.W2]::EnumWindows($cb2, [IntPtr]::Zero) | Out-Null
foreach ($t in $tops) {
  Write-Output ("=== TOP hwnd=" + $t.Handle + " '" + $t.Text + "' rect=" + $t.L + "," + $t.T + "," + $t.R + "," + $t.B)
  [N.W]::EnumChildWindows($t.Handle, $cb, [IntPtr]::Zero) | Out-Null
}
$targets | ForEach-Object {
  $cx = [int](($_.L + $_.R)/2); $cy = [int](($_.T + $_.B)/2)
  Write-Output ("  hwnd=" + $_.Handle + " cls=" + $_.Class + " vis=" + $_.Visible + " rect=" + $_.L + "," + $_.T + "," + $_.R + "," + $_.B + " ctr=" + $cx + "," + $cy + " text='" + $_.Text + "'")
}
