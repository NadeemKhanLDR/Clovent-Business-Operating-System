# CBOS Customer Deployment & Support Documentation Review Report

- **Review Role:** CBOS Customer Deployment & Support Documentation Reviewer
- **Target Repository:** Clovent Business Operating System (CBOS)
- **Pinned Git Revision:** `b428ad5270a47516189c776c8987a39c6864daa2`
- **Active Git Branch:** `fix/refund-allocation-and-ui-acceptance`
- **Working Tree State:** Clean (Zero uncommitted changes; read-only inspection)
- **Review Date:** October 13, 2026 (Updated with Review Corrections)
- **Review Scope:** Deployment, onboarding, daily operations, peripherals, data protection, upgrades, support runbooks, and documentation governance.

---

## 1. Executive Summary & Boundaries

This independent review evaluates whether a **shop owner**, **installer technician**, and **field support engineer** can locate clear, accurate, and actionable instructions to install, configure, operate, and troubleshoot the Clovent Business Operating System (CBOS).

### Review Boundaries & Methodological Clarifications:
- **Strict Read-Only Source Inspection:** Evaluated committed source code, installer scripts, and documentation at pinned revision `b428ad5270a47516189c776c8987a39c6864daa2`.
- **Source Inspection vs. Runtime Verification:** Source code inspection confirms structural implementation, contracts, UI controls, and logic, but does **NOT** prove clean-machine runtime execution, display scaling fidelity, hardware peripheral compatibility, or operating system privileges. Real runtime evidence outranks simulation; runtime acceptance is strictly owned by the active acceptance agent executing Windows Sandbox qualification.
- **Accounting for Both Installer Scripts:**
  - `installer/Clovent.BusinessOperatingSystem.iss`: Baseline installer script for CBOS 1.2.2 (frozen internal baseline).
  - `Tools/installer/cbos-setup.iss`: Authoritative installer script for CBOS 1.3.0 Release Candidate pilot deployments (referenced by packaging pipelines and acceptance build commands).
- **Withdrawal of Unsupported Claims:**
  - *Hardware & Display:* Prescriptive hardware sizing, multi-monitor behavior, and Per-Monitor High-DPI scaling (100%–250%) are design targets confirmed in source code, but runtime rendering without clipping or blur requires interactive physical display acceptance.
  - *Code Signing & Hashing:* Code signing private keys remain external; installer Authenticode signatures and `SHA256SUMS.txt` generation remain pending clean-machine qualification.
  - *Backup Automation:* CBOS possesses no built-in native automated backup scheduling engine. Automated backups require external operating system scheduling (e.g., Windows Task Scheduler or SQL Server Agent), which is an operator responsibility.
  - *Security Certification:* The codebase enforces defense-in-depth principles (Windows DPAPI, PBKDF2/Argon2 hashing, RBAC), but possesses no third-party security certification or PCI-DSS attestation.
- **Preservation of Approved Release Milestones:**
  - **CBOS 1.2.2:** Frozen Internal Acceptance Baseline (explicitly NOT paid-pilot ready, NOT commercial GA).
  - **CBOS 1.2.3:** Controlled Attended Single-Terminal Pilot Hardening (strict zero database migrations).
  - **CBOS 1.3.0:** First Broader Commercial Foundation (commercial GA target; incorporates compensating refund domain, optimistic concurrency, and high-DPI). Active candidate qualification testing in Windows Sandbox evaluates 1.3.0 candidate capabilities, but does not itself redefine the 1.2.3 pilot or 1.3.0 commercial milestones.

---

## 2. Documentation Coverage Matrix

Evaluated across four standard ratings:
- **`COMPLETE`**: Clear, factual, step-by-step instructions exist and match committed source code contracts.
- **`PARTIAL`**: Architectural or code-level explanations exist, but operational step-by-step guidance for technicians or users is incomplete or fragmented.
- **`MISSING`**: Required operational, administrative, or cashier workflow instructions do not exist in the documentation suite.
- **`CONTRADICTORY`**: Documented instructions conflict with other documents or directly contradict committed source code behavior.

*(Note: Ratings evaluate documentation alignment with committed source contracts. Runtime verification remains pending acceptance sign-off.)*

