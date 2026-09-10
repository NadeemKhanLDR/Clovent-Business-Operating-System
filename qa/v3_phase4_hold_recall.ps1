. "d:\Clovent Business Operating System\qa\v3_common.ps1"

Guard-Modals
$pos = GetPos
if (-not $pos) { Log "ERROR no POS"; exit 1 }
[QaV3.E]::SetForegroundWindow([IntPtr]$pos.Current.NativeWindowHandle) | Out-Null
Start-Sleep -Milliseconds 600

Log ("before hold: " + (Get-OrderPaneText))

# Hold button center from UIA dump: [329x75@2830,2110]
ClickXY 2994 2147 2500
Guard-Modals
Log ("after hold: '" + (Get-OrderPaneText) + "'")
Capture-Pos "v3_w4_afterhold.png" "after hold"
Dump-Pos "v3_w4_afterhold_uia.txt"

# check rail top cards
$rail = Select-String -Path "$QaDir\v3_w4_afterhold_uia.txt" -Pattern "ORD-92" | Select-Object -First 3
foreach ($m in $rail) { Log ("rail: " + $m.Line.Trim()) }

# Recall button [327x75@3163,2110]
Log "clicking Recall..."
ClickXY 3326 2147 1500
$recall = $null
for ($i = 0; $i -lt 20; $i++) {
    Start-Sleep -Milliseconds 500
    $recall = Get-AnyWindow "Recall / Sales History"
    if ($recall) { break }
}
if (-not $recall) { Log "ERROR: recall dialog did not open"; exit 1 }
Start-Sleep -Seconds 3
$rHwnd = [IntPtr]$recall.Current.NativeWindowHandle
[QaV3.E]::SetForegroundWindow($rHwnd) | Out-Null
Start-Sleep -Milliseconds 900
Capture-H $rHwnd "v3_w4_recall_held.png" "recall dialog HELD native"

# dump recall tree
$sb = New-Object System.Text.StringBuilder
function DumpR($el, $depth) {
    foreach ($child in $el.FindAll([System.Windows.Automation.TreeScope]::Children, [System.Windows.Automation.Condition]::TrueCondition)) {
        $n = $child.Current.Name
        if ($n -ne "" -and $n.Length -lt 150) {
            $ct = $child.Current.ControlType.ProgrammaticName -replace 'ControlType.',''
            $rc = $child.Current.BoundingRectangle
            $sb.AppendLine($("  " * $depth) + $ct + " '" + $n + "' [" + [int]$rc.Width + "x" + [int]$rc.Height + "@" + [int]$rc.X + "," + [int]$rc.Y + "]") | Out-Null
        }
        if ($depth -lt 11) { DumpR $child ($depth + 1) }
    }
}
DumpR $recall 0
$sb.ToString() | Out-File "$QaDir\v3_w4_recall_uia.txt" -Encoding utf8
Log "recall dump => v3_w4_recall_uia.txt"
Log "=== PHASE 4 COMPLETE ==="
