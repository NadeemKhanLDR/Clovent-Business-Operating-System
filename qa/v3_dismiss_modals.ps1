param()
Add-Type -TypeDefinition @"
using System;
using System.Text;
using System.Runtime.InteropServices;
using System.Collections.Generic;
namespace QaV3 {
    public static class Enum2 {
        public delegate bool EnumProc(IntPtr h, IntPtr l);
        [DllImport("user32.dll")] public static extern bool EnumWindows(EnumProc cb, IntPtr l);
        [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
        [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr h);
        [DllImport("user32.dll")] public static extern bool IsWindowEnabled(IntPtr h);
        [DllImport("user32.dll")] public static extern IntPtr GetWindow(IntPtr h, uint cmd);
        [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
        [DllImport("user32.dll")] public static extern void mouse_event(uint f, uint a, uint b, uint c, int d);
        [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
        [DllImport("user32.dll")] public static extern bool SetProcessDpiAwarenessContext(IntPtr v);
        public static List<IntPtr> ModalWindows = new List<IntPtr>();
        public static IntPtr MainWnd = IntPtr.Zero;
        public static void Collect(uint pid) {
            ModalWindows.Clear();
            EnumWindows((h, l) => {
                uint p; GetWindowThreadProcessId(h, out p);
                if (p == pid) {
                    if (GetWindow(h, 4) == IntPtr.Zero && IsWindowVisible(h) && h != MainWnd && MainWnd != IntPtr.Zero) { }
                    // main window = biggest visible unowned
                    if (GetWindow(h, 4) == IntPtr.Zero && IsWindowVisible(h)) {
                        if (MainWnd == IntPtr.Zero) MainWnd = h;
                    }
                }
                return true;
            }, IntPtr.Zero);
            EnumWindows((h, l) => {
                uint p; GetWindowThreadProcessId(h, out p);
                if (p == pid && h != MainWnd && IsWindowVisible(h) && GetWindow(h, 4) == MainWnd) {
                    ModalWindows.Add(h);
                }
                return true;
            }, IntPtr.Zero);
        }
    }
}
"@
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
[QaV3.Enum2]::SetProcessDpiAwarenessContext([IntPtr](-4)) | Out-Null

$procId = [int](Get-Content "d:\Clovent Business Operating System\qa\v3_pid.txt")
[QaV3.Enum2]::Collect($procId)
Write-Output ("Main window: " + [QaV3.Enum2]::MainWnd)
Write-Output ("Owned visible popups: " + [QaV3.Enum2]::ModalWindows.Count)
foreach ($m in [QaV3.Enum2]::ModalWindows) { Write-Output ("  popup: " + $m) }

foreach ($m in [QaV3.Enum2]::ModalWindows) {
    # find OK button inside via UIA from hwnd
    $el = [System.Windows.Automation.AutomationElement]::FromHandle($m)
    $btn = $null
    foreach ($c in $el.FindAll([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.Condition]::TrueCondition)) {
        if ($c.Current.Name -in @('OK','Ok','Yes','&OK')) { $btn = $c; break }
    }
    if ($btn) {
        $rc = $btn.Current.BoundingRectangle
        [QaV3.Enum2]::SetCursorPos([int]($rc.X + $rc.Width/2), [int]($rc.Y + $rc.Height/2))
        Start-Sleep -Milliseconds 250
        [QaV3.Enum2]::mouse_event(2,0,0,0,0); Start-Sleep -Milliseconds 90; [QaV3.Enum2]::mouse_event(4,0,0,0,0)
        Write-Output ("Clicked OK in popup " + $m)
    } else {
        # fallback: press Enter while popup foreground
        [QaV3.Enum2]::SetForegroundWindow($m)
        Start-Sleep -Milliseconds 400
        [System.Windows.Forms.SendKeys]::SendWait("{ENTER}")
        Write-Output ("Sent ENTER to popup " + $m)
    }
    Start-Sleep -Milliseconds 900
}

[QaV3.Enum2]::MainWnd = [IntPtr]::Zero
[QaV3.Enum2]::Collect($procId)
Write-Output ("After dismissal - popups: " + [QaV3.Enum2]::ModalWindows.Count)
Add-Type -MemberDefinition '[DllImport("user32.dll")] public static extern bool IsWindowEnabled(IntPtr h);' -Name C -Namespace W2
Write-Output ("POS window enabled: " + [W2.C]::IsWindowEnabled([QaV3.Enum2]::MainWnd))
