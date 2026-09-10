param([string]$src, [int]$x, [int]$y, [int]$w, [int]$h, [string]$out)
Add-Type -AssemblyName System.Drawing
$bmp = [System.Drawing.Bitmap]::FromFile($src)
$crop = New-Object System.Drawing.Bitmap($w, $h)
$g = [System.Drawing.Graphics]::FromImage($crop)
$g.DrawImage($bmp, (New-Object System.Drawing.Rectangle(0, 0, $w, $h)), (New-Object System.Drawing.Rectangle($x, $y, $w, $h)), [System.Drawing.GraphicsUnit]::Pixel)
$g.Dispose(); $bmp.Dispose()
$crop.Save($out, [System.Drawing.Imaging.ImageFormat]::Png)
$crop.Dispose()
Write-Output "CROP: $out"
