param([int]$ProcId)
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
$root = [System.Windows.Automation.AutomationElement]::RootElement
$cond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ProcessIdProperty, $ProcId)
$wins = $root.FindAll([System.Windows.Automation.TreeScope]::Children, $cond)
$sb = New-Object System.Text.StringBuilder
function Dump($el, $depth) {
    foreach ($child in $el.FindAll([System.Windows.Automation.TreeScope]::Children, [System.Windows.Automation.Condition]::TrueCondition)) {
        $name = $child.Current.Name
        $ctype = $child.Current.ControlType.ProgrammaticName -replace 'ControlType.',''
        $rect = $child.Current.BoundingRectangle
        $sb.AppendLine(("{0}{1} '{2}' [{3:f0}x{4:f0}@{5:f0},{6:f0}]" -f ("  " * $depth), $ctype, $name, $rect.Width, $rect.Height, $rect.X, $rect.Y)) | Out-Null
        if ($depth -lt 12) { Dump $child ($depth + 1) }
    }
}
foreach ($w in $wins) {
    $sb.AppendLine("=== WINDOW: '$($w.Current.Name)' ===") | Out-Null
    Dump $w 1
}
$sb.ToString() | Out-File "d:\Clovent Business Operating System\qa\v3_uia_full.txt" -Encoding utf8
Write-Output "DUMPED to v3_uia_full.txt"
