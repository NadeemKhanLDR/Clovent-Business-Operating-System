# CBOS Database Backup & Disaster Recovery Procedures

| Attribute | Details |
| :--- | :--- |
| **Area** | Database Administration & Disaster Preparedness |
| **Audience** | Database Administrators, Systems Engineers, Store Operators |
| **Last Reviewed Version** | CBOS 1.2.2 |
| **Status** | **PROCEDURAL STANDARD** |
| **Classification** | **PUBLIC-SAFE** |

---

## 1. Backup Architecture: Current Capability vs. Planned Evolution

> [!IMPORTANT]
> **SEPARATION OF CURRENT VS. PLANNED CAPABILITIES:**
> - **CURRENT CAPABILITY (CBOS 1.2.2):**
>   1. **Pre-Upgrade On-Demand Backup:** CBOS Desktop includes `DatabaseBackupService` (`src/Clovent.Desktop/Commissioning/Database/DatabaseBackupService.cs`), which executes an on-demand full database backup (`BACKUP DATABASE ... WITH COPY_ONLY, FORMAT, INIT`) prior to commissioning and schema upgrades.
>   2. **Manual & Scripted Backups:** Routine operational backups rely on external PowerShell scripts, `sqlcmd`, or SQL Server Management Studio (SSMS).
> - **KNOWN LIMITATION (CBOS 1.2.2):**
>   - `CloventMaintenanceService` is **NOT** implemented in the codebase.
>   - Automatic background scheduled backups do **NOT** exist in the product.
>   - Automatic restore workflows and automated restore drills do **NOT** exist in the product.
>   - Cloud backup replication does **NOT** exist.
> - **PLANNED ARCHITECTURE (CBOS 1.3.1+):**
>   - An automated background maintenance service providing scheduled verified backups, retention policy purging, automated restore verification drills, and optional encrypted offsite archival.

---

## 2. Technical Constraints for SQL Server Express Backups

When deploying on Microsoft SQL Server Express (the standard standalone single-store configuration), administrators must account for three native platform constraints:
1. **NO SQL SERVER AGENT:** SQL Server Express does **not** include the SQL Server Agent service. Scheduled automated backups **cannot** be scheduled via SQL Agent jobs. **They must be scheduled via Windows Task Scheduler using PowerShell or batch scripts.**
2. **NO NATIVE BACKUP COMPRESSION:** The `WITH COMPRESSION` option is disabled by Microsoft in SQL Server Express. Backups produced by `BACKUP DATABASE` are uncompressed. Administrators should compress `.bak` files using external compression tools (e.g., PowerShell `Compress-Archive` or 7-Zip).
3. **NO NATIVE BACKUP ENCRYPTION:** Transparent backup encryption (`ENCRYPTION = ...`) is unavailable in Express. Backups should reside on BitLocker-encrypted drives or DPAPI-encrypted storage.

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
