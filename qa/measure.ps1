Add-Type -AssemblyName System.Drawing
$bmp = [System.Drawing.Bitmap]::FromFile("D:\Clovent Business Operating System\qa\acceptance_test_103\test_reports_tab.png")
for ($y = 200; $y -lt 400; $y += 20) {
    for ($x = 60; $x -lt 120; $x += 20) {
        $c = $bmp.GetPixel($x, $y)
        Write-Host "($x, $y) : R=$($c.R) G=$($c.G) B=$($c.B)"
    }
}
$bmp.Dispose()
