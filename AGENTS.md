# Clovent Business Operating System (CBOS) — Agent Instructions

This document is the **single authoritative source of truth** for all AI coding agents working on the Clovent Business Operating System repository. All coding agents (Antigravity, Gemini, subagents, and peers) must read and adhere to these instructions before planning or modifying code.

Specialized domain rule files in `.agents/rules/` provide deep implementation details and must be consulted for targeted work:
- Financial Integrity & Accounting: [financial-integrity.md](.agents/rules/financial-integrity.md)
- WinForms & High-DPI UI: [winforms-ui.md](.agents/rules/winforms-ui.md)
- Database & Persistence: [database.md](.agents/rules/database.md)
- Security & Licensing: [security.md](.agents/rules/security.md)
- Testing & Quality: [testing.md](.agents/rules/testing.md)
- Packaging & Release: [release.md](.agents/rules/release.md)

Supporting Governance Standards:
- Definition of Done: [docs/development/definition-of-done.md](docs/development/definition-of-done.md)
- Engineering Roadmap: [docs/roadmap/engineering-roadmap.md](docs/roadmap/engineering-roadmap.md)
- Product Decision Records: [docs/product/pdr/README.md](docs/product/pdr/README.md)

---

## 1. Project Identity & Technology Stack

- **Product Name:** Clovent Business Operating System (CBOS)
- **Current Baseline:** CBOS 1.2.2 — Frozen Internal Acceptance Baseline
- **Solution File:** `Clovent.BusinessOperatingSystem.slnx`
- **Primary Language:** C# (C# 13 / .NET 10)
- **Target Frameworks:**
  - `net10.0-windows` for Desktop application & Desktop tests (`src/Clovent.Desktop`, `src/Clovent.Desktop.Tests`)
  - `net10.0` for Domain, Application, Infrastructure, Platform, and CLI tools
- **Desktop UI Framework:** Windows Forms with **DevExpress 26.1** (`DevExpress.Win`, `DevExpress.Reporting.Core`, `DevExpress.Images`)
- **Database Engine:** Microsoft SQL Server
- **ORM / Persistence:** Entity Framework Core (`Microsoft.EntityFrameworkCore.SqlServer`, `Microsoft.EntityFrameworkCore.Design`)
- **Mediator / CQRS:** MediatR
- **Hosting & Dependency Injection:** `Microsoft.Extensions.Hosting`, `Microsoft.Extensions.DependencyInjection`
- **Testing Stack:** xUnit, `Microsoft.NET.Test.Sdk`, `coverlet.collector`, SQLite in-memory (`Microsoft.EntityFrameworkCore.Sqlite`) for isolated unit/integration tests

---

## 2. Core Architectural Invariants

### 1. FINANCIAL CORRECTNESS BEFORE PERFORMANCE
Financial accuracy, audit durability, and mathematical reconciliation outrank system throughput, cashier convenience, and execution speed without exception.

### 2. COMPLETED FINANCIAL HISTORY IS IMMUTABLE
Completed orders, payments, ledger entries, and day close snapshots are permanently immutable. No hard `DELETE` or in-place financial updates. Adjustments occur exclusively via compensating transactions.

### 3. AUTHORIZATION FAILS CLOSED
Any failure to resolve permission, missing authentication context, or unexpected error must deny access immediately. System administration elevation requires authenticated role verification, never hardcoded usernames. UI control hiding is usability, not authorization.

### 4. ONE PHYSICAL DATABASE / BOUNDED SCHEMAS
Exactly one physical SQL Server database: `Clovent_BusinessOperatingSystem`. Each bounded context maintains its own `DbContext` targeting its dedicated schema (`[Authentication]`, `[Identity]`, `[MasterData]`, `[Catalog]`, `[Inventory]`, `[Restaurant]`) with isolated migration history tables.

### 5. CACHE IS NOT AUTHORITATIVE
The local Operational Cache exists exclusively for fast UI rendering and read lookups during primary database connectivity. The SQL Server relational database is the single source of truth.

### 6. CONTINUITY IS CONTROLLED AND RECONCILABLE
Continuity Mode operates strictly in cash-only mode against validated cached catalog items. All emergency transactions are cryptographically journaled with DPAPI/HMAC and must be reconciled upon primary database reconnection.

### 7. AUTOMATED TESTS MUST NOT POLLUTE WORKSTATION STATE
Unit and integration tests must run in complete isolation (in-memory SQLite, temporary directories). Tests must never modify `%ProgramData%`, local application settings, registry keys, or live SQL databases.

### 8. EXACT TESTED ARTIFACT = RELEASED ARTIFACT
The exact binary package tested and accepted in a clean Windows Sandbox is the final released artifact. Never rebuild, repackage, or alter binaries after acceptance qualification.

