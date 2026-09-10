. "d:\Clovent Business Operating System\qa\v3_common.ps1"

function Open-Recall {
    [QaV3.E]::SetForegroundWindow([QaV3.E]::Main) | Out-Null
    Start-Sleep -Milliseconds 600
    ClickXY 3326 2147 2000
    Start-Sleep -Seconds 3
    [QaV3.E]::FindMain($procId)
    foreach ($o in [QaV3.E]::Owned($procId)) {
        $r = New-Object QaV3.RECT
        [QaV3.E]::GetWindowRect($o, [ref]$r) | Out-Null
        $w = $r.R - $r.L; $h = $r.B - $r.T
        if ($w -gt 2000 -and $h -gt 1200 -and $w -lt 3040) { return $o }
    }
    return [IntPtr]::Zero
}
function Click-Dlg([IntPtr]$dlg, [string]$name) {
    $el = [System.Windows.Automation.AutomationElement]::FromHandle($dlg)
    foreach ($c in $el.FindAll([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.Condition]::TrueCondition)) {
        if ($c.Current.Name -eq $name) {
            $rc = $c.Current.BoundingRectangle
            ClickXY ([int]($rc.X + $rc.Width/2)) ([int]($rc.Y + $rc.Height/2)) 2000
            return $true
        }
    }
    return $false
}

# 1) HELD tab state
$dlg = Open-Recall
if ($dlg -eq [IntPtr]::Zero) { Log "ERROR no recall"; exit 1 }
[QaV3.E]::SetForegroundWindow($dlg) | Out-Null
Start-Sleep -Seconds 1
Capture-H $dlg "v3_inv_reg_held.png" "regression HELD"
$el = [System.Windows.Automation.AutomationElement]::FromHandle($dlg)
foreach ($c in $el.FindAll([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.Condition]::TrueCondition)) {
    if ($c.Current.Name -match 'order(s)? found') { Log ("HELD count: '" + $c.Current.Name + "'") }
    if ($c.Current.Name -match 'Selected:') { Log ("HELD preview: '" + $c.Current.Name + "'") }
}

# 2) OPEN tab
Click-Dlg $dlg "OPEN" | Out-Null
Capture-H $dlg "v3_inv_reg_open.png" "regression OPEN"
$el = [System.Windows.Automation.AutomationElement]::FromHandle($dlg)
foreach ($c in $el.FindAll([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.Condition]::TrueCondition)) {
    if ($c.Current.Name -match 'order(s)? found') { Log ("OPEN count: '" + $c.Current.Name + "'") }
}

# 3) VOIDED tab
Click-Dlg $dlg "VOIDED" | Out-Null
Capture-H $dlg "v3_inv_reg_voided.png" "regression VOIDED"

# close recall via footer Close
$el = [System.Windows.Automation.AutomationElement]::FromHandle($dlg)
foreach ($c in $el.FindAll([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.Condition]::TrueCondition)) {
    if ($c.Current.Name -eq 'Close') {
        $rc = $c.Current.BoundingRectangle
        if ($rc.Y -gt 1500) { ClickXY ([int]($rc.X + $rc.Width/2)) ([int]($rc.Y + $rc.Height/2)) 1500; Log "closed recall"; break }
    }
}
Start-Sleep -Seconds 1

# 4) Hold a fresh order: Salad 30
Guard-Modals
[QaV3.E]::SetForegroundWindow([QaV3.E]::Main) | Out-Null
Start-Sleep -Milliseconds 600
ClickXY 546 116 2500
Log ("hold-test order: '" + (Get-OrderPaneText) + "'")
ClickXY 1987 873 1200   # Salad tile price center (30.00)
Guard-Modals
Dump-Pos "v3_inv_reg_premeasure_uia.txt"
Log "=== check salad line added ==="
