Add-Type -TypeDefinition '
using System;
using System.Runtime.InteropServices;
namespace Qa {
  public struct RECT { public int L, T, R, B; }
  public static class U {
    [DllImport("user32.dll")] public static extern bool SetProcessDpiAwarenessContext(IntPtr v);
    [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr h, IntPtr a, int x, int y, int cx, int cy, uint f);
    [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h, int cmd);
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
    [DllImport("user32.dll")] public static extern bool GetClientRect(IntPtr h, out RECT r);
    [DllImport("user32.dll")] public static extern int GetDpiForWindow(IntPtr h);
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
    [DllImport("user32.dll")] public static extern void mouse_event(uint f, uint dx, uint dy, uint data, int ex);
    [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr hwnd, IntPtr hdcBlt, uint nFlags);
  }
}'
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

[Qa.U]::SetProcessDpiAwarenessContext([IntPtr](-4)) | Out-Null

function Click-Element {
    param([System.Windows.Automation.AutomationElement]$el)
    $inv = $null
    if ($el.TryGetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern, [ref]$inv)) {
        $inv.Invoke()
        Start-Sleep -Milliseconds 600
        return $true
    }
    $r = $el.Current.BoundingRectangle
    if (-not $r.IsEmpty -and $r.Width -gt 0 -and $r.Height -gt 0) {
        $cx = [int]($r.X + $r.Width / 2)
        $cy = [int]($r.Y + $r.Height / 2)
        [Qa.U]::SetCursorPos($cx, $cy) | Out-Null
        Start-Sleep -Milliseconds 100
        [Qa.U]::mouse_event(2, 0, 0, 0, 0)
        Start-Sleep -Milliseconds 60
        [Qa.U]::mouse_event(4, 0, 0, 0, 0)
        Start-Sleep -Milliseconds 600
        return $true
    }
    return $false
}

function Capture-Hwnd {
    param([IntPtr]$hwnd, [string]$outPath)
    $r = New-Object Qa.RECT
    [Qa.U]::GetWindowRect($hwnd, [ref]$r) | Out-Null
    $w = $r.R - $r.L
    $h = $r.B - $r.T
    if ($w -le 0 -or $h -le 0) {
        $b = [System.Windows.Forms.Screen]::PrimaryScreen.Bounds
        $w = $b.Width; $h = $b.Height
        $r.L = 0; $r.T = 0
    }
    
    # Try PrintWindow first for pixel-perfect render content
    $bmp = New-Object System.Drawing.Bitmap($w, $h)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $hdc = $g.GetHdc()
    $pwOk = [Qa.U]::PrintWindow($hwnd, $hdc, 2)
    $g.ReleaseHdc($hdc)
    
    if (-not $pwOk) {
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

# 1. Kill any running Clovent.Desktop
Get-Process Clovent.Desktop -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Seconds 1

# 2. Launch Clovent.Desktop with --pos flag
$exe = "d:\Clovent Business Operating System\src\Clovent.Desktop\bin\Debug\net10.0-windows\Clovent.Desktop.exe"
Write-Output "Launching $exe --pos..."
$proc = Start-Process $exe -ArgumentList "--pos" -PassThru
$procId = $proc.Id
Write-Output "Started with PID $procId"

$root = [System.Windows.Automation.AutomationElement]::RootElement
$procCond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ProcessIdProperty, $procId)

# 3. Wait for Restaurant POS form (bypassing splash screen)
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
    Write-Output "ERROR: Could not find Restaurant POS window"
    exit 1
}

Write-Output "POS Window found: '$($posWin.Current.Name)'"
$posHwnd = [IntPtr]$posWin.Current.NativeWindowHandle
[Qa.U]::ShowWindow($posHwnd, 3) | Out-Null # Maximize
Start-Sleep -Seconds 2
[Qa.U]::SetForegroundWindow($posHwnd) | Out-Null
Start-Sleep -Milliseconds 600

Capture-Hwnd $posHwnd "qa\qa_01_startup_pos.png"

# 4. Find Recall button (without restriction to Button ControlType)
$allElements = $posWin.FindAll([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.Condition]::TrueCondition)
$recallBtn = $null
foreach ($el in $allElements) {
    if ($el.Current.Name -eq "Recall") {
        $recallBtn = $el
        break
    }
}

