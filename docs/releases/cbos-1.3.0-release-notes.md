# Clovent Business Operating System (CBOS) — Release 1.3.0 Release Notes & Pilot Sign-Off Summary

**Target Baseline:** CBOS 1.3.0 (Release Candidate 1)  
**Distribution Target:** Windows x64 (`net10.0-windows` / `net10.0` Self-Contained)  
**Installer Package:** `Clovent.BusinessOperatingSystem-1.3.0-Setup.exe`  
**Date:** October 9, 2026  
**Status:** Certified Release Candidate (100% Test Green, 0 ReleaseGuard Violations)  

---

## 1. Executive Summary & Release Scope

CBOS 1.3.0 introduces enterprise-grade multi-terminal retail and restaurant operation resilience, statutory tax compliance, and automated accounting synchronization. Building upon the frozen CBOS 1.2.2 baseline, CBOS 1.3.0 delivers four core functional pillars:

1. **Offline Continuity Mode:** Fail-safe cash-only POS operation during network and SQL Server database outages, secured by DPAPI encryption and HMAC-SHA256 signature chains, backed by automatic idempotent replay upon reconnection.
2. **Multi-Terminal Delta-Sync Engine:** Distributed real-time synchronization across multiple checkout terminals, utilizing outbox messaging, transactional inbox ingestion, vector-based version tracking, and deterministic conflict resolution.
3. **Pakistan Tax & Refund Management:** Comprehensive fiscal compliance for provincial authorities (PRA, SRB, KPRA, BRA) and federal FBR integration, immutable receipt tax snapshots, and auditable itemized refunds with strict `MidpointRounding.AwayFromZero` calculation.
4. **Bi-Directional QuickBooks Integration:** Asynchronous, outbox-driven automated accounting synchronization mapping sales receipts, payments, refunds, and shift closures to QuickBooks Desktop / Online without locking operational POS workflows.

---

## 2. Version Deltas & Key Architectural Enhancements

### 2.1 Offline Continuity Engine (`src/Clovent.Restaurant/Continuity`, `src/Clovent.Desktop/Restaurant/Services`)
- **Emergency Operation:** POS switches seamlessly to cash-only Continuity Mode when primary SQL Server connectivity is interrupted.
- **Cryptographic Tamper-Evident Journaling:** All offline sales are appended to `%ProgramData%\Clovent\BusinessOperatingSystem\ContinuityJournal\journal.dat`, protected by Windows Data Protection API (DPAPI) and chained HMAC-SHA256 checksums (`ComputeChecksum`, `ComputeHmacSignature`).
- **Idempotent Replay Service:** `EmergencyJournalReplayer` processes pending transactions upon database restoration, ensuring zero duplicate ledger entries and zero drift between journal snapshots and master orders.
- **Resilient Operational Caching:** Fast in-memory/file caching of validated catalog hierarchies, price lists, and tax rules allowing full menu navigation while offline.

### 2.2 Multi-Terminal Delta-Sync (`src/Clovent.Platform/Sync`, `src/Clovent.Restaurant/Sync`)
- **Outbox-Driven Asynchronous Synchronization:** Terminal transactions publish delta payloads (`DeltaSyncOutboxHandlers`) without blocking cashier UI execution.
- **Inbox Ingestion & Conflict Handling:** `SyncInboxRecord` and `SyncConflictRecord` track peer transactions, detect concurrency collisions, and preserve cashier audit trails.
- **Shift Reconciliation Across Terminals:** `TerminalShiftSyncSummary` consolidates multi-lane sales, cash drawers, and tax collections into master shift closures.

### 2.3 Pakistan Tax Regimes & Itemized Refund Architecture
- **Statutory Tax Profiles:** Added `TaxProfile` entity with configurable rate regimes across Punjab Revenue Authority (PRA), Sindh Revenue Board (SRB), and Federal Board of Revenue (FBR).
- **Immutable Tax & Receipt Snapshots:** `ReceiptSnapshot` durably preserves line-level taxable amounts, inclusive/exclusive tax breakdowns, cashier credentials, and formatting versions (`1.3.0-AwayFromZero-v1`).
- **Compensating Refund Ledger:** Added `Refund` and `RefundLine` domain models (`AddRefundDomainAndTaxSnapshots` migration). Strictly preserves ledger immutability: adjustments are recorded as compensating transactions rather than destructive updates.

### 2.4 Bi-Directional QuickBooks Integration (`src/Clovent.Restaurant/QuickBooks`)
- **Asynchronous Sync Handlers:** Specialized outbox handlers (`QuickBooksInvoiceSyncHandler`, `QuickBooksPaymentSyncHandler`, `QuickBooksShiftSyncHandler`) dispatch accounting journals in the background.
- **Idempotent Mapping Repository:** `QuickBooksSyncMap` persists CBOS-to-QuickBooks entity mappings (`QuickBooksSyncMapRepository`), preventing duplicate invoices or payments during network retries.
- **Gateway Abstraction:** `IQuickBooksGateway` abstracts communications for QuickBooks Desktop (via QBFC/SDK) and QuickBooks Online REST APIs.

