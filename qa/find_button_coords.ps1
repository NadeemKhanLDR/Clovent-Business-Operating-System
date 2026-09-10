Add-Type -AssemblyName System.Drawing
$bmp = [System.Drawing.Bitmap]::FromFile("qa\qa_01_startup_pos.png")
Write-Output "Image Size: $($bmp.Width) x $($bmp.Height)"

# The button is purple (Indigo 79, 70, 229 -> ~ Color.FromArgb(79, 70, 229))
# Let's find pixels matching that color!
$minX = 99999; $maxX = 0; $minY = 99999; $maxY = 0; $count = 0
for ($y = [int]($bmp.Height * 0.8); $y -lt $bmp.Height; $y += 4) {
    for ($x = [int]($bmp.Width * 0.7); $x -lt $bmp.Width; $x += 4) {
        $c = $bmp.GetPixel($x, $y)
        if ($c.R -gt 70 -and $c.R -lt 90 -and $c.G -gt 60 -and $c.G -lt 80 -and $c.B -gt 215 -and $c.B -lt 240) {
            if ($x -lt $minX) { $minX = $x }
            if ($x -gt $maxX) { $maxX = $x }
            if ($y -lt $minY) { $minY = $y }
            if ($y -gt $maxY) { $maxY = $y }
            $count++
        }
    }
}
$bmp.Dispose()
Write-Output "Recall button pixel bounds in image: Left=$minX, Top=$minY, Right=$maxX, Bottom=$maxY (matching pixels: $count)"
Write-Output "Center: $([int](($minX+$maxX)/2)), $([int](($minY+$maxY)/2))"
