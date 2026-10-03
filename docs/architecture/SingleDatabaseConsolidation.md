# Single Database Consolidation Architecture

## Overview
The Clovent Business Operating System has been consolidated from separate physical SQL Server databases into a single physical database: `Clovent_BusinessOperatingSystem`.

## Bounded-Context Isolation via SQL Schemas
While physically residing in one database, bounded-context domain isolation is strictly maintained through dedicated SQL schemas:
1. `[Authentication]` - Authentication events, login attempts, user sessions
2. `[Identity]` - Users, roles, permissions, user-role assignments
3. `[MasterData]` - Branches, companies, warehouses, terminals, sequences
4. `[Catalog]` - Categories, groups, brands, units of measure, products, variants, barcodes, prices
5. `[Inventory]` - Warehouse stock, inventory transactions, stock adjustments, stock transfers
6. `[Restaurant]` - Dining areas, tables, orders, order lines, kitchen tickets, payments, payment methods, discounts, service charges, sequences, customers, customer ledger, shifts, cash movements, smart recommendations, templates, day close, attendance sessions

## Isolated EF Core Migrations History
Each bounded context manages its own migrations lifecycle in an isolated schema-specific migration history table:
- `[Authentication].[__EFMigrationsHistory]`
- `[Identity].[__EFMigrationsHistory]`
- `[MasterData].[__EFMigrationsHistory]`
- `[Catalog].[__EFMigrationsHistory]`
- `[Inventory].[__EFMigrationsHistory]`
- `[Restaurant].[__EFMigrationsHistory]`

## Canonical Connection String
All application components and modules resolve a single canonical connection string from `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "Default": "Server=.;Database=Clovent_BusinessOperatingSystem;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```
All module persistence registrations (`AddPersistence`) look for `"Default"` first, falling back to module-specific keys if present for backward compatibility.

## Design-Time DbContext Factories
All design-time factories across all bounded contexts:
- `AuthenticationDbContextFactory`
- `IdentityDbContextFactory`
- `MasterDataDbContextFactory`
- `CatalogDbContextFactory`
- `InventoryDbContextFactory`
- `RestaurantDbContextFactory`

are configured to use `Clovent_BusinessOperatingSystem` as the default target database with their respective schema-scoped migration history tables. No context uses a context-specific or design-time-only physical database.

## Legacy Database Cleanup & Elimination

### 1. Clovent.Identity
- **Origin / Root Cause**: Created on `2026-07-26 21:07:39` during Milestone 1 initial Identity setup before bounded-context schema standards and `Clovent_Identity` naming conventions were established.
- **Size**: 16.00 MB.
- **Data Audit**: Contained 0 rows in all business tables (`dbo.Branches`, `dbo.Companies`, `dbo.Permissions`, `dbo.RolePermissions`, `dbo.Roles`, `dbo.UserRoles`, `dbo.Users`).
- **Active Data Location**: All active Identity records (Organizations, Companies, Branches, 214 Permissions, Roles, Users) reside in `Clovent_BusinessOperatingSystem.[Identity]`.
- **Disposition**: Compressed backup verified at `DatabaseBackups\2026-10-01\Clovent.Identity.bak` via `RESTORE VERIFYONLY` and database dropped.

### 2. Clovent_Restaurant_DesignTime
- **Origin / Root Cause**: Created on `2026-08-09 08:22:55` as a fallback database name in `RestaurantDbContextFactory.cs` for EF Core design-time CLI migrations.
- **Size**: 16.00 MB.
- **Data Audit**: Contained 0 rows across all 14 restaurant tables.
- **Disposition**: `RestaurantDbContextFactory.cs` was updated to target `Clovent_BusinessOperatingSystem` with `Restaurant.__EFMigrationsHistory`. Compressed backup verified at `DatabaseBackups\2026-10-01\Clovent_Restaurant_DesignTime.bak` via `RESTORE VERIFYONLY` and database dropped.

### 3. EF Core Tooling Verification
- Executed `dotnet ef migrations list` for all infrastructure projects:
  - `Clovent.Authentication.Infrastructure` -> PASSED
  - `Clovent.Identity.Infrastructure` -> PASSED
  - `Clovent.MasterData.Infrastructure` -> PASSED
  - `Clovent.Catalog.Infrastructure` -> PASSED
  - `Clovent.Inventory.Infrastructure` -> PASSED
  - `Clovent.Restaurant.Infrastructure` -> PASSED
- Verified `sys.databases`: `Clovent_Restaurant_DesignTime` and `Clovent.Identity` were **NOT** recreated. Only `Clovent_BusinessOperatingSystem` exists.

## Backup & Verification
Prior to dropping the legacy databases, verified compressed backups were generated at:
`D:\Clovent Business Operating System\DatabaseBackups\2026-10-01\`
- `Clovent.Identity.bak` (Verified via `RESTORE VERIFYONLY`)
- `Clovent_Restaurant_DesignTime.bak` (Verified via `RESTORE VERIFYONLY`)
- Plus all initial 6 bounded context database backups (`Clovent_Authentication.bak`, `Clovent_Identity.bak`, `Clovent_MasterData.bak`, `Clovent_Catalog.bak`, `Clovent_Inventory.bak`, `Clovent_Restaurant.bak`).

The single consolidated database `Clovent_BusinessOperatingSystem` passed `DBCC CHECKDB` with zero allocation and zero consistency errors.
