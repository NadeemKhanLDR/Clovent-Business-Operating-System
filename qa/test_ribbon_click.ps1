$ErrorActionPreference = "Stop"

$code = @'
using System;
using System.Runtime.InteropServices;
public class WinTest {
    [DllImport("user32.dll")] public static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
    public static void Click(IntPtr hwnd, int x, int y) {
        IntPtr lParam = (IntPtr)((y << 16) | (x & 0xFFFF));
        SendMessage(hwnd, 0x0201, (IntPtr)1, lParam); // WM_LBUTTONDOWN
        System.Threading.Thread.Sleep(80);
        SendMessage(hwnd, 0x0202, IntPtr.Zero, lParam); // WM_LBUTTONUP
    }
}
'@
Add-Type -TypeDefinition $code -ErrorAction SilentlyContinue
Add-Type -AssemblyName UIAutomationClient

$root = [System.Windows.Automation.AutomationElement]::RootElement
$nameCond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::NameProperty, "Clovent Business Operating System")
$main = $root.FindFirst([System.Windows.Automation.TreeScope]::Children, $nameCond)
if (!$main) { throw "Main window not found" }

$ribbonCond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::NameProperty, "The Ribbon")
$ribbon = $main.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $ribbonCond)
if (!$ribbon) { throw "Ribbon not found" }

$reportsCond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::NameProperty, "Reports")
$reportsTab = $ribbon.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $reportsCond)
if (!$reportsTab) { throw "Reports tab not found" }

$rTab = $reportsTab.Current.BoundingRectangle
$rRib = $ribbon.Current.BoundingRectangle
$relX = [int]($rTab.X + $rTab.Width / 2 - $rRib.X)
$relY = [int]($rTab.Y + $rTab.Height / 2 - $rRib.Y)

Write-Host "Ribbon HWND: $($ribbon.Current.NativeWindowHandle)"
Write-Host "Clicking Reports tab at relative ($relX, $relY)..."
[WinTest]::Click([IntPtr]$ribbon.Current.NativeWindowHandle, $relX, $relY)
Start-Sleep -Seconds 1

& "Tools\ScreenCaptureUtil\bin\Release\net10.0-windows\ScreenCaptureUtil.exe" title "Clovent Business Operating System" "qa\test_reports_click.png"
Write-Host "Captured test_reports_click.png"
