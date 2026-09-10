Param([int]$ProcId)
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
$root = [System.Windows.Automation.AutomationElement]::RootElement
$cond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ProcessIdProperty, $ProcId)
$wins = $root.FindAll([System.Windows.Automation.TreeScope]::Children, $cond)
foreach ($w in $wins) {
  $edits = $w.FindAll([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.Condition]::TrueCondition)
  foreach ($e in $edits) {
    if ($e.Current.ControlType.ProgrammaticName -eq 'ControlType.Edit' -or $e.GetSupportedPatterns() -contains [System.Windows.Automation.ValuePattern]::Pattern) {
      $v = $e.GetCurrentPropertyValue([System.Windows.Automation.AutomationElement]::ValueProperty, $true)
      $r = $e.Current.BoundingRectangle
      Write-Output ("EDIT value=' " + $v + " ' [" + [int]$r.X + "," + [int]$r.Y + "]")
    }
  }
}
