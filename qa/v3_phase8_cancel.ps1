. "d:\Clovent Business Operating System\qa\v3_common.ps1"

Guard-Modals
[QaV3.E]::FindMain($procId)
[QaV3.E]::SetForegroundWindow([QaV3.E]::Main) | Out-Null
Start-Sleep -Milliseconds 700
Log ("order: '" + (Get-OrderPaneText) + "'")

# Cancel Order button in top bar (find by name)
$pos = GetPos
$cancelBtn = $null
foreach ($el in $pos.FindAll([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.Condition]::TrueCondition)) {
    if ($el.Current.Name -eq 'Cancel Order') { $cancelBtn = $el; break }
}
if (-not $cancelBtn) { Log "ERROR: Cancel Order button not found (order active?)"; exit 1 }
$rc = $cancelBtn.Current.BoundingRectangle
Log ("Cancel Order @ " + [int]$rc.X + "," + [int]$rc.Y)
ClickXY ([int]($rc.X + $rc.Width/2)) ([int]($rc.Y + $rc.Height/2)) 2500

# TextPromptForm should open - find it
[QaV3.E]::FindMain($procId)
$tp = [IntPtr]::Zero
foreach ($o in [QaV3.E]::Owned($procId)) {
    $r = New-Object QaV3.RECT
    [QaV3.E]::GetWindowRect($o, [ref]$r) | Out-Null
    $w = $r.R - $r.L; $h = $r.B - $r.T
    if ($w -gt 300 -and $w -lt 2000 -and $h -gt 200 -and $h -lt 1500) { $tp = $o; Log ("prompt hwnd " + $o + " " + $w + "x" + $h) }
}
if ($tp -eq [IntPtr]::Zero) { Log "ERROR: no cancel prompt appeared"; Capture-Pos "v3_w8_noprompt.png" "no prompt"; exit 1 }
[QaV3.E]::SetForegroundWindow($tp) | Out-Null
Start-Sleep -Milliseconds 800
Capture-H $tp "v3_w8_cancel_prompt.png" "cancel reason prompt"

# list its buttons
$el2 = [System.Windows.Automation.AutomationElement]::FromHandle($tp)
$cancelOpt = $null; $okOpt = $null
foreach ($c in $el2.FindAll([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.Condition]::TrueCondition)) {
    $n = $c.Current.Name
    if ($n -ne '') { Log ("prompt child: '" + $n + "'") }
    if ($n -eq 'Cancel') { $cancelOpt = $c }
    if ($n -in @('OK','Ok','Confirm','&OK')) { $okOpt = $c }
}

# 1) click CANCEL - order must remain
if ($cancelOpt) {
    $r2 = $cancelOpt.Current.BoundingRectangle
    ClickXY ([int]($r2.X + $r2.Width/2)) ([int]($r2.Y + $r2.Height/2)) 2000
    Log "clicked Cancel in prompt"
} else {
    Log "WARN no Cancel button; sending ESC"
    [System.Windows.Forms.SendKeys]::SendWait("{ESC}")
    Start-Sleep -Seconds 1
}
Log ("order after cancel-declined: '" + (Get-OrderPaneText) + "'")
Dump-Pos "v3_w8_declined_uia.txt"
$cnt = (Select-String -Path "$QaDir\v3_w8_declined_uia.txt" -Pattern "Ordered Items").Line
Log ("cart counter after decline: " + $cnt.Trim())

# 2) cancel again and CONFIRM
$pos = GetPos
$cancelBtn = $null
foreach ($el in $pos.FindAll([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.Condition]::TrueCondition)) {
    if ($el.Current.Name -eq 'Cancel Order') { $cancelBtn = $el; break }
}
if ($cancelBtn) {
    $rc = $cancelBtn.Current.BoundingRectangle
    ClickXY ([int]($rc.X + $rc.Width/2)) ([int]($rc.Y + $rc.Height/2)) 2500
}
[QaV3.E]::FindMain($procId)
$tp2 = [IntPtr]::Zero
foreach ($o in [QaV3.E]::Owned($procId)) {
    $r = New-Object QaV3.RECT
    [QaV3.E]::GetWindowRect($o, [ref]$r) | Out-Null
    $w = $r.R - $r.L; $h = $r.B - $r.T
    if ($w -gt 300 -and $w -lt 2000 -and $h -gt 200 -and $h -lt 1500) { $tp2 = $o }
}
if ($tp2 -eq [IntPtr]::Zero) { Log "ERROR: prompt did not reappear"; exit 1 }
[QaV3.E]::SetForegroundWindow($tp2) | Out-Null
Start-Sleep -Milliseconds 700
$el3 = [System.Windows.Automation.AutomationElement]::FromHandle($tp2)
$okOpt = $null
foreach ($c in $el3.FindAll([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.Condition]::TrueCondition)) {
    if ($c.Current.Name -in @('OK','Ok','Confirm','&OK')) { $okOpt = $c; break }
}
if ($okOpt) {
    $r3 = $okOpt.Current.BoundingRectangle
    ClickXY ([int]($r3.X + $r3.Width/2)) ([int]($r3.Y + $r3.Height/2)) 3000
    Log "clicked OK to confirm cancellation"
} else {
    Log "WARN no OK; sending ENTER"
    [System.Windows.Forms.SendKeys]::SendWait("{ENTER}")
    Start-Sleep -Seconds 2
}
Guard-Modals
Log ("order after cancel-confirmed: '" + (Get-OrderPaneText) + "'")
Capture-Pos "v3_w8_cancelled.png" "after cancel confirmed"
Dump-Pos "v3_w8_cancelled_uia.txt"
Log "=== PHASE 8 COMPLETE ==="
