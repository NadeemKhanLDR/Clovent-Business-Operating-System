param([int]$ProcId)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes, System.Windows.Forms
Add-Type -MemberDefinition '[DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h); [DllImport("user32.dll")] public static extern void mouse_event(uint f,uint dx,uint dy,uint data,int ex);' -Name W -Namespace Q
$root = [System.Windows.Automation.AutomationElement]::RootElement
$cond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ProcessIdProperty, $ProcId)
$wins = $root.FindAll([System.Windows.Automation.TreeScope]::Children, $cond)
$win = $null
foreach ($w in $wins) { if ($w.Current.Name -like 'Sign in*') { $win = $w } }
if (-not $win) { Write-Output 'ERR|no login window'; exit 1 }
[Q.W]::SetForegroundWindow([IntPtr]$win.Current.NativeWindowHandle) | Out-Null
Start-Sleep -Milliseconds 800
$all = $win.FindAll([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.Condition]::TrueCondition)

# print the pane layout under tlpForm with rects so we can see label vs editor rows
foreach ($e in $all) {
  $n = $e.Current.Name
  $r = $e.Current.BoundingRectangle
  if ($r.IsEmpty -or $r.Width -lt 5) { continue }
  if (-not $n) { continue }
  if ($n -in @('Username','Password','PIN','Welcome back')) {
    Write-Output ("LABEL '$n' x=$([int]$r.X) y=$([int]$r.Y) w=$([int]$r.Width) h=$([int]$r.Height) bottom=$([int]$r.Bottom)")
  }
}
# also the unnamed editor-host panes between labels: width > 800 and height < 120
foreach ($e in $all) {
  $n = $e.Current.Name
  if ($n) { continue }
  $r = $e.Current.BoundingRectangle
  if ($r.IsEmpty) { continue }
  if ($r.Width -gt 400 -and $r.Height -gt 20 -and $r.Height -lt 90 -and $r.X -gt 1200) {
    Write-Output ("EDITORPANE x=$([int]$r.X) y=$([int]$r.Y) w=$([int]$r.Width) h=$([int]$r.Height) cx=$([int]($r.X+$r.Width/2)) cy=$([int]($r.Y+$r.Height/2))")
  }
}
