Add-Type -AssemblyName System.Drawing
Get-ChildItem -Path "$env:LOCALAPPDATA\Clovent\MenuItemImages\*.png" | ForEach-Object {
    try {
        $img = [System.Drawing.Image]::FromFile($_.FullName)
        $bmp = New-Object System.Drawing.Bitmap($img)
        $bmp.Dispose()
        $img.Dispose()
        Write-Host "OK: $($_.Name)"
    } catch {
        Write-Host "FAIL: $($_.Name) - $($_.Exception.Message)"
    }
}
