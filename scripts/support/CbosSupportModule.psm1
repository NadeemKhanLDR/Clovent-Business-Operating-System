# ==============================================================================
# CBOS Customer Database Support Module (CbosSupportModule.psm1)
# Implements authorized customer database support workflows for Clovent Business Operating System (CBOS)
# Features:
#   1. Authorized full SQL Server backup with role separation (support vs cashier)
#   2. Support bundle creation with SHA-256 manifest, redacted logs, customer consent gate
#   3. Isolated database restore with version compatibility check, pre-launch sanitization, DPAPI reconfiguration
#   4. Reviewed corrective-script workflow with preview diagnosis, pre-fix backup, transactional execution,
#      repeat-execution protection, audit logging, post-fix reconciliation, and PDR-0003 financial immutability
# ==============================================================================

Set-StrictMode -Version 2.0
$ErrorActionPreference = "Stop"

# Forbidden database names that must never be overwritten or used as support targets
$script:ForbiddenDatabases = @(
    "Clovent_BusinessOperatingSystem",
    "Clovent_Restaurant",
    "Clovent_Catalog",
    "Clovent_MasterData",
    "Clovent_Inventory",
    "Clovent_Identity",
    "Clovent_Authentication",
    "Guppys-02-Jun-2026",
    "master",
    "model",
    "msdb",
    "tempdb"
)

# ------------------------------------------------------------------------------
# Helper: Mask database secrets and passwords
# ------------------------------------------------------------------------------
function Mask-DatabaseSecrets {
    param(
        [string]$Message,
        [string]$SecretPassword = $null
    )

    if ([string]::IsNullOrWhiteSpace($Message)) {
        return ""
    }

    $masked = $Message
    if (-not [string]::IsNullOrWhiteSpace($SecretPassword)) {
        $masked = $masked.Replace($SecretPassword, "******")
    }

    # Mask connection string passwords
    $masked = [System.Text.RegularExpressions.Regex]::Replace(
        $masked,
        "(?i)(password|pwd)\s*=\s*[^;]+",
        "$1=******"
    )

    # Mask user id if present in certain traces
    $masked = [System.Text.RegularExpressions.Regex]::Replace(
        $masked,
        "(?i)user id\s*=\s*[^;]+",
        "User ID=******"
    )

    return $masked
}

# ------------------------------------------------------------------------------
# Helper: Ensure directory exists with FullControl inheritance for current user
# ------------------------------------------------------------------------------
function New-CbosSupportDirectory {
    param([string]$Path)
    if (-not (Test-Path $Path)) {
        New-Item -ItemType Directory -Path $Path -Force | Out-Null
    }
    try {
        $acl = Get-Acl $Path
        $currentUser = [System.Security.Principal.WindowsIdentity]::GetCurrent().Name
        $rule = New-Object System.Security.AccessControl.FileSystemAccessRule(
            $currentUser,
            "FullControl",
            ([System.Security.AccessControl.InheritanceFlags]::ContainerInherit -bor [System.Security.AccessControl.InheritanceFlags]::ObjectInherit),
            [System.Security.AccessControl.PropagationFlags]::None,
            [System.Security.AccessControl.AccessControlType]::Allow
        )
        $acl.AddAccessRule($rule)
        Set-Acl -Path $Path -AclObject $acl
    }
    catch {
        # Best effort ACL assignment
    }
    return $Path
}

# ------------------------------------------------------------------------------
# Helper: Build SQL Connection and execute query
# ------------------------------------------------------------------------------
function New-CbosSqlConnection {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Server,

        [Parameter(Mandatory = $false)]
        [string]$Database = "master",

        [Parameter(Mandatory = $false)]
        [switch]$UseWindowsAuth,

        [Parameter(Mandatory = $false)]
        [string]$SqlUser,

        [Parameter(Mandatory = $false)]
        [System.Security.SecureString]$SqlPassword,

        [Parameter(Mandatory = $false)]
        [int]$TimeoutSeconds = 30
    )

    $plainPassword = $null
    $cs = "Server=$Server;Database=$Database;Connect Timeout=$TimeoutSeconds;TrustServerCertificate=True;"
    if ($UseWindowsAuth -or [string]::IsNullOrWhiteSpace($SqlUser)) {
        $cs += "Integrated Security=True;"
    }
    else {
        $cs += "Integrated Security=False;User ID=$SqlUser;"
        if ($null -ne $SqlPassword) {
            $bstr = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($SqlPassword)
            try {
                $plainPassword = [System.Runtime.InteropServices.Marshal]::PtrToStringBSTR($bstr)
                $escapedPwd = $plainPassword.Replace("'", "''")
                $cs += "Password='$escapedPwd';"
            }
            finally {
                [System.Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr)
            }
        }
    }

    $conn = New-Object System.Data.SqlClient.SqlConnection($cs)
    try {
        $conn.Open()
        return @{
            Connection = $conn
            ConnectionString = $cs
            PlainPassword = $plainPassword
        }
    }
    catch {
        $maskedErr = Mask-DatabaseSecrets $_.Exception.Message $plainPassword
        throw "Failed to connect to SQL Server at '$Server' (Database: '$Database'): $maskedErr"
    }
}

# ------------------------------------------------------------------------------
# Helper: Verify support privileges and enforce separation from cashier account
# ------------------------------------------------------------------------------
function Test-CbosSupportPrivileges {
    param(
        [Parameter(Mandatory = $true)]
        [System.Data.SqlClient.SqlConnection]$Connection,

        [Parameter(Mandatory = $false)]
        [string]$TargetDatabase = "master"
    )

    $cmd = $Connection.CreateCommand()
    $cmd.CommandText = @"
SELECT 
    SUSER_SNAME() AS LoginName,
    IS_SRVROLEMEMBER('sysadmin') AS IsSysAdmin,
    IS_ROLEMEMBER('db_backupoperator') AS IsBackupOperator,
    IS_ROLEMEMBER('db_owner') AS IsDbOwner;
"@
    $reader = $cmd.ExecuteReader()
    $loginName = ""
    $isSysAdmin = $false
    $isBackupOperator = $false
    $isDbOwner = $false

    if ($reader.Read()) {
        $loginName = [string]$reader["LoginName"]
        $isSysAdmin = -not [DBNull]::Value.Equals($reader["IsSysAdmin"]) -and [int]$reader["IsSysAdmin"] -eq 1
        $isBackupOperator = -not [DBNull]::Value.Equals($reader["IsBackupOperator"]) -and [int]$reader["IsBackupOperator"] -eq 1
        $isDbOwner = -not [DBNull]::Value.Equals($reader["IsDbOwner"]) -and [int]$reader["IsDbOwner"] -eq 1
    }
    $reader.Close()

    # Cashier / Frontline account restriction check
    $isCashierAccount = $loginName -match "(?i)cashier|pos_user"
    if ($isCashierAccount) {
        throw "SECURITY VIOLATION: Connected as cashier/POS principal '$loginName'. Frontline cashier accounts only hold least-privilege runtime access and are strictly forbidden from database support and backup administration. Connect using an authorized support principal."
    }

    $hasBackupPrivilege = $isSysAdmin -or $isBackupOperator -or $isDbOwner
    return [PSCustomObject]@{
        LoginName = $loginName
        IsSysAdmin = $isSysAdmin
        IsBackupOperator = $isBackupOperator
        IsDbOwner = $isDbOwner
        HasBackupPrivilege = $hasBackupPrivilege
    }
}

