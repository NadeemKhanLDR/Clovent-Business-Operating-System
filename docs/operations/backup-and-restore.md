# CBOS Database Backup & Disaster Recovery Procedures

| Attribute | Details |
| :--- | :--- |
| **Area** | Database Administration & Disaster Preparedness |
| **Audience** | Database Administrators, Systems Engineers, Store Operators |
| **Last Reviewed Version** | CBOS 1.3.0 |
| **Status** | **PROCEDURAL STANDARD** |
| **Classification** | **PUBLIC-SAFE** |

---

## 1. Backup & Restore Architecture

> [!IMPORTANT]
> **IMPLEMENTED CAPABILITIES (CBOS 1.3.0):**
> - **Manual & Pre-Upgrade Verified Backups:** `DatabaseBackupService` (`src/Clovent.Desktop/Commissioning/Database/DatabaseBackupService.cs`) executes `BACKUP DATABASE ... WITH COPY_ONLY, FORMAT, INIT, CHECKSUM` and automatically validates the resulting `.bak` file with `RESTORE VERIFYONLY` and `RESTORE HEADERONLY`.
> - **Backup & Restore UI:** Administrators can open **Settings -> Backup & Restore** (`DatabaseBackupRestoreForm`) to configure the backup destination, run manual backups, verify `.bak` files, view backup/restore history (`backup_history.json`), and restore into the configured database or an isolated target database.
> - **Restore Safeguards:** Restores require typing the target database name to confirm, verify the `.bak` file before touching any database, create a pre-restore safety backup when overwriting an existing database, use `SINGLE_USER WITH ROLLBACK IMMEDIATE` (restoring `MULTI_USER` in `finally`), and generate `WITH MOVE` clauses when restoring into an isolated database.
> - **Scheduled Backups:** `ScheduledBackupHostedService` (`IHostedService`) runs periodic verified `COPY_ONLY` backups when enabled in `backup_settings.json` (`BackupConfigurationStore`).
> - **Upgrade Integration:** Both `Clovent.Desktop` (`Program.cs` on `DatabaseTooOld`) and `Clovent.Installer.Provisioner.exe --upgrade` create a verified pre-upgrade backup before applying EF Core migrations, aborting safely if backup fails.
> - **Known Limitations:** Cloud/off-site backup replication is not included; administrators should copy verified `.bak` files to off-machine storage for disaster recovery.

---

## 2. Technical Constraints for SQL Server Express Backups

When deploying on Microsoft SQL Server Express (the standard standalone single-store configuration), administrators must account for platform constraints:
1. **NO SQL SERVER AGENT:** SQL Server Express does **not** include the SQL Server Agent service. Scheduled automated backups **cannot** be scheduled via SQL Agent jobs. **They must be scheduled via Windows Task Scheduler using PowerShell or batch scripts.**
2. **NO NATIVE BACKUP COMPRESSION:** The `WITH COMPRESSION` option is disabled by Microsoft in SQL Server Express. Backups produced by `BACKUP DATABASE` are uncompressed. Administrators should compress `.bak` files using external compression tools (e.g., PowerShell `Compress-Archive` or 7-Zip).
3. **NO NATIVE BACKUP ENCRYPTION VS. TDE DISTINCTION:**
   - **Transparent Data Encryption (TDE):** TDE encrypts database data-at-rest (`.mdf`/`.ldf` files) and is **not supported** in SQL Server Express.
   - **Native Backup Encryption:** The `BACKUP DATABASE ... WITH ENCRYPTION` clause (which encrypts the backup stream using server certificates or asymmetric keys) is **not supported** in SQL Server Express.
4. **DISASTER RECOVERY ARCHIVE PROTECTION (DPAPI RULE):**
   - Windows DPAPI (`DataProtectionScope.LocalMachine` or `CurrentUser`) binds encryption keys to the specific host machine hardware and OS installation.
   - **Critical Rule:** **Disaster recovery must NOT depend solely on the original machine's DPAPI.**
   - Off-machine disaster recovery archives must be protected using machine-independent mechanisms so that restores can be performed on replacement hardware following catastrophic machine failure. Specific archive technology selection (such as external passphrase-protected tools or encrypted volume schemes) represents future design work for the CBOS 1.3.1+ backup expansion and is explicitly **not a blocker for closing this governance and documentation baseline**. Zero claims of eliminating all operational data-loss risk are made.

