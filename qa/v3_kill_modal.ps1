param([int]$Hwnd)
Add-Type -TypeDefinition @"
using System;
using System.Runtime.InteropServices;
namespace QaV3 {
    public static class K {
        [DllImport("user32.dll")] public static extern bool PostMessage(IntPtr h, uint msg, IntPtr w, IntPtr l);
        [DllImport("user32.dll")] public static extern bool SetProcessDpiAwarenessContext(IntPtr v);
    }
}
"@
[QaV3.K]::SetProcessDpiAwarenessContext([IntPtr](-4)) | Out-Null
# WM_CLOSE the shadow wrapper first (owner of the message box visuals), then the box itself
[QaV3.K]::PostMessage([IntPtr]12586044, 0x0010, [IntPtr]0, [IntPtr]0) | Out-Null
Start-Sleep -Milliseconds 400
[QaV3.K]::PostMessage([IntPtr]10292828, 0x0010, [IntPtr]0, [IntPtr]0) | Out-Null
Start-Sleep -Seconds 1
Write-Output "WM_CLOSE posted to modal windows"
