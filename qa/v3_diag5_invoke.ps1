Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
Add-Type -MemberDefinition '[DllImport("user32.dll")] public static extern bool SetProcessDpiAwarenessContext(IntPtr v);' -Name D -Namespace W
[W.D]::SetProcessDpiAwarenessContext([IntPtr](-4)) | Out-Null
$procId = [int](Get-Content "d:\Clovent Business Operating System\qa\v3_pid.txt")
$procCond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ProcessIdProperty, $procId)
$root = [System.Windows.Automation.AutomationElement]::RootElement
$pos = $null
foreach ($w in $root.FindAll([System.Windows.Automation.TreeScope]::Children, $procCond)) {
    if ($w.Current.Name -like "*Restaurant POS*") { $pos = $w; break }
}
$target = $null
foreach ($el in $pos.FindAll([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.Condition]::TrueCondition)) {
    if ($el.Current.Name -eq '+ Dine In') { $target = $el; break }
}
if (-not $target) { Write-Output "NOT FOUND"; exit 1 }
Write-Output "target found; patterns:"
foreach ($p in $target.GetSupportedPatterns()) { Write-Output ("  " + $p.ProgrammaticName) }
$inv = $null
if ($target.TryGetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern, [ref]$inv)) {
    Write-Output "invoking..."
    $inv.Invoke()
    Start-Sleep -Seconds 3
    Write-Output "invoke done"
} else {
    Write-Output "no invoke pattern"
}
# state check
$pos2 = $null
foreach ($w in $root.FindAll([System.Windows.Automation.TreeScope]::Children, $procCond)) {
    if ($w.Current.Name -like "*Restaurant POS*") { $pos2 = $w; break }
}
foreach ($el in $pos2.FindAll([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.Condition]::TrueCondition)) {
    $n = $el.Current.Name
    if (($n -match '^ORD-\d+' -and $n.Length -lt 40) -or $n -eq 'Cancel Order') { Write-Output ("ORDER ACTIVE: '" + $n + "'") }
}
foreach ($w in $root.FindAll([System.Windows.Automation.TreeScope]::Children, $procCond)) {
    if ($w.Current.Name -notlike "*Restaurant POS*" -and $w.Current.Name) { Write-Output ("OTHER WIN: '" + $w.Current.Name + "'") }
}
