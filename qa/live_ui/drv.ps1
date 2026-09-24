param(
  [string]$Action,
  [string]$Name,
  [string]$Value,
  [string]$Out,
  [int]$ProcId,
  [string]$Name2 = '',
  [int]$Index = 0,
  [switch]$Contains
)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes, System.Drawing
Add-Type -MemberDefinition '
[DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
[DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
' -Name W -Namespace Q
[Q.W]::SetProcessDPIAware() | Out-Null

function Get-Wins([int]$pid2) {
  $root = [System.Windows.Automation.AutomationElement]::RootElement
  $cond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ProcessIdProperty, $pid2)
  $root.FindAll([System.Windows.Automation.TreeScope]::Children, $cond)
}
function Find-El($win, [string]$n, [switch]$contains, [int]$idx = 0) {
  $all = $win.FindAll([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.Condition]::TrueCondition)
  $hits = @()
  foreach ($e in $all) {
    $cn = $e.Current.Name
    if (-not $cn) { continue }
    if ($contains) { if ($cn -like "*$n*") { $hits += $e } }
    elseif ($cn.Trim() -eq $n.Trim()) { $hits += $e }
  }
  if ($hits.Count -eq 0) { return $null }
  return $hits[[Math]::Min($idx, $hits.Count - 1)]
}
function Invoke-El($e) {
  $o = $null
  if ($e.TryGetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern, [ref]$o)) { $o.Invoke(); return 'invoke' }
  if ($e.TryGetCurrentPattern([System.Windows.Automation.TogglePattern]::Pattern, [ref]$o)) { $o.Toggle(); return 'toggle' }
  if ($e.TryGetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern, [ref]$o)) { $o.Select(); return 'select' }
  if ($e.TryGetCurrentPattern([System.Windows.Automation.ExpandCollapsePattern]::Pattern, [ref]$o)) { $o.Expand(); return 'expand' }
  $l = $null
  if ($e.TryGetCurrentPattern([System.Windows.Automation.LegacyIAccessiblePattern]::Pattern, [ref]$l)) { try { $l.DoDefaultAction(); return 'legacy' } catch {} }
  return 'none'
}
function Get-Texts($win) {
  $all = $win.FindAll([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.Condition]::TrueCondition)
  foreach ($e in $all) {
    $n = $e.Current.Name
    if ($n) { Write-Output ("TXT|" + $n.Replace("`r",' ').Replace("`n",'\n')) }
  }
}

$wins = Get-Wins $ProcId
if ($wins.Count -eq 0) { Write-Output 'ERR|no windows'; exit 1 }
$win = $wins[0]
foreach ($w in $wins) { if ($w.Current.Name -eq 'Restaurant POS' -or $w.Current.Name -like 'Sign in*') { $win = $w } }

switch ($Action) {
  'click' {
    [Q.W]::SetForegroundWindow([IntPtr]$win.Current.NativeWindowHandle) | Out-Null
    Start-Sleep -Milliseconds 200
    $e = Find-El $win $Name -contains:$Contains -idx $Index
    if (-not $e) { Write-Output "ERR|not found: $Name"; exit 1 }
    $r = Invoke-El $e
    Write-Output "CLICKED|$Name|via $r"
  }
  'click2' {
    [Q.W]::SetForegroundWindow([IntPtr]$win.Current.NativeWindowHandle) | Out-Null
    Start-Sleep -Milliseconds 200
    $e = Find-El $win $Name -contains:$Contains -idx $Index
    if (-not $e) { Write-Output "ERR|not found: $Name"; exit 1 }
    $r = Invoke-El $e
    Start-Sleep -Milliseconds 400
    $e2 = Find-El $win $Name2 -contains:$true
    if (-not $e2) { Write-Output "ERR|second not found: $Name2"; exit 1 }
    $r2 = Invoke-El $e2
    Write-Output "CLICKED2|$Name|$Name2|via $r,$r2"
  }
  'set' {
    $e = Find-El $win $Name -contains:$Contains
    if (-not $e) { Write-Output "ERR|not found: $Name"; exit 1 }
    $vp = $null
    if ($e.TryGetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern, [ref]$vp)) { $vp.SetValue($Value); Write-Output "SET|$Name|$Value" }
    else { Write-Output 'ERR|no value pattern'; exit 1 }
  }
  'physclick' {
    Add-Type -AssemblyName System.Windows.Forms
    Add-Type -MemberDefinition '[DllImport("user32.dll")] public static extern void mouse_event(uint f,uint dx,uint dy,uint data,int ex);' -Name M -Namespace Q
    [Q.W]::SetForegroundWindow([IntPtr]$win.Current.NativeWindowHandle) | Out-Null
    Start-Sleep -Milliseconds 300
    $e = Find-El $win $Name -contains:$Contains -idx $Index
    if (-not $e) { Write-Output "ERR|not found: $Name"; exit 1 }
    $r = $e.Current.BoundingRectangle
    if ($r.IsEmpty -or $r.Width -eq 0) { Write-Output "ERR|empty rect: $Name"; exit 1 }
    $x = [int]($r.X + $r.Width/2); $y = [int]($r.Y + $r.Height/2)
    [System.Windows.Forms.Cursor]::Position = New-Object System.Drawing.Point($x, $y)
    Start-Sleep -Milliseconds 300
    [Q.M]::mouse_event(2,0,0,0,0); Start-Sleep -Milliseconds 90; [Q.M]::mouse_event(4,0,0,0,0)
    Start-Sleep -Milliseconds 500
    Write-Output "PHYSCLICKED|$Name @ $x,$y"
  }
  'texts' { Get-Texts $win }
  'cap' {
    $h = [IntPtr]$win.Current.NativeWindowHandle
    [Q.W]::SetForegroundWindow($h) | Out-Null
    Start-Sleep -Milliseconds 600
    $r = $win.Current.BoundingRectangle
    $bmp = New-Object System.Drawing.Bitmap([int]$r.Width, [int]$r.Height)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.CopyFromScreen([int]$r.X, [int]$r.Y, 0, 0, $bmp.Size)
    $bmp.Save($Out, [System.Drawing.Imaging.ImageFormat]::Png)
    Write-Output "CAP|$Out|$([int]$r.Width)x$([int]$r.Height)"
  }
  default { Write-Output "ERR|unknown action $Action" }
}