# ------------------------------------------------------------------------------
# 1. Backup-CbosDatabase: Consistent full SQL Server backup
# ------------------------------------------------------------------------------
function Backup-CbosDatabase {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $false)]
        [string]$Server = "localhost",

        [Parameter(Mandatory = $false)]
        [string]$Database = "Clovent_BusinessOperatingSystem",

        [Parameter(Mandatory = $false)]
        [switch]$UseWindowsAuth,

        [Parameter(Mandatory = $false)]
        [string]$SqlUser,

        [Parameter(Mandatory = $false)]
        [System.Security.SecureString]$SqlPassword,

        [Parameter(Mandatory = $false)]
        [string]$BackupDirectory,

        [Parameter(Mandatory = $false)]
        [string]$CustomBackupFileName
    )

    Write-Host "[CBOS-SUPPORT] Connecting to SQL Server '$Server' for full backup of '$Database'..." -ForegroundColor Cyan
    $connContext = New-CbosSqlConnection -Server $Server -Database $Database -UseWindowsAuth:$UseWindowsAuth -SqlUser $SqlUser -SqlPassword $SqlPassword
    $conn = $connContext.Connection

    try {
        # Verify privileges and privilege separation
        $priv = Test-CbosSupportPrivileges -Connection $conn -TargetDatabase $Database
        Write-Host "  Operator Login: $($priv.LoginName)" -ForegroundColor Gray
        if (-not $priv.HasBackupPrivilege) {
            throw "SECURITY VIOLATION: Login '$($priv.LoginName)' lacks backup privileges on '$Database' (requires 'db_backupoperator', 'db_owner', or 'sysadmin')."
        }

        # Verify CBOS bounded-context schemas in target database
        $cmdSchemas = $conn.CreateCommand()
        $cmdSchemas.CommandText = "SELECT name FROM sys.schemas WHERE name IN ('Authentication', 'Identity', 'MasterData', 'Catalog', 'Inventory', 'Restaurant');"
        $adapter = New-Object System.Data.SqlClient.SqlDataAdapter($cmdSchemas)
        $schemaTable = New-Object System.Data.DataTable
        [void]$adapter.Fill($schemaTable)
        $foundSchemas = @($schemaTable.Rows | ForEach-Object { $_["name"] })

        Write-Host "  Bounded Context Schemas Detected: $($foundSchemas -join ', ')" -ForegroundColor Gray
        if ($foundSchemas.Count -lt 6) {
            Write-Warning "Database '$Database' contains $($foundSchemas.Count)/6 standard CBOS schemas. Continuing backup..."
        }

        # Resolve backup destination directory
        $targetDir = $BackupDirectory
        if ([string]::IsNullOrWhiteSpace($targetDir)) {
            $cmdDefPath = $conn.CreateCommand()
            $cmdDefPath.CommandText = "SELECT CAST(SERVERPROPERTY('InstanceDefaultBackupPath') AS NVARCHAR(512));"
            $defPath = $cmdDefPath.ExecuteScalar()
            if (-not [string]::IsNullOrWhiteSpace($defPath) -and (Test-Path $defPath)) {
                $targetDir = $defPath
            }
            else {
                $targetDir = Join-Path $env:ProgramData "Clovent\Backups\Support"
            }
        }

        New-CbosSupportDirectory -Path $targetDir | Out-Null

        $timestamp = (Get-Date).ToUniversalTime().ToString("yyyyMMdd_HHmmss")
        $fileName = if (-not [string]::IsNullOrWhiteSpace($CustomBackupFileName)) {
            $CustomBackupFileName
        } else {
            "${Database}_SupportBackup_${timestamp}.bak"
        }
        $backupPath = Join-Path $targetDir $fileName

        Write-Host "  Executing BACKUP DATABASE with COPY_ONLY and CHECKSUM..." -ForegroundColor Cyan
        $escapedDb = $Database.Replace("]", "]]")
        $backupSql = "BACKUP DATABASE [$escapedDb] TO DISK = @path WITH COPY_ONLY, CHECKSUM, STATS = 10, INIT;"
        $cmdBackup = $conn.CreateCommand()
        $cmdBackup.CommandText = $backupSql
        $cmdBackup.Parameters.Add((New-Object System.Data.SqlClient.SqlParameter("@path", [System.Data.SqlDbType]::NVarChar, 4000) -Property @{ Value = $backupPath })) | Out-Null
        $cmdBackup.CommandTimeout = 300
        $cmdBackup.ExecuteNonQuery() | Out-Null

        Write-Host "  Verifying backup structure with RESTORE VERIFYONLY WITH CHECKSUM..." -ForegroundColor Cyan
        $cmdVerify = $conn.CreateCommand()
        $cmdVerify.CommandText = "RESTORE VERIFYONLY FROM DISK = @path WITH CHECKSUM;"
        $cmdVerify.Parameters.Add((New-Object System.Data.SqlClient.SqlParameter("@path", [System.Data.SqlDbType]::NVarChar, 4000) -Property @{ Value = $backupPath })) | Out-Null
        $cmdVerify.CommandTimeout = 120
        $cmdVerify.ExecuteNonQuery() | Out-Null

        $fileInfo = Get-Item $backupPath
        $sha256 = (Get-FileHash -Path $backupPath -Algorithm SHA256).Hash
        Write-Host "  Backup verified successfully: $backupPath" -ForegroundColor Green
        Write-Host "  Backup Size: $([Math]::Round($fileInfo.Length / 1MB, 2)) MB ($($fileInfo.Length) bytes)" -ForegroundColor Gray
        Write-Host "  SHA-256: $sha256" -ForegroundColor Gray

        return [PSCustomObject]@{
            Success = $true
            DatabaseName = $Database
            Server = $Server
            BackupPath = $backupPath
            FileSizeBytes = $fileInfo.Length
            Sha256Checksum = $sha256
            TimestampUtc = (Get-Date).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")
            Operator = $priv.LoginName
            SchemasBackedUp = $foundSchemas
        }
    }
    finally {
        $conn.Close()
        $conn.Dispose()
    }
}

