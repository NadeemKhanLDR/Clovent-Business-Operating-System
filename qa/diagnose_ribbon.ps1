$ErrorActionPreference = "Stop"

$cap = "Tools\ScreenCaptureUtil\bin\Release\net10.0-windows\ScreenCaptureUtil.exe"

$code = @'
using System;
using System.Runtime.InteropServices;
public class WinHelper {
    [DllImport("user32.dll")] public static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
    public static void ClickHwnd(IntPtr hwnd) {
        SendMessage(hwnd, 0x0201, (IntPtr)1, IntPtr.Zero);
        System.Threading.Thread.Sleep(80);
        SendMessage(hwnd, 0x0202, IntPtr.Zero, IntPtr.Zero);
    }
}
'@
Add-Type -TypeDefinition $code -ErrorAction SilentlyContinue
Add-Type -AssemblyName UIAutomationClient

Get-Process -Name "*Clovent*" -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Seconds 1

Write-Host "1. Launching Clovent.Desktop.exe..."
$p = Start-Process "C:\CloventClient105\Clovent.Desktop.exe" -PassThru
$root = [System.Windows.Automation.AutomationElement]::RootElement
$procCond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ProcessIdProperty, $p.Id)

# Wait for Sign in
$signIn = $null
for ($i = 0; $i -lt 30; $i++) {
    Start-Sleep -Seconds 1
    $wins = $root.FindAll([System.Windows.Automation.TreeScope]::Children, $procCond)
    foreach ($w in $wins) {
        if ($w.Current.Name -like "*Sign in*") { $signIn = $w; break }
    }
    if ($signIn) { break }
}
if (!$signIn) { throw "Sign In window not found" }
Write-Host "Found Sign In window: $($signIn.Current.Name)"

# Fill credentials
& $cap set-text "Sign in" "Username" "admin_acceptance"
Start-Sleep -Milliseconds 400
& $cap set-text "Sign in" "Password" "Password@2026!"
Start-Sleep -Milliseconds 400

# Click BACK OFFICE module
$all = $signIn.FindAll([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.Condition]::TrueCondition)
foreach ($e in $all) {
    if ($e.Current.Name -eq "BACK OFFICE") {
        Write-Host "Found BACK OFFICE element. Clicking..."
        [WinHelper]::ClickHwnd([IntPtr]$e.Current.NativeWindowHandle)
        break
    }
}

# Wait for Main Shell window
$mainWin = $null
for ($i = 0; $i -lt 30; $i++) {
    Start-Sleep -Seconds 1
    $wins = $root.FindAll([System.Windows.Automation.TreeScope]::Children, $procCond)
    foreach ($w in $wins) {
        if ($w.Current.Name -like "*Clovent Business Operating System*" -and $w.Current.Name -notlike "*Sign in*") { $mainWin = $w; break }
    }
    if ($mainWin) { break }
}
if (!$mainWin) { throw "Main Shell window not found" }
Write-Host "Main Shell active! HWND=$($mainWin.Current.NativeWindowHandle)"
Start-Sleep -Seconds 2

$mainWinTitle = "Clovent Business Operating System"

# Dump elements before clicking tab
Write-Host "Selecting tab 'Reports'..."
& $cap select-tab $mainWinTitle "Reports"
Start-Sleep -Seconds 1

Write-Host "Dumping elements..."
& $cap dump $mainWinTitle > "qa\diagnose_dump.txt"

# Capture using title
& $cap title $mainWinTitle "qa\diagnose_reports.png"
Write-Host "Captured diagnose_reports.png"
