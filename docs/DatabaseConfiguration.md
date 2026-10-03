# Clovent Business Operating System - Database Configuration Guide

## 1. Single Database Architecture
Following the database consolidation architecture, Clovent Business Operating System operates on a **single physical database**:
- **Database Name:** `Clovent_BusinessOperatingSystem`
- **Schemas:**
  - `Authentication`
  - `Identity`
  - `MasterData`
  - `Catalog`
  - `Inventory`
  - `Restaurant`
  - `Reporting`
- **Migration History Table:** `__EFMigrationsHistory` (partitioned per schema).

---

## 2. Configuration Storage Architecture & Resolution Precedence
CBOS strictly separates **static distribution defaults** from **mutable runtime configuration**:

| Configuration Tier | Physical Location | Scope & Precedence | Security & Protection |
|---|---|---|---|
| **1. Machine-Wide Config (Highest)** | `%ProgramData%\Clovent\BusinessOperatingSystem\database.config.json` | Machine-wide for all Windows operators on the terminal | DPAPI `LocalMachine` scope; restricted directory ACLs. |
| **2. User-Specific Config (Secondary)** | `%LocalAppData%\Clovent\Clovent.BusinessOperatingSystem\database.config.json` | Specific to the signed-in Windows user account | DPAPI `CurrentUser` scope. |
| **3. Static Defaults (Fallback)** | `appsettings.Production.json` / `appsettings.json` in install directory | Read-only global defaults shipped with binaries | Safe defaults only (`Trusted_Connection=True`). Zero plain-text passwords. |

### Resolution Order:
When connecting to SQL Server, `DatabaseSecretStore.ResolveConnectionString()` evaluates configuration in order:
1. If `%ProgramData%` machine config exists, it is loaded and its DPAPI password unprotected.
2. Else if `%LocalAppData%` user config exists, it is loaded and unprotected.
3. Else falls back to `appsettings.Production.json` / `appsettings.json`.

---

## 3. Supported Authentication Modes

### A. Windows Authentication (Integrated Security - Recommended)
- Preferred for corporate domains and secure workgroups.
- Connection string structure:
  ```text
  Server=SERVER_NAME;Database=Clovent_BusinessOperatingSystem;Trusted_Connection=True;TrustServerCertificate=True;
  ```
- No passwords stored or managed.

### B. SQL Server Authentication (Encrypted Credentials)
- Supported for distributed or non-domain POS terminals.
- Connection string structure:
  ```text
  Server=SERVER_NAME;Database=Clovent_BusinessOperatingSystem;User Id=cbos_app;Password=DECRYPTED_SECRET;TrustServerCertificate=True;
  ```
- **PCI-DSS Compliance:** The SQL password entered into `DatabaseConnectionDialog` is encrypted immediately via DPAPI before writing to disk. Plain-text passwords are never persisted to configuration files.

---

## 4. DPAPI Scope & Security Model
CBOS supports both DPAPI protection scopes:
- **`DataProtectionScope.LocalMachine`:** Used for machine-level configurations under `%ProgramData%`. Ensures multi-shift POS terminals where cashiers log in with different Windows accounts can all access the centralized database without individual configuration.
- **`DataProtectionScope.CurrentUser`:** Used for user-level configurations under `%LocalAppData%`.
- **Automatic Fallback:** `DatabaseSecretStore.Unprotect` transparently tries `CurrentUser` first, falling back to `LocalMachine` if needed.

---

## 5. Administrative Authorization & Pre-Login Elevation
Modifying database connection parameters affects system-wide operations and security:
- **Pre-Login Security (Windows UAC):** Before an application user logs in (such as initial workstation commissioning or database connection failure), opening or saving database connection settings requires **Windows Administrator UAC Elevation**. An unprivileged Windows standard user cannot alter database targets or credentials.
- **Post-Login Security (Application Admin):** After login, changing database connection parameters requires the authenticated user to possess the `Administrator` role (`AdministrativePrivilegeChecker`).
- **Identity Separation:** The runtime account configured here (`cbos_app`) must possess only `db_datareader`, `db_datawriter`, and `EXECUTE`. Privileged installer accounts (`sa` or DBA) are used strictly transiently during initial setup/migration and never stored.
- **Cold-Start Commissioning:** On a fresh client workstation, the First-Run Setup Wizard guides the administrator through server connection, database creation, and controlled schema migration before any cashier sign-in.