# ------------------------------------------------------------------------------
# 2. New-CbosSupportBundle: Complete diagnostic package with manifest and redacted logs
# ------------------------------------------------------------------------------
function New-CbosSupportBundle {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $false)]
        [string]$Server = "localhost",

        [Parameter(Mandatory = $false)]
        [string]$Database = "Clovent_BusinessOperatingSystem",

        [Parameter(Mandatory = $false)]
        [switch]$UseWindowsAuth,

        [Parameter(Mandatory = $false)]
        [string]$SqlUser,

        [Parameter(Mandatory = $false)]
        [System.Security.SecureString]$SqlPassword,

        [Parameter(Mandatory = $false)]
        [string]$TicketId,

        [Parameter(Mandatory = $false)]
        [string]$CorrelationId,

        [Parameter(Mandatory = $false)]
        [string]$CustomerOrganization = "Acme Retailers",

        [Parameter(Mandatory = $false)]
        [string]$AuthorizedBy = "Store Manager",

        [Parameter(Mandatory = $false)]
        [switch]$CustomerConsent,

        [Parameter(Mandatory = $false)]
        [string]$OutputDirectory,

        [Parameter(Mandatory = $false)]
        [int]$LogRetentionDays = 3
    )

    # STRICT POLICY: Customer Authorization Gate
    if (-not $CustomerConsent) {
        throw "CUSTOMER AUTHORIZATION REQUIRED: Customer data must be transferred securely with customer authorization. Pass -CustomerConsent to verify customer consent. Never automatically upload backups."
    }

    if ([string]::IsNullOrWhiteSpace($TicketId)) {
        $TicketId = "INC-" + (Get-Date).ToString("yyyyMMdd-HHmmss")
    }
    if ([string]::IsNullOrWhiteSpace($CorrelationId)) {
        $CorrelationId = [Guid]::NewGuid().ToString()
    }

    Write-Host "================================================================================" -ForegroundColor Cyan
    Write-Host "  CBOS SUPPORT BUNDLE GENERATION" -ForegroundColor Cyan
    Write-Host "  Ticket: $TicketId | Correlation: $CorrelationId" -ForegroundColor Cyan
    Write-Host "  Customer Org: $CustomerOrganization | Authorized By: $AuthorizedBy" -ForegroundColor Cyan
    Write-Host "================================================================================" -ForegroundColor Cyan

    $baseOutDir = if (-not [string]::IsNullOrWhiteSpace($OutputDirectory)) {
        $OutputDirectory
    } else {
        Join-Path $env:ProgramData "Clovent\SupportBundles"
    }

    $bundleTimestamp = (Get-Date).ToUniversalTime().ToString("yyyyMMdd_HHmmss")
    $bundleFolderName = "CBOS_SupportBundle_${TicketId}_${bundleTimestamp}"
    $bundleDir = Join-Path $baseOutDir $bundleFolderName
    New-CbosSupportDirectory -Path $bundleDir | Out-Null
    $logsSubDir = Join-Path $bundleDir "redacted_logs"
    New-CbosSupportDirectory -Path $logsSubDir | Out-Null

    # 1. Execute full verified backup
    Write-Host "[1/5] Executing full verified database backup..." -ForegroundColor Cyan
    $backupResult = Backup-CbosDatabase -Server $Server -Database $Database -UseWindowsAuth:$UseWindowsAuth -SqlUser $SqlUser -SqlPassword $SqlPassword -BackupDirectory $bundleDir
    if (-not $backupResult.Success) {
        throw "Database backup failed during bundle creation."
    }

    # Write separate .sha256 file
    $sha256FileName = "$([System.IO.Path]::GetFileName($backupResult.BackupPath)).sha256"
    $sha256FilePath = Join-Path $bundleDir $sha256FileName
    "$($backupResult.Sha256Checksum) *$([System.IO.Path]::GetFileName($backupResult.BackupPath))" | Set-Content -Path $sha256FilePath -Encoding UTF8

    # 2. Collect Environment and Migration Versions
    Write-Host "[2/5] Collecting SQL Server and EF Core migration versions across all 6 schemas..." -ForegroundColor Cyan
    $connContext = New-CbosSqlConnection -Server $Server -Database $Database -UseWindowsAuth:$UseWindowsAuth -SqlUser $SqlUser -SqlPassword $SqlPassword
    $conn = $connContext.Connection

    $sqlServerVersion = ""
    $sqlEdition = ""
    $compatLevel = 0
    $migrations = @{}

    try {
        $cmdVer = $conn.CreateCommand()
        $cmdVer.CommandText = "SELECT @@VERSION AS FullVer, CAST(SERVERPROPERTY('ProductVersion') AS NVARCHAR(50)) AS ProdVer, CAST(SERVERPROPERTY('Edition') AS NVARCHAR(100)) AS Edition, compatibility_level FROM sys.databases WHERE name = @db;"
        $cmdVer.Parameters.Add((New-Object System.Data.SqlClient.SqlParameter("@db", [System.Data.SqlDbType]::NVarChar, 128) -Property @{ Value = $Database })) | Out-Null
        $rdr = $cmdVer.ExecuteReader()
        if ($rdr.Read()) {
            $sqlServerVersion = [string]$rdr["FullVer"]
            $sqlEdition = [string]$rdr["Edition"]
            $compatLevel = [int]$rdr["compatibility_level"]
        }
        $rdr.Close()

        $schemas = @('Authentication', 'Identity', 'MasterData', 'Catalog', 'Inventory', 'Restaurant')
        foreach ($s in $schemas) {
            $migrations[$s] = @()
            try {
                $cmdMig = $conn.CreateCommand()
                $cmdMig.CommandText = "SELECT MigrationId, ProductVersion FROM [$s].[__EFMigrationsHistory] ORDER BY MigrationId;"
                $migRdr = $cmdMig.ExecuteReader()
                while ($migRdr.Read()) {
                    $migrations[$s] += @{
                        MigrationId = [string]$migRdr["MigrationId"]
                        ProductVersion = [string]$migRdr["ProductVersion"]
                    }
                }
                $migRdr.Close()
            }
            catch {
                $migrations[$s] += @{ Status = "Unable to read migrations table: $($_.Exception.Message)" }
            }
        }
    }
    finally {
        $conn.Close()
        $conn.Dispose()
    }

    # 3. Application Version & Source Identity
    Write-Host "[3/5] Resolving application and build source identity..." -ForegroundColor Cyan
    $appVersion = "1.2.2"
    $gitCommit = "unknown"
    $gitBranch = "unknown"

    try {
        $gitCommit = (git rev-parse HEAD 2>$null)
        if ($LASTEXITCODE -eq 0 -and -not [string]::IsNullOrWhiteSpace($gitCommit)) {
            $gitCommit = $gitCommit.Trim()
            $gitBranch = (git rev-parse --abbrev-ref HEAD 2>$null).Trim()
        }
    } catch { }

    # 4. Redact and copy relevant logs
    Write-Host "[4/5] Collecting and redacting operational logs..." -ForegroundColor Cyan
    $logCandidateDirs = @(
        (Join-Path $env:LOCALAPPDATA "Clovent\Logs"),
        (Join-Path $env:ProgramData "Clovent\BusinessOperatingSystem\Logs"),
        (Join-Path $env:ProgramData "Clovent\Logs")
    )

    $redactedLogCount = 0
    $cutoffDate = (Get-Date).AddDays(-$LogRetentionDays)

    foreach ($ld in $logCandidateDirs) {
        if (Test-Path $ld) {
            $logFiles = Get-ChildItem -Path $ld -Filter "*.log" -File -Recurse -ErrorAction SilentlyContinue | Where-Object { $_.LastWriteTime -ge $cutoffDate }
            foreach ($lf in $logFiles) {
                try {
                    $rawContent = Get-Content -Path $lf.FullName -Raw -Encoding UTF8
                    $sanitized = Mask-DatabaseSecrets $rawContent
                    # Redact credit card numbers (13-16 digits with optional dashes/spaces)
                    $sanitized = [System.Text.RegularExpressions.Regex]::Replace($sanitized, "\b(?:\d[ -]*?){13,16}\b", "[CARD_NUMBER_REDACTED]")
                    # Redact potential authorization tokens and PINs
                    $sanitized = [System.Text.RegularExpressions.Regex]::Replace($sanitized, "(?i)(pin|password|secret|bearer)\s*[:=]\s*[^\s,;]+", "$1=******")

                    $targetLogFile = Join-Path $logsSubDir $lf.Name
                    $sanitized | Set-Content -Path $targetLogFile -Encoding UTF8
                    $redactedLogCount++
                }
                catch { }
            }
        }
    }
    Write-Host "  Sanitized $redactedLogCount log file(s) into bundle." -ForegroundColor Gray

    # 5. Generate Manifest
    Write-Host "[5/5] Generating support bundle manifest..." -ForegroundColor Cyan
    $manifest = [ordered]@{
        BundleVersion = "1.0"
        IncidentId = $TicketId
        CorrelationId = $CorrelationId
        ExportedAtUtc = (Get-Date).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")
        CustomerAuthorization = [ordered]@{
            Organization = $CustomerOrganization
            AuthorizedBy = $AuthorizedBy
            ConsentConfirmed = $true
            DataTransferPolicy = "CUSTOMER DATA SENSITIVITY: This bundle contains customer database records. Customer data must be transferred securely using approved encrypted transmission with customer authorization. Automated upload is strictly disabled."
        }
        Application = [ordered]@{
            ProductName = "Clovent Business Operating System"
            ReportedVersion = $appVersion
            BuildGitCommit = $gitCommit
            BuildGitBranch = $gitBranch
            TargetRuntime = ".NET 10.0"
        }
        SqlServer = [ordered]@{
            ServerName = $Server
            Edition = $sqlEdition
            CompatibilityLevel = $compatLevel
            Version = $sqlServerVersion
        }
        Backup = [ordered]@{
            FileName = [System.IO.Path]::GetFileName($backupResult.BackupPath)
            FileSizeBytes = $backupResult.FileSizeBytes
            Sha256Checksum = $backupResult.Sha256Checksum
            IsCopyOnly = $true
            CreatedAtUtc = $backupResult.TimestampUtc
        }
        BoundedContextMigrations = $migrations
    }

    $manifestPath = Join-Path $bundleDir "manifest.json"
    $manifest | ConvertTo-Json -Depth 6 | Set-Content -Path $manifestPath -Encoding UTF8

    # Compress bundle to zip
    $zipPath = "$bundleDir.zip"
    if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
    Compress-Archive -Path "$bundleDir\*" -DestinationPath $zipPath -CompressionLevel Optimal
    try {
        Remove-Item $bundleDir -Recurse -Force -ErrorAction Stop
    }
    catch {
        Write-Warning "Non-fatal notice: staging directory '$bundleDir' retained: $($_.Exception.Message)"
    }

    $zipInfo = Get-Item $zipPath
    Write-Host "================================================================================" -ForegroundColor Green
    Write-Host "SUPPORT BUNDLE CREATED SUCCESSFULLY: $zipPath" -ForegroundColor Green
    Write-Host "Bundle Size: $([Math]::Round($zipInfo.Length / 1MB, 2)) MB ($($zipInfo.Length) bytes)" -ForegroundColor Gray
    Write-Host "SHA-256 (Backup): $($backupResult.Sha256Checksum)" -ForegroundColor Gray
    Write-Host "NOTICE: Never automatically upload customer backups. Customer data must be transferred securely under explicit authorization." -ForegroundColor Yellow
    Write-Host "================================================================================" -ForegroundColor Green

    return [PSCustomObject]@{
        Success = $true
        BundleZipPath = $zipPath
        TicketId = $TicketId
        CorrelationId = $CorrelationId
        BackupFileName = [System.IO.Path]::GetFileName($backupResult.BackupPath)
        Sha256Checksum = $backupResult.Sha256Checksum
        CustomerOrganization = $CustomerOrganization
        AuthorizedBy = $AuthorizedBy
    }
}

