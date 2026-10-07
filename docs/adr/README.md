# Architecture Decision Records (ADRs)

| Attribute | Details |
| :--- | :--- |
| **Area** | Architecture Governance |
| **Audience** | Software Architects, Engineering Leads, Developers |
| **Last Reviewed Version** | CBOS 1.2.2 |
| **Status** | **CANONICAL REGISTRY** |
| **Classification** | **PUBLIC-SAFE** |

---

## 1. Purpose & Framework

Architecture Decision Records (ADRs) capture significant architectural decisions made during the design and evolution of Clovent Business Operating System (CBOS). Each record describes the business and technical context, alternatives considered, the chosen decision, and resulting consequences.

### Standard ADR Format
Every ADR adheres to the standard schema:
1. **Title:** Sequential identifier and concise decision title (e.g., `ADR-0001-single-physical-database-multiple-schemas`).
2. **Status:** `PROPOSED`, `ACCEPTED`, `SUPERSEDED`, or `DEPRECATED`.
3. **Context:** The technical constraints, requirements, and problem statement.
4. **Decision:** The exact architectural or technological direction chosen.
5. **Consequences:** Positive outcomes, negative trade-offs, and downstream implementation impacts.
6. **Compliance & Verification:** How the decision is enforced in code and verified by tests.

---

## 2. Core System Architecture Decision Records

| Record | Title | Status | Scope |
| :--- | :--- | :---: | :--- |
| **[ADR-0001](ADR-0001-single-physical-database-multiple-schemas.md)** | Single Physical Database with Schema-Isolated Contexts | **ACCEPTED** | Database & Persistence |
| **[ADR-0002](ADR-0002-clean-architecture-bounded-contexts.md)** | Clean Architecture & Autonomous Bounded Contexts | **ACCEPTED** | Solution Structure & DDD |
| **[ADR-0003](ADR-0003-transactional-outbox.md)** | Transactional Outbox for Asynchronous Integration | **ACCEPTED** | Messaging & Reliability |
| **[ADR-0004](ADR-0004-continuity-mode.md)** | Emergency Continuity Mode & Offline Cash Journal | **ACCEPTED** | Fault Tolerance & Operations |
| **[ADR-0005](ADR-0005-local-operational-cache.md)** | Encrypted Local Operational Cache with Freshness TTL | **ACCEPTED** | Offline Data & Security |
| **[ADR-0006](ADR-0006-sql-server-database-platform.md)** | Microsoft SQL Server as Enterprise Database Engine | **ACCEPTED** | Data Platform & Storage |
| **[ADR-0007](ADR-0007-winforms-devexpress-desktop.md)** | Windows Forms & DevExpress 26.1 Desktop Presentation | **ACCEPTED** | Desktop Client & UI |
| **[ADR-0008](ADR-0008-offline-rsa-license-validation.md)** | Offline Asymmetric RSA-2048 Cryptographic Licensing | **ACCEPTED** | Software Licensing & Anti-Piracy |

---

## 3. Presentation & User Experience ADRs (Historical UI Series)

Detailed UI layout decisions established during POS frontend construction are maintained in the [`docs/architecture/adr/`](../architecture/adr/) directory:
- **[ADR-001: Restaurant POS Single Form Consolidated Layout](../architecture/adr/ADR-001-RestaurantPOS-SingleForm.md)** — Consolidation of register workflows into `RestaurantPosForm` to prevent coordinate drift.
- **[ADR-002: Integrated Bottom Payment Tender Strip](../architecture/adr/ADR-002-Payment-Tender-Strip.md)** — Inline bottom payment strip ensuring immediate one-click checkout.
- **[ADR-003: Visual Studio Designer-Safe WinForms Guidelines](../architecture/adr/ADR-003-Designer-Safe-WinForms.md)** — Strict rules for constructors and `InitializeComponent()`.
- **[ADR-004: Responsive POS Layout Design](../architecture/adr/ADR-004-Responsive-POS-Layout.md)** — Dynamic percentage-based column allocation across diverse screen ratios.
- **[ADR-005: Customer Selection and Credit-Sales Workflow](../architecture/adr/ADR-005-Customer-Credit-Workflow.md)** — On-account sales, credit limits, and advance balances.
- **[ADR-006: Customer Management and Ledger Statement Architecture](../architecture/adr/ADR-006-Customer-Management-and-Ledger.md)** — A/R ledger modeling and financial statements.
- **[ADR-007: Designer CodeDom Constraints and Code-Built Views](../architecture/adr/ADR-007-Designer-CodeDom-Constraints.md)** — Prohibiting modern C# constructs in `*.Designer.cs` that break VS CodeDom parsers.

---

## 4. Cross References
- [System Architecture](../architecture/system-architecture.md)
- [Database Architecture](../database/database-architecture.md)
- [Coding Guidelines](../development/coding-guidelines.md)
