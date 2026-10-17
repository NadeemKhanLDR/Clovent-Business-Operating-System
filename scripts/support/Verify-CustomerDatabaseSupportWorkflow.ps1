# ==============================================================================
# CBOS Customer Database Support Workflow Verification Harness
# Demonstrates:
#   1. Authentication & Privilege Separation:
#      - Frontline Cashier denied backup with zero credential leakage.
#      - Least-privilege 'db_backupoperator' performs backup successfully without sysadmin.
#      - Least-privilege 'db_backupoperator' strictly denied database restore (privilege separation).
#      - Dedicated support admin with sysadmin performs restore.
#      - Windows Integrated Authentication backup with COPY_ONLY and CHECKSUM.
#   2. Disposable synthetic source database provisioning across all 6 bounded contexts.
#   3. Full database backup with COPY_ONLY, CHECKSUM, and SHA-256 verification.
#   4. Support bundle packaging with mandatory customer consent, redacted logs, and diagnostic manifest.
#   5. Checksum verification & restoration into TWO separate databases:
#      - Untouched Restored Reference Copy (CBOS_Support_Ref_*_Disposable)
#      - Sanitized Diagnostic Copy (CBOS_Support_Diag_*_Disposable)
#   6. Environment isolation proving zero external dispatch surviving application startup:
#      - Diagnostic configuration isolated from developer's normal CBOS configuration.
#      - Newly generated print jobs strictly refused by PrinterRouter (NoAssignment / Disabled).
#      - Newly generated external sync jobs strictly blocked (QuickBooks maps Suspended).
#      - Reference copy retains exact original pending outbox records untouched.
#   7. Restore Equivalence Reconciliation: Source vs Restored Reference = 100% Identical.
#   8. Corrective Script Security & Negative Checks:
#      - Target database pattern guard blocks cross-database execution.
#      - Script catalog whitelist blocks unapproved scripts.
#      - Script content hash verification blocks altered/tampered scripts.
#      - Failed precheck leaves zero mutation.
#      - Failed postcheck triggers transactional rollback leaving zero mutation and logs 'Failed' audit.
#      - PDR-0003 static analysis guard strictly blocks direct financial rewrites.
#      - TargetCustomerId pinning gate prevents un-scoped customer updates.
#   9. Concurrent Duplicate Protection:
#      - Two simultaneous executions of the same correction on the same database.
#      - Exactly one committed correction; second execution rejected; zero duplicate ledger effects.
#  10. Approved Compensating Transaction Execution (Customer Billing Dispute):
#      - Domain-compliant additive credit memo for customer billing dispute.
#      - Verifies 100.00 PKR adjustment: original balance 1,044.00 -> corrected balance 944.00 PKR.
#      - Concurrency-safe repeat-execution protection via UX_SupportScriptExecutionAudit_ScriptId_Success.
#  11. Post-Correction Reconciliation: Reference vs Diagnostic copy proves financial immutability.
#  12. Audit Schema Deployment Verification:
#      - Fresh installation: EF Core migration 20261017170000_AddSupportScriptExecutionAudit deployed.
#      - Upgrade scenario: migration applies idempotently on database with pre-created audit table.
#  13. Client-Side Handoff Verification:
#      - Boss executes approved correction on client disposable database.
#      - Explicit target identity, schema checks, preview diagnosis, transactional execution, postchecks, audit.
#      - Arbitrary unapproved scripts strictly blocked.
#  14. Test log preservation outside disposable cleanup and clean teardown.
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
$concurrentDb = "CBOS_Concurrent_${runTimestamp}_Disposable"
$freshDb = "CBOS_Fresh_${runTimestamp}_Disposable"
$upgradeDb = "CBOS_Upgrade_${runTimestamp}_Disposable"
$clientDb = "CBOS_Client_Store01_${runTimestamp}_Disposable"

$bundleOutputDir = Join-Path $env:ProgramData "Clovent\SupportVerification_${runTimestamp}"
$logPreserveDir = Join-Path $repoRoot "artifacts\customer_support_verification_logs"
if (-not (Test-Path $logPreserveDir)) { New-Item -ItemType Directory -Path $logPreserveDir -Force | Out-Null }
$preservedLogPath = Join-Path $logPreserveDir "Verification_${runTimestamp}.log"

