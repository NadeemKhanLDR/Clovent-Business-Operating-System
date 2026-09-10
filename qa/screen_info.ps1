Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing
$screens = [System.Windows.Forms.Screen]::AllScreens
$i = 0
foreach ($s in $screens) {
    Write-Output "Screen ${i}: $($s.DeviceName) Bounds=$($s.Bounds.Width)x$($s.Bounds.Height) Primary=$($s.Primary)"
    $i++
}
Write-Output "VirtualScreen: $([System.Windows.Forms.SystemInformation]::VirtualScreen)"
