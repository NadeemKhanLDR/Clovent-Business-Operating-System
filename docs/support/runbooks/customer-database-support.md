# Support Runbook: Customer Database Support & Safe Corrective Workflow

| Attribute | Details |
| :--- | :--- |
| **Area** | Customer Database Support, Field Engineering, Disaster Recovery |
| **Audience** | Level 2–3 Support Engineers, Database Administrators, Systems Engineers |
| **Last Reviewed Version** | CBOS 1.2.2 / 1.2.3 |
| **Status** | **PROCEDURAL RUNBOOK & STANDARD** |
| **Classification** | **PUBLIC-SAFE** |

---

## 1. Overview & Operational Principles

This runbook establishes the formal, audited procedure for customer database diagnostics, support bundling, isolated restoration, and reviewed corrective-script execution across Clovent Business Operating System (CBOS).

### Core Principles
1. **Separation of Support Privileges:** Frontline cashier and POS application accounts (`cbos_cashier`, `CBOS_POS_Users`, `BUILTIN\Users`) operate under strict least-privilege rights (`db_datareader`, `db_datawriter`, execute, view definition) and **cannot** perform database backups, restores, or administrative maintenance. Support operations require authorized support staff credentials (`db_backupoperator`, `db_owner`, or `sysadmin`).
2. **Zero Password Leakage:** Plaintext passwords must never appear in logs, configuration files, console transcripts, or command-line parameters. Masking and DPAPI encryption protect credentials at all stages.
3. **Mandatory Customer Authorization:** Customer database archives contain proprietary business records. Diagnostic bundles may only be generated with explicit, recorded customer authorization (`-CustomerConsent`) and transferred via secure, customer-authorized channels. Automated or background cloud uploads are strictly prohibited.
4. **Isolated Non-Destructive Restores:** Support restorations must **never** target or overwrite an existing production database by default. Restores are performed exclusively into separate, isolated developer/test databases with external integrations, outbox synchronization, scheduled jobs, and printing disabled.
5. **Strict Financial Transaction Immutability (PDR-0003):** Completed orders, payments, refunds, tax snapshots, and customer ledger historical records are **strictly immutable**. Support scripts are prohibited from rewriting or deleting completed financial transactions. Financial discrepancies must be resolved exclusively via approved compensating transactions.

---

## 2. Authorized Database Backup Procedure

Authorized support staff can create a consistent full database backup of the entire CBOS database, including all six bounded-context schemas (`[Authentication]`, `[Identity]`, `[MasterData]`, `[Catalog]`, `[Inventory]`, `[Restaurant]`).

### 2.1 Authentication & Privilege Requirements
- **Windows Authentication (Preferred):** The technician runs PowerShell elevated as an authorized Windows domain or local administrator possessing the `sysadmin` or `db_backupoperator` SQL Server role.
- **SQL Server Authentication:** Dedicated support login (e.g. `cbos_support_admin`). Passwords must be provided as secure strings via `Get-Credential` or prompted interactively.

### 2.2 Backup Command
Execute the support backup script:

```powershell
# Using Windows Authentication (Integrated):
.\scripts\support\Backup-CbosDatabase.ps1 `
    -Server "localhost" `
    -Database "Clovent_BusinessOperatingSystem" `
    -UseWindowsAuth

# Or using SQL Server Authentication with Secure Credentials:
$cred = Get-Credential
.\scripts\support\Backup-CbosDatabase.ps1 `
    -Server "localhost" `
    -Database "Clovent_BusinessOperatingSystem" `
    -SqlUser $cred.UserName `
    -SqlPassword $cred.Password
