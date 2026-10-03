$code = @'
using System;
using System.Text;
using System.Runtime.InteropServices;
public class WinInfo {
    [DllImport("user32.dll")] public static extern IntPtr WindowFromPoint(POINT Point);
    [DllImport("user32.dll")] public static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);
    [DllImport("user32.dll")] public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);
    [StructLayout(LayoutKind.Sequential)]
    public struct POINT { public int X; public int Y; }
    public static string GetInfo(int x, int y) {
        IntPtr h = WindowFromPoint(new POINT { X = x, Y = y });
        var sbClass = new StringBuilder(256);
        GetClassName(h, sbClass, 256);
        var sbText = new StringBuilder(256);
        GetWindowText(h, sbText, 256);
        return $"HWND={h} Class={sbClass} Text='{sbText}'";
    }
}
'@
Add-Type -TypeDefinition $code -ErrorAction SilentlyContinue

$cap = "Tools\ScreenCaptureUtil\bin\Release\net10.0-windows\ScreenCaptureUtil.exe"
Get-Process -Name "*Clovent*" -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Seconds 1
$p = Start-Process "C:\CloventClient105\Clovent.Desktop.exe" -PassThru
Start-Sleep -Seconds 4
& $cap set-text "Sign in" "Username" "admin_acceptance"
& $cap set-text "Sign in" "Password" "Password@2026!"
& $cap click-btn "Sign in" "BACK OFFICE"
Start-Sleep -Seconds 4

$main = "Clovent Business Operating System"
& $cap select-tab $main "Reports"
Start-Sleep -Seconds 2

# Check WindowFromPoint at (1322, 1070)
$info = [WinInfo]::GetInfo(1322, 1070)
Write-Host "Info at (1322, 1070): $info"

Stop-Process -Id $p.Id -Force
