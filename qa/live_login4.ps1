Param()
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing
Add-Type -MemberDefinition '[DllImport("user32.dll")] public static extern bool SetProcessDPIAware();' -Name D -Namespace W
[W.D]::SetProcessDPIAware() | Out-Null
Add-Type -MemberDefinition '[DllImport("user32.dll")] public static extern bool mouse_event(uint f, uint dx, uint dy, uint data, int ex); [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h); [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h, int cmd); [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr h, IntPtr dc, uint f); [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r); public struct RECT { public int L,T,R,B; }' -Name U -Namespace W

$procId = [int](Get-Content 'qa\v3_pid.txt')
$root = [System.Windows.Automation.AutomationElement]::RootElement
$cond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ProcessIdProperty, $procId)
$login = $null
foreach ($w in $root.FindAll([System.Windows.Automation.TreeScope]::Children, $cond)) {
  if ($w.Current.Name -like 'Sign in*') { $login = $w }
}
$h = [IntPtr]$login.Current.NativeWindowHandle
[W.U]::ShowWindow($h, 9) | Out-Null
[W.U]::SetForegroundWindow($h) | Out-Null
Start-Sleep -Milliseconds 900

function Click { param([int]$x, [int]$y)
  [System.Windows.Forms.Cursor]::Position = New-Object System.Drawing.Point($x, $y)
  Start-Sleep -Milliseconds 350
  [W.U]::mouse_event(2,0,0,0,0); Start-Sleep -Milliseconds 100; [W.U]::mouse_event(4,0,0,0,0)
  Start-Sleep -Milliseconds 500
}
function Shot([string]$file) {
  $rect = New-Object 'W.U+RECT'
  [W.U]::GetWindowRect($h, [ref]$rect) | Out-Null
  $bmp = New-Object System.Drawing.Bitmap(($rect.R-$rect.L), ($rect.B-$rect.T))
  $g = [System.Drawing.Graphics]::FromImage($bmp); $hdc = $g.GetHdc()
  [W.U]::PrintWindow($h, $hdc, 2) | Out-Null
  $g.ReleaseHdc($hdc); $g.Dispose()
  # crop username/password region: window origin + offset
  $src = New-Object System.Drawing.Rectangle(700, 260, 700, 320)
  $dst = New-Object System.Drawing.Rectangle(0, 0, 700, 320)
  $crop = New-Object System.Drawing.Bitmap(700, 320)
  $cg = [System.Drawing.Graphics]::FromImage($crop)
  $cg.DrawImage($bmp, $dst, $src, [System.Drawing.GraphicsUnit]::Pixel)
  $cg.Dispose(); $bmp.Dispose()
  $crop.Save("qa\$file", [System.Drawing.Imaging.ImageFormat]::Png); $crop.Dispose()
  Write-Output "SHOT $file"
}

Click 2245 670
[System.Windows.Forms.SendKeys]::SendWait('^a')
Start-Sleep -Milliseconds 200
[System.Windows.Forms.SendKeys]::SendWait('admin')
Start-Sleep -Milliseconds 400
Shot 'live_user_field.png'
Click 2245 815
[System.Windows.Forms.SendKeys]::SendWait('^a')
Start-Sleep -Milliseconds 200
[System.Windows.Forms.SendKeys]::SendWait('Admin123!')
Start-Sleep -Milliseconds 400
Shot 'live_pw_field.png'
