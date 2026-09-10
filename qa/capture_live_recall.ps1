Add-Type -TypeDefinition '
using System;
using System.Text;
using System.Runtime.InteropServices;
namespace QaLive {
  public struct RECT { public int L, T, R, B; }
  public static class W {
    [DllImport("user32.dll")] public static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
    [DllImport("user32.dll")] public static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
    [DllImport("user32.dll")] public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr hWnd, int cmd);
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hWnd, out RECT r);
    [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr hwnd, IntPtr hdcBlt, uint nFlags);
    [DllImport("user32.dll")] public static extern int GetDpiForWindow(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool SetProcessDpiAwarenessContext(IntPtr v);
  }
}'
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

[QaLive.W]::SetProcessDpiAwarenessContext([IntPtr](-4)) | Out-Null

function Capture-WindowHwnd {
    param([IntPtr]$hwnd, [string]$outPath)
    $r = New-Object QaLive.RECT
    [QaLive.W]::GetWindowRect($hwnd, [ref]$r) | Out-Null
    $w = $r.R - $r.L
    $h = $r.B - $r.T
    if ($w -le 0 -or $h -le 0) {
        Write-Output "Invalid window dimensions: $w x $h"
        return
    }

    $bmp = New-Object System.Drawing.Bitmap($w, $h)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $hdc = $g.GetHdc()
    $ok = [QaLive.W]::PrintWindow($hwnd, $hdc, 2)
    $g.ReleaseHdc($hdc)

    if (-not $ok) {
        $g.Dispose()
        $bmp.Dispose()
        $bmp = New-Object System.Drawing.Bitmap($w, $h)
        $g = [System.Drawing.Graphics]::FromImage($bmp)
        $sl = [Math]::Max(0, $r.L)
        $st = [Math]::Max(0, $r.T)
        $sw = [Math]::Min($w, [System.Windows.Forms.Screen]::PrimaryScreen.Bounds.Width - $sl)
        $sh = [Math]::Min($h, [System.Windows.Forms.Screen]::PrimaryScreen.Bounds.Height - $st)
        $g.CopyFromScreen($sl, $st, 0, 0, (New-Object System.Drawing.Size($sw, $sh)))
    }

    $g.Dispose()
    $bmp.Save($outPath, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
    Write-Output "CAPTURED $outPath ($w x $h)"
}

# 1. Kill any existing
Get-Process Clovent.Desktop -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Seconds 1

# 2. Start Clovent.Desktop --pos
$exe = "d:\Clovent Business Operating System\src\Clovent.Desktop\bin\Debug\net10.0-windows\Clovent.Desktop.exe"
$p = Start-Process $exe -ArgumentList "--pos" -PassThru
Write-Output "Launched PID $($p.Id)"

$root = [System.Windows.Automation.AutomationElement]::RootElement
$procCond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ProcessIdProperty, $p.Id)

# 3. Wait for POS window
$posWin = $null
for ($i = 0; $i -lt 40; $i++) {
    Start-Sleep -Seconds 1
    $wins = $root.FindAll([System.Windows.Automation.TreeScope]::Children, $procCond)
    foreach ($w in $wins) {
        if ($w.Current.Name -eq "Restaurant POS") {
            $posWin = $w
            break
        }
    }
    if ($posWin) { break }
}

if (-not $posWin) {
    Write-Output "ERROR: POS window not found"
    exit 1
}

$posHwnd = [IntPtr]$posWin.Current.NativeWindowHandle
[QaLive.W]::ShowWindow($posHwnd, 3) | Out-Null # Maximize
Start-Sleep -Seconds 2
[QaLive.W]::SetForegroundWindow($posHwnd) | Out-Null
Start-Sleep -Milliseconds 600

Capture-WindowHwnd $posHwnd "qa\qa_01_startup_pos.png"

# 4. Find Recall button HWND and click it
$all = $posWin.FindAll([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.Condition]::TrueCondition)
$recallHwnd = [IntPtr]::Zero
foreach ($el in $all) {
    if ($el.Current.Name -eq "Recall") {
        $recallHwnd = [IntPtr]$el.Current.NativeWindowHandle
        break
    }
}

if ($recallHwnd -eq [IntPtr]::Zero) {
    Write-Output "ERROR: Recall button HWND not found"
    exit 1
}

Write-Output "Triggering Recall button (HWND: $recallHwnd)..."
[QaLive.W]::PostMessage($recallHwnd, 0x00F5, [IntPtr]::Zero, [IntPtr]::Zero) | Out-Null
[QaLive.W]::PostMessage($recallHwnd, 0x0201, [IntPtr]1, [IntPtr]::Zero) | Out-Null
[QaLive.W]::PostMessage($recallHwnd, 0x0202, [IntPtr]0, [IntPtr]::Zero) | Out-Null
Start-Sleep -Seconds 3

# 5. Locate Recall dialog
$dhwnd = [IntPtr]::Zero
for ($i = 0; $i -lt 25; $i++) {
    $wins = $root.FindAll([System.Windows.Automation.TreeScope]::Descendants, $procCond)
    foreach ($w in $wins) {
        if ($w.Current.Name -like "*Recall*" -or $w.Current.Name -like "*Sales History*") {
            $h = [IntPtr]$w.Current.NativeWindowHandle
            if ($h -ne [IntPtr]::Zero -and $h -ne $posHwnd -and $h -ne $recallHwnd) {
                $sb = New-Object System.Text.StringBuilder 256
                [QaLive.W]::GetWindowText($h, $sb, 256) | Out-Null
                $title = $sb.ToString()
                if ($title -like "*Recall*" -or $title -like "*Sales History*") {
                    $dhwnd = $h
                    Write-Output "Found Dialog HWND: $dhwnd Title: '$title'"
                    break
                }
            }
        }
    }
    if ($dhwnd -ne [IntPtr]::Zero) { break }
    Start-Sleep -Milliseconds 500
}

