param([int]$ProcId, [string]$Name)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes
Add-Type -MemberDefinition '[DllImport("user32.dll")] public static extern bool SetProcessDPIAware();' -Name W -Namespace Q
[Q.W]::SetProcessDPIAware() | Out-Null
$root = [System.Windows.Automation.AutomationElement]::RootElement
$cond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ProcessIdProperty, $ProcId)
$wins = $root.FindAll([System.Windows.Automation.TreeScope]::Children, $cond)
foreach ($w in $wins) {
  $all = $w.FindAll([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.Condition]::TrueCondition)
  $i = 0
  foreach ($e in $all) {
    $n = $e.Current.Name
    if ($n -and $n.Trim() -like "*$Name*") {
      $r = $e.Current.BoundingRectangle
      $loc = if ($r.IsEmpty) { 'EMPTY' } else { "[$([int]($r.X+$r.Width/2)),$([int]($r.Y+$r.Height/2))] $([int]$r.Width)x$([int]$r.Height)" }
      $safe = $n -replace '\p{Cs}','?' -replace "`r|`n",'\n'
      Write-Output "$i|$safe|$loc"
    }
    $i++
  }
}
