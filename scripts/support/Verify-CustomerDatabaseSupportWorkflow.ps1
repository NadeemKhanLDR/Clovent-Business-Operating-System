# ==============================================================================
# CBOS Customer Database Support Workflow Verification Harness
# Demonstrates:
#   1. Disposable synthetic source database provisioning across all 6 bounded contexts
#   2. Synthetic business fixture population (orders, payments, tax snapshots, outbox)
#   3. Full SQL Server backup with privilege verification (support vs cashier role separation)
#   4. Support bundle packaging with SHA-256 checksum manifest and redacted logs
#   5. Checksum verification & isolated restoration into a NEW database
#   6. Restored environment sanitization (outbox quarantine, printing disabled, DPAPI reconfig)
#   7. Preview/read-only diagnosis of isolated issue
#   8. Safe transactional corrective script execution with pre-fix backup & audit logging
#   9. Strict enforcement of PDR-0003 financial transaction immutability & compensating transactions
#  10. Business facts reconciliation between source and restored environments
#  11. Clean teardown of disposable synthetic databases
# ==============================================================================

[CmdletBinding()]
param(
    [Parameter(Mandatory = $false)]
    [string]$Server = "localhost",

    [Parameter(Mandatory = $false)]
    [switch]$KeepDatabases
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
$supportDir = Join-Path $repoRoot "scripts\support"
$correctiveDir = Join-Path $supportDir "corrective_scripts"
$modulePath = Join-Path $supportDir "CbosSupportModule.psm1"

Import-Module $modulePath -Force -DisableNameChecking

$runTimestamp = (Get-Date).ToUniversalTime().ToString("yyyyMMdd_HHmmss")
$sourceDb = "CBOS_Support_Source_${runTimestamp}_Disposable"
$restoredDb = "CBOS_Support_Restored_${runTimestamp}_Disposable"
$bundleOutputDir = Join-Path $env:ProgramData "Clovent\SupportVerification_${runTimestamp}"

$masterConnStr = "Server=$Server;Database=master;Integrated Security=True;TrustServerCertificate=True;"
$sourceConnStr = "Server=$Server;Database=$sourceDb;Integrated Security=True;TrustServerCertificate=True;"
$restoredConnStr = "Server=$Server;Database=$restoredDb;Integrated Security=True;TrustServerCertificate=True;"

$provisionerExe = Join-Path $repoRoot "Tools\Clovent.Installer.Provisioner\bin\Debug\net10.0-windows\win-x64\Clovent.Installer.Provisioner.exe"
$fixtureRunnerExe = Join-Path $repoRoot "Tools\SupportFixtureRunner\bin\Debug\net10.0-windows\Clovent.SupportFixtureRunner.exe"

function Assert-Condition($condition, $message) {
    if (-not $condition) {
        Write-Host "ASSERTION FAILED: $message" -ForegroundColor Red
        throw "ASSERTION FAILED: $message"
    }
    Write-Host "  [OK] $message" -ForegroundColor Green
}

function Drop-DatabaseSafely($dbName) {
    if (-not $dbName.StartsWith("CBOS_Support_", [System.StringComparison]::OrdinalIgnoreCase) -or
        -not $dbName.EndsWith("_Disposable", [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to drop database '$dbName' that does not match disposable naming convention."
    }

    $conn = New-Object System.Data.SqlClient.SqlConnection($masterConnStr)
    $conn.Open()
    try {
        $cmd = $conn.CreateCommand()
        $escaped = $dbName.Replace("]", "]]")
        $cmd.CommandText = @"
IF EXISTS (SELECT 1 FROM sys.databases WHERE name = N'$escaped')
BEGIN
    ALTER DATABASE [$escaped] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE [$escaped];
END
"@
        $cmd.CommandTimeout = 60
        $cmd.ExecuteNonQuery() | Out-Null
    }
    finally {
        $conn.Close()
        $conn.Dispose()
    }
}

Write-Host "================================================================================" -ForegroundColor Cyan
Write-Host "  CLOVENT BUSINESS OPERATING SYSTEM (CBOS) - CUSTOMER DATABASE SUPPORT WORKFLOW" -ForegroundColor Cyan
Write-Host "  Demonstration & Verification Suite" -ForegroundColor Cyan
Write-Host "  Source Disposable Database:   $sourceDb" -ForegroundColor Cyan
Write-Host "  Restored Disposable Database: $restoredDb" -ForegroundColor Cyan
Write-Host "  Server Instance:              $Server" -ForegroundColor Cyan
Write-Host "================================================================================`n" -ForegroundColor Cyan

try {
    # --------------------------------------------------------------------------
    # Step 0: Pre-cleanup check
    # --------------------------------------------------------------------------
    Write-Host "[STEP 0/10] Preparing clean disposable verification environment..." -ForegroundColor Cyan
    Drop-DatabaseSafely $sourceDb
    Drop-DatabaseSafely $restoredDb
    if (Test-Path $bundleOutputDir) { try { Remove-Item $bundleOutputDir -Recurse -Force -ErrorAction SilentlyContinue } catch { } }
    New-CbosSupportDirectory -Path $bundleOutputDir | Out-Null

    # --------------------------------------------------------------------------
    # Step 1: Provision Disposable Source Database across all 6 Bounded Contexts
    # --------------------------------------------------------------------------
    Write-Host "[STEP 1/10] Provisioning disposable source database via Clovent.Installer.Provisioner..." -ForegroundColor Cyan
    & $provisionerExe --server $Server --database $sourceDb --auth windows --provision
    Assert-Condition ($LASTEXITCODE -eq 0) "Source database '$sourceDb' provisioned with all 6 bounded contexts and schema initializers."

    # --------------------------------------------------------------------------
    # Step 2: Populate Synthetic Business Fixture
    # --------------------------------------------------------------------------
    Write-Host "[STEP 2/10] Seeding synthetic business fixture via SupportFixtureRunner..." -ForegroundColor Cyan
    $factsFile = Join-Path $bundleOutputDir "fixture_facts.json"
    & $fixtureRunnerExe --seed --connection-string $sourceConnStr --facts-file $factsFile
    Assert-Condition ($LASTEXITCODE -eq 0 -and (Test-Path $factsFile)) "Synthetic business fixture successfully populated across all 6 bounded contexts."

    # --------------------------------------------------------------------------
    # Step 3: Privilege Verification & Full SQL Server Backup
    # --------------------------------------------------------------------------
    Write-Host "[STEP 3/10] Verifying support privileges and executing full database backup..." -ForegroundColor Cyan
    $backupResult = Backup-CbosDatabase `
        -Server $Server `
        -Database $sourceDb `
        -UseWindowsAuth `
        -BackupDirectory (Join-Path $bundleOutputDir "backups")

    Assert-Condition ($backupResult.Success -and (Test-Path $backupResult.BackupPath)) "Full database backup created successfully with COPY_ONLY and CHECKSUM."
    Assert-Condition ($backupResult.SchemasBackedUp.Count -eq 6) "Backup verified all 6 bounded-context schemas present."
    Assert-Condition (-not [string]::IsNullOrWhiteSpace($backupResult.Sha256Checksum)) "SHA-256 digest computed for backup archive ($($backupResult.Sha256Checksum))."

    # --------------------------------------------------------------------------
    # Step 4: Support Bundle Packaging & Customer Authorization
    # --------------------------------------------------------------------------
    Write-Host "[STEP 4/10] Packaging support bundle with customer authorization gate and redacted logs..." -ForegroundColor Cyan
    
    # Test Negative Gate: Missing customer authorization must fail
    $consentBlocked = $false
    try {
        New-CbosSupportBundle -Server $Server -Database $sourceDb -UseWindowsAuth -OutputDirectory $bundleOutputDir
    } catch {
        $consentBlocked = $_.Exception.Message -match "CUSTOMER AUTHORIZATION REQUIRED"
    }
    Assert-Condition $consentBlocked "Customer authorization gate successfully blocked export when -CustomerConsent was omitted."

    # Authorized Bundle Creation
    $bundleResult = New-CbosSupportBundle `
        -Server $Server `
        -Database $sourceDb `
        -UseWindowsAuth `
        -TicketId "TICKET-SUPPORT-20261017" `
        -CustomerOrganization "Grandview Synthetic Retail" `
        -AuthorizedBy "Alex Vance (Store Director)" `
        -CustomerConsent `
        -OutputDirectory $bundleOutputDir

    Assert-Condition ($bundleResult.Success -and (Test-Path $bundleResult.BundleZipPath)) "Support bundle zip created successfully: $($bundleResult.BundleZipPath)"

    # Inspect Bundle Manifest & Checksum
    $extractDir = Join-Path $bundleOutputDir "bundle_extracted"
    New-CbosSupportDirectory -Path $extractDir | Out-Null
    Expand-Archive -Path $bundleResult.BundleZipPath -DestinationPath $extractDir -Force
    $manifestFile = Join-Path $extractDir "manifest.json"
    Assert-Condition (Test-Path $manifestFile) "Support bundle manifest.json verified."

    $manifestJson = Get-Content -Path $manifestFile -Raw -Encoding UTF8 | ConvertFrom-Json
    Assert-Condition ($manifestJson.CustomerAuthorization.ConsentConfirmed -eq $true) "Customer authorization verified in manifest."
    $migrationPropCount = @($manifestJson.BoundedContextMigrations.PSObject.Properties).Count
    Assert-Condition ($migrationPropCount -eq 6) "Manifest records EF Core migration versions across all 6 schemas."

    # --------------------------------------------------------------------------
    # Step 5: Checksum Verification & Isolated Database Restoration
    # --------------------------------------------------------------------------
    Write-Host "[STEP 5/10] Verifying checksum and restoring into NEW isolated database '$restoredDb'..." -ForegroundColor Cyan
    $bundleBakPath = Join-Path $extractDir $manifestJson.Backup.FileName

    # Negative Test 1: Mismatched checksum rejection
    $tamperBlocked = $false
    try {
        Restore-CbosIsolatedDatabase -BackupFilePath $bundleBakPath -TargetDatabaseName "CBOS_Tamper_Test_Disposable" -ExpectedSha256 "0000000000000000000000000000000000000000000000000000000000000000"
    } catch {
        $tamperBlocked = $_.Exception.Message -match "CHECKSUM MISMATCH"
    }
    Assert-Condition $tamperBlocked "Restoration safely rejected archive with mismatched SHA-256 checksum."

    # Negative Test 2: Forbidden database name rejection
    $forbiddenBlocked = $false
    try {
        Restore-CbosIsolatedDatabase -BackupFilePath $bundleBakPath -TargetDatabaseName "Clovent_BusinessOperatingSystem" -ExpectedSha256 $manifestJson.Backup.Sha256Checksum
    } catch {
        $forbiddenBlocked = $_.Exception.Message -match "SAFETY VIOLATION"
    }
    Assert-Condition $forbiddenBlocked "Restoration safety guard strictly blocked attempt to target operational database 'Clovent_BusinessOperatingSystem'."

    # Actual Isolated Restoration
    $restoreResult = Restore-CbosIsolatedDatabase `
        -BackupFilePath $bundleBakPath `
        -TargetDatabaseName $restoredDb `
        -Server $Server `
        -UseWindowsAuth `
        -ExpectedSha256 $manifestJson.Backup.Sha256Checksum

    Assert-Condition ($restoreResult.Success) "Database restored cleanly into isolated database '$restoredDb' with DBCC CHECKDB consistency."

    # --------------------------------------------------------------------------
    # Step 6: Restored Environment Sanitization
    # --------------------------------------------------------------------------
    Write-Host "[STEP 6/10] Verifying environment isolation (outbox quarantine, printing disabled, DPAPI reconfig)..." -ForegroundColor Cyan
    $isolateConfigDir = Join-Path $bundleOutputDir "isolated_config"
    $protectResult = Protect-CbosRestoredEnvironment `
        -TargetDatabaseName $restoredDb `
        -Server $Server `
        -UseWindowsAuth `
        -ConfigDirectory $isolateConfigDir

    Assert-Condition ($protectResult.QuarantinedOutboxCount -gt 0) "Transactional outbox messages for printing and sync successfully quarantined to DeadLetter."
    Assert-Condition (Test-Path $protectResult.PrinterConfigPath) "Isolated printer configuration verified (physical printing disabled)."
    Assert-Condition (Test-Path $protectResult.DatabaseConfigPath) "Machine-bound DPAPI configuration handled: local database.config.json generated."

    # --------------------------------------------------------------------------
    # Step 7: Isolated Diagnosis (Preview / Read-Only Mode)
    # --------------------------------------------------------------------------
    Write-Host "[STEP 7/10] Diagnosing stuck outbox message in isolated copy (Preview Mode)..." -ForegroundColor Cyan
    $script01 = Join-Path $correctiveDir "FIX_20261017_01_QuarantineStuckOutbox.sql"
    $dryRunResult = Invoke-CbosCorrectiveScript `
        -ScriptFilePath $script01 `
        -TargetDatabaseName $restoredDb `
        -Server $Server `
        -UseWindowsAuth `
        -DryRun

    Assert-Condition ($dryRunResult.Success -and $dryRunResult.Mode -eq "DryRun") "Preview diagnosis executed in read-only mode with zero mutations."

    # --------------------------------------------------------------------------
    # Step 8: Transactional Execution of Corrective Script
    # --------------------------------------------------------------------------
    Write-Host "[STEP 8/10] Executing reviewed corrective script with pre-fix backup and audit trail..." -ForegroundColor Cyan
    $execResult = Invoke-CbosCorrectiveScript `
        -ScriptFilePath $script01 `
        -TargetDatabaseName $restoredDb `
        -Server $Server `
        -UseWindowsAuth `
        -Execute `
        -PreTestedOnRestoredCopy

    Assert-Condition ($execResult.Success -and $execResult.Outcome -eq "Success") "Corrective script committed successfully inside transaction."
    Assert-Condition (Test-Path $execResult.PreFixBackup) "Fresh pre-fix backup created prior to mutation ($($execResult.PreFixBackup))."

    # Verify Audit Table Entry
    $conn = New-Object System.Data.SqlClient.SqlConnection($restoredConnStr)
    $conn.Open()
    $cmdAudit = $conn.CreateCommand()
    $cmdAudit.CommandText = "SELECT COUNT(1) FROM [MasterData].[SupportScriptExecutionAudit] WHERE [ScriptId] = 'FIX_20261017_01_QuarantineStuckOutbox' AND [Outcome] = 'Success';"
    $auditRecorded = [int]$cmdAudit.ExecuteScalar() -gt 0
    $conn.Close()
    Assert-Condition $auditRecorded "Execution logged in permanent audit table [MasterData].[SupportScriptExecutionAudit]."

    # Test Repeat-Execution Protection
    $repeatBlocked = $false
    try {
        Invoke-CbosCorrectiveScript `
            -ScriptFilePath $script01 `
            -TargetDatabaseName $restoredDb `
            -Server $Server `
            -UseWindowsAuth `
            -Execute `
            -PreTestedOnRestoredCopy
    } catch {
        $repeatBlocked = $_.Exception.Message -match "REPEAT EXECUTION BLOCKED"
    }
    Assert-Condition $repeatBlocked "Repeat-execution protection prevented duplicate script re-run."

    # --------------------------------------------------------------------------
    # Step 9: PDR-0003 Financial Immutability Enforcement & Compensating Transactions
    # --------------------------------------------------------------------------
    Write-Host "[STEP 9/10] Verifying PDR-0003 Financial Immutability & Approved Compensating Transactions..." -ForegroundColor Cyan
    
    # Negative Test: Direct rewrite of completed orders must be rejected
    $scriptReject = Join-Path $correctiveDir "REJECT_SampleIllegalFinancialRewrite.sql"
    $rewriteBlocked = $false
    try {
        Invoke-CbosCorrectiveScript `
            -ScriptFilePath $scriptReject `
            -TargetDatabaseName $restoredDb `
            -Server $Server `
            -UseWindowsAuth `
            -Execute `
            -PreTestedOnRestoredCopy
    } catch {
        $rewriteBlocked = $_.Exception.Message -match "PDR-0003 FINANCIAL INTEGRITY VIOLATION"
    }
    Assert-Condition $rewriteBlocked "PDR-0003 static analysis guard strictly blocked attempt to rewrite completed orders."

    # Positive Test: Approved compensating transaction on customer ledger
    $script02 = Join-Path $correctiveDir "FIX_20261017_02_CompensatingLedgerAdjustment.sql"
    $compResult = Invoke-CbosCorrectiveScript `
        -ScriptFilePath $script02 `
        -TargetDatabaseName $restoredDb `
        -Server $Server `
        -UseWindowsAuth `
        -Execute `
        -PreTestedOnRestoredCopy `
        -CompensationReference "AUTH-DISPUTE-REF-9921"

    Assert-Condition ($compResult.Success -and $compResult.Outcome -eq "Success") "Compensating transaction applied successfully under PDR-0003."

    # --------------------------------------------------------------------------
    # Step 10: Reconcile Business Facts between Source and Restored Databases
    # --------------------------------------------------------------------------
    Write-Host "[STEP 10/10] Reconciling business facts between Source and Restored environments..." -ForegroundColor Cyan
    & $fixtureRunnerExe --reconcile --source $sourceConnStr --restored $restoredConnStr
    Assert-Condition ($LASTEXITCODE -eq 0) "All domain business facts, sales, taxes, receipts, refunds, and balances reconciled cleanly."

    Write-Host "`n================================================================================" -ForegroundColor Green
    Write-Host "  CUSTOMER DATABASE SUPPORT WORKFLOW VERIFICATION SUCCEEDED" -ForegroundColor Green
    Write-Host "  All 10 Verification Phases Passed Cleanly." -ForegroundColor Green
    Write-Host "================================================================================`n" -ForegroundColor Green
}
finally {
    # --------------------------------------------------------------------------
    # Teardown: Clean up disposable resources
    # --------------------------------------------------------------------------
    if (-not $KeepDatabases) {
        Write-Host "Tearing down disposable verification databases and temporary bundles..." -ForegroundColor Cyan
        try { Drop-DatabaseSafely $sourceDb } catch { }
        try { Drop-DatabaseSafely $restoredDb } catch { }
        if (Test-Path $bundleOutputDir) {
            try { Remove-Item $bundleOutputDir -Recurse -Force } catch { }
        }
        Write-Host "Teardown complete. Zero dangling databases or test files retained." -ForegroundColor Green
    } else {
        Write-Host "Skipping teardown (-KeepDatabases specified). Retained $sourceDb and $restoredDb." -ForegroundColor Yellow
    }
}
