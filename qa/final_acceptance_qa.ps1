# ============================================================
# FINAL ACCEPTANCE QA
# ============================================================

Add-Type -TypeDefinition @"
using System;
using System.Text;
using System.Runtime.InteropServices;
namespace QaFinal {
    public struct RECT { public int L, T, R, B; }
    public static class Win {
        [DllImport("user32.dll")] public static extern bool SetProcessDpiAwarenessContext(IntPtr v);
        [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
        [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h, int cmd);
        [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
        [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr h, IntPtr hdc, uint flags);
        [DllImport("user32.dll")] public static extern bool PostMessage(IntPtr h, uint msg, IntPtr w, IntPtr l);
        [DllImport("user32.dll")] public static extern int GetDpiForWindow(IntPtr h);
        [DllImport("user32.dll")] public static extern void mouse_event(uint f, uint x, uint y, uint d, int e);
        [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
        [DllImport("user32.dll")] public static extern bool MoveWindow(IntPtr h, int x, int y, int w, int ht, bool repaint);
    }
}
"@

Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

[QaFinal.Win]::SetProcessDpiAwarenessContext([IntPtr](-4)) | Out-Null

$QaDir = "d:\Clovent Business Operating System\qa"
$ExePath = "d:\Clovent Business Operating System\src\Clovent.Desktop\bin\Debug\net10.0-windows\Clovent.Desktop.exe"

function Log($msg) { Write-Output $msg }

function Capture-Hwnd {
    param([IntPtr]$hwnd, [string]$path, [string]$label = "")
    $r = New-Object QaFinal.RECT
    [QaFinal.Win]::GetWindowRect($hwnd, [ref]$r) | Out-Null
    $w = $r.R - $r.L; $h = $r.B - $r.T
    if ($w -le 0 -or $h -le 0) { Log "WARN: $label - invalid bounds ${w}x${h}"; return }
    $bmp = New-Object System.Drawing.Bitmap($w, $h)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $hdc = $g.GetHdc()
    $ok = [QaFinal.Win]::PrintWindow($hwnd, $hdc, 2)
    $g.ReleaseHdc($hdc)
    if (-not $ok) {
        $g.Dispose(); $bmp.Dispose()
        $bmp = New-Object System.Drawing.Bitmap($w, $h)
        $g = [System.Drawing.Graphics]::FromImage($bmp)
        $sx = [Math]::Max(0, $r.L); $sy = [Math]::Max(0, $r.T)
        $sw = [Math]::Min($w, [System.Windows.Forms.Screen]::PrimaryScreen.Bounds.Width - $sx)
        $sh = [Math]::Min($h, [System.Windows.Forms.Screen]::PrimaryScreen.Bounds.Height - $sy)
        $g.CopyFromScreen($sx, $sy, 0, 0, (New-Object System.Drawing.Size($sw, $sh)))
    }
    $g.Dispose()
    $bmp.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
    Log "CAPTURED: $label => $(Split-Path $path -Leaf) (${w}x${h})"
}

function Click-UIA {
    param([System.Windows.Automation.AutomationElement]$el, [string]$label = "")
    try {
        $inv = $null
        if ($el.TryGetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern, [ref]$inv)) {
            $inv.Invoke(); Start-Sleep -Milliseconds 500; return $true
        }
    } catch {}
    $rect = $el.Current.BoundingRectangle
    if ($rect.Width -gt 0 -and $rect.Height -gt 0) {
        $cx = [int]($rect.X + $rect.Width / 2)
        $cy = [int]($rect.Y + $rect.Height / 2)
        [QaFinal.Win]::SetCursorPos($cx, $cy) | Out-Null
        Start-Sleep -Milliseconds 150
        [QaFinal.Win]::mouse_event(2, 0, 0, 0, 0)
        Start-Sleep -Milliseconds 80
        [QaFinal.Win]::mouse_event(4, 0, 0, 0, 0)
        Start-Sleep -Milliseconds 500
        return $true
    }
    Log "WARN: Could not click $label"
    return $false
}

function Find-ByName {
    param([System.Windows.Automation.AutomationElement]$root, [string]$name)
    $cond = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::NameProperty, $name)
    return $root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $cond)
}

