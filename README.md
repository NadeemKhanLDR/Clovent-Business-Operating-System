# Clovent Business Operating System (CBOS)

[![Build & Verification Status](https://img.shields.io/badge/Verification-1%2C824%20Passed%20%7C%200%20Failed-success)](#current-verification)
[![Release Version](https://img.shields.io/badge/Version-1.2.2-blue)](#current-version)
[![Engineering Status](https://img.shields.io/badge/Status-Ready%20for%20Sandbox%20Retest-orange)](#current-version)

Clovent Business Operating System (CBOS) is a modular, high-reliability enterprise business operating system built for retail and hospitality environments, currently specialized for **Restaurant POS**, **Back Office**, **Inventory Control**, **Catalog & Master Data Management**, **Shift & Cash Drawer Operations**, **Financial Reporting**, and **Enterprise Deployment / Licensing Infrastructure**.

---

## Product Overview

CBOS delivers an integrated operating environment designed for mission-critical point-of-sale workstations and management back offices:
- **Restaurant POS:** Fast-paced order management supporting Dine-In, Take Away, and Delivery workflows, multi-tender settlements, and customer accounts.
- **Back Office & Administration:** Centralized master data upkeep, dining area mapping, table configuration, printer routing, and user management.
- **Inventory & Stock Tracking:** Granular stock movements across receipt, issue, transfer, adjustment, reserve, and release workflows.
- **Catalog & Pricing Engine:** Hierarchical product categories, variants, barcodes, price tiers, and tax profiles.
- **Manager Operations & Shift Controls:** Real-time shift tracking, cash-in/cash-out drawer adjustments, and end-of-day reconciliation.
- **Financial & Margin Reporting:** Shift summaries, customer accounts receivable (A/R), known gross profit margin tracking, and tax audit reports.
- **Licensing & Commissioning Infrastructure:** First-run deployment wizard, encrypted database provisioning, and asymmetric cryptographic licensing.

---

## Technology Stack

CBOS is engineered on modern .NET technology:
- **Language:** C# 13
- **Runtime & Target Frameworks:** .NET 10 (`net10.0-windows` for Desktop shell and UI tests; `net10.0` for Domain, Application, Infrastructure, and Tools)
- **Desktop UI Framework:** Windows Forms with **DevExpress 26.1** (`DevExpress.Win`, `DevExpress.Reporting.Core`, `DevExpress.Images`)
- **Database Engine:** Microsoft SQL Server
- **Object-Relational Mapping (ORM):** Entity Framework Core 10 (`Microsoft.EntityFrameworkCore.SqlServer`, `Microsoft.EntityFrameworkCore.Design`)
- **CQRS & Mediator Pattern:** MediatR
- **Hosting & Dependency Injection:** `Microsoft.Extensions.Hosting` and `Microsoft.Extensions.DependencyInjection`
- **Testing Frameworks:** xUnit 2.9, `Microsoft.NET.Test.Sdk`, SQLite in-memory provider for isolated unit and integration testing

---

## Architecture & Bounded Contexts

CBOS implements strict Domain-Driven Design (DDD) with Clean Architecture principles across distinct bounded contexts:

```
src/
├── Clovent.Domain/              # Shared domain primitives and entities
├── Clovent.Platform/            # Cross-cutting platform abstractions and environment configuration
├── Clovent.Authentication/      # Login attempts, session tokens, audit events
├── Clovent.Identity/            # Users, roles, granular permissions, organization hierarchy
├── Clovent.MasterData/          # Warehouses, terminals, currencies, units of measure
├── Clovent.Catalog/             # Categories, brands, products, variants, price tiers
├── Clovent.Inventory/           # Stocks, inventory movements, adjustments, reservations
├── Clovent.Restaurant/          # Dining areas, tables, orders, kitchen tickets, settlements, shifts
└── Clovent.Desktop/             # WinForms presentation layer, DevExpress ribbon, view controls
```

### Database Persistence Model
- **Single Physical Database:** Exactly one SQL Server database: `Clovent_BusinessOperatingSystem`.
- **Schema Isolation:** Dedicated schema per bounded context (`[Authentication]`, `[Identity]`, `[MasterData]`, `[Catalog]`, `[Inventory]`, `[Restaurant]`).
- **Independent Migration Histories:** Each bounded context manages schema versions via its own migration history table (e.g. `[Restaurant].[__EFMigrationsHistory]`).
- **Runtime Least Privilege:** Application services connect with non-administrative accounts granted only `db_datareader`, `db_datawriter`, and `EXECUTE`. Runtime credentials are encrypted using Windows DPAPI in `database.config.json`.

---

## Key POS Capabilities

- **Order Workflows:** Frictionless Dine-In (table management, merge/split bills), Take Away, and Delivery dispatch.
- **Payment Processing:** Cash, Credit/Debit Card, Split Tender, and Customer On-Account sales with automated customer advance credit handling.
- **Quick Orders:** Template-driven rapid order entry for high-volume quick-service setups.
- **Smart Combo & Recommendation Engine:** Rule-based combo builder, intelligent up-selling, and item suggestions.
- **Rush Mode:** Streamlined UI layout optimized for peak service hours.
- **Cart Crash / Session Recovery:** Local session persistence protecting pending orders against unexpected application closure or power loss.
- **Customer Receivables:** Dedicated customer ledger, balance tracking, statement generation, and payment allocations.

---

## Always-On Resilience Architecture

CBOS includes an enterprise resilience pipeline designed to prevent operational stoppages during network and hardware disturbances:
- **Transactional Outbox:** Guaranteed eventual consistency for integration events, kitchen orders, and audit messages.
- **Circuit Breakers & Dependency Isolation:** External hardware faults (printers, customer displays) and network delays cannot lock the UI or drop active transactions.
- **Payment Idempotency:** Cryptographic idempotency keys ensure transactions are never recorded or billed more than once.
- **Emergency Continuity Mode:** In the event of primary database unavailability, POS stations seamlessly transition to emergency operations.
- **Protected Emergency Cash Journal:** Emergency sales are encrypted using Windows DPAPI and stored in a local append-only journal.
- **Exactly-Once Replay Safeguards:** Once database connectivity is restored, the journal replayer validates signatures and posts emergency sales without data corruption or duplicate transactions.
- **Operations Health Center:** Diagnostic dashboard displaying connection health, outbox queue depth, printer statuses, and cache freshness.

---

## Local Operational Cache (v1.2.0+)

To support safe selling when SQL Server is temporarily unavailable, CBOS features a local operational cache:
- **Protected Local Store:** Stores menu hierarchy, products, active pricing, tax rules, and dining tables using Windows DPAPI (`ProtectedOperationalCacheStore`).
- **Cryptographic Integrity:** Validated using HMAC-SHA256 signatures to prevent out-of-band tampering.
- **Freshness Policy:** Strict time-to-live (TTL) validation ensures outdated prices or archived menus are not served during extended outages.
- **Restricted Offline Policy:** To protect financial and inventory integrity, continuity mode allows **Cash-only** sales for validated catalog items. Credit-account sales, refund voids, and administrative configurations remain strictly disabled until SQL connectivity returns.
- **Sale-Time Price Snapshot:** Pricing and tax calculations are frozen into immutable transaction snapshots.
- **Collision-Safe Identifiers:** Local receipt identifiers (`LocalReceiptNumber`) guarantee zero primary-key conflicts when transactions replay into the central database.

---

## High-DPI Desktop Support

CBOS is designed for modern desktop displays, with high-DPI scaling across varied resolutions:
- **PerMonitorV2 Architecture:** Windows Forms `PerMonitorV2` High-DPI mode with native DevExpress vector rendering.
- **Centralized Desktop Typography:** Unified typographic scales (`DesktopStyle.cs`) governing font sizes, button dimensions, filter toolbars, search boxes, and grid row heights.
- **Layout Robustness:** Flow and table layouts eliminate control clipping and truncation across standard baseline display scales.
- *Note:* While tested against target high-DPI baseline configurations, physical display and multi-monitor setups vary by hardware.

---

## Licensing & First-Run Commissioning

- **First-Run Commissioning Wizard:** Step-by-step setup verifying SQL connectivity, applying schema migrations, configuring branch and terminal identities, and onboarding the initial administrator.
- **Single-Use Admin Provisioning:** The initial administrative setup endpoint automatically deactivates as soon as an administrator exists.
- **Cryptographic Licensing Engine:** Dual-mode licensing (Trial and Commercial) validated via asymmetric RSA-2048 with SHA-256 (`clovent-2026-v2`).
- **Hardware-Aware Verification:** License files validate hardware fingerprints, licensed workstation seats, and feature flags.
- **Tamper Protection:** Monitored via DPAPI-protected state files (`license_guard.dat` and `trial.state`) with anti-rollback clock verification.
- **Non-Destructive Expiration:** Expired licenses preserve full read-only access to historical sales, financial reports, and backups while blocking new commercial sales. Customer records are never locked or destroyed.

---

## Deployment & Distribution

CBOS ships as an enterprise Windows deployment package:
- **Self-Contained Client:** Win-x64 standalone package requiring zero pre-installed .NET SDKs or development dependencies on target workstations.
- **Single-File Database Provisioner:** Dedicated provisioning executable (`Clovent.Installer.Provisioner.exe`) performing silent database initialization, schema verification, and permission enforcement.
- **Inno Setup Production Installer:** Native 64-bit elevated setup wizard (`Clovent.BusinessOperatingSystem-1.2.2-Setup.exe`) chaining prerequisite checks, file deployment, and database connectivity.
- **Zero Shipped Credentials:** Production releases exclude default credentials, development configs (`appsettings.Development.json`), debug symbols (`*.pdb`), and active license files.

---

## Current Version & Acceptance Status

- **Current Version:** `1.2.2`
- **Engineering Status:** `READY FOR WINDOWS SANDBOX RETEST`
- **Acceptance Note:** Production pilot approval is pending successful clean-client Windows Sandbox acceptance testing. Release 1.2.2 is an engineering release candidate.

---

## Current Verification Evidence

Release 1.2.2 has passed rigorous automated verification:

| Verification Suite | Result | Details |
| :--- | :---: | :--- |
| **Solution Test Suite** | **PASS** | **1,824 passed**, 0 failed, 7 skipped (1,831 total tests) |
| **Workstation Startup Tests** | **PASS** | 7 passed, 0 failed (`WorkstationStartupVerificationTests`) |
| **Debug Solution Build** | **PASS** | 0 warnings, 0 errors (`dotnet build -c Debug`) |
| **Release Solution Build** | **PASS** | 0 warnings, 0 errors (`dotnet build -c Release`) |
| **Automated ReleaseGuard** | **PASS** | 0 security violations detected (`ScanReleasePackage.ps1`) |
| **Workstation Test Isolation** | **PASS** | 37 monitored files: 0 created, 0 deleted, 0 modified (100% hash parity) |

---

## Known Release Items

- **Authenticode Code Signing:** `PENDING` (Production certificate signing occurs during secure release distribution).
- **Clean Client Machine Acceptance:** `PENDING` (Formal Windows Sandbox validation on a bare workstation image).

---

## Documentation Links

For detailed architectural specifications and operational guides, consult [`docs/`](docs/):
- **[CBOS 1.2.2 Release Report](docs/cbos_1.2.2_release_report.md)** — Comprehensive technical release evidence and manifest comparisons.
- **[Documentation Index](docs/README.md)** — Architectural index, design decision records (ADRs), and testing guides.
- **[First-Run Commissioning Guide](docs/FirstRunCommissioning.md)** — Step-by-step workstation setup and terminal initialization.
- **[Software Licensing Architecture](docs/SoftwareLicensing.md)** — Asymmetric RSA licensing, hardware binding, and trial lifecycle.
- **[Database Configuration](docs/DatabaseConfiguration.md)** — SQL Server schemas, connection settings, and DPAPI credential protection.
- **[Installer Deployment Guide](docs/InstallerDeployment.md)** — Enterprise deployment, silent installation, and provisioner options.
