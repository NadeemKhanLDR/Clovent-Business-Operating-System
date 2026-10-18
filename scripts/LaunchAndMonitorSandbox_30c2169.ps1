# D:\CBOS\Worktrees\cbos-release-integration\scripts\LaunchAndMonitorSandbox_30c2169.ps1
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

$runDir = "D:\CBOS\Worktrees\cbos-release-integration\artifacts\acceptance_run_candidate_30c2169"
$evidenceDir = Join-Path $runDir "evidence_attempt5"
if (-not (Test-Path $evidenceDir)) {
    New-Item -ItemType Directory -Path $evidenceDir -Force | Out-Null
}
$wsbConfig = Join-Path $runDir "cbos-sandbox-acceptance-30c2169-attempt5.wsb"
$completionFlag = Join-Path $evidenceDir "ACCEPTANCE_30c2169_COMPLETE.flag"
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

# Monitor execution until completion flag or timeout (45 mins max bound)
$timeoutSec = 2700
$elapsed = 0
$lastLogSize = 0
$lastRunnerSize = 0

Write-Host "Monitoring evidence directory: $evidenceDir for $completionFlag..."

while ($elapsed -lt $timeoutSec) {
    Start-Sleep -Seconds 10
    $elapsed += 10

    $progressLog = Join-Path $evidenceDir "sandbox_acceptance_30c2169.log"
    if (Test-Path $progressLog) {
        $currentSize = (Get-Item $progressLog).Length
        if ($currentSize -gt $lastLogSize) {
            $lines = Get-Content $progressLog -Tail 5
            Write-Host "[$elapsed s] Progress:"
            $lines | ForEach-Object { Write-Host "   $_" }
            $lastLogSize = $currentSize
        }
    }

    $runnerLog = Join-Path $evidenceDir "inprocess_runner_console.log"
    if (-not (Test-Path $runnerLog)) {
        $runnerLog = Join-Path $evidenceDir "guest_ui_acceptance_stdout.log"
    }
    if (Test-Path $runnerLog) {
        $currentRunnerSize = (Get-Item $runnerLog).Length
        if ($currentRunnerSize -gt $lastRunnerSize) {
            $lines = Get-Content $runnerLog -Tail 5
            Write-Host "[$elapsed s] Runner:"
            $lines | ForEach-Object { Write-Host "   >> $_" }
            $lastRunnerSize = $currentRunnerSize
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

$fatalErrorFile = Join-Path $evidenceDir "sandbox_acceptance_fatal_error.txt"
if (Test-Path $fatalErrorFile) {
    $fatalErr = Get-Content $fatalErrorFile -Raw
    Write-Error "FAILURE: Sandbox encountered fatal error:`n$fatalErr"
}

# Verify output reports
$wfReportPath = Join-Path $evidenceDir "guest_winforms_ui_cashier_workflow_report.json"
$qualReportPath = Join-Path $evidenceDir "candidate_30c2169_qualification_report.json"
$sqlReportPath = Join-Path $evidenceDir "guest_read_only_sql_verification.json"

if (-not (Test-Path $wfReportPath)) {
    Write-Error "FAILURE: Workflow report not found at $wfReportPath"
    exit 1
}
if (-not (Test-Path $qualReportPath)) {
    Write-Error "FAILURE: Qualification report not found at $qualReportPath"
    exit 1
}
if (-not (Test-Path $sqlReportPath)) {
    Write-Error "FAILURE: Read-only SQL report not found at $sqlReportPath"
    exit 1
}

$wfJson = Get-Content $wfReportPath -Raw | ConvertFrom-Json
$qualJson = Get-Content $qualReportPath -Raw | ConvertFrom-Json

$wfPassed = if ($null -ne $wfJson.OverallPassed) { [bool]$wfJson.OverallPassed } else { $wfJson.OverallStatus -eq "PASS" }
$qualPassed = if ($null -ne $qualJson.OverallPassed) { [bool]$qualJson.OverallPassed } else { $qualJson.OverallStatus -eq "PASS" }

Write-Host "Workflow Report OverallPassed: $wfPassed"
Write-Host "Qualification Report OverallPassed: $qualPassed"

if (-not $wfPassed -or -not $qualPassed) {
    Write-Error "FAILURE: Acceptance reports reported failure! (WorkflowOverallPassed=$wfPassed, QualOverallPassed=$qualPassed)"
    exit 1
}

Write-Host "SUCCESS: Windows Sandbox qualification run for Candidate 30c2169 finished with status PASS."
exit 0
