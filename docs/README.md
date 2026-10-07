# Clovent Business Operating System (CBOS) — Master Documentation Index

> **CBOS Enterprise Documentation Portal**
> - **Product Version:** CBOS 1.2.2 — Frozen Internal Acceptance Baseline
> - **Target Audience:** Architects, Developers, QA, Operations, Support, Security
> - **Status:** COMPREHENSIVE MASTER INDEX
> - **Last Synchronized:** 2026-10-07

---

## 1. Welcome to the CBOS Documentation Portal

**Clovent Business Operating System (CBOS)** is an enterprise Point-of-Sale (POS) and retail/hospitality operating platform engineered for high-reliability workstation deployments. Built with C# 13, .NET 10, Windows Forms, DevExpress 26.1, and Microsoft SQL Server, CBOS follows strict Domain-Driven Design (DDD) with Clean Architecture across 6 isolated database schemas and provides offline resilience through Continuity Mode and local operational caching.

This portal is the single central entry point to all architectural, operational, development, security, and governance documentation across the platform.

```
                              CBOS DOCUMENTATION SUITE
  ┌───────────────────────┬───────────────────────┬───────────────────────┐
  │     ARCHITECTURE      │      DEVELOPMENT      │       DATABASE        │
  │ • System Architecture │ • Environment Setup   │ • Database Arch       │
  │ • Bounded Contexts    │ • Build & Compile     │ • Domain Data Model   │
  │ • Application Startup │ • Coding Guidelines   │ • EF Core Migrations  │
  │ • Transaction Outbox  │ • WinForms Designer   │ • Schema Isolation    │
  │                       │ • Definition of Done  │                       │
  ├───────────────────────┼───────────────────────┼───────────────────────┤
  │      POS / RETAIL     │      RESILIENCE       │     INTEGRATIONS      │
  │ • POS Architecture    │ • Continuity Mode     │ • Printing Arch       │
  │ • Order Lifecycle     │ • Operational Cache   │ • QuickBooks Gateway  │
  │ • Payment Architecture│ • HMAC Local Journal  │   (Simulated/Planned) │
  │ • Shift Management    │ • Replay Engine       │                       │
  ├───────────────────────┼───────────────────────┼───────────────────────┤
  │       SECURITY        │      DEPLOYMENT       │      OPERATIONS       │
  │ • Security Arch       │ • Installation Guide  │ • Health & Telemetry  │
  │ • Authentication      │ • Network Topologies  │ • Logging/Diagnostics │
  │ • Authorization/RBAC  │ • SQL Server Setup    │ • Backup & Restore    │
  │ • Offline RSA License │ • Commissioning       │ • Disaster Recovery   │
  │ • STRIDE Threat Model │ • Upgrades/Decomm     │                       │
  ├───────────────────────┼───────────────────────┼───────────────────────┤
  │    TESTING & QA       │  RELEASE ENGINEERING  │  GOVERNANCE & PDRs    │
  │ • Testing Strategy    │ • Release Process     │ • Engineering Roadmap │
  │ • Running Tests       │ • Release Checklist   │ • Readiness Levels    │
  │ • Clean Sandbox QA    │ • Versioning Policy   │ • PDR Registry        │
  │ • Evidence Standards  │ • Artifact Policy     │ • Known Limitations   │
  │ • Security Tests      │ • ReleaseGuard Scanner│ • Troubleshooting     │
  └───────────────────────┴───────────────────────┴───────────────────────┘
```

---

## 2. Role-Based Navigation Guides

If you are new to CBOS, start with your role's onboarding path:

- **For New Developers:**
  1. Read [System Architecture](architecture/system-architecture.md) and [Bounded Contexts](architecture/bounded-contexts.md).
  2. Follow [Environment Setup](development/environment-setup.md) to install .NET 10 and DevExpress 26.1.
  3. Review [Project Structure](development/project-structure.md), [Coding Guidelines](development/coding-guidelines.md), and [Definition of Done](development/definition-of-done.md).
  4. Study [WinForms Designer Safety](development/winforms-designer-safety.md) and [High-DPI Standards](development/ui-high-dpi.md).
  5. Run tests locally via [Running Tests](testing/running-tests.md).

