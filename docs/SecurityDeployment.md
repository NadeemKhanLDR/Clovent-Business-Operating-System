# Clovent Business Operating System - Security & Deployment Hardening Guide

## 1. Principle of Least Privilege: Database Identity Separation
Production deployments must **never use the SQL `sa` superuser account** or grant administrative DDL roles to everyday POS runtime processes.

CBOS enforces a strict architectural separation between **Migration/Installation Identity** and **Runtime Application Identity**.

---

### A. Runtime Application Identity (`cbos_app`)
The runtime POS user account must receive only the minimal permissions required to execute business operations.

#### Permitted Runtime Permissions:
- `db_datareader` (read all business tables)
- `db_datawriter` (insert, update, delete business transactions)
- `GRANT EXECUTE` (execute stored procedures/functions across application schemas)

#### Strictly Forbidden Runtime Permissions:
- `db_owner`
- `db_ddladmin` (Runtime user MUST NOT create, alter, or drop tables/schemas)
- `sysadmin`
- `ALTER DATABASE`
- `CREATE / DROP TABLE`, `CREATE / DROP PROCEDURE`
- Server-level backup/restore rights

#### Runtime SQL Provisioning Script:
```sql
USE [master];
GO

-- 1. Create Least-Privileged Runtime Login
IF NOT EXISTS (SELECT * FROM sys.server_principals WHERE name = 'cbos_app')
BEGIN
    CREATE LOGIN [cbos_app] 
    WITH PASSWORD = 'ReplaceWithStrongSecurePassword!9#', 
    CHECK_EXPIRATION = OFF, 
    CHECK_POLICY = ON;
END
GO

USE [Clovent_BusinessOperatingSystem];
GO

-- 2. Create Database User for Runtime
IF NOT EXISTS (SELECT * FROM sys.database_principals WHERE name = 'cbos_app')
BEGIN
    CREATE USER [cbos_app] FOR LOGIN [cbos_app];
END
GO

-- 3. Grant ONLY Data Manipulation and Execution Rights (NO db_ddladmin)
ALTER ROLE [db_datareader] ADD MEMBER [cbos_app];
ALTER ROLE [db_datawriter] ADD MEMBER [cbos_app];
GRANT EXECUTE TO [cbos_app];
GO
```

---

### B. Migration & Maintenance Identity (`cbos_admin` / DBA)
Database schema creation, table modifications, and EF Core migrations (`dotnet ef database update` or `ConsolidateToSingleDatabase.sql`) are applied exclusively by a dedicated migration account or DBA credential during scheduled maintenance windows:
- **Scope:** Used only during setup, initial install, or formal version upgrades.
- **Isolation:** Migration credentials are never stored in client workstation configurations or application settings files.

---

## 2. Authentication Recommendation: Windows Authentication
- **Recommended Standard:** **Windows Authentication (Integrated Security)**.  
  POS terminals joined to an Active Directory domain or managed Windows workgroup should authenticate using dedicated domain/machine service accounts. This eliminates SQL password storage entirely.
- **SQL Server Authentication:** Supported for standalone workgroup terminals where Windows Authentication is impractical. Passwords entered via the `DatabaseConnectionDialog` are encrypted using Windows DPAPI and never stored in plain text.

---

## 3. DPAPI Scope & Machine-Level Credential Protection
CBOS protects SQL credentials using the Windows Data Protection API (DPAPI):
1. **Multi-Operator Terminals (Recommended):**  
   Encrypted using `DataProtectionScope.LocalMachine` and persisted into `%ProgramData%\Clovent\BusinessOperatingSystem\database.config.json` with restricted directory ACLs (Administrators: Full Control; Users: Read). This allows all authorized Windows users logging into the shared POS terminal to connect without re-entering credentials.
2. **Single-User Workstations:**  
   Encrypted using `DataProtectionScope.CurrentUser` and stored in `%LocalAppData%\Clovent\Clovent.BusinessOperatingSystem\database.config.json`.
3. **Decryption Fallback:** The runtime `DatabaseSecretStore.Unprotect` method automatically attempts `CurrentUser` scope, falling back to `LocalMachine` scope seamlessly.

---

## 4. ProgramData Directory Access Control Lists (ACLs)
To prevent unauthorized modification of database configurations or software licenses by non-administrative users on shared POS workstations:
- **Base Directory:** `%ProgramData%\Clovent\BusinessOperatingSystem\`
- **Subdirectory Permissions:**
  - `Config\` (`database.config.json`, `commissioning.json`):
    - `Administrators` & `SYSTEM`: Full Control (Read, Write, Delete).
    - `Built-in Users` (Standard Cashiers): Read-Only (`ReadAndExecute`, `Synchronize`). Writes and modifications are strictly blocked.
  - `License\` (`clovent.lic`, `license_tamper.dat`):
    - `Administrators` & `SYSTEM`: Full Control.
    - `Built-in Users`: Read-Only (`ReadAndExecute`, `Synchronize`).
  - `State\` (Commissioning and integrity records):
    - `Administrators` & `SYSTEM`: Full Control.
    - `Built-in Users`: Read-Only.
  - `Logs\` (Rolling diagnostic logs):
    - `Administrators` & `SYSTEM`: Full Control.
    - `Built-in Users`: Read/Write/Append (`ReadAndExecute`, `Modify`, `Synchronize`), allowing standard cashier sessions to log diagnostics and errors without elevated privileges.

---

## 5. Administrative Authorization & Feature Gate Enforcement
To prevent unauthorized tampering by cashiers or standard operators:
- **Pre-Login Configuration Security (Windows UAC Elevation):**
  Before an application administrator is logged in (e.g. at initial setup or connection failure screen), accessing or modifying database connection settings or software license files requires **Windows Administrator UAC Elevation** (`runas`). Standard non-admin Windows users cannot modify machine configuration before login.
- **Post-Login Administrative Authorization:**
  After login, changing database parameters or replacing licenses requires that the authenticated user possesses the `Administrator` application role (`AdministrativePrivilegeChecker`).
- **Production Credential Audit:** Default development credentials (such as `Admin123!`) are wrapped in `#if DEBUG` and never compiled or auto-logged in production release binaries.
- **One-Time Admin Bootstrap Rule:** The first-administrator setup endpoint disables itself permanently once an active administrator account exists, preventing backdoor privilege escalation.

---

## 5. Data Privacy & Logging Hygiene
1. **PCI-DSS Compliance:** Cardholder data (PAN, track data, CVV) is never captured or logged.
2. **Masked Secrets:** Database passwords, user PINs, and authentication tokens are masked in logs.
3. **Zero Private Keys:** No cryptographic signing private keys exist in source repositories or client distributions.
4. **Log Retention:** Local diagnostic logs in `%LocalAppData%\Clovent\Clovent.BusinessOperatingSystem\Logs\` are capped at 31 days with automatic rolling cleanup.
