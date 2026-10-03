$ErrorActionPreference = "Stop"

$cap = "Tools\ScreenCaptureUtil\bin\Release\net10.0-windows\ScreenCaptureUtil.exe"

$code = @'
using System;
using System.Runtime.InteropServices;
public class WinHelper {
    [DllImport("user32.dll")] public static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, int dwExtraInfo);
    [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);

    public static void ClickHwnd(IntPtr hwnd) {
        SendMessage(hwnd, 0x0201, (IntPtr)1, IntPtr.Zero);
        System.Threading.Thread.Sleep(60);
        SendMessage(hwnd, 0x0202, IntPtr.Zero, IntPtr.Zero);
    }

    public static void ClickCoord(int x, int y) {
        SetCursorPos(x, y);
        System.Threading.Thread.Sleep(60);
        mouse_event(0x0002, 0, 0, 0, 0); // DOWN
        System.Threading.Thread.Sleep(60);
        mouse_event(0x0004, 0, 0, 0, 0); // UP
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
        [WinHelper]::ClickHwnd([IntPtr]$e.Current.NativeWindowHandle)
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
if (!$posWin) { throw "Restaurant POS window not found" }
Write-Host "Restaurant POS window active! HWND=$($posWin.Current.NativeWindowHandle)"
Start-Sleep -Seconds 2

# Helper function to find element in posWin
function Get-PosEl($name, $id = $null) {
    $desc = $posWin.FindAll([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.Condition]::TrueCondition)
    foreach ($el in $desc) {
        if ($id -and $el.Current.AutomationId -eq $id) { return $el }
        if ($name -and $el.Current.Name -eq $name) { return $el }
    }
    return $null
}

# -------------------------------------------------------------
# WORKFLOW A: CASH SALE
# -------------------------------------------------------------
Write-Host "`n=== STARTING CASH SALE ==="
$btnTakeAway = Get-PosEl "+ Take Away" "_newTakeAwayButton"
if (!$btnTakeAway) { throw "+ Take Away button not found" }
Write-Host "Clicking + Take Away (HWND=$($btnTakeAway.Current.NativeWindowHandle))..."
[WinHelper]::ClickHwnd([IntPtr]$btnTakeAway.Current.NativeWindowHandle)
Start-Sleep -Seconds 2

Write-Host "Clicking Chicken Biryani..."
$itemBiryani = Get-PosEl "Chicken Biryani" "ProductTileName"
if (!$itemBiryani) { throw "Chicken Biryani tile not found" }
$rect = $itemBiryani.Current.BoundingRectangle
[WinHelper]::ClickCoord([int]($rect.X + $rect.Width/2), [int]($rect.Y + $rect.Height/2))
Start-Sleep -Seconds 2

# Select Cash in Payment Method
Write-Host "Selecting Cash payment method..."
$comboPm = Get-PosEl "PAYMENT METHOD"
if ($comboPm) {
    $r = $comboPm.Current.BoundingRectangle
    # Click combo to focus
    [WinHelper]::ClickCoord([int]($r.X + 50), [int]($r.Y + $r.Height/2))
    Start-Sleep -Milliseconds 300
    # Type 'Cash' then enter or down arrow
    & $cap key "Restaurant POS" "c"
    Start-Sleep -Milliseconds 200
    & $cap key "Restaurant POS" "enter"
    Start-Sleep -Milliseconds 500
}

# Click Exact button
Write-Host "Clicking Exact button..."
$btnExact = Get-PosEl "Exact" "_exactAmountButton"
if ($btnExact) {
    [WinHelper]::ClickHwnd([IntPtr]$btnExact.Current.NativeWindowHandle)
    Start-Sleep -Milliseconds 500
}

# Verify receipt before completing: Click Print Bill
Write-Host "Clicking Print Bill..."
$btnPrint = Get-PosEl "Print Bill"
if ($btnPrint) {
    [WinHelper]::ClickHwnd([IntPtr]$btnPrint.Current.NativeWindowHandle)
    Start-Sleep -Seconds 2
    
    # Check for Receipt Preview window
    $previewWin = $null
    $wins = $root.FindAll([System.Windows.Automation.TreeScope]::Children, $procCond)
    foreach ($w in $wins) {
        if ($w.Current.Name -like "*Receipt*" -or $w.Current.Name -like "*Preview*") {
            $previewWin = $w
            break
        }
    }
    if ($previewWin) {
        Write-Host "Found Receipt Preview window! Capturing 12_pos_receipt.png..."
        & $cap title "Receipt" "qa\acceptance_test_103\12_pos_receipt.png"
        Start-Sleep -Milliseconds 500
        # Close receipt preview window
        & $cap key "Receipt" "esc"
        Start-Sleep -Seconds 1
    } else {
        Write-Host "Receipt Preview window not detected directly, checking title..."
        & $cap title "Preview" "qa\acceptance_test_103\12_pos_receipt.png"
    }
}

# Record Payment
Write-Host "Clicking Record Payment..."
$btnRecord = Get-PosEl "Record Payment" "_recordButton"
if ($btnRecord) {
    [WinHelper]::ClickHwnd([IntPtr]$btnRecord.Current.NativeWindowHandle)
    Start-Sleep -Seconds 3
    Write-Host "Payment recorded! Capturing 11_pos_cash_sale.png..."
    & $cap title "Restaurant POS" "qa\acceptance_test_103\11_pos_cash_sale.png"
}

# -------------------------------------------------------------
# WORKFLOW B: CARD SALE
# -------------------------------------------------------------
Write-Host "`n=== STARTING CARD SALE ==="
$btnTakeAway = Get-PosEl "+ Take Away" "_newTakeAwayButton"
Write-Host "Clicking + Take Away..."
[WinHelper]::ClickHwnd([IntPtr]$btnTakeAway.Current.NativeWindowHandle)
Start-Sleep -Seconds 2

Write-Host "Clicking Naan..."
$itemNaan = Get-PosEl "Naan" "ProductTileName"
if ($itemNaan) {
    $rect = $itemNaan.Current.BoundingRectangle
    [WinHelper]::ClickCoord([int]($rect.X + $rect.Width/2), [int]($rect.Y + $rect.Height/2))
    Start-Sleep -Seconds 2
}

# Select Card in Payment Method
Write-Host "Selecting Card payment method..."
$comboPm = Get-PosEl "PAYMENT METHOD"
if ($comboPm) {
    $r = $comboPm.Current.BoundingRectangle
    [WinHelper]::ClickCoord([int]($r.X + 50), [int]($r.Y + $r.Height/2))
    Start-Sleep -Milliseconds 300
    & $cap key "Restaurant POS" "down"
    Start-Sleep -Milliseconds 200
    & $cap key "Restaurant POS" "enter"
    Start-Sleep -Milliseconds 500
}

# Click Exact button
Write-Host "Clicking Exact button..."
$btnExact = Get-PosEl "Exact" "_exactAmountButton"
if ($btnExact) {
    [WinHelper]::ClickHwnd([IntPtr]$btnExact.Current.NativeWindowHandle)
    Start-Sleep -Milliseconds 500
}

# Capture Card sale pending tender
Write-Host "Capturing 13a_pos_card_sale_pending.png..."
& $cap title "Restaurant POS" "qa\acceptance_test_103\13a_pos_card_sale_pending.png"

# Record Payment
Write-Host "Clicking Record Payment for Card sale..."
$btnRecord = Get-PosEl "Record Payment" "_recordButton"
if ($btnRecord) {
    [WinHelper]::ClickHwnd([IntPtr]$btnRecord.Current.NativeWindowHandle)
    Start-Sleep -Seconds 3
    Write-Host "Card Payment recorded! Capturing 13_pos_card_sale.png..."
    & $cap title "Restaurant POS" "qa\acceptance_test_103\13_pos_card_sale.png"
}

Write-Host "`nCash and Card sales workflows completed successfully!"