function Wait-Window {
    param([System.Windows.Automation.PropertyCondition]$procCond, [string]$titlePart, [int]$maxWait = 40)
    $root = [System.Windows.Automation.AutomationElement]::RootElement
    for ($i = 0; $i -lt $maxWait; $i++) {
        $wins = $root.FindAll([System.Windows.Automation.TreeScope]::Children, $procCond)
        foreach ($w in $wins) {
            if ($w.Current.Name -like "*$titlePart*") { return $w }
        }
        Start-Sleep -Seconds 1
    }
    return $null
}

# KILL EXISTING
Get-Process Clovent.Desktop -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Seconds 1

# LAUNCH
Log "=== LAUNCHING POS APPLICATION ==="
$proc = Start-Process $ExePath -ArgumentList "--pos" -PassThru
Log "PID: $($proc.Id)"
$procCond = New-Object System.Windows.Automation.PropertyCondition(
    [System.Windows.Automation.AutomationElement]::ProcessIdProperty, $proc.Id)
$root = [System.Windows.Automation.AutomationElement]::RootElement

Log "Waiting for Restaurant POS window..."
$posWin = Wait-Window -procCond $procCond -titlePart "Restaurant POS" -maxWait 45
if (-not $posWin) {
    Log "ERROR: POS window not found after 45s"
    $all = $root.FindAll([System.Windows.Automation.TreeScope]::Children, $procCond)
    foreach ($w in $all) { Log "  Window: '$($w.Current.Name)'" }
    exit 1
}

$posHwnd = [IntPtr]$posWin.Current.NativeWindowHandle
[QaFinal.Win]::ShowWindow($posHwnd, 3) | Out-Null
Start-Sleep -Seconds 2
[QaFinal.Win]::SetForegroundWindow($posHwnd) | Out-Null
Start-Sleep -Milliseconds 800

$dpi = [QaFinal.Win]::GetDpiForWindow($posHwnd)
$dpiScale = $dpi / 96.0
Log "POS Window DPI: $dpi (scale: ${dpiScale}x)"
$screen = [System.Windows.Forms.Screen]::PrimaryScreen
Log "Screen: $($screen.Bounds.Width)x$($screen.Bounds.Height)"

# CAPTURE 1: NATIVE - STARTUP
Capture-Hwnd $posHwnd "$QaDir\accept_01_pos_startup_native.png" "POS Startup Native"

# RESIZE TO 1024x768
$pw = [int]($dpiScale * 1024); $ph = [int]($dpiScale * 768)
$ox = [int](($screen.WorkingArea.Width - $pw)/2); $oy = [int](($screen.WorkingArea.Height - $ph)/2)
[QaFinal.Win]::MoveWindow($posHwnd, $ox, $oy, $pw, $ph, $true) | Out-Null; Start-Sleep -Seconds 1
Capture-Hwnd $posHwnd "$QaDir\accept_02_pos_1024x768.png" "POS 1024x768"

# RESIZE TO 1366x768
$pw2 = [int]($dpiScale * 1366)
$ox2 = [int](($screen.WorkingArea.Width - $pw2)/2)
[QaFinal.Win]::MoveWindow($posHwnd, $ox2, $oy, $pw2, $ph, $true) | Out-Null; Start-Sleep -Seconds 1
Capture-Hwnd $posHwnd "$QaDir\accept_03_pos_1366x768.png" "POS 1366x768"

# RESIZE TO 1920x1080 (if screen allows)
$pw3 = [int]($dpiScale * 1920); $ph3 = [int]($dpiScale * 1080)
if ($pw3 -le $screen.WorkingArea.Width -and $ph3 -le $screen.WorkingArea.Height) {
    $ox3 = [int](($screen.WorkingArea.Width - $pw3)/2); $oy3 = [int](($screen.WorkingArea.Height - $ph3)/2)
    [QaFinal.Win]::MoveWindow($posHwnd, $ox3, $oy3, $pw3, $ph3, $true) | Out-Null; Start-Sleep -Seconds 1
    Capture-Hwnd $posHwnd "$QaDir\accept_04_pos_1920x1080.png" "POS 1920x1080"
} else {
    Log "Screen too small for 1920x1080 test - capturing current size"
    Capture-Hwnd $posHwnd "$QaDir\accept_04_pos_1920x1080.png" "POS native (1920x1080 not possible)"
}

# RESTORE MAXIMIZE
[QaFinal.Win]::ShowWindow($posHwnd, 3) | Out-Null; Start-Sleep -Seconds 1
[QaFinal.Win]::SetForegroundWindow($posHwnd) | Out-Null; Start-Sleep -Milliseconds 600

