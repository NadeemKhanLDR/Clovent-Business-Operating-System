# Clovent Business Operating System - Installer & Deployment Architecture

**Version:** 1.1.2 (Current) | 1.1.1, 1.1.0, 1.0.8, 1.0.7 (Frozen Baselines Preserved)  
**Authoritative Reference:** [AGENTS.md](../AGENTS.md) | [winforms-ui.md](../.agents/rules/winforms-ui.md) | [database.md](../.agents/rules/database.md) | [security.md](../.agents/rules/security.md) | [release.md](../.agents/rules/release.md)

---

## 1. Overview & Objective

The CBOS 1.1.2 single-file installer provides a frictionless, enterprise-grade deployment experience for retail and hospitality workstations. The end customer receives **one single installer executable**:

```text
artifacts\installer\Clovent.BusinessOperatingSystem-1.1.2-Setup.exe
```
*(Note: The frozen 1.0.7, 1.0.8, 1.1.0, and 1.1.1 release installers are permanently preserved).*

The installer orchestrates complete workstation onboarding without requiring manual operator intervention:
- **No manual .NET installation:** The CBOS 1.1.2 payload is fully self-contained (`win-x64`).
- **No manual SQL Server installation:** Local SQL Server instances are detected automatically; if none exist, Microsoft SQL Server 2022 Express is installed silently.
- **No manual SQL scripts:** Database creation, schema migrations, and payment method seeding are executed via CBOS's production C# provisioning services (`Clovent.Installer.Provisioner.exe` compiled as a true self-contained single-file win-x64 executable).
- **No plaintext configuration editing:** Machine-level database settings and directory ACLs are configured and encrypted via Windows DPAPI.
- **No default credentials:** Real customers provision their own enterprise hierarchy and first administrator via the First-Run Commissioning Wizard.
- **Responsive High-DPI First-Run Commissioning Wizard:** Automatically scales across 100%–250% display scaling and 1366x768 to 1920x1080+ resolutions without control clipping, label compression, or dialog under-sizing.

---

## 2. Installer Architecture & Technology

### Technology Selection: Inno Setup 6 (Native x64 Bootstrapper)
- **Engine:** Inno Setup 6.7.3 (Native Win32/x64 PE, zero external runtime dependencies).
- **Compression:** LZMA2/max solid compression (~101 MB package containing the full 350 MB self-contained application payload and provisioner).
- **Elevation Model:** `PrivilegesRequired=admin` with UAC elevation manifest, ensuring full administrative privileges for SQL Server configuration and directory ACL enforcement.
- **Prerequisite Chaining:** Integrated Pascal Script (`[Code]`) manages SQL Server instance probing, prerequisite acquisition, service status verification, and production provisioning execution.
- **Uninstaller Registration:** Full integration with Windows Apps & Features (`Software\Microsoft\Windows\CurrentVersion\Uninstall`).

