. "d:\Clovent Business Operating System\qa\v3_common.ps1"

# Wait for POS window
$pos = $null
for ($i = 0; $i -lt 60; $i++) {
    Start-Sleep -Seconds 1
    $pos = GetPos
    if ($pos) { break }
}
if (-not $pos) { Log "ERROR: POS did not start"; exit 1 }
Start-Sleep -Seconds 5
[QaV3.E]::FindMain($procId)
[QaV3.E]::ShowWindow([QaV3.E]::Main, 3) | Out-Null
Start-Sleep -Seconds 2
[QaV3.E]::SetForegroundWindow([QaV3.E]::Main) | Out-Null
Start-Sleep -Seconds 1

function Complete-BiryaniOrder([string]$tag) {
    Guard-Modals
    [QaV3.E]::SetForegroundWindow([QaV3.E]::Main) | Out-Null
    Start-Sleep -Milliseconds 600
    ClickXY 546 116 2500       # + Take Away
    $order = Get-OrderPaneText
    Log ("[$tag] order: '$order'")
    ClickXY 915 873 1200       # Chicken Biryani 450
    Guard-Modals
    ClickXY 3226 1785 400      # AMOUNT TENDERED
    [System.Windows.Forms.SendKeys]::SendWait("^a"); Start-Sleep -Milliseconds 150
    [System.Windows.Forms.SendKeys]::SendWait("450")
    Start-Sleep -Milliseconds 1500
    [QaV3.E]::SetForegroundWindow([QaV3.E]::Main) | Out-Null
    Start-Sleep -Milliseconds 400
    ClickXY 3008 2230 3500     # Record Payment
    Guard-Modals
    Start-Sleep -Seconds 1
    Log ("[$tag] after payment: '" + (Get-OrderPaneText) + "'")
}

Complete-BiryaniOrder "A"
Complete-BiryaniOrder "B"
Capture-Pos "v3_inv_two_orders_done.png" "after two completed orders"
Log "=== INVOICE PHASE 1 COMPLETE ==="
