Param()
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing
Add-Type -MemberDefinition '[DllImport("user32.dll")] public static extern bool SetProcessDPIAware();' -Name D -Namespace W
[W.D]::SetProcessDPIAware() | Out-Null
Add-Type -MemberDefinition '[DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr h, IntPtr dc, uint f); [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r); public struct RECT { public int L,T,R,B; }' -Name P -Namespace W

$procId = [int](Get-Content 'qa\v3_pid.txt')
$root = [System.Windows.Automation.AutomationElement]::RootElement
$cond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ProcessIdProperty, $procId)
$login = $null
foreach ($w in $root.FindAll([System.Windows.Automation.TreeScope]::Children, $cond)) {
  if ($w.Current.Name -like 'Sign in*') { $login = $w }
}
if (-not $login) { Write-Output 'NO LOGIN WINDOW'; exit 1 }
$h = [IntPtr]$login.Current.NativeWindowHandle

$rect = New-Object 'W.P+RECT'
[W.P]::GetWindowRect($h, [ref]$rect) | Out-Null
$bmp = New-Object System.Drawing.Bitmap(($rect.R-$rect.L), ($rect.B-$rect.T))
$g = [System.Drawing.Graphics]::FromImage($bmp); $hdc = $g.GetHdc()
[W.P]::PrintWindow($h, $hdc, 2) | Out-Null
$g.ReleaseHdc($hdc); $g.Dispose()
$bmp.Save('qa\QA_01_Startup.png', [System.Drawing.Imaging.ImageFormat]::Png); $bmp.Dispose()
Write-Output ("CAPTURED QA_01_Startup.png ({0}x{1})" -f ($rect.R-$rect.L), ($rect.B-$rect.T))

$eds = $login.FindAll([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.Condition]::TrueCondition)
$idx = 0
foreach ($el in $eds) {
  $n = $el.Current.Name
  $rc = $el.Current.BoundingRectangle
  if ($n -ne '' -and $n.Length -lt 80) { Write-Output ("EL[{0}] '{1}' [{2}x{3}@{4},{5}] type={6}" -f $idx, $n, [int]$rc.Width, [int]$rc.Height, [int]$rc.X, [int]$rc.Y, $el.Current.ControlType.ProgrammaticName) }
  $idx++
}
