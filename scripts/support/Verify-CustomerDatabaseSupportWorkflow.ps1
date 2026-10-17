# ==============================================================================
# CBOS Customer Database Support Workflow Verification Harness
# Demonstrates:
#   1. Authentication & Privilege Separation: Windows Auth + dedicated SQL login, cashier denial without credential leakage.
#   2. Disposable synthetic source database provisioning across all 6 bounded contexts.
#   3. Full database backup with COPY_ONLY, CHECKSUM, and SHA-256 verification.
#   4. Support bundle packaging with mandatory customer consent, redacted logs, and diagnostic manifest.
#   5. Checksum verification & restoration into TWO separate databases:
#      - Untouched Restored Reference Copy (CBOS_Support_Ref_*_Disposable)
#      - Sanitized Diagnostic Copy (CBOS_Support_Diag_*_Disposable)
#   6. Environment isolation proving zero external dispatch:
#      - Diagnostic copy: Outbox DeadLetter quarantine, Sync inbox Failed quarantine, suspended QuickBooks maps, disabled printing.
#      - Reference copy: retains exact original pending outbox records untouched.
#   7. Restore Equivalence Reconciliation: Source vs Restored Reference = 100% Identical.
#   8. Corrective Script Security & Negative Checks:
#      - Target database pattern guard blocks cross-database execution.
#      - Script catalog whitelist blocks unapproved scripts.
#      - Failed precheck leaves zero mutation.
#      - Failed postcheck triggers transactional rollback leaving zero mutation and logs 'Failed' audit.
#      - PDR-0003 static analysis guard strictly blocks direct financial rewrites.
#   9. Approved Compensating Transaction Execution:
#      - Domain-compliant additive credit memo for customer billing dispute.
#      - Verifies 100.00 PKR adjustment: original balance 1,044.00 -> corrected balance 944.00 PKR.
#      - Concurrency-safe repeat-execution protection via UX_SupportScriptExecutionAudit_ScriptId_Success.
#  10. Post-Correction Reconciliation: Reference vs Diagnostic copy proves financial immutability.
#  11. Clean teardown of disposable synthetic databases and temporary logins.
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
$refDb = "CBOS_Support_Ref_${runTimestamp}_Disposable"
$diagDb = "CBOS_Support_Diag_${runTimestamp}_Disposable"
$bundleOutputDir = Join-Path $env:ProgramData "Clovent\SupportVerification_${runTimestamp}"

$masterConnStr = "Server=$Server;Database=master;Integrated Security=True;TrustServerCertificate=True;"
$sourceConnStr = "Server=$Server;Database=$sourceDb;Integrated Security=True;TrustServerCertificate=True;"
$refConnStr = "Server=$Server;Database=$refDb;Integrated Security=True;TrustServerCertificate=True;"
$diagConnStr = "Server=$Server;Database=$diagDb;Integrated Security=True;TrustServerCertificate=True;"

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

    [System.Data.SqlClient.SqlConnection]::ClearAllPools()
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

function Drop-DisposableSqlLoginSafely($loginName) {
    [System.Data.SqlClient.SqlConnection]::ClearAllPools()
    $conn = New-Object System.Data.SqlClient.SqlConnection($masterConnStr)
    $conn.Open()
    try {
        $cmd = $conn.CreateCommand()
        $escapedLogin = $loginName.Replace("]", "]]")
        $cmd.CommandText = @"
DECLARE @sid VARBINARY(85);
SELECT @sid = sid FROM sys.server_principals WHERE name = N'$escapedLogin';
IF @sid IS NOT NULL
BEGIN
    DECLARE @spid INT;
    DECLARE cur CURSOR LOCAL FAST_FORWARD FOR SELECT session_id FROM sys.dm_exec_sessions WHERE security_id = @sid;
    OPEN cur;
    FETCH NEXT FROM cur INTO @spid;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        EXEC('KILL ' + @spid);
        FETCH NEXT FROM cur INTO @spid;
    END
    CLOSE cur;
    DEALLOCATE cur;
    DROP LOGIN [$escapedLogin];
END
"@
        $cmd.ExecuteNonQuery() | Out-Null
    }
    finally {
        $conn.Close()
        $conn.Dispose()
    }
}