---

## 3. Installation Prerequisites & System Requirements

### 3.1 Workstation Requirements
- **Operating System:** Windows 10 (version 1809 or higher, 64-bit), Windows 11 (64-bit), or Windows Server 2019/2022 (64-bit).
- **Architecture:** x64 (Native 64-bit execution).
- **Client Runtime Prerequisite:** **None.** The desktop package is fully self-contained (`--self-contained true` with .NET 10 runtime and DevExpress 26.1 UI controls embedded). Visual Studio or the .NET SDK are **never** required on target workstations.
- **Privileges:** Local Administrator rights required during installation to configure Windows Services and NTFS ACLs.

### 3.2 Database Engine Requirements
- **Supported Versions:** Microsoft SQL Server 2019, SQL Server 2022 (Express, Standard, or Enterprise editions).
- **Target Database Name:** `Clovent_BusinessOperatingSystem`
- **Default Local Instance:** Dedicated `.\CLOVENT` instance or standard `(local)` / `.\SQLEXPRESS`.
- **Authentication:** Windows Authentication (integrated security) or SQL Server Authentication (configured via DPAPI-protected store).
- **Automated Provisioning:** If no compatible instance is found on clean workstations, the setup package automatically chains Microsoft SQL Server 2022 Express installation.

### 3.3 Protected Shared Storage & Security ACLs
The installer automatically creates the shared directory structure under `%ProgramData%\Clovent\BusinessOperatingSystem` with least-privilege NTFS ACLs:
- **Sensitive Stores (Config, License, State):** Administrators & SYSTEM: Full Control; Standard Users: Read & Execute (`system-full admins-full users-readexec`).
- **Operational Stores (Logs, ContinuityJournal, OperationalCache, CartCheckpoints):** Administrators & SYSTEM: Full Control; Standard Users: Modify (`system-full admins-full users-modify`).

---

## 4. Database Schema Verification Steps

During installation or upgrade, the silent provisioner CLI (`Clovent.Installer.Provisioner.exe`) applies all pending Entity Framework Core migrations across all bounded schemas.

### 4.1 Schema Verification Queries
To manually verify schema health against `Clovent_BusinessOperatingSystem`, execute the following verification script in SQL Server Management Studio (SSMS) or `sqlcmd`:

```sql
USE [Clovent_BusinessOperatingSystem];
GO

-- 1. Verify Bounded Migration History
SELECT [ContextKey], [MigrationId], [ProductVersion] 
FROM [dbo].[__EFMigrationsHistory]
ORDER BY [MigrationId] DESC;

-- Verify 1.3.0 Catalog Tax Profiles Migration:
-- Expected: '20261008151934_AddTaxProfiles'
SELECT COUNT(*) AS [TaxProfilesMigrationPresent]
FROM [dbo].[__EFMigrationsHistory]
WHERE [MigrationId] = '20261008151934_AddTaxProfiles';

-- Verify 1.3.0 Restaurant Refunds, Taxes, and Sync Migration:
-- Expected: '20261008152015_AddRefundDomainAndTaxSnapshots'
SELECT COUNT(*) AS [RefundsAndSyncMigrationPresent]
FROM [dbo].[__EFMigrationsHistory]
WHERE [MigrationId] = '20261008152015_AddRefundDomainAndTaxSnapshots';

-- 2. Verify New Physical Tables Exist in Dedicated Schemas
SELECT TABLE_SCHEMA, TABLE_NAME 
FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME IN (
    'TaxProfiles',
    'Refunds',
    'RefundLines',
    'QuickBooksSyncMaps',
    'SyncInboxRecords',
    'SyncConflictRecords',
    'TerminalShiftSyncSummaries'
)
ORDER BY TABLE_SCHEMA, TABLE_NAME;

-- 3. Verify Refund Foreign Keys and Constraints
SELECT CONSTRAINT_NAME, CONSTRAINT_TYPE 
FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS
WHERE TABLE_NAME IN ('Refunds', 'RefundLines');
```

### 4.2 Migration Rollout Order
1. `[Catalog].[TaxProfiles]` — Configures tax categories and regional tax percentages.
2. `[Restaurant].[Refunds]` & `[Restaurant].[RefundLines]` — Implements immutable refund ledger records.
3. `[Restaurant].[QuickBooksSyncMaps]` — Stores mapping IDs for bi-directional QuickBooks synchronization.
4. `[Restaurant].[SyncInboxRecords]` & `[Restaurant].[SyncConflictRecords]` — Stores multi-terminal delta synchronization queues.
5. `[Restaurant].[TerminalShiftSyncSummaries]` — Consolidates terminal-level shift close summaries.

---