### 9. REAL RUNTIME EVIDENCE OUTRANKS SIMULATION
Truthful evidence reporting is mandatory. Distinguish `REAL SQL SERVER VALIDATED` from SQLite in-memory, `LIVE UI EXECUTED` from headless execution, and `CLEAN MACHINE ACCEPTED` from workstation test runs.

### 10. PARALLEL IMPLEMENTATION REQUIRES ISOLATED WORKTREES
Parallel agents and developers must operate in isolated Git worktrees. Do not edit shared files across uncoordinated workstreams.

### 11. PERSISTED CONTRACT VERSIONING
Outbox payloads, Continuity journals, Operational Cache tables, configuration JSON files, and cart checkpoints are persisted contracts. Any future contract changes require an explicit backwards-compatibility strategy.

### 12. CUSTOMER DATA PRIVACY & ACCESS GUARANTEE
Never commit, log, or distribute real customer business data. Use synthetic test data only. Customer historical records remain permanently accessible and exportable in read-only mode even after software license expiration.

---

## 3. High-Conflict File Governance

To prevent race conditions, merge conflicts, and regressions during parallel development, the following files are classified as **High-Conflict Files**:
1. `src/Clovent.Desktop/Restaurant/Orders/RestaurantPosForm.cs`
2. `src/Clovent.Desktop/Restaurant/Orders/RestaurantPosForm.Designer.cs`
3. `src/Clovent.Desktop/Program.cs`
4. `src/Clovent.Desktop/Startup/ApplicationBootstrapper.cs`
5. `src/Clovent.Desktop/Operations/OperationsHealthForm.cs`
6. `src/Clovent.Restaurant.Application/Orders/Commands/CompleteOrderCommand.cs`
7. `src/Clovent.Restaurant.Application/Orders/OrderTotalsCalculator.cs`
8. Inno Setup installer scripts (`installer/setup.iss` / `tools/**/*.iss`)

### High-Conflict Rules:
- **Single Active Owner:** Only one implementation stream or agent may modify a high-conflict file at any time.
- **Isolated Worktrees:** All implementation streams must operate in dedicated Git worktrees.
- **Concurrency Cap:** Maintain a maximum of 2–3 active implementation streams simultaneously.
- **Documentation Streams:** Documentation-only agents do not count toward this limit provided they modify zero production code files.

---

## 4. Bounded Context Structure & Code Placement

Each context maintains clean architecture separation:
- `Clovent.<Context>`: Domain layer (aggregates, entities, value objects, domain events, repository interfaces). Zero dependencies on outer layers.
- `Clovent.<Context>.Application`: Application layer (MediatR commands, queries, handlers, DTOs, domain service interfaces). References Domain.
- `Clovent.<Context>.Infrastructure`: Infrastructure & persistence (EF Core DbContext, entity configurations, repository implementations, migration snapshots). References Domain and Application.
- `Clovent.<Context>.Tests`, `*.Application.Tests`, `*.Infrastructure.Tests`: Unit and integration test suites.

### Strict Placement Rules:
- Commands, Queries, and DTOs: `src/Clovent.<Context>.Application/<Feature>/`.
- Entities, Enums, and Value Objects: `src/Clovent.<Context>/<Feature>/`.
- EF Configurations and Repositories: `src/Clovent.<Context>.Infrastructure/`.
- Forms, UserControls, and Dialogs: `src/Clovent.Desktop/<Context>/<Feature>/` or `src/Clovent.Desktop/Forms/<Context>/`.
- Tests: matching test project under matching feature namespaces.

---

## 5. Build, Test & Release Protocol

### Canonical Build Commands:
- Debug Build: `dotnet build Clovent.BusinessOperatingSystem.slnx -c Debug`
- Release Build: `dotnet build Clovent.BusinessOperatingSystem.slnx -c Release`
- Publish Client:
  ```powershell
  dotnet publish src\Clovent.Desktop\Clovent.Desktop.csproj -c Release -r win-x64 --self-contained true -o artifacts\release\Clovent.BusinessOperatingSystem-win-x64
  ```
- Automated Security Scan:
  ```powershell
  powershell -ExecutionPolicy Bypass -File tools\ReleaseGuard\ScanReleasePackage.ps1 -ReleaseDir artifacts\release\Clovent.BusinessOperatingSystem-win-x64
  ```

---

## 6. Task Final Report Standard

Every non-trivial task response must conclude with a structured technical report containing:
1. **Root Cause Analysis**
2. **Key Architectural & Business Decisions**
3. **Implementation Details**
4. **Files Modified / Created**
5. **Testing Verification:**
   - Targeted Tests run & results
   - Regression Tests run & results
   - Live UI Status: `LIVE UI EXECUTED` or `LIVE UI NOT EXECUTED`
   - Designer Status: `VISUAL STUDIO DESIGNER UI EXECUTED` or `VISUAL STUDIO DESIGNER UI NOT EXECUTED`
6. **Build Verification:** Debug and Release build status
7. **Risks, Edge Cases & Remaining Concerns**
8. **Final Status Statement**
