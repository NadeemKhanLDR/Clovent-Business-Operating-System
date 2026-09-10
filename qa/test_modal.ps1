Add-Type -TypeDefinition '
using System;
using System.Text;
using System.Runtime.InteropServices;
namespace Qa2 {
  public static class W {
    [DllImport("user32.dll")] public static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);
    [DllImport("user32.dll")] public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern void mouse_event(uint f, uint dx, uint dy, uint data, int ex);
    [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
  }
}'

$p = Get-Process Clovent.Desktop -ErrorAction SilentlyContinue
if (-not $p) {
    $exe = "d:\Clovent Business Operating System\src\Clovent.Desktop\bin\Debug\net10.0-windows\Clovent.Desktop.exe"
    $p = Start-Process $exe -ArgumentList "--pos" -PassThru
    Start-Sleep -Seconds 6
}

$h = $p.MainWindowHandle
Write-Output "POS Main Window Handle: $h"

# Click at Center: 3342, 2164
[Qa2.W]::SetForegroundWindow($h) | Out-Null
Start-Sleep -Milliseconds 400
[Qa2.W]::SetCursorPos(3342, 2164) | Out-Null
Start-Sleep -Milliseconds 150
[Qa2.W]::mouse_event(2, 0, 0, 0, 0)
Start-Sleep -Milliseconds 100
[Qa2.W]::mouse_event(4, 0, 0, 0, 0)
Start-Sleep -Seconds 2

# Check for popup window via GW_ENABLEDPOPUP (6)
$popup = [Qa2.W]::GetWindow($h, 6)
$sb = New-Object System.Text.StringBuilder 256
[Qa2.W]::GetWindowText($popup, $sb, 256) | Out-Null
Write-Output "Popup Handle: $popup, Title: '$($sb.ToString())'"

Get-Process Clovent.Desktop -ErrorAction SilentlyContinue | Stop-Process -Force
