# CBOS Known Limitations & Architectural Boundaries

| Attribute | Details |
| :--- | :--- |
| **Area** | Architecture, Engineering & Field Support |
| **Audience** | Technical Leadership, Architects, Operations, QA, Support |
| **Last Reviewed Version** | CBOS 1.2.2 |
| **Status** | **FACTUAL BASELINE** |
| **Classification** | **PUBLIC-SAFE** |

---

## 1. Executive Summary

This document captures verified, factual limitations and architectural boundaries present in the **Clovent Business Operating System (CBOS) 1.2.2** internal acceptance baseline. Recording these boundaries prevents unsupported assumptions, ensures honest representation during evaluations, and guides engineering hardening for the CBOS 1.2.3 pilot milestone and beyond.

---

## 2. Point of Sale & Financial Operations

### 2.1 Refund & Return Domain
- **Current State:** **REFUND DOMAIN NOT YET IMPLEMENTED**.
- **Impact:** CBOS 1.2.2 does not contain a dedicated Refund aggregate. Customer return slips, credit vouchers, partial item returns against completed orders, and refund tender transactions are not modeled in the domain layer.
- **Policy Requirement vs. Source Behavior (CBOS 1.2.3):** Voiding or pseudo-refunding already completed orders is strictly prohibited by policy during the attended single-terminal pilot to protect ledger integrity. In the 1.2.2 baseline source code, `Order.Void()` does not yet check if an order is completed; enforcing this restriction at domain and UI levels, along with BalanceEpsilon elimination, is scheduled for CBOS 1.2.3 under **TASK-04: BalanceEpsilon Elimination + Completed Void Restriction**.
- **Roadmap Target:** A formal compensating Refund & Return domain aggregate is scheduled for CBOS 1.3.0.

### 2.2 Payment Processor Direct Integration & Payment Identity (TASK-05)
- **Current State:** **MANUAL TENDER CLASSIFICATION ONLY**.
- **Impact:** CBOS does not integrate directly with bank card acquirers, payment gateways, or EMV card readers.
- **Workflow:** Cashiers process card transactions on external, standalone bank terminals and record the tender as a manual "Card" entry in the CBOS tender strip.
- **Compliance & Security:** CBOS is **not** PCI-DSS certified and does **not** process, capture, or store cardholder Primary Account Numbers (PAN), CVVs, or magnetic stripe track data.
- **Payment Identity Rule (TASK-05: Payment Idempotency + Credit-Limit Approval):** In CBOS 1.2.3 hardening, `PaymentAttemptId` is generated once per intentional payment attempt, reused across retries, retransmissions, and duplicate UI submissions. A genuinely separate intentional payment receives a new ID; reusing an ID with conflicting payment details must be rejected (without prescribing a new schema for this documentation task).

### 2.3 Financial Rounding & Currency Precision (TASK-03)
- **Current State:** **2-DECIMAL TRANSACTIONAL CURRENCY SCOPE**.
- **Scope & Permitted Precision:** CBOS 1.2.x transactional currency applies to final transaction records, payable tender, and customer ledger balances (e.g. PKR, USD, EUR). The system explicitly permits higher precision for intermediate calculations, tax/discount rates, unit pricing (`decimal(18,4)`), and inventory quantities/conversion factors (`decimal(18,4)`). Do not claim new currency or higher-precision storage support beyond the existing schema.
- **Selected Pilot Midpoint Direction & TASK-03 Contract:** Core order calculations in 1.2.2 lack a centralized rounding policy. `MidpointRounding.AwayFromZero` is recorded as the **selected pilot midpoint direction** for `MoneyRoundingPolicy`, pending completion of **TASK-03: Financial Rounding Contract** (which must complete rounding boundaries, tax/discount ordering, allocations/residual cents, and mathematical consistency across online sales, Continuity/replay, receipts, payments, Day Close, and all sale paths).

### 2.4 Customer Accounts & Ledger Semantics
- **Approved Current Behavior:** Customer credit transactions post to `[Restaurant].[CustomerLedgerEntries]` as a single-entry subledger recording debits, credits, and running balance (`OutstandingBalance`).
- **Separately Reviewed Contract Required:** Unsupported mandates for dual entries in `CustomerLedgerEntries`, automatic advance creation/consumption, and specific new fields are removed. Approved current behavior is preserved; introducing dual entries or automated advance lifecycle semantics requires a separately reviewed financial contract before adoption.

---

## 3. Resilience, Continuity & Deployment Scope

### 3.1 Single-Terminal Attended Scope Only
- **Current State:** **SINGLE-TERMINAL ATTENDED ONLY**.
- **Impact:** CBOS 1.2.2 / 1.2.3 is bounded strictly to single-terminal, attended operations.
- **Resilience Boundary:** Continuity Mode remains strictly terminal-local, secured via machine-bound Windows DPAPI and HMAC signatures. Distributed peer-to-peer LAN continuity synchronization and headquarters replication are **unapproved proposals** and are NOT part of accepted roadmap scope. Multi-till database concurrency against a shared SQL Server instance and multi-branch isolation are future milestone targets.

