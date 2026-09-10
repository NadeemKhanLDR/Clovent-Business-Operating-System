. "d:\Clovent Business Operating System\qa\v3_common.ps1"

Guard-Modals
$pos = GetPos
if (-not $pos) { Log "ERROR no POS"; exit 1 }
[QaV3.E]::SetForegroundWindow([QaV3.E]::Main) | Out-Null
Start-Sleep -Milliseconds 700
Log ("before clear: '" + (Get-OrderPaneText) + "'")

# Clear button [331x75@3494,2110]
ClickXY 3659 2147 2500
Guard-Modals
Log ("after clear: '" + (Get-OrderPaneText) + "'")
Capture-Pos "v3_w7_afterclear.png" "after clear"
Dump-Pos "v3_w7_afterclear_uia.txt"

$orderedLine = (Select-String -Path "$QaDir\v3_w7_afterclear_uia.txt" -Pattern "Ordered Items").Line
Log ("cart counter: " + $orderedLine.Trim())

# Re-open Recall to verify ORD-92 still exists in HELD
ClickXY 3326 2147 1500
Start-Sleep -Seconds 3
[QaV3.E]::FindMain($procId)
$dlg = [IntPtr]::Zero
foreach ($o in [QaV3.E]::Owned($procId)) {
    $r = New-Object QaV3.RECT
    [QaV3.E]::GetWindowRect($o, [ref]$r) | Out-Null
    $w = $r.R - $r.L; $h = $r.B - $r.T
    if ($w -gt 2000 -and $h -gt 1200 -and $w -lt 3040) { $dlg = $o; break }
}
if ($dlg -eq [IntPtr]::Zero) { Log "ERROR: recall dialog did not re-open"; exit 1 }
[QaV3.E]::SetForegroundWindow($dlg) | Out-Null
Start-Sleep -Seconds 2
Capture-H $dlg "v3_w7_recall_again.png" "recall after clear"

$el = [System.Windows.Automation.AutomationElement]::FromHandle($dlg)
foreach ($c in $el.FindAll([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.Condition]::TrueCondition)) {
    $n = $c.Current.Name
    if ($n -match 'order(s)? found') { Log ("COUNT: '" + $n + "'") }
    if ($n -match 'Selected:') { Log ("PREVIEW: '" + $n + "'") }
}

# select ORD-92 via search and recall again
$r2 = New-Object QaV3.RECT
[QaV3.E]::GetWindowRect($dlg, [ref]$r2) | Out-Null
ClickXY ($r2.L + 800) ($r2.T + 145) 500
[System.Windows.Forms.SendKeys]::SendWait("^a"); Start-Sleep -Milliseconds 200
[System.Windows.Forms.SendKeys]::SendWait("ORD-92")
Start-Sleep -Seconds 2
$el = [System.Windows.Automation.AutomationElement]::FromHandle($dlg)
foreach ($c in $el.FindAll([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.Condition]::TrueCondition)) {
    if ($c.Current.Name -eq 'Recall Order') {
        $rc = $c.Current.BoundingRectangle
        ClickXY ([int]($rc.X + $rc.Width/2)) ([int]($rc.Y + $rc.Height/2)) 3000
        Log "clicked Recall Order (2nd time)"
        break
    }
}
Start-Sleep -Seconds 2
Guard-Modals
Log ("order pane after 2nd recall: '" + (Get-OrderPaneText) + "'")
Capture-Pos "v3_w7_afterrecall2.png" "after 2nd recall"
Dump-Pos "v3_w7_afterrecall2_uia.txt"
Log "=== PHASE 7 COMPLETE ==="
