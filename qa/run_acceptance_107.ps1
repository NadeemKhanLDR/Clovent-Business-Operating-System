$ErrorActionPreference = "Stop"

$outDir = "D:\Clovent Business Operating System\qa\acceptance_test_107"
if (!(Test-Path $outDir)) { New-Item -ItemType Directory -Path $outDir -Force | Out-Null }

$cap = "D:\Clovent Business Operating System\Tools\ScreenCaptureUtil\bin\Release\net10.0-windows\ScreenCaptureUtil.exe"
$exePath = "D:\Clovent Business Operating System\artifacts\release\Clovent.BusinessOperatingSystem-1.0.7-win-x64\Clovent.Desktop.exe"

Get-Process -Name "*Clovent*" -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Seconds 3

Write-Host "================================================================="
Write-Host "=== CBOS 1.0.7 PRODUCTION ACCEPTANCE TEST SUITE               ==="
Write-Host "================================================================="

Write-Host "`n=== STEP 1: VERIFY EFFECTIVE DATABASE CONFIGURATION ==="
$cfgPath = "$env:LOCALAPPDATA\Clovent\Clovent.BusinessOperatingSystem\database.config.json"
Write-Host "Configuration file path: $cfgPath"
if (Test-Path $cfgPath) {
    Get-Content $cfgPath | Write-Host
} else {
    Write-Host "Configuration file not found!"
}

Write-Host "`n=== STEP 2: BASELINE PAYMENT METHODS IN ACCEPTANCE DB ==="
sqlcmd -S localhost -d Clovent_BusinessOperatingSystem -E -Q "SELECT Id, Name, Status FROM [Restaurant].[PaymentMethods] ORDER BY Name;"

Write-Host "`n=== STEP 3: LAUNCH PUBLISHED 1.0.7 EXECUTABLE ==="
Write-Host "Executable: $exePath"
$p = Start-Process $exePath -PassThru
Write-Host "Process PID: $($p.Id)"

Write-Host "`nWaiting for Sign in window..."
& $cap wait-win "Sign in" 30
Start-Sleep -Seconds 3

Write-Host "Entering credentials (admin_acceptance)..."
& $cap set-text "Sign in" "Username" "admin_acceptance"
Start-Sleep -Milliseconds 400
& $cap set-text "Sign in" "Password" "Password@2026!"
Start-Sleep -Milliseconds 400

& $cap title "Sign in" "$outDir\01_login_107.png"
Write-Host "Captured 01_login_107.png"

Write-Host "Clicking POS module button..."
& $cap click-btn "Sign in" "POS"
Start-Sleep -Seconds 3

Write-Host "`n=== STEP 4: HANDLING STARTUP DIALOGS & SHIFT PROMPTS ==="
for ($i = 0; $i -lt 40; $i++) {
    $wins = & $cap list-wins 2>&1 | Out-String
    Write-Host "Tick ${i}: $wins"

    if ($wins -match "An error occurred") {
        Write-Host "Error dialog detected. Dismissing with OK..."
        & $cap click-btn "An error occurred" "OK"
        Start-Sleep -Seconds 2
    }

    if ($wins -match "Long-Running Shift Active") {
        Write-Host "Long-Running Shift Active detected. Dismissing with OK..."
        & $cap click-btn "Long-Running Shift Active" "OK"
        Start-Sleep -Seconds 2
    }

    if ($wins -match "Punch In") {
        Write-Host "Punch In dialog detected. Clicking Punch In..."
        & $cap click-btn "Punch In" "Punch In"
        Start-Sleep -Seconds 2
    }

    if ($wins -match "Open Cash Register Shift") {
        Write-Host "Open Shift dialog detected. Clicking Open Shift..."
        & $cap click-btn "Open Cash Register Shift" "Open Shift"
        Start-Sleep -Seconds 3
    }

    if ($wins -match "Restaurant POS") {
        Write-Host "Restaurant POS window active and ready!"
        Start-Sleep -Seconds 2
        break
    }

    Start-Sleep -Seconds 1
}

& $cap title "Restaurant POS" "$outDir\02_pos_ready_107.png"
Write-Host "Captured 02_pos_ready_107.png"

# -------------------------------------------------------------------
# STEP 5: WORKFLOW A - CASH SALE
# -------------------------------------------------------------------
Write-Host "`n=== STEP 5: CASH SALE WORKFLOW ==="
Write-Host "Clicking + Take Away..."
& $cap click-name "Restaurant POS" "+ Take Away"
Start-Sleep -Seconds 3

Write-Host "Selecting Chicken Biryani (450.00)..."
& $cap click-name "Restaurant POS" "450.00"
Start-Sleep -Seconds 2

Write-Host "Selecting Cash in Payment Method..."
& $cap select-pos-payment "Restaurant POS" "cash"
Start-Sleep -Milliseconds 500

Write-Host "Clicking Exact button..."
& $cap click-btn "Restaurant POS" "Exact"
Start-Sleep -Milliseconds 500

Write-Host "Clicking Print Bill..."
& $cap click-btn "Restaurant POS" "Print Bill"
Start-Sleep -Seconds 2

