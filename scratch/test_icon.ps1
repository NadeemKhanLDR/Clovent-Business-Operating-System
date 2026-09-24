Add-Type -AssemblyName System.Drawing
$icoPath = (Resolve-Path "src\Clovent.Desktop\Resources\cbos.ico").Path
try {
    $ico = [System.Drawing.Icon]::new($icoPath)
    Write-Host "SUCCESS: Icon loaded, size is $($ico.Width)x$($ico.Height)"
    $ico.Dispose()
} catch {
    Write-Host "ERROR: $_"
}
