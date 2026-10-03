Add-Type -AssemblyName UIAutomationClient
$cap = "Tools\ScreenCaptureUtil\bin\Release\net10.0-windows\ScreenCaptureUtil.exe"
$root = [System.Windows.Automation.AutomationElement]::RootElement
$all = $root.FindAll([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.Condition]::TrueCondition)
foreach ($e in $all) {
    if ($e.Current.Name -like "*450*" -or $e.Current.Name -like "*Biryani*") {
        Write-Host "NAME='$($e.Current.Name)' TYPE=$($e.Current.ControlType.ProgrammaticName) HWND=$($e.Current.NativeWindowHandle)"
    }
}
