param([int]$ProcId, [string]$Filter = '')
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes
$root = [System.Windows.Automation.AutomationElement]::RootElement
$cond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ProcessIdProperty, $ProcId)
$wins = $root.FindAll([System.Windows.Automation.TreeScope]::Children, $cond)
foreach ($w in $wins) {
  $all = $w.FindAll([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.Condition]::TrueCondition)
  $i = 0
  foreach ($e in $all) {
    $n = $e.Current.Name
    if (-not $n) { continue }
    if ($Filter -and ($n -notlike "*$Filter*")) { continue }
    $ct = $e.Current.ControlType.ProgrammaticName -replace 'ControlType\.',''
    if ($Filter -or $ct -in @('Button','MenuItem','TabItem','ListItem','DataItem')) {
      $safe = $n -replace '\p{Cs}','?' -replace "`r|`n",'\n'
      Write-Output "$i|$ct|$safe"
    }
    $i++
  }
}