```

### 2.3 Technical Guarantees
- Executes `BACKUP DATABASE [<Database>] TO DISK = @path WITH COPY_ONLY, CHECKSUM, STATS = 10, INIT`. The `COPY_ONLY` clause ensures the customer's production log sequence number (LSN) chain is preserved.
- Verifies physical archive media integrity immediately using `RESTORE VERIFYONLY FROM DISK = @path WITH CHECKSUM`.
- Computes SHA-256 cryptographic digest of the generated `.bak` file.

---

## 3. Support Bundle Packaging & Customer Authorization

When an issue requires offline escalation or developer investigation, produce a self-contained support bundle.

### 3.1 Customer Consent Requirement
Customer consent is an unconditional prerequisite. The technician must obtain verbal or written agreement from the customer and specify `-CustomerConsent`, the customer organization name, and the authorized contact.

```powershell
.\scripts\support\New-CbosSupportBundle.ps1 `
    -Server "localhost" `
    -Database "Clovent_BusinessOperatingSystem" `
    -UseWindowsAuth `
    -TicketId "INC-20261017-001" `
    -CustomerOrganization "Grandview Hospitality LLC" `
    -AuthorizedBy "Jane Doe (General Manager)" `
    -CustomerConsent
```

### 3.2 Support Bundle Contents
The resulting zip package (`CBOS_SupportBundle_<TicketId>_<Timestamp>.zip`) contains:
1. **Full Database Backup (`.bak`):** Full copy-only backup encompassing all six bounded-context schemas.
2. **SHA-256 Hash Manifest (`<FileName>.sha256` and inside `manifest.json`):** Cryptographic verification hash.
3. **Structured Diagnostic Manifest (`manifest.json`):**
   - Application version and Git build commit/branch identity.
   - SQL Server product version, edition, and compatibility level.
   - Applied EF Core migration versions across all six schema tables (`[Authentication].[__EFMigrationsHistory]`, `[Identity].[__EFMigrationsHistory]`, `[MasterData].[__EFMigrationsHistory]`, `[Catalog].[__EFMigrationsHistory]`, `[Inventory].[__EFMigrationsHistory]`, `[Restaurant].[__EFMigrationsHistory]`).
   - Incident ticket ID, correlation ID, and backup timestamp (UTC).
   - Customer authorization record and data handling policy declaration.
4. **Redacted Operational Logs (`redacted_logs/`):**
   - Logs collected from `%LOCALAPPDATA%\Clovent\Logs\` and `%ProgramData%\Clovent\BusinessOperatingSystem\Logs\`.
   - Automated sanitization strips database connection passwords (`Password=******`), cardholder account numbers (13–16 digits masked), employee PINs, and authentication tokens.

> [!IMPORTANT]
> **Zero Automated Upload Policy:** CBOS strictly forbids background telemetry uploads of customer database archives. Bundles must be transferred manually through authorized encrypted storage (e.g. encrypted customer portal or VPN share).

---

## 4. Isolated Restoration in Developer / Test Environments

Restoring a customer database into a development or test environment must never interfere with operational databases or trigger unintentional side-effects.

### 4.1 Prerequisites & Version Compatibility
- **SQL Server Version Rule:** Microsoft SQL Server backups are **not backward-compatible**. A database backed up on SQL Server 2025 cannot be restored onto SQL Server 2022 or earlier. The developer instance major version must be greater than or equal to the source instance major version.
- **Disk Space:** Ensure destination storage has free capacity at least 3x the size of the uncompressed backup file.
- **Restricted Database Names:** Restoration tools strictly prohibit targeting production names (`Clovent_BusinessOperatingSystem`, `Guppys*`, `master`, `msdb`, `tempdb`).

### 4.2 Restoration Command
Restore the bundle backup into a **NEW isolated database**:

```powershell
.\scripts\support\Restore-CbosIsolatedDatabase.ps1 `
    -BackupFilePath "C:\Support\CBOS_SupportBackup_20261017_120000.bak" `
    -TargetDatabaseName "CBOS_Support_INC20261017_Isolated" `
    -Server "localhost" `
    -UseWindowsAuth `
    -ExpectedSha256 "A1B2C3D4..."
```

