. "d:\Clovent Business Operating System\qa\v3_common.ps1"

Guard-Modals
[QaV3.E]::FindMain($procId)
[QaV3.E]::SetForegroundWindow([QaV3.E]::Main) | Out-Null
Start-Sleep -Milliseconds 800

# click + Dine In
ClickXY 356 115 1200
Log "clicked + Dine In"

# immediately enumerate owned popups + capture screen
[QaV3.E]::FindMain($procId)
$owned = [QaV3.E]::Owned($procId)
foreach ($o in $owned) {
    $r = New-Object QaV3.RECT
    [QaV3.E]::GetWindowRect($o, [ref]$r) | Out-Null
    Log ("popup: " + $o + " @(" + $r.L + "," + $r.T + ")-(" + $r.R + "," + $r.B + ") " + ($r.R-$r.L) + "x" + ($r.B-$r.T))
}
# full screen capture
Add-Type -AssemblyName System.Drawing
$bmp = New-Object System.Drawing.Bitmap(3840, 2400)
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.CopyFromScreen(0, 0, 0, 0, (New-Object System.Drawing.Size(3840, 2400)))
$g.Dispose()
$bmp.Save("$QaDir\v3_w10_dinein_popup.png", [System.Drawing.Imaging.ImageFormat]::Png)
$bmp.Dispose()
Log "screen captured"

# look for T-02 list items in any popup
$found = $false
foreach ($o in $owned) {
    $el = [System.Windows.Automation.AutomationElement]::FromHandle($o)
    foreach ($c in $el.FindAll([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.Condition]::TrueCondition)) {
        $n = $c.Current.Name
        if ($n -match '^T-0?\d') {
            Log ("popup item: '" + $n + "'")
            $found = $true
        }
    }
}
Log ("table items found: " + $found)
Log "=== PHASE 10 COMPLETE ==="