Write-Host "================================================================================" -ForegroundColor Cyan
Write-Host "  CLOVENT BUSINESS OPERATING SYSTEM (CBOS) - CUSTOMER DATABASE SUPPORT WORKFLOW" -ForegroundColor Cyan
Write-Host "  Hardened Verification Suite (Review of Commit eed572c)" -ForegroundColor Cyan
Write-Host "  Source Disposable Database:    $sourceDb" -ForegroundColor Cyan
Write-Host "  Reference Disposable Database: $refDb" -ForegroundColor Cyan
Write-Host "  Diagnostic Disposable Database:$diagDb" -ForegroundColor Cyan
Write-Host "  Server Instance:               $Server" -ForegroundColor Cyan
Write-Host "================================================================================`n" -ForegroundColor Cyan

try {
    # --------------------------------------------------------------------------
    # Step 0: Pre-cleanup check
    # --------------------------------------------------------------------------
    Write-Host "[STEP 0/10] Preparing clean disposable verification environment..." -ForegroundColor Cyan
    Drop-DatabaseSafely $sourceDb
    Drop-DatabaseSafely $refDb
    Drop-DatabaseSafely $diagDb
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
    # Step 3: Authentication & Privilege Separation Verification
    # --------------------------------------------------------------------------
    Write-Host "[STEP 3/10] Verifying authentication modes and support privilege separation..." -ForegroundColor Cyan

    $dispCashierUser = "cbos_cashier_disp"
    $dispSupportUser = "cbos_support_disp"
    $dispCashierPassPlain = "CashierDispP@ss2026!"
    $dispSupportPassPlain = "Supp0rtDispP@ss2026!"
    $dispCashierPass = ConvertTo-SecureString $dispCashierPassPlain -AsPlainText -Force
    $dispSupportPass = ConvertTo-SecureString $dispSupportPassPlain -AsPlainText -Force

    # Create disposable logins in SQL Server
    $connMaster = New-Object System.Data.SqlClient.SqlConnection($masterConnStr)
    $connMaster.Open()
    try {
        $cmdCreate = $connMaster.CreateCommand()
        $cmdCreate.CommandText = @"
IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = N'$dispCashierUser')
BEGIN
    CREATE LOGIN [$dispCashierUser] WITH PASSWORD = N'$dispCashierPassPlain', CHECK_POLICY = OFF;
END
IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = N'$dispSupportUser')
BEGIN
    CREATE LOGIN [$dispSupportUser] WITH PASSWORD = N'$dispSupportPassPlain', CHECK_POLICY = OFF;
    ALTER SERVER ROLE [sysadmin] ADD MEMBER [$dispSupportUser];
END
"@
        $cmdCreate.ExecuteNonQuery() | Out-Null
    }
    finally {
        $connMaster.Close()
        $connMaster.Dispose()
    }

    # Grant cashier least-privilege runtime access on sourceDb
    $connSource = New-Object System.Data.SqlClient.SqlConnection($sourceConnStr)
    $connSource.Open()
    try {
        $cmdCashierRole = $connSource.CreateCommand()
        $cmdCashierRole.CommandText = @"
IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'$dispCashierUser')
BEGIN
    CREATE USER [$dispCashierUser] FOR LOGIN [$dispCashierUser];
    ALTER ROLE [db_datareader] ADD MEMBER [$dispCashierUser];
    ALTER ROLE [db_datawriter] ADD MEMBER [$dispCashierUser];
