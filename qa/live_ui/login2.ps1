param([int]$ProcId, [string]$Card, [string]$Out)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes, System.Windows.Forms, System.Drawing
Add-Type -MemberDefinition '[DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h); [DllImport("user32.dll")] public static extern void mouse_event(uint f,uint dx,uint dy,uint data,int ex); [DllImport("user32.dll")] public static extern bool SetCursorPos(int x,int y);' -Name W -Namespace Q
$root = [System.Windows.Automation.AutomationElement]::RootElement
$cond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ProcessIdProperty, $ProcId)
$wins = $root.FindAll([System.Windows.Automation.TreeScope]::Children, $cond)
$win = $null
foreach ($w in $wins) { if ($w.Current.Name -like 'Sign in*') { $win = $w } }
if (-not $win) { Write-Output 'ERR|no login window'; exit 1 }
[Q.W]::SetForegroundWindow([IntPtr]$win.Current.NativeWindowHandle) | Out-Null
Start-Sleep -Milliseconds 900

function Click([int]$x, [int]$y) {
  [Q.W]::SetCursorPos($x, $y) | Out-Null
  Start-Sleep -Milliseconds 350
  [Q.W]::mouse_event(2,0,0,0,0); Start-Sleep -Milliseconds 100; [Q.W]::mouse_event(4,0,0,0,0)
  Start-Sleep -Milliseconds 500
}
function TypeText([string]$t) {
  [System.Windows.Forms.SendKeys]::SendWait('^a')
  Start-Sleep -Milliseconds 150
  foreach ($ch in $t.ToCharArray()) {
    [System.Windows.Forms.SendKeys]::SendWait([string]$ch)
    Start-Sleep -Milliseconds 40
  }
  Start-Sleep -Milliseconds 250
}

Click 2245 760
TypeText 'admin'
Click 2245 905
TypeText 'Admin123!'
Write-Output 'TYPED'

$r = $win.Current.BoundingRectangle
$bmp = New-Object System.Drawing.Bitmap([int]$r.Width, [int]$r.Height)
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.CopyFromScreen([int]$r.X, [int]$r.Y, 0, 0, $bmp.Size)
$bmp.Save($Out, [System.Drawing.Imaging.ImageFormat]::Png)
Write-Output "CAP|$Out"

if ($Card -eq 'pos') { Click 1945 1668 } else { Click 2545 1668 }
Write-Output "CLICKED|$Card"
Start-Sleep -Seconds 14
$proc = Get-Process -Id $ProcId
Write-Output ("TITLE|" + $proc.MainWindowTitle)
