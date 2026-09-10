. "d:\Clovent Business Operating System\qa\v3_common.ps1"

[QaV3.E]::FindMain($procId)
$cands = @()
foreach ($o in [QaV3.E]::Owned($procId)) {
    $r = New-Object QaV3.RECT
    [QaV3.E]::GetWindowRect($o, [ref]$r) | Out-Null
    $w = $r.R - $r.L; $h = $r.B - $r.T
    if ($w -gt 2000 -and $h -gt 1200) { $cands += ,@($o, $w, $h) }
}
# dialog = the one whose size is smaller (not the shadow wrapper)
$dlg = [IntPtr]::Zero
foreach ($c in $cands) { if ($c[1] -lt 3040) { $dlg = [IntPtr]$c[0]; Log ("dialog: " + $c[0] + " " + $c[1] + "x" + $c[2]) } }
if ($dlg -eq [IntPtr]::Zero) { Log "ERROR no dialog"; exit 1 }

$el = [System.Windows.Automation.AutomationElement]::FromHandle($dlg)
Log ("root name: '" + $el.Current.Name + "'")
$sb = New-Object System.Text.StringBuilder
function DumpR($e2, $depth) {
    foreach ($child in $e2.FindAll([System.Windows.Automation.TreeScope]::Children, [System.Windows.Automation.Condition]::TrueCondition)) {
        $n = $child.Current.Name
        if ($n -ne "" -and $n.Length -lt 200) {
            $ct = $child.Current.ControlType.ProgrammaticName -replace 'ControlType.',''
            $rc = $child.Current.BoundingRectangle
            $sb.AppendLine($("  " * $depth) + $ct + " '" + ($n -replace "`r`n"," / ") + "' [" + [int]$rc.Width + "x" + [int]$rc.Height + "@" + [int]$rc.X + "," + [int]$rc.Y + "]") | Out-Null
        }
        if ($depth -lt 11) { DumpR $child ($depth + 1) }
    }
}
DumpR $el 0
$sb.ToString() | Out-File "$QaDir\v3_w4_recall_uia.txt" -Encoding utf8
Log ("dump lines: " + (Get-Content "$QaDir\v3_w4_recall_uia.txt").Count)
