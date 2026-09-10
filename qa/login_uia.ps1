Param([int]$ProcId)
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
$root = [System.Windows.Automation.AutomationElement]::RootElement
$cond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ProcessIdProperty, $ProcId)
$wins = $root.FindAll([System.Windows.Automation.TreeScope]::Children, $cond)
foreach ($w in $wins) {
  Write-Output ("WIN: " + $w.Current.Name)
  $editCond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ControlTypeProperty, [System.Windows.Automation.ControlType]::Edit)
  $edits = $w.FindAll([System.Windows.Automation.TreeScope]::Descendants, $editCond)
  $i = 0
  foreach ($e in $edits) {
    Write-Output ("EDIT " + $i + ": name='" + $e.Current.Name + "' automationid='" + $e.Current.AutomationId + "'")
    $i++
  }
  $btnCond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ControlTypeProperty, [System.Windows.Automation.ControlType]::Button)
  $btns = $w.FindAll([System.Windows.Automation.TreeScope]::Descendants, $btnCond)
  foreach ($b in $btns) { Write-Output ("BTN: '" + $b.Current.Name + "'") }
}