## 5. Artifact Manifest & Cryptographic Integrity

### 5.1 Final Distributables
| Deliverable | Path | Size | SHA-256 Checksum |
| :--- | :--- | :--- | :--- |
| **Production Setup Executable** | `artifacts/installer/Clovent.BusinessOperatingSystem-1.3.0-Setup.exe` | 115,894,297 bytes | `20DAA111999FFBBE966901C30DF72835E45CBCC270EB73788721192622A0FBFB` |
| **SHA-256 Manifest** | `artifacts/installer/SHA256SUMS.txt` | 100 bytes | N/A |
| **Self-Contained Binaries** | `artifacts/release/Clovent.BusinessOperatingSystem-win-x64/` | 473 files | Verified via ReleaseGuard |
| **Database Provisioner CLI** | `tools/Clovent.Installer.Provisioner/bin/publish/Clovent.Installer.Provisioner.exe` | 64,888 bytes | First-Party Deliverable |

### 5.2 ReleaseGuard Package Hygiene Scan
The automated ReleaseGuard scan script (`tools/ReleaseGuard/ScanReleasePackage.ps1`) executed against `artifacts/release/Clovent.BusinessOperatingSystem-win-x64`:
- **Files Inspected:** 473 total binary and resource files.
- **PDB Debug Symbols Found:** 0 (`*.pdb` strictly purged).
- **Development Configurations Found:** 0 (`appsettings.Development.json` purged).
- **Development/Test Licenses Found:** 0 (`*.lic` excluded from generic distribution).
- **Private Cryptographic Keys Found:** 0 (`*.pem`, `*.key`, `*.pfx`, `<RSAKeyValue>` markers).
- **Plaintext Secret Strings Found:** 0 (`Admin123!`, plaintext connection strings).
- **Result:** `PASS` (Security Gate Approved).

---

## 6. Automated Test Suite & Quality Verification

Full-solution compilation and automated test execution across all 21 test projects:

| Layer / Test Project | Passed | Failed | Skipped | Status |
| :--- | :---: | :---: | :---: | :---: |
| `Clovent.Domain.Tests` | 15 | 0 | 0 | GREEN |
| `Clovent.Platform.Tests` | 60 | 0 | 0 | GREEN |
| `Clovent.Authentication.Tests` | 110 | 0 | 0 | GREEN |
| `Clovent.Authentication.Application.Tests` | 55 | 0 | 0 | GREEN |
| `Clovent.Authentication.Infrastructure.Tests` | 21 | 0 | 0 | GREEN |
| `Clovent.Identity.Tests` | 132 | 0 | 0 | GREEN |
| `Clovent.Identity.Application.Tests` | 61 | 0 | 0 | GREEN |
| `Clovent.Identity.Infrastructure.Tests` | 32 | 0 | 0 | GREEN |
| `Clovent.MasterData.Tests` | 46 | 0 | 0 | GREEN |
| `Clovent.MasterData.Application.Tests` | 38 | 0 | 0 | GREEN |
| `Clovent.MasterData.Infrastructure.Tests` | 31 | 0 | 0 | GREEN |
| `Clovent.Catalog.Tests` | 45 | 0 | 0 | GREEN |
| `Clovent.Catalog.Application.Tests` | 39 | 0 | 0 | GREEN |
| `Clovent.Catalog.Infrastructure.Tests` | 31 | 0 | 0 | GREEN |
| `Clovent.Inventory.Tests` | 23 | 0 | 0 | GREEN |
| `Clovent.Inventory.Application.Tests` | 23 | 0 | 0 | GREEN |
| `Clovent.Inventory.Infrastructure.Tests` | 17 | 0 | 0 | GREEN |
| `Clovent.Restaurant.Tests` | 177 | 0 | 0 | GREEN |
| `Clovent.Restaurant.Application.Tests` | 414 | 0 | 0 | GREEN |
| `Clovent.Restaurant.Infrastructure.Tests` | 105 | 0 | 0 | GREEN |
| `Clovent.Desktop.Tests` | 825 | 0 | 7* | GREEN |
| **Total Automated Tests** | **2,299** | **0** | **7** | **100% GREEN** |

*\*Note: 7 skipped tests in `Clovent.Desktop.Tests` correspond to interactive live UI test fixtures (`LiveAcceptanceQaRun`, visual layout screenshot renderers) which are non-interactive by design during headless CI execution.*

---

## 7. Pilot Sign-Off Certification

- **Build Quality:** Solution compiles with 0 Errors and 0 Warnings in Release configuration.
- **Architectural Conformance:** Strict alignment with `AGENTS.md` and `.agents/rules/` regarding financial ledger immutability, DPAPI security, single physical database schema isolation, and exact tested artifact packaging.
- **Release Qualification:** CBOS 1.3.0 Release Candidate 1 is fully packaged, signed/hashed, and ready for deployment to pilot restaurant terminals.
