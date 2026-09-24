Add-Type -AssemblyName System.Drawing

$sizes = @(16, 24, 32, 48, 64, 128, 256)
$pngStreams = @()

foreach ($s in $sizes) {
    $bmp = New-Object System.Drawing.Bitmap $s, $s, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $g.Clear([System.Drawing.Color]::Transparent)

    # Outer rounded box
    $pad = [Math]::Max(1.0, $s * 0.05)
    $rect = New-Object System.Drawing.RectangleF $pad, $pad, ($s - 2 * $pad), ($s - 2 * $pad)
    $radius = $s * 0.22

    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $diameter = $radius * 2.0
    $path.AddArc($rect.X, $rect.Y, $diameter, $diameter, 180, 90)
    $path.AddArc($rect.Right - $diameter, $rect.Y, $diameter, $diameter, 270, 90)
    $path.AddArc($rect.Right - $diameter, $rect.Bottom - $diameter, $diameter, $diameter, 0, 90)
    $path.AddArc($rect.X, $rect.Bottom - $diameter, $diameter, $diameter, 90, 90)
    $path.CloseFigure()

    # Gradient fill for squircle (Slate-950 to Slate-900)
    $bgBrush = New-Object System.Drawing.Drawing2D.LinearGradientBrush (New-Object System.Drawing.PointF 0, 0), (New-Object System.Drawing.PointF 0, $s), ([System.Drawing.Color]::FromArgb(255, 15, 23, 42)), ([System.Drawing.Color]::FromArgb(255, 30, 41, 59))
    $g.FillPath($bgBrush, $path)

    # Outer border ring
    $borderPen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(120, 13, 148, 136)), ([Math]::Max(1.0, $s * 0.03))
    $g.DrawPath($borderPen, $path)

    # Stylized "C" monogram in vibrant Teal (#14B8A6 to #0D9488)
    $penWidth = [Math]::Max(2.0, $s * 0.16)
    $cPen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255, 20, 184, 166)), $penWidth
    $cPen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
    $cPen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round

    $inset = $s * 0.24
    $cRect = New-Object System.Drawing.RectangleF $inset, $inset, ($s - 2 * $inset), ($s - 2 * $inset)
    $g.DrawArc($cPen, $cRect, 45, 270)

    # Accent node inside C
    $nodeSize = [Math]::Max(2.0, $s * 0.14)
    $nodeX = $s * 0.52
    $nodeY = $s * 0.5 - ($nodeSize / 2)
    $nodeBrush = New-Object System.Drawing.Drawing2D.LinearGradientBrush (New-Object System.Drawing.PointF $nodeX, $nodeY), (New-Object System.Drawing.PointF ($nodeX + $nodeSize), ($nodeY + $nodeSize)), ([System.Drawing.Color]::FromArgb(255, 45, 212, 191)), ([System.Drawing.Color]::FromArgb(255, 13, 148, 136))
    $g.FillEllipse($nodeBrush, $nodeX, $nodeY, $nodeSize, $nodeSize)

    $ms = New-Object System.IO.MemoryStream
    $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    $pngStreams += @{ Size = $s; Bytes = $ms.ToArray() }

    $g.Dispose()
    $bmp.Dispose()
}

# Construct ICO binary
$fs = [System.IO.File]::Create("src\Clovent.Desktop\Resources\cbos.ico")
$bw = New-Object System.IO.BinaryWriter $fs

# ICONDIR header
$bw.Write([UInt16]0) # Reserved
$bw.Write([UInt16]1) # Type: 1 = ICO
$bw.Write([UInt16]$pngStreams.Count) # Count

# Entries
$offset = 6 + ($pngStreams.Count * 16)
foreach ($img in $pngStreams) {
    $w = if ($img.Size -ge 256) { 0 } else { [byte]$img.Size }
    $h = if ($img.Size -ge 256) { 0 } else { [byte]$img.Size }
    $bw.Write([byte]$w)
    $bw.Write([byte]$h)
    $bw.Write([byte]0) # ColorCount
    $bw.Write([byte]0) # Reserved
    $bw.Write([UInt16]1) # Planes
    $bw.Write([UInt16]32) # BitCount
    $bw.Write([UInt32]$img.Bytes.Length) # BytesInRes
    $bw.Write([UInt32]$offset) # ImageOffset
    $offset += $img.Bytes.Length
}

# Image Data (PNG streams)
foreach ($img in $pngStreams) {
    $bw.Write($img.Bytes)
}

$bw.Flush()
$fs.Close()
Write-Host "cbos.ico generated successfully, size: $((Get-Item 'src\Clovent.Desktop\Resources\cbos.ico').Length) bytes"
