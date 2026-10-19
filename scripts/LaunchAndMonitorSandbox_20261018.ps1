# D:\CBOS\Repository\scripts\LaunchAndMonitorSandbox_20261018.ps1
$ErrorActionPreference = "Continue"

$wsbExe = "$env:LOCALAPPDATA\Microsoft\WindowsApps\wsb.exe"
if (-not (Test-Path $wsbExe)) {
    $wsbExe = "C:\Program Files\Windows Sandbox\wsb.exe"
}

$runDir = "D:\CBOS\Repository\artifacts\acceptance_run_20261018_1015"
$evidenceDir = Join-Path $runDir "evidence"
if (-not (Test-Path $evidenceDir)) {
    New-Item -ItemType Directory -Path $evidenceDir -Force | Out-Null
}
$wsbConfig = Join-Path $runDir "cbos-sandbox-acceptance-20261018.wsb"
$completionFlag = Join-Path $evidenceDir "MANUAL_HANDOFF_READY.flag"
if (Test-Path $completionFlag) { Remove-Item $completionFlag -Force -ErrorAction SilentlyContinue }

$preExistingList = (& $wsbExe list 2>$null | Out-String).Trim()
$preExistingIds = @()
if (-not [string]::IsNullOrWhiteSpace($preExistingList)) {
    $preExistingIds = $preExistingList -split "\r?\n" | ForEach-Object { $_.Trim() } | Where-Object { $_ -match "^[0-9a-fA-F-]{36}$" }
}
Write-Host "Pre-existing Sandbox IDs: $($preExistingIds -join ', ')"

Write-Host "Starting Windows Sandbox with config: $wsbConfig"
Start-Process "C:\Windows\System32\WindowsSandbox.exe" -ArgumentList "`"$wsbConfig`""

# Detect the newly created Sandbox ID
$newSandboxId = $null
$detectAttempts = 0
while ($detectAttempts -lt 30 -and $null -eq $newSandboxId) {
    Start-Sleep -Seconds 2
    $detectAttempts++
    $currentList = (& $wsbExe list 2>$null | Out-String).Trim()
    if (-not [string]::IsNullOrWhiteSpace($currentList)) {
        $currentIds = $currentList -split "\r?\n" | ForEach-Object { $_.Trim() } | Where-Object { $_ -match "^[0-9a-fA-F-]{36}$" }
        foreach ($id in $currentIds) {
            if ($id -notin $preExistingIds) {
                $newSandboxId = $id
                break
            }
        }
    }
}

if ($null -ne $newSandboxId) {
    Write-Host "Tracked newly created Sandbox ID: $newSandboxId"
    $newSandboxId | Set-Content -Path (Join-Path $evidenceDir "tracked_sandbox_id.txt") -Encoding UTF8
} else {
    Write-Host "Warning: Could not isolate unique new Sandbox ID from wsb list. Will monitor completion flag."
}

# Monitor execution until completion flag or timeout (20 mins)
$timeoutSec = 1200
$elapsed = 0
$lastLogSize = 0

Write-Host "Monitoring evidence directory: $evidenceDir for $completionFlag..."

while ($elapsed -lt $timeoutSec) {
    Start-Sleep -Seconds 10
    $elapsed += 10

    $progressLog = Join-Path $evidenceDir "sandbox_handoff_20261018.log"
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
            Write-Host "WINDOWS SANDBOX ACCEPTANCE ENVIRONMENT IS READY!"
            Write-Host "Candidate 939d5e9 installed and commissioned. Ready for interactive testing."
            Write-Host "================================================================================"
            exit 0
        } elseif ($flagContent -eq "FAIL") {
            $fatalErr = Join-Path $evidenceDir "sandbox_fatal_error.txt"
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