### 4.3 Automated Pre-Launch Sanitization (`Protect-CbosRestoredEnvironment`)
Before launching CBOS against the restored database, the restoration process automatically neutralizes all live external connectors:
1. **Outbox Synchronization Neutralization:**
   Sets all pending, retry-scheduled, and processing records in `[Restaurant].[OutboxMessages]` to `DeadLetter` with reason `Quarantined for isolated support investigation`. This prevents background workers from attempting live HTTP calls to QuickBooks, central analytics, or store sync endpoints.
2. **Physical Printing Neutralization:**
   Generates a quarantined `printers.json` with all printer profiles disabled (`IsEnabled = false`) or empty routing assignments. No receipt thermal printers or kitchen prep printers will spool physical paper jobs.
3. **Machine-Bound DPAPI Reconfiguration:**
   Customer DPAPI connection strings in `database.config.json` cannot be decrypted on a different machine (LSA master key mismatch). The sanitization step automatically reconfigures `database.config.json` targeting the new isolated database using the local developer's credentials/Windows Authentication.
4. **Integrity Validation:**
   Executes `DBCC CHECKDB` to verify zero physical allocation or logical consistency faults.

---

## 5. Reviewed Corrective-Script Workflow

When a bug, stuck state, or edge-case discrepancy requires a database-level fix, the script must be authored, reviewed, and tested in accordance with strict governance.

### 5.1 Script Structure & Sections
Corrective scripts must be saved with explicit header metadata and operational sections:

```sql
-- SCRIPT_ID: FIX_YYYYMMDD_##_DescriptiveTitle
-- DESCRIPTION: Concise description of root cause and fix
-- TARGET_SCHEMA: SchemaName (e.g. Restaurant)
-- AUTHOR: Engineer Name / Support Team
-- REVIEW_STATUS: Approved for execution

-- PRECHECK:
-- Query that validates affected-row count and conditions before mutation.
SELECT COUNT(1) FROM ...;

-- PREVIEW_DIAGNOSIS:
-- Read-only query that outputs the exact records diagnosed for correction.
SELECT ... FROM ...;

-- MUTATION:
-- The corrective modification (DML or compensating transaction).
UPDATE / INSERT ...;

-- POSTCHECK_RECONCILIATION:
-- Query asserting that the corrected invariant holds.
SELECT CASE WHEN ... THEN 'PASS' ELSE 'FAIL' END;
```

### 5.2 Phase 1: Preview & Read-Only Diagnosis (`-DryRun`)
Always execute the script in preview mode first. This performs target database prechecks, displays diagnosed rows, reports affected counts, and validates idempotency without modifying any data.

```powershell
.\scripts\support\Invoke-CbosCorrectiveScript.ps1 `
    -ScriptFilePath ".\corrective_scripts\FIX_20261017_01_QuarantineStuckOutbox.sql" `
    -TargetDatabaseName "CBOS_Support_INC20261017_Isolated" `
    -DryRun
```

### 5.3 Phase 2: Transactional Execution (`-Execute`)
Once verified on the restored copy, execute the correction:

```powershell
.\scripts\support\Invoke-CbosCorrectiveScript.ps1 `
    -ScriptFilePath ".\corrective_scripts\FIX_20261017_01_QuarantineStuckOutbox.sql" `
    -TargetDatabaseName "CBOS_Support_INC20261017_Isolated" `
    -Execute `
    -PreTestedOnRestoredCopy
