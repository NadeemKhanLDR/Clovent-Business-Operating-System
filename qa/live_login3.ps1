Param()
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
Add-Type -AssemblyName System.Windows.Forms
Add-Type -MemberDefinition '[DllImport("user32.dll")] public static extern bool SetProcessDPIAware();' -Name D -Namespace W
[W.D]::SetProcessDPIAware() | Out-Null

$procId = [int](Get-Content 'qa\v3_pid.txt')
$root = [System.Windows.Automation.AutomationElement]::RootElement
$cond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ProcessIdProperty, $procId)
$login = $null
foreach ($w in $root.FindAll([System.Windows.Automation.TreeScope]::Children, $cond)) {
  if ($w.Current.Name -like 'Sign in*') { $login = $w }
}
if (-not $login) { Write-Output 'NO LOGIN WINDOW'; exit 1 }

$editCond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ControlTypeProperty, [System.Windows.Automation.ControlType]::Edit)
$edits = $login.FindAll([System.Windows.Automation.TreeScope]::Descendants, $editCond)
Write-Output ("EDITS FOUND: {0}" -f $edits.Count)
$i = 0
foreach ($e in $edits) {
  $rc = $e.Current.BoundingRectangle
  Write-Output ("EDIT[{0}] value='{1}' pw={2} [{3}x{4}@{5},{6}]" -f $i, $e.Current.GetValue([System.Windows.Automation.ValuePattern]::Current).Value, $e.Current.IsPassword, [int]$rc.Width, [int]$rc.Height, [int]$rc.X, [int]$rc.Y)
  $i++
}
