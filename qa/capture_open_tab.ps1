Add-Type -TypeDefinition @"
using System;
using System.Runtime.InteropServices;
namespace QaOpen {
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
Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes, System.Windows.Forms, System.Drawing
[QaOpen.Win]::SetProcessDpiAwarenessContext([IntPtr](-4)) | Out-Null

$ExePath = 'd:\Clovent Business Operating System\src\Clovent.Desktop\bin\Debug\net10.0-windows\Clovent.Desktop.exe'
$QaDir = 'd:\Clovent Business Operating System\qa'

function CaptureHwnd {
    param([IntPtr]$hwnd, [string]$path)
    $r = New-Object QaOpen.RECT
    [QaOpen.Win]::GetWindowRect($hwnd, [ref]$r) | Out-Null
    $w = $r.R - $r.L; $h = $r.B - $r.T
    $bmp = New-Object System.Drawing.Bitmap($w, $h)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $hdc = $g.GetHdc()
    [QaOpen.Win]::PrintWindow($hwnd, $hdc, 2) | Out-Null
    $g.ReleaseHdc($hdc); $g.Dispose()
    $bmp.Save($path, [System.Drawing.Imaging.ImageFormat]::Png); $bmp.Dispose()
    Write-Output "CAPTURED: $(Split-Path $path -Leaf) - ${w}x${h}"
}

function ClickUIA {
    param([System.Windows.Automation.AutomationElement]$el)
    $inv = $null
    if ($el.TryGetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern, [ref]$inv)) {
        $inv.Invoke(); Start-Sleep -Milliseconds 600; return
    }
    $rect = $el.Current.BoundingRectangle
    $cx = [int]($rect.X + $rect.Width / 2); $cy = [int]($rect.Y + $rect.Height / 2)
    [QaOpen.Win]::SetCursorPos($cx, $cy) | Out-Null; Start-Sleep -Milliseconds 150
    [QaOpen.Win]::mouse_event(2, 0, 0, 0, 0); Start-Sleep -Milliseconds 80; [QaOpen.Win]::mouse_event(4, 0, 0, 0, 0)
    Start-Sleep -Milliseconds 600
}

Get-Process Clovent.Desktop -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Seconds 1

$proc = Start-Process $ExePath -ArgumentList "--pos" -PassThru
$procCond = New-Object System.Windows.Automation.PropertyCondition(
    [System.Windows.Automation.AutomationElement]::ProcessIdProperty, $proc.Id)
$root = [System.Windows.Automation.AutomationElement]::RootElement

$posWin = $null
for ($i = 0; $i -lt 45 -and (-not $posWin); $i++) {
    $wins = $root.FindAll([System.Windows.Automation.TreeScope]::Children, $procCond)
    foreach ($w in $wins) { if ($w.Current.Name -like "*Restaurant POS*") { $posWin = $w; break } }
    if (-not $posWin) { Start-Sleep -Seconds 1 }
}
if (-not $posWin) { Write-Output "ERROR: POS not found"; exit 1 }

$posHwnd = [IntPtr]$posWin.Current.NativeWindowHandle
[QaOpen.Win]::ShowWindow($posHwnd, 3) | Out-Null
Start-Sleep -Seconds 2
[QaOpen.Win]::SetForegroundWindow($posHwnd) | Out-Null
Start-Sleep -Milliseconds 800

# Click Recall
$cond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::NameProperty, "Recall")
$recallEl = $posWin.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $cond)
if (-not $recallEl) { Write-Output "ERROR: Recall button not found"; exit 1 }
ClickUIA $recallEl
Start-Sleep -Seconds 3

$recallWin = $null
for ($i = 0; $i -lt 20 -and (-not $recallWin); $i++) {
    $wins = $root.FindAll([System.Windows.Automation.TreeScope]::Children, $procCond)
    foreach ($w in $wins) {
        $h = [IntPtr]$w.Current.NativeWindowHandle
        if ($h -ne $posHwnd -and ($w.Current.Name -like "*Recall*" -or $w.Current.Name -like "*Sales History*")) {
            $recallWin = $w; break
        }
    }
    if (-not $recallWin) { Start-Sleep -Milliseconds 500 }
}
if (-not $recallWin) { Write-Output "ERROR: Recall dialog not found"; exit 1 }

$rHwnd = [IntPtr]$recallWin.Current.NativeWindowHandle
[QaOpen.Win]::SetForegroundWindow($rHwnd) | Out-Null; Start-Sleep -Milliseconds 800
Write-Output "Recall dialog: $($recallWin.Current.Name)"

# Click OPEN tab
$openCond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::NameProperty, "OPEN")
$openTab = $recallWin.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $openCond)
if ($openTab) { ClickUIA $openTab; Start-Sleep -Seconds 2; Write-Output "Clicked OPEN tab" }

CaptureHwnd $rHwnd "$QaDir\accept_recall_open_tab.png"

# Select first row
[QaOpen.Win]::SetForegroundWindow($rHwnd) | Out-Null; Start-Sleep -Milliseconds 300
[System.Windows.Forms.SendKeys]::SendWait("{DOWN}"); Start-Sleep -Seconds 2
CaptureHwnd $rHwnd "$QaDir\accept_recall_open_selected.png"

Start-Sleep -Seconds 1
Get-Process Clovent.Desktop -ErrorAction SilentlyContinue | Stop-Process -Force
Write-Output "Done."
