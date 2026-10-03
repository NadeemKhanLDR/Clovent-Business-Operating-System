$ErrorActionPreference = "Stop"
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName System.Windows.Forms
$cap = "Tools\ScreenCaptureUtil\bin\Release\net10.0-windows\ScreenCaptureUtil.exe"

Get-Process -Name "*Clovent*" -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Seconds 2

$p = Start-Process "C:\CloventClient105\Clovent.Desktop.exe" -PassThru
$root = [System.Windows.Automation.AutomationElement]::RootElement

# Wait for Sign in
$signIn = $null
for ($i = 0; $i -lt 40; $i++) {
    Start-Sleep -Seconds 1
    $wins = $root.FindAll([System.Windows.Automation.TreeScope]::Children, [System.Windows.Automation.Condition]::TrueCondition)
    foreach ($w in $wins) {
        if ($w.Current.Name -like "*Sign in*") { $signIn = $w; break }
    }
    if ($signIn) { break }
}
& $cap set-text "Sign in" "Username" "admin_acceptance"
& $cap set-text "Sign in" "Password" "Password@2026!"
& $cap click-btn "Sign in" "BACK OFFICE"
Start-Sleep -Seconds 4

# Find Main Window
$main = $null
for ($i = 0; $i -lt 30; $i++) {
    Start-Sleep -Seconds 1
    $wins = $root.FindAll([System.Windows.Automation.TreeScope]::Children, [System.Windows.Automation.Condition]::TrueCondition)
    foreach ($w in $wins) {
        if ($w.Current.Name -like "*Clovent Business Operating System*" -and $w.Current.Name -notlike "*Sign in*") {
            $main = $w
            break
        }
    }
    if ($main) { break }
}

# Select Reports tab
& $cap select-tab "Clovent Business Operating System" "Reports"
Start-Sleep -Seconds 2

# Search all child HWNDs under Main Window
$mainHwnd = [IntPtr]$main.Current.NativeWindowHandle
Write-Host "Main HWND: $mainHwnd"

# EnumChildWindows
$code = @'
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
public class HwndFinder {
    [DllImport("user32.dll")] public static extern bool EnumChildWindows(IntPtr hWndParent, EnumWindowsProc lpEnumFunc, IntPtr lParam);
    public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
    public static List<IntPtr> GetChildren(IntPtr parent) {
        var list = new List<IntPtr>();
        EnumChildWindows(parent, (h, l) => { list.Add(h); return true; }, IntPtr.Zero);
        return list;
    }
}
'@
Add-Type -TypeDefinition $code -ErrorAction SilentlyContinue

$children = [HwndFinder]::GetChildren($mainHwnd)
Write-Host "Found $($children.Count) child windows."
foreach ($ch in $children) {
    try {
        $el = [System.Windows.Automation.AutomationElement]::FromHandle($ch)
        if ($el) {
            $desc = $el.FindAll([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.Condition]::TrueCondition)
            foreach ($d in $desc) {
                if ($d.Current.Name -like "*Sales*" -or $d.Current.Name -like "*Order*" -or $d.Current.Name -like "*Summary*") {
                    Write-Host "CHILD HWND $ch -> DESC: '$($d.Current.Name)' TYPE: $($d.Current.ControlType.ProgrammaticName) RECT: $($d.Current.BoundingRectangle)"
                }
            }
        }
    } catch {}
}

Stop-Process -Id $p.Id -Force