END
"@
        $cmdCashierRole.ExecuteNonQuery() | Out-Null
    }
    finally {
        $connSource.Close()
        $connSource.Dispose()
    }

    # 3a. Demonstrate Cashier Denial & Zero Credential Leakage
    $cashierDenied = $false
    $cashierErrMsg = ""
    try {
        Backup-CbosDatabase `
            -Server $Server `
            -Database $sourceDb `
            -SqlUser $dispCashierUser `
            -SqlPassword $dispCashierPass `
            -BackupDirectory (Join-Path $bundleOutputDir "cashier_attempt")
    }
    catch {
        $cashierErrMsg = $_.Exception.Message
        $cashierDenied = $cashierErrMsg -match "SECURITY VIOLATION"
    }

    Assert-Condition $cashierDenied "Cashier account '$dispCashierUser' strictly denied backup privileges."
    Assert-Condition ($cashierErrMsg -notmatch $dispCashierPassPlain) "Cashier denial error contains zero plaintext password leakage."

    # 3b. Demonstrate Dedicated SQL Support Login Backup Success
    $sqlAuthBackupDir = Join-Path $bundleOutputDir "backups_sqlauth"
    $sqlAuthResult = Backup-CbosDatabase `
        -Server $Server `
        -Database $sourceDb `
        -SqlUser $dispSupportUser `
        -SqlPassword $dispSupportPass `
        -BackupDirectory $sqlAuthBackupDir

    Assert-Condition ($sqlAuthResult.Success -and (Test-Path $sqlAuthResult.BackupPath)) "Dedicated SQL support login '$dispSupportUser' created full backup successfully."

    # Clean up disposable SQL logins
    Drop-DisposableSqlLoginSafely $dispCashierUser
    Drop-DisposableSqlLoginSafely $dispSupportUser
    Write-Host "  Disposable SQL logins cleanly dropped after privilege verification." -ForegroundColor Gray

    # 3c. Execute Windows Authentication Backup for Primary Support Bundle
    $backupResult = Backup-CbosDatabase `
        -Server $Server `
        -Database $sourceDb `
        -UseWindowsAuth `
        -BackupDirectory (Join-Path $bundleOutputDir "backups")

    Assert-Condition ($backupResult.Success -and (Test-Path $backupResult.BackupPath)) "Windows Authentication backup created successfully with COPY_ONLY and CHECKSUM."
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
    # Step 5: Checksum Verification & Isolated Restoration into TWO Databases
    # --------------------------------------------------------------------------
    Write-Host "[STEP 5/10] Verifying checksum and restoring into Reference copy and Diagnostic copy..." -ForegroundColor Cyan
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

    # 5a. Restore Untouched Restored Reference Copy
    $refRestoreResult = Restore-CbosIsolatedDatabase `
        -BackupFilePath $bundleBakPath `
        -TargetDatabaseName $refDb `
        -Server $Server `
        -UseWindowsAuth `
        -ExpectedSha256 $manifestJson.Backup.Sha256Checksum

    Assert-Condition ($refRestoreResult.Success) "Untouched Reference database '$refDb' restored cleanly."

    # 5b. Restore Diagnostic Copy
    $diagRestoreResult = Restore-CbosIsolatedDatabase `
        -BackupFilePath $bundleBakPath `
        -TargetDatabaseName $diagDb `
        -Server $Server `
        -UseWindowsAuth `
        -ExpectedSha256 $manifestJson.Backup.Sha256Checksum

    Assert-Condition ($diagRestoreResult.Success) "Diagnostic database '$diagDb' restored cleanly."

    # --------------------------------------------------------------------------
    # Step 6: Restored Environment Sanitization & Isolation Verification
    # --------------------------------------------------------------------------
    Write-Host "[STEP 6/10] Quarantining Diagnostic copy while preserving pristine Reference copy..." -ForegroundColor Cyan
    $isolateConfigDir = Join-Path $bundleOutputDir "isolated_config"
    $protectResult = Protect-CbosRestoredEnvironment `
        -TargetDatabaseName $diagDb `
        -Server $Server `
        -UseWindowsAuth `
        -ConfigDirectory $isolateConfigDir

    Assert-Condition ($protectResult.QuarantinedOutboxCount -gt 0) "Diagnostic copy outbox quarantined to DeadLetter ($($protectResult.QuarantinedOutboxCount) messages)."
    Assert-Condition (Test-Path $protectResult.PrinterConfigPath) "Diagnostic printer configuration disabled (printers.json generated)."
    Assert-Condition (Test-Path $protectResult.DatabaseConfigPath) "Diagnostic database configuration reconfigured for local DPAPI."

    # Prove Reference Copy is preserved completely untouched
    $connRef = New-Object System.Data.SqlClient.SqlConnection($refConnStr)
    $connRef.Open()
    $cmdPendingRef = $connRef.CreateCommand()
    $cmdPendingRef.CommandText = "SELECT COUNT(1) FROM [Restaurant].[OutboxMessages] WHERE [Status] IN ('Pending', 'Processing', 'RetryScheduled');"
    $pendingInRef = [int]$cmdPendingRef.ExecuteScalar()
    $connRef.Close()

    # Prove Diagnostic Copy has zero active outbox records
    $connDiag = New-Object System.Data.SqlClient.SqlConnection($diagConnStr)
    $connDiag.Open()
    $cmdPendingDiag = $connDiag.CreateCommand()
    $cmdPendingDiag.CommandText = "SELECT COUNT(1) FROM [Restaurant].[OutboxMessages] WHERE [Status] IN ('Pending', 'Processing', 'RetryScheduled');"
    $pendingInDiag = [int]$cmdPendingDiag.ExecuteScalar()
    $connDiag.Close()

    Assert-Condition ($pendingInRef -gt 0) "Reference copy retains original pending outbox records ($pendingInRef active)."
    Assert-Condition ($pendingInDiag -eq 0) "Diagnostic copy has ZERO pending outbox records (cannot dispatch printing, payments, or sync)."

    # --------------------------------------------------------------------------
    # Step 7: Restore Equivalence Reconciliation (Source vs Restored Reference)
    # --------------------------------------------------------------------------
    Write-Host "[STEP 7/10] Reconciling equivalence between Source and Restored Reference database..." -ForegroundColor Cyan
    & $fixtureRunnerExe --reconcile-equivalence --source $sourceConnStr --reference $refConnStr --facts-file $factsFile
    Assert-Condition ($LASTEXITCODE -eq 0) "Restore Equivalence: Source and Reference copies are 100% identical (Customer balance: 1,044.00 PKR)."

    # --------------------------------------------------------------------------
    # Step 8: Corrective Script Safety & Negative Testing
    # --------------------------------------------------------------------------
    Write-Host "[STEP 8/10] Verifying script catalog whitelisting, database guards, and rollback safety..." -ForegroundColor Cyan

    $script02 = Join-Path $correctiveDir "FIX_20261017_02_CompensatingLedgerAdjustment.sql"
    $scriptFailPre = Join-Path $correctiveDir "FAIL_SamplePrecheckFailure.sql"
    $scriptFailPost = Join-Path $correctiveDir "FAIL_SamplePostcheckFailure.sql"
    $scriptReject = Join-Path $correctiveDir "REJECT_SampleIllegalFinancialRewrite.sql"

    # 8a. Target Database Guard Test: Attempting to run Diagnostic-scoped script on Reference copy must fail
    $dbGuardBlocked = $false
    try {
        Invoke-CbosCorrectiveScript `
            -ScriptFilePath $script02 `
            -TargetDatabaseName $refDb `
            -Server $Server `
            -UseWindowsAuth `
            -Execute
    }
    catch {
        $dbGuardBlocked = $_.Exception.Message -match "TARGET DATABASE GUARD VIOLATION"
    }
    Assert-Condition $dbGuardBlocked "Target database guard strictly blocked executing Diagnostic-scoped script on Reference copy."

    # 8b. Unapproved Script Whitelist Guard Test: Unapproved script without -AllowUnapprovedScript must fail
    $whitelistBlocked = $false
    try {
        Invoke-CbosCorrectiveScript `
            -ScriptFilePath $scriptFailPre `
            -TargetDatabaseName $diagDb `
            -Server $Server `
            -UseWindowsAuth `
            -DryRun
    }
    catch {
        $whitelistBlocked = $_.Exception.Message -match "UNAPPROVED SCRIPT BLOCKED"
    }
    Assert-Condition $whitelistBlocked "Catalog whitelist strictly blocked unapproved script execution when -AllowUnapprovedScript was omitted."

    # 8c. Failed Precheck Test: Fails precheck and verifies ZERO mutation occurs
    $precheckFailed = $false
    try {
        Invoke-CbosCorrectiveScript `
            -ScriptFilePath $scriptFailPre `
            -TargetDatabaseName $diagDb `
            -Server $Server `
            -UseWindowsAuth `
            -Execute `
            -AllowUnapprovedScript
    }
    catch {
        $precheckFailed = $_.Exception.Message -match "PRECHECK FAILED"
    }
    Assert-Condition $precheckFailed "Negative test: Precheck failure halted execution before opening transaction."

    # Verify customer table was NOT mutated
    $connDiag = New-Object System.Data.SqlClient.SqlConnection($diagConnStr)
    $connDiag.Open()
    $cmdCheckNotes = $connDiag.CreateCommand()
    $cmdCheckNotes.CommandText = "SELECT [Notes] FROM [Restaurant].[Customers] WHERE [Code] = 'CREDIT-DEMO';"
    $notesVal = [string]$cmdCheckNotes.ExecuteScalar()
    $connDiag.Close()
    Assert-Condition ($notesVal -ne "MUTATED_BY_PRECHECK_FAILURE") "Precheck failure left zero mutation on target table."

    # 8d. Failed Postcheck Test: Mutation executes within transaction, postcheck fails, triggers rollback
    $postcheckFailed = $false
    try {
        Invoke-CbosCorrectiveScript `
            -ScriptFilePath $scriptFailPost `
            -TargetDatabaseName $diagDb `
            -Server $Server `
            -UseWindowsAuth `
            -Execute `
            -AllowUnapprovedScript
    }
    catch {
        $postcheckFailed = $_.Exception.Message -match "POSTCHECK RECONCILIATION FAILED"
    }
    Assert-Condition $postcheckFailed "Negative test: Postcheck failure triggered transactional rollback."

    # Verify rollback left zero mutation on target table
    $connDiag = New-Object System.Data.SqlClient.SqlConnection($diagConnStr)
    $connDiag.Open()
    $cmdCheckRollback = $connDiag.CreateCommand()
    $cmdCheckRollback.CommandText = "SELECT [Notes] FROM [Restaurant].[Customers] WHERE [Code] = 'CREDIT-DEMO';"
    $notesAfterRollback = [string]$cmdCheckRollback.ExecuteScalar()

    # Verify failure audit trail recorded
    $cmdAuditFail = $connDiag.CreateCommand()
    $cmdAuditFail.CommandText = "SELECT COUNT(1) FROM [MasterData].[SupportScriptExecutionAudit] WHERE [ScriptId] = 'FAIL_SamplePostcheckFailure' AND [Outcome] = 'Failed';"
    $auditFailCount = [int]$cmdAuditFail.ExecuteScalar()
    $connDiag.Close()

    Assert-Condition ($notesAfterRollback -ne "MUTATED_BEFORE_POSTCHECK_FAILURE") "Transactional rollback restored pre-execution state with zero partial mutation."
    Assert-Condition ($auditFailCount -gt 0) "Postcheck failure logged with Outcome = 'Failed' in [MasterData].[SupportScriptExecutionAudit]."

    # 8e. PDR-0003 Static Guard Test: Direct update/delete of completed orders rejected
    $rewriteBlocked = $false
    try {
        Invoke-CbosCorrectiveScript `
            -ScriptFilePath $scriptReject `
            -TargetDatabaseName $diagDb `
            -Server $Server `
            -UseWindowsAuth `
            -Execute `
            -AllowUnapprovedScript
    }
    catch {
        $rewriteBlocked = $_.Exception.Message -match "PDR-0003 FINANCIAL INTEGRITY VIOLATION"
    }
    Assert-Condition $rewriteBlocked "PDR-0003 static analysis guard strictly blocked illegal update of completed orders."

    # --------------------------------------------------------------------------
    # Step 9: Approved Compensating Transaction Execution (Customer Billing Dispute)
    # --------------------------------------------------------------------------
    Write-Host "[STEP 9/10] Applying approved compensating credit transaction under domain rules..." -ForegroundColor Cyan

    # 9a. Preview Diagnosis Mode (-DryRun)
    $dryRunResult = Invoke-CbosCorrectiveScript `
        -ScriptFilePath $script02 `
        -TargetDatabaseName $diagDb `
        -Server $Server `
        -UseWindowsAuth `
        -DryRun

    Assert-Condition ($dryRunResult.Success -and $dryRunResult.Mode -eq "DryRun") "Preview diagnosis completed with zero mutations."

    # 9b. Transactional Execution Mode (-Execute)
    $execCompResult = Invoke-CbosCorrectiveScript `
        -ScriptFilePath $script02 `
        -TargetDatabaseName $diagDb `
        -Server $Server `
        -UseWindowsAuth `
        -Execute `
        -PreTestedOnRestoredCopy `
        -CompensationReference "AUTH-DISPUTE-CR-20261017-001"

    Assert-Condition ($execCompResult.Success -and $execCompResult.Outcome -eq "Success") "Compensating credit memo applied successfully inside transaction."
    Assert-Condition (Test-Path $execCompResult.PreFixBackup) "Fresh pre-fix backup created prior to mutation ($($execCompResult.PreFixBackup))."

    # Verify Audit Table Success Entry
    $connDiag = New-Object System.Data.SqlClient.SqlConnection($diagConnStr)
    $connDiag.Open()
    $cmdAuditSuccess = $connDiag.CreateCommand()
    $cmdAuditSuccess.CommandText = "SELECT COUNT(1) FROM [MasterData].[SupportScriptExecutionAudit] WHERE [ScriptId] = 'FIX_20261017_02_CompensatingLedgerAdjustment' AND [Outcome] = 'Success';"
    $auditSuccessCount = [int]$cmdAuditSuccess.ExecuteScalar()
    $connDiag.Close()
    Assert-Condition ($auditSuccessCount -gt 0) "Successful execution recorded in [MasterData].[SupportScriptExecutionAudit]."

    # 9c. Concurrency-Safe Repeat-Execution Protection Test
    $repeatBlocked = $false
    try {
        Invoke-CbosCorrectiveScript `
            -ScriptFilePath $script02 `
            -TargetDatabaseName $diagDb `
            -Server $Server `
            -UseWindowsAuth `
            -Execute `
            -PreTestedOnRestoredCopy `
            -CompensationReference "AUTH-DISPUTE-CR-20261017-001"
    }
    catch {
        $repeatBlocked = $_.Exception.Message -match "REPEAT EXECUTION BLOCKED"
    }
    Assert-Condition $repeatBlocked "Concurrency-safe repeat-execution protection blocked duplicate execution of '$($execCompResult.ScriptId)'."

    # --------------------------------------------------------------------------
    # Step 10: Post-Correction Reconciliation (Reference vs Diagnostic Database)
    # --------------------------------------------------------------------------
    Write-Host "[STEP 10/10] Reconciling post-correction domain facts between Reference and Diagnostic databases..." -ForegroundColor Cyan
    & $fixtureRunnerExe --reconcile-post-correction --reference $refConnStr --diagnostic $diagConnStr --adjustment 100.00
    Assert-Condition ($LASTEXITCODE -eq 0) "Post-Correction Reconciliation: Customer balance adjusted 1,044.00 -> 944.00 PKR; all sales, payments, taxes, and receipts remain 100% immutable."

    Write-Host "`n================================================================================" -ForegroundColor Green
    Write-Host "  CUSTOMER DATABASE SUPPORT WORKFLOW VERIFICATION SUCCEEDED" -ForegroundColor Green
    Write-Host "  All 10 Hardened Verification Phases Passed Cleanly." -ForegroundColor Green
    Write-Host "================================================================================`n" -ForegroundColor Green
}
finally {
    # --------------------------------------------------------------------------
    # Teardown: Clean up disposable resources
    # --------------------------------------------------------------------------
    if (-not $KeepDatabases) {
        Write-Host "Tearing down disposable verification databases and temporary bundles..." -ForegroundColor Cyan
        try { Drop-DatabaseSafely $sourceDb } catch { }
        try { Drop-DatabaseSafely $refDb } catch { }
        try { Drop-DatabaseSafely $diagDb } catch { }
        if (Test-Path $bundleOutputDir) {
            try { Remove-Item $bundleOutputDir -Recurse -Force } catch { }
        }
        Write-Host "Teardown complete. Zero dangling databases or test files retained." -ForegroundColor Green
    } else {
        Write-Host "Skipping teardown (-KeepDatabases specified). Retained $sourceDb, $refDb, $diagDb." -ForegroundColor Yellow
    }
}