if ($dhwnd -eq [IntPtr]::Zero) {
    Write-Output "ERROR: Recall / Sales History dialog window not found"
    exit 1
}

[QaLive.W]::SetForegroundWindow($dhwnd) | Out-Null
Start-Sleep -Milliseconds 800

$dpi = [QaLive.W]::GetDpiForWindow($dhwnd)
$rect = New-Object QaLive.RECT
[QaLive.W]::GetWindowRect($dhwnd, [ref]$rect) | Out-Null
$dw = $rect.R - $rect.L
$dh = $rect.B - $rect.T
Write-Output "LIVE RECALL DIALOG: Bounds = ${dw}x${dh} at DPI = $dpi"

# Capture initial Held view at 240 DPI
Capture-WindowHwnd $dhwnd "qa\qa_02_recall_held.png"
Capture-WindowHwnd $dhwnd "qa\final_4k_recall.png"

# Find controls inside dialog
$dialogWin = [System.Windows.Automation.AutomationElement]::FromHandle($dhwnd)
$dElements = $dialogWin.FindAll([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.Condition]::TrueCondition)

$closedHwnd = [IntPtr]::Zero
$openHwnd = [IntPtr]::Zero
$voidedHwnd = [IntPtr]::Zero
$heldHwnd = [IntPtr]::Zero

foreach ($el in $dElements) {
    if ($el.Current.Name -eq "CLOSED") { $closedHwnd = [IntPtr]$el.Current.NativeWindowHandle }
    elseif ($el.Current.Name -eq "OPEN") { $openHwnd = [IntPtr]$el.Current.NativeWindowHandle }
    elseif ($el.Current.Name -eq "VOIDED") { $voidedHwnd = [IntPtr]$el.Current.NativeWindowHandle }
    elseif ($el.Current.Name -eq "HELD") { $heldHwnd = [IntPtr]$el.Current.NativeWindowHandle }
}

# Test OPEN tab
if ($openHwnd -ne [IntPtr]::Zero) {
    Write-Output "Clicking OPEN tab (HWND: $openHwnd)..."
    [QaLive.W]::PostMessage($openHwnd, 0x0201, [IntPtr]1, [IntPtr]::Zero) | Out-Null
    [QaLive.W]::PostMessage($openHwnd, 0x0202, [IntPtr]0, [IntPtr]::Zero) | Out-Null
    Start-Sleep -Seconds 2
    Capture-WindowHwnd $dhwnd "qa\qa_open_tab.png"
    # Select first row
    [System.Windows.Forms.SendKeys]::SendWait("{DOWN}")
    Start-Sleep -Seconds 2
    Capture-WindowHwnd $dhwnd "qa\qa_open_tab_selected.png"
}

# Test CLOSED tab
if ($closedHwnd -ne [IntPtr]::Zero) {
    Write-Output "Clicking CLOSED tab (HWND: $closedHwnd)..."
    [QaLive.W]::PostMessage($closedHwnd, 0x0201, [IntPtr]1, [IntPtr]::Zero) | Out-Null
    [QaLive.W]::PostMessage($closedHwnd, 0x0202, [IntPtr]0, [IntPtr]::Zero) | Out-Null
    Start-Sleep -Seconds 2
    Capture-WindowHwnd $dhwnd "qa\qa_03_recall_closed.png"
}

# Test VOIDED tab
if ($voidedHwnd -ne [IntPtr]::Zero) {
    Write-Output "Clicking VOIDED tab (HWND: $voidedHwnd)..."
    [QaLive.W]::PostMessage($voidedHwnd, 0x0201, [IntPtr]1, [IntPtr]::Zero) | Out-Null
    [QaLive.W]::PostMessage($voidedHwnd, 0x0202, [IntPtr]0, [IntPtr]::Zero) | Out-Null
    Start-Sleep -Seconds 2
    Capture-WindowHwnd $dhwnd "qa\qa_04_recall_voided.png"
}

# Switch back to HELD tab
if ($heldHwnd -ne [IntPtr]::Zero) {
    Write-Output "Clicking HELD tab (HWND: $heldHwnd)..."
    [QaLive.W]::PostMessage($heldHwnd, 0x0201, [IntPtr]1, [IntPtr]::Zero) | Out-Null
    [QaLive.W]::PostMessage($heldHwnd, 0x0202, [IntPtr]0, [IntPtr]::Zero) | Out-Null
    Start-Sleep -Seconds 2
    Capture-WindowHwnd $dhwnd "qa\qa_05_recall_held_again.png"
}

# Close Recall dialog
[QaLive.W]::PostMessage($dhwnd, 0x0010, [IntPtr]::Zero, [IntPtr]::Zero) | Out-Null # WM_CLOSE
Start-Sleep -Seconds 1
Get-Process Clovent.Desktop -ErrorAction SilentlyContinue | Stop-Process -Force
Write-Output "LIVE CAPTURE COMPLETE."