if (-not $recallBtn) {
    Write-Output "ERROR: Recall button could not be located."
    exit 1
}

Write-Output "Found Recall element: Bounds = $($recallBtn.Current.BoundingRectangle). Clicking..."
Click-Element $recallBtn | Out-Null
Start-Sleep -Seconds 3

# 5. Find Recall / Sales History dialog
$dialog = $null
for ($i = 0; $i -lt 20; $i++) {
    $wins = $root.FindAll([System.Windows.Automation.TreeScope]::Children, $procCond)
    foreach ($w in $wins) {
        if ($w.Current.Name -like "*Recall*" -or $w.Current.Name -like "*Sales History*") {
            $dialog = $w
            break
        }
    }
    if ($dialog) { break }
    Start-Sleep -Milliseconds 500
}

if (-not $dialog) {
    Write-Output "ERROR: Recall / Sales History dialog did not open!"
    exit 1
}

Write-Output "Recall dialog found: '$($dialog.Current.Name)'"
$dhwnd = [IntPtr]$dialog.Current.NativeWindowHandle
[Qa.U]::SetForegroundWindow($dhwnd) | Out-Null
Start-Sleep -Milliseconds 800

$dpi = [Qa.U]::GetDpiForWindow($dhwnd)
$rect = New-Object Qa.RECT
[Qa.U]::GetWindowRect($dhwnd, [ref]$rect) | Out-Null
$dw = $rect.R - $rect.L
$dh = $rect.B - $rect.T
Write-Output "LIVE RECALL DIALOG: Bounds = ${dw}x${dh} at DPI = $dpi"

# Capture live dialog at 240 DPI
Capture-Hwnd $dhwnd "qa\qa_02_recall_held.png"
Capture-Hwnd $dhwnd "qa\final_4k_recall.png"

# Inspect child elements in dialog
$dialogElements = $dialog.FindAll([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.Condition]::TrueCondition)
$closedTab = $null
$voidedTab = $null
$heldTab = $null
$recallActionBtn = $null

foreach ($b in $dialogElements) {
    if ($b.Current.Name -eq "CLOSED") { $closedTab = $b }
    elseif ($b.Current.Name -eq "VOIDED") { $voidedTab = $b }
    elseif ($b.Current.Name -eq "HELD") { $heldTab = $b }
    elseif ($b.Current.Name -like "*Recall Order*") { $recallActionBtn = $b }
}

if ($recallActionBtn) {
    Write-Output "Recall Order action button found: '$($recallActionBtn.Current.Name)', Bounds: $($recallActionBtn.Current.BoundingRectangle)"
} else {
    Write-Output "Recall Order action button not yet named 'Recall Order' (disabled or row unfocused)"
}

if ($closedTab) {
    Write-Output "Switching to CLOSED tab..."
    Click-Element $closedTab | Out-Null
    Start-Sleep -Seconds 1
    Capture-Hwnd $dhwnd "qa\qa_03_recall_closed.png"
}

if ($voidedTab) {
    Write-Output "Switching to VOIDED tab..."
    Click-Element $voidedTab | Out-Null
    Start-Sleep -Seconds 1
    Capture-Hwnd $dhwnd "qa\qa_04_recall_voided.png"
}

if ($heldTab) {
    Write-Output "Switching back to HELD tab..."
    Click-Element $heldTab | Out-Null
    Start-Sleep -Seconds 1
    Capture-Hwnd $dhwnd "qa\qa_05_recall_held_again.png"
}

# Close Recall dialog
Write-Output "Closing dialog..."
[Qa.U]::SetForegroundWindow($dhwnd) | Out-Null
Start-Sleep -Milliseconds 200

# Try Close button
$closeBtn = $null
foreach ($b in $dialogElements) {
    if ($b.Current.Name -eq "Close") { $closeBtn = $b; break }
}
if ($closeBtn) {
    Click-Element $closeBtn | Out-Null
}

Start-Sleep -Seconds 1
Get-Process Clovent.Desktop -ErrorAction SilentlyContinue | Stop-Process -Force
Write-Output "LIVE QA AUTOMATION FINISHED SUCCESSFULLY."
