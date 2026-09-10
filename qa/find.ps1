Param([int]$ProcId, [string]$Name)
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
Add-Type -MemberDefinition '[DllImport("user32.dll")] public static extern bool SetProcessDpiAwarenessContext(IntPtr v);' -Name D -Namespace W
[W.D]::SetProcessDpiAwarenessContext([IntPtr](-4)) | Out-Null
$root = [System.Windows.Automation.AutomationElement]::RootElement
$cond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ProcessIdProperty, $ProcId)
$wins = $root.FindAll([System.Windows.Automation.TreeScope]::Children, $cond)
foreach ($w in $wins) {
  $nameCond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::NameProperty, $Name)
  $els = $w.FindAll([System.Windows.Automation.TreeScope]::Descendants, $nameCond)
  foreach ($e in $els) {
    $r = $e.Current.BoundingRectangle
    Write-Output ("rect=" + [int]$r.X + "," + [int]$r.Y + " " + [int]$r.Width + "x" + [int]$r.Height + " type=" + $e.Current.ControlType.ProgrammaticName)
  }
}