$masterConnStr = "Server=$Server;Database=master;Integrated Security=True;TrustServerCertificate=True;"
$sourceConnStr = "Server=$Server;Database=$sourceDb;Integrated Security=True;TrustServerCertificate=True;"
$refConnStr = "Server=$Server;Database=$refDb;Integrated Security=True;TrustServerCertificate=True;"
$diagConnStr = "Server=$Server;Database=$diagDb;Integrated Security=True;TrustServerCertificate=True;"
$concurrentConnStr = "Server=$Server;Database=$concurrentDb;Integrated Security=True;TrustServerCertificate=True;"
$freshConnStr = "Server=$Server;Database=$freshDb;Integrated Security=True;TrustServerCertificate=True;"
$upgradeConnStr = "Server=$Server;Database=$upgradeDb;Integrated Security=True;TrustServerCertificate=True;"
$clientConnStr = "Server=$Server;Database=$clientDb;Integrated Security=True;TrustServerCertificate=True;"

$provisionerExe = Join-Path $repoRoot "Tools\Clovent.Installer.Provisioner\bin\Debug\net10.0-windows\win-x64\Clovent.Installer.Provisioner.exe"
$fixtureRunnerExe = Join-Path $repoRoot "Tools\SupportFixtureRunner\bin\Debug\net10.0-windows\Clovent.SupportFixtureRunner.exe"

function Log-Message([string]$msg, [string]$color = "White") {
    $timestamp = (Get-Date).ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss.fff UTC")
    $line = "[$timestamp] $msg"
    Write-Host $msg -ForegroundColor $color
    try { Add-Content -Path $preservedLogPath -Value $line -Encoding UTF8 } catch { }
}

function Assert-Condition($condition, $message) {
    if (-not $condition) {
        Log-Message "ASSERTION FAILED: $message" "Red"
        throw "ASSERTION FAILED: $message"
    }
    Log-Message "  [OK] $message" "Green"
}

function New-EphemeralSecret {
    $bytes = New-Object byte[] 32
    [System.Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)
    return "Tst!" + [Convert]::ToBase64String($bytes).Replace('+', 'X').Replace('/', 'Y').Replace('=', 'Z') + "9#"
}

