# Clovent Business Operating System (CBOS)

[![Build & Verification Status](https://img.shields.io/badge/Verification-1%2C824%20Passed%20%7C%200%20Failed-success)](#6-current-release-status--verification)
[![Release Version](https://img.shields.io/badge/Version-1.2.2-blue)](#6-current-release-status--verification)
[![Engineering Status](https://img.shields.io/badge/Status-Frozen%20Internal%20Baseline-orange)](#6-current-release-status--verification)

**Clovent Business Operating System (CBOS)** is an enterprise Point-of-Sale (POS) and retail/hospitality operating platform engineered for high-reliability workstation deployments. CBOS combines high-speed POS register workflows, back-office administration, inventory management, dynamic catalog and price tier engines, shift cash drawer controls, financial reporting, and offline resilience via Continuity Mode and protected local operational caching.

---

## 1. Core Capabilities

- **High-Velocity Restaurant POS:** 3-column unified layout supporting Dine-In (dining areas, table operations), Take Away, and Delivery workflows with touch-optimized numpad and tender strip.
- **Tender & Payment Processing:** Cash settlement, external credit/debit card recording with reconciliation fields, split tender across multiple methods, and Customer On-Account credit sales.
- **Quick Orders & Smart Combos:** Fast-service meal templates, dynamic combo builder with rule validation, and recommendation engines.
- **Inventory & Stock Management:** Warehouse-scoped inventory transactions across receipt, issue, transfer, adjustment, reserve, and release workflows with separate database vs display precision.
- **Back Office & Master Data:** Centralized management of organizations, companies, branches, dining areas, tables, terminals, currencies, units of measure, and number sequences.
- **Shift & Drawer Reconciliation:** Shift tracking with starting float, mid-shift cash in/out drops, expected vs counted cash, and reconciliation variance reporting.
- **Financial & Operational Reporting:** Shift summaries, customer accounts receivable (A/R) ledger, known gross profit margin tracking, and tax audit reports.
- **Offline Continuity Mode:** Automatic offline failover allowing cash-only orders to continue when SQL Server is unreachable, backed by an encrypted local append-only journal and replay engine.

---

## 2. Technology Stack

- **Primary Language:** C# 13
- **Target Runtime & Frameworks:**
  - `net10.0-windows` for Desktop shell and Desktop UI tests (`src/Clovent.Desktop`, `src/Clovent.Desktop.Tests`)
  - `net10.0` for Domain, Application, Infrastructure, Platform, and CLI Provisioner
- **Desktop UI Framework:** Windows Forms with **DevExpress 26.1** (`DevExpress.Win`, `DevExpress.Reporting.Core`, `DevExpress.Images`)
- **Database Engine:** Microsoft SQL Server (2019 / 2022 / Express / LocalDB)
- **Object-Relational Mapping (ORM):** Entity Framework Core (`Microsoft.EntityFrameworkCore.SqlServer`, `Microsoft.EntityFrameworkCore.Design`)
- **Mediator & CQRS:** MediatR
- **Hosting & Dependency Injection:** `Microsoft.Extensions.Hosting` and `Microsoft.Extensions.DependencyInjection`
- **Testing Stack:** xUnit, `Microsoft.NET.Test.Sdk`, `coverlet.collector`, SQLite in-memory (`Microsoft.EntityFrameworkCore.Sqlite`) for isolated unit/integration tests

---

## 3. Architecture Summary

CBOS implements strict Domain-Driven Design (DDD) with Clean Architecture across 6 independent bounded contexts:

```
src/
├── Clovent.Platform/            # Cross-cutting platform abstractions, environment, and encryption
├── Clovent.Authentication/      # Login attempts, session tokens, authentication audit events
├── Clovent.Identity/            # Users, roles, 214 granular permissions, organization hierarchy
├── Clovent.MasterData/          # Warehouses, terminals, currencies, units of measure, number sequences
├── Clovent.Catalog/             # Categories, brands, products, product variants, pricing tiers
├── Clovent.Inventory/           # Stocks, inventory movements, adjustments, reservations
├── Clovent.Restaurant/          # Dining areas, tables, orders, kitchen tickets, settlements, shifts
├── Clovent.Desktop/             # WinForms presentation shell, DevExpress ribbon, forms, dialogs
├── Clovent.Installer.Provisioner# Standalone database creation and schema provisioning utility
└── *.Tests/                     # Comprehensive xUnit test suites for each bounded context
```

### Database Persistence Model
- **Single Physical Database:** Exactly one physical SQL Server database: `Clovent_BusinessOperatingSystem`.
- **Schema Isolation:** Dedicated SQL schema per bounded context (`[Authentication]`, `[Identity]`, `[MasterData]`, `[Catalog]`, `[Inventory]`, `[Restaurant]`).
- **Independent Migration Histories:** Each bounded context manages migrations in its own schema-scoped table (`[<Schema>].[__EFMigrationsHistory]`).
- **Runtime Least Privilege:** Runtime application accounts (`cbos_app`) receive only `db_datareader`, `db_datawriter`, and `GRANT EXECUTE`. Schema modifications are restricted to provisioner/DBA accounts.

---

## 4. Build Prerequisites & Quick Start

### Build Prerequisites
1. **Operating System:** Windows 10 (1809+) or Windows 11 (64-bit).
2. **.NET 10 SDK:** Version `10.0.100` or higher.
3. **IDE:** Visual Studio 2022 (v17.12+) with *.NET desktop development* workload.
4. **DevExpress WinForms:** Version `26.1` installed or configured via local/authenticated NuGet feed.
5. **Database:** Microsoft SQL Server 2019/2022 or SQL Server Express (with mixed-mode SQL authentication enabled).

### Quick Start Commands

```powershell
# 1. Restore NuGet dependencies
dotnet restore Clovent.BusinessOperatingSystem.slnx

# 2. Compile Debug configuration
dotnet build Clovent.BusinessOperatingSystem.slnx -c Debug

# 3. Execute all automated test suites
dotnet test Clovent.BusinessOperatingSystem.slnx -c Debug

# 4. Compile Release configuration
dotnet build Clovent.BusinessOperatingSystem.slnx -c Release

# 5. Publish self-contained Win-x64 client
dotnet publish src\Clovent.Desktop\Clovent.Desktop.csproj -c Release -r win-x64 --self-contained true -o artifacts\release\Clovent.BusinessOperatingSystem-win-x64

# 6. Execute pre-distribution security scan
powershell -ExecutionPolicy Bypass -File tools\ReleaseGuard\ScanReleasePackage.ps1 -ReleaseDir artifacts\release\Clovent.BusinessOperatingSystem-win-x64
```

---

## 5. Security & Licensing Principles

- **Zero Shipped Credentials:** Production releases exclude default credentials, development configurations (`appsettings.Development.json`), debug symbols (`*.pdb`), and active license files.
- **DPAPI Credential Protection:** Runtime database credentials and local terminal configurations are encrypted using Windows Data Protection API (DPAPI).
- **Asymmetric RSA-2048 Licensing:** Cryptographic license validation with SHA-256 signatures, hardware fingerprint binding, seat enforcement, and non-destructive read-only access upon expiration.
- **Single-Use Administrator Setup:** First-run provisioning provides a single-use setup endpoint that permanently disables itself once the initial administrator account is established.
- **Role-Based Access Control (RBAC):** Granular authorization backed by 214 explicit permissions and manager override escalation.

---

## 6. Current Release Status & Verification

> [!IMPORTANT]
> **AUTHORITATIVE RELEASE MATURITY STATUS:**
> - **CBOS 1.2.2** is a **FROZEN INTERNAL ACCEPTANCE BASELINE ONLY**.
> - It is **NOT** paid-pilot ready, **NOT** commercial GA, and **NOT** production-certified.
> - Customer pilots and commercial rollouts require the hardening deliverables planned for **CBOS 1.2.3** and **CBOS 1.3.0**. Consult the [Canonical Engineering Roadmap](docs/roadmap/engineering-roadmap.md) and [Product Readiness Levels](docs/development/readiness-levels.md).

- **Current Baseline:** `1.2.2` (Internal Acceptance Baseline)
- **Next Target:** `1.2.3` (Controlled Single-Terminal Pilot Hardening)
- **Automated Verification:**
  - 1,824 automated unit/integration tests passing cleanly.
  - Debug and Release compilation clean (0 errors).
  - ReleaseGuard automated security scan: Clean (0 violations).
  - Workstation test isolation verified (100% in-memory / zero host pollution).

---

## 7. Developer & Engineering Governance

All developers and automated agents contributing to CBOS must adhere to authoritative guidelines:
1. **Authoritative Agent Instructions:** Review [AGENTS.md](AGENTS.md) and [GEMINI.md](GEMINI.md).
2. **Permanent Domain Rules:**
   - [Financial Integrity & Accounting Rules](.agents/rules/financial-integrity.md)
   - [WinForms & High-DPI UI Rules](.agents/rules/winforms-ui.md)
   - [Database & Persistence Rules](.agents/rules/database.md)
   - [Security & Licensing Rules](.agents/rules/security.md)
   - [Testing & Quality Rules](.agents/rules/testing.md)
   - [Packaging & Release Rules](.agents/rules/release.md)
3. **Definition of Done:** Comply with [Definition of Done](docs/development/definition-of-done.md).
4. **Product Decision Records:** Respect durable business policies in [PDR Registry](docs/product/pdr/README.md).
5. **Truthful Evidence Claims:** State `LIVE UI NOT EXECUTED` unless interactively operated on a physical display surface.

---

## 8. Documentation Portal

Consult the comprehensive documentation suite located in [`docs/`](docs/):

- **[Master Documentation Index](docs/README.md)** — Central documentation portal linking all architectural, operational, and support guides.
- **[Canonical Engineering Roadmap](docs/roadmap/engineering-roadmap.md)** — Multi-milestone roadmap from 1.2.2 to 1.4.0+.
- **[Known Limitations](docs/known-limitations.md)** — Factual operational boundaries (Refunds, manual card recording, etc.).
- **[System Architecture](docs/architecture/system-architecture.md)** — Clean Architecture, DDD bounded contexts, CQRS, and outbox patterns.
- **[Database Architecture](docs/database/database-architecture.md)** — Schema isolation, connection models, and EF Core migrations.
- **[POS Architecture](docs/pos/pos-architecture.md)** — Register layout, order lifecycle, payment tenders, and cash drawer management.
- **[Continuity Mode & Resilience](docs/resilience/continuity-mode.md)** — Offline sales handling, local operational cache, and journal replay.
- **[Security & Licensing](docs/security/security-architecture.md)** — Threat modeling, DPAPI protection, and RSA-2048 licensing.
- **[Deployment & Commissioning](docs/deployment/installation.md)** — Topology options, SQL Server setup, and onboarding wizard.
- **[Operations & Troubleshooting](docs/support/troubleshooting.md)** — Diagnostic dashboards, backup/restore runbooks, and incident procedures.
- **[Release Engineering](docs/release/release-process.md)** — Versioning policy, ReleaseGuard scanner, and release checklist.

---

## 9. License & Commercial Notice

Copyright &copy; 2026 Clovent Technologies. All rights reserved.
Clovent Business Operating System is proprietary enterprise software. Unauthorized reproduction, distribution, decompilation, or reverse engineering is strictly prohibited.
