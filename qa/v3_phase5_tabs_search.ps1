. "d:\Clovent Business Operating System\qa\v3_common.ps1"

# Dialog must be open now - find it
[QaV3.E]::FindMain($procId)
$dlg = [IntPtr]::Zero
foreach ($o in [QaV3.E]::Owned($procId)) {
    $r = New-Object QaV3.RECT
    [QaV3.E]::GetWindowRect($o, [ref]$r) | Out-Null
    $w = $r.R - $r.L; $h = $r.B - $r.T
    if ($w -gt 2000 -and $h -gt 1200 -and $w -lt 3040) { $dlg = $o; break }
}
if ($dlg -eq [IntPtr]::Zero) { Log "ERROR: recall dialog not open"; exit 1 }
Log ("dialog hwnd: " + $dlg)
[QaV3.E]::SetForegroundWindow($dlg) | Out-Null
Start-Sleep -Milliseconds 700

function Click-ByName([string]$name) {
    $el = [System.Windows.Automation.AutomationElement]::FromHandle($dlg)
    foreach ($c in $el.FindAll([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.Condition]::TrueCondition)) {
        if ($c.Current.Name -eq $name) {
            $rc = $c.Current.BoundingRectangle
            ClickXY ([int]($rc.X + $rc.Width/2)) ([int]($rc.Y + $rc.Height/2)) 2200
            return $true
        }
    }
    Log ("WARN: '" + $name + "' not found")
    return $false
}

# --- tabs ---
Click-ByName "OPEN"
Capture-H $dlg "v3_w5_recall_open.png" "recall OPEN"
Click-ByName "CLOSED"
Capture-H $dlg "v3_w5_recall_closed.png" "recall CLOSED"
Click-ByName "VOIDED"
Capture-H $dlg "v3_w5_recall_voided.png" "recall VOIDED"
Click-ByName "HELD"
Capture-H $dlg "v3_w5_recall_held_back.png" "recall HELD return"

# --- search ---
$el = [System.Windows.Automation.AutomationElement]::FromHandle($dlg)
$search = $null
foreach ($c in $el.FindAll([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.Condition]::TrueCondition)) {
    if ($c.Current.Name -eq 'Search invoice, order #, customer, table, phone...') { $search = $c; break }
}
if ($search) {
    $rc = $search.Current.BoundingRectangle
    ClickXY ([int]($rc.X + 150)) ([int]($rc.Y + $rc.Height/2)) 400
    [System.Windows.Forms.SendKeys]::SendWait("ORD-92")
    Start-Sleep -Seconds 2
    Capture-H $dlg "v3_w5_recall_search.png" "recall search ORD-92"
    # clear search via Clear button
    Click-ByName ([char]0x2715 + " Clear")
    Start-Sleep -Seconds 1
    Capture-H $dlg "v3_w5_recall_searchcleared.png" "recall search cleared"
} else {
    Log "WARN search field not found by name; trying first Edit"
}

# --- resize to 1024x768-equivalent (950x640 logical = 2375x1600 phys) ---
[QaV3.E]::SetForegroundWindow($dlg) | Out-Null
Start-Sleep -Milliseconds 400
[QaV3.E]::ShowWindow($dlg, 1) | Out-Null
Start-Sleep -Milliseconds 500
$ok = [QaV3.E]::PostMessage($dlg, 0x0005, [IntPtr]0, [IntPtr]0)
Add-Type -MemberDefinition '[DllImport("user32.dll")] public static extern bool MoveWindow(IntPtr h, int x, int y, int w, int ht, bool repaint);' -Name MV -Namespace QaV3
[QaV3.MV]::MoveWindow($dlg, 700, 350, 2375, 1600, $true) | Out-Null
Start-Sleep -Seconds 2
Capture-H $dlg "v3_w5_recall_950x640.png" "recall 950x640 logical"

# --- resize to 1366-equivalent (1140x680 logical = 2850x1700 phys) ---
[QaV3.MV]::MoveWindow($dlg, 450, 300, 2850, 1700, $true) | Out-Null
Start-Sleep -Seconds 2
Capture-H $dlg "v3_w5_recall_1140x680.png" "recall 1140x680 logical"

Log "=== PHASE 5 COMPLETE ==="