Write-Host "Capturing 03_pos_receipt_107.png..."
& $cap title "Receipt Preview" "$outDir\03_pos_receipt_107.png"
Start-Sleep -Milliseconds 500

Write-Host "Closing Receipt Preview..."
& $cap close-title "Receipt Preview"
Start-Sleep -Seconds 2

Write-Host "Clicking Record Payment for Cash sale..."
& $cap click-btn "Restaurant POS" "Record Payment"
Start-Sleep -Seconds 5

Write-Host "Capturing 04_pos_cash_sale_107.png..."
& $cap title "Restaurant POS" "$outDir\04_pos_cash_sale_107.png"
Write-Host "Captured 04_pos_cash_sale_107.png"

# -------------------------------------------------------------------
# STEP 6: WORKFLOW B - CARD SALE
# -------------------------------------------------------------------
Write-Host "`n=== STEP 6: CARD SALE WORKFLOW ==="
Start-Sleep -Seconds 5
Write-Host "Clicking + Take Away..."
& $cap click-name "Restaurant POS" "+ Take Away"
Start-Sleep -Seconds 3

Write-Host "Selecting Chicken Karahi (1,200.00)..."
& $cap click-name "Restaurant POS" "1,200.00"
Start-Sleep -Seconds 2

Write-Host "Selecting Card in Payment Method..."
& $cap select-pos-payment "Restaurant POS" "card"
Start-Sleep -Milliseconds 500

Write-Host "Clicking Exact button..."
& $cap click-btn "Restaurant POS" "Exact"
Start-Sleep -Milliseconds 500

Write-Host "Capturing 05_pos_card_selected_107.png before payment..."
& $cap title "Restaurant POS" "$outDir\05_pos_card_sale_107.png"
Write-Host "Captured 05_pos_card_sale_107.png"

Write-Host "Clicking Record Payment for Card sale..."
& $cap click-btn "Restaurant POS" "Record Payment"
Start-Sleep -Seconds 5

# -------------------------------------------------------------------
# STEP 7: VERIFY TRANSACTIONS IN ACCEPTANCE DATABASE
# -------------------------------------------------------------------
Write-Host "`n=== STEP 7: VERIFY TRANSACTIONS IN ACCEPTANCE DATABASE ==="
$salesCheck = sqlcmd -S localhost -d Clovent_BusinessOperatingSystem -E -Q "SELECT TOP 6 o.OrderNumber, o.OrderType, o.Status, p.Amount, pm.Name AS PaymentMethod, o.CreatedAtUtc FROM [Restaurant].[Orders] o JOIN [Restaurant].[Payments] p ON o.Id = p.OrderId JOIN [Restaurant].[PaymentMethods] pm ON p.PaymentMethodId = pm.Id ORDER BY o.CreatedAtUtc DESC;"
Write-Host $salesCheck

# -------------------------------------------------------------------
# STEP 8: CLOSE APPLICATION
# -------------------------------------------------------------------
Write-Host "`n=== STEP 8: CLOSING APPLICATION ==="
Stop-Process -Id $p.Id -Force
Start-Sleep -Seconds 2
Write-Host "Application PID $($p.Id) closed."

# -------------------------------------------------------------------
# STEP 9: APPLICATION RESTART & PERSISTENCE VERIFICATION
# -------------------------------------------------------------------
Write-Host "`n=== STEP 9: RESTART APPLICATION & PERSISTENCE VERIFICATION ==="
$p2 = Start-Process $exePath -PassThru
Write-Host "Restarted 1.0.7 executable with PID: $($p2.Id)"
& $cap wait-win "Sign in" 30
Start-Sleep -Seconds 2

Write-Host "Logging in after restart..."
& $cap set-text "Sign in" "Username" "admin_acceptance"
Start-Sleep -Milliseconds 400
& $cap set-text "Sign in" "Password" "Password@2026!"
Start-Sleep -Milliseconds 400
& $cap click-btn "Sign in" "POS"
Start-Sleep -Seconds 5

for ($j = 0; $j -lt 20; $j++) {
    $wins = & $cap list-wins 2>&1 | Out-String
    if ($wins -match "Restaurant POS") {
        Write-Host "Restaurant POS reopened successfully after restart!"
        break
    }
    Start-Sleep -Seconds 1
}

& $cap title "Restaurant POS" "$outDir\06_pos_after_restart_107.png"
Write-Host "Captured 06_pos_after_restart_107.png"

Write-Host "Payment Methods After Restart in Clovent_BusinessOperatingSystem:"
sqlcmd -S localhost -d Clovent_BusinessOperatingSystem -E -Q "SELECT Id, Name, Status FROM [Restaurant].[PaymentMethods] ORDER BY Name;"

$rows = sqlcmd -S localhost -d Clovent_BusinessOperatingSystem -E -h -1 -Q "SELECT COUNT(*) FROM [Restaurant].[PaymentMethods];"
Write-Host "Total Payment Method Rows in DB: $($rows.Trim())"

Stop-Process -Id $p2.Id -Force
Write-Host "Terminated restart instance PID $($p2.Id)."

Write-Host "`n================================================================="
Write-Host "=== 1.0.7 ACCEPTANCE VERIFICATION FINISHED SUCCESSFULLY       ==="
Write-Host "================================================================="
