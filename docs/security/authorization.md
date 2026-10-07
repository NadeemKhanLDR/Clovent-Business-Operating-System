# CBOS Authorization & Access Control Architecture

| Attribute | Details |
| :--- | :--- |
| **Area** | Role-Based Access Control (RBAC) & Privileges |
| **Audience** | Security Architects, Application Developers, System Administrators |
| **Last Reviewed Version** | CBOS 1.2.2 |
| **Status** | **UI GATING IMPLEMENTED / PIPELINE AUTHORIZATION PARTIAL** |
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
- Voiding an item from an active order after kitchen tickets have printed.
- Voiding an already-completed check.
- Applying manual ad-hoc discounts exceeding cashier tolerance thresholds.
- Overriding standard catalog item prices.

### Elevation Workflow:
1. When a cashier triggers a restricted action, CBOS renders a modal manager override dialog (`ManagerAuthorizationDialog.cs`).
2. A manager or supervisor enters their credentials or quick PIN.
3. The authorization service verifies that the elevated user possesses the requisite permission (e.g., `POS.VoidCompletedOrder`).
4. The elevation token is granted for that specific single action and logged to the audit trail with the authorizing manager's user ID.

---

## 4. UI Gating vs. Authoritative Application-Layer Authorization

> [!IMPORTANT]
> **HONEST STATUS OF APPLICATION-LAYER AUTHORIZATION:**
>
> - **UI Gating (Fully Implemented):** DevExpress ribbon controls, back-office navigation tiles, and action buttons dynamically query the user's cached permissions (`IAuthorizationService`). Inaccessible options are automatically hidden or disabled.
> - **Application-Layer Pipeline Authorization (Partially Implemented):** While administrative pre-login checks enforce Windows UAC elevation (`AdministrativePrivilegeChecker.cs`) and sensitive domain operations validate caller roles, universal MediatR pipeline behaviors (`IPipelineBehavior`) that authoritatively enforce permissions across *every* command handler regardless of caller are currently **PARTIALLY IMPLEMENTED**.
> - **Roadmap:** Full pipeline behavior authorization enforcement across all remaining handlers is actively scheduled for completion in an upcoming release.

---

## 5. Administrative Privilege Verification

- **Pre-Login Sensitive Tasks:** Modifying database connection parameters, generating provisioning scripts, or running repair tools prior to sign-in requires Windows Administrator UAC elevation (`AdministrativePrivilegeChecker.RequiresUacElevation()`).
- **Post-Login Administrative Tasks:** Managing users, resetting security PINs, or modifying branch master data requires membership in the `Administrator` role.

---

## 6. Key Classes & Source Traceability

- **Permission Cache:** `src/Clovent.Identity.Infrastructure/Caching/MemoryPermissionCache.cs`
- **Cache Abstraction:** `src/Clovent.Identity.Application/Authorization/IPermissionCache.cs`
- **Role Entity:** `src/Clovent.Identity/Roles/Role.cs`
- **Permission Entity:** `src/Clovent.Identity/Permissions/Permission.cs`
- **Administrative Checker:** `src/Clovent.Desktop/Forms/Base/AdministrativePrivilegeChecker.cs`
- **Manager Override Dialog:** `src/Clovent.Desktop/Restaurant/Orders/ManagerAuthorizationDialog.cs`

---

## 7. Cross References
- [Authentication Architecture](authentication.md)
- [Security Architecture](security-architecture.md)
- [Threat Model](threat-model.md)
- [Identity Bounded Context](../architecture/bounded-contexts.md)
