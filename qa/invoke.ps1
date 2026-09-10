Param([int]$ProcId, [string]$Name, [int]$Index=0)
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
$root = [System.Windows.Automation.AutomationElement]::RootElement
$cond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ProcessIdProperty, $ProcId)
$wins = $root.FindAll([System.Windows.Automation.TreeScope]::Children, $cond)
$found = @()
foreach ($w in $wins) {
  $nameCond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::NameProperty, $Name)
  $els = $w.FindAll([System.Windows.Automation.TreeScope]::Descendants, $nameCond)
  foreach ($e in $els) { $found += $e }
}
if ($found.Count -eq 0) { Write-Output "NOT FOUND: $Name"; exit 1 }
$e = $found[$Index]
$r = $e.Current.BoundingRectangle
Write-Output ("FOUND " + $found.Count + " rect=[" + [int]$r.X + "," + [int]$r.Y + " " + [int]$r.Width + "x" + [int]$r.Height + "]")
$inv = $null
if ($e.TryGetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern, [ref]$inv)) {
  $inv.Invoke()
  Write-Output "INVOKED"
} else {
  Write-Output "NO INVOKE PATTERN"
}
