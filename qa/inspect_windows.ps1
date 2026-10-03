Add-Type -AssemblyName UIAutomationClient
$root = [System.Windows.Automation.AutomationElement]::RootElement
$allWins = $root.FindAll([System.Windows.Automation.TreeScope]::Children, [System.Windows.Automation.Condition]::TrueCondition)
foreach ($w in $allWins) {
    try {
        $p = Get-Process -Id $w.Current.ProcessId -ErrorAction SilentlyContinue
        Write-Host "PID=$($w.Current.ProcessId) Process=$($p.ProcessName) Title='$($w.Current.Name)' Class='$($w.Current.ClassName)' HWND=$($w.Current.NativeWindowHandle)"
    } catch {
        Write-Host "Error inspecting window: $_"
    }
}
