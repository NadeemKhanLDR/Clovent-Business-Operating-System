Param([string]$In, [string]$Out, [double]$L=0, [double]$T=0, [double]$R=1, [double]$B=1)
Add-Type -AssemblyName System.Drawing
$s = [System.Drawing.Image]::FromFile((Resolve-Path $In))
$x0 = [int]($s.Width * $L); $y0 = [int]($s.Height * $T)
$w = [int]($s.Width * ($R - $L)); $h = [int]($s.Height * ($B - $T))
$bmp = New-Object System.Drawing.Bitmap($w, $h)
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.DrawImage($s, (New-Object System.Drawing.Rectangle(0,0,$w,$h)), (New-Object System.Drawing.Rectangle($x0,$y0,$w,$h)), [System.Drawing.GraphicsUnit]::Pixel)
$g.Dispose(); $bmp.Save((Join-Path (Get-Location) $Out)); $s.Dispose()
Write-Output "CROP $Out ${w}x${h} from $x0,$y0"
