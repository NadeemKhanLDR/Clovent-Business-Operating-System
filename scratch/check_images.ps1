Add-Type -AssemblyName System.Drawing
$img1 = [System.Drawing.Image]::FromFile('C:\Users\NexGen\.gemini\antigravity\brain\3bb8278a-1131-4a90-a8c7-9af3215f8e3a\.user_uploaded\media_1789992554584.png')
Write-Host "Image 1: $($img1.Width) x $($img1.Height), HorizontalResolution: $($img1.HorizontalResolution), VerticalResolution: $($img1.VerticalResolution)"
$img1.Dispose()

$img2 = [System.Drawing.Image]::FromFile('C:\Users\NexGen\.gemini\antigravity\brain\3bb8278a-1131-4a90-a8c7-9af3215f8e3a\.user_uploaded\media_1789992556853.png')
Write-Host "Image 2: $($img2.Width) x $($img2.Height), HorizontalResolution: $($img2.HorizontalResolution), VerticalResolution: $($img2.VerticalResolution)"
$img2.Dispose()
