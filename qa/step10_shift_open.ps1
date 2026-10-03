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

# Capture filled login form
& $cap title "Sign in" "qa\acceptance_test_103\09_login_form_filled.png"
Write-Host "Captured 09_login_form_filled.png"

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

# Wait for next window: Punch In or Open Shift
Write-Host "4. Waiting for Punch In or Shift Dialog..."
$punchWin = $null
$shiftWin = $null

for ($i = 0; $i -lt 20; $i++) {
    Start-Sleep -Seconds 1
    $wins = $root.FindAll([System.Windows.Automation.TreeScope]::Children, $procCond)
    foreach ($w in $wins) {
        if ($w.Current.Name -like "*Punch In*") { $punchWin = $w }
        if ($w.Current.Name -like "*Open Cash Register Shift*") { $shiftWin = $w }
    }
    if ($punchWin -or $shiftWin) { break }
}

if ($punchWin) {
    Write-Host "Found Punch In window: '$($punchWin.Current.Name)'. Capturing..."
    & $cap title "Punch In" "qa\acceptance_test_103\09b_punch_in.png"
    Write-Host "Captured 09b_punch_in.png"

    # Click Punch In button
    $punchBtn = $null
    foreach ($e in $punchWin.FindAll([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.Condition]::TrueCondition)) {
        if ($e.Current.Name -eq "Punch In" -and $e.Current.NativeWindowHandle -ne 0) {
            $punchBtn = $e
            break
        }
    }
    if ($punchBtn) {
        Write-Host "Clicking Punch In button (HWND=$($punchBtn.Current.NativeWindowHandle))..."
        [WinHelper]::Click([IntPtr]$punchBtn.Current.NativeWindowHandle)
    } else {
        Write-Host "Sending Enter to Punch In window..."
        & $cap key "Punch In" "enter"
    }
    Start-Sleep -Seconds 3
}

# Wait for Open Shift dialog
for ($i = 0; $i -lt 20; $i++) {
    Start-Sleep -Seconds 1
    $wins = $root.FindAll([System.Windows.Automation.TreeScope]::Children, $procCond)
    foreach ($w in $wins) {
        if ($w.Current.Name -like "*Open Cash Register Shift*") { $shiftWin = $w; break }
    }
    if ($shiftWin) { break }
}

if ($shiftWin) {
    Write-Host "Found Open Shift window: '$($shiftWin.Current.Name)'. Capturing 10_pos_shift_open.png..."
    & $cap title "Open Cash Register Shift" "qa\acceptance_test_103\10_pos_shift_open.png"
    Write-Host "Captured 10_pos_shift_open.png"

    # Set opening cash
    Write-Host "Typing opening float 100.00..."
    & $cap type "Open Cash Register Shift" "100"
    Start-Sleep -Milliseconds 500

    # Click Open Shift button
    $openBtn = $null
    foreach ($e in $shiftWin.FindAll([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.Condition]::TrueCondition)) {
        if ($e.Current.Name -eq "Open Shift" -and $e.Current.NativeWindowHandle -ne 0) {
            $openBtn = $e
            break
        }
    }
    if ($openBtn) {
        Write-Host "Clicking Open Shift button (HWND=$($openBtn.Current.NativeWindowHandle))..."
        [WinHelper]::Click([IntPtr]$openBtn.Current.NativeWindowHandle)
    } else {
        Write-Host "Sending Enter to Open Shift window..."
        & $cap key "Open Cash Register Shift" "enter"
    }
    Start-Sleep -Seconds 5
}

# Wait for Restaurant POS main window
Write-Host "5. Waiting for Restaurant POS main window..."
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
    Write-Host "Found Restaurant POS window: '$($posWin.Current.Name)'! Capturing..."
    Start-Sleep -Seconds 3
    & $cap title "Restaurant POS" "qa\acceptance_test_103\10b_pos_main_ready.png"
    Write-Host "Captured 10b_pos_main_ready.png"
} else {
    Write-Host "Restaurant POS window not found. Active windows:"
    $wins = $root.FindAll([System.Windows.Automation.TreeScope]::Children, $procCond)
    foreach ($w in $wins) { Write-Host "  Window: '$($w.Current.Name)'" }
}
