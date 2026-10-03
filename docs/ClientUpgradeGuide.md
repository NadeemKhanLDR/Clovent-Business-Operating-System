# Clovent Business Operating System - Client Update & Upgrade Guide

This guide details the client upgrade process, secret preservation, and installer readiness strategy for Clovent Business Operating System (CBOS).

---

## 1. Upgrade Invariants & State Preservation

When deploying an application update (e.g. from version `1.0.0` to `1.0.1`):
1. **Preserve Database Configuration:**  
   `database.config.json` lives in `%ProgramData%\Clovent\BusinessOperatingSystem\Config\` (outside the application binary folder). It is **NEVER overwritten** during binary updates.
2. **Preserve Customer Software License:**  
   `clovent.lic` resides in `%ProgramData%\Clovent\BusinessOperatingSystem\License\`. Existing valid licenses and hardware bindings remain active without re-activation.
3. **Preserve Transaction History & Business Settings:**  
   Stored securely in SQL Server (`Clovent_BusinessOperatingSystem`). Application updates never execute destructive drops or truncate tables.
4. **Preserve Historical Logs & Audit Records:**  
   Retained in `%ProgramData%\Clovent\BusinessOperatingSystem\Logs\`.

---

## 2. Standard Update Workflow (ZIP Deployment)

1. **Step 1: Shift Close & Exit**  
   Ensure all active cashier shifts are balanced and closed. Exit CBOS on all workstations.
2. **Step 2: Pre-Upgrade Database Backup**  
   Take a full database backup before applying updates:
   ```sql
   BACKUP DATABASE [Clovent_BusinessOperatingSystem] TO DISK = 'C:\ProgramData\Clovent\BusinessOperatingSystem\Backups\pre_upgrade.bak' WITH FORMAT;
   ```
3. **Step 3: Extract New Binaries**  
   Extract the updated release ZIP (e.g., `Clovent.BusinessOperatingSystem-1.0.1-win-x64.zip`) into the application folder (e.g. `C:\Program Files\Clovent\BOS\`).
4. **Step 4: Launch Controlled Migration**  
   Launch the application with administrative rights. The startup schema validator detects pending migrations and applies them safely:
   ```cmd
   Clovent.Desktop.exe --upgrade-database
   ```
5. **Step 5: Verify & Resume Operations**  
   Launch standard workstation desktop shortcuts. Log in and verify POS operation.

---

## 3. Rollback Procedure

If an upgrade cannot proceed or compatibility fails:
1. Revert application binaries to the previous version from backup.
2. If database schema migrations were applied before failure, restore the pre-upgrade database backup:
   ```sql
   ALTER DATABASE [Clovent_BusinessOperatingSystem] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
   RESTORE DATABASE [Clovent_BusinessOperatingSystem] FROM DISK = 'C:\ProgramData\Clovent\BusinessOperatingSystem\Backups\pre_upgrade.bak' WITH REPLACE;
   ALTER DATABASE [Clovent_BusinessOperatingSystem] SET MULTI_USER;
   ```
3. Launch previous version executable and verify workstation connectivity.

---

## 4. Windows Installer Readiness Assessment (MSI / Inno Setup / WiX)

While portable ZIP deployment is acceptable for controlled trials, client commercial rollouts require a managed Windows installer.

### Recommended Technology: **Inno Setup (P1 Recommendation)**
- **Why Inno Setup:**
  - Lightweight, single-file executable output.
  - Native Windows UAC elevation handling during installation.
  - Full support for setting directory ACLs on `%ProgramData%\Clovent\BusinessOperatingSystem\`.
  - Seamless version upgrade without touching ProgramData configuration.
  - Standard Windows Add/Remove Programs integration.
  - Optional Desktop and Start Menu shortcut creation.

### Installer Readiness Checklist:
- [x] Application binaries deployed to `C:\Program Files\Clovent\Business Operating System\`
- [x] Machine configuration and license isolated in `%ProgramData%\Clovent\BusinessOperatingSystem\`
- [x] Start Menu shortcut created pointing to `Clovent.Desktop.exe`
- [x] Desktop shortcut optional (prompted during install)
- [x] Standard Windows Uninstall entry registered in Registry (`HKLM\Software\Microsoft\Windows\CurrentVersion\Uninstall`)
- [x] Uninstaller preserves `%ProgramData%` configuration, licenses, and SQL database by default.