- **For Senior Architects & Governance Leads:**
  1. Review [System Architecture](architecture/system-architecture.md) and [Master ADR Index](adr/README.md).
  2. Inspect [Product Decision Records (PDRs)](product/pdr/README.md) and [Product Readiness Levels](development/readiness-levels.md).
  3. Review [Canonical Engineering Roadmap](roadmap/engineering-roadmap.md) and [Known Limitations](known-limitations.md).
  4. Inspect [Database Architecture](database/database-architecture.md) and [Domain Data Model](database/domain-data-model.md).
  5. Examine [Continuity Mode](resilience/continuity-mode.md), [Transactional Outbox](architecture/transactional-outbox.md), and [Operational Cache](resilience/operational-cache.md).

- **For QA & Release Engineers:**
  1. Follow the [Testing Strategy](testing/testing-strategy.md), [Running Tests](testing/running-tests.md), and [Evidence Standards](development/readiness-levels.md#3-truthful-runtime-evidence-standards).
  2. Execute the 10-step [Windows Sandbox Clean-Machine QA](testing/windows-sandbox-acceptance.md).
  3. Validate pre-distribution security via [ReleaseGuard Scanner](release/releaseguard.md).
  4. Execute the formal [Release Process](release/release-process.md) and sign off on the [Release Checklist](release/release-checklist.md).

- **For Operations & Sysadmins:**
  1. Review [Deployment Topologies](deployment/topologies.md) and [Installation Guide](deployment/installation.md).
  2. Follow the [SQL Server Setup Guide](deployment/sql-server.md) for mixed-mode auth and `cbos_app` permissions.
  3. Execute [Backup and Restore Runbooks](operations/backup-and-restore.md) and [Disaster Recovery](operations/disaster-recovery.md).
  4. Review [Upgrade Procedures](deployment/upgrades.md) and [Uninstall Procedures](deployment/uninstallation.md).

- **For Support Engineers:**
  1. Bookmark the [Master Troubleshooting Guide](support/troubleshooting.md).
  2. Study the operational runbooks in [`docs/support/runbooks/`](support/runbooks/).
  3. Understand [Continuity Mode](resilience/continuity-mode.md) and local journal replay before touching customer databases.
  4. Always observe [Anti-Destructive Support Rules](support/troubleshooting.md#anti-destructive-support-tenets).

---

## 3. Complete Documentation Inventory

### 3.1 Architecture & Core Design
| Document | Path | Audience | Status |
| :--- | :--- | :--- | :--- |
| **System Architecture** | [`docs/architecture/system-architecture.md`](architecture/system-architecture.md) | Architects, Devs | VALIDATED |
| **Bounded Contexts** | [`docs/architecture/bounded-contexts.md`](architecture/bounded-contexts.md) | Architects, Devs | VALIDATED |
| **Application Startup Lifecycle** | [`docs/architecture/application-startup.md`](architecture/application-startup.md) | Devs, Support | VALIDATED |
| **Transactional Outbox Engine** | [`docs/architecture/transactional-outbox.md`](architecture/transactional-outbox.md) | Architects, Devs | VALIDATED |

### 3.2 Architecture Decision Records (ADRs)
| Document | Path | Audience | Status |
| :--- | :--- | :--- | :--- |
| **Master ADR Index** | [`docs/adr/README.md`](adr/README.md) | Architects, Devs | VALIDATED |
| **ADR-0001: Single Physical DB, Multiple Schemas** | [`docs/adr/ADR-0001-single-physical-database-multiple-schemas.md`](adr/ADR-0001-single-physical-database-multiple-schemas.md) | Architects, DBAs | ACCEPTED |
| **ADR-0002: Clean Architecture & Context Isolation** | [`docs/adr/ADR-0002-clean-architecture-bounded-contexts.md`](adr/ADR-0002-clean-architecture-bounded-contexts.md) | Architects, Devs | ACCEPTED |
| **ADR-0003: Transactional Outbox Pattern** | [`docs/adr/ADR-0003-transactional-outbox.md`](adr/ADR-0003-transactional-outbox.md) | Architects, Devs | ACCEPTED |
| **ADR-0004: Offline Continuity Mode** | [`docs/adr/ADR-0004-continuity-mode.md`](adr/ADR-0004-continuity-mode.md) | Architects, Devs | ACCEPTED |
| **ADR-0005: Local Operational Cache with HMAC** | [`docs/adr/ADR-0005-local-operational-cache.md`](adr/ADR-0005-local-operational-cache.md) | Architects, Security | ACCEPTED |
| **ADR-0006: Microsoft SQL Server DB Engine** | [`docs/adr/ADR-0006-sql-server-database-platform.md`](adr/ADR-0006-sql-server-database-platform.md) | Architects, DBAs | ACCEPTED |
| **ADR-0007: WinForms & DevExpress 26.1 Client** | [`docs/adr/ADR-0007-winforms-devexpress-desktop.md`](adr/ADR-0007-winforms-devexpress-desktop.md) | UI Architects | ACCEPTED |
| **ADR-0008: Offline RSA-2048 Cryptographic Licensing**| [`docs/adr/ADR-0008-offline-rsa-license-validation.md`](adr/ADR-0008-offline-rsa-license-validation.md) | Security, Devs | ACCEPTED |
| *Legacy UI ADRs (ADR-001 through ADR-007)* | [`docs/architecture/adr/`](architecture/adr/) | UI Devs | ACCEPTED |

### 3.3 Product Decision Records (PDRs)
| Document | Path | Audience | Status |
| :--- | :--- | :--- | :--- |
| **PDR Registry** | [`docs/product/pdr/README.md`](product/pdr/README.md) | Product, Leadership | ACCEPTED |
| **PDR-0001: Continuity Mode Cash-Only Policy** | [`docs/product/pdr/PDR-0001-continuity-mode-cash-only-policy.md`](product/pdr/PDR-0001-continuity-mode-cash-only-policy.md) | Product, POS | ACCEPTED |
| **PDR-0002: Customer Data Ownership & Licensing** | [`docs/product/pdr/PDR-0002-customer-data-ownership-non-destructive-licensing.md`](product/pdr/PDR-0002-customer-data-ownership-non-destructive-licensing.md) | Product, Legal | ACCEPTED |
| **PDR-0003: Completed Financial Immutability** | [`docs/product/pdr/PDR-0003-completed-financial-transaction-immutability.md`](product/pdr/PDR-0003-completed-financial-transaction-immutability.md) | Product, Finance | ACCEPTED |
| **PDR-0004: Transactional Currency Precision** | [`docs/product/pdr/PDR-0004-transactional-currency-precision.md`](product/pdr/PDR-0004-transactional-currency-precision.md) | Product, MasterData | ACCEPTED |

### 3.4 Database Architecture & Data Modeling
| Document | Path | Audience | Status |
| :--- | :--- | :--- | :--- |
| **Database Architecture** | [`docs/database/database-architecture.md`](database/database-architecture.md) | DBAs, Architects | VALIDATED |
| **Domain Data Model** | [`docs/database/domain-data-model.md`](database/domain-data-model.md) | Devs, DBAs | VALIDATED |
| **EF Core Migrations Guide** | [`docs/database/migrations.md`](database/migrations.md) | Devs, DBAs | VALIDATED |

### 3.5 POS & Retail Workflows
| Document | Path | Audience | Status |
| :--- | :--- | :--- | :--- |
| **Restaurant POS Architecture** | [`docs/pos/pos-architecture.md`](pos/pos-architecture.md) | POS Devs, QA | VALIDATED |
| **Order Lifecycle State Machine** | [`docs/pos/order-lifecycle.md`](pos/order-lifecycle.md) | Devs, QA | VALIDATED (Refunds Planned 1.3.0) |
| **Payment Architecture & Tenders**| [`docs/pos/payments.md`](pos/payments.md) | Devs, Finance | VALIDATED (Manual Card Only) |
| **Shifts & Cash Management** | [`docs/pos/shifts-and-cash-management.md`](pos/shifts-and-cash-management.md) | Devs, QA | VALIDATED |

### 3.6 Resilience & Offline Operations
| Document | Path | Audience | Status |
| :--- | :--- | :--- | :--- |
| **Continuity Mode Architecture** | [`docs/resilience/continuity-mode.md`](resilience/continuity-mode.md) | Architects, Devs | VALIDATED |
| **Operational Cache Specification**| [`docs/resilience/operational-cache.md`](resilience/operational-cache.md) | Devs, Security | VALIDATED |

### 3.7 Third-Party & Peripheral Integrations
| Document | Path | Audience | Status |
| :--- | :--- | :--- | :--- |
| **Printing & Kitchen Dispatch** | [`docs/integrations/printing.md`](integrations/printing.md) | Devs, Hardware | VALIDATED (GDI Spooler; ESC/POS Planned) |
| **QuickBooks Gateway** | [`docs/integrations/quickbooks.md`](integrations/quickbooks.md) | Architects, Devs | ARCHITECTURE IMPLEMENTED / SIMULATED GATEWAY |

### 3.8 Configuration & Formatting
| Document | Path | Audience | Status |
| :--- | :--- | :--- | :--- |
| **Configuration Architecture** | [`docs/configuration/configuration-architecture.md`](configuration/configuration-architecture.md)| Devs, Ops | VALIDATED |
| **Display Formatting Standards** | [`docs/configuration/display-settings.md`](configuration/display-settings.md) | UI Devs | VALIDATED |
| **Terminal Identity Resolution** | [`docs/configuration/terminal-identity.md`](configuration/terminal-identity.md) | Ops, Devs | VALIDATED |

### 3.9 Security, Authentication & Licensing
| Document | Path | Audience | Status |
| :--- | :--- | :--- | :--- |
| **Security Architecture** | [`docs/security/security-architecture.md`](security/security-architecture.md) | Security, Devs | VALIDATED |
| **Authentication Architecture** | [`docs/security/authentication.md`](security/authentication.md) | Devs, QA | VALIDATED (PIN Gap Documented) |
| **Authorization & RBAC Matrix** | [`docs/security/authorization.md`](security/authorization.md) | Devs, Security | VALIDATED (Pipeline Auth Partial) |
| **Cryptographic Licensing** | [`docs/security/licensing.md`](security/licensing.md) | License Admins | VALIDATED |
| **STRIDE Threat Model** | [`docs/security/threat-model.md`](security/threat-model.md) | Security Officers | VALIDATED |

### 3.10 Deployment & Environment Provisioning
| Document | Path | Audience | Status |
| :--- | :--- | :--- | :--- |
| **Installation & Setup Guide** | [`docs/deployment/installation.md`](deployment/installation.md) | Sysadmins, Ops | VALIDATED |
| **Network & Deployment Topologies**| [`docs/deployment/topologies.md`](deployment/topologies.md) | Architects, Ops | VALIDATED (Single-Terminal 1.2.x Scope) |
| **SQL Server Setup & Security** | [`docs/deployment/sql-server.md`](deployment/sql-server.md) | DBAs, Sysadmins | VALIDATED |
| **First-Run Commissioning** | [`docs/deployment/commissioning.md`](deployment/commissioning.md) | Ops, Support | VALIDATED |
| **Upgrade Procedures & Compatibility**| [`docs/deployment/upgrades.md`](deployment/upgrades.md) | Ops, Devs | VALIDATED |
| **Uninstallation & Decommissioning**| [`docs/deployment/uninstallation.md`](deployment/uninstallation.md) | Sysadmins | VALIDATED |

### 3.11 Operations, Health & Diagnostics
| Document | Path | Audience | Status |
| :--- | :--- | :--- | :--- |
| **Operations Health & Telemetry** | [`docs/operations/operations-health.md`](operations/operations-health.md) | Ops, Support | VALIDATED |
| **Logging & Diagnostics** | [`docs/operations/logging-and-diagnostics.md`](operations/logging-and-diagnostics.md) | Support, Devs | VALIDATED (Text File; JSON Planned) |
| **Database Backup & Restore** | [`docs/operations/backup-and-restore.md`](operations/backup-and-restore.md) | DBAs, Ops | VALIDATED (Pre-Upgrade Only; Maint Planned) |
| **Disaster Recovery Runbook** | [`docs/operations/disaster-recovery.md`](operations/disaster-recovery.md) | Ops, Sysadmins | VALIDATED |

### 3.12 Development Standards & Governance
| Document | Path | Audience | Status |
| :--- | :--- | :--- | :--- |
| **Definition of Done (DoD)** | [`docs/development/definition-of-done.md`](development/definition-of-done.md) | All Engineers | PERMANENT STANDARD |
| **Product Readiness Levels** | [`docs/development/readiness-levels.md`](development/readiness-levels.md) | Leadership, QA | PERMANENT STANDARD |
| **Environment Setup Guide** | [`docs/development/environment-setup.md`](development/environment-setup.md) | Developers | VALIDATED |
| **Build & Compilation Commands** | [`docs/development/build.md`](development/build.md) | Developers, CI/CD | VALIDATED |
| **Project & Layer Structure** | [`docs/development/project-structure.md`](development/project-structure.md) | Developers | VALIDATED |
| **Clean Architecture Coding Rules**| [`docs/development/coding-guidelines.md`](development/coding-guidelines.md) | Developers | VALIDATED |
| **WinForms Designer Safety Rules** | [`docs/development/winforms-designer-safety.md`](development/winforms-designer-safety.md)| UI Devs | VALIDATED |
| **High-DPI PerMonitorV2 Standards**| [`docs/development/ui-high-dpi.md`](development/ui-high-dpi.md) | UI Devs | VALIDATED |

### 3.13 Testing & Quality Assurance
| Document | Path | Audience | Status |
| :--- | :--- | :--- | :--- |
| **Testing Strategy & Pyramid** | [`docs/testing/testing-strategy.md`](testing/testing-strategy.md) | QA Leads, Devs | VALIDATED |
| **Running Automated Tests** | [`docs/testing/running-tests.md`](testing/running-tests.md) | QA, Devs | VALIDATED |
| **Clean Windows Sandbox Acceptance**| [`docs/testing/windows-sandbox-acceptance.md`](testing/windows-sandbox-acceptance.md)| QA Engineers | VALIDATED |
| **Performance Budgets & Benchmarks**| [`docs/testing/performance.md`](testing/performance.md) | QA, Architects | VALIDATED |
| **Security Testing Protocols** | [`docs/testing/security-testing.md`](testing/security-testing.md) | QA, Security | VALIDATED |

### 3.14 Release Engineering
| Document | Path | Audience | Status |
| :--- | :--- | :--- | :--- |
| **Canonical Release Process** | [`docs/release/release-process.md`](release/release-process.md) | Release Engineers| VALIDATED |
| **Formal Release Checklist** | [`docs/release/release-checklist.md`](release/release-checklist.md) | QA, Product Owner| VALIDATED |
| **Semantic Versioning Policy** | [`docs/release/versioning.md`](release/versioning.md) | Architects, Devs | VALIDATED |
| **Artifact & Git Exclusion Policy**| [`docs/release/artifact-policy.md`](release/artifact-policy.md) | All Engineers | VALIDATED |
| **ReleaseGuard Security Scanner** | [`docs/release/releaseguard.md`](release/releaseguard.md) | DevSecOps, QA | VALIDATED |

### 3.15 Support & Incident Runbooks
| Document | Path | Audience | Status |
| :--- | :--- | :--- | :--- |
| **Master Troubleshooting Matrix** | [`docs/support/troubleshooting.md`](support/troubleshooting.md) | Support, Ops | VALIDATED |
| **Runbook: Database Unavailable** | [`docs/support/runbooks/database-unavailable.md`](support/runbooks/database-unavailable.md)| Support | VALIDATED |
| **Runbook: Continuity Mode Incident**| [`docs/support/runbooks/continuity-mode.md`](support/runbooks/continuity-mode.md)| Support | VALIDATED |
| **Runbook: Transaction Outbox Stalled**| [`docs/support/runbooks/outbox-stalled.md`](support/runbooks/outbox-stalled.md)| Support, Devs | VALIDATED |
| **Runbook: Printer / Kitchen Failure**| [`docs/support/runbooks/printer-failure.md`](support/runbooks/printer-failure.md)| Support | VALIDATED |
| **Runbook: License / Tamper Issue** | [`docs/support/runbooks/license-issue.md`](support/runbooks/license-issue.md) | Support, License | VALIDATED |
| **Runbook: Schema Compatibility Mismatch**| [`docs/support/runbooks/schema-mismatch.md`](support/runbooks/schema-mismatch.md)| Support, DBAs | VALIDATED |
| **Runbook: Installer Execution Failure**| [`docs/support/runbooks/installer-failure.md`](support/runbooks/installer-failure.md)| Support, Ops | VALIDATED |
| **Runbook: SQL Express Setup Stalled**| [`docs/support/runbooks/sql-installation-slow.md`](support/runbooks/sql-installation-slow.md)| Support, Ops | VALIDATED |

### 3.16 Enterprise Governance, Roadmap & Audit
| Document | Path | Audience | Status |
| :--- | :--- | :--- | :--- |
| **Documentation Coverage Matrix** | [`docs/documentation-coverage.md`](documentation-coverage.md) | Management, QA | VALIDATED |
| **Verified Documentation Gaps** | [`docs/documentation-gaps.md`](documentation-gaps.md) | Architects, Devs | VALIDATED |
| **Documentation Style Guide** | [`docs/documentation-style-guide.md`](documentation-style-guide.md)| Tech Writers | VALIDATED |
| **Enterprise Glossary** | [`docs/glossary.md`](glossary.md) | All Readers | VALIDATED |
| **Verified Known Limitations** | [`docs/known-limitations.md`](known-limitations.md) | All Readers | FACTUAL BASELINE |
| **Canonical Engineering Roadmap** | [`docs/roadmap/engineering-roadmap.md`](roadmap/engineering-roadmap.md)| Architects, PMs | CANONICAL BASELINE |

---

## 4. Documentation Governance & Authoring Standards

To maintain the high standard of documentation across future CBOS updates:
1. **Source of Truth Precedence:**
   - C# 13 Source Code > Automated Tests > Verified Runtime Telemetry > Permanent Rules (`.agents/rules/`) > Platform Documentation.
2. **Strict Claims Standard:**
   - State `LIVE UI NOT EXECUTED` unless the application was interactively operated on a physical/virtual display.
   - State `VISUAL STUDIO DESIGNER UI NOT EXECUTED` unless forms were opened inside Visual Studio Designer.
   - Never claim PCI certification.
   - Accurately label unbuilt or partially built capabilities (e.g., Refund workflow as `REFUND DOMAIN NOT YET IMPLEMENTED`).
3. **Zero Leaked Secrets:**
   - Never commit private keys, development passwords, machine tokens, or customer databases.
4. **Change Management:**
   - All documentation updates must follow the metadata format defined in [`docs/documentation-style-guide.md`](documentation-style-guide.md).