# OPEN RECALL DIALOG
Log "=== OPENING RECALL DIALOG ==="
$posWin = $null
$wins = $root.FindAll([System.Windows.Automation.TreeScope]::Children, $procCond)
foreach ($w in $wins) { if ($w.Current.Name -like "*Restaurant POS*") { $posWin = $w; break } }
if (-not $posWin) { Log "ERROR: Lost POS window"; exit 1 }

$recallEl = Find-ByName $posWin "Recall"
if (-not $recallEl) { Log "ERROR: Recall button not found"; exit 1 }
Click-UIA $recallEl "Recall"
Start-Sleep -Seconds 3

$recallWin = $null
for ($i = 0; $i -lt 20; $i++) {
    $wins = $root.FindAll([System.Windows.Automation.TreeScope]::Children, $procCond)
    foreach ($w in $wins) {
        $h = [IntPtr]$w.Current.NativeWindowHandle
        if ($h -ne $posHwnd -and ($w.Current.Name -like "*Recall*" -or $w.Current.Name -like "*Sales History*")) {
            $recallWin = $w; break
        }
    }
    if ($recallWin) { break }
    Start-Sleep -Milliseconds 500
}
if (-not $recallWin) { Log "ERROR: Recall dialog not found"; exit 1 }

$rHwnd = [IntPtr]$recallWin.Current.NativeWindowHandle
Log "Recall dialog: '$($recallWin.Current.Name)'"
[QaFinal.Win]::SetForegroundWindow($rHwnd) | Out-Null; Start-Sleep -Milliseconds 800

$rDpi = [QaFinal.Win]::GetDpiForWindow($rHwnd)
$rScale = $rDpi / 96.0
$rRect = New-Object QaFinal.RECT
[QaFinal.Win]::GetWindowRect($rHwnd, [ref]$rRect) | Out-Null
$rw = $rRect.R - $rRect.L; $rh = $rRect.B - $rRect.T
Log "Recall Dialog: ${rw}x${rh} @ DPI $rDpi (${rScale}x)"

# CAPTURE RECALL - NATIVE (HELD)
Capture-Hwnd $rHwnd "$QaDir\accept_05_recall_native_held.png" "Recall Native HELD"

# SIZE RECALL TO 950x640
$rw1 = [int](950 * $rScale); $rh1 = [int](640 * $rScale)
$rc1x = [int](($screen.WorkingArea.Width - $rw1)/2); $rc1y = [int](($screen.WorkingArea.Height - $rh1)/2)
[QaFinal.Win]::MoveWindow($rHwnd, $rc1x, $rc1y, $rw1, $rh1, $true) | Out-Null; Start-Sleep -Seconds 1
Capture-Hwnd $rHwnd "$QaDir\accept_06_recall_1024x768_held.png" "Recall 1024x768 HELD"

# SIZE RECALL TO 1140x680
$rw2 = [int](1140 * $rScale); $rh2 = [int](680 * $rScale)
$rc2x = [int](($screen.WorkingArea.Width - $rw2)/2); $rc2y = [int](($screen.WorkingArea.Height - $rh2)/2)
[QaFinal.Win]::MoveWindow($rHwnd, $rc2x, $rc2y, $rw2, $rh2, $true) | Out-Null; Start-Sleep -Seconds 1
Capture-Hwnd $rHwnd "$QaDir\accept_07_recall_1366x768_held.png" "Recall 1366x768 HELD"

# SIZE RECALL TO 1200x740
$rw3 = [int](1200 * $rScale); $rh3 = [int](740 * $rScale)
$rc3x = [int](($screen.WorkingArea.Width - $rw3)/2); $rc3y = [int](($screen.WorkingArea.Height - $rh3)/2)
[QaFinal.Win]::MoveWindow($rHwnd, $rc3x, $rc3y, $rw3, $rh3, $true) | Out-Null; Start-Sleep -Seconds 1
Capture-Hwnd $rHwnd "$QaDir\accept_08_recall_1920x1080_held.png" "Recall 1920x1080 HELD"

# RESTORE USABLE SIZE
[QaFinal.Win]::MoveWindow($rHwnd, $rc3x, $rc3y, $rw3, $rh3, $true) | Out-Null; Start-Sleep -Milliseconds 500

# TAB: OPEN
$openTab = Find-ByName $recallWin "OPEN"
if ($openTab) {
    Click-UIA $openTab "OPEN tab"; Start-Sleep -Seconds 2
    Capture-Hwnd $rHwnd "$QaDir\accept_09_recall_open.png" "Recall OPEN"
} else { Log "WARN: OPEN tab not found" }

