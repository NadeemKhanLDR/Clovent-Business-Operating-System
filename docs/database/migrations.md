# CBOS Database Migrations Guide

| Attribute | Details |
| :--- | :--- |
| **Area** | Relational Database & Entity Framework Core |
| **Audience** | Software Developers, DevOps Engineers, Database Administrators |
| **Last Reviewed Version** | CBOS 1.2.2 |
| **Status** | **PROCEDURAL STANDARD** |
| **Classification** | **PUBLIC-SAFE** |

---

## 1. Migration Architecture & Isolation Model

Entity Framework Core migrations in CBOS are strictly decoupled across the six bounded contexts. To maintain independent evolution:
1. Each context maintains its migrations folder in `src/Clovent.<Context>.Infrastructure/Migrations/`.
2. Each context includes a `DesignTimeDbContextFactory` targeting the canonical database `Clovent_BusinessOperatingSystem`.
3. Each context configures a schema-scoped migration table (`[<Schema>].[__EFMigrationsHistory]`), ensuring that running migrations for `Restaurant` never alters or locks `Identity` or `Catalog` migration tables.

---

## 2. Generating New Migrations

When domain entities or EF Core configurations change, generate new migrations using the `dotnet ef migrations add` command, explicitly specifying the target project, startup project, and context:

### Canonical Migration Commands

#### Authentication Context:
```powershell
dotnet ef migrations add <MigrationName> `
    --project src/Clovent.Authentication.Infrastructure/Clovent.Authentication.Infrastructure.csproj `
    --startup-project src/Clovent.Desktop/Clovent.Desktop.csproj `
    --context AuthenticationDbContext `
    --output-dir Persistence/Migrations
```

#### Identity Context:
```powershell
dotnet ef migrations add <MigrationName> `
    --project src/Clovent.Identity.Infrastructure/Clovent.Identity.Infrastructure.csproj `
    --startup-project src/Clovent.Desktop/Clovent.Desktop.csproj `
    --context IdentityDbContext `
    --output-dir Persistence/Migrations
```

#### MasterData Context:
```powershell
dotnet ef migrations add <MigrationName> `
    --project src/Clovent.MasterData.Infrastructure/Clovent.MasterData.Infrastructure.csproj `
    --startup-project src/Clovent.Desktop/Clovent.Desktop.csproj `
    --context MasterDataDbContext `
    --output-dir Persistence/Migrations
```

#### Catalog Context:
```powershell
dotnet ef migrations add <MigrationName> `
    --project src/Clovent.Catalog.Infrastructure/Clovent.Catalog.Infrastructure.csproj `
    --startup-project src/Clovent.Desktop/Clovent.Desktop.csproj `
    --context CatalogDbContext `
    --output-dir Persistence/Migrations
```

#### Inventory Context:
```powershell
dotnet ef migrations add <MigrationName> `
    --project src/Clovent.Inventory.Infrastructure/Clovent.Inventory.Infrastructure.csproj `
    --startup-project src/Clovent.Desktop/Clovent.Desktop.csproj `
    --context InventoryDbContext `
    --output-dir Persistence/Migrations
```

#### Restaurant Context:
```powershell
dotnet ef migrations add <MigrationName> `
    --project src/Clovent.Restaurant.Infrastructure/Clovent.Restaurant.Infrastructure.csproj `
    --startup-project src/Clovent.Desktop/Clovent.Desktop.csproj `
    --context RestaurantDbContext `
    --output-dir Persistence/Migrations
```

---

## 3. Applying Migrations in Environments

### 3.1 Developer Environment
Developers apply pending migrations during local development using:
```powershell
dotnet ef database update --project <InfrastructureProject> --context <TargetDbContext>
```

### 3.2 Production & Customer Deployments
In customer production environments, direct CLI execution of `dotnet ef database update` is **prohibited** because client workstations do not have the .NET SDK installed.

Production migrations are applied exclusively through:
1. **First-Run Commissioning Wizard (`FirstRunWizardForm.cs`):** Interactively applies all initial migrations during workstation setup.
2. **Dedicated Provisioner Tool (`Clovent.Installer.Provisioner.exe`):** Silent, single-file executable packaged with production releases that discovers unapplied migrations and executes them within an administrative transaction.

---

## 4. Migration Rules: What Developers Must Never Do

1. **NEVER modify or drop `__EFMigrationsHistory` tables manually:**
   Altering the schema history causes `DatabaseSchemaCompatibilityValidator` to report corrupt states, immediately halting application startup.
2. **NEVER create cross-schema foreign keys:**
   EF Core fluent configurations must never establish physical relationships pointing to tables in another bounded context's schema.
3. **NEVER drop columns in minor or patch releases:**
   Column removal breaks backward compatibility with active desktop clients. Follow a multi-release deprecation cycle:
   - *Phase 1:* Mark property deprecated, make nullable in schema.
   - *Phase 2:* Stop writing to column.
   - *Phase 3:* Drop column in a major version upgrade.
4. **NEVER embed customer data or default passwords in migration scripts:**
   Migrations must contain schema definitions and static reference tables only. Zero credentials may be seeded in migrations.

---

## 5. Cross References
- [Database Architecture](database-architecture.md)
- [Domain Data Model](domain-data-model.md)
- [Database Upgrade Guide](../deployment/upgrades.md)
- [SQL Server Deployment](../deployment/sql-server.md)