### Directory Layout
| Resource | Target Path | Access Control Lists (ACLs) |
|---|---|---|
| **Application Binaries** | `C:\Program Files\Clovent\Business Operating System\` | Administrators: Full; Users: Read & Execute |
| **Machine Config** | `%ProgramData%\Clovent\BusinessOperatingSystem\Config\` | Administrators: Full; Users: Read-Only |
| **Machine License** | `%ProgramData%\Clovent\BusinessOperatingSystem\License\` | Administrators: Full; Users: Read-Only |
| **Rolling Logs** | `%ProgramData%\Clovent\BusinessOperatingSystem\Logs\` | Administrators: Full; Users: Read / Write / Modify |
| **Machine State** | `%ProgramData%\Clovent\BusinessOperatingSystem\State\` | Administrators: Full; Users: Read-Only |

---

## 3. SQL Server Detection & Automatic Installation Policy

### A. Detection Precedence
On launch, the installer inspects the local machine for compatible Microsoft SQL Server instances in the following order:
1. **Dedicated Named Instance (`.\CLOVENT`):** Checked via `HKLM\SOFTWARE\Microsoft\Microsoft SQL Server\Instance Names\SQL` and Windows Service `MSSQL$CLOVENT`.
2. **Default Instance (`(local)` / `localhost`):** Checked via registry and Windows Service `MSSQLSERVER`.
3. **Standard Express Instance (`.\SQLEXPRESS`):** Checked via registry and Windows Service `MSSQL$SQLEXPRESS`.
4. **Any Custom Local Instance:** Any other running instance registered in the SQL Server instance registry key.

If any compatible instance is found:
- The installer **will not** install a redundant database engine.
- If the service is stopped, the installer starts the service and verifies its `RUNNING` state before continuing.

### B. Automatic Installation of SQL Server 2022 Express
If no compatible local SQL Server exists on the workstation:
1. **Prerequisite Acquisition:**
   - The installer checks if `SQLEXPR_x64_ENU.exe` exists beside `Setup.exe` or in a `prerequisites\` subfolder.
   - If not found locally, the installer automatically downloads Microsoft SQL Server 2022 Express from Microsoft's official CDN:
     `https://download.microsoft.com/download/3/8/d/38de7036-2433-4207-8eae-06e247e17b25/SQLEXPR_x64_ENU.exe`
2. **Silent Unattended Execution:**
   The database engine is installed using Microsoft's approved unattended setup flags:
   ```cmd
   SQLEXPR_x64_ENU.exe /QS /ACTION=Install /FEATURES=SQLEngine /INSTANCENAME=CLOVENT /SQLSVCSTARTUPTYPE=Automatic /SQLSYSADMINACCOUNTS="BUILTIN\Administrators" /TCPENABLED=0 /NPENABLED=1 /IACCEPTSQLSERVERLICENSETERMS
   ```
   - **Instance Name:** `CLOVENT`
   - **Startup:** Automatic Windows Service (`MSSQL$CLOVENT`)
   - **Network Exposure:** TCP disabled (`TCPENABLED=0`); local Named Pipes enabled (`NPENABLED=1`) for secure local standalone POS communication.
   - **Admin Access:** Local Administrators group (`BUILTIN\Administrators`).
3. **Verification:**
   The installer monitors the Service Control Manager until `MSSQL$CLOVENT` reaches status `RUNNING`.

---

## 4. Production Database Provisioning Pipeline

The installer does not maintain fragile, ad-hoc `CREATE TABLE` scripts. Instead, it embeds and invokes a dedicated self-contained tool: `Clovent.Installer.Provisioner.exe`.

### Packaging Architecture (CBOS 1.1.2 Single-File):
- **Standalone Executable:** Compiled with `<PublishSingleFile>true</PublishSingleFile>`, `<RuntimeIdentifier>win-x64</RuntimeIdentifier>`, `<SelfContained>true</SelfContained>`, `<IncludeNativeLibrariesForSelfExtract>true</IncludeNativeLibrariesForSelfExtract>`, and `<EnableCompressionInSingleFile>true</EnableCompressionInSingleFile>`.
- **Zero External DLL Dependencies:** Bundles .NET 10 BCL, Entity Framework Core 10, Microsoft.Data.SqlClient, and all 6 bounded context assemblies into a single ~44.6 MB executable. Does NOT rely on adjacent `.dll`, `.deps.json`, or `.runtimeconfig.json` files.
- **Root Cause & Fix for Exit Code `-2147450726` (`0x8000809A`):** In CBOS 1.1.1, the installer packaged only the 162 KB AppHost stub without managed binaries, causing .NET HostFXR to fail immediately before entering `Program.cs`. In CBOS 1.1.2, the true single-file bundle guarantees complete standalone execution from `{tmp}`.
- **Working Directory Enforcement:** Inno Setup's `Exec()` passes `ExpandConstant('{tmp}')` as the working directory, ensuring predictable host process initialization.