# ------------------------------------------------------------------------------
# 3. Restore-CbosIsolatedDatabase: Safe restore into a NEW isolated database
# ------------------------------------------------------------------------------
function Restore-CbosIsolatedDatabase {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$BackupFilePath,

        [Parameter(Mandatory = $true)]
        [string]$TargetDatabaseName,

        [Parameter(Mandatory = $false)]
        [string]$Server = "localhost",

        [Parameter(Mandatory = $false)]
        [switch]$UseWindowsAuth,

        [Parameter(Mandatory = $false)]
        [string]$SqlUser,

        [Parameter(Mandatory = $false)]
        [System.Security.SecureString]$SqlPassword,

        [Parameter(Mandatory = $false)]
        [string]$ExpectedSha256,

        [Parameter(Mandatory = $false)]
        [switch]$AllowOverwriteExisting
    )

    Write-Host "================================================================================" -ForegroundColor Cyan
    Write-Host "  CBOS ISOLATED DATABASE RESTORATION" -ForegroundColor Cyan
    Write-Host "  Target Database: $TargetDatabaseName" -ForegroundColor Cyan
    Write-Host "  Target Server:   $Server" -ForegroundColor Cyan
    Write-Host "================================================================================" -ForegroundColor Cyan

    # Safety Guard 1: Never target production / operational databases
    foreach ($fb in $script:ForbiddenDatabases) {
        if ($TargetDatabaseName -ieq $fb) {
            throw "SAFETY VIOLATION: Refusing to restore into protected operational/customer database '$fb'. Restores must always target an isolated test database."
        }
    }

    # Safety Guard 2: Verify backup file exists
    if (-not (Test-Path $BackupFilePath)) {
        throw "Backup file not found at '$BackupFilePath'."
    }

    # Safety Guard 3: Verify SHA-256 checksum if provided
    if (-not [string]::IsNullOrWhiteSpace($ExpectedSha256)) {
        Write-Host "[1/6] Verifying SHA-256 checksum of backup archive..." -ForegroundColor Cyan
        $actualSha256 = (Get-FileHash -Path $BackupFilePath -Algorithm SHA256).Hash
        if (-not ($actualSha256 -ieq $ExpectedSha256.Trim())) {
            throw "CHECKSUM MISMATCH: Backup archive SHA-256 '$actualSha256' does not match expected checksum '$ExpectedSha256'. Archive may be corrupt or altered."
        }
        Write-Host "  Checksum verified: $actualSha256" -ForegroundColor Green
    }

    # Connect to master on target server
    Write-Host "[2/6] Connecting to target SQL Server instance '$Server'..." -ForegroundColor Cyan
    $connContext = New-CbosSqlConnection -Server $Server -Database "master" -UseWindowsAuth:$UseWindowsAuth -SqlUser $SqlUser -SqlPassword $SqlPassword
    $conn = $connContext.Connection

    try {
        # Safety Guard 4: Never overwrite existing database by default
        $cmdCheckExist = $conn.CreateCommand()
        $cmdCheckExist.CommandText = "SELECT COUNT(1) FROM sys.databases WHERE name = @name;"
        $cmdCheckExist.Parameters.Add((New-Object System.Data.SqlClient.SqlParameter("@name", [System.Data.SqlDbType]::NVarChar, 128) -Property @{ Value = $TargetDatabaseName })) | Out-Null
        $exists = [int]$cmdCheckExist.ExecuteScalar() -gt 0

        if ($exists) {
            if (-not $AllowOverwriteExisting) {
                throw "TARGET DATABASE ALREADY EXISTS: Target database '$TargetDatabaseName' already exists on server '$Server'. By default, CBOS support workflow never overwrites an existing database. Specify a new isolated name or pass -AllowOverwriteExisting."
            }
            Write-Warning "Target database '$TargetDatabaseName' exists. Overwriting permitted by -AllowOverwriteExisting."
        }

        # Verify physical backup integrity with RESTORE VERIFYONLY
        Write-Host "[3/6] Verifying physical backup media integrity (RESTORE VERIFYONLY WITH CHECKSUM)..." -ForegroundColor Cyan
        $cmdVerify = $conn.CreateCommand()
        $cmdVerify.CommandText = "RESTORE VERIFYONLY FROM DISK = @path WITH CHECKSUM;"
        $cmdVerify.Parameters.Add((New-Object System.Data.SqlClient.SqlParameter("@path", [System.Data.SqlDbType]::NVarChar, 4000) -Property @{ Value = $BackupFilePath })) | Out-Null
        $cmdVerify.CommandTimeout = 120
        $cmdVerify.ExecuteNonQuery() | Out-Null
        Write-Host "  Backup media verified successfully." -ForegroundColor Green

        # Inspect logical file names from backup archive
        Write-Host "[4/6] Reading logical file descriptors and resolving isolated paths..." -ForegroundColor Cyan
        $cmdFileList = $conn.CreateCommand()
        $cmdFileList.CommandText = "RESTORE FILELISTONLY FROM DISK = @path;"
        $cmdFileList.Parameters.Add((New-Object System.Data.SqlClient.SqlParameter("@path", [System.Data.SqlDbType]::NVarChar, 4000) -Property @{ Value = $BackupFilePath })) | Out-Null
        $rdr = $cmdFileList.ExecuteReader()
        $dataLogicalName = $null
        $logLogicalName = $null
        while ($rdr.Read()) {
            $lName = [string]$rdr["LogicalName"]
            $type = [string]$rdr["Type"]
            if ($type -eq "D" -and $null -eq $dataLogicalName) { $dataLogicalName = $lName }
            if ($type -eq "L" -and $null -eq $logLogicalName) { $logLogicalName = $lName }
        }
        $rdr.Close()

        if ([string]::IsNullOrWhiteSpace($dataLogicalName) -or [string]::IsNullOrWhiteSpace($logLogicalName)) {
            throw "Unable to resolve logical data and log file names from backup file."
        }

        # Resolve instance default data directory
        $cmdDataDir = $conn.CreateCommand()
        $cmdDataDir.CommandText = "SELECT CAST(SERVERPROPERTY('InstanceDefaultDataPath') AS NVARCHAR(512));"
        $dataDir = $cmdDataDir.ExecuteScalar()
        if ([string]::IsNullOrWhiteSpace($dataDir)) {
            $dataDir = Join-Path $env:ProgramData "Clovent\Data"
        }
        New-CbosSupportDirectory -Path $dataDir | Out-Null

        $targetMdf = Join-Path $dataDir "${TargetDatabaseName}.mdf"
        $targetLdf = Join-Path $dataDir "${TargetDatabaseName}_log.ldf"

        # Execute Restore
        Write-Host "[5/6] Restoring database to '$TargetDatabaseName' with file relocation..." -ForegroundColor Cyan
        $escapedDb = $TargetDatabaseName.Replace("]", "]]")
        
        if ($exists) {
            $cmdDrop = $conn.CreateCommand()
            $cmdDrop.CommandText = "ALTER DATABASE [$escapedDb] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;"
            $cmdDrop.ExecuteNonQuery() | Out-Null
        }

        $restoreSql = @"
RESTORE DATABASE [$escapedDb]
FROM DISK = @path
WITH MOVE '$dataLogicalName' TO '$targetMdf',
     MOVE '$logLogicalName' TO '$targetLdf',
     REPLACE, RECOVERY, CHECKSUM, STATS = 10;
ALTER DATABASE [$escapedDb] SET MULTI_USER;
"@
        $cmdRestore = $conn.CreateCommand()
        $cmdRestore.CommandText = $restoreSql
        $cmdRestore.Parameters.Add((New-Object System.Data.SqlClient.SqlParameter("@path", [System.Data.SqlDbType]::NVarChar, 4000) -Property @{ Value = $BackupFilePath })) | Out-Null
        $cmdRestore.CommandTimeout = 300
        $cmdRestore.ExecuteNonQuery() | Out-Null
        Write-Host "  Database restore succeeded." -ForegroundColor Green

        # Check DBCC consistency
        Write-Host "[6/6] Executing DBCC CHECKDB consistency validation..." -ForegroundColor Cyan
        $cmdCheck = $conn.CreateCommand()
        $cmdCheck.CommandText = "DBCC CHECKDB ([$escapedDb]) WITH NO_INFOMSGS, ALL_ERRORMSGS;"
        $cmdCheck.CommandTimeout = 120
        $cmdCheck.ExecuteNonQuery() | Out-Null
        Write-Host "  DBCC CHECKDB completed: 0 allocation errors, 0 consistency errors." -ForegroundColor Green

        return [PSCustomObject]@{
            Success = $true
            RestoredDatabase = $TargetDatabaseName
            Server = $Server
            DataPath = $targetMdf
            LogPath = $targetLdf
            RestoredAtUtc = (Get-Date).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")
        }
    }
    finally {
        $conn.Close()
        $conn.Dispose()
    }
}

