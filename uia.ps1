Param([int]$ProcId, [string]$WinTitle, [int]$MaxDepth = 12)
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
$root = [System.Windows.Automation.AutomationElement]::RootElement
$cond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ProcessIdProperty, $ProcId)
$wins = $root.FindAll([System.Windows.Automation.TreeScope]::Children, $cond)
function Dump($el, $depth) {
  if ($depth -gt $MaxDepth) { return }
  $n = $el.Current.Name; $ct = $el.Current.ControlType.ProgrammaticName -replace 'ControlType\.',''
  $aid = $el.Current.AutomationId
  $cls = $el.Current.ClassName
  $b = $el.Current.BoundingRectangle
  $click = ''
  if (-not $b.IsEmpty -and $b.Width -gt 0) { $click = (' [{0},{1}]' -f [int]($b.X + $b.Width/2), [int]($b.Y + $b.Height/2)) }
  Write-Output (' ' * $depth + "$ct | name='$n' | id='$aid' | cls='$cls'$click")
  $kids = $el.FindAll([System.Windows.Automation.TreeScope]::Children, [System.Windows.Automation.Condition]::TrueCondition)
  foreach ($k in $kids) { Dump $k ($depth + 1) }
}
foreach ($w in $wins) {
  if ($WinTitle -and $w.Current.Name -notlike "*$WinTitle*") { continue }
  Write-Output ("=== WINDOW: '" + $w.Current.Name + "' rect=" + $w.Current.BoundingRectangle)
  Dump $w 0
}
