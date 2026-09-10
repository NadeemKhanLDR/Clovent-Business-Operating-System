Add-Type -TypeDefinition @"
using System;
using System.Text;
using System.Runtime.InteropServices;
using System.Collections.Generic;
namespace QaV3 {
    public static class Enum {
        public delegate bool EnumProc(IntPtr h, IntPtr l);
        [DllImport("user32.dll")] public static extern bool EnumWindows(EnumProc cb, IntPtr l);
        [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
        [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr h);
        [DllImport("user32.dll")] public static extern int GetWindowTextW(IntPtr h, StringBuilder s, int n);
        [DllImport("user32.dll")] public static extern int GetClassNameW(IntPtr h, StringBuilder s, int n);
        [DllImport("user32.dll")] public static extern IntPtr GetWindow(IntPtr h, uint cmd);
        [DllImport("user32.dll")] public static extern bool IsWindowEnabled(IntPtr h);
        [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
        [DllImport("user32.dll")] public static extern bool SetProcessDpiAwarenessContext(IntPtr v);
        public struct RECT { public int L, T, R, B; }
        public static List<string> Results = new List<string>();
        public static void Run(uint targetPid) {
            EnumWindows((h, l) => {
                uint pid; GetWindowThreadProcessId(h, out pid);
                if (pid == targetPid) {
                    var t = new StringBuilder(256); GetWindowTextW(h, t, 256);
                    var c = new StringBuilder(256); GetClassNameW(h, c, 256);
                    var owner = GetWindow(h, 4 /*GW_HWNDOWNER*/);
                    RECT r; GetWindowRect(h, out r);
                    Results.Add(string.Format("hwnd={0} class='{1}' text='{2}' visible={3} enabled={4} owner={5} rect=({6},{7})-({8},{9})",
                        h, c, t, IsWindowVisible(h), IsWindowEnabled(h), owner, r.L, r.T, r.R, r.B));
                }
                return true;
            }, IntPtr.Zero);
        }
    }
}
"@
[QaV3.Enum]::SetProcessDpiAwarenessContext([IntPtr](-4)) | Out-Null
$procId = [int](Get-Content "d:\Clovent Business Operating System\qa\v3_pid.txt")
[QaV3.Enum]::Run($procId)
foreach ($line in [QaV3.Enum]::Results) { Write-Output $line }