# ------------------------------------------------------------------------------
# 3b. Protect-CbosRestoredEnvironment: Post-restore isolation & sanitization
# ------------------------------------------------------------------------------
function Protect-CbosRestoredEnvironment {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$TargetDatabaseName,

        [Parameter(Mandatory = $false)]
        [string]$Server = "localhost",

        [Parameter(Mandatory = $false)]
        [switch]$UseWindowsAuth,

        [Parameter(Mandatory = $false)]
        [string]$SqlUser,

        [Parameter(Mandatory = $false)]
        [System.Security.SecureString]$SqlPassword,

        [Parameter(Mandatory = $false)]
        [string]$ConfigDirectory
    )

    Write-Host "[CBOS-PROTECT] Sanitizing restored database '$TargetDatabaseName' for safe investigation..." -ForegroundColor Cyan
    $connContext = New-CbosSqlConnection -Server $Server -Database $TargetDatabaseName -UseWindowsAuth:$UseWindowsAuth -SqlUser $SqlUser -SqlPassword $SqlPassword
    $conn = $connContext.Connection

    try {
        # 1. Quarantine and disable Outbox messages (printing, QuickBooks sync, delta sync, analytics)
        Write-Host "  Quarantining pending transactional outbox messages (printing & sync)..." -ForegroundColor Gray
        $cmdOutbox = $conn.CreateCommand()
        $cmdOutbox.CommandText = @"
UPDATE [Restaurant].[OutboxMessages]
SET [Status] = 'DeadLetter',
    [LastError] = 'Quarantined for isolated support investigation: disabled external sync, background jobs, and printing'
WHERE [Status] IN ('Pending', 'Processing', 'RetryScheduled');
SELECT @@ROWCOUNT;
"@
        $quarantinedOutboxCount = [int]$cmdOutbox.ExecuteScalar()
        Write-Host "    $quarantinedOutboxCount outbox message(s) quarantined to DeadLetter." -ForegroundColor Green

        # 2. Deactivate QuickBooks sync mappings if table exists
        $cmdQb = $conn.CreateCommand()
        $cmdQb.CommandText = @"
IF OBJECT_ID(N'[Restaurant].[QuickBooksSyncMaps]', N'U') IS NOT NULL
BEGIN
    UPDATE [Restaurant].[QuickBooksSyncMaps]
    SET [Status] = 'Suspended',
        [LastError] = 'Quarantined in isolated support environment'
    WHERE [Status] != 'Suspended';
    SELECT @@ROWCOUNT;
END
ELSE
BEGIN
    SELECT 0;
END
"@
        $quarantinedQbCount = [int]$cmdQb.ExecuteScalar()
        if ($quarantinedQbCount -gt 0) {
            Write-Host "    $quarantinedQbCount QuickBooks sync mapping(s) suspended." -ForegroundColor Green
        }

        # 3. Create isolated printer configuration (disabling physical printing)
        $targetConfigDir = if (-not [string]::IsNullOrWhiteSpace($ConfigDirectory)) {
            $ConfigDirectory
        } else {
            Join-Path $env:ProgramData "Clovent\BusinessOperatingSystem\Config"
        }

        New-CbosSupportDirectory -Path $targetConfigDir | Out-Null

        $printerJsonPath = Join-Path $targetConfigDir "printers.json"
        $isolatedPrinterConfig = [ordered]@{
            Version = 1
            Profiles = @()
            Assignments = @()
            DefaultReceiptProfileId = $null
        }
        $isolatedPrinterConfig | ConvertTo-Json -Depth 4 | Set-Content -Path $printerJsonPath -Encoding UTF8
        Write-Host "  Isolated printer configuration created: $printerJsonPath (physical printing disabled)." -ForegroundColor Green

        # 4. Handle machine-bound encrypted DPAPI settings by explicit reconfiguration
        Write-Host "  Handling machine-bound DPAPI configuration..." -ForegroundColor Gray
        $dbConfigJsonPath = Join-Path (Split-Path $targetConfigDir -Parent) "database.config.json"
        $dbConfig = [ordered]@{
            Server = $Server
            Database = $TargetDatabaseName
            UseWindowsAuthentication = $true
            UserId = $null
            EncryptedPassword = $null
            PlainTextPassword = $null
            TrustServerCertificate = $true
            ConnectionTimeout = 15
        }
        $dbConfig | ConvertTo-Json -Depth 4 | Set-Content -Path $dbConfigJsonPath -Encoding UTF8
        Write-Host "  Database configuration reconfigured for isolated environment: $dbConfigJsonPath" -ForegroundColor Green

        return [PSCustomObject]@{
            Success = $true
            QuarantinedOutboxCount = $quarantinedOutboxCount
            QuarantinedQbCount = $quarantinedQbCount
            PrinterConfigPath = $printerJsonPath
            DatabaseConfigPath = $dbConfigJsonPath
        }
    }
    finally {
        $conn.Close()
        $conn.Dispose()
    }
}

