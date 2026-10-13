# CBOS Workstation Uninstallation & Data Safety Policy

| Attribute | Details |
| :--- | :--- |
| **Area** | Workstation Decommissioning & Data Retention |
| **Audience** | System Administrators, Support Technicians, Compliance Officers |
| **Last Reviewed Version** | CBOS 1.2.2 |
| **Status** | **PROCEDURAL STANDARD** |
| **Classification** | **PUBLIC-SAFE** |

---

## 1. Ethical Data Preservation Policy

> [!IMPORTANT]
> **FINANCIAL DATA IS NEVER AUTOMATICALLY DELETED:**
> In accordance with commercial financial compliance standards and statutory accounting retention laws, uninstalling Clovent Business Operating System **NEVER deletes, drops, or alters the customer's SQL Server database (`Clovent_BusinessOperatingSystem`)**.
>
> Removing the desktop application from a workstation removes client software binaries only. Financial transaction records, audit ledgers, customer accounts receivable, and inventory movements remain 100% intact within Microsoft SQL Server.

---

## 2. Standard Uninstallation Procedure

To remove the CBOS Desktop client from a Windows workstation:
1. Close all active instances of CBOS Desktop and ensure cashier shifts are reconciled and closed.
2. Open Windows **Settings** -> **Apps** -> **Installed apps** (or **Control Panel** -> **Programs and Features**).
3. Locate **Clovent Business Operating System**.
4. Click **Uninstall** and confirm Windows UAC elevation.
5. The Inno Setup uninstaller (`unins000.exe`) removes:
   - Application binaries in `C:\Program Files\Clovent\Business Operating System\`.
   - Windows Desktop shortcuts and Start Menu entries.
   - Installer registry uninstall keys.

---

## 3. Routine Maintenance & Preserved Data

Routine uninstallation (for example, before a clean reinstallation or troubleshooting) removes **only** the application executables and shortcuts. The uninstaller intentionally leaves the following directories and databases completely intact:

| Preserved Resource | Path / Identifier | Reason for Preservation |
| :--- | :--- | :--- |
| **SQL Server Database** | `[Clovent_BusinessOperatingSystem]` | Primary financial ledger, order history, inventory movements, and customer accounts receivable. |
| **Configuration & License** | `%ProgramData%\Clovent\BusinessOperatingSystem\Config\`<br/>`%ProgramData%\Clovent\BusinessOperatingSystem\License\` | Software license (`clovent.lic`), DPAPI database config (`database.config.json`), and commissioning marker (`Config\commissioning.json`). |
| **Continuity Journal** | `%ProgramData%\Clovent\BusinessOperatingSystem\ContinuityJournal\journal.dat` | HMAC/DPAPI protected offline sales journal awaiting reconciliation. |
| **Diagnostic Logs** | `%LOCALAPPDATA%\Clovent\Logs\`<br/>`%ProgramData%\Clovent\BusinessOperatingSystem\Logs\` | Application and installer diagnostic logs for support audits. |
| **Microsoft SQL Server Engine** | SQL Server Windows Services | Database instance, logins, and service configuration remain untouched. |

> [!TIP]
> **Reinstallation Workflow:** To reinstall or upgrade CBOS on the same workstation, simply run the new installer package. It connects to the existing database and configuration seamlessly without data loss.

---

## 4. Quarantined Procedure: Complete Machine Decommissioning & Sanitization

> [!CAUTION]
> **CRITICAL: IRREVERSIBLE FINANCIAL DATA DESTRUCTION**
>
> The commands in this section are **STRICTLY QUARANTINED** for permanent physical hardware disposal, device recycling, or returning leased equipment to a vendor.
>
> **DO NOT** execute these steps during routine uninstallation, clean reinstallation, or software troubleshooting. Executing this section will **PERMANENTLY AND IRREVERSIBLY ERASE** all customer sales orders, tax records, payment ledgers, and audit journals.

### Mandatory Prerequisites Prior to Decommissioning:
1. **Written Customer Authorization:** Explicit written sign-off from the store owner or corporate financial officer authorizing permanent data sanitization.
2. **Verified Recoverable Backup:** Execute a full database backup (`.bak`), transfer the file to secure external/cloud corporate storage, and **verify that the backup can be successfully restored** on a separate server before proceeding.

### Decommissioning Steps (Hardware Disposal Only):

1. **Perform Verified Off-Machine Backup:**
   ```powershell
   sqlcmd -S "localhost\SQLEXPRESS" -E -Q "BACKUP DATABASE [Clovent_BusinessOperatingSystem] TO DISK = 'C:\Backups\CBOS_FINAL_DECOMMISSION_$(Get-Date -Format 'yyyyMMdd_HHmmss').bak' WITH INIT, STATS = 10;"
   ```
   *Transfer this `.bak` file to external secure storage.*

2. **Deactivate / Record Software License:**
   Export the customer license details (`clovent.lic`) and hardware ID for re-issuance on replacement hardware.

3. **Uninstall CBOS Client Software:**
   Execute standard uninstallation via Windows Control Panel / Settings.

4. **Drop SQL Database (Manual DBA Action):**
   *Execute only after verifying the off-machine backup above:*
   ```sql
   ALTER DATABASE [Clovent_BusinessOperatingSystem] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
   DROP DATABASE [Clovent_BusinessOperatingSystem];
   ```

5. **Purge ProgramData and Local Application State:**
   ```powershell
   Remove-Item -Path "C:\ProgramData\Clovent" -Recurse -Force -ErrorAction SilentlyContinue
   Remove-Item -Path "$env:LOCALAPPDATA\Clovent" -Recurse -Force -ErrorAction SilentlyContinue
   ```

6. **Cryptographic Disk Sanitization:**
   Execute NIST SP 800-88 compliant drive sanitization before releasing physical hardware.

---

## 5. Cross References
- [Installation Guide](installation.md)
- [Backup & Restore Procedures](../operations/backup-and-restore.md)
- [Disaster Recovery Planning](../operations/disaster-recovery.md)
