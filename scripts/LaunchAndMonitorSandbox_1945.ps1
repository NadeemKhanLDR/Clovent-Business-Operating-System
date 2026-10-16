# D:\CBOS\Repository\scripts\LaunchAndMonitorSandbox_1945.ps1
$ErrorActionPreference = "Continue"

$wsbCandidates = @(
    "C:\Program Files\Windows Sandbox\wsb.exe",
    "$env:LOCALAPPDATA\Microsoft\WindowsApps\wsb.exe"
)
$wsbExe = $wsbCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1
if ($null -eq $wsbExe) {
    Write-Error "wsb.exe not found!"
    exit 1
}

$runDir = "D:\CBOS\Repository\artifacts\acceptance_run_20261014_1945"
$evidenceDir = Join-Path $runDir "evidence"
if (-not (Test-Path $evidenceDir)) {
    New-Item -ItemType Directory -Path $evidenceDir -Force | Out-Null
}
$wsbConfig = Join-Path $runDir "cbos-sandbox-acceptance-20261014-1945.wsb"
$completionFlag = Join-Path $evidenceDir "ACCEPTANCE_20261014_1945_COMPLETE.flag"
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
} else {
    Write-Host "Warning: Could not isolate unique new Sandbox ID from wsb list. Will monitor completion flag."
}

# Monitor execution until completion flag or timeout (25 mins)
$timeoutSec = 1800
$elapsed = 0
$lastLogSize = 0

Write-Host "Monitoring evidence directory: $evidenceDir for $completionFlag..."

while ($elapsed -lt $timeoutSec) {
    Start-Sleep -Seconds 10
    $elapsed += 10

    $progressLog = Join-Path $evidenceDir "sandbox_acceptance_20261014_1945.log"
    if (Test-Path $progressLog) {
        $currentSize = (Get-Item $progressLog).Length
        if ($currentSize -gt $lastLogSize) {
            $lines = Get-Content $progressLog -Tail 5
            Write-Host "[$elapsed s] Progress:"
            $lines | ForEach-Object { Write-Host "   $_" }
            $lastLogSize = $currentSize
        }
    }

    if (Test-Path $completionFlag) {
        Write-Host "ACCEPTANCE RUN COMPLETE! Flag detected at $elapsed seconds."
        Start-Sleep -Seconds 5
        break
    }
}

# Stop ONLY the tracked Sandbox ID
if ($null -ne $newSandboxId) {
    Write-Host "Stopping tracked Sandbox ID: $newSandboxId via wsb.exe stop --id $newSandboxId..."
    & $wsbExe stop --id $newSandboxId
    Write-Host "Stopped Sandbox ID $newSandboxId."
} else {
    Write-Host "No tracked Sandbox ID to stop."
}

if ($elapsed -ge $timeoutSec) {
    Write-Error "TIMEOUT: Timed out after $timeoutSec seconds waiting for acceptance run."
    exit 1
}

if (-not (Test-Path $completionFlag)) {
    Write-Error "FAILURE: Completion flag not found at $completionFlag."
    exit 1
}

$flagContent = (Get-Content $completionFlag -Raw).Trim()
Write-Host "Completion Flag Content: '$flagContent'"

$fatalErrorFile = Join-Path $evidenceDir "sandbox_acceptance_20261014_1830_fatal_error.txt"
if (Test-Path $fatalErrorFile) {
    $fatalErr = Get-Content $fatalErrorFile -Raw
    Write-Error "FAILURE: Sandbox encountered fatal error:`n$fatalErr"
    exit 1
}

if ($flagContent -ne "PASS") {
    Write-Error "FAILURE: Acceptance run completed with non-PASS status: '$flagContent'"
    exit 1
}

# Strictly verify child acceptance reports
$standaloneReportPath = Join-Path $evidenceDir "standalone_interactive_ui_acceptance_report.json"
if (-not (Test-Path $standaloneReportPath)) {
    Write-Error "FAILURE: Standalone report not found at $standaloneReportPath"
    exit 1
}
$standaloneJson = Get-Content $standaloneReportPath -Raw | ConvertFrom-Json
if (-not $standaloneJson.OverallPassed) {
    Write-Error "FAILURE: Standalone acceptance reported OverallPassed = false"
    exit 1
}

$inProcessReportPath = Join-Path $evidenceDir "guest_winforms_ui_cashier_workflow_report.json"
if (-not (Test-Path $inProcessReportPath)) {
    Write-Error "FAILURE: In-process report not found at $inProcessReportPath"
    exit 1
}
$inProcessJson = Get-Content $inProcessReportPath -Raw | ConvertFrom-Json
if (-not $inProcessJson.OverallSuccess) {
    Write-Error "FAILURE: In-process harness reported OverallSuccess = false"
    exit 1
}

Write-Host "================================================================================"
Write-Host "SANDBOX ACCEPTANCE RUN 1830 COMPLETED WITH VERIFIED PASS ACROSS ALL GATES"
Write-Host "================================================================================"
exit 0