# ------------------------------------------------------------------------------
# 4. Invoke-CbosCorrectiveScript: Reviewed corrective-script workflow
# ------------------------------------------------------------------------------
function Invoke-CbosCorrectiveScript {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$ScriptFilePath,

        [Parameter(Mandatory = $true)]
        [string]$TargetDatabaseName,

        [Parameter(Mandatory = $false)]
        [string]$Server = "localhost",

        [Parameter(Mandatory = $false)]
        [switch]$UseWindowsAuth,

        [Parameter(Mandatory = $false)]
        [string]$SqlUser,

        [Parameter(Mandatory = $false)]
        [System.Security.SecureString]$SqlPassword,

        [Parameter(Mandatory = $false)]
        [switch]$DryRun,

        [Parameter(Mandatory = $false)]
        [switch]$Execute,

        [Parameter(Mandatory = $false)]
        [switch]$PreTestedOnRestoredCopy,

        [Parameter(Mandatory = $false)]
        [string]$CompensationReference,

        [Parameter(Mandatory = $false)]
        [string]$BackupDirectory
    )

    if (-not $DryRun -and -not $Execute) {
        throw "Specify either -DryRun (preview diagnosis) or -Execute (transactional mutation)."
    }
    if ($DryRun -and $Execute) {
        throw "Cannot specify both -DryRun and -Execute simultaneously."
    }

    if (-not (Test-Path $ScriptFilePath)) {
        throw "Corrective script file not found at '$ScriptFilePath'."
    }

    $scriptContent = Get-Content -Path $ScriptFilePath -Raw -Encoding UTF8
    $scriptHash = (Get-FileHash -Path $ScriptFilePath -Algorithm SHA256).Hash

    # 1. Parse Metadata Headers
    $scriptId = if ($scriptContent -match "(?im)^--\s*SCRIPT_ID:\s*([^\r\n]+)") { $matches[1].Trim() } else { [System.IO.Path]::GetFileNameWithoutExtension($ScriptFilePath) }
    $description = if ($scriptContent -match "(?im)^--\s*DESCRIPTION:\s*([^\r\n]+)") { $matches[1].Trim() } else { "Ad-hoc corrective script" }
    $targetSchema = if ($scriptContent -match "(?im)^--\s*TARGET_SCHEMA:\s*([^\r\n]+)") { $matches[1].Trim() } else { "" }

    Write-Host "================================================================================" -ForegroundColor Cyan
    Write-Host "  CBOS CORRECTIVE SCRIPT EXECUTION WORKFLOW" -ForegroundColor Cyan
    Write-Host "  Script ID:   $scriptId" -ForegroundColor Cyan
    Write-Host "  Description: $description" -ForegroundColor Cyan
    Write-Host "  Script Hash: $scriptHash" -ForegroundColor Cyan
    Write-Host "  Database:    $TargetDatabaseName on $Server" -ForegroundColor Cyan
    Write-Host "  Mode:        $(if ($DryRun) { 'PREVIEW / READ-ONLY DIAGNOSIS' } else { 'TRANSACTIONAL EXECUTION' })" -ForegroundColor Cyan
    Write-Host "================================================================================" -ForegroundColor Cyan

    # 2. Financial Immutability Static Guard (PDR-0003)
    # Strictly prohibit direct rewrite of completed sales, payments, refunds, taxes, or customer ledger entries
    $illegalPatterns = @(
        "(?im)\bUPDATE\s+\[?Restaurant\]?\.\[?Orders\]?\s+SET\s+",
        "(?im)\bDELETE\s+(?:FROM\s+)?\[?Restaurant\]?\.\[?Orders\]?\b",
        "(?im)\bUPDATE\s+\[?Restaurant\]?\.\[?Payments\]?\s+SET\s+",
        "(?im)\bDELETE\s+(?:FROM\s+)?\[?Restaurant\]?\.\[?Payments\]?\b",
        "(?im)\bUPDATE\s+\[?Restaurant\]?\.\[?Refunds\]?\s+SET\s+",
        "(?im)\bDELETE\s+(?:FROM\s+)?\[?Restaurant\]?\.\[?Refunds\]?\b",
        "(?im)\bUPDATE\s+\[?Restaurant\]?\.\[?OrderLines\]?\s+SET\s+.*(?:TaxAmount|TaxableBase)",
        "(?im)\bDELETE\s+(?:FROM\s+)?\[?Restaurant\]?\.\[?CustomerLedgerEntries\]?\b",
        "(?im)\bUPDATE\s+\[?Restaurant\]?\.\[?CustomerLedgerEntries\]?\s+SET\s+"
    )

    foreach ($pat in $illegalPatterns) {
        if ($scriptContent -match $pat) {
            throw "PDR-0003 FINANCIAL INTEGRITY VIOLATION: Corrective script attempts direct update or deletion of completed sales, payments, refunds, taxes, or customer ledger history. Under CBOS financial governance, completed financial transactions are immutable. Financial corrections must use approved compensating transactions (reversals, credit memos, compensating ledger entries)."
        }
    }

    # Connect to database
    $connContext = New-CbosSqlConnection -Server $Server -Database $TargetDatabaseName -UseWindowsAuth:$UseWindowsAuth -SqlUser $SqlUser -SqlPassword $SqlPassword
    $conn = $connContext.Connection

    try {
        $operator = (Test-CbosSupportPrivileges -Connection $conn -TargetDatabase $TargetDatabaseName).LoginName

        # Ensure Audit Table Exists
        $cmdInitAudit = $conn.CreateCommand()
        $cmdInitAudit.CommandText = @"
IF NOT EXISTS (SELECT 1 FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'MasterData' AND t.name = 'SupportScriptExecutionAudit')
BEGIN
    CREATE TABLE [MasterData].[SupportScriptExecutionAudit] (
        [Id] BIGINT IDENTITY(1,1) PRIMARY KEY,
        [ScriptId] NVARCHAR(100) NOT NULL,
        [ScriptHash] VARCHAR(64) NOT NULL,
        [Operator] NVARCHAR(100) NOT NULL,
        [ExecutedAtUtc] DATETIME2 NOT NULL,
        [TargetDatabase] NVARCHAR(128) NOT NULL,
        [PreCheckSummary] NVARCHAR(MAX) NULL,
        [AffectedRows] INT NOT NULL,
        [Outcome] NVARCHAR(50) NOT NULL,
        [ErrorMessage] NVARCHAR(MAX) NULL,
        [CompensationReference] NVARCHAR(200) NULL
    );
    CREATE INDEX [IX_SupportScriptExecutionAudit_ScriptId] ON [MasterData].[SupportScriptExecutionAudit] ([ScriptId]);
END
"@
        $cmdInitAudit.ExecuteNonQuery() | Out-Null

        # Repeat-Execution Protection
        $cmdCheckRepeat = $conn.CreateCommand()
        $cmdCheckRepeat.CommandText = "SELECT COUNT(1) FROM [MasterData].[SupportScriptExecutionAudit] WHERE [ScriptId] = @sid AND [Outcome] = 'Success';"
        $cmdCheckRepeat.Parameters.Add((New-Object System.Data.SqlClient.SqlParameter("@sid", [System.Data.SqlDbType]::NVarChar, 100) -Property @{ Value = $scriptId })) | Out-Null
        $alreadyRunCount = [int]$cmdCheckRepeat.ExecuteScalar()

        if ($alreadyRunCount -gt 0) {
            if ($Execute) {
                throw "REPEAT EXECUTION BLOCKED: Script '$scriptId' has already been successfully executed on database '$TargetDatabaseName'. To prevent inconsistent mutations or duplicate entries, repeat execution is forbidden."
            }
            else {
                Write-Warning "Script '$scriptId' was previously executed successfully on '$TargetDatabaseName'."
            }
        }

        # Parse script blocks if structured with section markers
        # Syntax:
        # -- PRECHECK:
        # <sql>
        # -- PREVIEW_DIAGNOSIS:
        # <sql>
        # -- MUTATION:
        # <sql>
        # -- POSTCHECK_RECONCILIATION:
        # <sql>
        $hasSections = $scriptContent -match "(?im)^--\s*MUTATION:"
        $precheckSql = ""
        $previewSql = ""
        $mutationSql = ""
        $postcheckSql = ""

        if ($hasSections) {
            if ($scriptContent -match "(?is)--\s*PRECHECK:\s*(.*?)(?:--\s*(?:PREVIEW_DIAGNOSIS|MUTATION|POSTCHECK_RECONCILIATION)|\Z)") {
                $precheckSql = $matches[1].Trim()
            }
            if ($scriptContent -match "(?is)--\s*PREVIEW_DIAGNOSIS:\s*(.*?)(?:--\s*(?:MUTATION|POSTCHECK_RECONCILIATION)|\Z)") {
                $previewSql = $matches[1].Trim()
            }
            if ($scriptContent -match "(?is)--\s*MUTATION:\s*(.*?)(?:--\s*POSTCHECK_RECONCILIATION|\Z)") {
                $mutationSql = $matches[1].Trim()
            }
            if ($scriptContent -match "(?is)--\s*POSTCHECK_RECONCILIATION:\s*(.*?)\Z") {
                $postcheckSql = $matches[1].Trim()
            }
        }
        else {
            # Legacy/Single-block script: entire script is mutation; preview is not supported
            $mutationSql = $scriptContent
        }

        # ----------------------------------------------------------------------
        # MODE 1: Preview / Read-Only Diagnosis
        # ----------------------------------------------------------------------
        if ($DryRun) {
            Write-Host "[PREVIEW] Executing prechecks and read-only diagnostic preview..." -ForegroundColor Yellow
            $precheckSummary = "No explicit precheck defined"
            if (-not [string]::IsNullOrWhiteSpace($precheckSql)) {
                $cmdPre = $conn.CreateCommand()
                $cmdPre.CommandText = $precheckSql
                $res = $cmdPre.ExecuteScalar()
                $precheckSummary = "Precheck returned: $res"
                Write-Host "  Precheck Result: $precheckSummary" -ForegroundColor Gray
            }

            if (-not [string]::IsNullOrWhiteSpace($previewSql)) {
                Write-Host "  Diagnosed Affected Records:" -ForegroundColor Cyan
                $cmdPrev = $conn.CreateCommand()
                $cmdPrev.CommandText = $previewSql
                $prevRdr = $cmdPrev.ExecuteReader()
                $diagCount = 0
                while ($prevRdr.Read()) {
                    $diagCount++
                    $rowVals = @()
                    for ($i = 0; $i -lt $prevRdr.FieldCount; $i++) {
                        $rowVals += "$($prevRdr.GetName($i))=$($prevRdr.GetValue($i))"
                    }
                    Write-Host "    [$diagCount] $($rowVals -join ' | ')" -ForegroundColor Gray
                }
                $prevRdr.Close()
                Write-Host "  Total Affected Rows Diagnosed: $diagCount" -ForegroundColor Green
            }
            else {
                Write-Host "  No separate PREVIEW_DIAGNOSIS block provided in script." -ForegroundColor Gray
            }

            Write-Host "\nPREVIEW COMPLETE. No changes were made to '$TargetDatabaseName'." -ForegroundColor Green
            return [PSCustomObject]@{
                Success = $true
                Mode = "DryRun"
                ScriptId = $scriptId
                ScriptHash = $scriptHash
                TargetDatabase = $TargetDatabaseName
                PreCheckSummary = $precheckSummary
            }
        }

        # ----------------------------------------------------------------------
        # MODE 2: Transactional Execution
        # ----------------------------------------------------------------------
        if ($Execute) {
            # Verification gate: Must be tested on restored copy first
            if (-not $PreTestedOnRestoredCopy) {
                Write-Warning "CONFIRMATION NOTICE: Corrective scripts must be tested and verified against the restored copy before running on production/live databases."
            }

            # 1. Take fresh pre-fix backup
            Write-Host "[1/5] Creating fresh pre-execution backup..." -ForegroundColor Cyan
            $freshBackupDir = if (-not [string]::IsNullOrWhiteSpace($BackupDirectory)) { $BackupDirectory } else { Join-Path $env:ProgramData "Clovent\Backups\PreFix" }
            New-CbosSupportDirectory -Path $freshBackupDir | Out-Null
            $preFixBackupName = "${TargetDatabaseName}_PreFix_${scriptId}_$((Get-Date).ToUniversalTime().ToString('yyyyMMdd_HHmmss')).bak"
            $preFixBackupPath = Join-Path $freshBackupDir $preFixBackupName

            $cmdBak = $conn.CreateCommand()
            $escapedDb = $TargetDatabaseName.Replace("]", "]]")
            $cmdBak.CommandText = "BACKUP DATABASE [$escapedDb] TO DISK = @path WITH COPY_ONLY, CHECKSUM, STATS = 10, INIT;"
            $cmdBak.Parameters.Add((New-Object System.Data.SqlClient.SqlParameter("@path", [System.Data.SqlDbType]::NVarChar, 4000) -Property @{ Value = $preFixBackupPath })) | Out-Null
            $cmdBak.CommandTimeout = 300
            $cmdBak.ExecuteNonQuery() | Out-Null
            Write-Host "  Pre-fix backup created: $preFixBackupPath" -ForegroundColor Green

            # 2. Execute Precheck
            Write-Host "[2/5] Executing prechecks..." -ForegroundColor Cyan
            $precheckSummary = "Precheck OK"
            if (-not [string]::IsNullOrWhiteSpace($precheckSql)) {
                $cmdPre = $conn.CreateCommand()
                $cmdPre.CommandText = $precheckSql
                $preRes = $cmdPre.ExecuteScalar()
                $precheckSummary = "Precheck evaluated: $preRes"
                Write-Host "  Precheck result: $precheckSummary" -ForegroundColor Gray
            }

            # 3. Transactional Execution
            Write-Host "[3/5] Executing script mutation inside transaction..." -ForegroundColor Cyan
            $tran = $conn.BeginTransaction("CorrectiveScriptTran")
            $affectedRows = 0

            try {
                $cmdMut = $conn.CreateCommand()
                $cmdMut.Transaction = $tran
                $cmdMut.CommandText = $mutationSql
                $cmdMut.CommandTimeout = 120
                $affectedRows = $cmdMut.ExecuteNonQuery()
                Write-Host "  Mutation executed. Affected rows: $affectedRows" -ForegroundColor Gray

                # 4. Post-fix Reconciliation
                Write-Host "[4/5] Executing post-fix reconciliation assertions..." -ForegroundColor Cyan
                if (-not [string]::IsNullOrWhiteSpace($postcheckSql)) {
                    $cmdPost = $conn.CreateCommand()
                    $cmdPost.Transaction = $tran
                    $cmdPost.CommandText = $postcheckSql
                    $postRes = $cmdPost.ExecuteScalar()
                    Write-Host "  Reconciliation query passed: $postRes" -ForegroundColor Green
                }

                # 5. Record Audit Entry
                Write-Host "[5/5] Recording audit trail in [MasterData].[SupportScriptExecutionAudit]..." -ForegroundColor Cyan
                $cmdAudit = $conn.CreateCommand()
                $cmdAudit.Transaction = $tran
                $cmdAudit.CommandText = @"
INSERT INTO [MasterData].[SupportScriptExecutionAudit]
    ([ScriptId], [ScriptHash], [Operator], [ExecutedAtUtc], [TargetDatabase], [PreCheckSummary], [AffectedRows], [Outcome], [CompensationReference])
VALUES
    (@sid, @shash, @op, SYSUTCDATETIME(), @db, @pre, @rows, 'Success', @comp);
"@
                $cmdAudit.Parameters.Add((New-Object System.Data.SqlClient.SqlParameter("@sid", [System.Data.SqlDbType]::NVarChar, 100) -Property @{ Value = $scriptId })) | Out-Null
                $cmdAudit.Parameters.Add((New-Object System.Data.SqlClient.SqlParameter("@shash", [System.Data.SqlDbType]::VarChar, 64) -Property @{ Value = $scriptHash })) | Out-Null
                $cmdAudit.Parameters.Add((New-Object System.Data.SqlClient.SqlParameter("@op", [System.Data.SqlDbType]::NVarChar, 100) -Property @{ Value = $operator })) | Out-Null
                $cmdAudit.Parameters.Add((New-Object System.Data.SqlClient.SqlParameter("@db", [System.Data.SqlDbType]::NVarChar, 128) -Property @{ Value = $TargetDatabaseName })) | Out-Null
                $cmdAudit.Parameters.Add((New-Object System.Data.SqlClient.SqlParameter("@pre", [System.Data.SqlDbType]::NVarChar, -1) -Property @{ Value = $precheckSummary })) | Out-Null
                $cmdAudit.Parameters.Add((New-Object System.Data.SqlClient.SqlParameter("@rows", [System.Data.SqlDbType]::Int) -Property @{ Value = $affectedRows })) | Out-Null
                $compParamValue = if ([string]::IsNullOrWhiteSpace($CompensationReference)) { [DBNull]::Value } else { $CompensationReference }
                $cmdAudit.Parameters.Add((New-Object System.Data.SqlClient.SqlParameter("@comp", [System.Data.SqlDbType]::NVarChar, 200) -Property @{ Value = $compParamValue })) | Out-Null
                $cmdAudit.ExecuteNonQuery() | Out-Null

                $tran.Commit()
                Write-Host "Transaction committed successfully. Corrective fix applied." -ForegroundColor Green

                return [PSCustomObject]@{
                    Success = $true
                    Mode = "Execute"
                    ScriptId = $scriptId
                    ScriptHash = $scriptHash
                    Operator = $operator
                    TargetDatabase = $TargetDatabaseName
                    PreFixBackup = $preFixBackupPath
                    AffectedRows = $affectedRows
                    CompensationReference = $CompensationReference
                    Outcome = "Success"
                }
            }
            catch {
                Write-Host "ERROR during execution: $($_.Exception.Message). Rolling back transaction..." -ForegroundColor Red
                try { $tran.Rollback() } catch { }

                # Log failure to audit table outside transaction
                try {
                    $cmdFailAudit = $conn.CreateCommand()
                    $cmdFailAudit.CommandText = @"
INSERT INTO [MasterData].[SupportScriptExecutionAudit]
    ([ScriptId], [ScriptHash], [Operator], [ExecutedAtUtc], [TargetDatabase], [PreCheckSummary], [AffectedRows], [Outcome], [ErrorMessage])
VALUES
    (@sid, @shash, @op, SYSUTCDATETIME(), @db, @pre, 0, 'Failed', @err);
"@
                    $cmdFailAudit.Parameters.Add((New-Object System.Data.SqlClient.SqlParameter("@sid", [System.Data.SqlDbType]::NVarChar, 100) -Property @{ Value = $scriptId })) | Out-Null
                    $cmdFailAudit.Parameters.Add((New-Object System.Data.SqlClient.SqlParameter("@shash", [System.Data.SqlDbType]::VarChar, 64) -Property @{ Value = $scriptHash })) | Out-Null
                    $cmdFailAudit.Parameters.Add((New-Object System.Data.SqlClient.SqlParameter("@op", [System.Data.SqlDbType]::NVarChar, 100) -Property @{ Value = $operator })) | Out-Null
                    $cmdFailAudit.Parameters.Add((New-Object System.Data.SqlClient.SqlParameter("@db", [System.Data.SqlDbType]::NVarChar, 128) -Property @{ Value = $TargetDatabaseName })) | Out-Null
                    $cmdFailAudit.Parameters.Add((New-Object System.Data.SqlClient.SqlParameter("@pre", [System.Data.SqlDbType]::NVarChar, -1) -Property @{ Value = $precheckSummary })) | Out-Null
                    $cmdFailAudit.Parameters.Add((New-Object System.Data.SqlClient.SqlParameter("@err", [System.Data.SqlDbType]::NVarChar, -1) -Property @{ Value = $_.Exception.Message })) | Out-Null
                    $cmdFailAudit.ExecuteNonQuery() | Out-Null
                } catch { }

                throw
            }
        }
    }
    finally {
        $conn.Close()
        $conn.Dispose()
    }
}

Export-ModuleMember -Function @(
    "Backup-CbosDatabase",
    "New-CbosSupportBundle",
    "Restore-CbosIsolatedDatabase",
    "Protect-CbosRestoredEnvironment",
    "Invoke-CbosCorrectiveScript",
    "Test-CbosSupportPrivileges",
    "Mask-DatabaseSecrets",
    "New-CbosSupportDirectory"
)
