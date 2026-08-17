# Clovent Business Operating System (CBOS) Documentation Index

Welcome to the CBOS enterprise platform documentation. This repository contains the architecture records, design decisions, testing results, and guides for developers and system architects.

---

## 🗺️ Core Sections

### 🏛️ Architecture & Domain Records
- **[Restaurant POS Architecture](file:///d:/Clovent%20Business%20Operating%20System/docs/architecture/RestaurantPOSArchitecture.md)** — Core design, layout panel structure, and implementation of the restaurant register system.
- **[Order Lifecycle Model](file:///d:/Clovent%20Business%20Operating%20System/docs/architecture/OrderLifecycle.md)** — Order state machine, transitions, table split/merge logic, and checks.
- **[Kitchen Ticket Workflow](file:///d:/Clovent%20Business%20Operating%20System/docs/architecture/KitchenWorkflow.md)** — Preparation state machine, ticket routing, and dispatcher logic.
- **[Desktop Shell & UI Layout](file:///d:/Clovent%20Business%20Operating%20System/docs/architecture/DesktopShellArchitecture.md)** — Application shell, tabbed document view, and layout rules.
- **[Identity & Authentication Domains](file:///d:/Clovent%20Business%20Operating%20System/docs/architecture/IdentityDomain.md)** — Users, roles, authentication services, login flows, and permissions.
- **[Catalog & Inventory Architecture](file:///d:/Clovent%20Business%20Operating%20System/docs/architecture/CatalogArchitecture.md)** — Variant management, pricing engines, stock allocations, and adjustment flows.

---

### 🏁 Architecture Decision Records (ADRs)
Locate sequential records for core decisions under the **[ADR Directory](file:///d:/Clovent%20Business%20Operating%20System/docs/architecture/adr/)**:
- **[ADR-001: Restaurant POS Single Form Consolidated Layout](file:///d:/Clovent%20Business%20Operating%20System/docs/architecture/adr/ADR-001-RestaurantPOS-SingleForm.md)**
- **[ADR-002: Integrated Bottom Payment Tender Strip](file:///d:/Clovent%20Business%20Operating%20System/docs/architecture/adr/ADR-002-Payment-Tender-Strip.md)**
- **[ADR-003: Visual Studio Designer-Safe WinForms Guidelines](file:///d:/Clovent%20Business%20Operating%20System/docs/architecture/adr/ADR-003-Designer-Safe-WinForms.md)**
- **[ADR-004: Responsive POS Layout Design](file:///d:/Clovent%20Business%20Operating%20System/docs/architecture/adr/ADR-004-Responsive-POS-Layout.md)**
- **[ADR-005: Customer Selection and Credit-Sales Workflow](file:///d:/Clovent%20Business%20Operating%20System/docs/architecture/adr/ADR-005-Customer-Credit-Workflow.md)**
- **[ADR-006: Customer Management and Ledger Statement Architecture](file:///d:/Clovent%20Business%20Operating%20System/docs/architecture/adr/ADR-006-Customer-Management-and-Ledger.md)**
- **[ADR-007: Designer CodeDom Constraints and Code-Built Views](file:///d:/Clovent%20Business%20Operating%20System/docs/architecture/adr/ADR-007-Designer-CodeDom-Constraints.md)** — extends ADR-003; the Designer *parses* `InitializeComponent()` before instantiating anything, so ADR-003's DI guarding alone does not make a form Designer-compatible.

---

### 🧪 Verification & QA Guides
- **[Automated Build & Test Results](file:///d:/Clovent%20Business%20Operating%20System/docs/testing/RestaurantPOSTesting.md)** — Compilation parameters, warnings, test suite outputs, and metrics.
- **[Manual QA Verification Matrix](file:///d:/Clovent%20Business%20Operating%20System/docs/testing/RestaurantPOSManualQA.md)** — Repeatable test scenarios for authentication, registers, customers, cart, and payment flows.
- **[Technical Debt Log](file:///d:/Clovent%20Business%20Operating%20System/docs/architecture/TechnicalDebt.md)** — Known design limits, N+1 query patterns, and local storage caveats.

---

### 📋 History & Changelog
- **[Restaurant POS Changelog](file:///d:/Clovent%20Business%20Operating%20System/docs/changelog/RestaurantPOS.md)** — Release notes, reasons, and file changes for the restaurant register screens.

---

## 🔍 Source-of-Truth Policy

All documentation must remain strictly synchronized with implementation under this precedence model:
1. **Current C# Source Code** (authoritative for execution logic and structural namespaces).
2. **Automated Test Cases** (authoritative for expected functional outputs and constraints).
3. **Runtime Verification Evidence** (authoritative for visual displays, layout docking, and clipping).
4. **Architecture Decisions** (ADRs).
5. **Platform Documentation**.
6. **Historical Notes**.

If code or verified runtime behavior conflicts with documentation, update the documentation immediately. Do not document unverified assumptions as facts.
