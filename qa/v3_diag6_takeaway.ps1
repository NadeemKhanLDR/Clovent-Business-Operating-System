Add-Type -TypeDefinition @"
using System;
using System.Runtime.InteropServices;
namespace QaV3 {
    public static class M {
        [DllImport("user32.dll")] public static extern bool SetProcessDpiAwarenessContext(IntPtr v);
        [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
        [DllImport("user32.dll")] public static extern void mouse_event(uint f, uint a, uint b, uint c, int d);
        [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
    }
}
"@
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
[QaV3.M]::SetProcessDpiAwarenessContext([IntPtr](-4)) | Out-Null
$procId = [int](Get-Content "d:\Clovent Business Operating System\qa\v3_pid.txt")
$procCond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ProcessIdProperty, $procId)
$root = [System.Windows.Automation.AutomationElement]::RootElement

function GetPos {
    foreach ($w in $root.FindAll([System.Windows.Automation.TreeScope]::Children, $procCond)) {
        if ($w.Current.Name -like "*Restaurant POS*") { return $w }
    }
    return $null
}
function ClickXY([int]$x, [int]$y) {
    [QaV3.M]::SetCursorPos($x, $y) | Out-Null
    Start-Sleep -Milliseconds 400
    [QaV3.M]::mouse_event(2,0,0,0,0); Start-Sleep -Milliseconds 120; [QaV3.M]::mouse_event(4,0,0,0,0)
    Start-Sleep -Milliseconds 1000
}

$pos = GetPos
$posHwnd = [IntPtr]$pos.Current.NativeWindowHandle
[QaV3.M]::SetForegroundWindow($posHwnd) | Out-Null
Start-Sleep -Milliseconds 800

# find + Take Away element fresh
$ta = $null
foreach ($el in $pos.FindAll([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.Condition]::TrueCondition)) {
    if ($el.Current.Name -eq '+ Take Away') { $ta = $el; break }
}
$rc = $ta.Current.BoundingRectangle
Write-Output ("+ Take Away rect: {0:f0},{1:f0} {2:f0}x{3:f0} enabled={4}" -f $rc.X, $rc.Y, $rc.Width, $rc.Height, $ta.Current.IsEnabled)
$cx = [int]($rc.X + $rc.Width/2); $cy = [int]($rc.Y + $rc.Height/2)
ClickXY $cx $cy
Write-Output ("clicked at $cx,$cy")

# state
$pos2 = GetPos
$order = "?"
foreach ($el in $pos2.FindAll([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.Condition]::TrueCondition)) {
    $n = $el.Current.Name
    if ($n -match '^ORD-\d+' -and $n.Length -lt 40) { $order = $n; break }
}
Write-Output ("order pane: '$order'")
foreach ($w in $root.FindAll([System.Windows.Automation.TreeScope]::Children, $procCond)) {
    if ($w.Current.Name -and $w.Current.Name -notlike "*Restaurant POS*") { Write-Output ("other win: '" + $w.Current.Name + "'") }
}
