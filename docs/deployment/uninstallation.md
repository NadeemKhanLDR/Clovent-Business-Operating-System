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

## 3. Preserved Directories & Files

The uninstaller intentionally leaves the following directories intact to protect customer business continuity:

| Preserved Path | Reason for Preservation |
| :--- | :--- |
| **`Clovent_BusinessOperatingSystem`** (SQL Server Database) | Primary financial ledger, order history, inventory, and accounts receivable data. |
| **`%ProgramData%\Clovent\BusinessOperatingSystem\`** | Contains software license keys (`clovent.lic`), commissioning records (`commissioned.json`), and emergency continuity journals (`continuity_journal.dat`). |
| **`%LOCALAPPDATA%\Clovent\Logs\`** | Diagnostic logs for historical troubleshooting and support audits. |
| **Microsoft SQL Server Engine** | The SQL Server service and instance remain untouched. |

---

## 4. Complete Machine Decommissioning (Sanitization)

If a physical POS computer is being permanently disposed of, recycled, or returned to a hardware vendor:
1. **Take Final Database Backup:** Produce a full `.bak` database backup and transfer it to secure corporate offline backup storage.
2. **Deactivate / Export License:** Record license credentials for re-issuance on the replacement workstation.
3. **Uninstall CBOS Client:** Execute standard uninstallation as described above.
4. **Drop SQL Database (Manual DBA Action):**
   ```sql
   DROP DATABASE [Clovent_BusinessOperatingSystem];
   ```
5. **Purge ProgramData Records:** Manually delete `C:\ProgramData\Clovent\` and `C:\Users\<User>\AppData\Local\Clovent\`.
6. **Execute Cryptographic Disk Sanitization:** Perform NIST 800-88 compliant drive wipe prior to hardware disposal.

---

## 5. Cross References
- [Installation Guide](installation.md)
- [Backup & Restore Procedures](../operations/backup-and-restore.md)
- [Disaster Recovery Planning](../operations/disaster-recovery.md)
