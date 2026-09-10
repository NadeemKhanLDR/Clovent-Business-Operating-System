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
$names = @('+ Dine In','+ Take Away','Hold','Recall','Clear','Record Payment','Split Payment','Print Bill','Place Order','History','More ▼','Logout','Cancel Order','PAYMENT METHOD','1','7','Exact','‹ Previous','Next ›')
foreach ($el in $pos.FindAll([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.Condition]::TrueCondition)) {
    $n = $el.Current.Name
    if ($names -contains $n) {
        Write-Output ("'{0}' IsEnabled={1}" -f $n, $el.Current.IsEnabled)
    }
}
