. "d:\Clovent Business Operating System\qa\v3_common.ps1"

# Find the owned modal dialog by EnumWindows (reliable), then capture + dump via FromHandle
[QaV3.E]::FindMain($procId)
$dlg = [IntPtr]::Zero
foreach ($o in [QaV3.E]::Owned($procId)) {
    $r = New-Object QaV3.RECT
    [QaV3.E]::GetWindowRect($o, [ref]$r) | Out-Null
    $w = $r.R - $r.L; $h = $r.B - $r.T
    if ($w -gt 1000 -and $h -gt 800) { $dlg = $o; Log ("dialog hwnd $o ${w}x${h}") }
}
if ($dlg -eq [IntPtr]::Zero) { Log "ERROR: no large dialog found"; exit 1 }

[QaV3.E]::SetForegroundWindow($dlg) | Out-Null
Start-Sleep -Milliseconds 800
Capture-H $dlg "v3_w4_recall_held.png" "recall dialog HELD native"

$el = [System.Windows.Automation.AutomationElement]::FromHandle($dlg)
$sb = New-Object System.Text.StringBuilder
function DumpR($e2, $depth) {
    foreach ($child in $e2.FindAll([System.Windows.Automation.TreeScope]::Children, [System.Windows.Automation.Condition]::TrueCondition)) {
        $n = $child.Current.Name
        if ($n -ne "" -and $n.Length -lt 200) {
            $ct = $child.Current.ControlType.ProgrammaticName -replace 'ControlType.',''
            $rc = $child.Current.BoundingRectangle
            $sb.AppendLine($("  " * $depth) + $ct + " '" + $n + "' [" + [int]$rc.Width + "x" + [int]$rc.Height + "@" + [int]$rc.X + "," + [int]$rc.Y + "]") | Out-Null
        }
        if ($depth -lt 11) { DumpR $child ($depth + 1) }
    }
}
DumpR $el 0
$sb.ToString() | Out-File "$QaDir\v3_w4_recall_uia.txt" -Encoding utf8
Log "recall dump => v3_w4_recall_uia.txt"
Log "=== PHASE 4B COMPLETE ==="
