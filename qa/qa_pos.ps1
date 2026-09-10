Param(
  [int]$ProcId,
  [string]$Mode,          # size | max | restore | shot
  [int]$W, [int]$H,       # logical client size for size mode
  [string]$Out            # output path for shot mode
)
Add-Type -TypeDefinition '
using System;
using System.Runtime.InteropServices;
namespace Qa {
  public struct RECT { public int L, T, R, B; }
  public static class U {
    [DllImport("user32.dll")] public static extern bool SetProcessDpiAwarenessContext(IntPtr v);
    [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr h, IntPtr a, int x, int y, int cx, int cy, uint f);
    [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h, int cmd);
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
    [DllImport("user32.dll")] public static extern bool GetClientRect(IntPtr h, out RECT r);
    [DllImport("user32.dll")] public static extern bool AdjustWindowRectExForDpi(ref RECT r, uint style, bool menu, uint exStyle, uint dpi);
    [DllImport("user32.dll")] public static extern int GetDpiForWindow(IntPtr h);
    [DllImport("user32.dll")] public static extern uint GetWindowLong(IntPtr h, int i);
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
  }
}' | Out-Null
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing
[Qa.U]::SetProcessDpiAwarenessContext([IntPtr](-4)) | Out-Null

$p = Get-Process -Id $ProcId
$p.Refresh()
$h = $p.MainWindowHandle
if ($h -eq [IntPtr]::Zero) { Write-Output "NO WINDOW"; exit 1 }

switch ($Mode) {
  'size' {
    [Qa.U]::ShowWindow($h, 9) | Out-Null
    Start-Sleep -Milliseconds 400
    $dpi = [Qa.U]::GetDpiForWindow($h)
    if ($dpi -eq 0) { $dpi = 240 }
    $style = [Qa.U]::GetWindowLong($h, -16)
    $ex = [Qa.U]::GetWindowLong($h, -20)
    $r = New-Object Qa.RECT
    $rw = [int]([math]::Round($W * $dpi / 96.0))
    $rh = [int]([math]::Round($H * $dpi / 96.0))
    $r.L = 0; $r.T = 0; $r.R = $rw; $r.B = $rh
    [Qa.U]::AdjustWindowRectExForDpi([ref]$r, $style, $false, $ex, $dpi) | Out-Null
    $ow = $r.R - $r.L; $oh = $r.B - $r.T
    $sb = [System.Windows.Forms.Screen]::PrimaryScreen.Bounds
    $x = [int](($sb.Width - $ow) / 2); $y = [int](($sb.Height - $oh) / 2)
    if ($x -lt 0) { $x = 0 }; if ($y -lt 0) { $y = 0 }
    [Qa.U]::SetWindowPos($h, [IntPtr]::Zero, $x, $y, $ow, $oh, 0x0004) | Out-Null
    Start-Sleep -Milliseconds 800
    $cr = New-Object Qa.RECT
    [Qa.U]::GetClientRect($h, [ref]$cr) | Out-Null
    Write-Output ("CLIENT_PHYS=" + ($cr.R - $cr.L) + "x" + ($cr.B - $cr.T) + " LOGICAL=" + [int](($cr.R-$cr.L)*96.0/$dpi) + "x" + [int](($cr.B-$cr.T)*96.0/$dpi) + " DPI=" + $dpi)
  }
  'max' {
    [Qa.U]::ShowWindow($h, 3) | Out-Null
    Start-Sleep -Milliseconds 800
    Write-Output "MAXIMIZED"
  }
  'restore' {
    [Qa.U]::ShowWindow($h, 9) | Out-Null
    Start-Sleep -Milliseconds 800
    $wr = New-Object Qa.RECT
    [Qa.U]::GetWindowRect($h, [ref]$wr) | Out-Null
    Write-Output ("RESTORED_RECT_PHYS=" + ($wr.R-$wr.L) + "x" + ($wr.B-$wr.T))
  }
  'shot' {
    $wr = New-Object Qa.RECT
    [Qa.U]::GetWindowRect($h, [ref]$wr) | Out-Null
    $w = $wr.R - $wr.L; $ht = $wr.B - $wr.T
    $bmp = New-Object System.Drawing.Bitmap($w, $ht)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.CopyFromScreen($wr.L, $wr.T, 0, 0, (New-Object System.Drawing.Size($w, $ht)))
    $g.Dispose()
    $bmp.Save($Out, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
    Write-Output ("SAVED " + $Out + " " + $w + "x" + $ht)
  }
}
