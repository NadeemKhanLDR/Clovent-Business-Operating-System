param([int]$ProcId, [string]$Out)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes, System.Windows.Forms, System.Drawing
Add-Type -AssemblyName System.Runtime.WindowsRuntime
Add-Type -MemberDefinition '[DllImport("user32.dll")] public static extern bool SetProcessDPIAware(); [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h); [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h,int c); [DllImport("user32.dll")] public static extern bool SetCursorPos(int x,int y); [DllImport("user32.dll")] public static extern void mouse_event(uint f,uint dx,uint dy,uint data,int ex);' -Name W -Namespace Q
[Q.W]::SetProcessDPIAware() | Out-Null
$p = Get-Process -Id $ProcId
$h = $p.MainWindowHandle
[Q.W]::ShowWindow($h, 3) | Out-Null
[Q.W]::SetForegroundWindow($h) | Out-Null
Start-Sleep -Milliseconds 1500
function ClickAt([int]$x, [int]$y) {
  [Q.W]::SetCursorPos($x, $y) | Out-Null
  Start-Sleep -Milliseconds 300
  [Q.W]::mouse_event(2,0,0,0,0); Start-Sleep -Milliseconds 90; [Q.W]::mouse_event(4,0,0,0,0)
  Start-Sleep -Milliseconds 600
}
function Cap([string]$path) {
  $r = (Get-Process -Id $ProcId).MainWindowRectangle() 2>$null
}
function Shot([string]$path) {
  Add-Type -AssemblyName System.Drawing
  $rw = [System.Windows.Automation.AutomationElement]::FromHandle($h).Current.BoundingRectangle
  $bmp = New-Object System.Drawing.Bitmap([int]$rw.Width, [int]$rw.Height)
  $g = [System.Drawing.Graphics]::FromImage($bmp)
  $g.CopyFromScreen([int]$rw.X, [int]$rw.Y, 0, 0, $bmp.Size)
  $bmp.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
}

# OCR via WinRT
$null = [Windows.Media.Ocr.OcrEngine, Windows.Foundation.Metadata, ContentType = WindowsRuntime]
$null = [Windows.Graphics.Imaging.BitmapDecoder, Windows.Foundation.Metadata, ContentType = WindowsRuntime]
$null = [Windows.Storage.StorageFile, Windows.Foundation.Metadata, ContentType = WindowsRuntime]
$asTaskGeneric = ([System.WindowsRuntimeSystemExtensions].GetMethods() | Where-Object { $_.Name -eq 'AsTask' -and $_.GetParameters().Count -eq 1 -and $_.GetParameters()[0].ParameterType.Name -eq 'IAsyncOperation`1' })[0]
function Await($WinRtTask, $ResultType) {
  $asTask = $asTaskGeneric.MakeGenericMethod($ResultType)
  $netTask = $asTask.Invoke($null, @($WinRtTask))
  $netTask.Wait(-1) | Out-Null
  $netTask.Result
}
function OcrWords([string]$path, [int]$top, [int]$left, [int]$w, [int]$hh) {
  $bmp = [System.Drawing.Image]::FromFile($path)
  if ($w -gt 0) {
    $crop = New-Object System.Drawing.Bitmap($w, $hh)
    $g = [System.Drawing.Graphics]::FromImage($crop)
    $g.DrawImage($bmp, (New-Object System.Drawing.Rectangle(0,0,$w,$hh)), (New-Object System.Drawing.Rectangle($left,$top,$w,$hh)), [System.Drawing.GraphicsUnit]::Pixel)
    $g.Dispose(); $bmp.Dispose(); $bmp = $crop
  }
  $tmp = [System.IO.Path]::GetTempFileName() + '.png'
  $bmp.Save($tmp, [System.Drawing.Imaging.ImageFormat]::Png); $bmp.Dispose()
  $sf = Await ([Windows.Storage.StorageFile]::GetFileFromPathAsync($tmp)) ([Windows.Storage.StorageFile])
  $st = Await ($sf.OpenAsync([Windows.Storage.FileAccessMode]::Read)) ([Windows.Storage.Streams.IRandomAccessStream])
  $dec = Await ([Windows.Graphics.Imaging.BitmapDecoder]::CreateAsync($st)) ([Windows.Graphics.Imaging.BitmapDecoder])
  $sb = Await ($dec.GetSoftwareBitmapAsync()) ([Windows.Graphics.Imaging.SoftwareBitmap])
  $eng = [Windows.Media.Ocr.OcrEngine]::TryCreateFromUserProfileLanguages()
  $res = Await ($eng.RecognizeAsync($sb)) ([Windows.Media.Ocr.OcrResult])
  Remove-Item $tmp -ErrorAction SilentlyContinue
  $res.Lines | ForEach-Object { $_.Words } | ForEach-Object {
    $r = $_.BoundingRect
    [pscustomobject]@{ Text=$_.Text; X=[int]($r.X+$left); Y=[int]($r.Y+$top); W=[int]$r.Width; H=[int]$r.Height }
  }
}

$shot1 = "$env:TEMP\flow_shot.png"
Shot $shot1
$tabs = OcrWords $shot1 140 0 3872 120
$adminTab = $tabs | Where-Object { $_.Text -like 'Admin*' } | Select-Object -First 1
if (-not $adminTab) { Write-Output 'ERR|no Administration tab'; exit 1 }
ClickAt ([int]($adminTab.X + $adminTab.W/2)) ([int]($adminTab.Y + $adminTab.H/2))
Start-Sleep -Milliseconds 1500
Shot $shot1
$panel = OcrWords $shot1 200 0 700 1300
$usersBtn = $panel | Where-Object { $_.Text -eq 'Users' } | Select-Object -First 1
if (-not $usersBtn) { Write-Output 'ERR|no Users button'; exit 1 }
ClickAt ([int]($usersBtn.X + $usersBtn.W/2)) ([int]($usersBtn.Y + $usersBtn.H/2))
Start-Sleep -Milliseconds 2500

# select cashier1 row via UIA cell text
$winEl = [System.Windows.Automation.AutomationElement]::FromHandle($h)
$all = $winEl.FindAll([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.Condition]::TrueCondition)
$cell = $null
foreach ($e in $all) { if ($e.Current.Name -eq 'cashier1') { $cell = $e; break } }
if ($cell) {
  $cr = $cell.Current.BoundingRectangle
  ClickAt ([int]($cr.X + $cr.Width/2)) ([int]($cr.Y + $cr.Height/2))
  Write-Output ('ROW|cashier1 selected')
} else { Write-Output 'WARN|cashier1 cell not found'; }

Start-Sleep -Milliseconds 800
Shot $shot1
$actions = OcrWords $shot1 700 100 700 900
$clearPin = $actions | Where-Object { $_.Text -eq 'Clear' } | Select-Object -First 1
if (-not $clearPin) { Write-Output 'ERR|no Clear PIN button'; exit 1 }
# Clear PIN is the word after 'Clear'
$pinWord = $actions | Where-Object { $_.Text -eq 'PIN' -and [Math]::Abs($_.Y - $clearPin.Y) -lt 20 } | Select-Object -First 1
$cx = if ($pinWord) { [int](($clearPin.X + $pinWord.X + $pinWord.W)/2) } else { [int]($clearPin.X + $clearPin.W/2) }
ClickAt $cx ([int]($clearPin.Y + 10))
Start-Sleep -Milliseconds 2500
Shot $Out
Write-Output "CAP|$Out"
Write-Output 'DONE|clear pin clicked'
