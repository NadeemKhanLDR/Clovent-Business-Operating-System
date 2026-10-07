# CBOS Authorization & Access Control Architecture

| Attribute | Details |
| :--- | :--- |
| **Area** | Role-Based Access Control (RBAC) & Privileges |
| **Audience** | Security Architects, Application Developers, System Administrators |
| **Last Reviewed Version** | CBOS 1.2.2 |
| **Status** | **FACTUAL BASELINE** |
| **Classification** | **PUBLIC-SAFE** |

---

## 1. Role-Based Access Control (RBAC) Architecture

CBOS manages user permissions through a granular Role-Based Access Control (RBAC) model implemented in `Clovent.Identity`:
- **Granular Permissions:** 214 discrete permissions categorized across business areas:
  - `POS.*` (Order entry, line voiding, bill splitting, customer selection)
  - `Catalog.*` (Category management, product creation, price updating)
  - `Inventory.*` (Stock receiving, adjustments, transfers)
  - `MasterData.*` (Terminal setup, warehouse configuration, currency rules)
  - `Reports.*` (Financial summaries, sales audits, tax reports)
  - `Admin.*` (User management, role assignment, database provisioning)
- **Standard System Roles:**
  - `Administrator`: Full access across all 214 permissions and system configuration.
  - `Manager`: Access to operational management, reporting, shift approvals, and price overrides.
  - `Cashier`: Standard POS order entry, cash tendering, and till operations.
  - `Supervisor`: Mid-level front-of-house authority for line voids and discounts.

---

## 2. In-Memory Permission Caching

To prevent continuous database round-trips during high-speed cashier interactions:
- Permissions for the active user are cached in-memory upon authentication via `MemoryPermissionCache` (`src/Clovent.Identity.Infrastructure/Caching/MemoryPermissionCache.cs`).
- Cache invalidation occurs automatically when a user's roles are updated or upon explicit session termination.

---

## 3. Managerial Elevation in POS

Certain high-risk operational actions require **Managerial Elevation**:
- Overriding standard catalog item prices.
- Approving credit sales that exceed a customer's credit limit.
- Voiding unprinted order lines or open checks.

### Elevation Workflow:
1. When a cashier triggers a restricted action, CBOS renders a modal manager override dialog (`ManagerAuthorizationForm.cs`).
2. A manager or supervisor enters their credentials.
3. The authorization service verifies that the elevated user possesses the requisite action permission.
4. The elevation token is granted for that specific single action and logged to the audit trail with the authorizing manager's identity.

---

## 4. UI Visibility vs. Authoritative Application-Layer Authorization

> [!IMPORTANT]
> **GOVERNANCE PRINCIPLE: UI VISIBILITY IS NOT AUTHORIZATION**
> - Hiding or disabling UI controls (ribbon buttons, menu items, action commands) is a usability feature, not security enforcement.
> - Authoritative security must be enforced independently at the application layer (`MediatR` pipeline or service boundary).

### Verified Status & Known Enforcement Gaps (CBOS 1.2.2 Baseline):
1. **UI-Level Gating (Fully Implemented):** DevExpress ribbon controls, back-office navigation tiles, and action buttons dynamically query cached user permissions (`IAuthorizationService`). Inaccessible options are automatically hidden or disabled.
2. **Administrative Fast-Path Bypass (Known Gap — Targeted 1.2.3):** `AdministrativePrivilegeChecker.HasAdministrativePrivileges()` contains a fast-path username check (`admin`/`administrator`) that elevates privileges without evaluating assigned roles in the Identity database. Eliminating this hardcoded check is a mandatory deliverable in CBOS 1.2.3.
3. **Manager Authorization Fail-Open Gap (Known Gap — Targeted 1.2.3):** In `ManagerAuthorizationForm.cs`, if `_authService` is null or unconfigured, the dialog defaults to `DialogResult.OK` and permits the privileged action. This must be refactored to fail closed unconditionally in CBOS 1.2.3.
4. **Universal Pipeline Authorization (Partially Implemented — Targeted 1.2.3):** Universal MediatR pipeline behaviors (`IPipelineBehavior`) that authoritatively enforce permissions across *every* command handler regardless of caller are currently in progress and not yet universally applied across all 6 bounded contexts.
5. **Completed-Order Void Prohibition (1.2.3 Pilot Rule):** In 1.2.2, `Order.Void()` permits voiding completed orders in the domain aggregate. During the attended 1.2.3 single-terminal pilot, voiding completed orders is formally prohibited to preserve ledger integrity until the formal compensating Refund aggregate ships in CBOS 1.3.0.

---

## 5. Administrative Privilege Verification

- **Pre-Login Sensitive Tasks:** Modifying database connection parameters, generating provisioning scripts, or running repair tools prior to sign-in requires Windows Administrator UAC elevation (`WindowsCommissioningSecurity.IsRunningAsAdministrator()`).
- **Post-Login Administrative Tasks:** Managing users, resetting security PINs, or modifying branch master data requires membership in the `Administrator` role.

---

## 6. Key Classes & Source Traceability

- **Permission Cache:** `src/Clovent.Identity.Infrastructure/Caching/MemoryPermissionCache.cs`
- **Cache Abstraction:** `src/Clovent.Identity.Application/Authorization/IPermissionCache.cs`
- **Role Entity:** `src/Clovent.Identity/Roles/Role.cs`
- **Permission Entity:** `src/Clovent.Identity/Permissions/Permission.cs`
- **Administrative Checker:** `src/Clovent.Desktop/Authorization/AdministrativePrivilegeChecker.cs`
- **Manager Override Dialog:** `src/Clovent.Desktop/Restaurant/Shared/ManagerAuthorizationForm.cs`

---

## 7. Cross References
- [Authentication Architecture](authentication.md)
- [Security Architecture](security-architecture.md)
- [Threat Model](threat-model.md)
- [Security & Licensing Rules](../../.agents/rules/security.md)
- [Canonical Engineering Roadmap](../roadmap/engineering-roadmap.md)
