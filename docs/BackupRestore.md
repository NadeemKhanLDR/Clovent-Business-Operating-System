# Clovent Business Operating System - Database Backup & Restore Readiness

This document defines the production database backup and disaster recovery architecture for Clovent Business Operating System (CBOS).

---

## 1. Architectural Strategy & Role Separation

A critical enterprise requirement is separating routine runtime POS database operations from privileged administrative database maintenance:
1. **Runtime POS User (`cbos_runtime`):**
   - Granted ONLY `db_datareader` and `db_datawriter`.
   - **NOT** granted `db_owner`, `db_ddladmin`, or `sysadmin`.
   - Cannot run `BACKUP DATABASE`, `RESTORE DATABASE`, or alter schema.
2. **Maintenance / Installer Identity (`cbos_installer` or Windows Administrator):**
   - Possesses temporary backup/restore rights (`db_backupoperator` or `sysadmin`).
   - Used during installation, version upgrades, and scheduled database backups.
   - Credentials are never stored permanently in plain text.

---

## 2. Backup Before Upgrade (Mandatory Pre-Migration Guard)

Before applying any schema migration or upgrade to an existing client production database:
1. An automated or DBA-verified backup MUST be completed.
2. The backup file MUST use timestamped naming:
   `Clovent_BusinessOperatingSystem_backup_yyyyMMdd_HHmmss.bak`
3. Backups must **never overwrite** previous backups.
4. Backups are saved to a dedicated, secured backup directory:
   `%ProgramData%\Clovent\BusinessOperatingSystem\Backups\` or a designated corporate backup share.
5. The upgrade wizard verifies the backup file exists and has non-zero size before applying migrations.

### Backup Command Syntax:
```sql
BACKUP DATABASE [Clovent_BusinessOperatingSystem]
TO DISK = N'C:\ProgramData\Clovent\BusinessOperatingSystem\Backups\Clovent_BusinessOperatingSystem_backup_20261001_211500.bak'
WITH FORMAT, INIT, COPY_ONLY, CHECKSUM, STATS = 10;
```

---

## 3. Scheduled Automated Backups

For retail and restaurant POS operations, standard data-protection policy requires:
- **Nightly Full Backup:** At the close of each business day / end-of-day register closure.
- **Hourly Transaction Log Backup (if Full Recovery Model is active):** Retains point-in-time recovery capability during active trading hours.
- **Retention Schedule:** Retain daily backups for 30 days; retain monthly backups for 1 year.

### Recommended Windows Scheduled Task / PowerShell Command:
```powershell
$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$backupDir = "$env:ProgramData\Clovent\BusinessOperatingSystem\Backups"
if (-not (Test-Path $backupDir)) { New-Item -ItemType Directory -Path $backupDir -Force }
$backupFile = Join-Path $backupDir "Clovent_BusinessOperatingSystem_backup_$timestamp.bak"

Invoke-Sqlcmd -ServerInstance "." -Query "BACKUP DATABASE [Clovent_BusinessOperatingSystem] TO DISK = N'$backupFile' WITH FORMAT, INIT, CHECKSUM;"
```

---

## 4. Disaster Recovery & Restore Verification Procedure

To verify backup integrity without impacting live production:
1. **Verification Only (Cheksum):**
   ```sql
   RESTORE VERIFYONLY FROM DISK = N'C:\ProgramData\Clovent\BusinessOperatingSystem\Backups\Clovent_BusinessOperatingSystem_backup_20261001_211500.bak';
   ```
2. **Test Restoration to Drill Database:**
   ```sql
   RESTORE DATABASE [Clovent_BusinessOperatingSystem_DRTest]
   FROM DISK = N'C:\ProgramData\Clovent\BusinessOperatingSystem\Backups\Clovent_BusinessOperatingSystem_backup_20261001_211500.bak'
   WITH MOVE 'Clovent_BusinessOperatingSystem' TO 'C:\SQLData\Clovent_BOS_DRTest.mdf',
        MOVE 'Clovent_BusinessOperatingSystem_log' TO 'C:\SQLData\Clovent_BOS_DRTest.ldf',
        REPLACE, STATS = 10;
   ```
3. Verify data integrity and drop drill database once verified:
   ```sql
   DROP DATABASE [Clovent_BusinessOperatingSystem_DRTest];
   ```

---

## 5. Release Packaging Rule

Per Non-Negotiable #11: **NO database backup files (*.bak) are ever included in release packages.**
All `.bak` files are excluded by `.gitignore` and enforced by `ScanReleasePackage.ps1`.
