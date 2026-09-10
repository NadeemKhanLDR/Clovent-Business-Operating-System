Param([int]$ProcId, [string]$Find)
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
$root = [System.Windows.Automation.AutomationElement]::RootElement
$cond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ProcessIdProperty, $ProcId)
$wins = $root.FindAll([System.Windows.Automation.TreeScope]::Children, $cond)
foreach ($w in $wins) {
  Write-Output ("=== WINDOW '" + $w.Current.Name + "'")
  $nameCond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::NameProperty, $Find)
  $found = $w.FindAll([System.Windows.Automation.TreeScope]::Descendants, $nameCond)
  foreach ($f in $found) {
    $ct = $f.Current.ControlType.ProgrammaticName -replace 'ControlType\.',''
    $b = $f.Current.BoundingRectangle
    Write-Output ("  $ct '" + $f.Current.Name + "' rect=" + $b)
  }
  # partial-name fallback via AutomationId too
  if ($found.Count -eq 0) {
    $all = $w.FindAll([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.Condition]::TrueCondition)
    foreach ($a in $all) {
      if ($a.Current.Name -like "*$Find*" -or $a.Current.AutomationId -like "*$Find*") {
        $ct = $a.Current.ControlType.ProgrammaticName -replace 'ControlType\.',''
        Write-Output ("  ~ $ct '" + $a.Current.Name + "' id=" + $a.Current.AutomationId + " rect=" + $a.Current.BoundingRectangle)
      }
    }
  }
}
