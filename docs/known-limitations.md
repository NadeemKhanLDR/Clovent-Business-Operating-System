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

### 2.1 Refund & Return Domain (TASK-10 Verification & Gap Analysis)
- **Current State:** **CORE REFUND AGGREGATE IMPLEMENTED; GATEWAY REVERSAL IS MANUAL/EXTERNAL**.
- **Domain & Application Capabilities (Implemented in CBOS 1.2.3):**
  - **Full & Partial Refunds:** Fully supported via `ProcessRefundCommand`, `Refund` aggregate, and `RefundLine`. Cashiers can refund specific quantities of order items.
  - **Multiple Partial Refunds:** Cumulative refunded amount is tracked on `Order` (`TotalRefundedAmount`), bounded strictly by the order's gross total.
  - **Settlement Channels:** Supports `RefundSettlementMethod.CashPayout` (deducts cash from active cashier shift drawer) and `RefundSettlementMethod.CustomerAccountCredit` (posts credit entry to `CustomerLedgerEntries`).
  - **Inventory Restock:** Each refund line supports optional restocking (`InventoryDisposition.Restock` triggers inventory adjustment).
  - **Idempotency & Audit Trail:** Idempotent command processing via `IdempotencyKey`, linking refund to `CashierShiftId`, `TerminalId`, and audit logs.
  - **Day Close Reconciliation:** Cash refunds are explicitly segregated from gross collections and deducted from `ExpectedClosingCash` in `BusinessDaySummaryDto`.
- **Known Limitations & Architectural Gaps:**
  - **Automated Card/Digital Processor Gateway Reversals (GAP):** CBOS does not integrate with card processor payment APIs. If a customer originally paid via credit card or digital wallet, CBOS cannot automatically transmit a reversal to the acquirer gateway. The refund must be recorded either as an over-the-counter cash refund (`CashPayout`) or as a store credit (`CustomerAccountCredit`), and the physical card reversal must be processed manually on the external EFTPOS terminal.
  - **Original Payment Method Restriction Enforcement:** While the refund dialog shows the original tender type, the system does not forcibly restrict refunds to the exact original tender due to the absence of direct payment gateway APIs.
- **Roadmap Target:** Direct EMV / payment gateway reversal integration is scheduled for CBOS 1.4.0+.

### 2.2 Payment Processor Direct Integration & Payment Identity (TASK-05)
- **Current State:** **MANUAL TENDER CLASSIFICATION ONLY**.
- **Impact:** CBOS does not integrate directly with bank card acquirers, payment gateways, or EMV card readers.
- **Workflow:** Cashiers process card transactions on external, standalone bank terminals and record the tender as a manual "Card" entry in the CBOS tender strip.
- **Compliance & Security:** CBOS is **not** PCI-DSS certified and does **not** process, capture, or store cardholder Primary Account Numbers (PAN), CVVs, or magnetic stripe track data.
- **Payment Identity, Concurrency & Manager Approval (TASK-05):** In CBOS 1.2.3 hardening, `PaymentAttemptId` is generated once per intentional payment attempt, reused across retries, retransmissions, and duplicate UI submissions; reusing an ID with conflicting details is rejected. Only the identified `IdempotencyKey` unique-constraint conflict (`IX_Payments_IdempotencyKey`) enters duplicate-success recovery (never treat every `DbUpdateException` as success; rollback/clean-context and immutable-detail checks required; test different attempt IDs competing for the same balance; the unique index does not solve all concurrency problems). A dedicated manager-approval table is not mandatory for the 1.2.3 pilot; trusted application-layer validation binding the attempt ID and session with safe retry semantics is required.

### 2.3 Financial Rounding & Currency Precision (TASK-03)
- **Current State:** **2-DECIMAL TRANSACTIONAL CURRENCY SCOPE**.
- **Scope & Permitted Precision:** CBOS 1.2.x transactional currency applies to final transaction records, payable tender, and customer ledger balances (e.g. PKR, USD, EUR). The system explicitly permits higher precision for intermediate calculations, tax/discount rates, unit pricing (`decimal(18,4)`), and inventory quantities/conversion factors (`decimal(18,4)`). Do not claim new currency or higher-precision storage support beyond the existing schema.
- **Selected Pilot Midpoint Direction & TASK-03 Contract:** Core order calculations in 1.2.2 lack a centralized rounding policy. `MidpointRounding.AwayFromZero` is recorded as the **selected pilot midpoint direction** for `MoneyRoundingPolicy`. The currently written tax/discount/service-charge sequence is **provisional**; TASK-03 must establish the formally supported contract with combined examples of order discounts with inclusive/exclusive taxes and service charges (separate arithmetic examples do not constitute approval of full sequence). Historical snapshots are immutable; replay must preserve sale-time financial facts rather than reprice transactions.

