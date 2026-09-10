Add-Type -TypeDefinition @"
using System;
using System.Runtime.InteropServices;
namespace QaV3 {
    public static class W {
        [DllImport("user32.dll")] public static extern bool SetProcessDpiAwarenessContext(IntPtr v);
        [DllImport("user32.dll")] public static extern bool PostMessage(IntPtr h, uint msg, IntPtr w, IntPtr l);
        [DllImport("user32.dll")] public static extern bool GetWindowText(IntPtr h, System.Text.StringBuilder s, int n);
        [DllImport("user32.dll")] public static extern int GetClassName(IntPtr h, System.Text.StringBuilder s, int n);
    }
}
"@
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
[QaV3.W]::SetProcessDpiAwarenessContext([IntPtr](-4)) | Out-Null

$procId = [int](Get-Content "d:\Clovent Business Operating System\qa\v3_pid.txt")
$procCond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ProcessIdProperty, $procId)
$root = [System.Windows.Automation.AutomationElement]::RootElement

Write-Output "=== windows ==="
foreach ($w in $root.FindAll([System.Windows.Automation.TreeScope]::Children, $procCond)) {
    Write-Output ("win: '" + $w.Current.Name + "' type=" + $w.Current.ControlType.ProgrammaticName)
}

$pos = $null
foreach ($w in $root.FindAll([System.Windows.Automation.TreeScope]::Children, $procCond)) {
    if ($w.Current.Name -like "*Restaurant POS*") { $pos = $w; break }
}
if (-not $pos) { Write-Output "NO POS"; exit 1 }

$target = $null
foreach ($el in $pos.FindAll([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.Condition]::TrueCondition)) {
    if ($el.Current.Name -eq '+ Dine In') { $target = $el; break }
}
if (-not $target) { Write-Output "NOT FOUND"; exit 1 }

try {
    $h = [IntPtr]$target.Current.NativeWindowHandle
    Write-Output ("NativeWindowHandle: " + $h)
    if ($h -ne [IntPtr]::Zero) {
        $cn = New-Object System.Text.StringBuilder 256
        [QaV3.W]::GetClassName($h, $cn, 256) | Out-Null
        Write-Output ("Class: " + $cn.ToString())
    }
} catch { Write-Output ("hwnd err: " + $_.Exception.Message) }

# PostMessage click at center (client coords packed in lParam)
$rc = $target.Current.BoundingRectangle
$x = [int]($rc.Width / 2); $y = [int]($rc.Height / 2)
$lparam = [IntPtr](($y -shl 16) -bor $x)
Write-Output ("PostMessage click at client " + $x + "," + $y)
[QaV3.W]::PostMessage($h, 0x200, [IntPtr]::Zero, $lparam) | Out-Null   # WM_MOUSEMOVE
Start-Sleep -Milliseconds 150
[QaV3.W]::PostMessage($h, 0x201, [IntPtr]1, $lparam) | Out-Null     # WM_LBUTTONDOWN
Start-Sleep -Milliseconds 120
[QaV3.W]::PostMessage($h, 0x202, [IntPtr]0, $lparam) | Out-Null     # WM_LBUTTONUP
Start-Sleep -Seconds 3

Write-Output "=== windows after click ==="
foreach ($w in $root.FindAll([System.Windows.Automation.TreeScope]::Children, $procCond)) {
    Write-Output ("win: '" + $w.Current.Name + "'")
}
# check order state
$pos2 = $null
foreach ($w in $root.FindAll([System.Windows.Automation.TreeScope]::Children, $procCond)) {
    if ($w.Current.Name -like "*Restaurant POS*") { $pos2 = $w; break }
}
$active = $false
foreach ($el in $pos2.FindAll([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.Condition]::TrueCondition)) {
    $n = $el.Current.Name
    if (($n -match '^ORD-\d+' -and $n.Length -lt 40) -or $n -eq 'Cancel Order' -or $n -eq 'T-02') { Write-Output ("STATE: '" + $n + "'"); $active = $true }
}
Write-Output ("Order/popup indicators found: " + $active)