| Domain | Topic | Status | Document & Source References | Summary Finding |
| :--- | :--- | :---: | :--- | :--- |
| **1. Installation & First Use** | Supported OS & Hardware Prerequisites | `CONTRADICTORY` | `docs/deployment/installation.md`<br/>`docs/DeploymentGuide.md` | `installation.md` specifies 4 GB RAM / 10 GB disk with Inno Setup; `DeploymentGuide.md` specifies 8 GB RAM / 2 GB disk with manual zip extraction. Benchmark-verified sizing guidelines are unverified. |
| | Installer Steps (GUI & Silent) | `PARTIAL` | `docs/deployment/installation.md`<br/>`installer/Clovent.BusinessOperatingSystem.iss`<br/>`Tools/installer/cbos-setup.iss` | Documentation references `1.2.2-Setup.exe` from `installer/`; the authoritative 1.3.0 RC installer script resides at `Tools/installer/cbos-setup.iss` with configurable `#define` flags. |
| | First-Run Commissioning Wizard | `CONTRADICTORY` | `docs/deployment/commissioning.md`<br/>`docs/FirstRunCommissioning.md`<br/>`src/Clovent.Desktop/Commissioning/UI/FirstRunWizardForm.cs` | `commissioning.md` documents a 9-phase flow and marker `commissioned.json`; code and `FirstRunCommissioning.md` implement an 8-step wizard saving to `Config\commissioning.json`. |
| | Administrator vs. Cashier Setup | `MISSING` | `docs/deployment/commissioning.md`<br/>`src/Clovent.Desktop/Forms/Identity/Users/UsersForm.cs` | Commissioning Step 5 provisions the initial Administrator only; documentation lacks guidance on creating Cashiers, assigning roles, or setting POS PINs in Back Office. |
| | Org, Branch & Terminal Setup | `COMPLETE` | `docs/deployment/commissioning.md`<br/>`docs/configuration/terminal-identity.md` | Wizard Steps 4 and 6 capture company, branch, and terminal settings and persist to `%LOCALAPPDATA%\Clovent\pos_settings.json`. |
| | First Login Procedure | `COMPLETE` | `docs/deployment/commissioning.md`<br/>`docs/security/authentication.md`<br/>`src/Clovent.Desktop/Login/LoginForm.cs` | Post-commissioning transition to `LoginForm` with Username/Password and PIN authentication tabs is clearly documented. |
| **2. Everyday Cashier Operations** | Opening Shifts & Starting Float | `PARTIAL` | `docs/pos/shifts-and-cash-management.md`<br/>`src/Clovent.Restaurant/Shifts/Shift.cs` | Domain models and reconciliation invariants are thoroughly detailed, but an end-user cashier visual guide for shift opening and float entry is missing. |
| | Taxable vs. Non-Taxable Items | `MISSING` | `docs/releases/cbos-1.3.0-release-notes.md`<br/>`src/Clovent.Catalog/TaxProfiles/TaxProfile.cs` | `TaxProfile` entity (Taxable, Zero-Rated, Exempt, Out-of-Scope; Inclusive/Exclusive) is implemented in code, but user documentation for setting up tax rules is absent. |
| | Applying Order Discounts | `PARTIAL` | `docs/architecture/OrderLifecycle.md`<br/>`src/Clovent.Desktop/Restaurant/Orders/DiscountDialog.cs` | Percentage and fixed discounts are modeled in code, but no user-facing cashier runbook explains how to trigger `DiscountDialog`. |
| | Split Tender Payments | `COMPLETE` | `docs/pos/payments.md`<br/>`src/Clovent.Desktop/Restaurant/Orders/BillSplitDialog.cs` | Relational split payment (Cash + Card, Cash + On Account) is well documented and supported by `BillSplitDialog` in source. |
| | Customer Credit ("On Account") | `COMPLETE` | `docs/pos/payments.md`<br/>`docs/architecture/adr/ADR-005-Customer-Credit-Workflow.md` | Credit limit checks, fail-closed manager elevation, and single-entry customer subledger postings are comprehensively specified. |
| | Partial & Full Refunds | `CONTRADICTORY` | `docs/known-limitations.md`<br/>`docs/pos/order-lifecycle.md`<br/>`docs/releases/cbos-1.3.0-release-notes.md`<br/>`src/Clovent.Desktop/Restaurant/Orders/RefundOrderDialog.cs` | 1.2.2 docs declare refunds unbuilt and prohibited; 1.3.0 notes announce refund domain models; code implements `RefundOrderDialog` under Operations menu. Active acceptance is currently pending. |
| | Receipt Reprints | `PARTIAL` | `docs/architecture/OrderLifecycle.md`<br/>`src/Clovent.Desktop/Restaurant/Orders/RestaurantPosForm.cs` | `Print Last Receipt` command exists in source, but instructions do not clarify that it is strictly scoped to the active shift's last order. |
| | Day-Close Reconciliation | `PARTIAL` | `docs/pos/shifts-and-cash-management.md`<br/>`docs/architecture/EndOfDayReporting.md` | Backend formulas, shift variances, and Day Close dialog constraints are documented, but no front-of-house closing checklist is provided for store managers. |
| **3. Printers & Peripherals** | Logical Printer Selection | `MISSING` | `docs/integrations/printing.md`<br/>`src/Clovent.Desktop/Forms/Settings/PrinterSettingsForm.cs` | `PrinterSettingsForm` provides logical profile management and Windows print queue binding, but is completely omitted from platform documentation. |
| | Multi-Printer Routing | `PARTIAL` | `docs/integrations/printing.md`<br/>`src/Clovent.Platform/Printing/PrinterRole.cs` | Code implements `PrinterRole` (`Receipt`, `Invoice`, `Kitchen`, `Bar`, `Label`, `Report`), but documentation does not instruct technicians how to configure routing. |
| | Paper Sizes (80mm / 58mm / A4) | `MISSING` | `docs/integrations/printing.md`<br/>`src/Clovent.Desktop/Printing/EnhancedReceiptPrintDocument.cs` | Multi-width paper support is implemented in UI dropdowns and document renderers, but documentation only describes generic GDI printing. |
| | Test Print Execution | `MISSING` | `src/Clovent.Desktop/Forms/Settings/PrinterSettingsForm.cs` | Form features Preview Test and Physical Test Print buttons, but neither is documented in setup guides. |
| | Failed Print Recovery & Quarantine | `PARTIAL` | `docs/support/runbooks/printer-failure.md`<br/>`src/Clovent.Desktop/Printing/PrinterHardwareHealthView.cs` | Runbook describes basic Windows spooler restarts, but omits the built-in `PrinterHardwareHealthView` and quarantined job replay features. |
| | Cash Drawer Trigger Setup | `PARTIAL` | `docs/integrations/printing.md`<br/>`src/Clovent.Platform/Printing/PrinterProfile.cs` | Direct ESC/POS pulse kick is unverified in code; cash drawer trigger relies on printer driver properties via RJ11/RJ12 cable. |
| **4. Data Protection & Upgrades** | Backup Ownership & Location | `CONTRADICTORY` | `docs/operations/backup-and-restore.md`<br/>`docs/BackupRestore.md` | `backup-and-restore.md` specifies `sqlcmd` to `C:\Backups\CBOS` (`cbos_app`); `BackupRestore.md` specifies `Invoke-Sqlcmd` to `%ProgramData%\...\Backups` (`cbos_runtime`). |
| | Database Restore Instructions | `COMPLETE` | `docs/operations/backup-and-restore.md` | Full, safe step-by-step restore script using `SINGLE_USER WITH ROLLBACK IMMEDIATE` is clearly documented. |
| | Upgrade Prerequisites & Steps | `CONTRADICTORY` | `docs/deployment/upgrades.md`<br/>`docs/DatabaseUpgrade.md`<br/>`Tools/Clovent.Installer.Provisioner/` | `upgrades.md` instructs running non-existent `Provisioner.exe --migrate`; `DatabaseUpgrade.md` instructs non-existent `Clovent.Desktop.exe --upgrade-database`. Actual command-line argument is `--provision`. |
| | Failed Upgrade Rollback | `COMPLETE` | `docs/deployment/upgrades.md` | Clearly warns that database schemas do not auto-downgrade and requires pre-upgrade `.bak` restore. |
| | Data Retention on Uninstall | `COMPLETE` | `docs/deployment/uninstallation.md`<br/>`installer/Clovent.BusinessOperatingSystem.iss` | Verified in source: Uninstaller removes client binaries only, preserving the SQL database, license files, and `%ProgramData%` configuration. |
| | **CRITICAL RISK:** Accidental Record Deletion | `CONTRADICTORY` | `docs/deployment/uninstallation.md` (Section 4) | Section 4 lists manual `DROP DATABASE [Clovent_BusinessOperatingSystem]` without high-visibility cautions separating disposal from routine maintenance. |
| **5. Support & Troubleshooting** | Diagnostic Log Locations | `CONTRADICTORY` | `docs/operations/logging-and-diagnostics.md`<br/>`docs/SupportDiagnostics.md`<br/>`src/Clovent.Platform/Logging/FileLoggerOptions.cs` | Three conflicting paths documented: Desktop app runtime logs write to `%LOCALAPPDATA%\Clovent\Logs`, whereas setup logs write to `%ProgramData%\Clovent\BusinessOperatingSystem\Logs`. |
| | Safe Diagnostic Collection | `COMPLETE` | `docs/SupportDiagnostics.md`<br/>`src/Clovent.Desktop/Program.cs` | `--diagnostics` CLI switch and Start Menu shortcut launch sanitized system summary without credentials. |
| | Sensitive Data Redaction | `COMPLETE` | `docs/operations/logging-and-diagnostics.md`<br/>`docs/security/security-architecture.md` | Connection string masking and zero PAN/CVV storage are strictly enforced across code and documentation. |
| | SQL Connectivity Failures | `COMPLETE` | `docs/support/runbooks/database-unavailable.md` | Systematic PowerShell diagnostics covering service state, TCP 1433, firewall, and DPAPI credential update dialog. |
| | Licensing & Tamper Issues | `COMPLETE` | `docs/support/runbooks/license-issue.md` | Clear runbook covering `ClockTampered` (NTP sync), `MachineMismatch` (fingerprint copy), and `Expired` (read-only mode). |
| | Stalled Outbox Jobs | `COMPLETE` | `docs/support/runbooks/outbox-stalled.md` | Diagnostic SQL queries to inspect `[Restaurant].[OutboxMessages]`, reset DeadLetter status, and strict warnings against row deletion. |
| | Support Escalation Paths | `COMPLETE` | `docs/support/troubleshooting.md`<br/>`docs/support/runbooks/` | Level 1 through Level 3 escalation boundaries and contact instructions are consistently defined. |
| **6. Documentation Structure** | README & docs/README Navigation | `PARTIAL` | `README.md`<br/>`docs/README.md` | Subdirectory documents are well indexed, but 16 legacy markdown documents residing directly in `docs/` are orphaned from navigation indices. |
| | Broken Markdown Links | `COMPLETE` | Whole repository markdown scan | 0 broken physical file links detected across all documentation files. |
| | Obsolete Paths & Navigation Mappings | `CONTRADICTORY` | `docs/support/runbooks/printer-failure.md`<br/>`src/Clovent.Desktop/Navigation/NavigationRegistry.cs` | Runbook directs technicians to `Administration -> Printers & Terminals`; actual ribbon path is `Settings -> Hardware -> Printers`. |
| | Release Maturity & Readiness Claims | `CONTRADICTORY` | `README.md`<br/>`docs/README.md`<br/>`docs/releases/cbos-1.3.0-release-notes.md`<br/>`docs/roadmap/engineering-roadmap.md` | Root READMEs declare CBOS 1.2.2 "Internal Baseline Only - NOT pilot ready"; 1.3.0 Release Notes describe candidate capabilities while qualification is active. Roadmap governance must be strictly preserved: current candidate acceptance of 1.3.0 does not itself redefine the 1.2.3 pilot or 1.3.0 commercial milestones. |

