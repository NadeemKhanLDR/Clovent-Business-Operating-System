. "d:\Clovent Business Operating System\qa\v3_common.ps1"

Guard-Modals
[QaV3.E]::FindMain($procId)
[QaV3.E]::SetForegroundWindow([QaV3.E]::Main) | Out-Null
Start-Sleep -Milliseconds 700

# Active Orders rail dropdown: pane at [477x54@125,194] - click center
ClickXY 363 221 1500
Start-Sleep -Seconds 1
Capture-Pos "v3_w11_rail_dropdown.png" "active orders dropdown"

# enumerate popup items
[QaV3.E]::FindMain($procId)
foreach ($o in [QaV3.E]::Owned($procId)) {
    $r = New-Object QaV3.RECT
    [QaV3.E]::GetWindowRect($o, [ref]$r) | Out-Null
    Log ("popup: " + $o + " @(" + $r.L + "," + $r.T + ") " + ($r.R-$r.L) + "x" + ($r.B-$r.T))
    $el = [System.Windows.Automation.AutomationElement]::FromHandle($o)
    foreach ($c in $el.FindAll([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.Condition]::TrueCondition)) {
        $n = $c.Current.Name
        if ($n -ne '') { Log ("  item: '" + $n + "'") }
    }
}
# close by ESC
[QaV3.E]::SetForegroundWindow([QaV3.E]::Main) | Out-Null
[System.Windows.Forms.SendKeys]::SendWait("{ESC}")
Start-Sleep -Milliseconds 500
Log "=== PHASE 11 COMPLETE ==="