### 2.4 Customer Accounts & Ledger Semantics
- **Approved Current Behavior:** Customer credit transactions post to `[Restaurant].[CustomerLedgerEntries]` as a single-entry subledger recording debits, credits, and running balance (`OutstandingBalance`).
- **Separately Reviewed Contract Required:** Unsupported mandates for dual entries in `CustomerLedgerEntries`, automatic advance creation/consumption, and specific new fields are removed. Approved current behavior is preserved; introducing dual entries or automated advance lifecycle semantics requires a separately reviewed financial contract before adoption.

### 2.5 Inventory, Recipe Costing & Theoretical vs. Verified Actual Profit (TASK-11)
- **Current State:** **RECIPE BOM EXPLOSION, UOM CONVERSION & THEORETICAL COSTING IMPLEMENTED (CBOS 1.2.3)**.
- **Implemented Capabilities:**
  - **Universal Kitchen Unit Conversion Engine:** Standardized conversion across mass (KG, G, MG, LB, OZ), volume (L, ML, CL, CUP, TBSP, TSP, FL_OZ), and count units (PCS, EACH, SLICE, PORTION, DOZEN) with 4-decimal precision (`decimal(18,4)`) and `MidpointRounding.AwayFromZero`.
  - **Recipe Aggregate & Bill of Materials (BOM):** Prepared sellable variants decompose into constituent raw ingredients with trim/prep wastage loss percentages (`GrossQuantityRequired`), optional modifier consumption rules, and immutable version snapshots.
  - **Sales-to-Inventory Outbox Decomposition:** Order completion atomically dispatches inventory posting; items with active recipes explode into raw ingredient warehouse stock deductions; line modifiers consume modifier stock; resale items without recipes deduct finished goods directly; idempotent retry avoids double deductions.
  - **Zero-Migration Durable Persistence:** `JsonRecipeRepository` persists recipe aggregates to `%ProgramData%\Clovent\Recipes\recipes.json` using atomic temporary file replacement, preserving zero database migrations for CBOS 1.2.3.
- **Critical Architectural Boundary — Theoretical Food Cost vs. Verified Actual Profit:**
  - **Theoretical Recipe Costing Scope:** Recipe cost calculations and menu engineering reports (`GetRecipeCostReportQuery`) compute theoretical food costs based on bill-of-materials ingredient formulas and active purchasing unit costs.
  - **Audited Financial P&L Distinction:** Theoretical food cost reflects target/standard kitchen consumption under ideal execution. Actual verified business profit is strictly derived from the double-entry financial ledger and cash drawer reconciliation (actual customer money received minus verified purchase invoices, physical shrinkage, and spoilage). Theoretical margins must never be reported as audited financial profit.
- **Operational Boundaries & Roadmap:**
  - **Electronic Data Interchange (EDI) Supplier Integration:** CBOS does not integrate directly with supplier EDI/EDIFACT procurement protocols. Purchasing and supplier receiving remain manual operational workflows via `OpenOrReceiveStockCommand`.
  - **Automated Cyclic Stocktake Terminals:** Stocktake adjustments are recorded via `StockAdjustment` commands. Automated mobile barcode terminal syncing for physical inventory counts is planned for CBOS 1.4.0+.

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
- **Remediation & Terminal-Level Protection (TASK-02):** Process-local throttling alone does not satisfy terminal-level protection against brute force. Terminal-level rate limiting, progressive delays, and bounded brute-force defense addressing restart and multiple-instance bypass without database migrations are scheduled for CBOS 1.2.3 under **TASK-02: PIN Throttling + Development Seed Protection**. If not resolved during implementation, that limitation remains open for pilot acceptance (UI delays alone do not satisfy the requirement).

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
