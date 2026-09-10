# ============================================================
# PHASE 2C - Dine In -> table popup -> T-02 -> items -> Hold -> Recall
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
    Start-Sleep -Milliseconds 900
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
function Order-Active {
    $pos = Get-Window "Restaurant POS"
    if (-not $pos) { return $false }
    foreach ($el in $pos.FindAll([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.Condition]::TrueCondition)) {
        $n = $el.Current.Name
        if (($n -match '^ORD-\d+' -and $n.Length -lt 40) -or $n -eq 'Cancel Order') { return $true }
    }
    return $false
}
function Find-Anywhere([string]$name) {
    $wins2 = $root.FindAll([System.Windows.Automation.TreeScope]::Children, $procCond)
    foreach ($w in $wins2) {
        foreach ($el in $w.FindAll([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.Condition]::TrueCondition)) {
            if ($el.Current.Name -eq $name) { return $el }
        }
    }
    return $null
}

# dismiss stray message boxes
foreach ($t in @("No Order Started", "No Location Selected")) {
    $m = Get-Window $t
    if ($m) {
        $ok = $null
        foreach ($el in $m.FindAll([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.Condition]::TrueCondition)) {
            if ($el.Current.Name -eq 'OK') { $ok = $el; break }
        }
        if ($ok) { $rc = $ok.Current.BoundingRectangle; Click ([int]($rc.X + $rc.Width / 2)) ([int]($rc.Y + $rc.Height / 2)); Log "Dismissed '$t'" }
    }
}

$pos = Get-Window "Restaurant POS"
if (-not $pos) { Log "ERROR: no POS window"; exit 1 }
$posHwnd = [IntPtr]$pos.Current.NativeWindowHandle
[QaV3.Win]::SetForegroundWindow($posHwnd) | Out-Null
Start-Sleep -Milliseconds 700

# ---- 1. + Dine In ----
Log "STEP 1: + Dine In"
Click 356 115
Start-Sleep -Seconds 2

if (-not (Order-Active)) {
    # table popup should be open - find and click T-02
    $t02 = Find-Anywhere "T-02"
    if ($t02) {
        Capture-Pos "v3_w2_tablepopup.png" "table popup open"
        $rc = $t02.Current.BoundingRectangle
        Log ("T-02 popup item at {0:f0},{1:f0}" -f $rc.X, $rc.Y)
        Click ([int]($rc.X + $rc.Width / 2)) ([int]($rc.Y + $rc.Height / 2))
        Start-Sleep -Seconds 2
    } else {
        Log "WARN: no popup found; dumping windows for diagnosis"
        $wins2 = $root.FindAll([System.Windows.Automation.TreeScope]::Children, $procCond)
        foreach ($w in $wins2) { Log ("  win: '{0}'" -f $w.Current.Name) }
    }
}

if (Order-Active) { Log "OK: dine-in order created" }
else { Log "ERROR: still no active order"; exit 1 }
Start-Sleep -Seconds 1
Capture-Pos "v3_w2_neworder.png" "new dine-in order T-02"

# ---- 2. add 5 items ----
$items = @(
    @(915, 873,  "Chicken Biryani 450"),
    @(1571, 2034, "Aloo Gobi Full 380"),
    @(1331, 2034, "Aloo Gobi Half 250"),
    @(2107, 1453, "White Daal Mash Full 340"),
    @(2107, 2034, "Koyla Half 350")
)
foreach ($it in $items) {
    Click $it[0] $it[1]
    Log ("Added: {0}" -f $it[2])
}
Start-Sleep -Seconds 2
Capture-Pos "v3_w2_cart.png" "cart 5 items"

# ---- 3. verify cart text ----
$pos = Get-Window "Restaurant POS"
$sb2 = New-Object System.Text.StringBuilder
foreach ($el in $pos.FindAll([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.Condition]::TrueCondition)) {
    $n = $el.Current.Name
    if ($n -ne "" -and ($n -match 'Ordered Items|Subtotal|Tax$|Discount|Service Charge|TOTAL PAYABLE|Balance Due|Table|Aloo|Biryani|Daal|Koyla|Chicken|Full|Half|Standard|Held|Open')) {
        $rc = $el.Current.BoundingRectangle
        $sb2.AppendLine("'{0}' [{1:f0}x{2:f0}@{3:f0},{4:f0}]" -f $n, $rc.Width, $rc.Height, $rc.X, $rc.Y) | Out-Null
    }
}
$sb2.ToString() | Out-File "$QaDir\v3_w2_cart_summary.txt" -Encoding utf8
Log "cart summary => v3_w2_cart_summary.txt"

# ---- 4. Hold ----
Log "STEP 4: Hold"
Click 2994 2147
Start-Sleep -Seconds 3
Capture-Pos "v3_w2_afterhold.png" "after hold"
Log ("Order active after hold: " + (Order-Active))

# ---- 5. Recall ----
Log "STEP 5: Recall"
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
Start-Sleep -Milliseconds 900
Capture-Hwnd2 $rHwnd "v3_w2_recall_held.png" "recall HELD"

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
Log "=== PHASE 2C COMPLETE ==="
