# CBOS Documentation Gaps & Open Architectural Questions

| Attribute | Details |
| :--- | :--- |
| **Area** | Documentation Quality & Engineering Governance |
| **Audience** | Chief Architect, Engineering Leads, Security Officer |
| **Last Reviewed Version** | CBOS 1.2.2 |
| **Status** | **ACTIVE GAP RECORD** |
| **Classification** | **PUBLIC-SAFE** |

---

## 1. Executive Summary

In adherence to the CBOS Documentation Principles, technical documentation must never guess or fabricate unverified software behavior. Where codebase inspection reveals ambiguities, partially implemented features without explicit specifications, or pending architectural choices, they are formally recorded in this Gap Report.

---

## 2. Identified Documentation Gaps & Open Architectural Questions

### Gap 1: Multi-Terminal P2P Continuity Replication
- **Question:** How will offline transactions be reconciled and shared between multiple POS registers if an extended network outage spans several days?
- **Source Inspected:**
  - `src/Clovent.Restaurant.Infrastructure/Continuity/ProtectedContinuityJournalStore.cs`
  - `src/Clovent.Desktop/Restaurant/Services/ContinuityCoordinator.cs`
- **Why Unclear:** The current codebase implements single-terminal emergency journal append and replay (`continuity_journal.dat`). There is no peer-to-peer LAN replication protocol or local SQLite shared replica across registers.
- **Required Owner/Decision:** System Architect / Resilience Working Group to specify multi-terminal offline synchronization topology.
- **Affected Documentation:** `docs/resilience/continuity-mode.md`, `docs/deployment/topologies.md`.

---

### Gap 2: Production QuickBooks Online Authentication & Rate Limiting Strategy
- **Question:** What is the approved token storage mechanism and tenant mapping strategy for multi-company QuickBooks Online sync?
- **Source Inspected:**
  - `src/Clovent.Restaurant.Application/Outbox/Handlers/QuickBooksSyncOutboxHandler.cs`
  - `src/Clovent.Restaurant.Application/QuickBooks/IQuickBooksGateway.cs`
- **Why Unclear:** `DefaultQuickBooksGateway` is an in-memory simulation designed for resilience tests. The production OAuth2 token refresh workflow, rate-limiting backoff strategy, and QuickBooks account mapping contracts are not yet modeled in domain code.
- **Required Owner/Decision:** Integrations Lead to define OAuth2 token vaulting (DPAPI vs Azure KeyVault) and multi-currency ledger account mapping.
- **Affected Documentation:** `docs/integrations/quickbooks.md`.

---

### Gap 3: Machine-Wide vs. Per-User Workstation Configuration Scope
- **Question:** Should POS workstation identity (`TerminalId`, `BranchId`) and display settings migrate from `%LOCALAPPDATA%` to machine-wide `%ProgramData%`?
- **Source Inspected:**
  - `src/Clovent.Desktop/Forms/Base/PosSettingsStore.cs`
  - `src/Clovent.Desktop/Forms/Base/CompanyDisplaySettings.cs`
  - `src/Clovent.Desktop/Commissioning/Security/ProgramDataAclManager.cs`
- **Why Unclear:** While database connection settings and license state reside machine-wide in `%ProgramData%\Clovent\BusinessOperatingSystem`, terminal binding and display preferences currently default to `%LOCALAPPDATA%\Clovent`. If multiple Windows users log into the same POS hardware, settings are scoped per-user.
- **Required Owner/Decision:** Desktop Architecture Team to approve migrating `pos_settings.json` to machine-wide scope with standard user write ACLs.
- **Affected Documentation:** `docs/configuration/configuration-architecture.md`, `docs/configuration/terminal-identity.md`.

---

### Gap 4: Direct ESC/POS Driver Specification
- **Question:** Will direct ESC/POS hardware printing support raw TCP network sockets, virtual COM ports, and raw USB endpoints concurrently?
- **Source Inspected:**
  - `src/Clovent.Desktop/Restaurant/Orders/ReceiptPrintDocument.cs`
  - `src/Clovent.Restaurant.Application/Printing/IReceiptPrintService.cs`
- **Why Unclear:** The current shipping solution relies exclusively on Windows print spooler GDI rendering via .NET `PrintDocument`. The planned ESC/POS command generator interface has not yet been checked into the repository.
- **Required Owner/Decision:** Hardware Integration Lead to define `IEscPosPrinterClient` interface and cut-feed code dialect support (Epson vs Star).
- **Affected Documentation:** `docs/integrations/printing.md`.

---

### Gap 5: Authoritative Pipeline Authorization Enforcement Timeline
- **Question:** In which sprint will MediatR pipeline behaviors (`IPipelineBehavior`) be made mandatory across all bounded contexts for application-level command authorization?
- **Source Inspected:**
  - `src/Clovent.Identity.Application/Authorization/`
  - `src/Clovent.Desktop/Forms/Base/AdministrativePrivilegeChecker.cs`
- **Why Unclear:** UI-level button/ribbon disabling is fully operational, but application command authorization is currently enforced ad-hoc in specific handlers rather than uniformly in the mediator pipeline.
- **Required Owner/Decision:** Security Architect & Application Lead to finalize MediatR authorization behavior rollout schedule.
- **Affected Documentation:** `docs/security/authorization.md`, `docs/security/security-architecture.md`.

---

## 3. Maintenance Policy

As architectural decisions are formalized and corresponding code is committed, items in this report must be removed or marked resolved, and the affected documentation updated accordingly.

---

## 4. Cross References
- [Documentation Coverage Matrix](documentation-coverage.md)
- [Known Limitations](known-limitations.md)
- [System Architecture](architecture/system-architecture.md)
