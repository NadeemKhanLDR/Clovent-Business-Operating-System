# CBOS Solution Architecture & Project Structure

| Attribute | Details |
| :--- | :--- |
| **Area** | Solution Layout & Code Organization |
| **Audience** | Software Engineers, Architects, Code Reviewers |
| **Last Reviewed Version** | CBOS 1.2.2 |
| **Status** | **CANONICAL REFERENCE** |
| **Classification** | **PUBLIC-SAFE** |

---

## 1. Solution Root Organization

The CBOS repository is structured using the modern XML-based Visual Studio solution format: `Clovent.BusinessOperatingSystem.slnx`. The repository layout enforces strict separation between domain code, presentation hosts, build utilities, installers, and documentation:

```text
d:\Clovent Business Operating System\
├── .agents/                 # AI coding agent operational guidelines and domain rules
├── docs/                    # Authoritative technical, architectural, and operational documentation
├── installer/               # Inno Setup production installer script (Clovent.BusinessOperatingSystem.iss)
├── src/                     # Production source code organized by bounded context
├── tools/                   # Build security tools (ReleaseGuard) and licensing utilities
├── Clovent.BusinessOperatingSystem.slnx # Canonical solution file
├── README.md                # Master repository overview
├── CHANGELOG.md             # Semantic version history and release notes
└── AGENTS.md                # Single authoritative source of truth for engineering agents
```

---

## 2. Bounded Context Structure (`src/`)

Every business capability follows the 3-project Clean Architecture pattern:
$$\mathbf{src/Clovent.\{Context\}} \quad \longrightarrow \quad \mathbf{src/Clovent.\{Context\}.Application} \quad \longrightarrow \quad \mathbf{src/Clovent.\{Context\}.Infrastructure}$$

| Context / Project | Layer | Target Framework | Purpose & Contained Types |
| :--- | :--- | :---: | :--- |
| **`Clovent.Domain`** | Shared Core | `net10.0` | Shared domain primitives: `AggregateRoot<TId>`, `Entity<TId>`, `ValueObject`, `IDomainEvent`. |
| **`Clovent.Platform`** | Cross-Cutting | `net10.0` | Infrastructure abstractions: `ICircuitBreakerRegistry`, `ApplicationBootstrapper`, logging. |
| **`Clovent.Authentication`** | Domain | `net10.0` | `SessionToken`, `LoginAttempt`, `AuthenticationAuditEvent`. |
| **`Clovent.Authentication.Application`** | Application | `net10.0` | MediatR login commands, token queries, audit handlers. |
| **`Clovent.Authentication.Infrastructure`** | Persistence | `net10.0` | `AuthenticationDbContext`, entity configurations, migrations. |
| **`Clovent.Identity`** | Domain | `net10.0` | `User`, `Role`, `Permission`, `Organization`, `Company`, `Branch`. |
| **`Clovent.Identity.Application`** | Application | `net10.0` | User creation, role assignment, `IPermissionCache` abstraction. |
| **`Clovent.Identity.Infrastructure`** | Persistence | `net10.0` | `IdentityDbContext`, `MemoryPermissionCache`, migrations. |
| **`Clovent.MasterData`** | Domain | `net10.0` | `Terminal`, `Warehouse`, `Currency`, `UnitOfMeasure`, `NumberSequence`. |
| **`Clovent.MasterData.Application`** | Application | `net10.0` | Terminal commands, currency queries, sequence generators. |
| **`Clovent.MasterData.Infrastructure`** | Persistence | `net10.0` | `MasterDataDbContext`, migrations. |
| **`Clovent.Catalog`** | Domain | `net10.0` | `Category`, `Product`, `ProductVariant`, `Barcode`, `PricingTier`. |
| **`Clovent.Catalog.Application`** | Application | `net10.0` | Variant management, barcode scanning queries, price lookups. |
| **`Clovent.Catalog.Infrastructure`** | Persistence | `net10.0` | `CatalogDbContext`, migrations. |
| **`Clovent.Inventory`** | Domain | `net10.0` | `WarehouseStock`, `InventoryTransaction`, `InventoryAdjustment`. |
| **`Clovent.Inventory.Application`** | Application | `net10.0` | Stock receipt, issue, transfer, and adjustment commands. |
| **`Clovent.Inventory.Infrastructure`** | Persistence | `net10.0` | `InventoryDbContext`, migrations. |
| **`Clovent.Restaurant`** | Domain | `net10.0` | `Order`, `OrderLine`, `Payment`, `Shift`, `CashMovement`, `Customer`, `DiningArea`, `Table`, `OutboxMessage`. |
| **`Clovent.Restaurant.Application`** | Application | `net10.0` | Checkout commands, shift reconciliation, Outbox handlers. |
| **`Clovent.Restaurant.Infrastructure`** | Persistence | `net10.0` | `RestaurantDbContext`, `OutboxProcessor`, `ProtectedOperationalCacheStore`, `ProtectedContinuityJournalStore`. |
| **`Clovent.Desktop`** | Presentation | `net10.0-windows` | WinForms UI, DevExpress ribbon, `RestaurantPosForm`, `FirstRunWizardForm`, composition root (`Program.cs`). |

---

## 3. Test Suites Structure

Each bounded context maintains matching unit, application, and infrastructure test suites:
- `Clovent.<Context>.Tests`: Fast, in-memory domain aggregate tests.
- `Clovent.<Context>.Application.Tests`: MediatR handler pipeline and business rule tests.
- `Clovent.<Context>.Infrastructure.Tests`: SQLite in-memory integration tests and Outbox atomicity verification.
- `Clovent.Desktop.Tests`: UI formatting, layout docking, High-DPI typography, and workstation test isolation tests (`WorkstationStartupVerificationTests`).

---

## 4. Build Tools & Packaging (`tools/` & `installer/`)

- **`tools/ReleaseGuard/ScanReleasePackage.ps1`**: Automated static security scanner for release packages.
- **`tools/LicenseIssuer/`**: Offline vendor CLI tool for generating RSA-2048 signed customer licenses.
- **`installer/Clovent.BusinessOperatingSystem.iss`**: Inno Setup 6 script compiling the production Windows setup wizard.

---

## 5. Cross References
- [Environment Setup](environment-setup.md)
- [Build Guide](build.md)
- [Coding Guidelines](coding-guidelines.md)
- [System Architecture](../architecture/system-architecture.md)
