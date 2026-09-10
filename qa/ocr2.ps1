Param([string[]]$Files, [string]$Base = "C:\Users\NexGen\Pictures\recallqa2")
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

foreach ($f in $Files) {
    $fs = [System.IO.File]::OpenRead((Join-Path $Base $f))
    try {
        $stream = [System.IO.WindowsRuntimeStreamExtensions]::AsRandomAccessStream($fs)
        $dec = Await ([Windows.Graphics.Imaging.BitmapDecoder]::CreateAsync($stream)) ([Windows.Graphics.Imaging.BitmapDecoder])
        $bmp = Await ($dec.GetSoftwareBitmapAsync()) ([Windows.Graphics.Imaging.SoftwareBitmap])
        $res = Await ($engine.RecognizeAsync($bmp)) ([Windows.Media.Ocr.OcrResult])
        $lines = $res.Text -split "`r?`n" | Where-Object { $_.Trim() -ne "" }
        Write-Output ("=== " + $f + " ===")
        Write-Output ($lines -join " | ")
    } catch {
        Write-Output ("ERR " + $f + ": " + $_.Exception.Message)
    }
    finally { $fs.Dispose() }
}
