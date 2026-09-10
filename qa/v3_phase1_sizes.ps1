# ============================================================
# PHASE 1 - Launch POS, capture at all 4 resolution cases
#   - native maximized  = 3840x2400 @ 250%
#   - logical 1024x768  (physical 2560x1920)
#   - logical 1366x768  (physical 3415x1920)
#   - logical 1920x1080 (physical 4800x2700, extends off-screen)
# Leaves app running; PID written to qa\v3_pid.txt
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
        [DllImport("user32.dll")] public static extern int GetDpiForWindow(IntPtr h);
        [DllImport("user32.dll")] public static extern bool MoveWindow(IntPtr h, int x, int y, int w, int ht, bool repaint);
    }
}
"@
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

[QaV3.Win]::SetProcessDpiAwarenessContext([IntPtr](-4)) | Out-Null

$QaDir = "d:\Clovent Business Operating System\qa"
$ExePath = "d:\Clovent Business Operating System\src\Clovent.Desktop\bin\Debug\net10.0-windows\Clovent.Desktop.exe"

function Log($msg) { Write-Output $msg }

function Capture-Hwnd {
    param([IntPtr]$hwnd, [string]$path, [string]$label = "")
    $r = New-Object QaV3.RECT
    [QaV3.Win]::GetWindowRect($hwnd, [ref]$r) | Out-Null
    $w = $r.R - $r.L; $h = $r.B - $r.T
    if ($w -le 0 -or $h -le 0) { Log "WARN: $label invalid bounds ${w}x${h}"; return }
    $bmp = New-Object System.Drawing.Bitmap($w, $h)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $hdc = $g.GetHdc()
    $ok = [QaV3.Win]::PrintWindow($hwnd, $hdc, 2)
    $g.ReleaseHdc($hdc); $g.Dispose()
    $bmp.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
    Log "CAPTURED: $label => $(Split-Path $path -Leaf) (${w}x${h}) ok=$ok"
}

function Wait-Window {
    param($procCond, [string]$titlePart, [int]$maxWait = 45)
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

Get-Process Clovent.Desktop -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Seconds 1

Log "=== LAUNCHING POS (--pos) ==="
$proc = Start-Process $ExePath -ArgumentList "--pos" -PassThru
$proc.Id | Out-File "$QaDir\v3_pid.txt" -Encoding ascii
Log "PID: $($proc.Id)"
$procCond = New-Object System.Windows.Automation.PropertyCondition(
    [System.Windows.Automation.AutomationElement]::ProcessIdProperty, $proc.Id)
$root = [System.Windows.Automation.AutomationElement]::RootElement

$posWin = Wait-Window -procCond $procCond -titlePart "Restaurant POS" -maxWait 60
if (-not $posWin) {
    Log "ERROR: POS window not found"
    $wins = $root.FindAll([System.Windows.Automation.TreeScope]::Children, $procCond)
    foreach ($w in $wins) { Log "  Window: '$($w.Current.Name)'" }
    exit 1
}
$posHwnd = [IntPtr]$posWin.Current.NativeWindowHandle

# Give data loads a moment, then maximize
Start-Sleep -Seconds 4
[QaV3.Win]::ShowWindow($posHwnd, 3) | Out-Null
Start-Sleep -Seconds 2
[QaV3.Win]::SetForegroundWindow($posHwnd) | Out-Null
Start-Sleep -Seconds 1

$dpi = [QaV3.Win]::GetDpiForWindow($posHwnd)
$scale = $dpi / 96.0
Log "POS DPI: $dpi (scale ${scale}x)"

# --- Capture 1: native maximized (3840x2400 @250%) ---
Capture-Hwnd $posHwnd "$QaDir\v3_pos_4k250_maximized.png" "POS native 250% maximized"

# --- Resize helper ---
function Size-And-Capture {
    param([int]$logicalW, [int]$logicalH, [string]$file, [string]$label)
    [QaV3.Win]::ShowWindow($posHwnd, 1) | Out-Null   # SW_RESTORE
    Start-Sleep -Milliseconds 600
    $physW = [int][Math]::Round($logicalW * $scale)
    $physH = [int][Math]::Round($logicalH * $scale)
    # Center on screen, allow partial off-screen for larger-than-screen
    $x = [int](($script:screenW - $physW) / 2)
    $y = [int](($script:screenH - $physH) / 2)
    [QaV3.Win]::MoveWindow($posHwnd, $x, $y, $physW, $physH, $true) | Out-Null
    Start-Sleep -Seconds 2
    [QaV3.Win]::SetForegroundWindow($posHwnd) | Out-Null
    Start-Sleep -Milliseconds 500
    Capture-Hwnd $posHwnd "$QaDir\$file" $label
}

$script:screenW = 3840; $script:screenH = 2400

Size-And-Capture 1024 768 "v3_pos_1024x768.png" "POS 1024x768 logical"
Size-And-Capture 1366 768 "v3_pos_1366x768.png" "POS 1366x768 logical"
Size-And-Capture 1920 1080 "v3_pos_1920x1080.png" "POS 1920x1080 logical"

# --- Restore maximized for interactive phases ---
[QaV3.Win]::ShowWindow($posHwnd, 3) | Out-Null
Start-Sleep -Seconds 2
Capture-Hwnd $posHwnd "$QaDir\v3_pos_final_maximized.png" "POS final maximized"

# --- Dump UIA tree for control names ---
function Dump-Uia($el, $depth, $sb) {
    foreach ($child in $el.FindAll([System.Windows.Automation.TreeScope]::Children, [System.Windows.Automation.Condition]::TrueCondition)) {
        $name = $child.Current.Name
        $type = $child.Current.ControlType.ProgrammaticName
        $autoid = $child.Current.AutomationId
        $rect = $child.Current.BoundingRectangle
        if ($type -match 'Button|Edit|Text|ListItem|DataItem|Tab|ComboBox|Custom|Pane|Table') {
            $sb.AppendLine(("{0}{1} '{2}' id={3} [{4:f0}x{5:f0}@{6:f0},{7:f0}]" -f ("  " * $depth), $type, $name, $autoid, $rect.Width, $rect.Height, $rect.X, $rect.Y)) | Out-Null
        }
        if ($depth -lt 9) { Dump-Uia $child ($depth + 1) $sb }
    }
}
$sb = New-Object System.Text.StringBuilder
Dump-Uia $posWin 0 $sb
$sb.ToString() | Out-File "$QaDir\v3_uia_pos.txt" -Encoding utf8
Log "UIA tree dumped: v3_uia_pos.txt"

Log "=== PHASE 1 COMPLETE (app left running, PID $($proc.Id)) ==="
