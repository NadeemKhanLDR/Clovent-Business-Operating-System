# CBOS Documentation Coverage Matrix

| Attribute | Details |
| :--- | :--- |
| **Area** | Documentation Governance & Quality Assurance |
| **Audience** | Technical Leadership, Architects, Auditing Teams, QA |
| **Last Reviewed Version** | CBOS 1.2.2 |
| **Status** | **VERIFIED COMPLETE** |
| **Classification** | **PUBLIC-SAFE** |

---

## 1. Documentation Coverage Overview

This matrix maps every major CBOS subsystem across all critical engineering dimensions (Architecture, Developer Guides, Operations & Deployment, Testing & QA, Security, and Field Support). Every bounded context and cross-cutting platform capability must be documented with source-traceable references.

---

## 2. Comprehensive Subsystem Coverage Matrix

| Subsystem / Context | Architecture Document | Developer / Coding Document | Operations / Deployment | Testing / QA Document | Security & Compliance | Support & Runbooks | Status |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- | :---: |
| **Overall Solution** | `system-architecture.md`<br/>`ADR-0001` - `0008` | `project-structure.md`<br/>`environment-setup.md`<br/>`build.md` | `installation.md`<br/>`topologies.md`<br/>`uninstallation.md` | `testing-strategy.md`<br/>`running-tests.md`<br/>`release-process.md` | `security-architecture.md`<br/>`threat-model.md`<br/>`releaseguard.md` | `troubleshooting.md`<br/>`operations-health.md` | **COVERED** |
| **Authentication** | `bounded-contexts.md`<br/>`application-startup.md` | `coding-guidelines.md` | `commissioning.md` | `running-tests.md` | `authentication.md` | `troubleshooting.md` | **COVERED** |
| **Identity & Access** | `bounded-contexts.md` | `coding-guidelines.md` | `commissioning.md` | `running-tests.md` | `authorization.md` | `troubleshooting.md` | **COVERED** |
| **Master Data** | `bounded-contexts.md`<br/>`domain-data-model.md` | `coding-guidelines.md` | `commissioning.md` | `running-tests.md` | `security-architecture.md` | `troubleshooting.md` | **COVERED** |
| **Product Catalog** | `bounded-contexts.md`<br/>`domain-data-model.md` | `coding-guidelines.md` | `commissioning.md` | `running-tests.md` | `security-architecture.md` | `troubleshooting.md` | **COVERED** |
| **Inventory Control** | `bounded-contexts.md`<br/>`domain-data-model.md` | `coding-guidelines.md` | `topologies.md` | `running-tests.md` | `security-architecture.md` | `troubleshooting.md` | **COVERED** |
| **Restaurant POS Core** | `pos-architecture.md`<br/>`order-lifecycle.md` | `winforms-designer-safety.md`<br/>`ui-high-dpi.md` | `topologies.md`<br/>`installation.md` | `windows-sandbox-acceptance.md`<br/>`performance.md` | `authorization.md`<br/>`threat-model.md` | `troubleshooting.md` | **COVERED** |
| **Payment & Settlements** | `payments.md`<br/>`order-lifecycle.md` | `coding-guidelines.md` | `topologies.md` | `running-tests.md` | `security-architecture.md` | `troubleshooting.md` | **COVERED** |
| **Shifts & Cash Drawer** | `shifts-and-cash-management.md` | `coding-guidelines.md` | `topologies.md` | `running-tests.md` | `authorization.md` | `troubleshooting.md` | **COVERED** |
| **Database & Persistence** | `database-architecture.md`<br/>`domain-data-model.md` | `migrations.md` | `sql-server.md`<br/>`upgrades.md` | `running-tests.md` | `security-architecture.md` | `database-unavailable.md`<br/>`schema-mismatch.md` | **COVERED** |
| **Transactional Outbox** | `transactional-outbox.md`<br/>`ADR-0003` | `coding-guidelines.md` | `operations-health.md` | `running-tests.md` | `threat-model.md` | `outbox-stalled.md` | **COVERED** |
| **Continuity Mode** | `continuity-mode.md`<br/>`ADR-0004` | `coding-guidelines.md` | `topologies.md` | `windows-sandbox-acceptance.md` | `security-architecture.md` | `continuity-mode.md` | **COVERED** |
| **Operational Cache** | `operational-cache.md`<br/>`ADR-0005` | `coding-guidelines.md` | `operations-health.md` | `running-tests.md`<br/>`performance.md` | `security-architecture.md` | `troubleshooting.md` | **COVERED** |
| **QuickBooks Integration** | `quickbooks.md` | `coding-guidelines.md` | `operations-health.md` | `running-tests.md` | `threat-model.md` | `outbox-stalled.md` | **COVERED** |
| **Receipt Printing** | `printing.md` | `winforms-designer-safety.md` | `installation.md` | `running-tests.md` | `security-architecture.md` | `printer-failure.md` | **COVERED** |
| **Software Licensing** | `licensing.md`<br/>`ADR-0008` | `environment-setup.md` | `commissioning.md`<br/>`installation.md` | `security-testing.md` | `security-architecture.md`<br/>`licensing.md` | `license-issue.md` | **COVERED** |
| **Configuration & Formats**| `configuration-architecture.md`<br/>`terminal-identity.md` | `display-settings.md`<br/>`ui-high-dpi.md` | `commissioning.md` | `running-tests.md` | `security-architecture.md` | `troubleshooting.md` | **COVERED** |
| **Backup & Disaster Recovery** | `system-architecture.md` | `coding-guidelines.md` | `backup-and-restore.md`<br/>`disaster-recovery.md` | `running-tests.md` | `security-architecture.md` | `database-unavailable.md` | **COVERED** |
| **Packaging & Installer** | `system-architecture.md` | `build.md` | `installation.md`<br/>`upgrades.md` | `releaseguard.md`<br/>`windows-sandbox-acceptance.md` | `releaseguard.md` | `installer-failure.md`<br/>`sql-installation-slow.md` | **COVERED** |

---

## 3. Dimension Verification Checklist

- [x] **Architecture Coverage:** All 6 bounded contexts, Clean Architecture layers, single-database multi-schema model, Transactional Outbox, and Continuity Mode fully documented with ADRs and Mermaid diagrams.
- [x] **Developer Coverage:** SDK prerequisites, build commands, solution structure, C# 13 / .NET 10 coding standards, Designer-safety rules, and High-DPI guidelines.
- [x] **Operations & Deployment Coverage:** Topologies, SQL Server Express parameters, First-Run Commissioning, upgrades, uninstallation, backup scripts, and disaster recovery.
- [x] **Testing & QA Coverage:** Testing pyramid, exact execution commands, Windows Sandbox acceptance, performance boundaries, and security validation.
- [x] **Security & Licensing Coverage:** Trust boundaries, DPAPI/HMAC/RSA cryptography, least-privilege SQL access, STRIDE threat model, and offline licensing.
- [x] **Support & Troubleshooting Coverage:** Operations health metrics, FileLoggerProvider diagnostics, troubleshooting guide, and 8 dedicated step-by-step runbooks.

---

## 4. Cross References
- [Master Documentation Portal](README.md)
- [Documentation Gaps Report](documentation-gaps.md)
- [Known Limitations](known-limitations.md)
- [Documentation Style Guide](documentation-style-guide.md)