### 3.2 Restricted Cash-Only Continuity Mode
- **Current State:** **CASH-ONLY OFFLINE TRADING**.
- **Impact:** When primary SQL Server connectivity is interrupted, Continuity Mode enforces a strict cash-only policy against cached menu items.
- **Policy:** Customer credit ("On Account"), credit limit overrides, account balance lookups, and setup modifications are completely blocked while offline. Transactions are journaled locally and reconciled upon reconnection.

---

## 4. Security & Access Control

### 4.1 Application-Layer Authorization Enforcement
- **Current State:** **PARTIALLY IMPLEMENTED (TARGETED FOR 1.2.3)**.
- **Status:** UI-level visibility controls (hiding/disabling menu items across 214 permissions) are active. However, known gaps exist at the application boundary:
  - `AdministrativePrivilegeChecker` contains a fast-path username check (`admin`/`administrator`) bypassing role evaluation.
  - `ManagerAuthorizationForm` fails open if the authorization service is null or unconfigured.
  - Universal MediatR command pipeline authorization behaviors (`IPipelineBehavior`) are partially implemented.
- **Remediation:** Full fail-closed enforcement and removal of username bypass are scheduled for CBOS 1.2.3 under **TASK-01: RBAC & Manager Elevation Hardening**.

### 4.2 Interactive PIN Authentication Throttling
- **Current State:** **KNOWN GAP IN 1.2.2 (TARGETED FOR 1.2.3)**.
- **Impact:** PIN-only authentication resolves users from submitted PIN hashes. Unmatched PIN submissions do not associate with a user account, bypassing per-user lockout policies.
- **Remediation:** Terminal-level rate limiting, progressive delays, and bounded brute-force protection are scheduled for CBOS 1.2.3 under **TASK-02: PIN Throttling + Development Seed Protection**.

---

## 5. Operations, Diagnostics & Database

### 5.1 Database Backup & Disaster Recovery
- **Current State:** **MANUAL / PRE-UPGRADE BACKUP ONLY**.
- **Impact:** CBOS 1.2.2 provides an on-demand `DatabaseBackupService` executed prior to commissioning and migrations. There is **no automated background maintenance service** (`CloventMaintenanceService` is not implemented), no automated scheduled backup engine, and no automated restore drill.
- **Platform Facts (SQL Server Express):** Express edition does not include SQL Server Agent, native backup compression, or native backup encryption.
- **TDE vs. Native Backup Encryption:** Transparent Data Encryption (TDE - data-at-rest encryption of database files) and native backup encryption (`BACKUP ... WITH ENCRYPTION`) are distinct SQL Server features; neither is available in Express.
- **Disaster Recovery DPAPI Rule & Future Design:** While local machine DPAPI protects local credentials and terminal caches, **disaster recovery must NOT depend solely on the original machine's DPAPI**. Off-machine backup archives must utilize portable, machine-independent protection so that data can be recovered on replacement hardware. Specific backup archive technology selection represents future design work (CBOS 1.3.1+) and is not a governance-closure blocker. No unsupported guarantees of eliminating all operational data-loss risk are made.

### 5.2 Observability & Diagnostics
- **Current State:** **BASIC TEXT-FILE LOGGING**.
- **Impact:** Diagnostic logging relies on `FileLoggerProvider` writing plain text entries to local application directories.
- **Known Limitations:**
  - Exception stack traces are omitted (logging only exception type and message).
  - Inconsistent log directory paths across certain components.
  - No automated diagnostic bundle exporter for support personnel.
  - No EF Core SQL command interceptor for query timing and telemetry.
- **Roadmap & Proposals:** Transitioning to structured JSON logging, stack trace capture, and diagnostic support packages is planned for CBOS 1.3.1+. Specific framework choices (such as Serilog, OpenTelemetry, or enhanced Microsoft.Extensions.Logging) are candidate proposals under evaluation, not permanent architectural mandates.

### 5.3 External Accounting Integration (QuickBooks)
- **Current State:** **ARCHITECTURE IMPLEMENTED / PRODUCTION INTEGRATION NOT VALIDATED**.
- **Impact:** The Transactional Outbox and gateway interface (`IQuickBooksGateway`) are structurally implemented. However, the runtime currently uses `DefaultQuickBooksGateway`, an in-memory test simulation.
- **Roadmap:** Production OAuth2 / REST gateway communication with live QuickBooks Online is planned for a subsequent commercial release.

---

## 6. Cross References
- [Canonical Engineering Roadmap](roadmap/engineering-roadmap.md)
- [Product Readiness Levels](development/readiness-levels.md)
- [Financial Integrity Rules](../.agents/rules/financial-integrity.md)
- [Security & Licensing Rules](../.agents/rules/security.md)
- [PDR Registry](product/pdr/README.md)
