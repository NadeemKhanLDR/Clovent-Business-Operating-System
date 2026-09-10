param()
Add-Type -TypeDefinition @"
using System;
using System.Runtime.InteropServices;
namespace QaV3 {
    public static class Win {
        [DllImport("user32.dll")] public static extern bool SetProcessDpiAwarenessContext(IntPtr v);
        [DllImport("user32.dll")] public static extern void mouse_event(uint f, uint x, uint y, uint d, int e);
        [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
        [DllImport("user32.dll")] public static extern bool GetAsyncKeyState(int k);
    }
}
"@
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
[QaV3.Win]::SetProcessDpiAwarenessContext([IntPtr](-4)) | Out-Null

$QaDir = "d:\Clovent Business Operating System\qa"
$procId = [int](Get-Content "$QaDir\v3_pid.txt")
$procCond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ProcessIdProperty, $procId)
$root = [System.Windows.Automation.AutomationElement]::RootElement
$pos = $null
foreach ($w in $root.FindAll([System.Windows.Automation.TreeScope]::Children, $procCond)) {
    if ($w.Current.Name -like "*Restaurant POS*") { $pos = $w; break }
}
if (-not $pos) { Write-Output "NO POS"; exit 1 }

$target = $null
foreach ($el in $pos.FindAll([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.Condition]::TrueCondition)) {
    if ($el.Current.Name -eq '+ Dine In') { $target = $el; break }
}
if (-not $target) { Write-Output "BUTTON NOT FOUND"; exit 1 }

Write-Output ("Found: '" + $target.Current.Name + "' type=" + $target.Current.ControlType.ProgrammaticName)
$rc = $target.Current.BoundingRectangle
Write-Output ("Rect: {0:f0},{1:f0} {2:f0}x{3:f0}" -f $rc.X, $rc.Y, $rc.Width, $rc.Height)
Write-Output ("IsEnabled: " + $target.Current.IsEnabled)
Write-Output ("IsOffscreen: " + $target.Current.IsOffscreen)

foreach ($p in $target.GetSupportedPatterns()) { Write-Output ("Pattern: " + $p.ProgrammaticName) }

# Try LegacyIAccessible DoDefaultAction
$lia = $null
if ($target.TryGetCurrentPattern([System.Windows.Automation.LegacyIAccessiblePattern]::Pattern, [ref]$lia)) {
    Write-Output "LegacyIAccessible available"
    try { $lia.DoDefaultAction(); Write-Output "DoDefaultAction OK"; Start-Sleep -Seconds 2 } catch { Write-Output ("DoDefaultAction failed: " + $_.Exception.Message) }
} else {
    Write-Output "No LegacyIAccessible pattern"
}

# check state
function Order-Active {
    $pos2 = $null
    foreach ($w in $root.FindAll([System.Windows.Automation.TreeScope]::Children, $procCond)) {
        if ($w.Current.Name -like "*Restaurant POS*") { $pos2 = $w; break }
    }
    if (-not $pos2) { return $false }
    foreach ($el in $pos2.FindAll([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.Condition]::TrueCondition)) {
        $n = $el.Current.Name
        if (($n -match '^ORD-\d+' -and $n.Length -lt 40) -or $n -eq 'Cancel Order') { return $true }
    }
    return $false
}
Write-Output ("Order active after DoDefaultAction: " + (Order-Active))

# list any new popup windows
foreach ($w in $root.FindAll([System.Windows.Automation.TreeScope]::Children, $procCond)) {
    Write-Output ("win: '" + $w.Current.Name + "'")
}
