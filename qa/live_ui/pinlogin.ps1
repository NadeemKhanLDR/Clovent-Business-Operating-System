param([int]$ProcId, [string]$Pin, [string]$Out, [string]$Card = 'pos')
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes, System.Windows.Forms, System.Drawing
Add-Type -MemberDefinition '[DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);' -Name W -Namespace Q
$root = [System.Windows.Automation.AutomationElement]::RootElement
$cond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ProcessIdProperty, $ProcId)
$wins = $root.FindAll([System.Windows.Automation.TreeScope]::Children, $cond)
$win = $null
foreach ($w in $wins) { if ($w.Current.Name -like 'Sign in*') { $win = $w } }
if (-not $win) { Write-Output 'ERR|no login window'; exit 1 }
[Q.W]::SetForegroundWindow([IntPtr]$win.Current.NativeWindowHandle) | Out-Null
Start-Sleep -Milliseconds 900

# find the PIN label pane; editor sits directly below it, same x center
$all = $win.FindAll([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.Condition]::TrueCondition)
$pinLabel = $null
foreach ($e in $all) {
  if ($e.Current.Name -eq 'PIN' -and $e.Current.ControlType.ProgrammaticName -eq 'ControlType.Pane') { $pinLabel = $e; break }
}
if (-not $pinLabel) { Write-Output 'ERR|no PIN label'; exit 1 }
$r = $pinLabel.Current.BoundingRectangle
$px = [int]($r.X + $r.Width/2); $py = [int]($r.Bottom + 41)

Add-Type -MemberDefinition '[DllImport("user32.dll")] public static extern bool SetCursorPos(int x,int y); [DllImport("user32.dll")] public static extern void mouse_event(uint f,uint dx,uint dy,uint data,int ex);' -Name M -Namespace Q
[Q.M]::SetCursorPos($px, $py) | Out-Null
Start-Sleep -Milliseconds 350
[Q.M]::mouse_event(2,0,0,0,0); Start-Sleep -Milliseconds 100; [Q.M]::mouse_event(4,0,0,0,0)
Start-Sleep -Milliseconds 500
foreach ($ch in $Pin.ToCharArray()) {
  [System.Windows.Forms.SendKeys]::SendWait([string]$ch)
  Start-Sleep -Milliseconds 50
}
Write-Output "TYPED PIN into ($px,$py)"

$wr = $win.Current.BoundingRectangle
$bmp = New-Object System.Drawing.Bitmap([int]$wr.Width, [int]$wr.Height)
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.CopyFromScreen([int]$wr.X, [int]$wr.Y, 0, 0, $bmp.Size)
$bmp.Save($Out, [System.Drawing.Imaging.ImageFormat]::Png)
Write-Output "CAP|$Out"

if ($Card -eq 'pos') { [Q.M]::SetCursorPos(1945,1668) } else { [Q.M]::SetCursorPos(2545,1668) }
Start-Sleep -Milliseconds 350
[Q.M]::mouse_event(2,0,0,0,0); Start-Sleep -Milliseconds 100; [Q.M]::mouse_event(4,0,0,0,0)
Write-Output "CLICKED|$Card"
Start-Sleep -Seconds 14
$proc = Get-Process -Id $ProcId
Write-Output ("TITLE|" + $proc.MainWindowTitle)
