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
- **Pilot Rule (CBOS 1.2.3):** Voiding or pseudo-refunding already completed orders is strictly prohibited during the attended single-terminal pilot to protect ledger integrity.
- **Roadmap Target:** A formal compensating Refund & Return domain aggregate is scheduled for CBOS 1.3.0.

### 2.2 Payment Processor Direct Integration
- **Current State:** **MANUAL TENDER CLASSIFICATION ONLY**.
- **Impact:** CBOS does not integrate directly with bank card acquirers, payment gateways, or EMV card readers.
- **Workflow:** Cashiers process card transactions on external, standalone bank terminals and record the tender as a manual "Card" entry in the CBOS tender strip.
- **Compliance & Security:** CBOS is **not** PCI-DSS certified and does **not** process, capture, or store cardholder Primary Account Numbers (PAN), CVVs, or magnetic stripe track data.

### 2.3 Financial Rounding & Currency Precision
- **Current State:** **2-DECIMAL CURRENCY SCOPE ONLY**.
- **Scope:** CBOS 1.2.x transactional financial calculation supports 2-decimal currencies exclusively (e.g. PKR, USD, EUR). High-precision 3-decimal currencies are not supported in 1.2.x.
- **Rounding Strategy:** Core order calculations currently lack a centralized rounding policy. Formal adoption of `MoneyRoundingPolicy` and resolution of the midpoint rounding standard (AwayFromZero vs ToEven) is targeted for CBOS 1.2.3.

---

## 3. Resilience, Continuity & Deployment Scope

### 3.1 Single-Terminal Attended Scope Only
- **Current State:** **SINGLE-TERMINAL ONLY**.
- **Impact:** CBOS 1.2.2 / 1.2.3 is bounded strictly to single-terminal, attended operations.
- **Roadmap:** Multi-terminal peer synchronization, cross-terminal order handoff, and multi-branch data replication are planned for CBOS 1.4.0+.

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
- **Remediation:** Full fail-closed enforcement and removal of username bypass are scheduled for CBOS 1.2.3.

### 4.2 Interactive PIN Authentication Throttling
- **Current State:** **KNOWN GAP IN 1.2.2 (TARGETED FOR 1.2.3)**.
- **Impact:** PIN-only authentication resolves users from submitted PIN hashes. Unmatched PIN submissions do not associate with a user account, bypassing per-user lockout policies.
- **Remediation:** Terminal-level rate limiting, progressive delays, and bounded brute-force protection are scheduled for CBOS 1.2.3.

---

## 5. Operations, Diagnostics & Database

### 5.1 Database Backup & Disaster Recovery
- **Current State:** **MANUAL / PRE-UPGRADE BACKUP ONLY**.
- **Impact:** CBOS 1.2.2 provides an on-demand `DatabaseBackupService` executed prior to commissioning and migrations. There is **no automated background maintenance service** (`CloventMaintenanceService` is not implemented), no automated scheduled backup engine, and no automated restore drill.
- **Platform Facts (SQL Server Express):** Express edition does not include SQL Server Agent, native backup compression, or Transparent Data Encryption (TDE). Scheduled backups require external Windows Task Scheduler scripts.

### 5.2 Observability & Diagnostics
- **Current State:** **BASIC TEXT-FILE LOGGING**.
- **Impact:** Diagnostic logging relies on `FileLoggerProvider` writing plain text entries to local application directories.
- **Known Limitations:**
  - Exception stack traces are omitted (logging only exception type and message).
  - Inconsistent log directory paths across certain components.
  - No automated diagnostic bundle exporter for support personnel.
  - No EF Core SQL command interceptor for query timing and telemetry.
- **Roadmap:** Structured JSON logging, stack trace capture, and diagnostic support packages are planned for CBOS 1.3.1+.

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