---

## 3. Detailed Inspection Findings by Domain

### 3.1 Installation and First Use
1. **Conflicting Workstation Prerequisites:**
   - [docs/deployment/installation.md](file:///D:/CBOS/Repository/docs/deployment/installation.md#L28-L32) specifies: Minimum 4 GB RAM (8 GB recommended) for POS terminal, 8 GB (16 GB recommended) for standalone, and minimum 10 GB SSD storage.
   - [docs/DeploymentGuide.md](file:///D:/CBOS/Repository/docs/DeploymentGuide.md#L28-L29) specifies: Minimum 8 GB RAM (16 GB recommended) and 2 GB free disk space.
   - *Clarification:* These figures are unbenchmarked estimates. Documentation must treat sizing as provisional guidance until formal clean-machine acceptance is concluded.
2. **Accounting for Both Installer Scripts:**
   - Baseline Script: `installer/Clovent.BusinessOperatingSystem.iss` builds `Clovent.BusinessOperatingSystem-1.2.2-Setup.exe` against versioned 1.2.2 release directories.
   - Authoritative 1.3.0 RC Script: `Tools/installer/cbos-setup.iss` builds `Clovent.BusinessOperatingSystem-1.3.0-Setup.exe` with parameterized `#define` macros (`PublishDir`, `ProvisionerDir`, `OutputDir`), aligned with current packaging scripts (`tools/ReleaseGuard/ScanReleasePackage.ps1`).
   - Legacy documentation ([docs/DeploymentGuide.md](file:///D:/CBOS/Repository/docs/DeploymentGuide.md#L36-L42)) incorrectly directs technicians to manually unzip binaries, bypassing NTFS ACL configuration.
3. **Commissioning Marker & Step Discrepancy:**
   - [docs/deployment/commissioning.md](file:///D:/CBOS/Repository/docs/deployment/commissioning.md#L15-L33) claims a 9-phase wizard writing a marker at `%ProgramData%\Clovent\BusinessOperatingSystem\commissioned.json`.
   - In committed source code ([FirstRunWizardForm.cs](file:///D:/CBOS/Repository/src/Clovent.Desktop/Commissioning/UI/FirstRunWizardForm.cs#L26-L27) and [CommissioningStateService.cs](file:///D:/CBOS/Repository/src/Clovent.Desktop/Commissioning/Services/CommissioningStateService.cs#L228-L237)), the wizard implements **8 steps** (`TotalSteps = 8`) and persists the marker to:
     `%ProgramData%\Clovent\BusinessOperatingSystem\Config\commissioning.json`
4. **Missing Cashier Account Provisioning Guide:**
   - First-run commissioning provisions only the root `Administrator` account ([FirstRunWizardForm.cs](file:///D:/CBOS/Repository/src/Clovent.Desktop/Commissioning/UI/FirstRunWizardForm.cs#L84-L89)).
   - Neither [installation.md](file:///D:/CBOS/Repository/docs/deployment/installation.md) nor [commissioning.md](file:///D:/CBOS/Repository/docs/deployment/commissioning.md) explains how an administrator creates Cashier accounts, assigns the `Cashier` role, or configures cashier login PINs in Back Office ([UsersForm.cs](file:///D:/CBOS/Repository/src/Clovent.Desktop/Forms/Identity/Users/UsersForm.cs)).

---

## 3.2 Everyday Cashier Operations
1. **The Refund Documentation Contradiction & Pending Status:**
   - [docs/known-limitations.md](file:///D:/CBOS/Repository/docs/known-limitations.md#L21-L26) and [docs/pos/order-lifecycle.md](file:///D:/CBOS/Repository/docs/pos/order-lifecycle.md#L93-L100) state that the Refund Aggregate is NOT implemented in CBOS 1.2.2/1.2.3.
   - [docs/releases/cbos-1.3.0-release-notes.md](file:///D:/CBOS/Repository/docs/releases/cbos-1.3.0-release-notes.md#L38) announces the Compensating Refund Ledger in 1.3.0, and [RestaurantPosForm.cs](file:///D:/CBOS/Repository/src/Clovent.Desktop/Restaurant/Orders/RestaurantPosForm.cs#L6970-L6975) activates [RefundOrderDialog.cs](file:///D:/CBOS/Repository/src/Clovent.Desktop/Restaurant/Orders/RefundOrderDialog.cs).
   - *Status:* This capability exists in source code, but runtime acceptance remains actively underway. Documentation must clearly identify refunds as candidate capabilities awaiting final acceptance sign-off.
2. **Missing Tax Configuration & Classification Guide:**
   - The codebase contains advanced tax modeling in [TaxProfile.cs](file:///D:/CBOS/Repository/src/Clovent.Catalog/TaxProfiles/TaxProfile.cs) (distinguishing `Taxable`, `ZeroRated`, `Exempt`, `OutOfScope`, along with `Inclusive` vs `Exclusive` pricing modes).
   - Documentation lacks a guide explaining how to establish tax profiles or link them to menu items. All tax examples must be labeled as synthetic and non-prescriptive.
3. **Missing POS Operations Menu Guide:**
   - Key operational workflows—`Print Last Receipt`, `Refund / Return`, `End of Day`, `Cash Movement`, and `Shift Close`—reside under the teal `Operations ▼` dropdown button ([RestaurantPosForm.cs](file:///D:/CBOS/Repository/src/Clovent.Desktop/Restaurant/Orders/RestaurantPosForm.cs#L7380-L7400)). A dedicated cashier operations guide is required.
4. **Receipt Reprint Scoping Ambiguity:**
   - The POS provides `Print Last Receipt` under `Operations ▼`. Cashiers must be informed that this is strictly scoped to the *last completed order of the active shift*. Earlier orders require Back Office order history access.

---

## 3.3 Printers and Peripherals
1. **Undocumented `PrinterSettingsForm`:**
   - [PrinterSettingsForm.cs](file:///D:/CBOS/Repository/src/Clovent.Desktop/Forms/Settings/PrinterSettingsForm.cs) allows configuring profiles, binding Windows print queues, assigning roles (`Receipt`, `Invoice`, `Kitchen`, `Bar`, `Label`, `Report`), setting paper widths (`80 mm`, `58 mm`, `A4`), and executing test prints. This form must be documented in platform guides.
2. **Incorrect Support Runbook Navigation Path:**
   - In [docs/support/runbooks/printer-failure.md](file:///D:/CBOS/Repository/docs/support/runbooks/printer-failure.md#L40-L45), the path `Administration -> Printers & Terminals` does not exist. The actual source paths are:
     - **Settings** ribbon page -> **Hardware** group -> **Printers** button (`PrinterSettingsForm`).
     - **Manager Panel** ribbon page -> **Operations / Controls** group -> **Printer Health** button (`PrinterHardwareHealthView`).
3. **Undocumented Printer Health & Quarantine Recovery:**
   - [PrinterHardwareHealthView.cs](file:///D:/CBOS/Repository/src/Clovent.Desktop/Printing/PrinterHardwareHealthView.cs) and [PrintJobQuarantineStore.cs](file:///D:/CBOS/Repository/src/Clovent.Platform/Printing/PrintJobQuarantineStore.cs) enable supervisors to monitor queue depths and replay quarantined print jobs.
4. **Cash Drawer Kick Limitations & Driver Dependency:**
   - Direct ESC/POS hardware kick pulse commands remain unverified at the software layer. Technicians must connect cash drawers via RJ11/RJ12 to the thermal printer and configure drawer-kick settings directly in the Windows printer driver properties.

---

## 3.4 Data Protection and Upgrades
1. **CRITICAL RISK: Accidental Customer Record Deletion in Routine Flow:**
   - In [docs/deployment/uninstallation.md](file:///D:/CBOS/Repository/docs/deployment/uninstallation.md#L50-L62), Section 4 lists `DROP DATABASE [Clovent_BusinessOperatingSystem];` without prominent warnings.
   - Decommissioning instructions must be moved out of routine flows, placed under strict `[!CAUTION]` warnings, and require explicit customer written authorization and verified off-machine backups.
2. **Conflicting Backup Paths & Ownership:**
   - [docs/operations/backup-and-restore.md](file:///D:/CBOS/Repository/docs/operations/backup-and-restore.md#L51-L54) targets `C:\Backups\CBOS` using `sqlcmd` (`cbos_app`).
   - [docs/BackupRestore.md](file:///D:/CBOS/Repository/docs/BackupRestore.md#L10-L29) targets `%ProgramData%\...\Backups` using `Invoke-Sqlcmd` (`cbos_runtime`).
   - Automated native scheduling does not exist in CBOS; backup scheduling is an operating system task.
3. **Invalid Database Upgrade Instructions:**
   - [docs/deployment/upgrades.md](file:///D:/CBOS/Repository/docs/deployment/upgrades.md#L73-L76) instructs running `.\Clovent.Installer.Provisioner.exe --migrate`.
   - `Clovent.Installer.Provisioner.exe` is deleted after setup (`deleteafterinstall`) and is not in `{app}`. Furthermore, `--migrate` is invalid; the argument is `--provision`.
   - [docs/DatabaseUpgrade.md](file:///D:/CBOS/Repository/docs/DatabaseUpgrade.md#L56) references `Clovent.Desktop.exe --upgrade-database`, which does not exist in code.

---

## 3.5 Support and Troubleshooting
1. **Diagnostic Log Directory Clarification:**
   - Desktop application runtime logs: `%LOCALAPPDATA%\Clovent\Logs\` (defined in [FileLoggerOptions.cs](file:///D:/CBOS/Repository/src/Clovent.Platform/Logging/FileLoggerOptions.cs#L20)).
   - Setup / Installer logs: `%ProgramData%\Clovent\BusinessOperatingSystem\Logs\` (used by Inno Setup).
2. **Inaccurate Continuity Journal File Path:**
   - [docs/support/runbooks/continuity-mode.md](file:///D:/CBOS/Repository/docs/support/runbooks/continuity-mode.md#L30) cites `continuity_journal.dat` at the root of `BusinessOperatingSystem`.
   - Committed source code ([ProtectedContinuityJournalStore.cs](file:///D:/CBOS/Repository/src/Clovent.Restaurant.Infrastructure/Continuity/ProtectedContinuityJournalStore.cs#L66-L67)) places the file at:
     `%ProgramData%\Clovent\BusinessOperatingSystem\ContinuityJournal\journal.dat`
3. **Effective Diagnostic Tools:**
   - [SupportDiagnosticsForm.cs](file:///D:/CBOS/Repository/src/Clovent.Desktop/Commissioning/UI/SupportDiagnosticsForm.cs) (`--diagnostics`) provides safe, credential-sanitized diagnostic snapshots.

---

## 3.6 Documentation Structure and Governance
1. **Orphaned Root Documents in `docs/`:**
   - 16 legacy markdown documents in `docs/` lack supersession pointers and conflict with modern subfolder guides.
2. **Preserving Release Milestones:**
   - CBOS 1.2.2 remains the frozen internal acceptance baseline (not paid-pilot ready, not commercial GA).
   - CBOS 1.2.3 remains the canonical controlled attended single-terminal pilot hardening milestone.
   - CBOS 1.3.0 remains the canonical first broader commercial foundation milestone. Current candidate qualification in Windows Sandbox evaluates candidate features, but does not itself redefine the 1.2.3 pilot or 1.3.0 commercial milestones. Operational qualification remains explicitly pending acceptance sign-off.

---

## 4. The Ten Highest-Priority Documentation Gaps

| # | Priority Gap | Customer / Operational Impact | Recommended Documentation Correction |
| :-: | :--- | :--- | :--- |
| **1** | **Broken Schema Upgrade Instructions** (`--migrate` invalid; Provisioner in `{tmp}`, not `{app}`) | Field technicians cannot resolve `DatabaseTooOld` blocks during upgrades. | Update [upgrades.md](file:///D:/CBOS/Repository/docs/deployment/upgrades.md) to explain that Inno Setup executes provisioning automatically during upgrade. For manual operations, document `Clovent.Installer.Provisioner.exe --provision` from tools build output. Mark [DatabaseUpgrade.md](file:///D:/CBOS/Repository/docs/DatabaseUpgrade.md) superseded. |
| **2** | **Critical Safety Risk: Decommissioning `DROP DATABASE` in routine flow** | A technician performing troubleshooting may execute `DROP DATABASE`, destroying customer financial records. | Quarantine destructive decommissioning in [uninstallation.md](file:///D:/CBOS/Repository/docs/deployment/uninstallation.md) behind prominent `[!CAUTION]` alerts requiring written customer authorization and verified recoverable backups. Clarify that routine uninstallation preserves all data. |
| **3** | **Refund Capability Documentation & Acceptance Status** | Store managers cannot determine if refunds are supported or how cashiers operate them. | Reconcile [known-limitations.md](file:///D:/CBOS/Repository/docs/known-limitations.md) and [order-lifecycle.md](file:///D:/CBOS/Repository/docs/pos/order-lifecycle.md) by clearly documenting the 1.3.0 compensating refund domain as a candidate capability currently under active Windows Sandbox qualification; author a source-backed cashier refund guide. |
| **4** | **Missing Cashier Account Onboarding Guide** | Shop owners completing setup cannot onboard cashiers or configure PINs. | Author `docs/deployment/cashier-account-setup.md` detailing Back Office navigation to `Users` (`UsersForm.cs`), role assignment (`Cashier`), and PIN configuration (`PinPromptForm.cs`). |
| **5** | **Missing Cashier Operations Manual** | Cashiers cannot locate `Operations ▼` commands (discounts, reprints, day close). | Author `docs/pos/cashier-operations-guide.md` illustrating layout, discount workflows, split payments, customer credit, reprints, and blind cash close. |
| **6** | **Missing Printer Configuration Guide & Broken Runbook Navigation** | Technicians cannot configure multi-printer routing or paper widths, and runbooks cite non-existent navigation. | Author `docs/integrations/printer-configuration.md` covering [PrinterSettingsForm.cs](file:///D:/CBOS/Repository/src/Clovent.Desktop/Forms/Settings/PrinterSettingsForm.cs). Update [printer-failure.md](file:///D:/CBOS/Repository/docs/support/runbooks/printer-failure.md) to point to `Settings -> Hardware -> Printers` and `Manager Panel -> Operations / Controls -> Printer Health`. |
| **7** | **Commissioning Marker & Step Discrepancy** (9 phases vs 8 steps; `commissioned.json` vs `Config\commissioning.json`) | Technicians investigating commissioning detection look for the wrong marker file. | Align [commissioning.md](file:///D:/CBOS/Repository/docs/deployment/commissioning.md) with [FirstRunWizardForm.cs](file:///D:/CBOS/Repository/src/Clovent.Desktop/Commissioning/UI/FirstRunWizardForm.cs) (8 steps) and specify the true path: `%ProgramData%\Clovent\BusinessOperatingSystem\Config\commissioning.json`. |
| **8** | **Missing Tax Profile Setup Guide** | Business owners cannot configure sales tax rates, pricing modes, or non-taxable item classifications. | Author `docs/catalog/tax-profiles.md` detailing `TaxProfile.cs` (Taxable, Zero-Rated, Exempt, Out-of-Scope; Inclusive vs Exclusive). Mark all examples as synthetic. |
| **9** | **Inaccurate Continuity Journal File Path in Runbook** | Technicians troubleshooting offline continuity fail to find the emergency journal file. | Correct the path in [continuity-mode.md](file:///D:/CBOS/Repository/docs/support/runbooks/continuity-mode.md), [troubleshooting.md](file:///D:/CBOS/Repository/docs/support/troubleshooting.md), and [uninstallation.md](file:///D:/CBOS/Repository/docs/deployment/uninstallation.md) to `%ProgramData%\Clovent\BusinessOperatingSystem\ContinuityJournal\journal.dat`. |
| **10** | **Release Maturity & Readiness Governance** | Users receive conflicting signals regarding whether candidate qualification alters planned release targets or milestone scope. | Maintain strict canonical roadmap governance: 1.2.2 is the frozen internal acceptance baseline; 1.2.3 is the controlled attended single-terminal pilot; 1.3.0 is the broader commercial GA foundation. Current candidate qualification of 1.3.0 does not redefine the 1.2.3 pilot or 1.3.0 commercial milestones; operational qualification remains pending acceptance sign-off. |

---

## 5. Items Dependent on Active Acceptance Agent's Results

The following items are source-inspected implementations whose operational qualification remains strictly owned by the active acceptance agent:

1. **Compensating Refund Ledger & Cash Drawer Limits:**
   - Financial cap verification on `ExternalCardRefund` (`38ac204`).
   - Restock deduplication and cash payout drawer limits (`63aebaf`, `02825a3`).
   - Clean Windows Sandbox acceptance of [RefundOrderDialog.cs](file:///D:/CBOS/Repository/src/Clovent.Desktop/Restaurant/Orders/RefundOrderDialog.cs).
   - *Status: PENDING acceptance agent test sign-off.*
2. **Standard Non-Elevated User Rights:**
   - Verification that unprivileged Windows users (`Users` group / designated POS accounts) can execute CBOS Desktop without UAC prompts, access DPAPI machine stores, and write rolling logs (`38ac204`, `cbc65dd`).
   - *Status: PENDING acceptance agent test sign-off.*
3. **Additive Schema Upgrade & Migration Reconciliation:**
   - Verification that upgrading from prior versions correctly detects existing databases without data truncation or migration failures (`b4a98f5`, `63aebaf`).
   - *Status: PENDING acceptance agent test sign-off.*
4. **Code-Signing, Packaging & SHA-256 Checksums:**
   - Production packaging of `Clovent.BusinessOperatingSystem-1.3.0-Setup.exe` with Authenticode RFC 3161 timestamping and final `SHA256SUMS.txt` generation via `Tools/installer/cbos-setup.iss`.
   - *Status: PENDING acceptance agent build and signature execution.*
5. **Release Milestone Qualification & Governance:**
   - Preservation of canonical roadmap milestones: current candidate qualification in Windows Sandbox evaluates 1.3.0 candidate capabilities, but does not redefine the 1.2.3 pilot or 1.3.0 commercial milestones.
   - *Status: PENDING acceptance agent final qualification.*

---

## 6. Proposed Client Handover Checklist (Source-Inspected Capabilities; Runtime Acceptance Pending)

This checklist provides a structured deployment protocol based on source-inspected capabilities. All runtime items remain subject to final acceptance verification:

### Phase 1: Pre-Installation & Environment Preparation
- [ ] **Operating System:** Windows 10 (version 1809+, 64-bit) or Windows 11 (64-bit). *(Hardware sizing: 4–8 GB RAM for dedicated POS, 8–16 GB for standalone/server; sizing remains provisional pending benchmark acceptance).*
- [ ] **Display Setup:** 1366×768 minimum, 1920×1080 recommended. *(Note: High-DPI scaling behavior requires physical display verification).*
- [ ] **SQL Server Preparation:** Microsoft SQL Server 2019/2022 Express installed with mixed-mode authentication and TCP/IP enabled on port 1433.
- [ ] **Runtime User Provisioning:** Create SQL login `cbos_app` with `db_datareader`, `db_datawriter`, and `GRANT EXECUTE` permissions.
- [ ] **Printer Hardware Preparation:** Connect thermal receipt printer via USB or local network; install manufacturer Windows spooler driver; set paper width (80mm or 58mm) in driver preferences; configure driver cash drawer kick pulse if drawer is attached via RJ11/RJ12.

### Phase 2: Client Installation & First-Run Commissioning
- [ ] **Installer Execution:** Run `Clovent.BusinessOperatingSystem-1.3.0-Setup.exe` (or baseline `1.2.2-Setup.exe`) with Administrator elevation. Confirm installation to `C:\Program Files\Clovent\Business Operating System\`.
- [ ] **Verify Directory Security:** Confirm `%ProgramData%\Clovent\BusinessOperatingSystem\` exists with least-privilege DACLs (SYSTEM/Admins: Full Control; Users: Read-Only for Config/License, Read/Write for Logs/ContinuityJournal).
- [ ] **Complete Commissioning Wizard (8 Steps):**
  - [ ] Step 1: Review system prerequisites.
  - [ ] Step 2: Connect to SQL Server and test connection.
  - [ ] Step 3: Initialize database schema across all 6 bounded contexts.
  - [ ] Step 4: Configure Organization, Company, and Branch identity.
  - [ ] Step 5: Provision master Administrator account (minimum 8 chars, strong password; no default passwords).
  - [ ] Step 6: Configure regional settings (Currency, date/time formats, Terminal code).
  - [ ] Step 7: Import client-specific signed `clovent.lic` software license (or initialize 30-day evaluation trial).
  - [ ] Step 8: Confirm summary and verify completion marker written to `%ProgramData%\Clovent\BusinessOperatingSystem\Config\commissioning.json`.

### Phase 3: Administrative Back-Office Configuration
- [ ] **First Administrator Sign-In:** Sign in using master Administrator credentials.
- [ ] **Cashier Account Creation:** Navigate to **Users** ribbon page -> **Security** group -> **Users** (`UsersForm`). Create frontline cashier accounts, assign the `Cashier` role, and configure passwords/PINs.
- [ ] **Printer Configuration:** Navigate to **Settings** -> **Hardware** -> **Printers** (`PrinterSettingsForm`). Select the installed Windows printer queue, assign the `Receipt` role, select paper width (80mm/58mm), and click **Test Print** to verify output.
- [ ] **Catalog & Dining Setup:** Verify menu categories, products, prices, and dining tables are configured.
- [ ] **Tax Profiles:** Configure statutory tax profiles in Catalog context (mark example rates as synthetic; configure Inclusive vs Exclusive pricing).

### Phase 4: Daily Frontline Cashier Operations
- [ ] **Cashier Sign-In & Shift Open:** Cashier signs in at register; enters counted starting cash drawer float; confirms shift status is `Open`.
- [ ] **Ringing Sales:** Ring Dine-In, Takeaway, or Delivery orders; add items; apply percentage or fixed discounts with audit reason if authorized.
- [ ] **Tender Settlement:**
  - [ ] **Cash:** Enter cash tendered; verify change calculation; drawer opens via printer hook; receipt prints.
  - [ ] **Card:** Process payment on standalone bank card terminal; enter manual Card tender and approval reference in CBOS.
  - [ ] **Split Tender:** Settle order across multiple tenders (e.g. Cash + Card) via `BillSplitDialog`.
  - [ ] **On Account:** Select customer; verify credit limit; approve manager override if limit exceeded.
- [ ] **Reprinting Receipts:** Click teal `Operations ▼` button -> `Print Last Receipt` for the current shift's latest check.
- [ ] **Mid-Shift Cash Movements:** Record float adjustments (`Cash In` / `Cash Out`) under `Operations ▼` -> `Cash Movement`.
- [ ] **Compensating Refunds (Candidate):** Access `Operations ▼` -> `Refund / Return` (`RefundOrderDialog`); select order, line items, and disposition. *(Marked pending acceptance qualification).*

### Phase 5: Evening Shift Close & Business Day Reconciliation
- [ ] **Blind Drawer Cash Count:** Cashier selects `Operations ▼` -> `Shift` -> `Close Shift`; performs blind physical cash count; enters `Counted Cash`.
- [ ] **Variance Review:** System evaluates $\text{Expected Cash} = \text{Starting Float} + \text{Net Cash Sales} + \text{Cash In} + \text{Collections} - \text{Cash Out}$. Enter mandatory justification reason if variance exists. Confirm closure.
- [ ] **Business Day Close:** Store manager opens `Operations ▼` -> `End of Day` (`EndOfDayCloseDialog`), reviews branch daily totals, and confirms day close.
- [ ] **External Backup Task:** Ensure an external operating system backup task (e.g., Windows Task Scheduler executing PowerShell `sqlcmd` backup) runs outside business hours. *(Note: CBOS does not include native automated backup scheduling).*
- [ ] **Accidental Deletion Prevention:** Reiterate to all staff that uninstalling or upgrading CBOS **never** requires dropping the database, and `DROP DATABASE` is strictly quarantined for permanent hardware decommissioning with prior authorization and verified backups.

---

## 7. Report Conclusion & Status

- **Status:** Review report updated with verified source contracts and explicit boundary distinctions.
- **Active Acceptance Stream:** Preserved without interference. All Sandbox execution, packaging, and code-signing decisions remain with the active acceptance agent.
- **Deliverable:** Full review report persisted at `docs/reviews/CUSTOMER_DEPLOYMENT_SUPPORT_DOC_REVIEW.md`.
