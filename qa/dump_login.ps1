$cap = "D:\Clovent Business Operating System\Tools\ScreenCaptureUtil\bin\Release\net10.0-windows\ScreenCaptureUtil.exe"
$exe = "D:\Clovent Business Operating System\artifacts\release\Clovent.BusinessOperatingSystem-1.0.7-win-x64\Clovent.Desktop.exe"
$p = Start-Process $exe -PassThru
& $cap wait-win "Sign in" 30
& $cap dump "Sign in"
Stop-Process -Id $p.Id -Force