# TAB: CLOSED
$closedTab = Find-ByName $recallWin "CLOSED"
if ($closedTab) {
    Click-UIA $closedTab "CLOSED tab"; Start-Sleep -Seconds 2
    Capture-Hwnd $rHwnd "$QaDir\accept_10_recall_closed.png" "Recall CLOSED"
} else { Log "WARN: CLOSED tab not found" }

# TAB: VOIDED
$voidedTab = Find-ByName $recallWin "VOIDED"
if ($voidedTab) {
    Click-UIA $voidedTab "VOIDED tab"; Start-Sleep -Seconds 2
    Capture-Hwnd $rHwnd "$QaDir\accept_11_recall_voided.png" "Recall VOIDED"
} else { Log "WARN: VOIDED tab not found" }

# RETURN TO HELD
$heldTab = Find-ByName $recallWin "HELD"
if ($heldTab) {
    Click-UIA $heldTab "HELD tab"; Start-Sleep -Seconds 2
    Capture-Hwnd $rHwnd "$QaDir\accept_12_recall_held_return.png" "Recall HELD return"
}

# SEARCH TEST
$editCond = New-Object System.Windows.Automation.PropertyCondition(
    [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
    [System.Windows.Automation.ControlType]::Edit)
$allEdits = $recallWin.FindAll([System.Windows.Automation.TreeScope]::Descendants, $editCond)
if ($allEdits.Count -gt 0) {
    $searchEl = $allEdits[0]
    Click-UIA $searchEl "Search"
    [System.Windows.Forms.SendKeys]::SendWait("ORD"); Start-Sleep -Seconds 1
    Capture-Hwnd $rHwnd "$QaDir\accept_13_recall_search_ord.png" "Recall search ORD"
    [System.Windows.Forms.SendKeys]::SendWait("^a{DELETE}"); Start-Sleep -Milliseconds 600
    Capture-Hwnd $rHwnd "$QaDir\accept_14_recall_search_cleared.png" "Recall search cleared"
}

# SELECT FIRST ORDER (press Down)
[QaFinal.Win]::SetForegroundWindow($rHwnd) | Out-Null; Start-Sleep -Milliseconds 300
[System.Windows.Forms.SendKeys]::SendWait("{DOWN}"); Start-Sleep -Seconds 2
Capture-Hwnd $rHwnd "$QaDir\accept_15_recall_order_selected.png" "Recall order selected + preview"

# CLOSE RECALL
$closeEl = Find-ByName $recallWin "Close"
if ($closeEl) { Click-UIA $closeEl "Close" } else { [QaFinal.Win]::PostMessage($rHwnd, 0x0010, [IntPtr]::Zero, [IntPtr]::Zero) | Out-Null }
Start-Sleep -Seconds 1

# POS FULL - 1024x768
[QaFinal.Win]::ShowWindow($posHwnd, 3) | Out-Null; Start-Sleep -Seconds 1
[QaFinal.Win]::SetForegroundWindow($posHwnd) | Out-Null; Start-Sleep -Milliseconds 600
[QaFinal.Win]::MoveWindow($posHwnd, $ox, $oy, $pw, $ph, $true) | Out-Null; Start-Sleep -Seconds 1
Capture-Hwnd $posHwnd "$QaDir\accept_16_pos_1024x768_full.png" "POS full 1024x768"

[QaFinal.Win]::MoveWindow($posHwnd, $ox2, $oy, $pw2, $ph, $true) | Out-Null; Start-Sleep -Seconds 1
Capture-Hwnd $posHwnd "$QaDir\accept_17_pos_1366x768_full.png" "POS full 1366x768"

[QaFinal.Win]::ShowWindow($posHwnd, 3) | Out-Null; Start-Sleep -Seconds 1
Capture-Hwnd $posHwnd "$QaDir\accept_18_pos_native_final.png" "POS native final"

Log ""
Log "=== CAPTURE COMPLETE ==="
$captures = 1..18 | ForEach-Object { "accept_{0:D2}_*.png" -f $_ } |
    ForEach-Object { Get-Item "$QaDir\$_" -ErrorAction SilentlyContinue }
Log "Total files captured: $(@($captures | Where-Object {$_}).Count) / 18"

Start-Sleep -Seconds 2
Get-Process Clovent.Desktop -ErrorAction SilentlyContinue | Stop-Process -Force
Log "Done."
