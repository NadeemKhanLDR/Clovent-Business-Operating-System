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
        Write-Host "Found POS element. Clicking..."
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
Write-Host "Restaurant POS active!"
Start-Sleep -Seconds 2

# WORKFLOW: CARD SALE
Write-Host "`n=== EXECUTING CARD SALE ==="
Write-Host "Clicking _newTakeAwayButton..."
& $cap click-id "Restaurant POS" "_newTakeAwayButton"
Start-Sleep -Seconds 2

Write-Host "Clicking Naan (25.00)..."
& $cap click-name "Restaurant POS" "Naan"
Start-Sleep -Seconds 2

Write-Host "Clicking payment method dropdown arrow button at (3785, 1705)..."
& $cap click-xy 3785 1705
Start-Sleep -Milliseconds 400

Write-Host "Sending Home then Enter to select first item (Card)..."
& $cap key "Restaurant POS" "home"
Start-Sleep -Milliseconds 200
& $cap key "Restaurant POS" "enter"
Start-Sleep -Milliseconds 400

Write-Host "Clicking Exact button..."
& $cap click-btn "Restaurant POS" "Exact"
Start-Sleep -Milliseconds 500

Write-Host "Capturing 13a_pos_card_sale_pending.png..."
& $cap title "Restaurant POS" "qa\acceptance_test_103\13a_pos_card_sale_pending.png"

Write-Host "Clicking Record Payment for Card sale..."
& $cap click-btn "Restaurant POS" "Record Payment"
Start-Sleep -Seconds 3

Write-Host "Capturing 13_pos_card_sale.png..."
& $cap title "Restaurant POS" "qa\acceptance_test_103\13_pos_card_sale.png"

Write-Host "`n=== CHECKING CARD SALE IN DATABASE ==="
$checkCard = sqlcmd -S localhost -d Clovent_BusinessOperatingSystem -E -Q "SELECT TOP 1 o.OrderNumber, o.OrderType, o.Status, p.Amount, pm.Name AS PaymentMethod FROM [Restaurant].[Orders] o JOIN [Restaurant].[Payments] p ON o.Id = p.OrderId JOIN [Restaurant].[PaymentMethods] pm ON p.PaymentMethodId = pm.Id ORDER BY o.CreatedAtUtc DESC;"
Write-Host $checkCard

Write-Host "`nCard sale execution complete!"
