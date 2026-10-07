# Support Runbook: Database Schema Mismatch & Upgrade

| Attribute | Details |
| :--- | :--- |
| **Area** | Support Runbook / Database Migrations |
| **Audience** | Level 2–3 Support Technicians, Field Engineers |
| **Last Reviewed Version** | CBOS 1.2.2 |
| **Status** | **PROCEDURAL RUNBOOK** |
| **Classification** | **PUBLIC-SAFE** |

---

## 1. Problem Description
On application launch, CBOS halts with one of the following dialogs:
- **`DatabaseTooOld`:** `"Database Schema Update Required: The database schema is older than the current application version..."`
- **`DatabaseNewer`:** `"Database Newer Than Application: The database schema is newer than this application executable..."`

---

## 2. Step-by-Step Diagnostic & Resolution Procedure

### Scenario A: Resolving `DatabaseTooOld` (Pending Migrations)

1. **Step 1: Take Mandatory Database Backup:**
   Before applying schema migrations, produce a backup via PowerShell:
   ```powershell
   sqlcmd -S "localhost\SQLEXPRESS" -E -Q "BACKUP DATABASE [Clovent_BusinessOperatingSystem] TO DISK = 'C:\Backups\CBOS_PreMigration.bak' WITH INIT;"
   ```

2. **Step 2: Run the Single-File Database Provisioner:**
   Open PowerShell as Administrator on the workstation:
   ```powershell
   cd "C:\Program Files\Clovent\Business Operating System"
   .\Clovent.Installer.Provisioner.exe --migrate
   ```

3. **Step 3: Inspect Provisioner Output:**
   The provisioner will output:
   ```text
   [INFO] Discovered 6 bounded contexts.
   [INFO] Applying pending migrations for [Restaurant]...
   [INFO] Applying pending migrations for [Catalog]...
   [SUCCESS] All schema migrations applied successfully. ExitCode: 0
   ```

4. **Step 4: Launch CBOS Desktop:**
   Launch `Clovent.Desktop.exe`. `DatabaseSchemaCompatibilityValidator` will report `SchemaCompatibilityStatus.Compatible` and startup proceeds.

---

### Scenario B: Resolving `DatabaseNewer` (Outdated Client Binary)

1. **Root Cause:** A newer version of CBOS was deployed to the database server or another terminal, updating the SQL schema, but the local terminal is running an older client executable.
2. **Resolution:**
   - Deploy and run the latest setup installer (`Clovent.BusinessOperatingSystem-1.2.2-Setup.exe`) on this terminal workstation.
   - Verify that all workstations across the store LAN run identical software versions.