```

### 5.4 Execution Safety Safeguards
1. **Pre-Tested Gate (`-PreTestedOnRestoredCopy`):** Corrective scripts must be executed against the isolated restored database copy before they are applied to customer environments.
2. **Fresh Pre-Fix Backup:** Automatically creates a copy-only full backup (`<Database>_PreFix_<ScriptId>_<Timestamp>.bak`) immediately before opening the transaction.
3. **Repeat-Execution Protection:** Queries `[MasterData].[SupportScriptExecutionAudit]`. If the script ID has already been executed successfully on the target database, execution is rejected to prevent duplicate mutations.
4. **Atomic Transactional Rollback:** The mutation is wrapped in a SQL transaction (`BEGIN TRANSACTION ... COMMIT`). If any precheck, DML statement, or post-check fails, the entire transaction is rolled back (`ROLLBACK TRANSACTION`).
5. **Permanent Audit Trail:** Inserts an immutable record into `[MasterData].[SupportScriptExecutionAudit]` recording `ScriptId`, `ScriptHash`, `Operator`, `ExecutedAtUtc`, `TargetDatabase`, `PreCheckSummary`, `AffectedRows`, `Outcome`, and `CompensationReference`.

---

## 6. Financial Immutability & Compensating Transactions (PDR-0003)

> [!CAUTION]
> **STRICT FINANCIAL IMMUTABILITY POLICY:**
> Under no circumstances may support staff execute `UPDATE` or `DELETE` statements on:
> - Completed Orders (`[Restaurant].[Orders]` where `Status = 'Completed'`)
> - Completed Payments (`[Restaurant].[Payments]`)
> - Completed Refunds (`[Restaurant].[Refunds]`)
> - Tax Snapshots (`[Restaurant].[OrderLines]` `TaxAmount`, `TaxableBase`)
> - Customer Ledger History (`[Restaurant].[CustomerLedgerEntries]`)

The script runner contains an automated static analysis guard that scans script contents and rejects any script attempting direct updates or deletions on completed financial tables.

### Approved Financial Correction Pattern
When customer financial balances require adjustment (e.g. resolving a billing dispute, applying an off-system goodwill credit, or clearing a cash variance):
1. **Never edit historical invoices or settled payments.**
2. **Insert an additive compensating transaction** into `[Restaurant].[CustomerLedgerEntries]` with an explicit compensation reference (e.g. `COMP-DISPUTE-20261017`), adjusting `Debit` or `Credit`.
3. **Adjust the customer's current `OutstandingBalance`** in `[Restaurant].[Customers]` so it reconciles with the algebraic sum of all historical ledger entries.
4. Pass `-CompensationReference` to the runner to link the adjustment to formal management approval.

---

## 7. Verification Summary & Reference Architecture

| Workflow Step | Tool / Script | Key Output / Artifact |
| :--- | :--- | :--- |
| **1. Full Backup** | `Backup-CbosDatabase.ps1` | `<Database>_SupportBackup_<Timestamp>.bak` (COPY_ONLY, verified) |
| **2. Support Bundle** | `New-CbosSupportBundle.ps1` | `CBOS_SupportBundle_<Ticket>_<Timestamp>.zip` + SHA-256 manifest + redacted logs |
| **3. Checksum Verify** | `Restore-CbosIsolatedDatabase.ps1` | SHA-256 hash match against manifest |
| **4. Isolated Restore** | `Restore-CbosIsolatedDatabase.ps1` | `CBOS_Support_<Ticket>_Isolated` (new isolated database) |
| **5. Environment Sanitize** | `Protect-CbosRestoredEnvironment` | Outbox DeadLetter quarantine, disabled printers, DPAPI reconfiguration |
| **6. Preview Diagnosis** | `Invoke-CbosCorrectiveScript.ps1 -DryRun` | Read-only precheck and diagnosed affected records |
| **7. Safe Execution** | `Invoke-CbosCorrectiveScript.ps1 -Execute` | Pre-fix backup, transactional commit, audit log in `[MasterData]` |
| **8. Reconciliation** | `CbosSupportModule` / Reconciler | Pass/fail table confirming all domain facts and invariants match |

---

## 8. Cross References
- [CBOS Database Backup & Disaster Recovery Procedures](../operations/backup-and-restore.md)
- [Logging & Diagnostics Architecture](../operations/logging-and-diagnostics.md)
- [PDR-0003: Completed Financial Transaction Immutability](../product/pdr/PDR-0003-completed-financial-transaction-immutability.md)
- [Database Architecture & Persistence Model](../database/database-architecture.md)
- [Master Troubleshooting Guide](troubleshooting.md)
