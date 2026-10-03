# Database & Persistence Architecture Rules

**Scope:** `src/**/Infrastructure/**`, `src/Clovent.Desktop/Commissioning/Database/**`, persistence, EF Core migrations  
**Authoritative Reference:** [AGENTS.md](file:///d:/Clovent%20Business%20Operating%20System/AGENTS.md)

---

## 1. Single Database Consolidation

The Clovent Business Operating System operates against **exactly one physical SQL Server database**:
- **Database Name:** `Clovent_BusinessOperatingSystem`
- **Canonical Connection String:** Registered under `ConnectionStrings:Default` in `appsettings.json`:
  ```json
  {
    "ConnectionStrings": {
      "Default": "Server=.;Database=Clovent_BusinessOperatingSystem;Trusted_Connection=True;TrustServerCertificate=True;"
    }
  }
  ```
- **Architectural Constraints:**
  - **Never** create separate physical databases for individual modules or design-time tools.
  - **Never** merge all entity models into a single monolithic DbContext.
  - Each bounded context maintains its own isolated `DbContext` class targeting the shared database.

---

## 2. Schema Isolation & Migration History

Each bounded context owns a dedicated SQL schema within `Clovent_BusinessOperatingSystem`:

| Bounded Context | Schema | DbContext | Migration History Table |
|---|---|---|---|
| **Authentication** | `[Authentication]` | `AuthenticationDbContext` | `[Authentication].[__EFMigrationsHistory]` |
| **Identity** | `[Identity]` | `IdentityDbContext` | `[Identity].[__EFMigrationsHistory]` |
| **MasterData** | `[MasterData]` | `MasterDataDbContext` | `[MasterData].[__EFMigrationsHistory]` |
| **Catalog** | `[Catalog]` | `CatalogDbContext` | `[Catalog].[__EFMigrationsHistory]` |
| **Inventory** | `[Inventory]` | `InventoryDbContext` | `[Inventory].[__EFMigrationsHistory]` |
| **Restaurant** | `[Restaurant]` | `RestaurantDbContext` | `[Restaurant].[__EFMigrationsHistory]` |

### Schema Configuration Rule:
Every `DbContext` must set its default schema in `OnModelCreating`:
```csharp
modelBuilder.HasDefaultSchema("<ContextName>");
```

### Migration History Configuration Rule:
Every `DbContextFactory` and runtime registration must isolate its migration history table:
```csharp
options.UseSqlServer(connectionString, sql =>
    sql.MigrationsHistoryTable("__EFMigrationsHistory", "<ContextName>"));
```

---

## 3. Database Security & Least Privilege

CBOS enforces strict separation between application runtime identities and migration/admin credentials:

### A. Runtime Identity (`cbos_app`)
Routine POS and back-office operations run under the least-privileged `cbos_app` identity:
- **Permitted Roles:** `db_datareader`, `db_datawriter`, `GRANT EXECUTE`.
- **Strictly Forbidden:** `db_owner`, `db_ddladmin`, `sysadmin`, `ALTER DATABASE`, `CREATE TABLE`, `DROP TABLE`.
- The runtime application must never alter schemas or execute DDL statements.

### B. Migration & Setup Identity
- Schema migrations (`dotnet ef database update`) and database initial provisioning are executed exclusively by maintenance credentials or DBA setup routines.
- Setup credentials are never stored in client workstation settings.

### C. DPAPI Credential Storage
- Database passwords entered via connection dialogs are encrypted using the Windows Data Protection API (DPAPI).
- **Multi-User Terminals:** Encrypted with `DataProtectionScope.LocalMachine` in `%ProgramData%\Clovent\BusinessOperatingSystem\Config\database.config.json` (Administrators: Full Control; Users: Read-Only).
- **Single-User Terminals:** Encrypted with `DataProtectionScope.CurrentUser` in `%LocalAppData%\Clovent\Clovent.BusinessOperatingSystem\database.config.json`.
- Plaintext SQL connection passwords must **never** be logged, printed to console, or saved to JSON.
