# Shared helpers for v3 QA phases - dot-source this file
Add-Type -TypeDefinition @"
using System;
using System.Text;
using System.Runtime.InteropServices;
using System.Collections.Generic;
namespace QaV3 {
    public struct RECT { public int L, T, R, B; }
    public static class E {
        public delegate bool EnumProc(IntPtr h, IntPtr l);
        [DllImport("user32.dll")] public static extern bool EnumWindows(EnumProc cb, IntPtr l);
        [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
        [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr h);
        [DllImport("user32.dll")] public static extern bool IsWindowEnabled(IntPtr h);
        [DllImport("user32.dll")] public static extern IntPtr GetWindow(IntPtr h, uint cmd);
        [DllImport("user32.dll", CharSet=CharSet.Unicode)] public static extern int GetWindowText(IntPtr h, StringBuilder s, int n);
        [DllImport("user32.dll", CharSet=CharSet.Unicode)] public static extern int GetClassName(IntPtr h, StringBuilder s, int n);
        [DllImport("user32.dll")] public static extern bool PostMessage(IntPtr h, uint msg, IntPtr w, IntPtr l);
        [DllImport("user32.dll")] public static extern bool SetProcessDpiAwarenessContext(IntPtr v);
        [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
        [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h, int cmd);
        [DllImport("user32.dll")] public static extern void mouse_event(uint f, uint a, uint b, uint c, int d);
        [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
        [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
        [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr h, IntPtr hdc, uint flags);
        public static IntPtr Main = IntPtr.Zero;
        public static void FindMain(uint pid) {
            Main = IntPtr.Zero;
            EnumWindows((h, l) => {
                uint p; GetWindowThreadProcessId(h, out p);
                if (p == pid && GetWindow(h, 4) == IntPtr.Zero && IsWindowVisible(h)) {
                    var t = new StringBuilder(256); GetWindowText(h, t, 256);
                    if (t.ToString().Contains("Restaurant POS")) Main = h;
                }
                return true;
            }, IntPtr.Zero);
        }
        public static List<IntPtr> Owned(uint pid) {
            var list = new List<IntPtr>();
            EnumWindows((h, l) => {
                uint p; GetWindowThreadProcessId(h, out p);
                if (p == pid && h != Main && IsWindowVisible(h) && GetWindow(h, 4) == Main) list.Add(h);
                return true;
            }, IntPtr.Zero);
            return list;
        }
    }
}
"@
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing
[QaV3.E]::SetProcessDpiAwarenessContext([IntPtr](-4)) | Out-Null

$script:QaDir = "d:\Clovent Business Operating System\qa"
$script:procId = [int](Get-Content "$script:QaDir\v3_pid.txt")
$script:procCond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ProcessIdProperty, $script:procId)
$script:root = [System.Windows.Automation.AutomationElement]::RootElement

function Log($m) { Write-Output $m }
function GetPos {
    foreach ($w in $script:root.FindAll([System.Windows.Automation.TreeScope]::Children, $script:procCond)) {
        if ($w.Current.Name -like "*Restaurant POS*") { return $w }
    }
    return $null
}
function Get-AnyWindow([string]$titlePart) {
    foreach ($w in $script:root.FindAll([System.Windows.Automation.TreeScope]::Children, $script:procCond)) {
        if ($w.Current.Name -like "*$titlePart*") { return $w }
    }
    return $null
}
function ClickXY([int]$x, [int]$y, [int]$delay = 800) {
    [QaV3.E]::SetCursorPos($x, $y) | Out-Null
    Start-Sleep -Milliseconds 350
    [QaV3.E]::mouse_event(2,0,0,0,0); Start-Sleep -Milliseconds 110; [QaV3.E]::mouse_event(4,0,0,0,0)
    Start-Sleep -Milliseconds $delay
}
function Capture-H([IntPtr]$hwnd, [string]$file, [string]$label) {
    $r = New-Object QaV3.RECT
    [QaV3.E]::GetWindowRect($hwnd, [ref]$r) | Out-Null
    $bw = $r.R - $r.L; $bh = $r.B - $r.T
    if ($bw -le 0 -or $bh -le 0) { Log "WARN bad rect for $label"; return }
    $bmp = New-Object System.Drawing.Bitmap($bw, $bh)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $hdc = $g.GetHdc()
    [QaV3.E]::PrintWindow($hwnd, $hdc, 2) | Out-Null
    $g.ReleaseHdc($hdc); $g.Dispose()
    $bmp.Save("$script:QaDir\$file", [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
    Log "CAPTURED $label => $file (${bw}x${bh})"
}
function Capture-Pos([string]$file, [string]$label) {
    [QaV3.E]::FindMain($script:procId)
    if ([QaV3.E]::Main -eq [IntPtr]::Zero) { Log "WARN no main window"; return }
    Capture-H ([QaV3.E]::Main) $file $label
}
function Guard-Modals {
    [QaV3.E]::FindMain($script:procId)
    $owned = [QaV3.E]::Owned($script:procId)
    foreach ($o in $owned) {
        $r = New-Object QaV3.RECT
        [QaV3.E]::GetWindowRect($o, [ref]$r) | Out-Null
        if (($r.R - $r.L) -lt 300 -and ($r.B - $r.T) -lt 200) { continue }
        Log ("guard: closing owned popup @({0},{1})-({2},{3})" -f $r.L, $r.T, $r.R, $r.B)
        [QaV3.E]::PostMessage($o, 0x0010, [IntPtr]0, [IntPtr]0) | Out-Null
        Start-Sleep -Milliseconds 700
    }
}
function Dump-Pos([string]$file) {
    $pos = GetPos
    if (-not $pos) { Log "WARN no pos"; return }
    $sb = New-Object System.Text.StringBuilder
    foreach ($el in $pos.FindAll([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.Condition]::TrueCondition)) {
        $n = $el.Current.Name
        if ($n -ne "" -and $n.Length -lt 120) {
            $rc = $el.Current.BoundingRectangle
            $sb.AppendLine("'" + $n + "' [" + [int]$rc.Width + "x" + [int]$rc.Height + "@" + [int]$rc.X + "," + [int]$rc.Y + "]") | Out-Null
        }
    }
    $sb.ToString() | Out-File "$script:QaDir\$file" -Encoding utf8
    Log "dump => $file"
}
function Get-OrderPaneText {
    $pos = GetPos
    if (-not $pos) { return "" }
    foreach ($el in $pos.FindAll([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.Condition]::TrueCondition)) {
        $n = $el.Current.Name
        if ($n -match '^ORD-\d+' -and $n.Length -lt 40) { return $n }
    }
    return ""
}
function Get-RightPanelPane([string]$pattern) {
    $pos = GetPos
    foreach ($el in $pos.FindAll([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.Condition]::TrueCondition)) {
        $n = $el.Current.Name
        $rc = $el.Current.BoundingRectangle
        if ($n -match $pattern -and $rc.X -gt 2700) { return $el }
    }
    return $null
}
