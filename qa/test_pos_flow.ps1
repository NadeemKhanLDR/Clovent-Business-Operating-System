$ErrorActionPreference = "Stop"

$cap = "Tools\ScreenCaptureUtil\bin\Release\net10.0-windows\ScreenCaptureUtil.exe"

$code = @'
using System;
using System.Runtime.InteropServices;
public class WinHelper {
    [DllImport("user32.dll")] public static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
    public static void Click(IntPtr hwnd) {
        SendMessage(hwnd, 0x0201, (IntPtr)1, IntPtr.Zero);
        System.Threading.Thread.Sleep(60);
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
Write-Host "Found Sign In window: $($signIn.Current.Name)"

# Fill credentials
Write-Host "2. Entering credentials..."
& $cap set-text "Sign in" "Username" "admin_acceptance"
Start-Sleep -Milliseconds 400
& $cap set-text "Sign in" "Password" "Password@2026!"
Start-Sleep -Milliseconds 400

# Click POS module
Write-Host "3. Clicking POS module..."
$all = $signIn.FindAll([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.Condition]::TrueCondition)
foreach ($e in $all) {
    if ($e.Current.Name -eq "POS") {
        Write-Host "Found POS element: HWND=$($e.Current.NativeWindowHandle). Clicking..."
        [WinHelper]::Click([IntPtr]$e.Current.NativeWindowHandle)
        break
    }
}

# Wait for Restaurant POS main window
Write-Host "4. Waiting for Restaurant POS main window..."
$posWin = $null
for ($i = 0; $i -lt 30; $i++) {
    Start-Sleep -Seconds 1
    $wins = $root.FindAll([System.Windows.Automation.TreeScope]::Children, $procCond)
    foreach ($w in $wins) {
        if ($w.Current.Name -like "*Restaurant POS*") { $posWin = $w; break }
    }
    if ($posWin) { break }
}

if ($posWin) {
    Write-Host "Found Restaurant POS window! HWND=$($posWin.Current.NativeWindowHandle)"
    Start-Sleep -Seconds 2
    & $cap dump "Restaurant POS" > "qa\acceptance_test_103\pos_full_dump.txt"
    Write-Host "DUMP saved to qa\acceptance_test_103\pos_full_dump.txt"
    & $cap title "Restaurant POS" "qa\acceptance_test_103\pos_live_check.png"
    Write-Host "Captured qa\acceptance_test_103\pos_live_check.png"
} else {
    Write-Host "Restaurant POS window not found."
}
