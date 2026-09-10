# ============================================================
# PHASE 2B - Robust retry: dismiss msgbox, create Dine In order,
# table T-02, add 5 variant items, Hold, open Recall
# ============================================================
Add-Type -TypeDefinition @"
using System;
using System.Runtime.InteropServices;
namespace QaV3 {
    public struct RECT { public int L, T, R, B; }
    public static class Win {
        [DllImport("user32.dll")] public static extern bool SetProcessDpiAwarenessContext(IntPtr v);
        [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
        [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h, int cmd);
        [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
        [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr h, IntPtr hdc, uint flags);
        [DllImport("user32.dll")] public static extern void mouse_event(uint f, uint x, uint y, uint d, int e);
        [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
    }
}
"@
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing
[QaV3.Win]::SetProcessDpiAwarenessContext([IntPtr](-4)) | Out-Null

$QaDir = "d:\Clovent Business Operating System\qa"
$procId = [int](Get-Content "$QaDir\v3_pid.txt")
$procCond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ProcessIdProperty, $procId)
$root = [System.Windows.Automation.AutomationElement]::RootElement

function Log($m) { Write-Output $m }
function Click([int]$x, [int]$y) {
    [QaV3.Win]::SetCursorPos($x, $y) | Out-Null
    Start-Sleep -Milliseconds 350
    [QaV3.Win]::mouse_event(2, 0, 0, 0, 0); Start-Sleep -Milliseconds 100; [QaV3.Win]::mouse_event(4, 0, 0, 0, 0)
    Start-Sleep -Milliseconds 800
}
function Get-Window([string]$titlePart) {
    $wins = $root.FindAll([System.Windows.Automation.TreeScope]::Children, $procCond)
    foreach ($w in $wins) { if ($w.Current.Name -like "*$titlePart*") { return $w } }
    return $null
}
function Capture-Hwnd2([IntPtr]$hwnd, [string]$file, [string]$label) {
    $r = New-Object QaV3.RECT
    [QaV3.Win]::GetWindowRect($hwnd, [ref]$r) | Out-Null
    $bw = $r.R - $r.L; $bh = $r.B - $r.T
    if ($bw -le 0) { Log "WARN bad rect"; return }
    $bmp = New-Object System.Drawing.Bitmap($bw, $bh)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $hdc = $g.GetHdc()
    [QaV3.Win]::PrintWindow($hwnd, $hdc, 2) | Out-Null
    $g.ReleaseHdc($hdc); $g.Dispose()
    $bmp.Save("$QaDir\$file", [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
    Log "CAPTURED $label => $file (${bw}x${bh})"
}
function Capture-Pos([string]$file, [string]$label) {
    $w = Get-Window "Restaurant POS"
    if ($w) { Capture-Hwnd2 ([IntPtr]$w.Current.NativeWindowHandle) $file $label }
}
function TopBar-OrderActive {
    $pos = Get-Window "Restaurant POS"
    if (-not $pos) { return $false }
    $all = $pos.FindAll([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.Condition]::TrueCondition)
    foreach ($el in $all) {
        $n = $el.Current.Name
        if ($n -match '^ORD-\d+' -and $n.Length -lt 40) { return $true }
        if ($n -eq 'Cancel Order') { return $true }
    }
    return $false
}

# ---- 1. dismiss any modal messagebox ----
$msg = Get-Window "No Order Started"
if ($msg) {
    $ok = $null
    $items = $msg.FindAll([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.Condition]::TrueCondition)
    foreach ($el in $items) { if ($el.Current.Name -eq 'OK') { $ok = $el; break } }
    if ($ok) {
        $rc = $ok.Current.BoundingRectangle
        Click ([int]($rc.X + $rc.Width / 2)) ([int]($rc.Y + $rc.Height / 2))
        Log "Dismissed 'No Order Started' message"
    }
}
Start-Sleep -Milliseconds 500
$msg = Get-Window "No Order Started"
if ($msg) { Log "WARN: message box still open"; Click 1919 1261; Start-Sleep -Milliseconds 800 }

# ---- 2. activate POS via title bar click ----
$pos = Get-Window "Restaurant POS"
if (-not $pos) { Log "ERROR: no POS window"; exit 1 }
$posHwnd = [IntPtr]$pos.Current.NativeWindowHandle
[QaV3.Win]::ShowWindow($posHwnd, 3) | Out-Null
Start-Sleep -Seconds 1
Click 1900 30     # title bar - pure activation
Start-Sleep -Milliseconds 900

# ---- 3. + Dine In with verification retry ----
$created = $false
for ($try = 1; $try -le 3 -and -not $created; $try++) {
    Log "STEP: click + Dine In (try $try)"
    Click 356 115
    Start-Sleep -Seconds 2
    $created = TopBar-OrderActive
}
if (-not $created) { Log "ERROR: Dine In order not created after 3 tries"; exit 1 }
Log "OK: dine-in order active"
Start-Sleep -Seconds 1
Capture-Pos "v3_w2_neworder.png" "new dine-in order"

# ---- 4. table picker ----
$pos = Get-Window "Restaurant POS"
$tableEl = $null
$all = $pos.FindAll([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.Condition]::TrueCondition)
foreach ($el in $all) {
    $n = $el.Current.Name
    if ($n -like "Table*" -and $n -notlike "*`u{00b7}*" -and $n -notlike "*ORD-*") { $tableEl = $el; break }
}
if (-not $tableEl) {
    # fall back: any pane named 'Table: ...' in right panel (x > 2800)
    foreach ($el in $all) {
        $rc = $el.Current.BoundingRectangle
        if ($el.Current.Name -like "*Table*" -and $rc.X -gt 2700 -and $rc.Width -gt 100 -and $rc.Width -lt 600) { $tableEl = $el; break }
    }
}
if ($tableEl) {
    $rc = $tableEl.Current.BoundingRectangle
    Log ("Table picker: '{0}' @ {1:f0},{2:f0}" -f $tableEl.Current.Name, $rc.X, $rc.Y)
    Click ([int]($rc.X + $rc.Width / 2)) ([int]($rc.Y + $rc.Height / 2))
    Start-Sleep -Seconds 1
    $t02 = $null
    $wins2 = $root.FindAll([System.Windows.Automation.TreeScope]::Children, $procCond)
    foreach ($w in $wins2) {
        $items = $w.FindAll([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.Condition]::TrueCondition)
        foreach ($el in $items) { if ($el.Current.Name -match '^T-0?2$|^Table 2$|^2$') { $t02 = $el; break } }
        if ($t02) { break }
    }
    if ($t02) {
        Capture-Pos "v3_w2_tabledropdown.png" "table dropdown open"
        $rc2 = $t02.Current.BoundingRectangle
        Click ([int]($rc2.X + $rc2.Width / 2)) ([int]($rc2.Y + $rc2.Height / 2))
        Log "Clicked T-02"
        Start-Sleep -Seconds 1
    } else {
        Log "WARN: T-02 not found in popup"
        Capture-Pos "v3_w2_tabledropdown.png" "table dropdown (T-02 missing)"
        [System.Windows.Forms.SendKeys]::SendWait("{ESC}")
    }
} else { Log "WARN: table picker not found - will dump and continue" }
Capture-Pos "v3_w2_table_selected.png" "table selected"

# ---- 5. add items ----
$items = @(
    @(915, 873,  "Chicken Biryani"),
    @(1571, 2034, "Aloo Gobi Full"),
    @(1331, 2034, "Aloo Gobi Half"),
    @(2107, 1453, "White Daal Mash Full"),
    @(2107, 2034, "Koyla Half")
)
foreach ($it in $items) {
    Click $it[0] $it[1]
    Log ("Added: {0}" -f $it[2])
    Start-Sleep -Milliseconds 400
}
Start-Sleep -Seconds 2
Capture-Pos "v3_w2_cart.png" "cart 5 items"

# ---- 6. verify cart via dump ----
$pos = Get-Window "Restaurant POS"
$sb2 = New-Object System.Text.StringBuilder
foreach ($el in $pos.FindAll([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.Condition]::TrueCondition)) {
    $n = $el.Current.Name
    if ($n -match 'Ordered Items|Subtotal|Tax|Discount|Service Charge|TOTAL PAYABLE|Balance Due|Table|Aloo|Biryani|Daal|Koyla|Karahi') {
        $rc = $el.Current.BoundingRectangle
        $sb2.AppendLine("'{0}' [{1:f0}x{2:f0}@{3:f0},{4:f0}]" -f $n, $rc.Width, $rc.Height, $rc.X, $rc.Y) | Out-Null
    }
}
$sb2.ToString() | Out-File "$QaDir\v3_w2_cart_summary.txt" -Encoding utf8
Log "Cart summary written"

# ---- 7. Hold ----
Log "STEP: Hold"
Click 2994 2147
Start-Sleep -Seconds 3
Capture-Pos "v3_w2_afterhold.png" "after hold"

# ---- 8. Recall ----
Log "STEP: Recall"
Click 3326 2147
$recallWin = $null
for ($i = 0; $i -lt 24; $i++) {
    Start-Sleep -Milliseconds 500
    $recallWin = Get-Window "Recall / Sales History"
    if ($recallWin) { break }
}
if (-not $recallWin) { Log "ERROR: Recall dialog did not open"; exit 1 }
Start-Sleep -Seconds 3
$rHwnd = [IntPtr]$recallWin.Current.NativeWindowHandle
[QaV3.Win]::SetForegroundWindow($rHwnd) | Out-Null
Start-Sleep -Milliseconds 800
Capture-Hwnd2 $rHwnd "v3_w2_recall_held.png" "recall HELD"

# dump recall dialog tree
$sb3 = New-Object System.Text.StringBuilder
function Dump3($el, $depth) {
    foreach ($child in $el.FindAll([System.Windows.Automation.TreeScope]::Children, [System.Windows.Automation.Condition]::TrueCondition)) {
        $n = $child.Current.Name
        $ct = $child.Current.ControlType.ProgrammaticName -replace 'ControlType.',''
        $rc = $child.Current.BoundingRectangle
        $sb3.AppendLine(("{0}{1} '{2}' [{3:f0}x{4:f0}@{5:f0},{6:f0}]" -f ("  " * $depth), $ct, $n, $rc.Width, $rc.Height, $rc.X, $rc.Y)) | Out-Null
        if ($depth -lt 12) { Dump3 $child ($depth + 1) }
    }
}
Dump3 $recallWin 0
$sb3.ToString() | Out-File "$QaDir\v3_w2_recall_uia.txt" -Encoding utf8
Log "=== PHASE 2B COMPLETE ==="