### Provisioning Sequence:
1. **Directory Security:**
   Calls `ProgramDataAclManager.ConfigureDirectorySecurity()`, establishing `%ProgramData%\Clovent\BusinessOperatingSystem\` with hardened ACLs.
2. **Connection Probe:**
   Calls `DatabaseProvisioningService.TestConnectionAsync()` with 5 retry attempts (2-second backoff) to account for database engine spin-up.
3. **Database Creation:**
   Calls `DatabaseProvisioningService.CreateDatabaseAsync()`. If `Clovent_BusinessOperatingSystem` already exists, it verifies existence idempotently.
4. **EF Core Migrations & Persistence Initializers:**
   Sequentially executes migrations and schema initializers across all 6 bounded contexts:
   - `[Authentication].[__EFMigrationsHistory]` & `AuthenticationPersistenceInitializer`
   - `[Identity].[__EFMigrationsHistory]` & `IdentityPersistenceInitializer`
   - `[MasterData].[__EFMigrationsHistory]` & `MasterDataPersistenceInitializer`
   - `[Catalog].[__EFMigrationsHistory]` & `CatalogPersistenceInitializer`
   - `[Inventory].[__EFMigrationsHistory]` & `InventoryPersistenceInitializer`
   - `[Restaurant].[__EFMigrationsHistory]`, `RestaurantPersistenceInitializer`, and `PaymentMethodSeeder`
5. **Payment Method Verification & Deduplication:**
   Verifies that core required payment methods exist and are active, deduplicating any legacy records:
   - `Cash` (Active, exactly 1 record)
   - `Card` (Active, exactly 1 record)
   - `On Account` (Active, exactly 1 record)
6. **Encrypted Configuration Persistence:**
   Calls `DatabaseSecretStore.Save(settings, machineLevel: true)`, encrypting connection secrets via Windows DPAPI (`DataProtectionScope.LocalMachine`) in `%ProgramData%\Clovent\BusinessOperatingSystem\Config\database.config.json`.
7. **Schema Compatibility Gate:**
   Executes `DatabaseSchemaCompatibilityValidator.ValidateCompatibilityAsync()` to guarantee the resulting schema is 100% `Compatible`.

---

## 5. First-Run Commissioning Experience

Once the technical installer completes and launches `Clovent.Desktop.exe`:
1. Signals 1 & 2 (database connectivity and schema migrations) are satisfied.
2. Because operational master data and the first administrator do not yet exist, `FirstRunWizardForm` launches automatically:
   - **Step 1:** System Overview & Diagnostics.
   - **Step 2:** Database Connection (pre-populated with verified installer settings; user clicks Next).
   - **Step 3:** Schema Status (pre-verified; user clicks Next).
   - **Step 4:** Enterprise Hierarchy (Customer enters Organization, Company, and Branch names).
   - **Step 5:** First Administrator (Customer creates unique admin credentials; default passwords like `Admin123!` are rejected).
   - **Step 6:** Regional Settings (Timezone, date/time format, currency, terminal name).
   - **Step 7:** Software Licensing (Displays machine Hardware ID; allows importing signed customer license).
   - **Step 8:** Review & Finalize (Writes protected `commissioning.json` marker).
3. The operator is immediately directed to the standard CBOS Sign-In screen.

### High-DPI & Multi-Resolution Sizing Architecture
To ensure seamless onboarding across varying clean-machine environments (e.g., Windows Sandbox, remote sessions, 4K POS terminals, 100%–250% DPI scaling):
- **Dynamic Screen-Proportional Geometry:** On load, the wizard computes its initial bounds relative to `Screen.FromControl(this).WorkingArea`. It targets 72% width and 76% height of the usable screen, clamping between a scaled minimum size (`980x660` baseline scaled via `DesktopDpi.Scale`) and a maximum limit of 94% of the working area.
- **Omission of AutoScaleMode in Designer:** In adherence to AGENTS.md Rule 5 and PerMonitorV2 requirements, `AutoScaleMode.Font` and `AutoScaleDimensions` are omitted in `FirstRunWizardForm.Designer.cs` to prevent Windows Forms coordinate recomputation and double-scaling artifacts.
- **Sizable Window with Full Maximize Support:** `FormBorderStyle = FormBorderStyle.Sizable` and `MaximizeBox = true` give operators full control over dialog geometry.
- **AutoScroll Protection:** `panelContainer` and all 8 step content panels have `AutoScroll = true` enabled, preventing control cutoff or hidden buttons even in constrained virtual viewports.
- **Responsive Layout Engine (`ApplyResponsiveLayout`):**
  - **Sidebar:** Scaled to 240px logical baseline, dynamically distributing the 8 step indicator labels across available vertical space.
  - **Header:** Scaled to 78px logical baseline with properly padded, wrapped titles.
  - **Footer:** Scaled to 60px logical baseline with right-aligned action buttons (`btnCancel`, `btnBack`, `btnNext`, `btnFinish`). The status label `lblFooterStatus` is bounded and auto-ellipsized between the step counter and cancel button, preventing horizontal collisions.
  - **Step Panels:** Dynamically positions labels, text boxes, and buttons using DPI-scaled control heights and gutters with responsive multiline wrapping.

---

## 6. Upgrade, Reinstall & Uninstall Policies

### Upgrades & Reinstalls
- When running `Setup.exe` on a workstation with an existing CBOS installation:
  - The installer detects the existing version via Windows Registry.
  - Binaries in `C:\Program Files\Clovent\Business Operating System\` are updated in-place.
  - The database provisioner executes migrations idempotently.
  - **Zero Data Loss:** Existing tables, sales records, customers, ledger entries, administrators, settings, and licenses are strictly preserved.

### Uninstallation
- Removing CBOS via Windows Settings / Control Panel uninstalls:
  - Application binaries in `C:\Program Files\Clovent\Business Operating System\`
  - Start Menu and Desktop shortcuts
  - Windows registry uninstall keys
- **Non-Destructive Guarantee:**
  - The SQL Server database `Clovent_BusinessOperatingSystem` is **NEVER** deleted.
  - Database backups are **NEVER** deleted.
  - License files and configuration in `%ProgramData%\Clovent\BusinessOperatingSystem\` are preserved.
  - The SQL Server engine service is preserved.

---

## 7. Logging & Diagnostic Support

- **Installer Engine Log:** `%ProgramData%\Clovent\BusinessOperatingSystem\Logs\Setup-1.1.2.log` (Setup logs details including provisioner path, working directory, and exit codes)
- **Database Provisioner Log:** `%ProgramData%\Clovent\BusinessOperatingSystem\Logs\installer-provisioning.log`
- **Application Startup Log:** `%ProgramData%\Clovent\BusinessOperatingSystem\Logs\application.log`
- **Zero Secrets Rule:** All connection strings and passwords are systematically masked (`***`) before being emitted to log files.
- **Support-Useful Exit Code Classification:**
  - Exit code `-2147450726` (`0x8000809A`): Missing .NET runtime or application host dependency.
  - Exit code `1`: SQL Server connection failure.
  - Exit code `2`: Database creation failure.
  - Exit code `3`: Migration execution or seed data failure.
  - Exit code `4`: Post-migration schema compatibility failure.
  - Exit code `99`: Unhandled exception during provisioning.

---

## 8. Authenticode Signing Specification

When commercial EV Code Signing credentials are provided, sign the installer using Windows `signtool.exe`:

```cmd
signtool.exe sign /tr http://timestamp.digicert.com /td sha256 /fd sha256 /a "artifacts\installer\Clovent.BusinessOperatingSystem-1.1.2-Setup.exe"
```

To verify the Authenticode signature:
```cmd
signtool.exe verify /pa /v "artifacts\installer\Clovent.BusinessOperatingSystem-1.1.2-Setup.exe"
```