---

## 3. Production Backup Procedures

### 3.1 On-Demand Full Database Backup (PowerShell)
Execute the following script as Administrator on the SQL Server machine:

```powershell
$DatabaseName = "Clovent_BusinessOperatingSystem"
$BackupDir = "C:\Backups\CBOS"
$Timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$BackupFile = "$BackupDir\CBOS_Full_$Timestamp.bak"

if (-not (Test-Path $BackupDir)) {
    New-Item -ItemType Directory -Path $BackupDir -Force | Out-Null
}

$SqlQuery = "BACKUP DATABASE [$DatabaseName] TO DISK = N'$BackupFile' WITH INIT, STATS = 10;"

Write-Host "Backing up $DatabaseName to $BackupFile..." -ForegroundColor Cyan
sqlcmd -S "localhost\SQLEXPRESS" -E -Q $SqlQuery

if ($LASTEXITCODE -eq 0) {
    Write-Host "Backup completed successfully." -ForegroundColor Green
    # Compress the uncompressed SQL Express backup:
    Compress-Archive -Path $BackupFile -DestinationPath "$BackupFile.zip" -Force
    Remove-Item -Path $BackupFile -Force
    Write-Host "Compressed backup archive created: $BackupFile.zip" -ForegroundColor Green
} else {
    Write-Error "Backup failed with exit code $LASTEXITCODE."
}
```

### 3.2 Automating Scheduled Daily Backups (Windows Task Scheduler)
1. Save the above script as `C:\Scripts\Backup-CBOS.ps1`.
2. Open **Windows Task Scheduler** -> **Create Basic Task**.
3. Trigger: **Daily at 02:00 AM** (outside operating store hours).
4. Action: **Start a program**:
   - Program: `powershell.exe`
   - Arguments: `-ExecutionPolicy Bypass -File "C:\Scripts\Backup-CBOS.ps1"`
5. Set task to run with elevated privileges (`Run whether user is logged on or not`).

---

## 4. Production Database Restore Procedure

> [!CAUTION]
> **DATA OVERWRITE WARNING:**
> Restoring a database replaces all current data in `Clovent_BusinessOperatingSystem` with the contents of the backup archive. Always take a tail-of-the-log backup or snapshot before restoring.

### Step-by-Step Restore Procedure:
1. **Close All CBOS Clients:** Terminate all running instances of `Clovent.Desktop.exe` across all workstations on the network.
2. **Decompress Archive:** Extract the target `.bak` file from its `.zip` archive.
3. **Set Single-User Mode & Restore:** Execute the restore command using `sqlcmd`:

```powershell
$BackupFile = "C:\Backups\CBOS\CBOS_Full_20261007_020000.bak"
$DatabaseName = "Clovent_BusinessOperatingSystem"

$RestoreSql = @"
USE [master];
ALTER DATABASE [$DatabaseName] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
RESTORE DATABASE [$DatabaseName] FROM DISK = N'$BackupFile' WITH REPLACE, RECOVERY, STATS = 10;
ALTER DATABASE [$DatabaseName] SET MULTI_USER;
"@

sqlcmd -S "localhost\SQLEXPRESS" -E -Q $RestoreSql
```

4. **Verify Schema Compatibility:** Launch CBOS Desktop. The startup `DatabaseSchemaCompatibilityValidator` will verify that the restored database is fully compatible.

---

## 5. Cross References
- [Disaster Recovery Planning](disaster-recovery.md)
- [SQL Server Deployment Guide](../deployment/sql-server.md)
- [Upgrades & Compatibility](../deployment/upgrades.md)
- [Support Runbook: Database Unavailable](../support/runbooks/database-unavailable.md)
