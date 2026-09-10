. "d:\Clovent Business Operating System\qa\v3_common.ps1"

# settle: wait until no owned dialogs remain
for ($i = 0; $i -lt 10; $i++) {
    [QaV3.E]::FindMain($procId)
    $owned = @([QaV3.E]::Owned($procId))
    if ($owned.Count -eq 0) { break }
    Start-Sleep -Milliseconds 800
}
Start-Sleep -Seconds 2
[QaV3.E]::SetForegroundWindow([QaV3.E]::Main) | Out-Null
Start-Sleep -Milliseconds 900

ClickXY 546 116 2500     # + Take Away
$order = Get-OrderPaneText
Log ("order: '$order'")
if ($order -notmatch '^ORD-') {
    Start-Sleep -Seconds 2
    ClickXY 546 116 2500
    $order = Get-OrderPaneText
    Log ("retry order: '$order'")
    if ($order -notmatch '^ORD-') { Log "ERROR: no order"; exit 1 }
}

ClickXY 1987 873 1500    # Salad 30
Guard-Modals
Start-Sleep -Seconds 1
Dump-Pos "v3_inv_hold_uia.txt"
$cnt = (Select-String -Path "$QaDir\v3_inv_hold_uia.txt" -Pattern "Ordered Items").Line
Log ("cart: " + $cnt.Trim())

ClickXY 2994 2147 3000   # Hold
Guard-Modals
Log ("after hold: '" + (Get-OrderPaneText) + "'")
Capture-Pos "v3_inv_afterhold.png" "hold regression"

# Recall it back
ClickXY 3326 2147 2000
Start-Sleep -Seconds 3
[QaV3.E]::FindMain($procId)
$dlg = [IntPtr]::Zero
foreach ($o in [QaV3.E]::Owned($procId)) {
    $r = New-Object QaV3.RECT
    [QaV3.E]::GetWindowRect($o, [ref]$r) | Out-Null
    $w = $r.R - $r.L; $h = $r.B - $r.T
    if ($w -gt 2000 -and $h -gt 1200 -and $w -lt 3040) { $dlg = $o; break }
}
if ($dlg -eq [IntPtr]::Zero) { Log "ERROR: no recall dialog"; exit 1 }
[QaV3.E]::SetForegroundWindow($dlg) | Out-Null
Start-Sleep -Seconds 2
$el = [System.Windows.Automation.AutomationElement]::FromHandle($dlg)
foreach ($c in $el.FindAll([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.Condition]::TrueCondition)) {
    if ($c.Current.Name -match 'order(s)? found') { Log ("HELD: '" + $c.Current.Name + "'") }
}
# focus first row: click in grid area (first data row)
$r2 = New-Object QaV3.RECT
[QaV3.E]::GetWindowRect($dlg, [ref]$r2) | Out-Null
ClickXY ([int]($r2.L + 600)) ([int]($r2.T + 700)) 2000
# click Recall Order
$el = [System.Windows.Automation.AutomationElement]::FromHandle($dlg)
foreach ($c in $el.FindAll([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.Condition]::TrueCondition)) {
    if ($c.Current.Name -eq 'Recall Order') {
        $rc = $c.Current.BoundingRectangle
        ClickXY ([int]($rc.X + $rc.Width/2)) ([int]($rc.Y + $rc.Height/2)) 3000
        Log "clicked Recall Order"
        break
    }
}
Start-Sleep -Seconds 2
Guard-Modals
Log ("after recall: '" + (Get-OrderPaneText) + "'")
Dump-Pos "v3_inv_afterrecall_uia.txt"
Capture-Pos "v3_inv_afterrecall.png" "recall regression restored"
Log "=== INVOICE PHASE 4 COMPLETE ==="
