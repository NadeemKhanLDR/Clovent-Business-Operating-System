. "d:\Clovent Business Operating System\qa\v3_common.ps1"

Guard-Modals
[QaV3.E]::FindMain($procId)
[QaV3.E]::SetForegroundWindow([QaV3.E]::Main) | Out-Null
Start-Sleep -Milliseconds 700
Log ("order: '" + (Get-OrderPaneText) + "'")

$pos = GetPos
$cancelBtn = $null
foreach ($el in $pos.FindAll([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.Condition]::TrueCondition)) {
    if ($el.Current.Name -eq 'Cancel Order') { $cancelBtn = $el; break }
}
if (-not $cancelBtn) { Log "ERROR: no Cancel Order button"; exit 1 }
$rc = $cancelBtn.Current.BoundingRectangle
ClickXY ([int]($rc.X + $rc.Width/2)) ([int]($rc.Y + $rc.Height/2)) 2500

# find prompt hwnd (the inner one, not shadow)
[QaV3.E]::FindMain($procId)
$tp = [IntPtr]::Zero
foreach ($o in [QaV3.E]::Owned($procId)) {
    $r = New-Object QaV3.RECT
    [QaV3.E]::GetWindowRect($o, [ref]$r) | Out-Null
    $w = $r.R - $r.L; $h = $r.B - $r.T
    if ($w -gt 400 -and $w -lt 800 -and $h -gt 200 -and $h -lt 400) {
        if ($tp -eq [IntPtr]::Zero -or $w -lt 700) { $tp = $o }
        Log ("prompt: " + $o + " " + $w + "x" + $h + " @(" + $r.L + "," + $r.T + ")")
    }
}
if ($tp -eq [IntPtr]::Zero) { Log "ERROR: no prompt"; exit 1 }
$r2 = New-Object QaV3.RECT
[QaV3.E]::GetWindowRect($tp, [ref]$r2) | Out-Null
[QaV3.E]::SetForegroundWindow($tp) | Out-Null
Start-Sleep -Milliseconds 800
Capture-H $tp "v3_w8b_prompt.png" "cancel prompt open"

# OK button at relative (320,268) of prompt origin
ClickXY ($r2.L + 320) ($r2.T + 268) 3500
Log "clicked OK"

# verify cancelled
Guard-Modals
Log ("order after confirm: '" + (Get-OrderPaneText) + "'")
Capture-Pos "v3_w8b_cancelled.png" "after cancel confirmed"
Dump-Pos "v3_w8b_cancelled_uia.txt"
$rail = Select-String -Path "$QaDir\v3_w8b_cancelled_uia.txt" -Pattern "ORD-92" | Select-Object -First 2
foreach ($m in $rail) { Log ("rail: " + $m.Line.Trim()) }
Log "=== PHASE 8B COMPLETE ==="
