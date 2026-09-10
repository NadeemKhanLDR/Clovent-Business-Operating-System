Get-Process Clovent.Desktop -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Seconds 1
$exe = "d:\Clovent Business Operating System\src\Clovent.Desktop\bin\Debug\net10.0-windows\Clovent.Desktop.exe"
$p = Start-Process $exe -ArgumentList "--pos" -PassThru
Write-Output "Launched with PID $($p.Id)"
Start-Sleep -Seconds 6
$p.Refresh()
Write-Output "MainWindowTitle: '$($p.MainWindowTitle)', MainWindowHandle: $($p.MainWindowHandle)"
Get-Process Clovent.Desktop -ErrorAction SilentlyContinue | Stop-Process -Force
