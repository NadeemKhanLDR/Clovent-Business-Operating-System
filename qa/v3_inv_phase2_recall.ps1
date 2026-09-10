. "d:\Clovent Business Operating System\qa\v3_common.ps1"

Guard-Modals
[QaV3.E]::FindMain($procId)
[QaV3.E]::SetForegroundWindow([QaV3.E]::Main) | Out-Null
Start-Sleep -Milliseconds 700

ClickXY 3326 2147 2000     # Recall
Start-Sleep -Seconds 3
[QaV3.E]::FindMain($procId)
$dlg = [IntPtr]::Zero
foreach ($o in [QaV3.E]::Owned($procId)) {
    $r = New-Object QaV3.RECT
    [QaV3.E]::GetWindowRect($o, [ref]$r) | Out-Null
    $w = $r.R - $r.L; $h = $r.B - $r.T
    if ($w -gt 2000 -and $h -gt 1200 -and $w -lt 3040) { $dlg = $o; break }
}
if ($dlg -eq [IntPtr]::Zero) { Log "ERROR: recall dialog did not open"; exit 1 }
[QaV3.E]::SetForegroundWindow($dlg) | Out-Null
Start-Sleep -Seconds 2

$el = [System.Windows.Automation.AutomationElement]::FromHandle($dlg)
foreach ($c in $el.FindAll([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.Condition]::TrueCondition)) {
    if ($c.Current.Name -eq 'CLOSED') {
        $rc = $c.Current.BoundingRectangle
        ClickXY ([int]($rc.X + $rc.Width/2)) ([int]($rc.Y + $rc.Height/2)) 2500
        Log "clicked CLOSED"
        break
    }
}
Capture-H $dlg "v3_inv_recall_closed.png" "recall CLOSED with ORD-94/95"

$el = [System.Windows.Automation.AutomationElement]::FromHandle($dlg)
foreach ($c in $el.FindAll([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.Condition]::TrueCondition)) {
    if ($c.Current.Name -match 'order(s)? found') { Log ("COUNT: '" + $c.Current.Name + "'") }
}

# Select ORD-95 (search) and check preview shows invoice
$r2 = New-Object QaV3.RECT
[QaV3.E]::GetWindowRect($dlg, [ref]$r2) | Out-Null
ClickXY ($r2.L + 800) ($r2.T + 145) 400
[System.Windows.Forms.SendKeys]::SendWait("ORD-95")
Start-Sleep -Seconds 2
Capture-H $dlg "v3_inv_recall_ord95.png" "recall search ORD-95"
$el = [System.Windows.Automation.AutomationElement]::FromHandle($dlg)
foreach ($c in $el.FindAll([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.Condition]::TrueCondition)) {
    $n = $c.Current.Name
    if ($n -match 'Selected:|order(s)? found') { Log ("PREVIEW: '" + $n + "'") }
}

# Read-only View test on the selected closed order
foreach ($c in $el.FindAll([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.Condition]::TrueCondition)) {
    if ($c.Current.Name -eq 'View') {
        $rc = $c.Current.BoundingRectangle
        ClickXY ([int]($rc.X + $rc.Width/2)) ([int]($rc.Y + $rc.Height/2)) 2500
        Log "clicked View"
        break
    }
}
# read-only viewer window should appear - capture it
Start-Sleep -Seconds 2
[QaV3.E]::FindMain($procId)
foreach ($o in [QaV3.E]::Owned($procId)) {
    if ($o -eq $dlg) { continue }
    $r = New-Object QaV3.RECT
    [QaV3.E]::GetWindowRect($o, [ref]$r) | Out-Null
    $w = $r.R - $r.L; $h = $r.B - $r.T
    if ($w -gt 400 -and $h -gt 300) {
        Log ("viewer: " + $o + " " + $w + "x" + $h)
        Capture-H $o "v3_inv_readonly_viewer.png" "read-only viewer ORD-95"
        [QaV3.E]::PostMessage($o, 0x0010, [IntPtr]0, [IntPtr]0) | Out-Null
    }
}
Start-Sleep -Seconds 1
Log "=== INVOICE PHASE 2 COMPLETE ==="
