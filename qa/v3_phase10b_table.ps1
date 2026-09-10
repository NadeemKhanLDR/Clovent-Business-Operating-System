. "d:\Clovent Business Operating System\qa\v3_common.ps1"

# Table popup may have auto-closed; if so, reopen via + Dine In
[QaV3.E]::FindMain($procId)
[QaV3.E]::SetForegroundWindow([QaV3.E]::Main) | Out-Null
Start-Sleep -Milliseconds 600
[QaV3.E]::FindMain($procId)
$popup = [IntPtr]::Zero
foreach ($o in [QaV3.E]::Owned($procId)) {
    $r = New-Object QaV3.RECT
    [QaV3.E]::GetWindowRect($o, [ref]$r) | Out-Null
    if (($r.R-$r.L) -gt 200 -and ($r.R-$r.L) -lt 600 -and ($r.B-$r.T) -gt 100 -and ($r.B-$r.T) -lt 400) { $popup = $o; break }
}
if ($popup -eq [IntPtr]::Zero) {
    Log "popup closed - reopening via + Dine In"
    ClickXY 356 115 1500
    [QaV3.E]::FindMain($procId)
    foreach ($o in [QaV3.E]::Owned($procId)) {
        $r = New-Object QaV3.RECT
        [QaV3.E]::GetWindowRect($o, [ref]$r) | Out-Null
        if (($r.R-$r.L) -gt 200 -and ($r.R-$r.L) -lt 600 -and ($r.B-$r.T) -gt 100 -and ($r.B-$r.T) -lt 400) { $popup = $o; break }
    }
}
if ($popup -eq [IntPtr]::Zero) { Log "ERROR: no table popup"; exit 1 }

$r2 = New-Object QaV3.RECT
[QaV3.E]::GetWindowRect($popup, [ref]$r2) | Out-Null
Log ("popup at (" + $r2.L + "," + $r2.T + ") " + ($r2.R-$r2.L) + "x" + ($r2.B-$r2.T))

# T-02 is second item: header ~40px then items ~48px each at 250%
$ty = $r2.T + 40 + 48 + 24
ClickXY ([int]($r2.L + ($r2.R-$r2.L)/2)) ([int]$ty) 3000
Log "clicked T-02"

Log ("order: '" + (Get-OrderPaneText) + "'")
Dump-Pos "v3_w10b_dinein_uia.txt"
$tableLine = (Select-String -Path "$QaDir\v3_w10b_dinein_uia.txt" -Pattern "Table No|Table:" | Select-Object -First 3)
foreach ($m in $tableLine) { Log ("table: " + $m.Line.Trim()) }
Capture-Pos "v3_w10b_dinein_order.png" "dine in order on T-02"
Log "=== PHASE 10B COMPLETE ==="
