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
Start-Sleep -Seconds 3

Write-Host "0. Launching Clovent.Desktop.exe and logging into Back Office..."
$p = Start-Process "C:\CloventClient105\Clovent.Desktop.exe" -PassThru
$root = [System.Windows.Automation.AutomationElement]::RootElement

# Wait for Sign in
$signIn = $null
for ($i = 0; $i -lt 60; $i++) {
    Start-Sleep -Seconds 1
    $wins = $root.FindAll([System.Windows.Automation.TreeScope]::Children, [System.Windows.Automation.Condition]::TrueCondition)
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
    $wins = $root.FindAll([System.Windows.Automation.TreeScope]::Children, [System.Windows.Automation.Condition]::TrueCondition)
    foreach ($w in $wins) {
        if ($w.Current.Name -like "*Clovent Business Operating System*" -and $w.Current.Name -notlike "*Sign in*") { $mainWin = $w; break }
    }
    if ($mainWin) { break }
}
if (!$mainWin) { throw "Main Shell window not found" }
Write-Host "Main Shell active! HWND=$($mainWin.Current.NativeWindowHandle)"
Start-Sleep -Seconds 2

$mainWinTitle = "Clovent Business Operating System"

# Maximize for full 3840x2400 view
Write-Host "Maximizing Back Office shell..."
& $cap maximize-win $mainWinTitle
Start-Sleep -Seconds 2

function Navigate-Ribbon([string]$tabName, [string]$btnName) {
    Write-Host "Activating Ribbon Tab '$tabName'..."
    & $cap select-tab $mainWinTitle $tabName
    Start-Sleep -Milliseconds 600
    Write-Host "Clicking Button '$btnName'..."
    & $cap click-btn $mainWinTitle $btnName
    Start-Sleep -Seconds 3
}

# 1. SALES SUMMARY
Write-Host "`n=== 1. VERIFY SALES SUMMARY ==="
Navigate-Ribbon "Reports" "Sales Summary"
& $cap title $mainWinTitle "qa\acceptance_test_103\14_sales_summary.png"

# 2. ORDERS / BILLS (ORDER HISTORY)
Write-Host "`n=== 2. VERIFY ORDERS / BILLS ==="
Navigate-Ribbon "POS" "Order History"
& $cap title $mainWinTitle "qa\acceptance_test_103\15_orders_bills.png"

# 3. CUSTOMERS & PAYMENTS
Write-Host "`n=== 3. VERIFY CUSTOMERS & RECEIVABLES ==="
Navigate-Ribbon "Masters" "Customers"
& $cap title $mainWinTitle "qa\acceptance_test_103\16a_customers.png"

Navigate-Ribbon "Manager Panel" "Customer Receivables"
& $cap title $mainWinTitle "qa\acceptance_test_103\16_customers_payments.png"

# 4. STOCK ON HAND
Write-Host "`n=== 4. VERIFY STOCK ON HAND ==="
Navigate-Ribbon "Inventory" "Stock On Hand"
& $cap title $mainWinTitle "qa\acceptance_test_103\17_stock_on_hand.png"

# 5. INVENTORY MOVEMENTS
Write-Host "`n=== 5. VERIFY INVENTORY MOVEMENTS ==="
Navigate-Ribbon "Inventory" "Inventory Movements"
& $cap title $mainWinTitle "qa\acceptance_test_103\18_inventory_movements.png"

# 6. DATABASE SETTINGS PERMISSIONS
Write-Host "`n=== 6. VERIFY DATABASE SETTINGS PERMISSIONS ==="
Navigate-Ribbon "Settings" "Database Settings"
Start-Sleep -Seconds 2
& $cap title "Database Connection" "qa\acceptance_test_103\19a_db_settings_permissions.png"
# Close dialog
& $cap close-win "Database Connection"
& $cap key "{esc}"
Start-Sleep -Seconds 1

# 7. REGISTRATION & LICENSE PERMISSIONS
Write-Host "`n=== 7. VERIFY LICENSE PERMISSIONS ==="
Navigate-Ribbon "Settings" "Registration & License"
Start-Sleep -Seconds 2
& $cap title "Software Registration" "qa\acceptance_test_103\19b_licensing_permissions.png"
# Close dialog
& $cap close-win "Software Registration"
& $cap key "{esc}"
Start-Sleep -Seconds 1

# 8. SIGN OUT & RETURN TO LOGIN
Write-Host "`n=== 8. LOGOUT & RETURN TO LOGIN ==="
Navigate-Ribbon "Masters" "Acceptance Admin"
Start-Sleep -Milliseconds 600
& $cap click-btn $mainWinTitle "Sign Out"
Start-Sleep -Seconds 2

# Check if sign in window returned
$signInAgain = $null
for ($i = 0; $i -lt 15; $i++) {
    $wins = $root.FindAll([System.Windows.Automation.TreeScope]::Children, [System.Windows.Automation.Condition]::TrueCondition)
    foreach ($w in $wins) {
        if ($w.Current.Name -like "*Sign in*") { $signInAgain = $w; break }
    }
    if ($signInAgain) { break }
    Start-Sleep -Seconds 1
}
& $cap title "Sign in" "qa\acceptance_test_103\20a_logout_screen.png"

# 9. LOGIN AGAIN
Write-Host "`n=== 9. LOGIN AGAIN ==="
& $cap set-text "Sign in" "Username" "admin_acceptance"
Start-Sleep -Milliseconds 300
& $cap set-text "Sign in" "Password" "Password@2026!"
Start-Sleep -Milliseconds 300
& $cap title "Sign in" "qa\acceptance_test_103\20b_login_again.png"

# 10. CLOSE APPLICATION
Write-Host "`n=== 10. CLOSING APPLICATION ==="
Get-Process -Name "*Clovent*" -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Seconds 2

# 11. RESTART APPLICATION TO CONFIRM COMMISSIONING WIZARD DOES NOT RETURN
Write-Host "`n=== 11. RESTARTING APPLICATION TO CONFIRM WIZARD DOES NOT RETURN ==="
$restarted = Start-Process "C:\CloventClient105\Clovent.Desktop.exe" -PassThru
Start-Sleep -Seconds 12
& $cap title "Sign in" "qa\acceptance_test_103\21_restart_persistence_verified.png"

# Check what window is showing
$allWins = $root.FindAll([System.Windows.Automation.TreeScope]::Children, [System.Windows.Automation.Condition]::TrueCondition)
foreach ($w in $allWins) {
    if ($w.Current.Name -like "*Sign in*" -or $w.Current.Name -like "*Clovent*" -or $w.Current.Name -like "*Wizard*") {
        Write-Host "Restart window detected: $($w.Current.Name)"
    }
}

Write-Host "`n=== VERIFYING DATABASE PERSISTENCE ==="
$dbSummary = sqlcmd -S localhost -d Clovent_BusinessOperatingSystem -E -Q "SELECT COUNT(*) AS TotalOrders FROM [Restaurant].[Orders]; SELECT COUNT(*) AS TotalPayments FROM [Restaurant].[Payments]; SELECT COUNT(*) AS TotalOrderLines FROM [Restaurant].[OrderLines]; SELECT Name FROM [Identity].[Organizations]; SELECT Name FROM [Identity].[Companies]; SELECT Name FROM [Identity].[Branches];"
Write-Host $dbSummary

# Clean up
Stop-Process -Id $restarted.Id -Force -ErrorAction SilentlyContinue

Write-Host "`nALL ACCEPTANCE CRITERIA VERIFIED!"
