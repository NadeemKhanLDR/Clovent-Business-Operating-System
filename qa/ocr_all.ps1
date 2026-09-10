Param([string]$Folder)
Add-Type -AssemblyName System.Runtime.WindowsRuntime
$null = [Windows.Media.Ocr.OcrEngine, Windows.Media.Ocr, ContentType = WindowsRuntime]
$null = [Windows.Graphics.Imaging.BitmapDecoder, Windows.Graphics.Imaging, ContentType = WindowsRuntime]

function Await($WinRtTask, $ResultType) {
    $asTaskGeneric = ([System.WindowsRuntimeSystemExtensions].GetMethods() | Where-Object { $_.Name -eq 'AsTask' -and $_.GetParameters().Count -eq 1 -and $_.GetParameters()[0].ParameterType.Name -eq 'IAsyncOperation`1' })[0]
    $asTask = $asTaskGeneric.MakeGenericMethod($ResultType)
    $netTask = $asTask.Invoke($null, @($WinRtTask))
    $netTask.Wait(-1) | Out-Null
    $netTask.Result
}

$engine = [Windows.Media.Ocr.OcrEngine]::TryCreateFromUserProfileLanguages()

foreach ($p in Get-ChildItem -Path $Folder -Filter *.png | Sort-Object Name | Select-Object -ExpandProperty FullName) {
    $done = $false
    for ($attempt = 1; $attempt -le 4 -and -not $done; $attempt++) {
        try {
            $file = Await ([Windows.Storage.StorageFile]::GetFileFromPathAsync($p)) ([Windows.Storage.StorageFile])
            $stream = Await ($file.OpenAsync([Windows.Storage.FileAccessMode]::Read)) ([Windows.Storage.Streams.IRandomAccessStream])
            $decoder = Await ([Windows.Graphics.Imaging.BitmapDecoder]::CreateAsync($stream)) ([Windows.Graphics.Imaging.BitmapDecoder])
            $bitmap = Await ($decoder.GetSoftwareBitmapAsync()) ([Windows.Graphics.Imaging.SoftwareBitmap])
            $result = Await ($engine.RecognizeAsync($bitmap)) ([Windows.Media.Ocr.OcrResult])
            Write-Output ("=== " + [System.IO.Path]::GetFileName($p) + " (" + $decoder.PixelWidth + "x" + $decoder.PixelHeight + ") ===")
            $lines = $result.Text -split "`r?`n" | Where-Object { $_.Trim() -ne "" }
            Write-Output ($lines -join " | ")
            $done = $true
        } catch {
            Start-Sleep -Milliseconds 800
        }
    }
    if (-not $done) { Write-Output ("=== " + [System.IO.Path]::GetFileName($p) + " FAILED ===") }
}
