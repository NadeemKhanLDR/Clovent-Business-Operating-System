Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes

$exe = "d:\Clovent Business Operating System\src\Clovent.Desktop\bin\Debug\net10.0-windows\Clovent.Desktop.exe"
$p = Start-Process $exe -ArgumentList "--pos" -PassThru

$root = [System.Windows.Automation.AutomationElement]::RootElement
$procCond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ProcessIdProperty, $p.Id)

$posWin = $null
for ($i = 0; $i -lt 40; $i++) {
    Start-Sleep -Seconds 1
    $wins = $root.FindAll([System.Windows.Automation.TreeScope]::Children, $procCond)
    foreach ($w in $wins) {
        if ($w.Current.Name -like "*POS*" -or $w.Current.Name -like "*Restaurant*") {
            $posWin = $w
            break
        }
    }
    if ($posWin) {
        Write-Output "Found after $i seconds: '$($posWin.Current.Name)'"
        break
    }
}

if ($posWin) {
    Start-Sleep -Seconds 2
    $btns = $posWin.FindAll([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.Condition]::TrueCondition)
    Write-Output "Total elements in POS: $($btns.Count)"
    foreach ($b in $btns) {
        if ($b.Current.Name -like "*Recall*" -or $b.Current.Name -like "*Hold*" -or $b.Current.Name -like "*Clear*") {
            Write-Output "  MATCH: Name='$($b.Current.Name)' Id='$($b.Current.AutomationId)' Type='$($b.Current.ControlType.ProgrammaticName)' Bounds='$($b.Current.BoundingRectangle)'"
        }
    }
}

Get-Process Clovent.Desktop -ErrorAction SilentlyContinue | Stop-Process -Force
