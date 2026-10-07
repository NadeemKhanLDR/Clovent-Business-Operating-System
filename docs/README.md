# Clovent Business Operating System (CBOS) — Master Documentation Index

> **CBOS Enterprise Documentation Portal**
> - **Product Version:** CBOS 1.2.2
> - **Repository Branch:** `docs/cbos-documentation-foundation`
> - **Target Audience:** Architects, Developers, QA, Operations, Support, Security
> - **Status:** COMPREHENSIVE MASTER INDEX
> - **Last Synchronized:** 2026-10-07

---

## 1. Welcome to the CBOS Documentation Portal

**Clovent Business Operating System (CBOS)** is an enterprise Point-of-Sale (POS) and retail/restaurant ERP built with C# 13, .NET 10, Windows Forms, DevExpress 26.1, and Microsoft SQL Server. It follows strict Domain-Driven Design (DDD) with Clean Architecture across 6 isolated database schemas and provides offline resilience through Continuity Mode and local operational caching.

This portal is the single central entry point to all architectural, operational, development, security, and support documentation across the platform.

```
                              CBOS DOCUMENTATION SUITE
  ┌───────────────────────┬───────────────────────┬───────────────────────┐
  │     ARCHITECTURE      │      DEVELOPMENT      │       DATABASE        │
  │ • System Architecture │ • Environment Setup   │ • Database Arch       │
  │ • Bounded Contexts    │ • Build & Compile     │ • Domain Data Model   │
  │ • Application Startup │ • Coding Guidelines   │ • EF Core Migrations  │
  │ • Transaction Outbox  │ • WinForms Designer   │ • Schema Isolation    │
  ├───────────────────────┼───────────────────────┼───────────────────────┤
  │      POS / RETAIL     │      RESILIENCE       │     INTEGRATIONS      │
  │ • POS Architecture    │ • Continuity Mode     │ • Printing Architecture│
  │ • Order Lifecycle     │ • Operational Cache   │ • QuickBooks Gateway  │
  │ • Payment Architecture│ • HMAC Local Journal  │   (Simulated/Planned) │
  │ • Shift Management    │ • Replay Engine       │                       │
  ├───────────────────────┼───────────────────────┼───────────────────────┤
  │       SECURITY        │      DEPLOYMENT       │      OPERATIONS       │
  │ • Security Architecture│ • Installation Guide  │ • Health & Telemetry  │
  │ • Authentication      │ • Network Topologies  │ • Logging/Diagnostics │
  │ • Authorization/RBAC  │ • SQL Server Setup    │ • Backup & Restore    │
  │ • Offline RSA License │ • Commissioning       │ • Disaster Recovery   │
  │ • STRIDE Threat Model │ • Upgrades/Decomm     │                       │
  ├───────────────────────┼───────────────────────┼───────────────────────┤
  │    TESTING & QA       │  RELEASE ENGINEERING  │  SUPPORT & RUNBOOKS   │
  │ • Testing Strategy    │ • Release Process     │ • Troubleshooting     │
  │ • Running Tests       │ • Release Checklist   │ • 8 Dedicated Runbooks│
  │ • Clean Sandbox QA    │ • Versioning Policy   │ • Anti-Destructive Ops│
  │ • Performance Tests   │ • Artifact Policy     │                       │
  │ • Security Tests      │ • ReleaseGuard Scanner│                       │
  └───────────────────────┴───────────────────────┴───────────────────────┘
```

---

## 2. Role-Based Navigation Guides

If you are new to CBOS, start with your role's onboarding path:

- **For New Developers:**
  1. Read [System Architecture](file:///docs/architecture/system-architecture.md) and [Bounded Contexts](file:///docs/architecture/bounded-contexts.md).
  2. Follow [Environment Setup](file:///docs/development/environment-setup.md) to install .NET 10 and DevExpress 26.1.
  3. Review [Project Structure](file:///docs/development/project-structure.md) and [Coding Guidelines](file:///docs/development/coding-guidelines.md).
  4. Study [WinForms Designer Safety](file:///docs/development/winforms-designer-safety.md) and [High-DPI Standards](file:///docs/development/ui-high-dpi.md).
  5. Run tests locally via [Running Tests](file:///docs/testing/running-tests.md).

- **For Senior Architects:**
  1. Review [System Architecture](file:///docs/architecture/system-architecture.md) and [Master ADR Index](file:///docs/adr/README.md).
  2. Inspect [Database Architecture](file:///docs/database/database-architecture.md) and [Domain Data Model](file:///docs/database/domain-data-model.md).
  3. Examine [Continuity Mode](file:///docs/resilience/continuity-mode.md), [Transactional Outbox](file:///docs/architecture/transactional-outbox.md), and [Operational Cache](file:///docs/resilience/operational-cache.md).
  4. Check [STRIDE Threat Model](file:///docs/security/threat-model.md) and [Known Limitations](file:///docs/known-limitations.md).

- **For QA & Release Engineers:**
  1. Follow the [Testing Strategy](file:///docs/testing/testing-strategy.md) and [Running Tests](file:///docs/testing/running-tests.md).
  2. Execute the 10-step [Windows Sandbox Clean-Machine QA](file:///docs/testing/windows-sandbox-acceptance.md).
  3. Validate pre-distribution security via [ReleaseGuard Scanner](file:///docs/release/releaseguard.md).
  4. Execute the formal [Release Process](file:///docs/release/release-process.md) and sign off on the [Release Checklist](file:///docs/release/release-checklist.md).

- **For Operations & Sysadmins:**
  1. Review [Deployment Topologies](file:///docs/deployment/topologies.md) and [Installation Guide](file:///docs/deployment/installation.md).
  2. Follow the [SQL Server Setup Guide](file:///docs/deployment/sql-server.md) for mixed-mode auth and `cbos_app` permissions.
  3. Execute [Backup and Restore Runbooks](file:///docs/operations/backup-and-restore.md) and [Disaster Recovery](file:///docs/operations/disaster-recovery.md).
  4. Review [Upgrade Procedures](file:///docs/deployment/upgrades.md) and [Uninstall Procedures](file:///docs/deployment/uninstallation.md).

- **For Support Engineers:**
  1. Bookmark the [Master Troubleshooting Guide](file:///docs/support/troubleshooting.md).
  2. Study the operational runbooks in [`docs/support/runbooks/`](file:///docs/support/runbooks/).
  3. Understand [Continuity Mode](file:///docs/resilience/continuity-mode.md) and local journal replay before touching customer databases.
  4. Always observe [Anti-Destructive Support Rules](file:///docs/support/troubleshooting.md#anti-destructive-support-tenets).

---

## 3. Complete Documentation Inventory

### 3.1 Architecture & Core Design
| Document | Path | Audience | Status |
| :--- | :--- | :--- | :--- |
| **System Architecture** | [`docs/architecture/system-architecture.md`](file:///docs/architecture/system-architecture.md) | Architects, Devs | VALIDATED |
| **Bounded Contexts** | [`docs/architecture/bounded-contexts.md`](file:///docs/architecture/bounded-contexts.md) | Architects, Devs | VALIDATED |
| **Application Startup Lifecycle** | [`docs/architecture/application-startup.md`](file:///docs/architecture/application-startup.md) | Devs, Support | VALIDATED |
| **Transactional Outbox Engine** | [`docs/architecture/transactional-outbox.md`](file:///docs/architecture/transactional-outbox.md) | Architects, Devs | VALIDATED |

### 3.2 Architecture Decision Records (ADRs)
| Document | Path | Audience | Status |
| :--- | :--- | :--- | :--- |
| **Master ADR Index** | [`docs/adr/README.md`](file:///docs/adr/README.md) | Architects, Devs | VALIDATED |
| **ADR-0001: Single Physical DB, Multiple Schemas** | [`docs/adr/ADR-0001-single-physical-database-multiple-schemas.md`](file:///docs/adr/ADR-0001-single-physical-database-multiple-schemas.md) | Architects, DBAs | APPROVED |
| **ADR-0002: Clean Architecture & Context Isolation** | [`docs/adr/ADR-0002-clean-architecture-bounded-contexts.md`](file:///docs/adr/ADR-0002-clean-architecture-bounded-contexts.md) | Architects, Devs | APPROVED |
| **ADR-0003: Transactional Outbox Pattern** | [`docs/adr/ADR-0003-transactional-outbox.md`](file:///docs/adr/ADR-0003-transactional-outbox.md) | Architects, Devs | APPROVED |
| **ADR-0004: Offline Continuity Mode** | [`docs/adr/ADR-0004-continuity-mode.md`](file:///docs/adr/ADR-0004-continuity-mode.md) | Architects, Devs | APPROVED |
| **ADR-0005: Local Operational Cache with HMAC** | [`docs/adr/ADR-0005-local-operational-cache.md`](file:///docs/adr/ADR-0005-local-operational-cache.md) | Architects, Security | APPROVED |
| **ADR-0006: Microsoft SQL Server DB Engine** | [`docs/adr/ADR-0006-sql-server-database-platform.md`](file:///docs/adr/ADR-0006-sql-server-database-platform.md) | Architects, DBAs | APPROVED |
| **ADR-0007: WinForms & DevExpress 26.1 Client** | [`docs/adr/ADR-0007-winforms-devexpress-desktop.md`](file:///docs/adr/ADR-0007-winforms-devexpress-desktop.md) | UI Architects | APPROVED |
| **ADR-0008: Offline RSA-2048 Cryptographic Licensing**| [`docs/adr/ADR-0008-offline-rsa-license-validation.md`](file:///docs/adr/ADR-0008-offline-rsa-license-validation.md) | Security, Devs | APPROVED |
| *Legacy UI ADRs (ADR-001 through ADR-007)* | [`docs/architecture/adr/`](file:///docs/architecture/adr/) | UI Devs | APPROVED |

### 3.3 Database Architecture & Data Modeling
| Document | Path | Audience | Status |
| :--- | :--- | :--- | :--- |
| **Database Architecture** | [`docs/database/database-architecture.md`](file:///docs/database/database-architecture.md) | DBAs, Architects | VALIDATED |
| **Domain Data Model** | [`docs/database/domain-data-model.md`](file:///docs/database/domain-data-model.md) | Devs, DBAs | VALIDATED |
| **EF Core Migrations Guide** | [`docs/database/migrations.md`](file:///docs/database/migrations.md) | Devs, DBAs | VALIDATED |

### 3.4 POS & Retail Workflows
| Document | Path | Audience | Status |
| :--- | :--- | :--- | :--- |
| **Restaurant POS Architecture** | [`docs/pos/pos-architecture.md`](file:///docs/pos/pos-architecture.md) | POS Devs, QA | VALIDATED |
| **Order Lifecycle State Machine** | [`docs/pos/order-lifecycle.md`](file:///docs/pos/order-lifecycle.md) | Devs, QA | VALIDATED (Refunds Planned) |
| **Payment Architecture & Tenders**| [`docs/pos/payments.md`](file:///docs/pos/payments.md) | Devs, Finance | VALIDATED |
| **Shifts & Cash Management** | [`docs/pos/shifts-and-cash-management.md`](file:///docs/pos/shifts-and-cash-management.md) | Devs, QA | VALIDATED |

### 3.5 Resilience & Offline Operations
| Document | Path | Audience | Status |
| :--- | :--- | :--- | :--- |
| **Continuity Mode Architecture** | [`docs/resilience/continuity-mode.md`](file:///docs/resilience/continuity-mode.md) | Architects, Devs | VALIDATED |
| **Operational Cache Specification**| [`docs/resilience/operational-cache.md`](file:///docs/resilience/operational-cache.md) | Devs, Security | VALIDATED |

### 3.6 Third-Party & Peripheral Integrations
| Document | Path | Audience | Status |
| :--- | :--- | :--- | :--- |
| **Printing & Kitchen Dispatch** | [`docs/integrations/printing.md`](file:///docs/integrations/printing.md) | Devs, Hardware | VALIDATED (ESC/POS Planned) |
| **QuickBooks Gateway** | [`docs/integrations/quickbooks.md`](file:///docs/integrations/quickbooks.md) | Architects, Devs | SIMULATION / REST PLANNED |

### 3.7 Configuration & Formatting
| Document | Path | Audience | Status |
| :--- | :--- | :--- | :--- |
| **Configuration Architecture** | [`docs/configuration/configuration-architecture.md`](file:///docs/configuration/configuration-architecture.md)| Devs, Ops | VALIDATED |
| **Display Formatting Standards** | [`docs/configuration/display-settings.md`](file:///docs/configuration/display-settings.md) | UI Devs | VALIDATED |
| **Terminal Identity Resolution** | [`docs/configuration/terminal-identity.md`](file:///docs/configuration/terminal-identity.md) | Ops, Devs | VALIDATED |

### 3.8 Security, Authentication & Licensing
| Document | Path | Audience | Status |
| :--- | :--- | :--- | :--- |
| **Security Architecture** | [`docs/security/security-architecture.md`](file:///docs/security/security-architecture.md) | Security, Devs | VALIDATED |
| **Authentication Architecture** | [`docs/security/authentication.md`](file:///docs/security/authentication.md) | Devs, QA | VALIDATED |
| **Authorization & RBAC Matrix** | [`docs/security/authorization.md`](file:///docs/security/authorization.md) | Devs, Security | VALIDATED |
| **Cryptographic Licensing** | [`docs/security/licensing.md`](file:///docs/security/licensing.md) | License Admins | VALIDATED |
| **STRIDE Threat Model** | [`docs/security/threat-model.md`](file:///docs/security/threat-model.md) | Security Officers | VALIDATED |

### 3.9 Deployment & Environment Provisioning
| Document | Path | Audience | Status |
| :--- | :--- | :--- | :--- |
| **Installation & Setup Guide** | [`docs/deployment/installation.md`](file:///docs/deployment/installation.md) | Sysadmins, Ops | VALIDATED |
| **Network & Deployment Topologies**| [`docs/deployment/topologies.md`](file:///docs/deployment/topologies.md) | Architects, Ops | VALIDATED |
| **SQL Server Setup & Security** | [`docs/deployment/sql-server.md`](file:///docs/deployment/sql-server.md) | DBAs, Sysadmins | VALIDATED |
| **First-Run Commissioning** | [`docs/deployment/commissioning.md`](file:///docs/deployment/commissioning.md) | Ops, Support | VALIDATED |
| **Upgrade Procedures & Compatibility**| [`docs/deployment/upgrades.md`](file:///docs/deployment/upgrades.md) | Ops, Devs | VALIDATED |
| **Uninstallation & Decommissioning**| [`docs/deployment/uninstallation.md`](file:///docs/deployment/uninstallation.md) | Sysadmins | VALIDATED |

### 3.10 Operations, Health & Diagnostics
| Document | Path | Audience | Status |
| :--- | :--- | :--- | :--- |
| **Operations Health & Telemetry** | [`docs/operations/operations-health.md`](file:///docs/operations/operations-health.md) | Ops, Support | VALIDATED |
| **Logging & Diagnostics** | [`docs/operations/logging-and-diagnostics.md`](file:///docs/operations/logging-and-diagnostics.md) | Support, Devs | VALIDATED |
| **Database Backup & Restore** | [`docs/operations/backup-and-restore.md`](file:///docs/operations/backup-and-restore.md) | DBAs, Ops | VALIDATED |
| **Disaster Recovery Runbook** | [`docs/operations/disaster-recovery.md`](file:///docs/operations/disaster-recovery.md) | Ops, Sysadmins | VALIDATED |

### 3.11 Development Standards
| Document | Path | Audience | Status |
| :--- | :--- | :--- | :--- |
| **Environment Setup Guide** | [`docs/development/environment-setup.md`](file:///docs/development/environment-setup.md) | Developers | VALIDATED |
| **Build & Compilation Commands** | [`docs/development/build.md`](file:///docs/development/build.md) | Developers, CI/CD | VALIDATED |
| **Project & Layer Structure** | [`docs/development/project-structure.md`](file:///docs/development/project-structure.md) | Developers | VALIDATED |
| **Clean Architecture Coding Rules**| [`docs/development/coding-guidelines.md`](file:///docs/development/coding-guidelines.md) | Developers | VALIDATED |
| **WinForms Designer Safety Rules** | [`docs/development/winforms-designer-safety.md`](file:///docs/development/winforms-designer-safety.md)| UI Devs | VALIDATED |
| **High-DPI PerMonitorV2 Standards**| [`docs/development/ui-high-dpi.md`](file:///docs/development/ui-high-dpi.md) | UI Devs | VALIDATED |

### 3.12 Testing & Quality Assurance
| Document | Path | Audience | Status |
| :--- | :--- | :--- | :--- |
| **Testing Strategy & Pyramid** | [`docs/testing/testing-strategy.md`](file:///docs/testing/testing-strategy.md) | QA Leads, Devs | VALIDATED |
| **Running Automated Tests** | [`docs/testing/running-tests.md`](file:///docs/testing/running-tests.md) | QA, Devs | VALIDATED |
| **Clean Windows Sandbox Acceptance**| [`docs/testing/windows-sandbox-acceptance.md`](file:///docs/testing/windows-sandbox-acceptance.md)| QA Engineers | VALIDATED |
| **Performance Budgets & Benchmarks**| [`docs/testing/performance.md`](file:///docs/testing/performance.md) | QA, Architects | VALIDATED |
| **Security Testing Protocols** | [`docs/testing/security-testing.md`](file:///docs/testing/security-testing.md) | QA, Security | VALIDATED |

### 3.13 Release Engineering
| Document | Path | Audience | Status |
| :--- | :--- | :--- | :--- |
| **Canonical Release Process** | [`docs/release/release-process.md`](file:///docs/release/release-process.md) | Release Engineers| VALIDATED |
| **Formal Release Checklist** | [`docs/release/release-checklist.md`](file:///docs/release/release-checklist.md) | QA, Product Owner| VALIDATED |
| **Semantic Versioning Policy** | [`docs/release/versioning.md`](file:///docs/release/versioning.md) | Architects, Devs | VALIDATED |
| **Artifact & Git Exclusion Policy**| [`docs/release/artifact-policy.md`](file:///docs/release/artifact-policy.md) | All Engineers | VALIDATED |
| **ReleaseGuard Security Scanner** | [`docs/release/releaseguard.md`](file:///docs/release/releaseguard.md) | DevSecOps, QA | VALIDATED |

### 3.14 Support & Incident Runbooks
| Document | Path | Audience | Status |
| :--- | :--- | :--- | :--- |
| **Master Troubleshooting Matrix** | [`docs/support/troubleshooting.md`](file:///docs/support/troubleshooting.md) | Support, Ops | VALIDATED |
| **Runbook: Database Unavailable** | [`docs/support/runbooks/database-unavailable.md`](file:///docs/support/runbooks/database-unavailable.md)| Support | VALIDATED |
| **Runbook: Continuity Mode Incident**| [`docs/support/runbooks/continuity-mode.md`](file:///docs/support/runbooks/continuity-mode.md)| Support | VALIDATED |
| **Runbook: Transaction Outbox Stalled**| [`docs/support/runbooks/outbox-stalled.md`](file:///docs/support/runbooks/outbox-stalled.md)| Support, Devs | VALIDATED |
| **Runbook: Printer / Kitchen Failure**| [`docs/support/runbooks/printer-failure.md`](file:///docs/support/runbooks/printer-failure.md)| Support | VALIDATED |
| **Runbook: License / Tamper Issue** | [`docs/support/runbooks/license-issue.md`](file:///docs/support/runbooks/license-issue.md) | Support, License | VALIDATED |
| **Runbook: Schema Compatibility Mismatch**| [`docs/support/runbooks/schema-mismatch.md`](file:///docs/support/runbooks/schema-mismatch.md)| Support, DBAs | VALIDATED |
| **Runbook: Installer Execution Failure**| [`docs/support/runbooks/installer-failure.md`](file:///docs/support/runbooks/installer-failure.md)| Support, Ops | VALIDATED |
| **Runbook: SQL Express Setup Stalled**| [`docs/support/runbooks/sql-installation-slow.md`](file:///docs/support/runbooks/sql-installation-slow.md)| Support, Ops | VALIDATED |

### 3.15 Enterprise Governance, Roadmap & Audit
| Document | Path | Audience | Status |
| :--- | :--- | :--- | :--- |
| **Documentation Coverage Matrix** | [`docs/documentation-coverage.md`](file:///docs/documentation-coverage.md) | Management, QA | VALIDATED |
| **Verified Documentation Gaps** | [`docs/documentation-gaps.md`](file:///docs/documentation-gaps.md) | Architects, Devs | VALIDATED |
| **Documentation Style Guide** | [`docs/documentation-style-guide.md`](file:///docs/documentation-style-guide.md)| Tech Writers | VALIDATED |
| **Enterprise Glossary** | [`docs/glossary.md`](file:///docs/glossary.md) | All Readers | VALIDATED |
| **Verified Known Limitations** | [`docs/known-limitations.md`](file:///docs/known-limitations.md) | All Readers | VALIDATED |
| **Engineering Roadmap** | [`docs/roadmap/engineering-roadmap.md`](file:///docs/roadmap/engineering-roadmap.md)| Architects, PMs | ROADMAP SYNC PENDING |

---

## 4. Documentation Governance & Authoring Standards

To maintain the high standard of documentation across future CBOS updates:
1. **Source of Truth Precedence:**
   - C# 13 Source Code > Automated Tests > Verified Runtime Telemetry > ADRs > Platform Documentation.
2. **Strict Claims Standard:**
   - State `LIVE UI NOT EXECUTED` unless the application was interactively operated on a physical/virtual display.
   - State `VISUAL STUDIO DESIGNER UI NOT EXECUTED` unless forms were opened inside Visual Studio Designer.
   - Never claim PCI certification.
   - Accurately label unbuilt or partially built capabilities (e.g., Refund workflow as `REFUND DOMAIN NOT YET IMPLEMENTED`).
3. **Zero Leaked Secrets:**
   - Never commit private keys, development passwords, machine tokens, or customer databases.
4. **Change Management:**
   - All documentation updates must follow the metadata format defined in [`docs/documentation-style-guide.md`](file:///docs/documentation-style-guide.md).
