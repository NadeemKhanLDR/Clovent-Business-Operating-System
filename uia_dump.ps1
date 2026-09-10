Param([int]$ProcId)
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes

function Dump {
  param([System.Windows.Automation.AutomationElement]$el, [int]$depth)
  $kids = $el.FindAll([System.Windows.Automation.TreeScope]::Children, [System.Windows.Automation.Condition]::TrueCondition)
  foreach ($k in $kids) {
    $r = $k.Current.BoundingRectangle
    $indent = '  ' * $depth
    $rect = if ($r.IsEmpty) { "(empty)" } else { "[$([int]$r.X),$([int]$r.Y) $([int]$r.Width)x$([int]$r.Height)]" }
    Write-Output "$indent$($k.Current.ControlType.ProgrammaticName) '$($k.Current.Name)' $rect"
    if ($depth -lt 14) { Dump -el $k -depth ($depth + 1) }
  }
}

$root = [System.Windows.Automation.AutomationElement]::RootElement
$cond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ProcessIdProperty, $ProcId)
$wins = $root.FindAll([System.Windows.Automation.TreeScope]::Children, $cond)
foreach ($w in $wins) {
  $r = $w.Current.BoundingRectangle
  Write-Output "WINDOW: '$($w.Current.Name)' [$([int]$r.X),$([int]$r.Y) $([int]$r.Width)x$([int]$r.Height)]"
  Dump -el $w -depth 0
}
