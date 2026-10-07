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

This document captures verified, factual limitations and architectural boundaries present in **Clovent Business Operating System (CBOS) 1.2.2**. This record prevents unsupported assumptions, ensures honest representation during customer deployments, and provides field support and operations engineers with clear boundaries for system capabilities.

---

## 2. Point of Sale & Business Operations

### 2.1 Refund & Return Domain
- **Current State:** **REFUND DOMAIN NOT YET IMPLEMENTED**.
- **Impact:** While voiding unpaid open orders and voiding completed transactions via managerial authorization is implemented, formal partial line returns, customer refund vouchers, and inventory restock credits are not yet modeled as first-class domain aggregates in `Clovent.Restaurant`.
- **Workaround:** Managerial voiding of completed orders or inventory adjustment issues.
- **Roadmap:** Formal Refund & Return aggregate planned for future release.

### 2.2 Payment Processor Direct Integration
- **Current State:** **EXTERNAL PAYMENT BOUNDARY ONLY**.
- **Impact:** CBOS does not integrate directly with bank card acquirers or credit card processing gateways (no integrated EFTPOS/EMV chip-and-pin SDKs).
- **Behavior:** Cashiers tender card payments on an external physical card machine and manually enter the authorization reference or card type in the CBOS tender strip.
- **Certification Note:** CBOS is **not** PCI-DSS certified and does not handle or store cardholder Primary Account Numbers (PAN) or sensitive authentication data.

### 2.3 Single Open Shift Per Cashier/Terminal
- **Current State:** A cashier can maintain only one active open shift session per terminal at any given time.
- **Impact:** Shift sharing across multiple concurrent cashiers on the same physical till is prohibited by domain rules.

---

## 3. Resilience & Offline Continuity

### 3.1 Restricted Cash-Only Continuity Policy
- **Current State:** When primary SQL Server database connectivity is lost, Continuity Mode activates and strictly enforces a **Cash-Only** sales policy against cached catalog items.
- **Impact:** Customer on-account sales, credit advances, customer account lookups, discount profile alterations, and administrative setup are completely blocked while offline.
- **Rationale:** Prevents uncollectible receivables, customer credit limit overruns, and duplicate sequence allocations.

### 3.2 Single Terminal Failover (No P2P Offline Sync)
- **Current State:** Each POS terminal maintains its own local emergency journal (`continuity_journal.dat`) during database downtime.
- **Impact:** Terminals operating offline do not communicate peer-to-peer with other terminals on the LAN. Central reconciliation and sequence de-duplication occur solely when connectivity to the central SQL Server database is restored.

---

## 4. External Integrations

### 4.1 QuickBooks Accounting Integration
- **Current State:** **SIMULATED GATEWAY IMPLEMENTATION**.
- **Impact:** The repository includes a complete Transactional Outbox pipeline (`QuickBooksSyncOutboxHandler`, `QuickBooksSyncPayload`, circuit breaker protection), but uses `DefaultQuickBooksGateway`, an in-memory simulated gateway designed for fault tolerance and testing.
- **Production Status:** Production QuickBooks Online / Desktop REST/OAuth API communication is not shipping in 1.2.2 and is **PLANNED FOR FUTURE RELEASE**.

### 4.2 Receipt Printing
- **Current State:** Plain-text GDI rendering via .NET's built-in `PrintDocument` (`ReceiptPrintDocument.cs`) targeting Windows thermal print queues.
- **Impact:** Direct low-level raw ESC/POS byte streaming via TCP sockets or raw USB serial ports is not yet implemented.
- **Production Requirement:** Printers must be installed as standard Windows printers on the workstation host OS.

---

## 5. Security & Authorization

### 5.1 Application-Layer Pipeline Authorization
- **Current State:** **PARTIALLY IMPLEMENTED**.
- **Status:** UI-layer gating (hiding and disabling ribbon items, buttons, and navigation nodes based on 214 granular permissions) is 100% implemented. Authoritative MediatR pipeline behaviors (`IPipelineBehavior`) validating permissions at the application command boundary are in progress and not yet universally applied across all 6 bounded contexts.

### 5.2 Authenticode Code Signing
- **Current State:** **PENDING SECURE RELEASE DISTRIBUTION**.
- **Status:** Release 1.2.2 binaries and installers are not signed with a public commercial Authenticode code-signing certificate in this development environment. Formal signing occurs during final release pipeline packaging.

---

## 6. Database & Operations

### 6.1 SQL Server Express Constraints
- **Platform Limitation:** When deployed on SQL Server Express, the following database platform constraints apply:
  - Maximum 1.4 GB RAM buffer pool per instance.
  - Maximum 10 GB relational data file size per database.
  - No SQL Server Agent (automated scheduled backups require Windows Task Scheduler).
  - No native backup compression or TDE encryption in Express editions.

### 6.2 Workstation Identity Scope
- **Current State:** Workstation POS terminal settings (`pos_settings.json`) and display format overrides (`company_display_settings.json`) reside in `%LOCALAPPDATA%\Clovent\`, tying configuration to the active Windows user profile rather than machine-wide `%ProgramData%`.
- **Remediation:** Centralization to machine-wide configuration files is planned for a subsequent maintenance release.

---

## 7. Cross References
- [Order Lifecycle Documentation](../pos/order-lifecycle.md)
- [Payments Architecture](../pos/payments.md)
- [Continuity Mode](../resilience/continuity-mode.md)
- [QuickBooks Integration](../integrations/quickbooks.md)
- [Printing Integration](../integrations/printing.md)
- [SQL Server Deployment](../deployment/sql-server.md)