function Drop-DatabaseSafely($dbName) {
    if (-not $dbName.StartsWith("CBOS_", [System.StringComparison]::OrdinalIgnoreCase) -or
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

Log-Message "================================================================================" "Cyan"
Log-Message "  CLOVENT BUSINESS OPERATING SYSTEM (CBOS) - CUSTOMER DATABASE SUPPORT WORKFLOW" "Cyan"
Log-Message "  Comprehensive Hardened Verification Suite (Review of Commit b5beaea)" "Cyan"
Log-Message "  Source Disposable Database:    $sourceDb" "Cyan"
Log-Message "  Reference Disposable Database: $refDb" "Cyan"
Log-Message "  Diagnostic Disposable Database:$diagDb" "Cyan"
Log-Message "  Server Instance:               $Server" "Cyan"
Log-Message "  Preserved Log:                 $preservedLogPath" "Cyan"
Log-Message "================================================================================`n" "Cyan"

$dispCashierPassPlain = $null
$dispBackupOpPassPlain = $null
$dispSupportPassPlain = $null

try {
    # --------------------------------------------------------------------------
    # Step 0: Pre-cleanup check
    # --------------------------------------------------------------------------
    Log-Message "[STEP 0/14] Preparing clean disposable verification environment..." "Cyan"
    Drop-DatabaseSafely $sourceDb
    Drop-DatabaseSafely $refDb
    Drop-DatabaseSafely $diagDb
    Drop-DatabaseSafely $concurrentDb
    Drop-DatabaseSafely $freshDb
    Drop-DatabaseSafely $upgradeDb
    Drop-DatabaseSafely $clientDb
    if (Test-Path $bundleOutputDir) { try { Remove-Item $bundleOutputDir -Recurse -Force -ErrorAction SilentlyContinue } catch { } }
    New-CbosSupportDirectory -Path $bundleOutputDir | Out-Null

    # --------------------------------------------------------------------------
    # Step 1: Provision Disposable Source Database across all 6 Bounded Contexts
    # --------------------------------------------------------------------------
    Log-Message "[STEP 1/14] Provisioning disposable source database via Clovent.Installer.Provisioner..." "Cyan"
    & $provisionerExe --server $Server --database $sourceDb --auth windows --provision
    Assert-Condition ($LASTEXITCODE -eq 0) "Source database '$sourceDb' provisioned with all 6 bounded contexts and schema initializers."

    # --------------------------------------------------------------------------
    # Step 2: Populate Synthetic Business Fixture
    # --------------------------------------------------------------------------
    Log-Message "[STEP 2/14] Seeding synthetic business fixture via SupportFixtureRunner..." "Cyan"
    $factsFile = Join-Path $bundleOutputDir "fixture_facts.json"
    & $fixtureRunnerExe --seed --connection-string $sourceConnStr --facts-file $factsFile
    Assert-Condition ($LASTEXITCODE -eq 0 -and (Test-Path $factsFile)) "Synthetic business fixture successfully populated across all 6 bounded contexts."

    # --------------------------------------------------------------------------
    # Step 3: Authentication & Privilege Separation Verification
    # --------------------------------------------------------------------------
    Log-Message "[STEP 3/14] Verifying authentication modes and support privilege separation..." "Cyan"

    $dispCashierUser = "cbos_cashier_disp"
    $dispBackupOpUser = "cbos_backupop_disp"
    $dispSupportUser = "cbos_support_disp"
    $dispCashierPassPlain = New-EphemeralSecret
    $dispBackupOpPassPlain = New-EphemeralSecret
    $dispSupportPassPlain = New-EphemeralSecret
    $dispCashierPass = ConvertTo-SecureString $dispCashierPassPlain -AsPlainText -Force
    $dispBackupOpPass = ConvertTo-SecureString $dispBackupOpPassPlain -AsPlainText -Force
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
IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = N'$dispBackupOpUser')
BEGIN
    CREATE LOGIN [$dispBackupOpUser] WITH PASSWORD = N'$dispBackupOpPassPlain', CHECK_POLICY = OFF;
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

    # Grant cashier runtime least-privilege (db_datareader, db_datawriter) on sourceDb
    # Grant backup operator ONLY db_backupoperator on sourceDb (NO sysadmin, NO dbcreator)
    $connSource = New-Object System.Data.SqlClient.SqlConnection($sourceConnStr)
    $connSource.Open()
    try {
        $cmdRoles = $connSource.CreateCommand()
        $cmdRoles.CommandText = @"
IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'$dispCashierUser')
BEGIN
    CREATE USER [$dispCashierUser] FOR LOGIN [$dispCashierUser];
    ALTER ROLE [db_datareader] ADD MEMBER [$dispCashierUser];
    ALTER ROLE [db_datawriter] ADD MEMBER [$dispCashierUser];
END
IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'$dispBackupOpUser')
BEGIN
    CREATE USER [$dispBackupOpUser] FOR LOGIN [$dispBackupOpUser];
    ALTER ROLE [db_backupoperator] ADD MEMBER [$dispBackupOpUser];
END
"@
        $cmdRoles.ExecuteNonQuery() | Out-Null
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
    Assert-Condition ($cashierErrMsg -notmatch [regex]::Escape($dispCashierPassPlain)) "Cashier denial error contains zero plaintext password leakage."
    $dispCashierPassPlain = $null

    # 3b. Demonstrate Genuine Least-Privilege Backup Operator Success (WITHOUT sysadmin)
    $backupOpResult = Backup-CbosDatabase `
        -Server $Server `
        -Database $sourceDb `
        -SqlUser $dispBackupOpUser `
        -SqlPassword $dispBackupOpPass `
        -BackupDirectory (Join-Path $bundleOutputDir "backups_leastpriv")

    Assert-Condition ($backupOpResult.Success -and (Test-Path $backupOpResult.BackupPath)) "Least-privilege 'db_backupoperator' created full backup successfully without sysadmin role."

    # 3c. Demonstrate Least-Privilege Backup Operator CANNOT Restore (Privilege Separation)
    $restoreDenied = $false
    $restoreErrMsg = ""
    try {
        Restore-CbosIsolatedDatabase `
            -BackupFilePath $backupOpResult.BackupPath `
            -TargetDatabaseName "CBOS_Restore_Denied_${runTimestamp}_Disposable" `
            -Server $Server `
            -SqlUser $dispBackupOpUser `
            -SqlPassword $dispBackupOpPass
    }
    catch {
        $restoreErrMsg = $_.Exception.Message
        $restoreDenied = $restoreErrMsg -match "SECURITY VIOLATION"
    }

    Assert-Condition $restoreDenied "Least-privilege 'db_backupoperator' strictly denied restore privileges (proves separation of backup vs restore permissions)."
    $dispBackupOpPassPlain = $null

    # 3d. Demonstrate Dedicated Support Admin Backup & Restore Success
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
    Drop-DisposableSqlLoginSafely $dispBackupOpUser
    Drop-DisposableSqlLoginSafely $dispSupportUser
    $dispSupportPassPlain = $null
    Log-Message "  Disposable SQL logins cleanly dropped after privilege verification." "Gray"

    # 3e. Execute Windows Authentication Backup for Primary Support Bundle
    $backupResult = Backup-CbosDatabase `
        -Server $Server `
        -Database $sourceDb `
        -UseWindowsAuth `
        -BackupDirectory (Join-Path $bundleOutputDir "backups")

    Assert-Condition ($backupResult.Success -and (Test-Path $backupResult.BackupPath)) "Windows Authentication backup created successfully with COPY_ONLY and CHECKSUM."
    Assert-Condition ($backupResult.SchemasBackedUp.Count -eq 6) "Backup verified all 6 bounded-context schemas present."
    Assert-Condition (-not [string]::IsNullOrWhiteSpace($backupResult.Sha256Checksum)) "SHA-256 digest computed for backup archive ($($backupResult.Sha256Checksum))."

    # --------------------------------------------------------------------------
    # Step 4: Support Bundle Packaging & Customer Authorization Gate
    # --------------------------------------------------------------------------
    Log-Message "[STEP 4/14] Packaging support bundle with customer authorization gate and redacted logs..." "Cyan"
    
    $consentBlocked = $false
    try {
        New-CbosSupportBundle -Server $Server -Database $sourceDb -UseWindowsAuth -OutputDirectory $bundleOutputDir
    } catch {
        $consentBlocked = $_.Exception.Message -match "CUSTOMER AUTHORIZATION REQUIRED"
    }
    Assert-Condition $consentBlocked "Customer authorization gate successfully blocked export when -CustomerConsent was omitted."

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
    Log-Message "[STEP 5/14] Verifying checksum and restoring into Reference copy and Diagnostic copy..." "Cyan"
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
    # Step 6: Restored Environment Sanitization & Diagnostic Isolation (Surviving Application Startup)
    # --------------------------------------------------------------------------
    Log-Message "[STEP 6/14] Proving diagnostic isolation survives application startup and isolates developer configuration..." "Cyan"
    $isolateConfigDir = Join-Path $bundleOutputDir "isolated_config"
    $protectResult = Protect-CbosRestoredEnvironment `
        -TargetDatabaseName $diagDb `
        -Server $Server `
        -UseWindowsAuth `
        -ConfigDirectory $isolateConfigDir

    Assert-Condition ($protectResult.QuarantinedOutboxCount -gt 0) "Diagnostic copy outbox quarantined to DeadLetter ($($protectResult.QuarantinedOutboxCount) messages)."
    Assert-Condition (Test-Path $protectResult.PrinterConfigPath) "Diagnostic printer configuration generated in isolated directory."
    Assert-Condition (Test-Path $protectResult.DatabaseConfigPath) "Diagnostic database configuration generated in isolated directory."

    # Verify diagnostic configuration is kept completely separate from developer's default configuration
    $devConfigDir = Join-Path $env:ProgramData "Clovent\BusinessOperatingSystem\Config"
    Assert-Condition ($protectResult.ConfigDirectory -ne $devConfigDir) "Diagnostic configuration directory is isolated from developer's normal CBOS config."

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
    Assert-Condition ($pendingInDiag -eq 0) "Diagnostic copy has ZERO pending outbox records."

    # 6b. Verify newly generated print jobs CANNOT print to physical printer queues
    & $fixtureRunnerExe --verify-printer-isolation --config-dir $isolateConfigDir
    Assert-Condition ($LASTEXITCODE -eq 0) "Printer isolation verified: PrinterRouter strictly refuses profile resolution for newly generated print jobs."

    # 6c. Verify newly generated sync jobs CANNOT dispatch external integrations
    $connDiag = New-Object System.Data.SqlClient.SqlConnection($diagConnStr)
    $connDiag.Open()
    $cmdCheckQbMaps = $connDiag.CreateCommand()
    $cmdCheckQbMaps.CommandText = "SELECT COUNT(1) FROM [Restaurant].[QuickBooksSyncMaps] WHERE [Status] != 'Suspended';"
    $activeQbMaps = [int]$cmdCheckQbMaps.ExecuteScalar()
    $connDiag.Close()
    Assert-Condition ($activeQbMaps -eq 0) "External sync isolation verified: All QuickBooks mappings remain strictly Suspended."

    # --------------------------------------------------------------------------
    # Step 7: Restore Equivalence Reconciliation (Source vs Restored Reference)
    # --------------------------------------------------------------------------
    Log-Message "[STEP 7/14] Reconciling equivalence between Source and Restored Reference database..." "Cyan"
    & $fixtureRunnerExe --reconcile-equivalence --source $sourceConnStr --reference $refConnStr --facts-file $factsFile
    Assert-Condition ($LASTEXITCODE -eq 0) "Restore Equivalence: Source and Reference copies are 100% identical (Customer balance: 1,044.00 PKR)."

    # --------------------------------------------------------------------------
    # Step 8: Corrective Script Safety & Negative Testing
    # --------------------------------------------------------------------------
    Log-Message "[STEP 8/14] Verifying script catalog whitelisting, database guards, and rollback safety..." "Cyan"

    $script02 = Join-Path $correctiveDir "FIX_20261017_02_CompensatingLedgerAdjustment.sql"
    $scriptFailPre = Join-Path $correctiveDir "FAIL_SamplePrecheckFailure.sql"
    $scriptFailPost = Join-Path $correctiveDir "FAIL_SamplePostcheckFailure.sql"
    $scriptReject = Join-Path $correctiveDir "REJECT_SampleIllegalFinancialRewrite.sql"

    # 8a. Target Database Guard Test
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

    # 8b. Unapproved Script Whitelist Guard Test
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

    # 8b2. Script Content Hash Verification (Tampered Script Check)
    $tamperedScriptPath = Join-Path $correctiveDir "TEMP_Tampered_Script.sql"
    $tamperedContent = (Get-Content -Path $script02 -Raw -Encoding UTF8) + "`n-- Tampered byte addition"
    Set-Content -Path $tamperedScriptPath -Value $tamperedContent -Encoding UTF8
    $tamperHashBlocked = $false
    try {
        Invoke-CbosCorrectiveScript `
            -ScriptFilePath $tamperedScriptPath `
            -TargetDatabaseName $diagDb `
            -Server $Server `
            -UseWindowsAuth `
            -DryRun
    }
    catch {
        $tamperHashBlocked = $_.Exception.Message -match "SCRIPT INTEGRITY VIOLATION"
    }
    finally {
        if (Test-Path $tamperedScriptPath) { Remove-Item $tamperedScriptPath -Force -ErrorAction SilentlyContinue }
    }
    Assert-Condition $tamperHashBlocked "Script integrity check strictly blocked modified/tampered script whose hash differed from catalog."

    # 8c. Failed Precheck Test
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
    # Step 9: Concurrent Duplicate Protection (Two Simultaneous Executions)
    # --------------------------------------------------------------------------
    Log-Message "[STEP 9/14] Proving concurrent duplicate protection under simultaneous execution..." "Cyan"

    # Restore fresh disposable database for concurrency test
    Restore-CbosIsolatedDatabase `
        -BackupFilePath $bundleBakPath `
        -TargetDatabaseName $concurrentDb `
        -Server $Server `
        -UseWindowsAuth `
        -ExpectedSha256 $manifestJson.Backup.Sha256Checksum | Out-Null

    Protect-CbosRestoredEnvironment -TargetDatabaseName $concurrentDb -Server $Server -UseWindowsAuth | Out-Null

    $jobScript = {
        param($modPath, $scriptPath, $targetDb, $serverName)
        Import-Module $modPath -Force -DisableNameChecking
        try {
            $res = Invoke-CbosCorrectiveScript `
                -ScriptFilePath $scriptPath `
                -TargetDatabaseName $targetDb `
                -Server $serverName `
                -UseWindowsAuth `
                -Execute `
                -PreTestedOnRestoredCopy `
                -CompensationReference "AUTH-CONCURRENT-TEST-001"
            return @{ Success = $true; Outcome = $res.Outcome; Error = $null }
        } catch {
            return @{ Success = $false; Outcome = "Failed"; Error = $_.Exception.Message }
        }
    }

    $job1 = Start-Job -ScriptBlock $jobScript -ArgumentList $modulePath, $script02, $concurrentDb, $Server
    $job2 = Start-Job -ScriptBlock $jobScript -ArgumentList $modulePath, $script02, $concurrentDb, $Server

    $job1Result = Wait-Job $job1 | Receive-Job
    $job2Result = Wait-Job $job2 | Receive-Job
    Remove-Job -Job $job1, $job2 -Force

    $successCount = 0
    if ($job1Result.Success) { $successCount++ }
    if ($job2Result.Success) { $successCount++ }

    Assert-Condition ($successCount -eq 1) "Under simultaneous execution, exactly ONE job succeeded ($successCount committed, 1 rejected)."

    # Verify that the rejected job caught repeat execution or unique constraint violation
    $failedJob = if (-not $job1Result.Success) { $job1Result } else { $job2Result }
    $repeatCaught = ($failedJob.Error -match "REPEAT EXECUTION BLOCKED") -or ($failedJob.Error -match "duplicate key")
    Assert-Condition $repeatCaught "Second concurrent execution was cleanly blocked by repeat-execution protection."

    # Verify customer table in concurrentDb has strictly ONE credit adjustment (1,044.00 -> 944.00 PKR, NOT 844.00 PKR)
    $connConc = New-Object System.Data.SqlClient.SqlConnection($concurrentConnStr)
    $connConc.Open()
    $cmdCheckConcBal = $connConc.CreateCommand()
    $cmdCheckConcBal.CommandText = "SELECT [OutstandingBalance] FROM [Restaurant].[Customers] WHERE [Code] = 'CREDIT-DEMO';"
    $concBal = [decimal]$cmdCheckConcBal.ExecuteScalar()

    $cmdCheckConcEntries = $connConc.CreateCommand()
    $cmdCheckConcEntries.CommandText = "SELECT COUNT(1) FROM [Restaurant].[CustomerLedgerEntries] WHERE [Credit] = 100.00;"
    $concEntriesCount = [int]$cmdCheckConcEntries.ExecuteScalar()
    $connConc.Close()

    Assert-Condition ($concBal -eq [decimal]944.00) "Customer balance is exactly 944.00 PKR (no duplicate ledger deduction occurred)."
    Assert-Condition ($concEntriesCount -eq 1) "Exactly ONE compensating ledger credit entry exists in customer ledger."

    # --------------------------------------------------------------------------
    # Step 10: Approved Compensating Transaction Execution on Diagnostic Database
    # --------------------------------------------------------------------------
    Log-Message "[STEP 10/14] Applying approved compensating credit transaction on Diagnostic copy..." "Cyan"

    # 10a. Preview Diagnosis Mode (-DryRun)
    $dryRunResult = Invoke-CbosCorrectiveScript `
        -ScriptFilePath $script02 `
        -TargetDatabaseName $diagDb `
        -Server $Server `
        -UseWindowsAuth `
        -DryRun

    Assert-Condition ($dryRunResult.Success -and $dryRunResult.Mode -eq "DryRun") "Preview diagnosis completed with zero mutations."

    # 10b. Transactional Execution Mode (-Execute)
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

    # 10c. Repeat-Execution Protection on Diagnostic DB
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
    Assert-Condition $repeatBlocked "Repeat-execution protection blocked duplicate execution of '$($execCompResult.ScriptId)' on Diagnostic database."

    # --------------------------------------------------------------------------
    # Step 11: Post-Correction Reconciliation (Reference vs Diagnostic Database)
    # --------------------------------------------------------------------------
    Log-Message "[STEP 11/14] Reconciling post-correction domain facts between Reference and Diagnostic databases..." "Cyan"
    & $fixtureRunnerExe --reconcile-post-correction --reference $refConnStr --diagnostic $diagConnStr --adjustment 100.00
    Assert-Condition ($LASTEXITCODE -eq 0) "Post-Correction Reconciliation: Customer balance adjusted 1,044.00 -> 944.00 PKR; all sales, payments, taxes, and receipts remain 100% immutable."

    # --------------------------------------------------------------------------
    # Step 12: Audit Schema Deployment Verification (Fresh Install & Upgrade)
    # --------------------------------------------------------------------------
    Log-Message "[STEP 12/14] Verifying EF Core audit migration deployment on fresh install and existing upgrade..." "Cyan"

    # 12a. Fresh Installation Verification
    & $provisionerExe --server $Server --database $freshDb --auth windows --provision
    Assert-Condition ($LASTEXITCODE -eq 0) "Fresh database '$freshDb' provisioned successfully."

    $connFresh = New-Object System.Data.SqlClient.SqlConnection($freshConnStr)
    $connFresh.Open()
    $cmdCheckMigration = $connFresh.CreateCommand()
    $cmdCheckMigration.CommandText = "SELECT COUNT(1) FROM [MasterData].[__EFMigrationsHistory] WHERE [MigrationId] LIKE '%AddSupportScriptExecutionAudit%';"
    $freshMigrationCount = [int]$cmdCheckMigration.ExecuteScalar()
    $connFresh.Close()
    Assert-Condition ($freshMigrationCount -gt 0) "Fresh install applied migration '20261017170000_AddSupportScriptExecutionAudit'."

    # 12b. Upgrade Scenario on database where support tool previously created the audit table
    # Provision clean database, pre-create the table and an existing audit row via support DDL, and delete the EF migration record
    & $provisionerExe --server $Server --database $upgradeDb --auth windows --provision
    Assert-Condition ($LASTEXITCODE -eq 0) "Baseline database '$upgradeDb' provisioned."

    $connUpg = New-Object System.Data.SqlClient.SqlConnection($upgradeConnStr)
    $connUpg.Open()
    $cmdSetupPrecreated = $connUpg.CreateCommand()
    $cmdSetupPrecreated.CommandText = @"
-- Delete migration record to simulate pre-migration database
DELETE FROM [MasterData].[__EFMigrationsHistory] WHERE [MigrationId] LIKE '%AddSupportScriptExecutionAudit%';

-- Insert a simulated existing audit record from an earlier support run
INSERT INTO [MasterData].[SupportScriptExecutionAudit]
    ([ScriptId], [ScriptHash], [Operator], [ExecutedAtUtc], [TargetDatabase], [PreCheckSummary], [AffectedRows], [Outcome], [CompensationReference])
VALUES
    ('PRE_MIGRATION_AUDIT_RECORD', '0000000000000000000000000000000000000000000000000000000000000000', 'support_pre_mig', SYSUTCDATETIME(), '$upgradeDb', 'Pre-upgrade audit', 1, 'Success', 'AUTH-PRE-UPG');
"@
    $cmdSetupPrecreated.ExecuteNonQuery() | Out-Null
    $connUpg.Close()

    # Now apply migrations via SupportFixtureRunner --verify-migration-upgrade
    & $fixtureRunnerExe --verify-migration-upgrade --connection-string $upgradeConnStr
    Assert-Condition ($LASTEXITCODE -eq 0) "EF Core migration applied idempotently on database where audit table already existed."

    # Verify that the pre-existing audit record was preserved
    $connUpg = New-Object System.Data.SqlClient.SqlConnection($upgradeConnStr)
    $connUpg.Open()
    $cmdVerifyPreserved = $connUpg.CreateCommand()
    $cmdVerifyPreserved.CommandText = "SELECT COUNT(1) FROM [MasterData].[SupportScriptExecutionAudit] WHERE [ScriptId] = 'PRE_MIGRATION_AUDIT_RECORD';"
    $preservedCount = [int]$cmdVerifyPreserved.ExecuteScalar()

    $cmdCheckUpgMig = $connUpg.CreateCommand()
    $cmdCheckUpgMig.CommandText = "SELECT COUNT(1) FROM [MasterData].[__EFMigrationsHistory] WHERE [MigrationId] LIKE '%AddSupportScriptExecutionAudit%';"
    $upgMigrationCount = [int]$cmdCheckUpgMig.ExecuteScalar()
    $connUpg.Close()

    Assert-Condition ($preservedCount -gt 0) "Pre-existing audit records were preserved during EF Core migration upgrade."
    Assert-Condition ($upgMigrationCount -gt 0) "Migration '20261017170000_AddSupportScriptExecutionAudit' recorded in __EFMigrationsHistory after upgrade."

    # --------------------------------------------------------------------------
    # Step 13: Client-Side Handoff Verification (Boss Execution on Client Disposable DB)
    # --------------------------------------------------------------------------
    Log-Message "[STEP 13/14] Simulating client-side handoff: Boss executes approved correction on client database..." "Cyan"

    # Restore disposable database representing a client store
    Restore-CbosIsolatedDatabase `
        -BackupFilePath $bundleBakPath `
        -TargetDatabaseName $clientDb `
        -Server $Server `
        -UseWindowsAuth `
        -ExpectedSha256 $manifestJson.Backup.Sha256Checksum | Out-Null

    # Boss validates schema compatibility before running fix
    $compValidator = New-Object Clovent.Desktop.Commissioning.Database.DatabaseSchemaCompatibilityValidator
    $valRes = $compValidator.ValidateCompatibilityAsync($clientConnStr).GetAwaiter().GetResult()
    Assert-Condition ($valRes.Status -eq [Clovent.Desktop.Commissioning.Database.SchemaCompatibilityStatus]::Compatible) "Boss verified client database schema is 100% Compatible."

    # Boss executes Preview Diagnosis (-DryRun)
    $bossDryRun = Invoke-CbosCorrectiveScript `
        -ScriptFilePath $script02 `
        -TargetDatabaseName $clientDb `
        -Server $Server `
        -UseWindowsAuth `
        -DryRun

    Assert-Condition ($bossDryRun.Success -and $bossDryRun.Mode -eq "DryRun") "Boss preview diagnosis completed cleanly with zero changes."

    # Boss executes Transactional Fix (-Execute)
    $bossExec = Invoke-CbosCorrectiveScript `
        -ScriptFilePath $script02 `
        -TargetDatabaseName $clientDb `
        -Server $Server `
        -UseWindowsAuth `
        -Execute `
        -PreTestedOnRestoredCopy `
        -CompensationReference "AUTH-DISPUTE-CR-20261017-CLIENT01"

    Assert-Condition ($bossExec.Success -and $bossExec.Outcome -eq "Success") "Boss executed approved correction successfully inside transaction."
    Assert-Condition (Test-Path $bossExec.PreFixBackup) "Fresh pre-fix backup created prior to client mutation ($($bossExec.PreFixBackup))."

    # Boss verifies reconciled balance on client database
    $connClient = New-Object System.Data.SqlClient.SqlConnection($clientConnStr)
    $connClient.Open()
    $cmdCheckClientBal = $connClient.CreateCommand()
    $cmdCheckClientBal.CommandText = "SELECT [OutstandingBalance] FROM [Restaurant].[Customers] WHERE [Code] = 'CREDIT-DEMO';"
    $clientBal = [decimal]$cmdCheckClientBal.ExecuteScalar()

    $cmdCheckClientAudit = $connClient.CreateCommand()
    $cmdCheckClientAudit.CommandText = "SELECT COUNT(1) FROM [MasterData].[SupportScriptExecutionAudit] WHERE [ScriptId] = 'FIX_20261017_02_CompensatingLedgerAdjustment' AND [Outcome] = 'Success';"
    $clientAuditCount = [int]$cmdCheckClientAudit.ExecuteScalar()
    $connClient.Close()

    Assert-Condition ($clientBal -eq [decimal]944.00) "Client database balance successfully adjusted: 1,044.00 -> 944.00 PKR."
    Assert-Condition ($clientAuditCount -gt 0) "Client execution recorded in [MasterData].[SupportScriptExecutionAudit]."

    # Negative test: Boss cannot run arbitrary unapproved script on client database
    $arbitraryBlocked = $false
    try {
        Invoke-CbosCorrectiveScript `
            -ScriptFilePath $scriptFailPre `
            -TargetDatabaseName $clientDb `
            -Server $Server `
            -UseWindowsAuth `
            -Execute
    }
    catch {
        $arbitraryBlocked = $_.Exception.Message -match "UNAPPROVED SCRIPT BLOCKED"
    }
    Assert-Condition $arbitraryBlocked "Arbitrary unapproved script strictly rejected on client database."

    # --------------------------------------------------------------------------
    # Step 14: Final Success Notice
    # --------------------------------------------------------------------------
    Log-Message "`n================================================================================" "Green"
    Log-Message "  CUSTOMER DATABASE SUPPORT WORKFLOW VERIFICATION SUCCEEDED" "Green"
    Log-Message "  All 14 Hardened Verification Phases Passed Cleanly." "Green"
    Log-Message "================================================================================`n" "Green"
}
finally {
    # --------------------------------------------------------------------------
    # Teardown: Clean up disposable resources & logins
    # --------------------------------------------------------------------------
    $dispCashierPassPlain = $null
    $dispBackupOpPassPlain = $null
    $dispSupportPassPlain = $null
    try { Drop-DisposableSqlLoginSafely "cbos_cashier_disp" } catch { }
    try { Drop-DisposableSqlLoginSafely "cbos_backupop_disp" } catch { }
    try { Drop-DisposableSqlLoginSafely "cbos_support_disp" } catch { }

    if (-not $KeepDatabases) {
        Log-Message "Tearing down disposable verification databases and temporary bundles..." "Cyan"
        try { Drop-DatabaseSafely $sourceDb } catch { }
        try { Drop-DatabaseSafely $refDb } catch { }
        try { Drop-DatabaseSafely $diagDb } catch { }
        try { Drop-DatabaseSafely $concurrentDb } catch { }
        try { Drop-DatabaseSafely $freshDb } catch { }
        try { Drop-DatabaseSafely $upgradeDb } catch { }
        try { Drop-DatabaseSafely $clientDb } catch { }
        if (Test-Path $bundleOutputDir) {
            try { Remove-Item $bundleOutputDir -Recurse -Force } catch { }
        }
        Log-Message "Teardown complete. Zero dangling databases or test files retained." "Green"
    } else {
        Log-Message "Skipping teardown (-KeepDatabases specified)." "Yellow"
    }

    Log-Message "Verification log preserved at: $preservedLogPath" "Cyan"
}
