. "d:\Clovent Business Operating System\qa\v3_common.ps1"

# Locate recall dialog (may be at 2850x1700 now)
[QaV3.E]::FindMain($procId)
$dlg = [IntPtr]::Zero
$dlgRect = $null
foreach ($o in [QaV3.E]::Owned($procId)) {
    $r = New-Object QaV3.RECT
    [QaV3.E]::GetWindowRect($o, [ref]$r) | Out-Null
    $w = $r.R - $r.L; $h = $r.B - $r.T
    if ($w -gt 2000 -and $h -gt 1200 -and $w -lt 3040) { $dlg = $o; $dlgRect = $r; break }
}
if ($dlg -eq [IntPtr]::Zero) { Log "ERROR: recall dialog not open"; exit 1 }
Log ("dialog at (" + $dlgRect.L + "," + $dlgRect.T + ") " + ($dlgRect.R - $dlgRect.L) + "x" + ($dlgRect.B - $dlgRect.T))
[QaV3.E]::SetForegroundWindow($dlg) | Out-Null
Start-Sleep -Milliseconds 700

# We are on HELD tab (returned in phase 5). Search field: first ~2100px of search row.
$searchX = $dlgRect.L + 800
$searchY = $dlgRect.T + 145
ClickXY $searchX $searchY 500
[System.Windows.Forms.SendKeys]::SendWait("^a")
Start-Sleep -Milliseconds 200
[System.Windows.Forms.SendKeys]::SendWait("ORD-92")
Start-Sleep -Seconds 2
Capture-H $dlg "v3_w6_search_ord92.png" "search ORD-92"

# read the count label text
$el = [System.Windows.Automation.AutomationElement]::FromHandle($dlg)
foreach ($c in $el.FindAll([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.Condition]::TrueCondition)) {
    if ($c.Current.Name -match 'order(s)? found') { Log ("COUNT: '" + $c.Current.Name + "'") }
}
foreach ($c in $el.FindAll([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.Condition]::TrueCondition)) {
    if ($c.Current.Name -match 'Selected:') { Log ("PREVIEW: '" + $c.Current.Name + "'") }
}

# Clear search via the header Clear button
foreach ($c in $el.FindAll([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.Condition]::TrueCondition)) {
    if ($c.Current.Name -eq ([char]0x2715 + " Clear")) {
        $rc = $c.Current.BoundingRectangle
        ClickXY ([int]($rc.X + $rc.Width/2)) ([int]($rc.Y + $rc.Height/2)) 2000
        Log "clicked Clear"
        break
    }
}
Capture-H $dlg "v3_w6_search_cleared.png" "search cleared"
$el = [System.Windows.Automation.AutomationElement]::FromHandle($dlg)
foreach ($c in $el.FindAll([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.Condition]::TrueCondition)) {
    if ($c.Current.Name -match 'order(s)? found') { Log ("COUNT after clear: '" + $c.Current.Name + "'") }
}

# Re-search ORD-92 to isolate the row (auto-focus single result)
ClickXY $searchX $searchY 500
[System.Windows.Forms.SendKeys]::SendWait("^a"); Start-Sleep -Milliseconds 200
[System.Windows.Forms.SendKeys]::SendWait("ORD-92")
Start-Sleep -Seconds 2

# Click Recall Order button
$el = [System.Windows.Automation.AutomationElement]::FromHandle($dlg)
$btn = $null
foreach ($c in $el.FindAll([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.Condition]::TrueCondition)) {
    if ($c.Current.Name -eq 'Recall Order') { $btn = $c; break }
}
if (-not $btn) { Log "ERROR: Recall Order button not found"; exit 1 }
$rc = $btn.Current.BoundingRectangle
Log ("Recall Order btn enabled: " + $btn.Current.IsEnabled)
ClickXY ([int]($rc.X + $rc.Width/2)) ([int]($rc.Y + $rc.Height/2)) 3000

# Dialog should be closed now; check POS state
[QaV3.E]::FindMain($procId)
$still = [QaV3.E]::Owned($procId) | Where-Object { $_ -eq $dlg }
Log ("dialog still open: " + [bool]$still)
Start-Sleep -Seconds 2
Guard-Modals
Log ("order pane after recall: '" + (Get-OrderPaneText) + "'")
Capture-Pos "v3_w6_afterrecall.png" "after recall ORD-92"
Dump-Pos "v3_w6_afterrecall_uia.txt"
Log "=== PHASE 6 COMPLETE ==="
