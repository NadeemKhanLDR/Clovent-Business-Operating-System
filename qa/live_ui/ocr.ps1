param([string]$Path, [int]$Top = 0, [int]$Left = 0, [int]$W = 0, [int]$H = 0)
Add-Type -AssemblyName System.Runtime.WindowsRuntime
Add-Type -AssemblyName System.Drawing
$null = [Windows.Media.Ocr.OcrEngine, Windows.Foundation.Metadata, ContentType = WindowsRuntime]
$null = [Windows.Graphics.Imaging.BitmapDecoder, Windows.Foundation.Metadata, ContentType = WindowsRuntime]
$null = [Windows.Storage.StorageFile, Windows.Foundation.Metadata, ContentType = WindowsRuntime]

# load, optionally crop, save crop as temp png
$bmp = [System.Drawing.Image]::FromFile($Path)
if ($W -gt 0) {
  $crop = New-Object System.Drawing.Bitmap($W, $H)
  $g = [System.Drawing.Graphics]::FromImage($crop)
  $g.DrawImage($bmp, (New-Object System.Drawing.Rectangle(0,0,$W,$H)), (New-Object System.Drawing.Rectangle($Left,$Top,$W,$H)), [System.Drawing.GraphicsUnit]::Pixel)
  $g.Dispose(); $bmp.Dispose(); $bmp = $crop
}
$tmp = [System.IO.Path]::GetTempFileName() + '.png'
$bmp.Save($tmp, [System.Drawing.Imaging.ImageFormat]::Png)
$bmp.Dispose()

# WinRT async helpers
$asTaskGeneric = ([System.WindowsRuntimeSystemExtensions].GetMethods() | Where-Object { $_.Name -eq 'AsTask' -and $_.GetParameters().Count -eq 1 -and $_.GetParameters()[0].ParameterType.Name -eq 'IAsyncOperation`1' })[0]
function Await($WinRtTask, $ResultType) {
  $asTask = $asTaskGeneric.MakeGenericMethod($ResultType)
  $netTask = $asTask.Invoke($null, @($WinRtTask))
  $netTask.Wait(-1) | Out-Null
  $netTask.Result
}
$storageFile = Await ([Windows.Storage.StorageFile]::GetFileFromPathAsync($tmp)) ([Windows.Storage.StorageFile])
$stream = Await ($storageFile.OpenAsync([Windows.Storage.FileAccessMode]::Read)) ([Windows.Storage.Streams.IRandomAccessStream])
$decoder = Await ([Windows.Graphics.Imaging.BitmapDecoder]::CreateAsync($stream)) ([Windows.Graphics.Imaging.BitmapDecoder])
$softBmp = Await ($decoder.GetSoftwareBitmapAsync()) ([Windows.Graphics.Imaging.SoftwareBitmap])
$engine = [Windows.Media.Ocr.OcrEngine]::TryCreateFromUserProfileLanguages()
$ocrResult = Await ($engine.RecognizeAsync($softBmp)) ([Windows.Media.Ocr.OcrResult])

$scaleX = 1.0
foreach ($line in $ocrResult.Lines) {
  foreach ($word in $line.Words) {
    $r = $word.BoundingRect
    Write-Output ("WORD '{0}' [{1},{2} {3}x{4}]" -f $word.Text, [int]($r.X + $Left), [int]($r.Y + $Top), [int]$r.Width, [int]$r.Height)
  }
}
Remove-Item $tmp -ErrorAction SilentlyContinue
