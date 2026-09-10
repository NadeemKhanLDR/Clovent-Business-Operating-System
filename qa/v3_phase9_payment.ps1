. "d:\Clovent Business Operating System\qa\v3_common.ps1"

# --- A: check ORD-92 in Recall VOIDED tab ---
Guard-Modals
[QaV3.E]::FindMain($procId)
[QaV3.E]::SetForegroundWindow([QaV3.E]::Main) | Out-Null
Start-Sleep -Milliseconds 700
ClickXY 3326 2147 2000   # Recall
Start-Sleep -Seconds 3
[QaV3.E]::FindMain($procId)
$dlg = [IntPtr]::Zero
foreach ($o in [QaV3.E]::Owned($procId)) {
    $r = New-Object QaV3.RECT
    [QaV3.E]::GetWindowRect($o, [ref]$r) | Out-Null
    $w = $r.R - $r.L; $h = $r.B - $r.T
    if ($w -gt 2000 -and $h -gt 1200 -and $w -lt 3040) { $dlg = $o; break }
}
if ($dlg -ne [IntPtr]::Zero) {
    [QaV3.E]::SetForegroundWindow($dlg) | Out-Null
    Start-Sleep -Seconds 2
    $el = [System.Windows.Automation.AutomationElement]::FromHandle($dlg)
    foreach ($c in $el.FindAll([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.Condition]::TrueCondition)) {
        if ($c.Current.Name -eq 'VOIDED') {
            $rc = $c.Current.BoundingRectangle
            ClickXY ([int]($rc.X + $rc.Width/2)) ([int]($rc.Y + $rc.Height/2)) 2500
            Log "clicked VOIDED tab"
            break
        }
    }
    Capture-H $dlg "v3_w9_recall_voided_ord92.png" "VOIDED with ORD-92"
    # close via footer Close button
    $el = [System.Windows.Automation.AutomationElement]::FromHandle($dlg)
    foreach ($c in $el.FindAll([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.Condition]::TrueCondition)) {
        if ($c.Current.Name -eq 'Close' -and $c.Current.ControlType.ProgrammaticName -ne 'ControlType.Button') {
            $rc = $c.Current.BoundingRectangle
            if ($rc.Y -gt 1500) {
                ClickXY ([int]($rc.X + $rc.Width/2)) ([int]($rc.Y + $rc.Height/2)) 1500
                Log "closed recall"
                break
            }
        }
    }
    Start-Sleep -Seconds 1
} else { Log "WARN recall dialog not found" }
Guard-Modals

# --- B: new Take Away order + 1 item ---
[QaV3.E]::SetForegroundWindow([QaV3.E]::Main) | Out-Null
Start-Sleep -Milliseconds 700
ClickXY 546 116 2500          # + Take Away
Log ("order: '" + (Get-OrderPaneText) + "'")
ClickXY 915 873 1500          # Chicken Biryani 450
Start-Sleep -Seconds 1
Guard-Modals
Capture-Pos "v3_w9_neworder.png" "new order with 1 item"

# --- C: tender 500 ---
ClickXY 3226 1785 400         # AMOUNT TENDERED field
[System.Windows.Forms.SendKeys]::SendWait("^a"); Start-Sleep -Milliseconds 150
[System.Windows.Forms.SendKeys]::SendWait("500")
Start-Sleep -Seconds 2
Capture-Pos "v3_w9_tender500.png" "tendered 500"
Dump-Pos "v3_w9_tender500_uia.txt"

# --- D: Record Payment ---
[QaV3.E]::SetForegroundWindow([QaV3.E]::Main) | Out-Null
Start-Sleep -Milliseconds 500
ClickXY 3008 2230 3000        # Record Payment
Guard-Modals
Start-Sleep -Seconds 2
Log ("after payment order: '" + (Get-OrderPaneText) + "'")
Capture-Pos "v3_w9_paid.png" "after record payment"
Dump-Pos "v3_w9_paid_uia.txt"
Log "=== PHASE 9 COMPLETE ==="
