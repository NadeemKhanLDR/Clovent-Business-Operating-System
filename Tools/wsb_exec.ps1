Param(
    [Parameter(Mandatory=$true)]
    [string]$ScriptContent
)
$wsbExe = "$env:LOCALAPPDATA\Microsoft\WindowsApps\wsb.exe"
$sandboxId = "ec5c21ce-b1f7-4a78-b5b9-d608b5865f8b"
$evidenceDirHost = "D:\CBOS\Repository\artifacts\acceptance_run_20261018_1015\evidence"
$scriptFileHost = Join-Path $evidenceDirHost "sb_cmd.ps1"
$outFileHost = Join-Path $evidenceDirHost "sb_cmd.out"

if (Test-Path $outFileHost) { Remove-Item $outFileHost -Force }

# Wrap script content to capture output and errors
$wrappedScript = @"
try {
    `$out = & {
$ScriptContent
    } *>&1 | Out-String
    [System.IO.File]::WriteAllText('C:\CBOS_Evidence\sb_cmd.out', `$out)
} catch {
    [System.IO.File]::WriteAllText('C:\CBOS_Evidence\sb_cmd.out', "EXCEPTION: " + `$_.ToString())
}
"@

[System.IO.File]::WriteAllText($scriptFileHost, $wrappedScript, [System.Text.Encoding]::UTF8)

# Execute via wsb inside sandbox
$res = & $wsbExe exec --id $sandboxId --run-as ExistingLogin -c "powershell.exe -NoProfile -ExecutionPolicy Bypass -File C:\CBOS_Evidence\sb_cmd.ps1"

# Wait for output file to appear
$timeout = 20
$sw = [System.Diagnostics.Stopwatch]::StartNew()
while ($sw.Elapsed.TotalSeconds -lt $timeout) {
    if (Test-Path $outFileHost) {
        # give a tiny moment to flush
        Start-Sleep -Milliseconds 200
        Get-Content $outFileHost
        return
    }
    Start-Sleep -Milliseconds 250
}

Write-Error "Timed out waiting for sandbox script output"
