# Clovent Business Operating System - Database Schema & Upgrade Safety Guide

> [!NOTE]
> **SUPERSEDED HISTORICAL DOCUMENTATION**
>
> This document is superseded by the authoritative guides:
> - Canonical Upgrades Guide: [`docs/deployment/upgrades.md`](deployment/upgrades.md)
> - Database Migrations Architecture: [`docs/database/migrations.md`](database/migrations.md)
> - Database Schema Architecture: [`docs/database/database-architecture.md`](database/database-architecture.md)
> - Schema Mismatch Runbook: [`docs/support/runbooks/schema-mismatch.md`](support/runbooks/schema-mismatch.md)
>
> *Note: The command `Clovent.Desktop.exe --upgrade-database` does NOT exist in CBOS. Upgrades are performed by running the setup installer (e.g. `Clovent.BusinessOperatingSystem-1.3.0-Setup.exe`) or the standalone provisioner `Clovent.Installer.Provisioner.exe --provision`.*

This document defines the schema compatibility, migration lifecycle, and database upgrade procedures in Clovent Business Operating System (CBOS).

---

## 1. Safe Migration Philosophy

Blindly executing automated Entity Framework Core migrations on every normal application launch is hazardous in high-volume, multi-terminal retail/restaurant environments:
- Concurrent terminals launching simultaneously could trigger race conditions during DDL locks.
- An unexpected schema mismatch could lock active transaction tables during peak sales hours.
- A corrupted or partial migration execution could leave databases in an unusable state without a prior backup.

Therefore, CBOS strictly enforces **Controlled Schema Management**:
1. **Commissioning Phase:** Applies initial baseline schema across all 6 bounded contexts during the First-Run Setup Wizard.
2. **Upgrade Phase:** Requires an intentional administrative upgrade command or wizard step, following a mandatory pre-migration database backup.
3. **Routine POS Startup:** Performs **Read-Only Schema Compatibility Validation**. No destructive DDL mutations are ever executed on routine cashier startup.

---

## 2. Schema Compatibility Lifecycle States

During startup, `DatabaseSchemaCompatibilityValidator` checks the migration history tables (`__EFMigrationsHistory`) across all registered DbContexts:

| Compatibility State | Meaning | Runtime Action |
|---|---|---|
| **`Compatible`** | Database schema matches all compiled EF Core migrations across all 6 contexts. | Application boots directly to Sign-In / POS. |
| **`DatabaseTooOld`** | The compiled application includes pending migrations not yet applied to this database. | Startup halts. Prompts administrator to launch the database upgrade wizard or run commissioning migration. Cashier operation blocked. |
| **`DatabaseNewer`** | The database has migrations higher than this application binary recognizes (e.g. workstation running older release). | Startup halts immediately with error: *"Database schema version is newer than this application. Please update the application binary."* Prevents data corruption. |
| **`ConnectionFailed`** | Target SQL Server or database is unreachable, offline, or credentials rejected. | Prompts user to verify network connectivity and open Database Connection Settings. |

---

## 3. Supported DbContext Schemas

All tables reside within a single physical database (`Clovent_BusinessOperatingSystem`) segregated into dedicated bounded-context schemas:

1. **`[Authentication]` (`AuthenticationDbContext`):** User credentials, password history, refresh tokens, active sessions, lockout policies.
2. **`[Identity]` (`IdentityDbContext`):** Users, roles, permissions, role-permission mappings, organizations, companies, branches.
3. **`[MasterData]` (`MasterDataDbContext`):** Departments, fiscal years, currencies, warehouses, terminals, business settings, timezone tables.
4. **`[Catalog]` (`CatalogDbContext`):** Categories, brands, units of measure, products, product variants, barcodes, pricing tiers.
5. **`[Inventory]` (`InventoryDbContext`):** Warehouse stock balances, inventory transactions, adjustments, transfers, serial numbers.
6. **`[Restaurant]` (`RestaurantDbContext`):** Dining areas, tables, orders, order items, kitchen tickets, tender lines, shifts, cash movements, customer receivables, recommendations.

---

## 4. Controlled Upgrade Step-by-Step Procedure

1. **Take Machine Offline / Close Active Shifts:** Ensure all cashier shifts on all terminals are closed.
2. **Execute Timestamped Backup:**
   ```powershell
   BACKUP DATABASE [Clovent_BusinessOperatingSystem] TO DISK = 'C:\ProgramData\Clovent\BusinessOperatingSystem\Backups\Clovent_BusinessOperatingSystem_backup_preupgrade.bak' WITH FORMAT, INIT;
   ```
3. **Execute Upgrade Provisioning:**
   Run the updated setup installer package (which provisions automatically during setup), or execute the standalone provisioner from the release package tools:
   ```powershell
   # Standalone provisioner tool execution:
   .\Clovent.Installer.Provisioner.exe --server "localhost\SQLEXPRESS" --database "Clovent_BusinessOperatingSystem" --auth windows --provision
   ```
   *(Note: The command `Clovent.Desktop.exe --upgrade-database` does not exist).*
4. **Validation:** Review log output in `%ProgramData%\Clovent\BusinessOperatingSystem\Logs\` confirming all 6 contexts reported `All migrations successfully applied`.
5. **Relaunch Terminals:** Launch standard POS terminals. Startup schema check validates `Compatible` and presents Sign-In screen.
