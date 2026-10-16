# D:\CBOS\Repository\scripts\LaunchSandboxManualHandoff_1215.ps1
$ErrorActionPreference = "Continue"

$wsbCandidates = @(
    "C:\Program Files\Windows Sandbox\wsb.exe",
    "$env:LOCALAPPDATA\Microsoft\WindowsApps\wsb.exe"
)
$wsbExe = $wsbCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1

$runDir = "D:\CBOS\Repository\artifacts\acceptance_run_20261015_1215"
$evidenceDir = Join-Path $runDir "evidence"
if (-not (Test-Path $evidenceDir)) {
    New-Item -ItemType Directory -Path $evidenceDir -Force | Out-Null
}
$wsbConfig = Join-Path $runDir "cbos-sandbox-manual-1215.wsb"
$completionFlag = Join-Path $evidenceDir "MANUAL_HANDOFF_READY.flag"
if (Test-Path $completionFlag) { Remove-Item $completionFlag -Force -ErrorAction SilentlyContinue }

Write-Host "Starting Windows Sandbox for Manual Acceptance Handoff..."
Write-Host "Configuration: $wsbConfig"

Start-Process "C:\Windows\System32\WindowsSandbox.exe" -ArgumentList "`"$wsbConfig`""

# Monitor evidence directory until READY flag appears
$timeoutSec = 1800 # 30 mins
$elapsed = 0
$lastLogSize = 0

Write-Host "Monitoring evidence directory: $evidenceDir for $completionFlag..."

while ($elapsed -lt $timeoutSec) {
    Start-Sleep -Seconds 10
    $elapsed += 10

    $progressLog = Join-Path $evidenceDir "sandbox_manual_handoff_1215.log"
    if (Test-Path $progressLog) {
        $currentSize = (Get-Item $progressLog).Length
        if ($currentSize -gt $lastLogSize) {
            $lines = Get-Content $progressLog -Tail 5
            Write-Host "[$elapsed s] Setup Progress:"
            $lines | ForEach-Object { Write-Host "   $_" }
            $lastLogSize = $currentSize
        }
    }

    if (Test-Path $completionFlag) {
        $flagContent = (Get-Content $completionFlag -Raw).Trim()
        if ($flagContent -eq "READY") {
            Write-Host "================================================================================"
            Write-Host "WINDOWS SANDBOX MANUAL ACCEPTANCE ENVIRONMENT IS READY!"
            Write-Host "POS is open on screen with Order ORD-1 tendered and ready for refund verification."
            Write-Host "Leaving Windows Sandbox running for interactive user testing."
            Write-Host "================================================================================"
            exit 0
        } elseif ($flagContent -eq "FAIL") {
            $fatalErr = Join-Path $evidenceDir "sandbox_manual_handoff_fatal_error.txt"
            if (Test-Path $fatalErr) {
                Write-Error (Get-Content $fatalErr -Raw)
            } else {
                Write-Error "Sandbox setup reported FAIL."
            }
            exit 1
        }
    }
}

Write-Error "TIMEOUT: Timed out waiting for Sandbox environment setup."
exit 1
