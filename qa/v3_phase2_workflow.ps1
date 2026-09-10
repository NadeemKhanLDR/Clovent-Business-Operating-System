# ============================================================
# PHASE 2 - POS workflow: new Dine In order, table, variant items,
#           Hold, open Recall dialog. Dumps + captures each step.
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
    Start-Sleep -Milliseconds 200
    [QaV3.Win]::mouse_event(2, 0, 0, 0, 0); Start-Sleep -Milliseconds 90; [QaV3.Win]::mouse_event(4, 0, 0, 0, 0)
    Start-Sleep -Milliseconds 600
}
function Get-Window([string]$titlePart) {
    $wins = $root.FindAll([System.Windows.Automation.TreeScope]::Children, $procCond)
    foreach ($w in $wins) { if ($w.Current.Name -like "*$titlePart*") { return $w } }
    return $null
}
function Capture([string]$file, [string]$label) {
    $w = Get-Window "Restaurant POS"
    if (-not $w) { Log "WARN no POS window"; return }
    $hwnd = [IntPtr]$w.Current.NativeWindowHandle
    $r = New-Object QaV3.RECT
    [QaV3.Win]::GetWindowRect($hwnd, [ref]$r) | Out-Null
    $bw = $r.R - $r.L; $bh = $r.B - $r.T
    $bmp = New-Object System.Drawing.Bitmap($bw, $bh)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $hdc = $g.GetHdc()
    [QaV3.Win]::PrintWindow($hwnd, $hdc, 2) | Out-Null
    $g.ReleaseHdc($hdc); $g.Dispose()
    $bmp.Save("$QaDir\$file", [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
    Log "CAPTURED $label => $file"
}
function Capture-Hwnd2([IntPtr]$hwnd, [string]$file, [string]$label) {
    $r = New-Object QaV3.RECT
    [QaV3.Win]::GetWindowRect($hwnd, [ref]$r) | Out-Null
    $bw = $r.R - $r.L; $bh = $r.B - $r.T
    if ($bw -le 0) { Log "WARN bad rect for $label"; return }
    $bmp = New-Object System.Drawing.Bitmap($bw, $bh)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $hdc = $g.GetHdc()
    [QaV3.Win]::PrintWindow($hwnd, $hdc, 2) | Out-Null
    $g.ReleaseHdc($hdc); $g.Dispose()
    $bmp.Save("$QaDir\$file", [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
    Log "CAPTURED $label => $file (${bw}x${bh})"
}
function Dump-Proc([string]$file) {
    $sb = New-Object System.Text.StringBuilder
    function Dump($el, $depth) {
        foreach ($child in $el.FindAll([System.Windows.Automation.TreeScope]::Children, [System.Windows.Automation.Condition]::TrueCondition)) {
            $name = $child.Current.Name
            $ctype = $child.Current.ControlType.ProgrammaticName -replace 'ControlType.',''
            $rect = $child.Current.BoundingRectangle
            $sb.AppendLine(("{0}{1} '{2}' [{3:f0}x{4:f0}@{5:f0},{6:f0}]" -f ("  " * $depth), $ctype, $name, $rect.Width, $rect.Height, $rect.X, $rect.Y)) | Out-Null
            if ($depth -lt 12) { Dump $child ($depth + 1) }
        }
    }
    $wins = $root.FindAll([System.Windows.Automation.TreeScope]::Children, $procCond)
    foreach ($w in $wins) {
        $sb.AppendLine("=== WINDOW: '$($w.Current.Name)' ===") | Out-Null
        Dump $w 1
    }
    $sb.ToString() | Out-File "$QaDir\$file" -Encoding utf8
    Log "DUMP => $file"
}

$pos = Get-Window "Restaurant POS"
if (-not $pos) { Log "ERROR: POS window not found"; exit 1 }
$posHwnd = [IntPtr]$pos.Current.NativeWindowHandle
[QaV3.Win]::SetForegroundWindow($posHwnd) | Out-Null
Start-Sleep -Milliseconds 600

# ---------- A: New Dine In order ----------
Log "STEP A: + Dine In"
Click 356 115
Start-Sleep -Seconds 2
Dump-Proc "v3_w2_neworder_uia.txt"
Capture "v3_w2_neworder.png" "new dine-in order"

# ---------- B: Table picker ----------
Log "STEP B: open table picker"
# locate table pane from fresh dump
$pos = Get-Window "Restaurant POS"
$tableEl = $null
$all = $pos.FindAll([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.Condition]::TrueCondition)
foreach ($el in $all) { if ($el.Current.Name -like "Table:*") { $tableEl = $el; break } }
if ($tableEl) {
    $rc = $tableEl.Current.BoundingRectangle
    Log ("Table picker found: '{0}' at {1:f0},{2:f0}" -f $tableEl.Current.Name, $rc.X, $rc.Y)
    Click ([int]($rc.X + $rc.Width / 2)) ([int]($rc.Y + $rc.Height / 2))
    Start-Sleep -Seconds 1
    # popup: find T-02 item across all process windows incl. popups
    $t02 = $null
    $all2 = $root.FindAll([System.Windows.Automation.TreeScope]::Children, $procCond)
    foreach ($w in $all2) {
        $items = $w.FindAll([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.Condition]::TrueCondition)
        foreach ($el in $items) { if ($el.Current.Name -eq "T-02") { $t02 = $el; break } }
        if ($t02) { break }
    }
    if ($t02) {
        $rc2 = $t02.Current.BoundingRectangle
        Capture "v3_w2_tabledropdown.png" "table dropdown open"
        Log ("Clicking T-02 at {0:f0},{1:f0}" -f $rc2.X, $rc2.Y)
        Click ([int]($rc2.X + $rc2.Width / 2)) ([int]($rc2.Y + $rc2.Height / 2))
        Start-Sleep -Seconds 1
    } else {
        Log "WARN: T-02 item not found in popup"
        Capture "v3_w2_tabledropdown.png" "table dropdown (no T-02 found)"
        # close popup with Escape
        [System.Windows.Forms.SendKeys]::SendWait("{ESC}")
    }
} else {
    Log "WARN: table picker pane not found"
}
Dump-Proc "v3_w2_table_uia.txt"
Capture "v3_w2_table_selected.png" "table selected"

# ---------- C: add items ----------
Log "STEP C: add items"
Click 915 873    # Chicken Biryani 450
Start-Sleep -Milliseconds 800
Click 1571 2034  # Aloo Gobi - Full 380
Start-Sleep -Milliseconds 800
Click 1331 2034  # Aloo Gobi - Half 250
Start-Sleep -Milliseconds 800
Click 2107 1453  # White Daal Mash - Full 340
Start-Sleep -Milliseconds 800
Click 2107 2034  # Chicken Koyla Karahi - Half 350
Start-Sleep -Seconds 2
Dump-Proc "v3_w2_cart_uia.txt"
Capture "v3_w2_cart.png" "cart with 5 items"

# ---------- D: Hold ----------
Log "STEP D: Hold"
Click 2994 2147
Start-Sleep -Seconds 3
Dump-Proc "v3_w2_afterhold_uia.txt"
Capture "v3_w2_afterhold.png" "after hold"

# ---------- E: Recall dialog ----------
Log "STEP E: open Recall"
Click 3326 2147
$recallWin = $null
for ($i = 0; $i -lt 20; $i++) {
    Start-Sleep -Milliseconds 500
    $recallWin = Get-Window "Recall / Sales History"
    if ($recallWin) { break }
}
if (-not $recallWin) { Log "ERROR: Recall dialog did not open"; exit 1 }
Start-Sleep -Seconds 3
$rHwnd = [IntPtr]$recallWin.Current.NativeWindowHandle
[QaV3.Win]::SetForegroundWindow($rHwnd) | Out-Null
Start-Sleep -Milliseconds 800
Dump-Proc "v3_w2_recall_uia.txt"
Capture-Hwnd2 $rHwnd "v3_w2_recall_held.png" "recall dialog HELD"

Log "=== PHASE 2 COMPLETE ==="
