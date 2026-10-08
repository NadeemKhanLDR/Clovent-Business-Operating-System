# Database & Persistence Architecture Rules

**Scope:** `src/**/Infrastructure/**`, `src/Clovent.Desktop/Commissioning/Database/**`, persistence, EF Core migrations  
**Authoritative Reference:** [AGENTS.md](../../AGENTS.md)

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

## 3. Migration Data Safety & Destructive Change Review

1. **Destructive Migration Review:** Any EF Core migration or script that drops a table, drops a column, renames a column without data migration, or alters column precision downward requires explicit review and approval.
2. **Zero Unintentional Data Loss:** Schema migrations must preserve existing data on upgrade. Breaking schema changes require expand-and-contract migration patterns.
3. **No Speculative Migrations:** Migrations must correspond to active domain aggregates and verified features. Never commit speculative migrations for unapproved backlog concepts.
4. **Real SQL Server Validation Triggers:**
   - Any modification to EF Core entity configurations (`IEntityTypeConfiguration<T>`).
   - Any new or modified migration (`Add-Migration` / `Up` / `Down` scripts).
   - Any raw SQL command, index change, or schema-scoped table change.
   - Any concurrency token (`RowVersion`) or precision modification.
   *These changes must be validated against real SQL Server before release acceptance.*

---

## 4. Persisted Contract Compatibility

1. **Schema & Contract Evolution:** Changes to persisted entity schemas, Outbox payloads, Operational Cache tables, and local journal structures must maintain backward compatibility with existing databases and serialized payloads.
2. **Explicit Decimal Precision:** Every monetary and fractional column must declare explicit SQL Server precision and scale in EF Core configurations:
   ```csharp
   builder.Property(e => e.Amount).HasPrecision(18, 2);
   builder.Property(e => e.Quantity).HasPrecision(18, 4);
   ```
   Never rely on database provider default precisions.
3. **Zero Database Migrations for CBOS 1.2.3:** CBOS 1.2.3 requires zero database schema migrations; the database schema remains strictly frozen at the 1.2.2 baseline.
4. **Milestone Bounding for Optimistic Concurrency:** Aggregate-level optimistic concurrency tokens (`RowVersion` / `IsRowVersion()`) across `Order`, `Table`, `Shift`, and `WarehouseStock` belong to the approved **CBOS 1.3.0** milestone. This permanent rule must not silently expand the scope of CBOS 1.2.3 or mandate immediate schema migrations before CBOS 1.3.0.

---

## 5. Database Security & Least Privilege

CBOS enforces strict separation between application runtime identities and migration/admin credentials:

### A. Runtime Identity (`cbos_app`)
Routine POS and back-office operations run under the least-privileged `cbos_app` identity:
- **Permitted Roles:** `db_datareader`, `db_datawriter`, `GRANT EXECUTE`.
- **Strictly Forbidden:** `db_owner`, `db_ddladmin`, `sysadmin`, `ALTER DATABASE`, `CREATE TABLE`, `DROP TABLE`.
- The runtime application must never alter schemas or execute DDL statements.

### B. Migration & Setup Identity
- Schema migrations (`dotnet ef database update`) and database initial provisioning are executed exclusively by maintenance credentials or DBA setup routines.
- Setup credentials are never stored in client workstation settings.

### C. DPAPI Credential Storage & Configuration Precedence
- Database passwords entered via connection dialogs are encrypted using the Windows Data Protection API (DPAPI).
- **Configuration Precedence:** Commissioned machine configuration in `%ProgramData%\Clovent\BusinessOperatingSystem\Config\database.config.json` (encrypted with `DataProtectionScope.LocalMachine`; Administrators: Full Control, Users: Read-Only) takes strict precedence.
- **LocalAppData Fallback:** User-level configuration in `%LocalAppData%\Clovent\Clovent.BusinessOperatingSystem\database.config.json` (encrypted with `DataProtectionScope.CurrentUser`) is strictly limited to controlled development and test scenarios and must never override commissioned machine configuration.
- **Atomic Writes for Critical Configuration:** Writes to Tier-1 database and commissioning configurations must execute atomically: write to a temporary file, validate structure and content, flush to disk, and atomically replace the target file. This requirement targets Tier-1 critical configuration and does not expand into a rewrite of every UI theme or preference store.
- Plaintext SQL connection passwords must **never** be logged, printed to console, or saved to JSON.

### D. Backup & Disaster Recovery Encryption Rules
- **TDE vs. Native Backup Encryption:** Transparent Data Encryption (TDE - protecting database files at rest) and native backup encryption (`BACKUP ... WITH ENCRYPTION`) are distinct SQL Server features. Neither is supported natively in SQL Server Express.
- **DPAPI Disaster Recovery Mandate:** While local credentials and terminal caches use machine-bound Windows DPAPI, **disaster recovery must NOT depend solely on the original machine's DPAPI**. Off-machine backup archives must use portable, machine-independent protection so databases can be restored on replacement hardware following catastrophic machine failure.
- **Future Design Selection (Non-Blocking):** Specific archive technology selection (such as passphrase-protected archive tools or encrypted external storage with escrowed keys) represents technical design work for the CBOS 1.3.1+ backup and maintenance expansion and is explicitly **not a blocker for closing this governance and documentation baseline**. Zero claims of eliminating all operational data-loss risk are made.
