Param([int]$ProcId, [string]$WinName, [string]$SetName, [string]$Value)
# Find window by name part, set value of element whose Name contains $SetName (or AutomationId match), then optionally click element named $ClickName
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
$root = [System.Windows.Automation.AutomationElement]::RootElement
$cond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ProcessIdProperty, $ProcId)
$wins = $root.FindAll([System.Windows.Automation.TreeScope]::Children, $cond)
$win = $null
foreach ($w in $wins) { if ($w.Current.Name -like "*$WinName*") { $win = $w; break } }
if (-not $win) { Write-Output 'NO WINDOW'; exit 1 }
Write-Output ("WINDOW: " + $win.Current.Name)
if ($SetName) {
  $all = $win.FindAll([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.Condition]::TrueCondition)
  foreach ($a in $all) {
    if ($a.Current.Name -like "*$SetName*" -or $a.Current.AutomationId -eq $SetName) {
      $vp = $null
      if ($a.TryGetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern, [ref]$vp)) {
        $vp.SetValue($Value)
        Write-Output ("SET value on: " + $a.Current.Name + " -> " + $Value)
      }
    }
  }
}
