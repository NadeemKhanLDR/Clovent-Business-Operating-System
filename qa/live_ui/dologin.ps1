param([int]$ProcId, [string]$Out, [string]$Card = 'tlpCardPos')
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes, System.Windows.Forms, System.Drawing
Add-Type -MemberDefinition '
[DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
[DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
' -Name W -Namespace Q
Add-Type -MemberDefinition '[DllImport("user32.dll")] public static extern void mouse_event(uint f,uint dx,uint dy,uint data,int ex);' -Name M -Namespace Q
[Q.W]::SetProcessDPIAware() | Out-Null
$root = [System.Windows.Automation.AutomationElement]::RootElement
$cond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ProcessIdProperty, $ProcId)
$wins = $root.FindAll([System.Windows.Automation.TreeScope]::Children, $cond)
$win = $null
foreach ($w in $wins) { if ($w.Current.Name -like 'Sign in*') { $win = $w } }
if (-not $win) { Write-Output 'ERR|no login window'; exit 1 }
[Q.W]::SetForegroundWindow([IntPtr]$win.Current.NativeWindowHandle) | Out-Null
Start-Sleep -Milliseconds 800
$all = $win.FindAll([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.Condition]::TrueCondition)
function CenterOf([string]$paneId) {
  foreach ($e in $script:all) {
    if ($e.Current.AutomationId -eq $paneId) {
      $r = $e.Current.BoundingRectangle
      return @{X=[int]($r.X + $r.Width/2); Y=[int]($r.Y + $r.Height/2)}
    }
  }
  return $null
}
function EditorBelow([string]$labelName) {
  foreach ($e in $script:all) {
    if ($e.Current.Name -eq $labelName -and $e.Current.ControlType.ProgrammaticName -eq 'ControlType.Pane') {
      $r = $e.Current.BoundingRectangle
      return @{X=[int]($r.X + $r.Width/2); Y=[int]($r.Bottom + 35)}
    }
  }
  return $null
}
function Click([int]$x, [int]$y) {
  [System.Windows.Forms.Cursor]::Position = New-Object System.Drawing.Point($x, $y)
  Start-Sleep -Milliseconds 300
  [Q.M]::mouse_event(2,0,0,0,0); Start-Sleep -Milliseconds 90; [Q.M]::mouse_event(4,0,0,0,0)
  Start-Sleep -Milliseconds 400
}
# pane below label pane holds the editor: Username label id=264660, editor pane id=264662; Password label 264652, editor pane 264526
$uPane = EditorBelow 'Username'; $pPane = EditorBelow 'Password'
if (-not $uPane -or -not $pPane) { Write-Output "ERR|panes u=$($null -ne $uPane) p=$($null -ne $pPane)"; exit 1 }
Click $uPane.X $uPane.Y
[System.Windows.Forms.SendKeys]::SendWait('^a'); Start-Sleep -Milliseconds 150
[System.Windows.Forms.SendKeys]::SendWait('admin'); Start-Sleep -Milliseconds 300
Click $pPane.X $pPane.Y
[System.Windows.Forms.SendKeys]::SendWait('^a'); Start-Sleep -Milliseconds 150
[System.Windows.Forms.SendKeys]::SendWait('Admin123!'); Start-Sleep -Milliseconds 400
Write-Output 'TYPED|admin / Admin123!'

$r = $win.Current.BoundingRectangle
$bmp = New-Object System.Drawing.Bitmap([int]$r.Width, [int]$r.Height)
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.CopyFromScreen([int]$r.X, [int]$r.Y, 0, 0, $bmp.Size)
$bmp.Save($Out, [System.Drawing.Imaging.ImageFormat]::Png)
Write-Output "CAP|$Out"

$posCard = CenterOf $Card
if (-not $posCard) { Write-Output 'ERR|no pos card'; exit 1 }
Click $posCard.X $posCard.Y
Write-Output "CLICKED|POS card @ $($posCard.X),$($posCard.Y)"
Start-Sleep -Seconds 12
$proc = Get-Process -Id $ProcId
Write-Output ("TITLE|" + $proc.MainWindowTitle)
