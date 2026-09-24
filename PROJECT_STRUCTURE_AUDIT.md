# PROJECT STRUCTURE AUDIT — CLOVENT BUSINESS OPERATING SYSTEM

> **AUDIT MODE:** STRICT READ-ONLY
> **AUDIT DATE:** 2026-09-16
> **REPORT FILE:** `PROJECT_STRUCTURE_AUDIT.md` (Solution Root)
> **SAFETY VERIFICATION:** Confirmed that ZERO code files, databases, configurations, dependencies, or folders have been altered, refactored, moved, or deleted.

---

## TABLE OF CONTENTS
1. [Complete Hierarchical Solution / Project Tree](#1-complete-hierarchical-solution--project-tree)
2. [Detailed Project Specifications & Metadata](#2-detailed-project-specifications--metadata)
3. [Comprehensive File Inventory & Metadata](#3-comprehensive-file-inventory--metadata)
4. [Dependency & Reference Map](#4-dependency--reference-map)
5. [Duplicate & Similar File Detection](#5-duplicate--similar-file-detection)
6. [Unused & Possibly Unused Files](#6-unused--possibly-unused-files)
7. [Generated & Build Artifacts](#7-generated--build-artifacts)
8. [Database & SQL Files](#8-database--sql-files)
9. [Restaurant POS Feature Mapping](#9-restaurant-pos-feature-mapping)
10. [Test & QA Files](#10-test--qa-files)
11. [Configuration & Environment Files](#11-configuration--environment-files)
12. [Git & Source Control Inspection](#12-git--source-control-inspection)
13. [System Architecture Overview](#13-system-architecture-overview)
14. [Cleanup Candidates — DO NOT DELETE](#14-cleanup-candidates--do-not-delete)
15. [Final Statistical Summary](#15-final-statistical-summary)

---

## 1. Complete Hierarchical Solution / Project Tree

The following tree presents the complete recursive structural tree of the solution. Every project, directory, source file, configuration, documentation, asset, and script is included. Build and compilation directories are explicitly designated as `[GENERATED/BINARY]`.

```text
Clovent Business Operating System (Root: D:/Clovent Business Operating System/)
│
├── [Solution Files]
│   ├── Clovent.BusinessOperatingSystem.slnx    # Primary Enterprise Solution (43 projects)
│   ├── Tools/Clovent.CLI/Clovent.CLI.slnx     # CLI & Generator Solution (11 projects)
│   └── Tools/Clovent.CLI/Clovent.PackageManager.slnx # Package Manager Solution (4 projects)
│
├── [Root Files & Ad-hoc QA Scripts]
│   ├── .gitignore
│   ├── Clovent Business Operating System.md
│   ├── PROJECT_STRUCTURE_REPORT.md
│   ├── PROJECT_STRUCTURE_AUDIT.md
│   ├── Restaurant_POS_*.md (8 Acceptance and Bug Reports)
│   ├── cart_diagnostics.txt                   # Diagnostic log (2026-08-17)
│   ├── click.ps1, enum.ps1, fg.ps1, find.ps1, login.ps1, pclick.ps1, shoot.ps1, type.ps1, uia.ps1, uia_dump.ps1, uia_set.ps1
│   ├── qauia_pos.txt, qauia_pos_tree.txt, qauia_tree.txt, qawin32_pos.txt
│   └── shot_login.png, shot_login2.png, test_screen.png
│
├── [Architecture & Specifications Documentation]
│   ├── 00 Vision/ (7 files: Vision, Scope, Principles)
│   ├── 01 Product Strategy/ (5 files: Market, OKRs, Personas)
│   ├── 02 Business Analysis/ (Business analysis models)
│   ├── 03 SDLC/ (Development lifecycle rules)
│   ├── 04 UI UX Standards/ (Design system, tokens, typography)
│   ├── 05 Software Architecture/ (Clean architecture & DDD specs)
│   ├── 06 Coding Standards/ (C# / .NET conventions)
│   ├── 07 Domain Driven Design/ (Tactical DDD patterns)
│   ├── 08 Database Design/ (Persistence & schema specs)
│   ├── 09 Security/ (Threat model, RBAC, PIN security)
│   ├── 10 AI Architecture/ (AI assistant integrations)
│   ├── 11 Platform Services/ (Shared infrastructure specs)
│   ├── 12 Restaurant POS/ (POS operational workflows)
│   ├── 13 ADR/ (Architecture Decision Records)
│   ├── docs/ (34 files: test plans, reports, feature checklists)
│   └── Foundation/ (11 files: architectural foundation docs)
│
├── [Assets & Images]
│   ├── Assets/ (Design assets)
│   ├── Diagrams/ (System diagrams)
│   └── Images/ (26 screenshot assets)
│
├── [Backups Archive (Out-of-tree)]
│   └── backups/
│       ├── config-security-fix-2026-08-17/ (appsettings.json.bak, csproj backups)
│       ├── logging-2026-08-17/ (appsettings.json.bak, Program.cs.bak)
│       └── rootcause-2026-08-18/ (POS form & logger backups)
│
├── [QA Automation Suite]
│   └── qa/
│       ├── v3_*.sql (6 direct SQLite inspection scripts)
│       ├── live_ui/
│       │   ├── ClickTool.cs (Native input simulator)
│       │   ├── ClickTool.exe [GENERATED/BINARY]
│       │   ├── *.ps1 (15 interactive UI automation scripts: flow, drv, login2, btns, cap, etc.)
│       │   └── *.png (25+ live verification screenshots)
│       └── (400+ acceptance screenshots and UIA tree dumps)
│
├── [Scratch & Sandboxes]
│   └── scratch/
│       ├── dialog_crop.png, dialog_exact.png, menu_inspection.txt
│       └── DesignerTest/ (Standalone sandbox project)
│           ├── DesignerTest.csproj
│           ├── DesignerTest.csproj.user
│           ├── Form1.cs, Form1.Designer.cs, Program.cs
│           ├── bin/ [GENERATED/BINARY]
│           └── obj/ [GENERATED/BINARY]
│
├── [Tools & CLI Scaffolding]
│   └── Tools/Clovent.CLI/
│       ├── Clovent.CLI.slnx
│       ├── Clovent.PackageManager.slnx
│       ├── manifest.json, installed.json
│       ├── Clovent.CLI/ (CLI Console Executable)
│       │   ├── Clovent.CLI.csproj
│       │   ├── Program.cs
│       │   ├── Configuration/, Extensions/, Infrastructure/, Services/, Templates/
│       │   ├── bin/ [GENERATED/BINARY]
│       │   └── obj/ [GENERATED/BINARY]
│       ├── Clovent.CBOS.Desktop/ (Prototype WinForms Host)
│       │   ├── Clovent.CBOS.Desktop.csproj
│       │   ├── Configuration/, Forms/, Infrastructure/, Services/, Session/
│       │   ├── bin/ [GENERATED/BINARY]
│       │   └── obj/ [GENERATED/BINARY]
│       ├── Clovent.PackageManager/ (CLI Host)
│       ├── Clovent.PackageManager.Abstractions/ (Class Library)
│       ├── Clovent.PackageManager.Core/ (Class Library)
│       ├── Clovent.PackageManager.Tests/ (Test Library)
│       ├── src/ (Shared generator modules: Configuration, Core, Documents, Generator, Modules.Identity, Shared, Templates)
│       │   └── (Each project contains .csproj, source files, bin/ [GENERATED/BINARY], obj/ [GENERATED/BINARY])
│       └── tests/ (Clovent.Core.Tests, Clovent.Generator.Tests)
│
└── [Main Source Tree: src/]
    ├── Clovent.Authentication.Application.Tests/
    │   ├── Clovent.Authentication.Application.Tests.csproj
    │   ├── Credentials/
    │   ├── DependencyInjection/
    │   ├── LoginAttempts/
    │   ├── RefreshSessions/
    │   ├── Sessions/
    │   ├── TestSupport/
    │   ├── bin/ [GENERATED/BINARY]
    │   └── obj/ [GENERATED/BINARY]
    ├── Clovent.Authentication.Application/
    │   ├── Clovent.Authentication.Application.csproj
    │   ├── Credentials/
    │   ├── DependencyInjection/
    │   ├── LoginAttempts/
    │   ├── RefreshSessions/
    │   ├── Sessions/
    │   ├── bin/ [GENERATED/BINARY]
    │   └── obj/ [GENERATED/BINARY]
    ├── Clovent.Authentication.Infrastructure.Tests/
    │   ├── Clovent.Authentication.Infrastructure.Tests.csproj
    │   ├── Persistence/
    │   ├── Repositories/
    │   ├── Security/
    │   ├── TestSupport/
    │   ├── bin/ [GENERATED/BINARY]
    │   └── obj/ [GENERATED/BINARY]
    ├── Clovent.Authentication.Infrastructure/
    │   ├── Clovent.Authentication.Infrastructure.csproj
    │   ├── DependencyInjection/
    │   ├── Persistence/
    │   ├── Repositories/
    │   ├── Security/
    │   ├── bin/ [GENERATED/BINARY]
    │   └── obj/ [GENERATED/BINARY]
    ├── Clovent.Authentication.Tests/
    │   ├── Clovent.Authentication.Tests.csproj
    │   ├── Credentials/
    │   ├── Lockouts/
    │   ├── LoginAttempts/
    │   ├── Passwords/
    │   ├── Pins/
    │   ├── RefreshSessions/
    │   ├── Sessions/
    │   ├── Shared/
    │   ├── bin/ [GENERATED/BINARY]
    │   └── obj/ [GENERATED/BINARY]
    ├── Clovent.Authentication/
    │   ├── Clovent.Authentication.csproj
    │   ├── Credentials/
    │   ├── Lockouts/
    │   ├── LoginAttempts/
    │   ├── Passwords/
    │   ├── Pins/
    │   ├── RefreshSessions/
    │   ├── Sessions/
    │   ├── Shared/
    │   ├── bin/ [GENERATED/BINARY]
    │   └── obj/ [GENERATED/BINARY]
    ├── Clovent.Catalog.Application.Tests/
    │   ├── Clovent.Catalog.Application.Tests.csproj
    │   ├── Barcodes/
    │   ├── Brands/
    │   ├── Categories/
    │   ├── Groups/
    │   ├── Prices/
    │   ├── Products/
    │   ├── TestSupport/
    │   ├── UnitsOfMeasure/
    │   ├── Variants/
    │   ├── bin/ [GENERATED/BINARY]
    │   └── obj/ [GENERATED/BINARY]
    ├── Clovent.Catalog.Application/
    │   ├── Clovent.Catalog.Application.csproj
    │   ├── Barcodes/
    │   ├── Brands/
    │   ├── Categories/
    │   ├── DependencyInjection/
    │   ├── Groups/
    │   ├── Prices/
    │   ├── Products/
    │   ├── UnitsOfMeasure/
    │   ├── Variants/
    │   ├── bin/ [GENERATED/BINARY]
    │   └── obj/ [GENERATED/BINARY]
    ├── Clovent.Catalog.Infrastructure.Tests/
    │   ├── Clovent.Catalog.Infrastructure.Tests.csproj
    │   ├── Persistence/
    │   ├── Repositories/
    │   ├── TestSupport/
    │   ├── bin/ [GENERATED/BINARY]
    │   └── obj/ [GENERATED/BINARY]
    ├── Clovent.Catalog.Infrastructure/
    │   ├── Clovent.Catalog.Infrastructure.csproj
    │   ├── DependencyInjection/
    │   ├── Migrations/
    │   ├── Persistence/
    │   ├── Repositories/
    │   ├── bin/ [GENERATED/BINARY]
    │   └── obj/ [GENERATED/BINARY]
    ├── Clovent.Catalog.Tests/
    │   ├── Clovent.Catalog.Tests.csproj
    │   ├── Barcodes/
    │   ├── Brands/
    │   ├── Categories/
    │   ├── Groups/
    │   ├── Prices/
    │   ├── Products/
    │   ├── Shared/
    │   ├── UnitsOfMeasure/
    │   ├── Variants/
    │   ├── bin/ [GENERATED/BINARY]
    │   └── obj/ [GENERATED/BINARY]
    ├── Clovent.Catalog/
    │   ├── Clovent.Catalog.csproj
    │   ├── Barcodes/
    │   ├── Brands/
    │   ├── Categories/
    │   ├── Groups/
    │   ├── Prices/
    │   ├── Products/
    │   ├── Shared/
    │   ├── UnitsOfMeasure/
    │   ├── Variants/
    │   ├── bin/ [GENERATED/BINARY]
    │   └── obj/ [GENERATED/BINARY]
    ├── Clovent.Desktop.Tests/
    │   ├── Clovent.Desktop.Tests.csproj
    │   ├── Authorization/
    │   ├── Dashboard/
    │   ├── Forms/
    │   ├── Login/
    │   ├── MasterData/
    │   ├── Modules/
    │   ├── Navigation/
    │   ├── Notifications/
    │   ├── Restaurant/
    │   ├── Sessions/
    │   ├── Shared/
    │   ├── Shell/
    │   ├── Startup/
    │   ├── TestSupport/
    │   ├── UI/
    │   ├── bin/ [GENERATED/BINARY]
    │   └── obj/ [GENERATED/BINARY]
    ├── Clovent.Desktop.UiQa/
    │   ├── Clovent.Desktop.UiQa.csproj
    │   ├── bin/ [GENERATED/BINARY]
    │   └── obj/ [GENERATED/BINARY]
    ├── Clovent.Desktop/
    │   ├── Clovent.Desktop.csproj
    │   ├── Authorization/
    │   ├── Catalog/
    │   ├── Composition/
    │   ├── Dashboard/
    │   ├── DependencyInjection/
    │   ├── Forms/
    │   ├── Identity/
    │   ├── Inventory/
    │   ├── Login/
    │   ├── MasterData/
    │   ├── Modules/
    │   ├── Navigation/
    │   ├── Notifications/
    │   ├── Properties/
    │   ├── Restaurant/
    │   ├── Seed/
    │   ├── Sessions/
    │   ├── Shared/
    │   ├── Shell/
    │   ├── Startup/
    │   ├── Theming/
    │   ├── bin/ [GENERATED/BINARY]
    │   └── obj/ [GENERATED/BINARY]
    ├── Clovent.Domain.Tests/
    │   ├── Clovent.Domain.Tests.csproj
    │   ├── TestSupport/
    │   ├── bin/ [GENERATED/BINARY]
    │   └── obj/ [GENERATED/BINARY]
    ├── Clovent.Domain/
    │   ├── Clovent.Domain.csproj
    │   ├── bin/ [GENERATED/BINARY]
    │   └── obj/ [GENERATED/BINARY]
    ├── Clovent.Identity.Application.Tests/
    │   ├── Clovent.Identity.Application.Tests.csproj
    │   ├── Authorization/
    │   ├── Branches/
    │   ├── Companies/
    │   ├── Organizations/
    │   ├── Permissions/
    │   ├── Roles/
    │   ├── TestSupport/
    │   ├── Users/
    │   ├── bin/ [GENERATED/BINARY]
    │   └── obj/ [GENERATED/BINARY]
    ├── Clovent.Identity.Application/
    │   ├── Clovent.Identity.Application.csproj
    │   ├── Authorization/
    │   ├── Branches/
    │   ├── Companies/
    │   ├── DependencyInjection/
    │   ├── Organizations/
    │   ├── Permissions/
    │   ├── Roles/
    │   ├── Users/
    │   ├── bin/ [GENERATED/BINARY]
    │   └── obj/ [GENERATED/BINARY]
    ├── Clovent.Identity.Infrastructure.Tests/
    │   ├── Clovent.Identity.Infrastructure.Tests.csproj
    │   ├── Caching/
    │   ├── Persistence/
    │   ├── Repositories/
    │   ├── TestSupport/
    │   ├── bin/ [GENERATED/BINARY]
    │   └── obj/ [GENERATED/BINARY]
    ├── Clovent.Identity.Infrastructure/
    │   ├── Clovent.Identity.Infrastructure.csproj
    │   ├── Caching/
    │   ├── DependencyInjection/
    │   ├── Persistence/
    │   ├── Repositories/
    │   ├── bin/ [GENERATED/BINARY]
    │   └── obj/ [GENERATED/BINARY]
    ├── Clovent.Identity.Tests/
    │   ├── Clovent.Identity.Tests.csproj
    │   ├── Branches/
    │   ├── Companies/
    │   ├── Organizations/
    │   ├── Permissions/
    │   ├── Roles/
    │   ├── Shared/
    │   ├── Users/
    │   ├── bin/ [GENERATED/BINARY]
    │   └── obj/ [GENERATED/BINARY]
    ├── Clovent.Identity/
    │   ├── Clovent.Identity.csproj
    │   ├── Branches/
    │   ├── Companies/
    │   ├── Organizations/
    │   ├── Permissions/
    │   ├── Roles/
    │   ├── Shared/
    │   ├── Users/
    │   ├── bin/ [GENERATED/BINARY]
    │   └── obj/ [GENERATED/BINARY]
    ├── Clovent.Inventory.Application.Tests/
    │   ├── Clovent.Inventory.Application.Tests.csproj
    │   ├── Adjustments/
    │   ├── TestSupport/
    │   ├── Transactions/
    │   ├── Transfers/
    │   ├── WarehouseStocks/
    │   ├── bin/ [GENERATED/BINARY]
    │   └── obj/ [GENERATED/BINARY]
    ├── Clovent.Inventory.Application/
    │   ├── Clovent.Inventory.Application.csproj
    │   ├── Adjustments/
    │   ├── DependencyInjection/
    │   ├── Transactions/
    │   ├── Transfers/
    │   ├── WarehouseStocks/
    │   ├── bin/ [GENERATED/BINARY]
    │   └── obj/ [GENERATED/BINARY]
    ├── Clovent.Inventory.Infrastructure.Tests/
    │   ├── Clovent.Inventory.Infrastructure.Tests.csproj
    │   ├── Persistence/
    │   ├── Repositories/
    │   ├── TestSupport/
    │   ├── bin/ [GENERATED/BINARY]
    │   └── obj/ [GENERATED/BINARY]
    ├── Clovent.Inventory.Infrastructure/
    │   ├── Clovent.Inventory.Infrastructure.csproj
    │   ├── DependencyInjection/
    │   ├── Migrations/
    │   ├── Persistence/
    │   ├── Repositories/
    │   ├── bin/ [GENERATED/BINARY]
    │   └── obj/ [GENERATED/BINARY]
    ├── Clovent.Inventory.Tests/
    │   ├── Clovent.Inventory.Tests.csproj
    │   ├── Adjustments/
    │   ├── Transactions/
    │   ├── Transfers/
    │   ├── WarehouseStocks/
    │   ├── bin/ [GENERATED/BINARY]
    │   └── obj/ [GENERATED/BINARY]
    ├── Clovent.Inventory/
    │   ├── Clovent.Inventory.csproj
    │   ├── Adjustments/
    │   ├── Transactions/
    │   ├── Transfers/
    │   ├── WarehouseStocks/
    │   ├── bin/ [GENERATED/BINARY]
    │   └── obj/ [GENERATED/BINARY]
    ├── Clovent.MasterData.Application.Tests/
    │   ├── Clovent.MasterData.Application.Tests.csproj
    │   ├── Currencies/
    │   ├── Departments/
    │   ├── FiscalYears/
    │   ├── Languages/
    │   ├── Settings/
    │   ├── Terminals/
    │   ├── TestSupport/
    │   ├── TimeZones/
    │   ├── Warehouses/
    │   ├── bin/ [GENERATED/BINARY]
    │   └── obj/ [GENERATED/BINARY]
    ├── Clovent.MasterData.Application/
    │   ├── Clovent.MasterData.Application.csproj
    │   ├── Currencies/
    │   ├── Departments/
    │   ├── DependencyInjection/
    │   ├── FiscalYears/
    │   ├── Languages/
    │   ├── Settings/
    │   ├── Terminals/
    │   ├── TimeZones/
    │   ├── Warehouses/
    │   ├── bin/ [GENERATED/BINARY]
    │   └── obj/ [GENERATED/BINARY]
    ├── Clovent.MasterData.Infrastructure.Tests/
    │   ├── Clovent.MasterData.Infrastructure.Tests.csproj
    │   ├── Persistence/
    │   ├── Repositories/
    │   ├── TestSupport/
    │   ├── bin/ [GENERATED/BINARY]
    │   └── obj/ [GENERATED/BINARY]
    ├── Clovent.MasterData.Infrastructure/
    │   ├── Clovent.MasterData.Infrastructure.csproj
    │   ├── DependencyInjection/
    │   ├── Migrations/
    │   ├── Persistence/
    │   ├── Repositories/
    │   ├── bin/ [GENERATED/BINARY]
    │   └── obj/ [GENERATED/BINARY]
    ├── Clovent.MasterData.Tests/
    │   ├── Clovent.MasterData.Tests.csproj
    │   ├── Currencies/
    │   ├── Departments/
    │   ├── FiscalYears/
    │   ├── Languages/
    │   ├── Settings/
    │   ├── Shared/
    │   ├── Terminals/
    │   ├── TimeZones/
    │   ├── Warehouses/
    │   ├── bin/ [GENERATED/BINARY]
    │   └── obj/ [GENERATED/BINARY]
    ├── Clovent.MasterData/
    │   ├── Clovent.MasterData.csproj
    │   ├── Currencies/
    │   ├── Departments/
    │   ├── FiscalYears/
    │   ├── Languages/
    │   ├── Settings/
    │   ├── Shared/
    │   ├── Terminals/
    │   ├── TimeZones/
    │   ├── Warehouses/
    │   ├── bin/ [GENERATED/BINARY]
    │   └── obj/ [GENERATED/BINARY]
    ├── Clovent.Platform.Tests/
    │   ├── Clovent.Platform.Tests.csproj
    │   ├── Bootstrap/
    │   ├── Configuration/
    │   ├── Execution/
    │   ├── Logging/
    │   ├── Modules/
    │   ├── TestResults/
    │   ├── TestSupport/
    │   ├── bin/ [GENERATED/BINARY]
    │   └── obj/ [GENERATED/BINARY]
    ├── Clovent.Platform/
    │   ├── Clovent.Platform.csproj
    │   ├── Bootstrap/
    │   ├── Configuration/
    │   ├── DependencyInjection/
    │   ├── Execution/
    │   ├── Logging/
    │   ├── Modules/
    │   ├── bin/ [GENERATED/BINARY]
    │   └── obj/ [GENERATED/BINARY]
    ├── Clovent.Restaurant.Application.Tests/
    │   ├── Clovent.Restaurant.Application.Tests.csproj
    │   ├── CustomerReorder/
    │   ├── Customers/
    │   ├── DiningAreas/
    │   ├── Discounts/
    │   ├── EndOfDay/
    │   ├── KitchenTickets/
    │   ├── OrderHealth/
    │   ├── OrderLines/
    │   ├── Orders/
    │   ├── PaymentMethods/
    │   ├── Payments/
    │   ├── QuickOrderTemplates/
    │   ├── RestaurantPulse/
    │   ├── ServiceCharges/
    │   ├── Shifts/
    │   ├── SmartRecommendations/
    │   ├── Tables/
    │   ├── TestSupport/
    │   ├── UniversalPosSearch/
    │   ├── bin/ [GENERATED/BINARY]
    │   └── obj/ [GENERATED/BINARY]
    ├── Clovent.Restaurant.Application/
    │   ├── Clovent.Restaurant.Application.csproj
    │   ├── ActivityLogs/
    │   ├── CustomerReorder/
    │   ├── Customers/
    │   ├── DependencyInjection/
    │   ├── DiningAreas/
    │   ├── Discounts/
    │   ├── EndOfDay/
    │   ├── KitchenTickets/
    │   ├── OrderHealth/
    │   ├── OrderLines/
    │   ├── Orders/
    │   ├── PaymentMethods/
    │   ├── Payments/
    │   ├── QuickOrderTemplates/
    │   ├── RestaurantPulse/
    │   ├── ServiceCharges/
    │   ├── Shifts/
    │   ├── SmartRecommendations/
    │   ├── Tables/
    │   ├── UniversalPosSearch/
    │   ├── bin/ [GENERATED/BINARY]
    │   └── obj/ [GENERATED/BINARY]
    ├── Clovent.Restaurant.Infrastructure.Tests/
    │   ├── Clovent.Restaurant.Infrastructure.Tests.csproj
    │   ├── Integration/
    │   ├── Persistence/
    │   ├── Repositories/
    │   ├── TestSupport/
    │   ├── bin/ [GENERATED/BINARY]
    │   └── obj/ [GENERATED/BINARY]
    ├── Clovent.Restaurant.Infrastructure/
    │   ├── Clovent.Restaurant.Infrastructure.csproj
    │   ├── DependencyInjection/
    │   ├── Migrations/
    │   ├── Persistence/
    │   ├── Repositories/
    │   ├── bin/ [GENERATED/BINARY]
    │   └── obj/ [GENERATED/BINARY]
    ├── Clovent.Restaurant.Tests/
    │   ├── Clovent.Restaurant.Tests.csproj
    │   ├── ActivityLogs/
    │   ├── Customers/
    │   ├── DiningAreas/
    │   ├── Discounts/
    │   ├── KitchenTickets/
    │   ├── OrderLines/
    │   ├── Orders/
    │   ├── PaymentMethods/
    │   ├── Payments/
    │   ├── ServiceCharges/
    │   ├── Shifts/
    │   ├── Tables/
    │   ├── bin/ [GENERATED/BINARY]
    │   └── obj/ [GENERATED/BINARY]
    ├── Clovent.Restaurant/
    │   ├── Clovent.Restaurant.csproj
    │   ├── ActivityLogs/
    │   ├── Customers/
    │   ├── DiningAreas/
    │   ├── Discounts/
    │   ├── KitchenTickets/
    │   ├── OrderLines/
    │   ├── Orders/
    │   ├── PaymentMethods/
    │   ├── Payments/
    │   ├── QuickOrderTemplates/
    │   ├── Sales/
    │   ├── ServiceCharges/
    │   ├── Shared/
    │   ├── Shifts/
    │   ├── SmartRecommendations/
    │   ├── Tables/
    │   ├── bin/ [GENERATED/BINARY]
    │   └── obj/ [GENERATED/BINARY]
```

---

## 2. Detailed Project Specifications & Metadata

A total of **59 projects** (`.csproj`) were discovered across the workspace:

| Project Name | Relative Path | Target Framework | Output Type | WinForms | Layer / App Type | References | Packages |
|---|---|---|---|---|---|---|---|
| **Clovent.CBOS.Desktop** | `Tools/Clovent.CLI/Clovent.CBOS.Desktop/Clovent.CBOS.Desktop.csproj` | `net10.0-windows` | `WinExe` | Yes | Class Library | *None* | Microsoft.EntityFrameworkCore.Design `10.0.10`<br/>Microsoft.EntityFrameworkCore.SqlServer `10.0.10`<br/>Microsoft.Extensions.Configuration `10.0.10`<br/>*+4 more* |
| **Clovent.CLI** | `Tools/Clovent.CLI/Clovent.CLI/Clovent.CLI.csproj` | `net10.0` | `Exe` | No | CLI Utility | Clovent.Core<br/>Clovent.Generator<br/>Clovent.Documents<br/>Clovent.Configuration<br/>Clovent.Shared | Microsoft.Extensions.Configuration `10.0.10`<br/>Microsoft.Extensions.Configuration.Json `10.0.10`<br/>Microsoft.Extensions.DependencyInjection `10.0.10`<br/>*+5 more* |
| **Clovent.PackageManager.Abstractions** | `Tools/Clovent.CLI/Clovent.PackageManager.Abstractions/Clovent.PackageManager.Abstractions.csproj` | `net10.0` | `Library` | No | Class Library | *None* | *None* |
| **Clovent.PackageManager.Core** | `Tools/Clovent.CLI/Clovent.PackageManager.Core/Clovent.PackageManager.Core.csproj` | `net10.0` | `Library` | No | Class Library | Clovent.PackageManager.Abstractions | *None* |
| **Clovent.PackageManager.Tests** | `Tools/Clovent.CLI/Clovent.PackageManager.Tests/Clovent.PackageManager.Tests.csproj` | `net10.0` | `Library` | No | Test Project | *None* | coverlet.collector `6.0.4`<br/>Microsoft.NET.Test.Sdk `17.14.1`<br/>xunit `2.9.3`<br/>*+1 more* |
| **Clovent.PackageManager** | `Tools/Clovent.CLI/Clovent.PackageManager/Clovent.PackageManager.csproj` | `net10.0` | `Exe` | No | Class Library | Clovent.PackageManager.Core<br/>Clovent.PackageManager.Abstractions | *None* |
| **Clovent.Configuration** | `Tools/Clovent.CLI/src/Clovent.Configuration/Clovent.Configuration.csproj` | `net10.0` | `Library` | No | Class Library | *None* | *None* |
| **Clovent.Core** | `Tools/Clovent.CLI/src/Clovent.Core/Clovent.Core.csproj` | `net10.0` | `Library` | No | Class Library | *None* | *None* |
| **Clovent.Documents** | `Tools/Clovent.CLI/src/Clovent.Documents/Clovent.Documents.csproj` | `net10.0` | `Library` | No | Class Library | Clovent.Core | *None* |
| **Clovent.Generator** | `Tools/Clovent.CLI/src/Clovent.Generator/Clovent.Generator.csproj` | `net10.0` | `Library` | No | Class Library | Clovent.Core<br/>Clovent.Documents<br/>Clovent.Templates<br/>Clovent.Configuration<br/>Clovent.Shared | *None* |
| **Clovent.Modules.Identity** | `Tools/Clovent.CLI/src/Clovent.Modules.Identity/Clovent.Modules.Identity.csproj` | `net10.0` | `Library` | No | Class Library | Clovent.Core<br/>Clovent.Shared | BCrypt.Net-Next `4.2.0`<br/>MediatR `12.4.1`<br/>Microsoft.EntityFrameworkCore `10.0.10`<br/>*+6 more* |
| **Clovent.Shared** | `Tools/Clovent.CLI/src/Clovent.Shared/Clovent.Shared.csproj` | `net10.0` | `Library` | No | Class Library | *None* | MediatR.Contracts `2.0.1` |
| **Clovent.Templates** | `Tools/Clovent.CLI/src/Clovent.Templates/Clovent.Templates.csproj` | `net10.0` | `Library` | No | Class Library | Clovent.Core | *None* |
| **Clovent.Core.Tests** | `Tools/Clovent.CLI/tests/Clovent.Core.Tests/Clovent.Core.Tests.csproj` | `net10.0` | `Library` | No | Test Project | Clovent.Core | coverlet.collector `6.0.4`<br/>Microsoft.NET.Test.Sdk `17.14.1`<br/>xunit `2.9.3`<br/>*+1 more* |
| **Clovent.Generator.Tests** | `Tools/Clovent.CLI/tests/Clovent.Generator.Tests/Clovent.Generator.Tests.csproj` | `net10.0` | `Library` | No | Test Project | Clovent.Generator<br/>Clovent.Core<br/>Clovent.Templates<br/>Clovent.Documents | Microsoft.NET.Test.Sdk `17.13.0`<br/>xunit `2.9.3`<br/>xunit.runner.visualstudio `3.0.2` |
| **DesignerTest** | `scratch/DesignerTest/DesignerTest.csproj` | `net10.0-windows` | `WinExe` | Yes | Class Library | Clovent.Desktop | *None* |
| **Clovent.Authentication.Application.Tests** | `src/Clovent.Authentication.Application.Tests/Clovent.Authentication.Application.Tests.csproj` | `net10.0` | `Library` | No | Test Project | Clovent.Authentication.Application | Microsoft.NET.Test.Sdk `17.14.1`<br/>Microsoft.Extensions.Configuration `10.0.10`<br/>Microsoft.Extensions.DependencyInjection `10.0.10`<br/>*+3 more* |
| **Clovent.Authentication.Application** | `src/Clovent.Authentication.Application/Clovent.Authentication.Application.csproj` | `net10.0` | `Library` | No | Application / CQRS Layer | Clovent.Authentication<br/>Clovent.Identity | MediatR `12.4.1`<br/>Microsoft.Extensions.Configuration.Abstractions `10.0.10`<br/>Microsoft.Extensions.DependencyInjection.Abstractions `10.0.10` |
| **Clovent.Authentication.Infrastructure.Tests** | `src/Clovent.Authentication.Infrastructure.Tests/Clovent.Authentication.Infrastructure.Tests.csproj` | `net10.0` | `Library` | No | Test Project | Clovent.Authentication.Infrastructure | Microsoft.NET.Test.Sdk `17.14.1`<br/>xunit `2.9.3`<br/>xunit.runner.visualstudio `3.1.4`<br/>*+4 more* |
| **Clovent.Authentication.Infrastructure** | `src/Clovent.Authentication.Infrastructure/Clovent.Authentication.Infrastructure.csproj` | `net10.0` | `Library` | No | Database / Persistence Layer | Clovent.Authentication<br/>Clovent.Authentication.Application<br/>Clovent.Identity<br/>Clovent.Platform | Microsoft.EntityFrameworkCore `10.0.10`<br/>Microsoft.EntityFrameworkCore.SqlServer `10.0.10`<br/>Microsoft.EntityFrameworkCore.Design `10.0.10`<br/>*+3 more* |
| **Clovent.Authentication.Tests** | `src/Clovent.Authentication.Tests/Clovent.Authentication.Tests.csproj` | `net10.0` | `Library` | No | Test Project | Clovent.Authentication | Microsoft.NET.Test.Sdk `17.14.1`<br/>xunit `2.9.3`<br/>xunit.runner.visualstudio `3.1.4`<br/>*+1 more* |
| **Clovent.Authentication** | `src/Clovent.Authentication/Clovent.Authentication.csproj` | `net10.0` | `Library` | No | Class Library | Clovent.Domain<br/>Clovent.Identity | *None* |
| **Clovent.Catalog.Application.Tests** | `src/Clovent.Catalog.Application.Tests/Clovent.Catalog.Application.Tests.csproj` | `net10.0` | `Library` | No | Test Project | Clovent.Catalog.Application | Microsoft.NET.Test.Sdk `17.14.1`<br/>xunit `2.9.3`<br/>xunit.runner.visualstudio `3.1.4`<br/>*+1 more* |
| **Clovent.Catalog.Application** | `src/Clovent.Catalog.Application/Clovent.Catalog.Application.csproj` | `net10.0` | `Library` | No | Application / CQRS Layer | Clovent.Catalog | MediatR `12.4.1`<br/>Microsoft.Extensions.DependencyInjection.Abstractions `10.0.10`<br/>Microsoft.Extensions.Configuration.Abstractions `10.0.10` |
| **Clovent.Catalog.Infrastructure.Tests** | `src/Clovent.Catalog.Infrastructure.Tests/Clovent.Catalog.Infrastructure.Tests.csproj` | `net10.0` | `Library` | No | Test Project | Clovent.Catalog.Infrastructure | Microsoft.NET.Test.Sdk `17.14.1`<br/>xunit `2.9.3`<br/>xunit.runner.visualstudio `3.1.4`<br/>*+3 more* |
| **Clovent.Catalog.Infrastructure** | `src/Clovent.Catalog.Infrastructure/Clovent.Catalog.Infrastructure.csproj` | `net10.0` | `Library` | No | Database / Persistence Layer | Clovent.Catalog<br/>Clovent.Catalog.Application<br/>Clovent.Platform | Microsoft.EntityFrameworkCore `10.0.10`<br/>Microsoft.EntityFrameworkCore.SqlServer `10.0.10`<br/>Microsoft.EntityFrameworkCore.Design `10.0.10`<br/>*+3 more* |
| **Clovent.Catalog.Tests** | `src/Clovent.Catalog.Tests/Clovent.Catalog.Tests.csproj` | `net10.0` | `Library` | No | Test Project | Clovent.Catalog | Microsoft.NET.Test.Sdk `17.14.1`<br/>xunit `2.9.3`<br/>xunit.runner.visualstudio `3.1.4`<br/>*+1 more* |
| **Clovent.Catalog** | `src/Clovent.Catalog/Clovent.Catalog.csproj` | `net10.0` | `Library` | No | Class Library | Clovent.Domain<br/>Clovent.MasterData | *None* |
| **Clovent.Desktop.Tests** | `src/Clovent.Desktop.Tests/Clovent.Desktop.Tests.csproj` | `net10.0-windows` | `Library` | Yes | Test Project | Clovent.Desktop | Microsoft.NET.Test.Sdk `17.14.1`<br/>xunit `2.9.3`<br/>xunit.runner.visualstudio `3.1.4`<br/>*+1 more* |
| **Clovent.Desktop.UiQa** | `src/Clovent.Desktop.UiQa/Clovent.Desktop.UiQa.csproj` | `net10.0-windows` | `Exe` | Yes | Live UI Test Harness | Clovent.Desktop | *None* |
| **Clovent.Desktop** | `src/Clovent.Desktop/Clovent.Desktop.csproj` | `net10.0-windows` | `WinExe` | Yes | Class Library | Clovent.Platform<br/>Clovent.Authentication.Application<br/>Clovent.Authentication.Infrastructure<br/>Clovent.Identity<br/>Clovent.Identity.Application<br/>Clovent.Identity.Infrastructure<br/>Clovent.MasterData<br/>Clovent.MasterData.Application<br/>Clovent.MasterData.Infrastructure<br/>Clovent.Catalog<br/>Clovent.Catalog.Application<br/>Clovent.Catalog.Infrastructure<br/>Clovent.Inventory<br/>Clovent.Inventory.Application<br/>Clovent.Inventory.Infrastructure<br/>Clovent.Restaurant<br/>Clovent.Restaurant.Application<br/>Clovent.Restaurant.Infrastructure | DevExpress.Reporting.Core `26.1.4-pre-26179`<br/>DevExpress.Win `26.1.4-pre-26179`<br/>DevExpress.Images `26.1.4-pre-26179`<br/>*+1 more* |
| **Clovent.Domain.Tests** | `src/Clovent.Domain.Tests/Clovent.Domain.Tests.csproj` | `net10.0` | `Library` | No | Test Project | Clovent.Domain | Microsoft.NET.Test.Sdk `17.14.1`<br/>xunit `2.9.3`<br/>xunit.runner.visualstudio `3.1.4`<br/>*+1 more* |
| **Clovent.Domain** | `src/Clovent.Domain/Clovent.Domain.csproj` | `net10.0` | `Library` | No | Domain Kernel | *None* | *None* |
| **Clovent.Identity.Application.Tests** | `src/Clovent.Identity.Application.Tests/Clovent.Identity.Application.Tests.csproj` | `net10.0` | `Library` | No | Test Project | Clovent.Identity.Application | Microsoft.NET.Test.Sdk `17.14.1`<br/>xunit `2.9.3`<br/>xunit.runner.visualstudio `3.1.4`<br/>*+1 more* |
| **Clovent.Identity.Application** | `src/Clovent.Identity.Application/Clovent.Identity.Application.csproj` | `net10.0` | `Library` | No | Application / CQRS Layer | Clovent.Identity | MediatR `12.4.1`<br/>Microsoft.Extensions.DependencyInjection.Abstractions `10.0.10`<br/>Microsoft.Extensions.Configuration.Abstractions `10.0.10` |
| **Clovent.Identity.Infrastructure.Tests** | `src/Clovent.Identity.Infrastructure.Tests/Clovent.Identity.Infrastructure.Tests.csproj` | `net10.0` | `Library` | No | Test Project | Clovent.Identity.Infrastructure | Microsoft.NET.Test.Sdk `17.14.1`<br/>xunit `2.9.3`<br/>xunit.runner.visualstudio `3.1.4`<br/>*+3 more* |
| **Clovent.Identity.Infrastructure** | `src/Clovent.Identity.Infrastructure/Clovent.Identity.Infrastructure.csproj` | `net10.0` | `Library` | No | Database / Persistence Layer | Clovent.Identity<br/>Clovent.Identity.Application<br/>Clovent.Platform | Microsoft.EntityFrameworkCore `10.0.10`<br/>Microsoft.EntityFrameworkCore.SqlServer `10.0.10`<br/>Microsoft.EntityFrameworkCore.Design `10.0.10`<br/>*+4 more* |
| **Clovent.Identity.Tests** | `src/Clovent.Identity.Tests/Clovent.Identity.Tests.csproj` | `net10.0` | `Library` | No | Test Project | Clovent.Identity | Microsoft.NET.Test.Sdk `17.14.1`<br/>xunit `2.9.3`<br/>xunit.runner.visualstudio `3.1.4`<br/>*+1 more* |
| **Clovent.Identity** | `src/Clovent.Identity/Clovent.Identity.csproj` | `net10.0` | `Library` | No | Class Library | Clovent.Domain | *None* |
| **Clovent.Inventory.Application.Tests** | `src/Clovent.Inventory.Application.Tests/Clovent.Inventory.Application.Tests.csproj` | `net10.0` | `Library` | No | Test Project | Clovent.Inventory.Application | Microsoft.NET.Test.Sdk `17.14.1`<br/>xunit `2.9.3`<br/>xunit.runner.visualstudio `3.1.4`<br/>*+1 more* |
| **Clovent.Inventory.Application** | `src/Clovent.Inventory.Application/Clovent.Inventory.Application.csproj` | `net10.0` | `Library` | No | Application / CQRS Layer | Clovent.Inventory | MediatR `12.4.1`<br/>Microsoft.Extensions.DependencyInjection.Abstractions `10.0.10`<br/>Microsoft.Extensions.Configuration.Abstractions `10.0.10` |
| **Clovent.Inventory.Infrastructure.Tests** | `src/Clovent.Inventory.Infrastructure.Tests/Clovent.Inventory.Infrastructure.Tests.csproj` | `net10.0` | `Library` | No | Test Project | Clovent.Inventory.Infrastructure | Microsoft.NET.Test.Sdk `17.14.1`<br/>xunit `2.9.3`<br/>xunit.runner.visualstudio `3.1.4`<br/>*+3 more* |
| **Clovent.Inventory.Infrastructure** | `src/Clovent.Inventory.Infrastructure/Clovent.Inventory.Infrastructure.csproj` | `net10.0` | `Library` | No | Database / Persistence Layer | Clovent.Inventory<br/>Clovent.Inventory.Application<br/>Clovent.Platform | Microsoft.EntityFrameworkCore `10.0.10`<br/>Microsoft.EntityFrameworkCore.SqlServer `10.0.10`<br/>Microsoft.EntityFrameworkCore.Design `10.0.10`<br/>*+3 more* |
| **Clovent.Inventory.Tests** | `src/Clovent.Inventory.Tests/Clovent.Inventory.Tests.csproj` | `net10.0` | `Library` | No | Test Project | Clovent.Inventory | Microsoft.NET.Test.Sdk `17.14.1`<br/>xunit `2.9.3`<br/>xunit.runner.visualstudio `3.1.4`<br/>*+1 more* |
| **Clovent.Inventory** | `src/Clovent.Inventory/Clovent.Inventory.csproj` | `net10.0` | `Library` | No | Class Library | Clovent.Domain<br/>Clovent.MasterData<br/>Clovent.Catalog | *None* |
| **Clovent.MasterData.Application.Tests** | `src/Clovent.MasterData.Application.Tests/Clovent.MasterData.Application.Tests.csproj` | `net10.0` | `Library` | No | Test Project | Clovent.MasterData.Application | Microsoft.NET.Test.Sdk `17.14.1`<br/>xunit `2.9.3`<br/>xunit.runner.visualstudio `3.1.4`<br/>*+1 more* |
| **Clovent.MasterData.Application** | `src/Clovent.MasterData.Application/Clovent.MasterData.Application.csproj` | `net10.0` | `Library` | No | Application / CQRS Layer | Clovent.MasterData | MediatR `12.4.1`<br/>Microsoft.Extensions.DependencyInjection.Abstractions `10.0.10`<br/>Microsoft.Extensions.Configuration.Abstractions `10.0.10` |
| **Clovent.MasterData.Infrastructure.Tests** | `src/Clovent.MasterData.Infrastructure.Tests/Clovent.MasterData.Infrastructure.Tests.csproj` | `net10.0` | `Library` | No | Test Project | Clovent.MasterData.Infrastructure | Microsoft.NET.Test.Sdk `17.14.1`<br/>xunit `2.9.3`<br/>xunit.runner.visualstudio `3.1.4`<br/>*+3 more* |
| **Clovent.MasterData.Infrastructure** | `src/Clovent.MasterData.Infrastructure/Clovent.MasterData.Infrastructure.csproj` | `net10.0` | `Library` | No | Database / Persistence Layer | Clovent.MasterData<br/>Clovent.MasterData.Application<br/>Clovent.Platform | Microsoft.EntityFrameworkCore `10.0.10`<br/>Microsoft.EntityFrameworkCore.SqlServer `10.0.10`<br/>Microsoft.EntityFrameworkCore.Design `10.0.10`<br/>*+3 more* |
| **Clovent.MasterData.Tests** | `src/Clovent.MasterData.Tests/Clovent.MasterData.Tests.csproj` | `net10.0` | `Library` | No | Test Project | Clovent.MasterData | Microsoft.NET.Test.Sdk `17.14.1`<br/>xunit `2.9.3`<br/>xunit.runner.visualstudio `3.1.4`<br/>*+1 more* |
| **Clovent.MasterData** | `src/Clovent.MasterData/Clovent.MasterData.csproj` | `net10.0` | `Library` | No | Class Library | Clovent.Domain<br/>Clovent.Identity | *None* |
| **Clovent.Platform.Tests** | `src/Clovent.Platform.Tests/Clovent.Platform.Tests.csproj` | `net10.0` | `Library` | No | Test Project | Clovent.Platform | Microsoft.NET.Test.Sdk `17.14.1`<br/>xunit `2.9.3`<br/>xunit.runner.visualstudio `3.1.4`<br/>*+1 more* |
| **Clovent.Platform** | `src/Clovent.Platform/Clovent.Platform.csproj` | `net10.0` | `Library` | No | Platform / Shared Infrastructure | *None* | Microsoft.Extensions.Hosting `10.0.10`<br/>Microsoft.Extensions.Options.DataAnnotations `10.0.10` |
| **Clovent.Restaurant.Application.Tests** | `src/Clovent.Restaurant.Application.Tests/Clovent.Restaurant.Application.Tests.csproj` | `net10.0` | `Library` | No | Test Project | Clovent.Restaurant.Application | Microsoft.NET.Test.Sdk `17.14.1`<br/>xunit `2.9.3`<br/>xunit.runner.visualstudio `3.1.4`<br/>*+1 more* |
| **Clovent.Restaurant.Application** | `src/Clovent.Restaurant.Application/Clovent.Restaurant.Application.csproj` | `net10.0` | `Library` | No | Application / CQRS Layer | Clovent.Restaurant<br/>Clovent.Catalog.Application<br/>Clovent.Inventory.Application | MediatR `12.4.1`<br/>Microsoft.Extensions.Configuration.Abstractions `10.0.10`<br/>Microsoft.Extensions.DependencyInjection.Abstractions `10.0.10` |
| **Clovent.Restaurant.Infrastructure.Tests** | `src/Clovent.Restaurant.Infrastructure.Tests/Clovent.Restaurant.Infrastructure.Tests.csproj` | `net10.0` | `Library` | No | Test Project | Clovent.Restaurant.Infrastructure<br/>Clovent.Catalog.Infrastructure<br/>Clovent.Inventory.Infrastructure | Microsoft.NET.Test.Sdk `17.14.1`<br/>xunit `2.9.3`<br/>xunit.runner.visualstudio `3.1.4`<br/>*+4 more* |
| **Clovent.Restaurant.Infrastructure** | `src/Clovent.Restaurant.Infrastructure/Clovent.Restaurant.Infrastructure.csproj` | `net10.0` | `Library` | No | Database / Persistence Layer | Clovent.Restaurant<br/>Clovent.Restaurant.Application<br/>Clovent.Platform | Microsoft.EntityFrameworkCore `10.0.10`<br/>Microsoft.EntityFrameworkCore.SqlServer `10.0.10`<br/>Microsoft.EntityFrameworkCore.Design `10.0.10`<br/>*+3 more* |
| **Clovent.Restaurant.Tests** | `src/Clovent.Restaurant.Tests/Clovent.Restaurant.Tests.csproj` | `net10.0` | `Library` | No | Test Project | Clovent.Restaurant | Microsoft.NET.Test.Sdk `17.14.1`<br/>xunit `2.9.3`<br/>xunit.runner.visualstudio `3.1.4`<br/>*+1 more* |
| **Clovent.Restaurant** | `src/Clovent.Restaurant/Clovent.Restaurant.csproj` | `net10.0` | `Library` | No | Class Library | Clovent.Domain<br/>Clovent.Identity<br/>Clovent.MasterData<br/>Clovent.Catalog | *None* |

---

## 3. Comprehensive File Inventory & Metadata

There are **2,536 active source, configuration, documentation, and resource files** in the workspace. Below is the detailed catalog listing every file, its size, lines of code, and architectural purpose:

### 3.1 Presentation Layer: `src/Clovent.Desktop` (WinForms + DevExpress)

| File Path | Name | Ext | Size | Lines | Category / Purpose | Status |
|---|---|---|---|---|---|---|
| `src/Clovent.Desktop/Authorization/IManagerAuthorizationService.cs` | `IManagerAuthorizationService.cs` | `.cs` | 3,524 B | 72 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Authorization/ManagerAuthorizationService.cs` | `ManagerAuthorizationService.cs` | `.cs` | 6,195 B | 134 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Catalog/Barcodes/BarcodeCreateForm.Designer.cs` | `BarcodeCreateForm.Designer.cs` | `.cs` | 2,844 B | 78 | WinForms Visual Designer Code | Active |
| `src/Clovent.Desktop/Catalog/Barcodes/BarcodeCreateForm.cs` | `BarcodeCreateForm.cs` | `.cs` | 1,471 B | 42 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Catalog/Barcodes/BarcodeCreateForm.resx` | `BarcodeCreateForm.resx` | `.resx` | 5,627 B | 120 | WinForms Resource XML | Active |
| `src/Clovent.Desktop/Catalog/Barcodes/BarcodeManagementView.Designer.cs` | `BarcodeManagementView.Designer.cs` | `.cs` | 3,190 B | 86 | WinForms Visual Designer Code | Active |
| `src/Clovent.Desktop/Catalog/Barcodes/BarcodeManagementView.cs` | `BarcodeManagementView.cs` | `.cs` | 3,246 B | 80 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Catalog/Brands/BrandEditForm.Designer.cs` | `BrandEditForm.Designer.cs` | `.cs` | 2,055 B | 63 | WinForms Visual Designer Code | Active |
| `src/Clovent.Desktop/Catalog/Brands/BrandEditForm.cs` | `BrandEditForm.cs` | `.cs` | 1,514 B | 46 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Catalog/Brands/BrandEditForm.resx` | `BrandEditForm.resx` | `.resx` | 5,627 B | 120 | WinForms Resource XML | Active |
| `src/Clovent.Desktop/Catalog/Brands/BrandManagementView.Designer.cs` | `BrandManagementView.Designer.cs` | `.cs` | 1,778 B | 58 | WinForms Visual Designer Code | Active |
| `src/Clovent.Desktop/Catalog/Brands/BrandManagementView.cs` | `BrandManagementView.cs` | `.cs` | 2,675 B | 69 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Catalog/Categories/ProductCategoryEditForm.Designer.cs` | `ProductCategoryEditForm.Designer.cs` | `.cs` | 5,195 B | 122 | WinForms Visual Designer Code | Active |
| `src/Clovent.Desktop/Catalog/Categories/ProductCategoryEditForm.cs` | `ProductCategoryEditForm.cs` | `.cs` | 3,437 B | 84 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Catalog/Categories/ProductCategoryEditForm.resx` | `ProductCategoryEditForm.resx` | `.resx` | 5,627 B | 120 | WinForms Resource XML | Active |
| `src/Clovent.Desktop/Catalog/Categories/ProductCategoryManagementView.Designer.cs` | `ProductCategoryManagementView.Designer.cs` | `.cs` | 1,870 B | 58 | WinForms Visual Designer Code | Active |
| `src/Clovent.Desktop/Catalog/Categories/ProductCategoryManagementView.cs` | `ProductCategoryManagementView.cs` | `.cs` | 3,831 B | 83 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Catalog/Prices/ProductPriceEditForm.Designer.cs` | `ProductPriceEditForm.Designer.cs` | `.cs` | 4,761 B | 116 | WinForms Visual Designer Code | Active |
| `src/Clovent.Desktop/Catalog/Prices/ProductPriceEditForm.cs` | `ProductPriceEditForm.cs` | `.cs` | 3,511 B | 88 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Catalog/Prices/ProductPriceEditForm.resx` | `ProductPriceEditForm.resx` | `.resx` | 5,627 B | 120 | WinForms Resource XML | Active |
| `src/Clovent.Desktop/Catalog/Prices/ProductPriceManagementView.Designer.cs` | `ProductPriceManagementView.Designer.cs` | `.cs` | 2,896 B | 83 | WinForms Visual Designer Code | Active |
| `src/Clovent.Desktop/Catalog/Prices/ProductPriceManagementView.cs` | `ProductPriceManagementView.cs` | `.cs` | 5,216 B | 128 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Catalog/UnitsOfMeasure/UnitOfMeasureEditForm.Designer.cs` | `UnitOfMeasureEditForm.Designer.cs` | `.cs` | 3,116 B | 85 | WinForms Visual Designer Code | Active |
| `src/Clovent.Desktop/Catalog/UnitsOfMeasure/UnitOfMeasureEditForm.cs` | `UnitOfMeasureEditForm.cs` | `.cs` | 2,459 B | 66 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Catalog/UnitsOfMeasure/UnitOfMeasureEditForm.resx` | `UnitOfMeasureEditForm.resx` | `.resx` | 5,627 B | 120 | WinForms Resource XML | Active |
| `src/Clovent.Desktop/Catalog/UnitsOfMeasure/UnitOfMeasureManagementView.Designer.cs` | `UnitOfMeasureManagementView.Designer.cs` | `.cs` | 1,936 B | 59 | WinForms Visual Designer Code | Active |
| `src/Clovent.Desktop/Catalog/UnitsOfMeasure/UnitOfMeasureManagementView.cs` | `UnitOfMeasureManagementView.cs` | `.cs` | 2,862 B | 69 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Catalog/Variants/ProductVariantEditForm.Designer.cs` | `ProductVariantEditForm.Designer.cs` | `.cs` | 3,905 B | 101 | WinForms Visual Designer Code | Active |
| `src/Clovent.Desktop/Catalog/Variants/ProductVariantEditForm.cs` | `ProductVariantEditForm.cs` | `.cs` | 3,206 B | 90 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Catalog/Variants/ProductVariantEditForm.resx` | `ProductVariantEditForm.resx` | `.resx` | 5,627 B | 120 | WinForms Resource XML | Active |
| `src/Clovent.Desktop/Catalog/Variants/ProductVariantManagementView.Designer.cs` | `ProductVariantManagementView.Designer.cs` | `.cs` | 2,890 B | 83 | WinForms Visual Designer Code | Active |
| `src/Clovent.Desktop/Catalog/Variants/ProductVariantManagementView.cs` | `ProductVariantManagementView.cs` | `.cs` | 4,549 B | 103 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Clovent.Desktop.csproj` | `Clovent.Desktop.csproj` | `.csproj` | 4,866 B | 0 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Clovent.Desktop.csproj.user` | `Clovent.Desktop.csproj.user` | `.user` | 6,637 B | 177 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Composition/IdentityUserServiceAdapter.cs` | `IdentityUserServiceAdapter.cs` | `.cs` | 2,756 B | 64 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Dashboard/CatalogDashboardCalculations.cs` | `CatalogDashboardCalculations.cs` | `.cs` | 1,915 B | 37 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Dashboard/RestaurantDashboardCalculations.cs` | `RestaurantDashboardCalculations.cs` | `.cs` | 1,764 B | 35 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/DependencyInjection/DesktopServiceCollectionExtensions.cs` | `DesktopServiceCollectionExtensions.cs` | `.cs` | 9,097 B | 175 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/DependencyInjection/DesktopServiceCollectionExtensions.cs.backup-orderhistory-20260817-124222` | `DesktopServiceCollectionExtensions.cs.backup-orderhistory-20260817-124222` | `.backup-orderhistory-20260817-124222` | 8,511 B | 0 | Timestamped Snapshot Backup | Duplicate / Obsolete |
| `src/Clovent.Desktop/Forms/Base/Appearance/AppearanceManager.cs` | `AppearanceManager.cs` | `.cs` | 10,531 B | 260 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Forms/Base/Appearance/AppearanceRule.cs` | `AppearanceRule.cs` | `.cs` | 1,414 B | 36 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Forms/Base/Appearance/AppearanceScopeType.cs` | `AppearanceScopeType.cs` | `.cs` | 1,364 B | 27 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Forms/Base/Appearance/AppearanceSettingsStore.cs` | `AppearanceSettingsStore.cs` | `.cs` | 2,464 B | 56 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Forms/Base/BaseForm.Designer.cs` | `BaseForm.Designer.cs` | `.cs` | 7,333 B | 159 | WinForms Visual Designer Code | Active |
| `src/Clovent.Desktop/Forms/Base/BaseForm.cs` | `BaseForm.cs` | `.cs` | 9,631 B | 227 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Forms/Base/BaseForm.resx` | `BaseForm.resx` | `.resx` | 5,627 B | 120 | WinForms Resource XML | Active |
| `src/Clovent.Desktop/Forms/Base/CommandPanelLayout.cs` | `CommandPanelLayout.cs` | `.cs` | 9,881 B | 193 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Forms/Base/CurrencyDisplay.cs` | `CurrencyDisplay.cs` | `.cs` | 2,375 B | 46 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Forms/Base/CurrencyDisplayLoader.cs` | `CurrencyDisplayLoader.cs` | `.cs` | 2,080 B | 54 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Forms/Base/DateTimeDisplay.cs` | `DateTimeDisplay.cs` | `.cs` | 2,327 B | 59 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Forms/Base/DateTimeDisplayLoader.cs` | `DateTimeDisplayLoader.cs` | `.cs` | 3,316 B | 75 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Forms/Base/DesignModeHelper.cs` | `DesignModeHelper.cs` | `.cs` | 856 B | 28 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Forms/Base/DesktopDpi.cs` | `DesktopDpi.cs` | `.cs` | 857 B | 17 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Forms/Base/DesktopIcons.cs` | `DesktopIcons.cs` | `.cs` | 2,136 B | 46 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Forms/Base/DesktopStyle.cs` | `DesktopStyle.cs` | `.cs` | 2,734 B | 58 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Forms/Base/GridSpacer.cs` | `GridSpacer.cs` | `.cs` | 738 B | 25 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Forms/Base/GuardedAction.cs` | `GuardedAction.cs` | `.cs` | 3,013 B | 72 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Forms/Base/Localization/LanguageInitializationStartupTask.cs` | `LanguageInitializationStartupTask.cs` | `.cs` | 904 B | 24 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Forms/Base/Localization/LanguagePreferenceStore.cs` | `LanguagePreferenceStore.cs` | `.cs` | 1,670 B | 40 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Forms/Base/Localization/LocalizationHelper.cs` | `LocalizationHelper.cs` | `.cs` | 8,495 B | 215 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Forms/Base/Localization/PosStrings.cs` | `PosStrings.cs` | `.cs` | 5,502 B | 117 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Forms/Base/Localization/PosStrings.resx` | `PosStrings.resx` | `.resx` | 8,629 B | 280 | WinForms Resource XML | Active |
| `src/Clovent.Desktop/Forms/Base/Localization/PosStrings.ur.resx` | `PosStrings.ur.resx` | `.resx` | 9,311 B | 280 | WinForms Resource XML | Active |
| `src/Clovent.Desktop/Forms/Base/PosSettingsStore.cs` | `PosSettingsStore.cs` | `.cs` | 5,195 B | 157 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Forms/Base/ScreenOperationGate.cs` | `ScreenOperationGate.cs` | `.cs` | 1,577 B | 46 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Forms/Base/SerializedFeatureAuthorizationPolicy.cs` | `SerializedFeatureAuthorizationPolicy.cs` | `.cs` | 954 B | 18 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Forms/Base/SerializedMediator.cs` | `SerializedMediator.cs` | `.cs` | 4,874 B | 86 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Forms/Base/StatusBadgeStyler.cs` | `StatusBadgeStyler.cs` | `.cs` | 2,100 B | 48 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Forms/Base/WindowPlacementStore.cs` | `WindowPlacementStore.cs` | `.cs` | 4,383 B | 98 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Forms/Catalog/Products/ProductEditForm.Designer.cs` | `ProductEditForm.Designer.cs` | `.cs` | 9,335 B | 213 | WinForms Visual Designer Code | Active |
| `src/Clovent.Desktop/Forms/Catalog/Products/ProductEditForm.cs` | `ProductEditForm.cs` | `.cs` | 5,985 B | 141 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Forms/Catalog/Products/ProductEditForm.resx` | `ProductEditForm.resx` | `.resx` | 5,627 B | 120 | WinForms Resource XML | Active |
| `src/Clovent.Desktop/Forms/Catalog/Products/ProductsForm.Designer.cs` | `ProductsForm.Designer.cs` | `.cs` | 8,648 B | 202 | WinForms Visual Designer Code | Active |
| `src/Clovent.Desktop/Forms/Catalog/Products/ProductsForm.cs` | `ProductsForm.cs` | `.cs` | 15,891 B | 404 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Forms/Catalog/Products/ProductsForm.resx` | `ProductsForm.resx` | `.resx` | 5,745 B | 120 | WinForms Resource XML | Active |
| `src/Clovent.Desktop/Forms/Dashboard/DashboardView.Designer.cs` | `DashboardView.Designer.cs` | `.cs` | 61,585 B | 1199 | WinForms Visual Designer Code | Active |
| `src/Clovent.Desktop/Forms/Dashboard/DashboardView.cs` | `DashboardView.cs` | `.cs` | 16,129 B | 383 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Forms/Dashboard/DashboardView.resx` | `DashboardView.resx` | `.resx` | 5,748 B | 120 | WinForms Resource XML | Active |
| `src/Clovent.Desktop/Forms/Identity/LoginForm.Designer.cs` | `LoginForm.Designer.cs` | `.cs` | 37,182 B | 773 | WinForms Visual Designer Code | Active |
| `src/Clovent.Desktop/Forms/Identity/LoginForm.cs` | `LoginForm.cs` | `.cs` | 26,663 B | 567 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Forms/Identity/LoginForm.cs.backup-ui-fixes-20260818` | `LoginForm.cs.backup-ui-fixes-20260818` | `.backup-ui-fixes-20260818` | 21,277 B | 0 | Timestamped Snapshot Backup | Duplicate / Obsolete |
| `src/Clovent.Desktop/Forms/Identity/LoginForm.resx` | `LoginForm.resx` | `.resx` | 5,748 B | 120 | WinForms Resource XML | Active |
| `src/Clovent.Desktop/Forms/Identity/Roles/RoleEditForm.Designer.cs` | `RoleEditForm.Designer.cs` | `.cs` | 3,791 B | 97 | WinForms Visual Designer Code | Active |
| `src/Clovent.Desktop/Forms/Identity/Roles/RoleEditForm.cs` | `RoleEditForm.cs` | `.cs` | 3,546 B | 83 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Forms/Identity/Roles/RoleEditForm.resx` | `RoleEditForm.resx` | `.resx` | 5,627 B | 120 | WinForms Resource XML | Active |
| `src/Clovent.Desktop/Forms/Identity/Roles/RolesForm.Designer.cs` | `RolesForm.Designer.cs` | `.cs` | 5,711 B | 130 | WinForms Visual Designer Code | Active |
| `src/Clovent.Desktop/Forms/Identity/Roles/RolesForm.cs` | `RolesForm.cs` | `.cs` | 7,726 B | 210 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Forms/Identity/Roles/RolesForm.resx` | `RolesForm.resx` | `.resx` | 5,627 B | 120 | WinForms Resource XML | Active |
| `src/Clovent.Desktop/Forms/Identity/Users/UserEditForm.Designer.cs` | `UserEditForm.Designer.cs` | `.cs` | 7,796 B | 179 | WinForms Visual Designer Code | Active |
| `src/Clovent.Desktop/Forms/Identity/Users/UserEditForm.cs` | `UserEditForm.cs` | `.cs` | 6,835 B | 156 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Forms/Identity/Users/UserEditForm.resx` | `UserEditForm.resx` | `.resx` | 5,627 B | 120 | WinForms Resource XML | Active |
| `src/Clovent.Desktop/Forms/Identity/Users/UsersForm.Designer.cs` | `UsersForm.Designer.cs` | `.cs` | 10,253 B | 237 | WinForms Visual Designer Code | Active |
| `src/Clovent.Desktop/Forms/Identity/Users/UsersForm.cs` | `UsersForm.cs` | `.cs` | 20,112 B | 498 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Forms/Identity/Users/UsersForm.resx` | `UsersForm.resx` | `.resx` | 5,627 B | 120 | WinForms Resource XML | Active |
| `src/Clovent.Desktop/Forms/Restaurant/ActivityLog/ActivityLogView.Designer.cs` | `ActivityLogView.Designer.cs` | `.cs` | 7,914 B | 194 | WinForms Visual Designer Code | Active |
| `src/Clovent.Desktop/Forms/Restaurant/ActivityLog/ActivityLogView.cs` | `ActivityLogView.cs` | `.cs` | 6,198 B | 150 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Forms/Restaurant/ActivityLog/ActivityLogView.resx` | `ActivityLogView.resx` | `.resx` | 5,817 B | 120 | WinForms Resource XML | Active |
| `src/Clovent.Desktop/Forms/Restaurant/Appearance/AppearanceRuleEditForm.Designer.cs` | `AppearanceRuleEditForm.Designer.cs` | `.cs` | 13,794 B | 293 | WinForms Visual Designer Code | Active |
| `src/Clovent.Desktop/Forms/Restaurant/Appearance/AppearanceRuleEditForm.cs` | `AppearanceRuleEditForm.cs` | `.cs` | 8,986 B | 194 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Forms/Restaurant/Appearance/AppearanceRuleEditForm.resx` | `AppearanceRuleEditForm.resx` | `.resx` | 5,627 B | 120 | WinForms Resource XML | Active |
| `src/Clovent.Desktop/Forms/Restaurant/Appearance/AppearanceSettingsView.Designer.cs` | `AppearanceSettingsView.Designer.cs` | `.cs` | 3,510 B | 90 | WinForms Visual Designer Code | Active |
| `src/Clovent.Desktop/Forms/Restaurant/Appearance/AppearanceSettingsView.cs` | `AppearanceSettingsView.cs` | `.cs` | 6,065 B | 164 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Forms/Restaurant/MenuItems/CategoryColorDialog.Designer.cs` | `CategoryColorDialog.Designer.cs` | `.cs` | 3,997 B | 104 | WinForms Visual Designer Code | Active |
| `src/Clovent.Desktop/Forms/Restaurant/MenuItems/CategoryColorDialog.cs` | `CategoryColorDialog.cs` | `.cs` | 2,317 B | 58 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Forms/Restaurant/MenuItems/CategoryColorDialog.resx` | `CategoryColorDialog.resx` | `.resx` | 5,627 B | 120 | WinForms Resource XML | Active |
| `src/Clovent.Desktop/Forms/Restaurant/MenuItems/IMenuItemsChangeNotifier.cs` | `IMenuItemsChangeNotifier.cs` | `.cs` | 1,350 B | 31 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Forms/Restaurant/MenuItems/MenuItemEditForm.Designer.cs` | `MenuItemEditForm.Designer.cs` | `.cs` | 17,243 B | 378 | WinForms Visual Designer Code | Active |
| `src/Clovent.Desktop/Forms/Restaurant/MenuItems/MenuItemEditForm.cs` | `MenuItemEditForm.cs` | `.cs` | 17,448 B | 454 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Forms/Restaurant/MenuItems/MenuItemEditForm.resx` | `MenuItemEditForm.resx` | `.resx` | 5,627 B | 120 | WinForms Resource XML | Active |
| `src/Clovent.Desktop/Forms/Restaurant/MenuItems/MenuItemImageStore.cs` | `MenuItemImageStore.cs` | `.cs` | 4,871 B | 102 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Forms/Restaurant/MenuItems/MenuItemsForm.Designer.cs` | `MenuItemsForm.Designer.cs` | `.cs` | 14,750 B | 298 | WinForms Visual Designer Code | Active |
| `src/Clovent.Desktop/Forms/Restaurant/MenuItems/MenuItemsForm.cs` | `MenuItemsForm.cs` | `.cs` | 39,458 B | 889 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Forms/Restaurant/Setup/PaymentMethodEditForm.Designer.cs` | `PaymentMethodEditForm.Designer.cs` | `.cs` | 2,066 B | 65 | WinForms Visual Designer Code | Active |
| `src/Clovent.Desktop/Forms/Restaurant/Setup/PaymentMethodEditForm.cs` | `PaymentMethodEditForm.cs` | `.cs` | 1,628 B | 42 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Forms/Restaurant/Setup/PaymentMethodEditForm.resx` | `PaymentMethodEditForm.resx` | `.resx` | 5,627 B | 120 | WinForms Resource XML | Active |
| `src/Clovent.Desktop/Forms/Restaurant/Setup/PaymentMethodsView.Designer.cs` | `PaymentMethodsView.Designer.cs` | `.cs` | 1,666 B | 57 | WinForms Visual Designer Code | Active |
| `src/Clovent.Desktop/Forms/Restaurant/Setup/PaymentMethodsView.cs` | `PaymentMethodsView.cs` | `.cs` | 5,774 B | 130 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Forms/Restaurant/Setup/PaymentMethodsView.resx` | `PaymentMethodsView.resx` | `.resx` | 5,817 B | 120 | WinForms Resource XML | Active |
| `src/Clovent.Desktop/Forms/Restaurant/Setup/RestaurantSetupView.Designer.cs` | `RestaurantSetupView.Designer.cs` | `.cs` | 16,322 B | 341 | WinForms Visual Designer Code | Active |
| `src/Clovent.Desktop/Forms/Restaurant/Setup/RestaurantSetupView.cs` | `RestaurantSetupView.cs` | `.cs` | 11,402 B | 269 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Forms/Restaurant/Setup/RestaurantSetupView.resx` | `RestaurantSetupView.resx` | `.resx` | 5,817 B | 120 | WinForms Resource XML | Active |
| `src/Clovent.Desktop/Forms/Shell/IWorkspaceHost.cs` | `IWorkspaceHost.cs` | `.cs` | 1,118 B | 23 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Forms/Shell/MainForm.Designer.cs` | `MainForm.Designer.cs` | `.cs` | 14,085 B | 275 | WinForms Visual Designer Code | Active |
| `src/Clovent.Desktop/Forms/Shell/MainForm.Designer.cs.backup-orderhistory-20260817-124222` | `MainForm.Designer.cs.backup-orderhistory-20260817-124222` | `.backup-orderhistory-20260817-124222` | 13,787 B | 0 | Timestamped Snapshot Backup | Duplicate / Obsolete |
| `src/Clovent.Desktop/Forms/Shell/MainForm.cs` | `MainForm.cs` | `.cs` | 20,077 B | 514 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Forms/Shell/MainForm.resx` | `MainForm.resx` | `.resx` | 5,627 B | 120 | WinForms Resource XML | Active |
| `src/Clovent.Desktop/Identity/Users/PasswordPromptForm.Designer.cs` | `PasswordPromptForm.Designer.cs` | `.cs` | 2,313 B | 63 | WinForms Visual Designer Code | Active |
| `src/Clovent.Desktop/Identity/Users/PasswordPromptForm.cs` | `PasswordPromptForm.cs` | `.cs` | 2,434 B | 63 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Identity/Users/PasswordPromptForm.resx` | `PasswordPromptForm.resx` | `.resx` | 5,627 B | 120 | WinForms Resource XML | Active |
| `src/Clovent.Desktop/Identity/Users/PinPromptForm.Designer.cs` | `PinPromptForm.Designer.cs` | `.cs` | 1,775 B | 54 | WinForms Visual Designer Code | Active |
| `src/Clovent.Desktop/Identity/Users/PinPromptForm.cs` | `PinPromptForm.cs` | `.cs` | 2,021 B | 56 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Inventory/Adjustments/StockAdjustmentCreateForm.Designer.cs` | `StockAdjustmentCreateForm.Designer.cs` | `.cs` | 5,598 B | 136 | WinForms Visual Designer Code | Active |
| `src/Clovent.Desktop/Inventory/Adjustments/StockAdjustmentCreateForm.cs` | `StockAdjustmentCreateForm.cs` | `.cs` | 3,018 B | 81 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Inventory/Adjustments/StockAdjustmentCreateForm.resx` | `StockAdjustmentCreateForm.resx` | `.resx` | 5,626 B | 120 | WinForms Resource XML | Active |
| `src/Clovent.Desktop/Inventory/Adjustments/StockAdjustmentManagementView.Designer.cs` | `StockAdjustmentManagementView.Designer.cs` | `.cs` | 2,822 B | 72 | WinForms Visual Designer Code | Active |
| `src/Clovent.Desktop/Inventory/Adjustments/StockAdjustmentManagementView.cs` | `StockAdjustmentManagementView.cs` | `.cs` | 4,198 B | 102 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Inventory/Transactions/InventoryTransactionsView.Designer.cs` | `InventoryTransactionsView.Designer.cs` | `.cs` | 2,922 B | 74 | WinForms Visual Designer Code | Active |
| `src/Clovent.Desktop/Inventory/Transactions/InventoryTransactionsView.cs` | `InventoryTransactionsView.cs` | `.cs` | 5,201 B | 120 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Inventory/Transfers/StockTransferCreateForm.Designer.cs` | `StockTransferCreateForm.Designer.cs` | `.cs` | 5,521 B | 131 | WinForms Visual Designer Code | Active |
| `src/Clovent.Desktop/Inventory/Transfers/StockTransferCreateForm.cs` | `StockTransferCreateForm.cs` | `.cs` | 3,510 B | 89 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Inventory/Transfers/StockTransferCreateForm.resx` | `StockTransferCreateForm.resx` | `.resx` | 5,817 B | 120 | WinForms Resource XML | Active |
| `src/Clovent.Desktop/Inventory/Transfers/StockTransferManagementView.Designer.cs` | `StockTransferManagementView.Designer.cs` | `.cs` | 2,078 B | 50 | WinForms Visual Designer Code | Active |
| `src/Clovent.Desktop/Inventory/Transfers/StockTransferManagementView.cs` | `StockTransferManagementView.cs` | `.cs` | 5,560 B | 121 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Inventory/WarehouseStocks/QuantityPromptForm.Designer.cs` | `QuantityPromptForm.Designer.cs` | `.cs` | 2,164 B | 64 | WinForms Visual Designer Code | Active |
| `src/Clovent.Desktop/Inventory/WarehouseStocks/QuantityPromptForm.cs` | `QuantityPromptForm.cs` | `.cs` | 1,585 B | 46 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Inventory/WarehouseStocks/QuantityPromptForm.resx` | `QuantityPromptForm.resx` | `.resx` | 5,817 B | 120 | WinForms Resource XML | Active |
| `src/Clovent.Desktop/Inventory/WarehouseStocks/ReceiveInventoryForm.Designer.cs` | `ReceiveInventoryForm.Designer.cs` | `.cs` | 5,473 B | 132 | WinForms Visual Designer Code | Active |
| `src/Clovent.Desktop/Inventory/WarehouseStocks/ReceiveInventoryForm.cs` | `ReceiveInventoryForm.cs` | `.cs` | 3,597 B | 91 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Inventory/WarehouseStocks/ReceiveInventoryForm.resx` | `ReceiveInventoryForm.resx` | `.resx` | 5,626 B | 120 | WinForms Resource XML | Active |
| `src/Clovent.Desktop/Inventory/WarehouseStocks/WarehouseStockEditForm.Designer.cs` | `WarehouseStockEditForm.Designer.cs` | `.cs` | 5,321 B | 124 | WinForms Visual Designer Code | Active |
| `src/Clovent.Desktop/Inventory/WarehouseStocks/WarehouseStockEditForm.cs` | `WarehouseStockEditForm.cs` | `.cs` | 3,503 B | 85 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Inventory/WarehouseStocks/WarehouseStockEditForm.resx` | `WarehouseStockEditForm.resx` | `.resx` | 5,626 B | 120 | WinForms Resource XML | Active |
| `src/Clovent.Desktop/Inventory/WarehouseStocks/WarehouseStockManagementView.Designer.cs` | `WarehouseStockManagementView.Designer.cs` | `.cs` | 4,284 B | 94 | WinForms Visual Designer Code | Active |
| `src/Clovent.Desktop/Inventory/WarehouseStocks/WarehouseStockManagementView.cs` | `WarehouseStockManagementView.cs` | `.cs` | 8,736 B | 198 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Login/ILoginService.cs` | `ILoginService.cs` | `.cs` | 1,867 B | 36 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Login/LoginService.cs` | `LoginService.cs` | `.cs` | 9,990 B | 218 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/MasterData/Branches/BranchEditForm.Designer.cs` | `BranchEditForm.Designer.cs` | `.cs` | 4,720 B | 121 | WinForms Visual Designer Code | Active |
| `src/Clovent.Desktop/MasterData/Branches/BranchEditForm.cs` | `BranchEditForm.cs` | `.cs` | 2,892 B | 71 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/MasterData/Branches/BranchEditForm.resx` | `BranchEditForm.resx` | `.resx` | 5,745 B | 120 | WinForms Resource XML | Active |
| `src/Clovent.Desktop/MasterData/Branches/BranchManagementView.Designer.cs` | `BranchManagementView.Designer.cs` | `.cs` | 2,067 B | 62 | WinForms Visual Designer Code | Active |
| `src/Clovent.Desktop/MasterData/Branches/BranchManagementView.cs` | `BranchManagementView.cs` | `.cs` | 3,891 B | 97 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/MasterData/ComboBoxBinder.cs` | `ComboBoxBinder.cs` | `.cs` | 2,898 B | 72 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/MasterData/Companies/CompanyEditForm.Designer.cs` | `CompanyEditForm.Designer.cs` | `.cs` | 3,104 B | 85 | WinForms Visual Designer Code | Active |
| `src/Clovent.Desktop/MasterData/Companies/CompanyEditForm.cs` | `CompanyEditForm.cs` | `.cs` | 1,753 B | 46 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/MasterData/Companies/CompanyEditForm.resx` | `CompanyEditForm.resx` | `.resx` | 5,627 B | 120 | WinForms Resource XML | Active |
| `src/Clovent.Desktop/MasterData/Companies/CompanyManagementView.Designer.cs` | `CompanyManagementView.Designer.cs` | `.cs` | 1,821 B | 52 | WinForms Visual Designer Code | Active |
| `src/Clovent.Desktop/MasterData/Companies/CompanyManagementView.cs` | `CompanyManagementView.cs` | `.cs` | 3,791 B | 96 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/MasterData/Currencies/CurrencyCreateForm.Designer.cs` | `CurrencyCreateForm.Designer.cs` | `.cs` | 5,431 B | 132 | WinForms Visual Designer Code | Active |
| `src/Clovent.Desktop/MasterData/Currencies/CurrencyCreateForm.cs` | `CurrencyCreateForm.cs` | `.cs` | 1,756 B | 57 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/MasterData/Currencies/CurrencyCreateForm.resx` | `CurrencyCreateForm.resx` | `.resx` | 5,627 B | 120 | WinForms Resource XML | Active |
| `src/Clovent.Desktop/MasterData/Currencies/CurrencyManagementView.Designer.cs` | `CurrencyManagementView.Designer.cs` | `.cs` | 1,834 B | 51 | WinForms Visual Designer Code | Active |
| `src/Clovent.Desktop/MasterData/Currencies/CurrencyManagementView.cs` | `CurrencyManagementView.cs` | `.cs` | 6,788 B | 161 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/MasterData/Departments/DepartmentEditForm.Designer.cs` | `DepartmentEditForm.Designer.cs` | `.cs` | 2,073 B | 63 | WinForms Visual Designer Code | Active |
| `src/Clovent.Desktop/MasterData/Departments/DepartmentEditForm.cs` | `DepartmentEditForm.cs` | `.cs` | 1,478 B | 44 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/MasterData/Departments/DepartmentEditForm.resx` | `DepartmentEditForm.resx` | `.resx` | 5,627 B | 120 | WinForms Resource XML | Active |
| `src/Clovent.Desktop/MasterData/Departments/DepartmentManagementView.Designer.cs` | `DepartmentManagementView.Designer.cs` | `.cs` | 1,795 B | 51 | WinForms Visual Designer Code | Active |
| `src/Clovent.Desktop/MasterData/Departments/DepartmentManagementView.cs` | `DepartmentManagementView.cs` | `.cs` | 3,596 B | 93 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/MasterData/EntityPicker.Designer.cs` | `EntityPicker.Designer.cs` | `.cs` | 3,411 B | 89 | WinForms Visual Designer Code | Active |
| `src/Clovent.Desktop/MasterData/EntityPicker.cs` | `EntityPicker.cs` | `.cs` | 6,154 B | 142 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/MasterData/FiscalYears/FiscalYearEditForm.Designer.cs` | `FiscalYearEditForm.Designer.cs` | `.cs` | 4,217 B | 107 | WinForms Visual Designer Code | Active |
| `src/Clovent.Desktop/MasterData/FiscalYears/FiscalYearEditForm.cs` | `FiscalYearEditForm.cs` | `.cs` | 2,887 B | 71 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/MasterData/FiscalYears/FiscalYearEditForm.resx` | `FiscalYearEditForm.resx` | `.resx` | 5,627 B | 120 | WinForms Resource XML | Active |
| `src/Clovent.Desktop/MasterData/FiscalYears/FiscalYearManagementView.Designer.cs` | `FiscalYearManagementView.Designer.cs` | `.cs` | 1,829 B | 52 | WinForms Visual Designer Code | Active |
| `src/Clovent.Desktop/MasterData/FiscalYears/FiscalYearManagementView.cs` | `FiscalYearManagementView.cs` | `.cs` | 3,906 B | 96 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/MasterData/MasterDataColumn.cs` | `MasterDataColumn.cs` | `.cs` | 656 B | 12 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/MasterData/MasterDataEditFormBase.Designer.cs` | `MasterDataEditFormBase.Designer.cs` | `.cs` | 4,824 B | 121 | WinForms Visual Designer Code | Active |
| `src/Clovent.Desktop/MasterData/MasterDataEditFormBase.cs` | `MasterDataEditFormBase.cs` | `.cs` | 14,130 B | 322 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/MasterData/MasterDataEditFormBase.resx` | `MasterDataEditFormBase.resx` | `.resx` | 5,817 B | 120 | WinForms Resource XML | Active |
| `src/Clovent.Desktop/MasterData/MasterDataFilter.cs` | `MasterDataFilter.cs` | `.cs` | 2,286 B | 40 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/MasterData/MasterDataListView.cs` | `MasterDataListView.cs` | `.cs` | 31,638 B | 685 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/MasterData/OrganizationHierarchySelector.Designer.cs` | `OrganizationHierarchySelector.Designer.cs` | `.cs` | 4,522 B | 110 | WinForms Visual Designer Code | Active |
| `src/Clovent.Desktop/MasterData/OrganizationHierarchySelector.cs` | `OrganizationHierarchySelector.cs` | `.cs` | 7,492 B | 194 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/MasterData/Organizations/OrganizationEditForm.Designer.cs` | `OrganizationEditForm.Designer.cs` | `.cs` | 3,118 B | 85 | WinForms Visual Designer Code | Active |
| `src/Clovent.Desktop/MasterData/Organizations/OrganizationEditForm.cs` | `OrganizationEditForm.cs` | `.cs` | 1,815 B | 48 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/MasterData/Organizations/OrganizationEditForm.resx` | `OrganizationEditForm.resx` | `.resx` | 5,627 B | 120 | WinForms Resource XML | Active |
| `src/Clovent.Desktop/MasterData/Organizations/OrganizationManagementView.Designer.cs` | `OrganizationManagementView.Designer.cs` | `.cs` | 1,616 B | 47 | WinForms Visual Designer Code | Active |
| `src/Clovent.Desktop/MasterData/Organizations/OrganizationManagementView.cs` | `OrganizationManagementView.cs` | `.cs` | 3,287 B | 82 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/MasterData/Settings/BusinessSettingsManagementView.Designer.cs` | `BusinessSettingsManagementView.Designer.cs` | `.cs` | 6,579 B | 160 | WinForms Visual Designer Code | Active |
| `src/Clovent.Desktop/MasterData/Settings/BusinessSettingsManagementView.cs` | `BusinessSettingsManagementView.cs` | `.cs` | 11,928 B | 299 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/MasterData/Terminals/TerminalEditForm.Designer.cs` | `TerminalEditForm.Designer.cs` | `.cs` | 3,094 B | 85 | WinForms Visual Designer Code | Active |
| `src/Clovent.Desktop/MasterData/Terminals/TerminalEditForm.cs` | `TerminalEditForm.cs` | `.cs` | 2,232 B | 64 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/MasterData/Terminals/TerminalEditForm.resx` | `TerminalEditForm.resx` | `.resx` | 5,627 B | 120 | WinForms Resource XML | Active |
| `src/Clovent.Desktop/MasterData/Terminals/TerminalManagementView.Designer.cs` | `TerminalManagementView.Designer.cs` | `.cs` | 1,828 B | 52 | WinForms Visual Designer Code | Active |
| `src/Clovent.Desktop/MasterData/Terminals/TerminalManagementView.cs` | `TerminalManagementView.cs` | `.cs` | 3,530 B | 93 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/MasterData/Warehouses/WarehouseEditForm.Designer.cs` | `WarehouseEditForm.Designer.cs` | `.cs` | 3,097 B | 85 | WinForms Visual Designer Code | Active |
| `src/Clovent.Desktop/MasterData/Warehouses/WarehouseEditForm.cs` | `WarehouseEditForm.cs` | `.cs` | 2,313 B | 65 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/MasterData/Warehouses/WarehouseEditForm.resx` | `WarehouseEditForm.resx` | `.resx` | 5,627 B | 120 | WinForms Resource XML | Active |
| `src/Clovent.Desktop/MasterData/Warehouses/WarehouseManagementView.Designer.cs` | `WarehouseManagementView.Designer.cs` | `.cs` | 1,839 B | 52 | WinForms Visual Designer Code | Active |
| `src/Clovent.Desktop/MasterData/Warehouses/WarehouseManagementView.cs` | `WarehouseManagementView.cs` | `.cs` | 3,553 B | 93 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Modules/DesktopModuleCatalog.cs` | `DesktopModuleCatalog.cs` | `.cs` | 881 B | 19 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Modules/DesktopModuleLoader.cs` | `DesktopModuleLoader.cs` | `.cs` | 2,019 B | 42 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Navigation/INavigationService.cs` | `INavigationService.cs` | `.cs` | 2,050 B | 41 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Navigation/NavigationMenuBuilder.cs` | `NavigationMenuBuilder.cs` | `.cs` | 1,198 B | 29 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Navigation/NavigationService.cs` | `NavigationService.cs` | `.cs` | 3,133 B | 78 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Notifications/INotificationService.cs` | `INotificationService.cs` | `.cs` | 653 B | 17 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Notifications/Notification.cs` | `Notification.cs` | `.cs` | 228 B | 4 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Notifications/NotificationService.cs` | `NotificationService.cs` | `.cs` | 939 B | 30 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Notifications/NotificationsForm.Designer.cs` | `NotificationsForm.Designer.cs` | `.cs` | 1,535 B | 52 | WinForms Visual Designer Code | Active |
| `src/Clovent.Desktop/Notifications/NotificationsForm.cs` | `NotificationsForm.cs` | `.cs` | 1,369 B | 32 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Notifications/NotificationsForm.resx` | `NotificationsForm.resx` | `.resx` | 5,627 B | 120 | WinForms Resource XML | Active |
| `src/Clovent.Desktop/Program.cs` | `Program.cs` | `.cs` | 17,779 B | 286 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Program.cs.backup-orderhistory-20260817-124222` | `Program.cs.backup-orderhistory-20260817-124222` | `.backup-orderhistory-20260817-124222` | 15,852 B | 0 | Timestamped Snapshot Backup | Duplicate / Obsolete |
| `src/Clovent.Desktop/Properties/launchSettings.json` | `launchSettings.json` | `.json` | 249 B | 11 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Restaurant/Customers/CustomerEditForm.Designer.cs` | `CustomerEditForm.Designer.cs` | `.cs` | 14,589 B | 309 | WinForms Visual Designer Code | Active |
| `src/Clovent.Desktop/Restaurant/Customers/CustomerEditForm.cs` | `CustomerEditForm.cs` | `.cs` | 5,553 B | 146 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Restaurant/Customers/CustomerEditForm.resx` | `CustomerEditForm.resx` | `.resx` | 2,794 B | 61 | WinForms Resource XML | Active |
| `src/Clovent.Desktop/Restaurant/Customers/CustomerLedgerDialog.Designer.cs` | `CustomerLedgerDialog.Designer.cs` | `.cs` | 21,323 B | 453 | WinForms Visual Designer Code | Active |
| `src/Clovent.Desktop/Restaurant/Customers/CustomerLedgerDialog.cs` | `CustomerLedgerDialog.cs` | `.cs` | 9,934 B | 251 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Restaurant/Customers/CustomerLedgerDialog.resx` | `CustomerLedgerDialog.resx` | `.resx` | 2,794 B | 61 | WinForms Resource XML | Active |
| `src/Clovent.Desktop/Restaurant/Customers/CustomerPaymentForm.Designer.cs` | `CustomerPaymentForm.Designer.cs` | `.cs` | 10,600 B | 228 | WinForms Visual Designer Code | Active |
| `src/Clovent.Desktop/Restaurant/Customers/CustomerPaymentForm.cs` | `CustomerPaymentForm.cs` | `.cs` | 5,446 B | 132 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Restaurant/Customers/CustomerPaymentForm.resx` | `CustomerPaymentForm.resx` | `.resx` | 5,817 B | 120 | WinForms Resource XML | Active |
| `src/Clovent.Desktop/Restaurant/Customers/CustomersView.Designer.cs` | `CustomersView.Designer.cs` | `.cs` | 16,258 B | 332 | WinForms Visual Designer Code | Active |
| `src/Clovent.Desktop/Restaurant/Customers/CustomersView.cs` | `CustomersView.cs` | `.cs` | 26,045 B | 641 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Restaurant/Customers/CustomersView.resx` | `CustomersView.resx` | `.resx` | 2,794 B | 61 | WinForms Resource XML | Active |
| `src/Clovent.Desktop/Restaurant/DiningAreas/DiningAreaEditForm.Designer.cs` | `DiningAreaEditForm.Designer.cs` | `.cs` | 2,060 B | 65 | WinForms Visual Designer Code | Active |
| `src/Clovent.Desktop/Restaurant/DiningAreas/DiningAreaEditForm.cs` | `DiningAreaEditForm.cs` | `.cs` | 1,510 B | 42 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Restaurant/DiningAreas/DiningAreaEditForm.resx` | `DiningAreaEditForm.resx` | `.resx` | 5,627 B | 120 | WinForms Resource XML | Active |
| `src/Clovent.Desktop/Restaurant/DiningAreas/DiningAreaManagementView.Designer.cs` | `DiningAreaManagementView.Designer.cs` | `.cs` | 2,637 B | 70 | WinForms Visual Designer Code | Active |
| `src/Clovent.Desktop/Restaurant/DiningAreas/DiningAreaManagementView.cs` | `DiningAreaManagementView.cs` | `.cs` | 4,099 B | 102 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Restaurant/EndOfDay/EndOfDayReportView.Designer.cs` | `EndOfDayReportView.Designer.cs` | `.cs` | 21,770 B | 450 | WinForms Visual Designer Code | Active |
| `src/Clovent.Desktop/Restaurant/EndOfDay/EndOfDayReportView.cs` | `EndOfDayReportView.cs` | `.cs` | 12,636 B | 260 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Restaurant/Orders/BillSplitDialog.Designer.cs` | `BillSplitDialog.Designer.cs` | `.cs` | 3,298 B | 88 | WinForms Visual Designer Code | Active |
| `src/Clovent.Desktop/Restaurant/Orders/BillSplitDialog.cs` | `BillSplitDialog.cs` | `.cs` | 2,903 B | 79 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Restaurant/Orders/BillSplitDialog.resx` | `BillSplitDialog.resx` | `.resx` | 5,627 B | 120 | WinForms Resource XML | Active |
| `src/Clovent.Desktop/Restaurant/Orders/CustomerReorderDialogs.cs` | `CustomerReorderDialogs.cs` | `.cs` | 13,948 B | 355 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Restaurant/Orders/DiscountDialog.Designer.cs` | `DiscountDialog.Designer.cs` | `.cs` | 4,503 B | 115 | WinForms Visual Designer Code | Active |
| `src/Clovent.Desktop/Restaurant/Orders/DiscountDialog.cs` | `DiscountDialog.cs` | `.cs` | 2,427 B | 73 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Restaurant/Orders/DiscountDialog.resx` | `DiscountDialog.resx` | `.resx` | 5,627 B | 120 | WinForms Resource XML | Active |
| `src/Clovent.Desktop/Restaurant/Orders/HoldOrdersView.Designer.cs` | `HoldOrdersView.Designer.cs` | `.cs` | 1,709 B | 48 | WinForms Visual Designer Code | Active |
| `src/Clovent.Desktop/Restaurant/Orders/HoldOrdersView.cs` | `HoldOrdersView.cs` | `.cs` | 4,609 B | 119 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Restaurant/Orders/KitchenTicketViewerView.Designer.cs` | `KitchenTicketViewerView.Designer.cs` | `.cs` | 2,307 B | 54 | WinForms Visual Designer Code | Active |
| `src/Clovent.Desktop/Restaurant/Orders/KitchenTicketViewerView.cs` | `KitchenTicketViewerView.cs` | `.cs` | 5,355 B | 142 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Restaurant/Orders/MergeTablesDialog.Designer.cs` | `MergeTablesDialog.Designer.cs` | `.cs` | 3,185 B | 85 | WinForms Visual Designer Code | Active |
| `src/Clovent.Desktop/Restaurant/Orders/MergeTablesDialog.cs` | `MergeTablesDialog.cs` | `.cs` | 2,434 B | 64 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Restaurant/Orders/MergeTablesDialog.resx` | `MergeTablesDialog.resx` | `.resx` | 5,627 B | 120 | WinForms Resource XML | Active |
| `src/Clovent.Desktop/Restaurant/Orders/OrderHistoryRules.cs` | `OrderHistoryRules.cs` | `.cs` | 1,538 B | 31 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Restaurant/Orders/OrderHistoryView.Designer.cs` | `OrderHistoryView.Designer.cs` | `.cs` | 2,117 B | 54 | WinForms Visual Designer Code | Active |
| `src/Clovent.Desktop/Restaurant/Orders/OrderHistoryView.cs` | `OrderHistoryView.cs` | `.cs` | 8,553 B | 211 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Restaurant/Orders/PaymentHistoryDialog.Designer.cs` | `PaymentHistoryDialog.Designer.cs` | `.cs` | 4,720 B | 110 | WinForms Visual Designer Code | Active |
| `src/Clovent.Desktop/Restaurant/Orders/PaymentHistoryDialog.cs` | `PaymentHistoryDialog.cs` | `.cs` | 6,113 B | 144 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Restaurant/Orders/PaymentHistoryDialog.resx` | `PaymentHistoryDialog.resx` | `.resx` | 5,817 B | 120 | WinForms Resource XML | Active |
| `src/Clovent.Desktop/Restaurant/Orders/PosPaymentMethodPreferenceStore.cs` | `PosPaymentMethodPreferenceStore.cs` | `.cs` | 1,915 B | 47 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Restaurant/Orders/PosPaymentRules.cs` | `PosPaymentRules.cs` | `.cs` | 2,456 B | 53 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Restaurant/Orders/PriceOverrideDialog.Designer.cs` | `PriceOverrideDialog.Designer.cs` | `.cs` | 3,909 B | 106 | WinForms Visual Designer Code | Active |
| `src/Clovent.Desktop/Restaurant/Orders/PriceOverrideDialog.cs` | `PriceOverrideDialog.cs` | `.cs` | 2,523 B | 60 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Restaurant/Orders/PriceOverrideDialog.resx` | `PriceOverrideDialog.resx` | `.resx` | 5,627 B | 120 | WinForms Resource XML | Active |
| `src/Clovent.Desktop/Restaurant/Orders/QuickOrderPreviewDialog.cs` | `QuickOrderPreviewDialog.cs` | `.cs` | 11,136 B | 240 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Restaurant/Orders/RecallOrderDialog.Designer.cs` | `RecallOrderDialog.Designer.cs` | `.cs` | 43,437 B | 945 | WinForms Visual Designer Code | Active |
| `src/Clovent.Desktop/Restaurant/Orders/RecallOrderDialog.cs` | `RecallOrderDialog.cs` | `.cs` | 32,458 B | 785 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Restaurant/Orders/ReceiptFormatter.cs` | `ReceiptFormatter.cs` | `.cs` | 4,035 B | 95 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Restaurant/Orders/ReceiptPreviewForm.Designer.cs` | `ReceiptPreviewForm.Designer.cs` | `.cs` | 3,922 B | 110 | WinForms Visual Designer Code | Active |
| `src/Clovent.Desktop/Restaurant/Orders/ReceiptPreviewForm.cs` | `ReceiptPreviewForm.cs` | `.cs` | 2,682 B | 73 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Restaurant/Orders/ReceiptPreviewForm.resx` | `ReceiptPreviewForm.resx` | `.resx` | 5,745 B | 120 | WinForms Resource XML | Active |
| `src/Clovent.Desktop/Restaurant/Orders/ReceiptPrintDocument.cs` | `ReceiptPrintDocument.cs` | `.cs` | 1,814 B | 59 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Restaurant/Orders/RestaurantPosDesignDataProvider.cs` | `RestaurantPosDesignDataProvider.cs` | `.cs` | 4,341 B | 121 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Restaurant/Orders/RestaurantPosForm.Designer.cs` | `RestaurantPosForm.Designer.cs` | `.cs` | 107,444 B | 2291 | WinForms Visual Designer Code | Active |
| `src/Clovent.Desktop/Restaurant/Orders/RestaurantPosForm.Designer.cs.backup-20260816` | `RestaurantPosForm.Designer.cs.backup-20260816` | `.backup-20260816` | 104,146 B | 0 | Timestamped Snapshot Backup | Duplicate / Obsolete |
| `src/Clovent.Desktop/Restaurant/Orders/RestaurantPosForm.Designer.cs.backup-20260816-2200` | `RestaurantPosForm.Designer.cs.backup-20260816-2200` | `.backup-20260816-2200` | 108,566 B | 0 | Timestamped Snapshot Backup | Duplicate / Obsolete |
| `src/Clovent.Desktop/Restaurant/Orders/RestaurantPosForm.Designer.cs.backup-barcodefix-20260818-102021` | `RestaurantPosForm.Designer.cs.backup-barcodefix-20260818-102021` | `.backup-barcodefix-20260818-102021` | 109,384 B | 0 | Timestamped Snapshot Backup | Duplicate / Obsolete |
| `src/Clovent.Desktop/Restaurant/Orders/RestaurantPosForm.Designer.cs.backup-captions-20260817-162651` | `RestaurantPosForm.Designer.cs.backup-captions-20260817-162651` | `.backup-captions-20260817-162651` | 109,286 B | 0 | Timestamped Snapshot Backup | Duplicate / Obsolete |
| `src/Clovent.Desktop/Restaurant/Orders/RestaurantPosForm.Designer.cs.backup-cleanup-20260817-153334` | `RestaurantPosForm.Designer.cs.backup-cleanup-20260817-153334` | `.backup-cleanup-20260817-153334` | 110,163 B | 0 | Timestamped Snapshot Backup | Duplicate / Obsolete |
| `src/Clovent.Desktop/Restaurant/Orders/RestaurantPosForm.Designer.cs.backup-parenting-20260817-160507` | `RestaurantPosForm.Designer.cs.backup-parenting-20260817-160507` | `.backup-parenting-20260817-160507` | 109,168 B | 0 | Timestamped Snapshot Backup | Duplicate / Obsolete |
| `src/Clovent.Desktop/Restaurant/Orders/RestaurantPosForm.Designer.cs.backup-pre-leafautosize` | `RestaurantPosForm.Designer.cs.backup-pre-leafautosize` | `.backup-pre-leafautosize` | 105,828 B | 0 | Timestamped Snapshot Backup | Duplicate / Obsolete |
| `src/Clovent.Desktop/Restaurant/Orders/RestaurantPosForm.Designer.cs.backup-readiness-20260818` | `RestaurantPosForm.Designer.cs.backup-readiness-20260818` | `.backup-readiness-20260818` | 107,307 B | 0 | Timestamped Snapshot Backup | Duplicate / Obsolete |
| `src/Clovent.Desktop/Restaurant/Orders/RestaurantPosForm.Designer.cs.backup-runtime-fix` | `RestaurantPosForm.Designer.cs.backup-runtime-fix` | `.backup-runtime-fix` | 107,710 B | 0 | Timestamped Snapshot Backup | Duplicate / Obsolete |
| `src/Clovent.Desktop/Restaurant/Orders/RestaurantPosForm.Designer.cs.backup-ui-fixes-20260818` | `RestaurantPosForm.Designer.cs.backup-ui-fixes-20260818` | `.backup-ui-fixes-20260818` | 107,208 B | 0 | Timestamped Snapshot Backup | Duplicate / Obsolete |
| `src/Clovent.Desktop/Restaurant/Orders/RestaurantPosForm.cs` | `RestaurantPosForm.cs` | `.cs` | 328,398 B | 7598 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Restaurant/Orders/RestaurantPosForm.cs.backup-castfix-20260818-093351` | `RestaurantPosForm.cs.backup-castfix-20260818-093351` | `.backup-castfix-20260818-093351` | 87,595 B | 0 | Timestamped Snapshot Backup | Duplicate / Obsolete |
| `src/Clovent.Desktop/Restaurant/Orders/RestaurantPosForm.cs.backup-cleanup-20260817-153334` | `RestaurantPosForm.cs.backup-cleanup-20260817-153334` | `.backup-cleanup-20260817-153334` | 89,146 B | 0 | Timestamped Snapshot Backup | Duplicate / Obsolete |
| `src/Clovent.Desktop/Restaurant/Orders/RestaurantPosForm.cs.backup-parenting-20260817-161245` | `RestaurantPosForm.cs.backup-parenting-20260817-161245` | `.backup-parenting-20260817-161245` | 87,233 B | 0 | Timestamped Snapshot Backup | Duplicate / Obsolete |
| `src/Clovent.Desktop/Restaurant/Orders/RestaurantPosForm.cs.backup-paymentfix-20260817-104258` | `RestaurantPosForm.cs.backup-paymentfix-20260817-104258` | `.backup-paymentfix-20260817-104258` | 87,861 B | 0 | Timestamped Snapshot Backup | Duplicate / Obsolete |
| `src/Clovent.Desktop/Restaurant/Orders/RestaurantPosForm.cs.backup-readiness-20260818` | `RestaurantPosForm.cs.backup-readiness-20260818` | `.backup-readiness-20260818` | 89,472 B | 0 | Timestamped Snapshot Backup | Duplicate / Obsolete |
| `src/Clovent.Desktop/Restaurant/Orders/RestaurantPosForm.cs.backup-ui-fixes-20260818` | `RestaurantPosForm.cs.backup-ui-fixes-20260818` | `.backup-ui-fixes-20260818` | 89,702 B | 0 | Timestamped Snapshot Backup | Duplicate / Obsolete |
| `src/Clovent.Desktop/Restaurant/Orders/RestaurantPosForm.resx` | `RestaurantPosForm.resx` | `.resx` | 6,330 B | 129 | WinForms Resource XML | Active |
| `src/Clovent.Desktop/Restaurant/Orders/RestaurantPulseForm.cs` | `RestaurantPulseForm.cs` | `.cs` | 11,964 B | 301 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Restaurant/Orders/RunningOrdersView.Designer.cs` | `RunningOrdersView.Designer.cs` | `.cs` | 2,016 B | 51 | WinForms Visual Designer Code | Active |
| `src/Clovent.Desktop/Restaurant/Orders/RunningOrdersView.cs` | `RunningOrdersView.cs` | `.cs` | 5,139 B | 129 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Restaurant/Orders/ServiceChargeDialog.Designer.cs` | `ServiceChargeDialog.Designer.cs` | `.cs` | 4,411 B | 116 | WinForms Visual Designer Code | Active |
| `src/Clovent.Desktop/Restaurant/Orders/ServiceChargeDialog.cs` | `ServiceChargeDialog.cs` | `.cs` | 2,489 B | 69 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Restaurant/Orders/ServiceChargeDialog.resx` | `ServiceChargeDialog.resx` | `.resx` | 5,627 B | 120 | WinForms Resource XML | Active |
| `src/Clovent.Desktop/Restaurant/Orders/SmartPosState.cs` | `SmartPosState.cs` | `.cs` | 4,364 B | 138 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Restaurant/Orders/SplitPaymentDialog.cs` | `SplitPaymentDialog.cs` | `.cs` | 10,070 B | 235 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Restaurant/Orders/TablePickerEdit.cs` | `TablePickerEdit.cs` | `.cs` | 5,661 B | 165 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Restaurant/Orders/TableSelectionDineInPolicy.cs` | `TableSelectionDineInPolicy.cs` | `.cs` | 701 B | 15 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Restaurant/Orders/TableTransferDialog.Designer.cs` | `TableTransferDialog.Designer.cs` | `.cs` | 2,034 B | 67 | WinForms Visual Designer Code | Active |
| `src/Clovent.Desktop/Restaurant/Orders/TableTransferDialog.cs` | `TableTransferDialog.cs` | `.cs` | 1,733 B | 49 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Restaurant/Orders/TableTransferDialog.resx` | `TableTransferDialog.resx` | `.resx` | 5,745 B | 120 | WinForms Resource XML | Active |
| `src/Clovent.Desktop/Restaurant/Orders/UniversalSearchDropdown.cs` | `UniversalSearchDropdown.cs` | `.cs` | 9,340 B | 291 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Restaurant/Shared/ManagerAuthorizationForm.Designer.cs` | `ManagerAuthorizationForm.Designer.cs` | `.cs` | 4,591 B | 109 | WinForms Visual Designer Code | Active |
| `src/Clovent.Desktop/Restaurant/Shared/ManagerAuthorizationForm.cs` | `ManagerAuthorizationForm.cs` | `.cs` | 2,823 B | 73 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Restaurant/Shared/SelectionPromptForm.Designer.cs` | `SelectionPromptForm.Designer.cs` | `.cs` | 2,014 B | 61 | WinForms Visual Designer Code | Active |
| `src/Clovent.Desktop/Restaurant/Shared/SelectionPromptForm.cs` | `SelectionPromptForm.cs` | `.cs` | 1,329 B | 39 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Restaurant/Shared/SelectionPromptForm.resx` | `SelectionPromptForm.resx` | `.resx` | 5,627 B | 120 | WinForms Resource XML | Active |
| `src/Clovent.Desktop/Restaurant/Shared/TextPromptForm.Designer.cs` | `TextPromptForm.Designer.cs` | `.cs` | 2,060 B | 64 | WinForms Visual Designer Code | Active |
| `src/Clovent.Desktop/Restaurant/Shared/TextPromptForm.cs` | `TextPromptForm.cs` | `.cs` | 2,251 B | 60 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Restaurant/Shared/TextPromptForm.resx` | `TextPromptForm.resx` | `.resx` | 5,627 B | 120 | WinForms Resource XML | Active |
| `src/Clovent.Desktop/Restaurant/Shifts/CashMovementDialog.cs` | `CashMovementDialog.cs` | `.cs` | 6,694 B | 194 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Restaurant/Shifts/CloseShiftDialog.cs` | `CloseShiftDialog.cs` | `.cs` | 13,718 B | 347 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Restaurant/Shifts/OpenShiftDialog.cs` | `OpenShiftDialog.cs` | `.cs` | 6,239 B | 195 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Restaurant/Shifts/ShiftDetailDialog.cs` | `ShiftDetailDialog.cs` | `.cs` | 8,849 B | 215 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Restaurant/Shifts/ShiftHistoryView.cs` | `ShiftHistoryView.cs` | `.cs` | 10,815 B | 299 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Restaurant/SmartPos/OrderHealthSettingsForm.cs` | `OrderHealthSettingsForm.cs` | `.cs` | 4,010 B | 107 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Restaurant/SmartPos/QuickOrderTemplateEditForm.cs` | `QuickOrderTemplateEditForm.cs` | `.cs` | 14,003 B | 370 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Restaurant/SmartPos/QuickOrderTemplatesView.Designer.cs` | `QuickOrderTemplatesView.Designer.cs` | `.cs` | 6,919 B | 170 | WinForms Visual Designer Code | Active |
| `src/Clovent.Desktop/Restaurant/SmartPos/QuickOrderTemplatesView.cs` | `QuickOrderTemplatesView.cs` | `.cs` | 11,771 B | 299 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Restaurant/SmartPos/RecommendationRuleEditForm.cs` | `RecommendationRuleEditForm.cs` | `.cs` | 9,570 B | 239 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Restaurant/SmartPos/RecommendationRulesView.Designer.cs` | `RecommendationRulesView.Designer.cs` | `.cs` | 6,945 B | 171 | WinForms Visual Designer Code | Active |
| `src/Clovent.Desktop/Restaurant/SmartPos/RecommendationRulesView.cs` | `RecommendationRulesView.cs` | `.cs` | 13,646 B | 344 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Restaurant/SmartPos/SmartPosCatalogOptions.cs` | `SmartPosCatalogOptions.cs` | `.cs` | 3,619 B | 73 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Restaurant/Tables/TableEditForm.Designer.cs` | `TableEditForm.Designer.cs` | `.cs` | 4,598 B | 114 | WinForms Visual Designer Code | Active |
| `src/Clovent.Desktop/Restaurant/Tables/TableEditForm.cs` | `TableEditForm.cs` | `.cs` | 2,694 B | 75 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Restaurant/Tables/TableEditForm.resx` | `TableEditForm.resx` | `.resx` | 5,627 B | 120 | WinForms Resource XML | Active |
| `src/Clovent.Desktop/Restaurant/Tables/TableManagementView.Designer.cs` | `TableManagementView.Designer.cs` | `.cs` | 3,756 B | 84 | WinForms Visual Designer Code | Active |
| `src/Clovent.Desktop/Restaurant/Tables/TableManagementView.cs` | `TableManagementView.cs` | `.cs` | 4,168 B | 104 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Seed/DevelopmentAuthorizationSeedStartupTask.cs` | `DevelopmentAuthorizationSeedStartupTask.cs` | `.cs` | 9,491 B | 226 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Seed/DevelopmentAuthorizationSeedStartupTask.cs.backup-orderhistory-20260817-124222` | `DevelopmentAuthorizationSeedStartupTask.cs.backup-orderhistory-20260817-124222` | `.backup-orderhistory-20260817-124222` | 8,672 B | 0 | Timestamped Snapshot Backup | Duplicate / Obsolete |
| `src/Clovent.Desktop/Seed/DevelopmentCatalogSeedStartupTask.cs` | `DevelopmentCatalogSeedStartupTask.cs` | `.cs` | 23,834 B | 541 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Seed/DevelopmentMasterDataSeedStartupTask.cs` | `DevelopmentMasterDataSeedStartupTask.cs` | `.cs` | 7,375 B | 147 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Seed/DevelopmentRestaurantSeedStartupTask.cs` | `DevelopmentRestaurantSeedStartupTask.cs` | `.cs` | 3,877 B | 90 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Seed/DevelopmentUserSeedStartupTask.cs` | `DevelopmentUserSeedStartupTask.cs` | `.cs` | 4,271 B | 102 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Seed/WorldCurrencySeedStartupTask.cs` | `WorldCurrencySeedStartupTask.cs` | `.cs` | 9,098 B | 206 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Seed/WorldLanguageSeedStartupTask.cs` | `WorldLanguageSeedStartupTask.cs` | `.cs` | 1,616 B | 49 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Seed/WorldTimeZoneSeedStartupTask.cs` | `WorldTimeZoneSeedStartupTask.cs` | `.cs` | 1,777 B | 50 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Sessions/CurrentSession.cs` | `CurrentSession.cs` | `.cs` | 1,174 B | 42 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Sessions/ICurrentSession.cs` | `ICurrentSession.cs` | `.cs` | 1,760 B | 35 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Shared/CsvFile.cs` | `CsvFile.cs` | `.cs` | 3,319 B | 91 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Shell/IRecentItemsService.cs` | `IRecentItemsService.cs` | `.cs` | 1,341 B | 26 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Shell/RecentItemsService.cs` | `RecentItemsService.cs` | `.cs` | 1,229 B | 35 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Startup/ErrorDialogForm.Designer.cs` | `ErrorDialogForm.Designer.cs` | `.cs` | 4,236 B | 116 | WinForms Visual Designer Code | Active |
| `src/Clovent.Desktop/Startup/ErrorDialogForm.cs` | `ErrorDialogForm.cs` | `.cs` | 2,549 B | 68 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Startup/ErrorDialogForm.resx` | `ErrorDialogForm.resx` | `.resx` | 5,627 B | 120 | WinForms Resource XML | Active |
| `src/Clovent.Desktop/Startup/ErrorDialogService.cs` | `ErrorDialogService.cs` | `.cs` | 2,550 B | 72 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Startup/FriendlyErrorText.cs` | `FriendlyErrorText.cs` | `.cs` | 3,575 B | 68 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Startup/GlobalExceptionHandler.cs` | `GlobalExceptionHandler.cs` | `.cs` | 1,931 B | 44 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Startup/IErrorDialogService.cs` | `IErrorDialogService.cs` | `.cs` | 549 B | 10 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Startup/ISplashScreenService.cs` | `ISplashScreenService.cs` | `.cs` | 547 B | 14 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Startup/SplashScreenService.cs` | `SplashScreenService.cs` | `.cs` | 1,390 B | 42 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Theming/DesktopOptions.cs` | `DesktopOptions.cs` | `.cs` | 2,711 B | 61 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Theming/IThemeService.cs` | `IThemeService.cs` | `.cs` | 864 B | 21 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Theming/ThemeInitializationStartupTask.cs` | `ThemeInitializationStartupTask.cs` | `.cs` | 886 B | 25 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/Theming/ThemeService.cs` | `ThemeService.cs` | `.cs` | 1,787 B | 51 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/appsettings.Development.json` | `appsettings.Development.json` | `.json` | 235 B | 11 | Core POS / Desktop | Active |
| `src/Clovent.Desktop/appsettings.json` | `appsettings.json` | `.json` | 1,239 B | 33 | Core POS / Desktop | Active |

### 3.2 Restaurant Bounded Context (`src/Clovent.Restaurant*`)

| Project | Relative Path | Name | Size | Lines | Category / Layer | Status |
|---|---|---|---|---|---|---|
| `Clovent.Restaurant.Application.Tests` | `Clovent.Restaurant.Application.Tests.csproj` | `Clovent.Restaurant.Application.Tests.csproj` | 689 B | 0 | Unit / Integration Test | Active |
| `Clovent.Restaurant.Application.Tests` | `CustomerReorder/CustomerReorderHandlerTests.cs` | `CustomerReorderHandlerTests.cs` | 6,754 B | 146 | Unit / Integration Test | Active |
| `Clovent.Restaurant.Application.Tests` | `Customers/CustomerHandlerTests.cs` | `CustomerHandlerTests.cs` | 15,843 B | 367 | Unit / Integration Test | Active |
| `Clovent.Restaurant.Application.Tests` | `DiningAreas/DiningAreaHandlerTests.cs` | `DiningAreaHandlerTests.cs` | 3,652 B | 87 | Unit / Integration Test | Active |
| `Clovent.Restaurant.Application.Tests` | `Discounts/DiscountHandlerTests.cs` | `DiscountHandlerTests.cs` | 10,902 B | 222 | Unit / Integration Test | Active |
| `Clovent.Restaurant.Application.Tests` | `Discounts/DiscountHandlerTests.cs.backup-m4m5-20260817-121628` | `DiscountHandlerTests.cs.backup-m4m5-20260817-121628` | 2,727 B | 0 | Unit / Integration Test | Active |
| `Clovent.Restaurant.Application.Tests` | `EndOfDay/GetEndOfDayReportQueryHandlerTests.cs` | `GetEndOfDayReportQueryHandlerTests.cs` | 10,181 B | 206 | Unit / Integration Test | Active |
| `Clovent.Restaurant.Application.Tests` | `KitchenTickets/KitchenTicketHandlerTests.cs` | `KitchenTicketHandlerTests.cs` | 4,281 B | 97 | Unit / Integration Test | Active |
| `Clovent.Restaurant.Application.Tests` | `OrderHealth/OrderHealthEvaluatorTests.cs` | `OrderHealthEvaluatorTests.cs` | 2,698 B | 67 | Unit / Integration Test | Active |
| `Clovent.Restaurant.Application.Tests` | `OrderLines/OrderLineHandlerTests.cs` | `OrderLineHandlerTests.cs` | 5,362 B | 110 | Unit / Integration Test | Active |
| `Clovent.Restaurant.Application.Tests` | `Orders/CompleteOrderCommandHandlerTests.cs` | `CompleteOrderCommandHandlerTests.cs` | 21,909 B | 470 | Unit / Integration Test | Active |
| `Clovent.Restaurant.Application.Tests` | `Orders/CompleteOrderCommandHandlerTests.cs.backup-h2-20260817-113226` | `CompleteOrderCommandHandlerTests.cs.backup-h2-20260817-113226` | 4,927 B | 0 | Unit / Integration Test | Active |
| `Clovent.Restaurant.Application.Tests` | `Orders/CompleteOrderCommandHandlerTests.cs.backup-m4m5-20260817-121628` | `CompleteOrderCommandHandlerTests.cs.backup-m4m5-20260817-121628` | 17,912 B | 0 | Unit / Integration Test | Active |
| `Clovent.Restaurant.Application.Tests` | `Orders/GetOrderSummaryQueryHandlerTests.cs` | `GetOrderSummaryQueryHandlerTests.cs` | 1,878 B | 44 | Unit / Integration Test | Active |
| `Clovent.Restaurant.Application.Tests` | `Orders/MergeTablesCommandHandlerTests.cs` | `MergeTablesCommandHandlerTests.cs` | 3,298 B | 73 | Unit / Integration Test | Active |
| `Clovent.Restaurant.Application.Tests` | `Orders/OrderHandlerTests.cs` | `OrderHandlerTests.cs` | 21,269 B | 461 | Unit / Integration Test | Active |
| `Clovent.Restaurant.Application.Tests` | `Orders/OrderHandlerTests.cs.backup-m3-20260817-115714` | `OrderHandlerTests.cs.backup-m3-20260817-115714` | 9,004 B | 0 | Unit / Integration Test | Active |
| `Clovent.Restaurant.Application.Tests` | `Orders/OrderNumberSequenceHandlerTests.cs` | `OrderNumberSequenceHandlerTests.cs` | 2,223 B | 54 | Unit / Integration Test | Active |
| `Clovent.Restaurant.Application.Tests` | `Orders/OrderTotalsCalculatorTests.cs` | `OrderTotalsCalculatorTests.cs` | 5,603 B | 152 | Unit / Integration Test | Active |
| `Clovent.Restaurant.Application.Tests` | `Orders/SplitOrderCommandHandlerTests.cs` | `SplitOrderCommandHandlerTests.cs` | 3,387 B | 71 | Unit / Integration Test | Active |
| `Clovent.Restaurant.Application.Tests` | `Orders/TableSelectionDineInTests.cs` | `TableSelectionDineInTests.cs` | 8,127 B | 179 | Unit / Integration Test | Active |
| `Clovent.Restaurant.Application.Tests` | `Orders/TableSwitchingCommandHandlerTests.cs` | `TableSwitchingCommandHandlerTests.cs` | 13,946 B | 309 | Unit / Integration Test | Active |
| `Clovent.Restaurant.Application.Tests` | `PaymentMethods/PaymentMethodHandlerTests.cs` | `PaymentMethodHandlerTests.cs` | 2,456 B | 60 | Unit / Integration Test | Active |
| `Clovent.Restaurant.Application.Tests` | `Payments/PaymentHandlerTests.cs` | `PaymentHandlerTests.cs` | 51,219 B | 1082 | Unit / Integration Test | Active |
| `Clovent.Restaurant.Application.Tests` | `Payments/PaymentHandlerTests.cs.backup-orderhistory-20260817-124222` | `PaymentHandlerTests.cs.backup-orderhistory-20260817-124222` | 30,114 B | 0 | Unit / Integration Test | Active |
| `Clovent.Restaurant.Application.Tests` | `Payments/PaymentHandlerTests.cs.backup-paymentfix-20260817-104258` | `PaymentHandlerTests.cs.backup-paymentfix-20260817-104258` | 23,833 B | 0 | Unit / Integration Test | Active |
| `Clovent.Restaurant.Application.Tests` | `QuickOrderTemplates/DealPreviewTests.cs` | `DealPreviewTests.cs` | 11,695 B | 253 | Unit / Integration Test | Active |
| `Clovent.Restaurant.Application.Tests` | `QuickOrderTemplates/QuickOrderTemplateHandlerTests.cs` | `QuickOrderTemplateHandlerTests.cs` | 7,966 B | 178 | Unit / Integration Test | Active |
| `Clovent.Restaurant.Application.Tests` | `RestaurantPulse/RestaurantPulseCalculatorTests.cs` | `RestaurantPulseCalculatorTests.cs` | 5,097 B | 121 | Unit / Integration Test | Active |
| `Clovent.Restaurant.Application.Tests` | `ServiceCharges/ServiceChargeHandlerTests.cs` | `ServiceChargeHandlerTests.cs` | 2,617 B | 57 | Unit / Integration Test | Active |
| `Clovent.Restaurant.Application.Tests` | `Shifts/ShiftHandlerTests.cs` | `ShiftHandlerTests.cs` | 11,749 B | 222 | Unit / Integration Test | Active |
| `Clovent.Restaurant.Application.Tests` | `SmartRecommendations/GetBasketRecommendationsQueryHandlerTests.cs` | `GetBasketRecommendationsQueryHandlerTests.cs` | 8,214 B | 184 | Unit / Integration Test | Active |
| `Clovent.Restaurant.Application.Tests` | `SmartRecommendations/RecommendationRuleCrudTests.cs` | `RecommendationRuleCrudTests.cs` | 3,866 B | 91 | Unit / Integration Test | Active |
| `Clovent.Restaurant.Application.Tests` | `Tables/TableHandlerTests.cs` | `TableHandlerTests.cs` | 10,710 B | 243 | Unit / Integration Test | Active |
| `Clovent.Restaurant.Application.Tests` | `Tables/TableHandlerTests.cs.backup-vacateguard-20260817-135631` | `TableHandlerTests.cs.backup-vacateguard-20260817-135631` | 5,412 B | 0 | Unit / Integration Test | Active |
| `Clovent.Restaurant.Application.Tests` | `TestSupport/CatalogFakes.cs` | `CatalogFakes.cs` | 2,545 B | 46 | Unit / Integration Test | Active |
| `Clovent.Restaurant.Application.Tests` | `TestSupport/FakeActivityLogEntryRepository.cs` | `FakeActivityLogEntryRepository.cs` | 971 B | 28 | Unit / Integration Test | Active |
| `Clovent.Restaurant.Application.Tests` | `TestSupport/FakeCustomerLedgerEntryRepository.cs` | `FakeCustomerLedgerEntryRepository.cs` | 1,124 B | 28 | Unit / Integration Test | Active |
| `Clovent.Restaurant.Application.Tests` | `TestSupport/FakeCustomerRepository.cs` | `FakeCustomerRepository.cs` | 4,730 B | 104 | Unit / Integration Test | Active |
| `Clovent.Restaurant.Application.Tests` | `TestSupport/FakeDailySalesSequenceRepository.cs` | `FakeDailySalesSequenceRepository.cs` | 881 B | 20 | Unit / Integration Test | Active |
| `Clovent.Restaurant.Application.Tests` | `TestSupport/FakeDiningAreaRepository.cs` | `FakeDiningAreaRepository.cs` | 1,137 B | 26 | Unit / Integration Test | Active |
| `Clovent.Restaurant.Application.Tests` | `TestSupport/FakeDiscountRepository.cs` | `FakeDiscountRepository.cs` | 946 B | 23 | Unit / Integration Test | Active |
| `Clovent.Restaurant.Application.Tests` | `TestSupport/FakeKitchenTicketRepository.cs` | `FakeKitchenTicketRepository.cs` | 1,291 B | 26 | Unit / Integration Test | Active |
| `Clovent.Restaurant.Application.Tests` | `TestSupport/FakeMediator.cs` | `FakeMediator.cs` | 1,849 B | 37 | Unit / Integration Test | Active |
| `Clovent.Restaurant.Application.Tests` | `TestSupport/FakeOrderLineRepository.cs` | `FakeOrderLineRepository.cs` | 928 B | 23 | Unit / Integration Test | Active |
| `Clovent.Restaurant.Application.Tests` | `TestSupport/FakeOrderNumberSequenceRepository.cs` | `FakeOrderNumberSequenceRepository.cs` | 631 B | 19 | Unit / Integration Test | Active |
| `Clovent.Restaurant.Application.Tests` | `TestSupport/FakeOrderRepository.cs` | `FakeOrderRepository.cs` | 1,569 B | 32 | Unit / Integration Test | Active |
| `Clovent.Restaurant.Application.Tests` | `TestSupport/FakePaymentMethodRepository.cs` | `FakePaymentMethodRepository.cs` | 911 B | 22 | Unit / Integration Test | Active |
| `Clovent.Restaurant.Application.Tests` | `TestSupport/FakePaymentRepository.cs` | `FakePaymentRepository.cs` | 1,193 B | 26 | Unit / Integration Test | Active |
| `Clovent.Restaurant.Application.Tests` | `TestSupport/FakeQuickOrderTemplateRepository.cs` | `FakeQuickOrderTemplateRepository.cs` | 1,399 B | 31 | Unit / Integration Test | Active |
| `Clovent.Restaurant.Application.Tests` | `TestSupport/FakeRecommendationRuleRepository.cs` | `FakeRecommendationRuleRepository.cs` | 1,336 B | 31 | Unit / Integration Test | Active |
| `Clovent.Restaurant.Application.Tests` | `TestSupport/FakeServiceChargeRepository.cs` | `FakeServiceChargeRepository.cs` | 1,000 B | 23 | Unit / Integration Test | Active |
| `Clovent.Restaurant.Application.Tests` | `TestSupport/FakeShiftRepository.cs` | `FakeShiftRepository.cs` | 2,561 B | 68 | Unit / Integration Test | Active |
| `Clovent.Restaurant.Application.Tests` | `TestSupport/FakeTableRepository.cs` | `FakeTableRepository.cs` | 1,091 B | 26 | Unit / Integration Test | Active |
| `Clovent.Restaurant.Application.Tests` | `UniversalPosSearch/UniversalPosSearchQueryHandlerTests.cs` | `UniversalPosSearchQueryHandlerTests.cs` | 6,158 B | 144 | Unit / Integration Test | Active |
| `Clovent.Restaurant.Application` | `ActivityLogs/Commands/RecordActivityCommand.cs` | `RecordActivityCommand.cs` | 1,385 B | 29 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `ActivityLogs/Dtos/ActivityLogEntryDto.cs` | `ActivityLogEntryDto.cs` | 735 B | 22 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `ActivityLogs/Queries/ListRecentActivityQuery.cs` | `ListRecentActivityQuery.cs` | 1,000 B | 20 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `Clovent.Restaurant.Application.csproj` | `Clovent.Restaurant.Application.csproj` | 888 B | 0 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `CustomerReorder/Dtos/CustomerReorderDtos.cs` | `CustomerReorderDtos.cs` | 628 B | 17 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `CustomerReorder/Queries/GetCustomerFrequentProductsQuery.cs` | `GetCustomerFrequentProductsQuery.cs` | 3,142 B | 64 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `CustomerReorder/Queries/GetCustomerLastOrderQuery.cs` | `GetCustomerLastOrderQuery.cs` | 6,049 B | 129 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `Customers/Commands/CreateCustomerCommand.cs` | `CreateCustomerCommand.cs` | 3,836 B | 113 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `Customers/Commands/RecordCustomerPaymentCommand.cs` | `RecordCustomerPaymentCommand.cs` | 3,259 B | 75 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `Customers/Commands/SetCustomerStatusCommand.cs` | `SetCustomerStatusCommand.cs` | 1,281 B | 28 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `Customers/Commands/SetDefaultCustomerCommand.cs` | `SetDefaultCustomerCommand.cs` | 1,686 B | 39 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `Customers/Commands/UpdateCustomerCommand.cs` | `UpdateCustomerCommand.cs` | 2,370 B | 67 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `Customers/Dtos/CustomerDto.cs` | `CustomerDto.cs` | 1,369 B | 44 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `Customers/Dtos/CustomerLedgerEntryDto.cs` | `CustomerLedgerEntryDto.cs` | 788 B | 24 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `Customers/Queries/GetCustomerByCodeQuery.cs` | `GetCustomerByCodeQuery.cs` | 1,079 B | 30 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `Customers/Queries/GetCustomerByIdQuery.cs` | `GetCustomerByIdQuery.cs` | 908 B | 21 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `Customers/Queries/GetCustomerLedgerQuery.cs` | `GetCustomerLedgerQuery.cs` | 966 B | 20 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `Customers/Queries/GetDefaultCustomerQuery.cs` | `GetDefaultCustomerQuery.cs` | 953 B | 20 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `Customers/Queries/ListCustomersQuery.cs` | `ListCustomersQuery.cs` | 1,178 B | 28 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `DependencyInjection/ApplicationServiceCollectionExtensions.cs` | `ApplicationServiceCollectionExtensions.cs` | 1,191 B | 20 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `DiningAreas/Commands/ActivateDiningAreaCommand.cs` | `ActivateDiningAreaCommand.cs` | 955 B | 22 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `DiningAreas/Commands/CreateDiningAreaCommand.cs` | `CreateDiningAreaCommand.cs` | 1,004 B | 24 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `DiningAreas/Commands/DeactivateDiningAreaCommand.cs` | `DeactivateDiningAreaCommand.cs` | 969 B | 22 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `DiningAreas/Commands/RenameDiningAreaCommand.cs` | `RenameDiningAreaCommand.cs` | 1,040 B | 23 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `DiningAreas/Dtos/DiningAreaDto.cs` | `DiningAreaDto.cs` | 592 B | 11 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `DiningAreas/Queries/GetDiningAreaByIdQuery.cs` | `GetDiningAreaByIdQuery.cs` | 920 B | 21 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `DiningAreas/Queries/ListAllDiningAreasQuery.cs` | `ListAllDiningAreasQuery.cs` | 930 B | 20 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `DiningAreas/Queries/ListDiningAreasByBranchQuery.cs` | `ListDiningAreasByBranchQuery.cs` | 992 B | 21 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `Discounts/Commands/ApplyDiscountToOrderCommand.cs` | `ApplyDiscountToOrderCommand.cs` | 5,024 B | 102 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `Discounts/Commands/ApplyDiscountToOrderCommand.cs.backup-m4m5-20260817-121628` | `ApplyDiscountToOrderCommand.cs.backup-m4m5-20260817-121628` | 1,316 B | 0 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `Discounts/Commands/RemoveDiscountFromOrderCommand.cs` | `RemoveDiscountFromOrderCommand.cs` | 1,401 B | 29 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `Discounts/Dtos/DiscountDto.cs` | `DiscountDto.cs` | 639 B | 11 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `Discounts/Queries/GetDiscountByIdQuery.cs` | `GetDiscountByIdQuery.cs` | 889 B | 21 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `Discounts/Queries/ListDiscountsByOrderQuery.cs` | `ListDiscountsByOrderQuery.cs` | 960 B | 21 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `EndOfDay/Dtos/EndOfDayReportDto.cs` | `EndOfDayReportDto.cs` | 1,515 B | 31 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `EndOfDay/Queries/GetEndOfDayReportQuery.cs` | `GetEndOfDayReportQuery.cs` | 7,033 B | 151 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `IUnitOfWork.cs` | `IUnitOfWork.cs` | 504 B | 8 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `KitchenTickets/Commands/CancelKitchenTicketCommand.cs` | `CancelKitchenTicketCommand.cs` | 1,024 B | 22 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `KitchenTickets/Commands/MarkKitchenTicketReadyCommand.cs` | `MarkKitchenTicketReadyCommand.cs` | 1,035 B | 22 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `KitchenTickets/Commands/SendOrderToKitchenCommand.cs` | `SendOrderToKitchenCommand.cs` | 1,705 B | 36 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `KitchenTickets/Commands/ServeKitchenTicketCommand.cs` | `ServeKitchenTicketCommand.cs` | 1,003 B | 22 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `KitchenTickets/Commands/StartKitchenTicketCommand.cs` | `StartKitchenTicketCommand.cs` | 1,012 B | 22 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `KitchenTickets/Dtos/KitchenTicketDto.cs` | `KitchenTicketDto.cs` | 919 B | 26 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `KitchenTickets/Queries/GetKitchenTicketByIdQuery.cs` | `GetKitchenTicketByIdQuery.cs` | 981 B | 21 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `KitchenTickets/Queries/ListActiveKitchenTicketsQuery.cs` | `ListActiveKitchenTicketsQuery.cs` | 984 B | 20 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `KitchenTickets/Queries/ListKitchenTicketsByOrderQuery.cs` | `ListKitchenTicketsByOrderQuery.cs` | 1,017 B | 21 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `NotFoundException.cs` | `NotFoundException.cs` | 487 B | 11 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `OrderHealth/OrderHealthEvaluator.cs` | `OrderHealthEvaluator.cs` | 1,617 B | 36 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `OrderHealth/OrderHealthResult.cs` | `OrderHealthResult.cs` | 486 B | 8 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `OrderHealth/OrderHealthStatus.cs` | `OrderHealthStatus.cs` | 517 B | 14 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `OrderHealth/OrderHealthThresholds.cs` | `OrderHealthThresholds.cs` | 1,375 B | 23 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `OrderLines/Commands/AddOrderLineCommand.cs` | `AddOrderLineCommand.cs` | 2,546 B | 55 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `OrderLines/Commands/OverrideOrderLinePriceCommand.cs` | `OverrideOrderLinePriceCommand.cs` | 1,444 B | 28 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `OrderLines/Commands/RemoveOrderLineCommand.cs` | `RemoveOrderLineCommand.cs` | 1,603 B | 39 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `OrderLines/Commands/SetOrderLineNotesCommand.cs` | `SetOrderLineNotesCommand.cs` | 973 B | 22 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `OrderLines/Commands/SetOrderLineQuantityCommand.cs` | `SetOrderLineQuantityCommand.cs` | 998 B | 22 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `OrderLines/Commands/UnvoidOrderLineCommand.cs` | `UnvoidOrderLineCommand.cs` | 930 B | 22 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `OrderLines/Commands/VoidOrderLineCommand.cs` | `VoidOrderLineCommand.cs` | 997 B | 22 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `OrderLines/Dtos/OrderLineDto.cs` | `OrderLineDto.cs` | 1,274 B | 42 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `OrderLines/Queries/GetOrderLineByIdQuery.cs` | `GetOrderLineByIdQuery.cs` | 902 B | 21 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `OrderLines/Queries/ListOrderLinesByOrderQuery.cs` | `ListOrderLinesByOrderQuery.cs` | 973 B | 21 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `Orders/Commands/CancelOrderCommand.cs` | `CancelOrderCommand.cs` | 1,415 B | 35 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `Orders/Commands/CompleteOrderCommand.cs` | `CompleteOrderCommand.cs` | 10,646 B | 223 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `Orders/Commands/CompleteOrderCommand.cs.backup-h2-20260817-113226` | `CompleteOrderCommand.cs.backup-h2-20260817-113226` | 4,182 B | 0 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `Orders/Commands/CompleteOrderCommand.cs.backup-m4m5-20260817-121628` | `CompleteOrderCommand.cs.backup-m4m5-20260817-121628` | 9,704 B | 0 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `Orders/Commands/ConfigureOrderNumberSequenceCommand.cs` | `ConfigureOrderNumberSequenceCommand.cs` | 1,327 B | 28 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `Orders/Commands/CreateOrderCommand.cs` | `CreateOrderCommand.cs` | 5,017 B | 105 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `Orders/Commands/CreateOrderCommand.cs.backup-m3-20260817-115714` | `CreateOrderCommand.cs.backup-m3-20260817-115714` | 2,089 B | 0 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `Orders/Commands/HoldOrderCommand.cs` | `HoldOrderCommand.cs` | 957 B | 25 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `Orders/Commands/MergeTablesCommand.cs` | `MergeTablesCommand.cs` | 2,880 B | 64 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `Orders/Commands/ReopenOrderCommand.cs` | `ReopenOrderCommand.cs` | 1,295 B | 34 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `Orders/Commands/ResumeOrderCommand.cs` | `ResumeOrderCommand.cs` | 853 B | 22 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `Orders/Commands/SetOrderCustomerCommand.cs` | `SetOrderCustomerCommand.cs` | 1,497 B | 39 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `Orders/Commands/SetOrderCustomerNotesCommand.cs` | `SetOrderCustomerNotesCommand.cs` | 974 B | 22 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `Orders/Commands/SetOrderNotesCommand.cs` | `SetOrderNotesCommand.cs` | 903 B | 22 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `Orders/Commands/SplitOrderCommand.cs` | `SplitOrderCommand.cs` | 2,418 B | 54 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `Orders/Commands/TransferOrderTableCommand.cs` | `TransferOrderTableCommand.cs` | 3,603 B | 72 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `Orders/Commands/VoidOrderCommand.cs` | `VoidOrderCommand.cs` | 3,273 B | 75 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `Orders/Dtos/OrderDto.cs` | `OrderDto.cs` | 1,412 B | 42 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `Orders/Dtos/OrderNumberSequenceDto.cs` | `OrderNumberSequenceDto.cs` | 485 B | 10 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `Orders/OrderTotals.cs` | `OrderTotals.cs` | 1,744 B | 23 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `Orders/OrderTotalsCalculator.cs` | `OrderTotalsCalculator.cs` | 2,704 B | 56 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `Orders/Queries/GetOpenOrHeldOrderByTableQuery.cs` | `GetOpenOrHeldOrderByTableQuery.cs` | 989 B | 21 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `Orders/Queries/GetOrderByIdQuery.cs` | `GetOrderByIdQuery.cs` | 827 B | 21 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `Orders/Queries/GetOrderNumberSequenceQuery.cs` | `GetOrderNumberSequenceQuery.cs` | 1,174 B | 26 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `Orders/Queries/GetOrderSummaryQuery.cs` | `GetOrderSummaryQuery.cs` | 2,139 B | 43 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `Orders/Queries/ListAllOrdersQuery.cs` | `ListAllOrdersQuery.cs` | 807 B | 19 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `Orders/Queries/ListHeldOrdersQuery.cs` | `ListHeldOrdersQuery.cs` | 872 B | 19 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `Orders/Queries/ListOpenOrdersQuery.cs` | `ListOpenOrdersQuery.cs` | 875 B | 19 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `PaymentMethods/Commands/ActivatePaymentMethodCommand.cs` | `ActivatePaymentMethodCommand.cs` | 1,018 B | 22 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `PaymentMethods/Commands/CreatePaymentMethodCommand.cs` | `CreatePaymentMethodCommand.cs` | 966 B | 23 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `PaymentMethods/Commands/DeactivatePaymentMethodCommand.cs` | `DeactivatePaymentMethodCommand.cs` | 1,032 B | 22 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `PaymentMethods/Commands/RenamePaymentMethodCommand.cs` | `RenamePaymentMethodCommand.cs` | 1,109 B | 23 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `PaymentMethods/Dtos/PaymentMethodDto.cs` | `PaymentMethodDto.cs` | 590 B | 11 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `PaymentMethods/Queries/GetPaymentMethodByIdQuery.cs` | `GetPaymentMethodByIdQuery.cs` | 981 B | 21 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `PaymentMethods/Queries/ListPaymentMethodsQuery.cs` | `ListPaymentMethodsQuery.cs` | 889 B | 20 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `Payments/Commands/RecordPaymentCommand.cs` | `RecordPaymentCommand.cs` | 8,356 B | 165 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `Payments/Commands/RecordPaymentCommand.cs.backup-paymentfix-20260817-104258` | `RecordPaymentCommand.cs.backup-paymentfix-20260817-104258` | 4,063 B | 0 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `Payments/Commands/VoidPaymentCommand.cs` | `VoidPaymentCommand.cs` | 2,868 B | 65 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `Payments/Dtos/PaymentDto.cs` | `PaymentDto.cs` | 673 B | 11 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `Payments/Queries/GetPaymentByIdQuery.cs` | `GetPaymentByIdQuery.cs` | 868 B | 21 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `Payments/Queries/ListPaymentsByOrderQuery.cs` | `ListPaymentsByOrderQuery.cs` | 950 B | 21 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `QuickOrderTemplates/Commands/CreateQuickOrderTemplateCommand.cs` | `CreateQuickOrderTemplateCommand.cs` | 1,402 B | 36 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `QuickOrderTemplates/Commands/SetQuickOrderTemplateStatusCommand.cs` | `SetQuickOrderTemplateStatusCommand.cs` | 1,060 B | 23 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `QuickOrderTemplates/Commands/UpdateQuickOrderTemplateCommand.cs` | `UpdateQuickOrderTemplateCommand.cs` | 1,561 B | 40 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `QuickOrderTemplates/Dtos/QuickOrderTemplateDto.cs` | `QuickOrderTemplateDto.cs` | 433 B | 11 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `QuickOrderTemplates/Dtos/QuickOrderTemplateItemDto.cs` | `QuickOrderTemplateItemDto.cs` | 490 B | 13 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `QuickOrderTemplates/Queries/ListActiveQuickOrderTemplatesQuery.cs` | `ListActiveQuickOrderTemplatesQuery.cs` | 1,645 B | 36 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `QuickOrderTemplates/Queries/ListAllQuickOrderTemplatesQuery.cs` | `ListAllQuickOrderTemplatesQuery.cs` | 1,508 B | 35 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `QuickOrderTemplates/Queries/QuickOrderTemplateProjection.cs` | `QuickOrderTemplateProjection.cs` | 3,312 B | 73 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `RestaurantPulse/Dtos/RestaurantPulseDto.cs` | `RestaurantPulseDto.cs` | 1,050 B | 26 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `RestaurantPulse/Dtos/RestaurantPulseLowStockItemDto.cs` | `RestaurantPulseLowStockItemDto.cs` | 361 B | 8 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `RestaurantPulse/Queries/GetRestaurantPulseQuery.cs` | `GetRestaurantPulseQuery.cs` | 5,131 B | 107 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `RestaurantPulse/RestaurantPulseCalculator.cs` | `RestaurantPulseCalculator.cs` | 4,422 B | 87 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `RestaurantPulse/RestaurantPulseSamples.cs` | `RestaurantPulseSamples.cs` | 807 B | 16 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `ServiceCharges/Commands/ApplyServiceChargeToOrderCommand.cs` | `ApplyServiceChargeToOrderCommand.cs` | 1,414 B | 29 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `ServiceCharges/Commands/RemoveServiceChargeFromOrderCommand.cs` | `RemoveServiceChargeFromOrderCommand.cs` | 1,521 B | 29 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `ServiceCharges/Dtos/ServiceChargeDto.cs` | `ServiceChargeDto.cs` | 675 B | 11 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `ServiceCharges/Queries/GetServiceChargeByIdQuery.cs` | `GetServiceChargeByIdQuery.cs` | 981 B | 21 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `ServiceCharges/Queries/ListServiceChargesByOrderQuery.cs` | `ListServiceChargesByOrderQuery.cs` | 1,027 B | 21 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `Shifts/Commands/CloseShiftCommand.cs` | `CloseShiftCommand.cs` | 4,544 B | 114 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `Shifts/Commands/OpenShiftCommand.cs` | `OpenShiftCommand.cs` | 2,915 B | 76 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `Shifts/Commands/RecordCashMovementCommand.cs` | `RecordCashMovementCommand.cs` | 2,120 B | 53 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `Shifts/Dtos/CashMovementDto.cs` | `CashMovementDto.cs` | 786 B | 27 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `Shifts/Dtos/ShiftDto.cs` | `ShiftDto.cs` | 1,247 B | 45 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `Shifts/Dtos/ShiftSummaryDto.cs` | `ShiftSummaryDto.cs` | 572 B | 20 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `Shifts/Queries/GetActiveShiftQuery.cs` | `GetActiveShiftQuery.cs` | 1,390 B | 35 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `Shifts/Queries/GetShiftByIdQuery.cs` | `GetShiftByIdQuery.cs` | 943 B | 25 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `Shifts/Queries/GetShiftSummaryQuery.cs` | `GetShiftSummaryQuery.cs` | 3,616 B | 91 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `Shifts/Queries/ListShiftsQuery.cs` | `ListShiftsQuery.cs` | 1,520 B | 41 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `SmartRecommendations/Commands/CreateRecommendationRuleCommand.cs` | `CreateRecommendationRuleCommand.cs` | 1,419 B | 37 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `SmartRecommendations/Commands/SetRecommendationRuleStatusCommand.cs` | `SetRecommendationRuleStatusCommand.cs` | 1,069 B | 23 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `SmartRecommendations/Commands/UpdateRecommendationRuleCommand.cs` | `UpdateRecommendationRuleCommand.cs` | 1,652 B | 42 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `SmartRecommendations/Dtos/BasketRecommendationDto.cs` | `BasketRecommendationDto.cs` | 323 B | 9 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `SmartRecommendations/Dtos/RecommendationReason.cs` | `RecommendationReason.cs` | 580 B | 14 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `SmartRecommendations/Dtos/RecommendationRuleDto.cs` | `RecommendationRuleDto.cs` | 878 B | 28 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `SmartRecommendations/Queries/GetBasketRecommendationsQuery.cs` | `GetBasketRecommendationsQuery.cs` | 6,502 B | 146 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `SmartRecommendations/Queries/ListRecommendationRulesQuery.cs` | `ListRecommendationRulesQuery.cs` | 1,084 B | 20 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `Tables/Commands/ActivateTableCommand.cs` | `ActivateTableCommand.cs` | 862 B | 22 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `Tables/Commands/CreateTableCommand.cs` | `CreateTableCommand.cs` | 1,310 B | 33 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `Tables/Commands/DeactivateTableCommand.cs` | `DeactivateTableCommand.cs` | 876 B | 22 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `Tables/Commands/OccupyTableCommand.cs` | `OccupyTableCommand.cs` | 846 B | 22 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `Tables/Commands/ReserveTableCommand.cs` | `ReserveTableCommand.cs` | 877 B | 22 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `Tables/Commands/ReturnTableToServiceCommand.cs` | `ReturnTableToServiceCommand.cs` | 931 B | 22 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `Tables/Commands/SetTableOutOfServiceCommand.cs` | `SetTableOutOfServiceCommand.cs` | 915 B | 22 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `Tables/Commands/UpdateTableCommand.cs` | `UpdateTableCommand.cs` | 1,025 B | 25 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `Tables/Commands/VacateTableCommand.cs` | `VacateTableCommand.cs` | 3,274 B | 68 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `Tables/Commands/VacateTableCommand.cs.backup-vacateguard-20260817-135631` | `VacateTableCommand.cs.backup-vacateguard-20260817-135631` | 846 B | 0 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `Tables/Dtos/TableDto.cs` | `TableDto.cs` | 774 B | 26 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `Tables/Queries/GetTableByIdQuery.cs` | `GetTableByIdQuery.cs` | 826 B | 21 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `Tables/Queries/ListAllTablesQuery.cs` | `ListAllTablesQuery.cs` | 860 B | 20 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `Tables/Queries/ListTablesByDiningAreaQuery.cs` | `ListTablesByDiningAreaQuery.cs` | 969 B | 21 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `UniversalPosSearch/Dtos/UniversalPosSearchResultsDto.cs` | `UniversalPosSearchResultsDto.cs` | 1,329 B | 38 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Application` | `UniversalPosSearch/Queries/UniversalPosSearchQuery.cs` | `UniversalPosSearchQuery.cs` | 7,432 B | 158 | CQRS Application Pipeline | Active |
| `Clovent.Restaurant.Infrastructure.Tests` | `Clovent.Restaurant.Infrastructure.Tests.csproj` | `Clovent.Restaurant.Infrastructure.Tests.csproj` | 1,571 B | 0 | Unit / Integration Test | Active |
| `Clovent.Restaurant.Infrastructure.Tests` | `Integration/OrderLifecycleIntegrationTests.cs` | `OrderLifecycleIntegrationTests.cs` | 18,499 B | 337 | Unit / Integration Test | Active |
| `Clovent.Restaurant.Infrastructure.Tests` | `Integration/OrderLifecycleIntegrationTests.cs.backup-vacateguard-20260817-135631` | `OrderLifecycleIntegrationTests.cs.backup-vacateguard-20260817-135631` | 11,685 B | 0 | Unit / Integration Test | Active |
| `Clovent.Restaurant.Infrastructure.Tests` | `Integration/TableSwitchingIntegrationTests.cs` | `TableSwitchingIntegrationTests.cs` | 5,247 B | 104 | Unit / Integration Test | Active |
| `Clovent.Restaurant.Infrastructure.Tests` | `Persistence/UnitOfWorkBehaviorTests.cs` | `UnitOfWorkBehaviorTests.cs` | 1,784 B | 58 | Unit / Integration Test | Active |
| `Clovent.Restaurant.Infrastructure.Tests` | `Repositories/CustomerRepositoryTests.cs` | `CustomerRepositoryTests.cs` | 8,432 B | 212 | Unit / Integration Test | Active |
| `Clovent.Restaurant.Infrastructure.Tests` | `Repositories/DiningAreaRepositoryTests.cs` | `DiningAreaRepositoryTests.cs` | 2,404 B | 65 | Unit / Integration Test | Active |
| `Clovent.Restaurant.Infrastructure.Tests` | `Repositories/DiscountRepositoryTests.cs` | `DiscountRepositoryTests.cs` | 2,146 B | 61 | Unit / Integration Test | Active |
| `Clovent.Restaurant.Infrastructure.Tests` | `Repositories/KitchenTicketRepositoryTests.cs` | `KitchenTicketRepositoryTests.cs` | 2,994 B | 86 | Unit / Integration Test | Active |
| `Clovent.Restaurant.Infrastructure.Tests` | `Repositories/OrderLineRepositoryTests.cs` | `OrderLineRepositoryTests.cs` | 2,624 B | 70 | Unit / Integration Test | Active |
| `Clovent.Restaurant.Infrastructure.Tests` | `Repositories/OrderRepositoryTests.cs` | `OrderRepositoryTests.cs` | 5,757 B | 141 | Unit / Integration Test | Active |
| `Clovent.Restaurant.Infrastructure.Tests` | `Repositories/PaymentMethodRepositoryTests.cs` | `PaymentMethodRepositoryTests.cs` | 1,909 B | 55 | Unit / Integration Test | Active |
| `Clovent.Restaurant.Infrastructure.Tests` | `Repositories/PaymentRepositoryTests.cs` | `PaymentRepositoryTests.cs` | 2,162 B | 63 | Unit / Integration Test | Active |
| `Clovent.Restaurant.Infrastructure.Tests` | `Repositories/ServiceChargeRepositoryTests.cs` | `ServiceChargeRepositoryTests.cs` | 2,165 B | 60 | Unit / Integration Test | Active |
| `Clovent.Restaurant.Infrastructure.Tests` | `Repositories/SmartPosRepositoryTests.cs` | `SmartPosRepositoryTests.cs` | 3,250 B | 81 | Unit / Integration Test | Active |
| `Clovent.Restaurant.Infrastructure.Tests` | `Repositories/TableRepositoryTests.cs` | `TableRepositoryTests.cs` | 3,292 B | 85 | Unit / Integration Test | Active |
| `Clovent.Restaurant.Infrastructure.Tests` | `TestSupport/SqliteTestBase.cs` | `SqliteTestBase.cs` | 1,197 B | 34 | Unit / Integration Test | Active |
| `Clovent.Restaurant.Infrastructure` | `Clovent.Restaurant.Infrastructure.csproj` | `Clovent.Restaurant.Infrastructure.csproj` | 1,299 B | 0 | EF Core Persistence / Migrations | Active |
| `Clovent.Restaurant.Infrastructure` | `DependencyInjection/InfrastructureServiceCollectionExtensions.cs` | `InfrastructureServiceCollectionExtensions.cs` | 1,179 B | 21 | EF Core Persistence / Migrations | Active |
| `Clovent.Restaurant.Infrastructure` | `DependencyInjection/PersistenceServiceCollectionExtensions.cs` | `PersistenceServiceCollectionExtensions.cs` | 4,207 B | 74 | EF Core Persistence / Migrations | Active |
| `Clovent.Restaurant.Infrastructure` | `Migrations/20260728041421_InitialCreate.Designer.cs` | `20260728041421_InitialCreate.Designer.cs` | 12,914 B | 358 | EF Core Persistence / Migrations | Active |
| `Clovent.Restaurant.Infrastructure` | `Migrations/20260728041421_InitialCreate.cs` | `20260728041421_InitialCreate.cs` | 13,806 B | 290 | EF Core Persistence / Migrations | Active |
| `Clovent.Restaurant.Infrastructure` | `Migrations/RestaurantDbContextModelSnapshot.cs` | `RestaurantDbContextModelSnapshot.cs` | 30,789 B | 839 | EF Core Persistence / Migrations | Active |
| `Clovent.Restaurant.Infrastructure` | `Persistence/Configurations/ActivityLogEntryConfiguration.cs` | `ActivityLogEntryConfiguration.cs` | 1,148 B | 28 | EF Core Persistence / Migrations | Active |
| `Clovent.Restaurant.Infrastructure` | `Persistence/Configurations/CashMovementConfiguration.cs` | `CashMovementConfiguration.cs` | 1,386 B | 39 | EF Core Persistence / Migrations | Active |
| `Clovent.Restaurant.Infrastructure` | `Persistence/Configurations/CustomerConfiguration.cs` | `CustomerConfiguration.cs` | 1,887 B | 44 | EF Core Persistence / Migrations | Active |
| `Clovent.Restaurant.Infrastructure` | `Persistence/Configurations/CustomerLedgerEntryConfiguration.cs` | `CustomerLedgerEntryConfiguration.cs` | 1,342 B | 33 | EF Core Persistence / Migrations | Active |
| `Clovent.Restaurant.Infrastructure` | `Persistence/Configurations/DailySalesSequenceConfiguration.cs` | `DailySalesSequenceConfiguration.cs` | 1,078 B | 30 | EF Core Persistence / Migrations | Active |
| `Clovent.Restaurant.Infrastructure` | `Persistence/Configurations/DiningAreaConfiguration.cs` | `DiningAreaConfiguration.cs` | 1,249 B | 38 | EF Core Persistence / Migrations | Active |
| `Clovent.Restaurant.Infrastructure` | `Persistence/Configurations/DiscountConfiguration.cs` | `DiscountConfiguration.cs` | 1,219 B | 36 | EF Core Persistence / Migrations | Active |
| `Clovent.Restaurant.Infrastructure` | `Persistence/Configurations/KitchenTicketConfiguration.cs` | `KitchenTicketConfiguration.cs` | 1,485 B | 41 | EF Core Persistence / Migrations | Active |
| `Clovent.Restaurant.Infrastructure` | `Persistence/Configurations/OrderConfiguration.cs` | `OrderConfiguration.cs` | 2,687 B | 74 | EF Core Persistence / Migrations | Active |
| `Clovent.Restaurant.Infrastructure` | `Persistence/Configurations/OrderLineConfiguration.cs` | `OrderLineConfiguration.cs` | 1,891 B | 46 | EF Core Persistence / Migrations | Active |
| `Clovent.Restaurant.Infrastructure` | `Persistence/Configurations/OrderNumberSequenceConfiguration.cs` | `OrderNumberSequenceConfiguration.cs` | 892 B | 24 | EF Core Persistence / Migrations | Active |
| `Clovent.Restaurant.Infrastructure` | `Persistence/Configurations/PaymentConfiguration.cs` | `PaymentConfiguration.cs` | 1,356 B | 39 | EF Core Persistence / Migrations | Active |
| `Clovent.Restaurant.Infrastructure` | `Persistence/Configurations/PaymentMethodConfiguration.cs` | `PaymentMethodConfiguration.cs` | 1,097 B | 33 | EF Core Persistence / Migrations | Active |
| `Clovent.Restaurant.Infrastructure` | `Persistence/Configurations/QuickOrderTemplateConfiguration.cs` | `QuickOrderTemplateConfiguration.cs` | 1,430 B | 37 | EF Core Persistence / Migrations | Active |
| `Clovent.Restaurant.Infrastructure` | `Persistence/Configurations/QuickOrderTemplateItemConfiguration.cs` | `QuickOrderTemplateItemConfiguration.cs` | 1,228 B | 31 | EF Core Persistence / Migrations | Active |
| `Clovent.Restaurant.Infrastructure` | `Persistence/Configurations/RecommendationRuleConfiguration.cs` | `RecommendationRuleConfiguration.cs` | 1,577 B | 43 | EF Core Persistence / Migrations | Active |
| `Clovent.Restaurant.Infrastructure` | `Persistence/Configurations/ServiceChargeConfiguration.cs` | `ServiceChargeConfiguration.cs` | 1,259 B | 36 | EF Core Persistence / Migrations | Active |
| `Clovent.Restaurant.Infrastructure` | `Persistence/Configurations/ShiftConfiguration.cs` | `ShiftConfiguration.cs` | 2,624 B | 71 | EF Core Persistence / Migrations | Active |
| `Clovent.Restaurant.Infrastructure` | `Persistence/Configurations/TableConfiguration.cs` | `TableConfiguration.cs` | 1,582 B | 50 | EF Core Persistence / Migrations | Active |
| `Clovent.Restaurant.Infrastructure` | `Persistence/Migrations/20260729054113_AddDailySalesSequenceAndDailySalesNumber.Designer.cs` | `20260729054113_AddDailySalesSequenceAndDailySalesNumber.Designer.cs` | 13,843 B | 383 | EF Core Persistence / Migrations | Active |
| `Clovent.Restaurant.Infrastructure` | `Persistence/Migrations/20260729054113_AddDailySalesSequenceAndDailySalesNumber.cs` | `20260729054113_AddDailySalesSequenceAndDailySalesNumber.cs` | 2,044 B | 57 | EF Core Persistence / Migrations | Active |
| `Clovent.Restaurant.Infrastructure` | `Persistence/Migrations/20260804060819_AddOrderNumberSequence.Designer.cs` | `20260804060819_AddOrderNumberSequence.Designer.cs` | 14,444 B | 401 | EF Core Persistence / Migrations | Active |
| `Clovent.Restaurant.Infrastructure` | `Persistence/Migrations/20260804060819_AddOrderNumberSequence.cs` | `20260804060819_AddOrderNumberSequence.cs` | 1,277 B | 37 | EF Core Persistence / Migrations | Active |
| `Clovent.Restaurant.Infrastructure` | `Persistence/Migrations/20260804082352_AddOrderLinePriceOverride.Designer.cs` | `20260804082352_AddOrderLinePriceOverride.Designer.cs` | 15,193 B | 419 | EF Core Persistence / Migrations | Active |
| `Clovent.Restaurant.Infrastructure` | `Persistence/Migrations/20260804082352_AddOrderLinePriceOverride.cs` | `20260804082352_AddOrderLinePriceOverride.cs` | 3,195 B | 91 | EF Core Persistence / Migrations | Active |
| `Clovent.Restaurant.Infrastructure` | `Persistence/Migrations/20260804084644_AddActivityLog.Designer.cs` | `20260804084644_AddActivityLog.Designer.cs` | 16,441 B | 453 | EF Core Persistence / Migrations | Active |
| `Clovent.Restaurant.Infrastructure` | `Persistence/Migrations/20260804084644_AddActivityLog.cs` | `20260804084644_AddActivityLog.cs` | 1,861 B | 46 | EF Core Persistence / Migrations | Active |
| `Clovent.Restaurant.Infrastructure` | `Persistence/Migrations/20260809031725_AddCustomerAndLedger.Designer.cs` | `20260809031725_AddCustomerAndLedger.Designer.cs` | 20,451 B | 560 | EF Core Persistence / Migrations | Active |
| `Clovent.Restaurant.Infrastructure` | `Persistence/Migrations/20260809031725_AddCustomerAndLedger.cs` | `20260809031725_AddCustomerAndLedger.cs` | 4,965 B | 106 | EF Core Persistence / Migrations | Active |
| `Clovent.Restaurant.Infrastructure` | `Persistence/Migrations/20260821110043_AddTableNameToTable.Designer.cs` | `20260821110043_AddTableNameToTable.Designer.cs` | 20,640 B | 565 | EF Core Persistence / Migrations | Active |
| `Clovent.Restaurant.Infrastructure` | `Persistence/Migrations/20260821110043_AddTableNameToTable.cs` | `20260821110043_AddTableNameToTable.cs` | 1,021 B | 34 | EF Core Persistence / Migrations | Active |
| `Clovent.Restaurant.Infrastructure` | `Persistence/Migrations/20260821123746_UpdateMonetaryDecimalPrecision.Designer.cs` | `20260821123746_UpdateMonetaryDecimalPrecision.Designer.cs` | 20,662 B | 565 | EF Core Persistence / Migrations | Active |
| `Clovent.Restaurant.Infrastructure` | `Persistence/Migrations/20260821123746_UpdateMonetaryDecimalPrecision.cs` | `20260821123746_UpdateMonetaryDecimalPrecision.cs` | 3,234 B | 96 | EF Core Persistence / Migrations | Active |
| `Clovent.Restaurant.Infrastructure` | `Persistence/Migrations/20260821123934_AddCustomerFields.Designer.cs` | `20260821123934_AddCustomerFields.Designer.cs` | 21,094 B | 577 | EF Core Persistence / Migrations | Active |
| `Clovent.Restaurant.Infrastructure` | `Persistence/Migrations/20260821123934_AddCustomerFields.cs` | `20260821123934_AddCustomerFields.cs` | 1,744 B | 57 | EF Core Persistence / Migrations | Active |
| `Clovent.Restaurant.Infrastructure` | `Persistence/Migrations/20260912025646_AddShiftManagement.Designer.cs` | `20260912025646_AddShiftManagement.Designer.cs` | 26,196 B | 715 | EF Core Persistence / Migrations | Active |
| `Clovent.Restaurant.Infrastructure` | `Persistence/Migrations/20260912025646_AddShiftManagement.cs` | `20260912025646_AddShiftManagement.cs` | 6,333 B | 135 | EF Core Persistence / Migrations | Active |
| `Clovent.Restaurant.Infrastructure` | `Persistence/Migrations/20260914160000_AddCustomerIsDefault.Designer.cs` | `20260914160000_AddCustomerIsDefault.Designer.cs` | 21,300 B | 583 | EF Core Persistence / Migrations | Active |
| `Clovent.Restaurant.Infrastructure` | `Persistence/Migrations/20260914160000_AddCustomerIsDefault.cs` | `20260914160000_AddCustomerIsDefault.cs` | 879 B | 31 | EF Core Persistence / Migrations | Active |
| `Clovent.Restaurant.Infrastructure` | `Persistence/Migrations/20260915054626_AddSmartPosFeatures.Designer.cs` | `20260915054626_AddSmartPosFeatures.Designer.cs` | 30,910 B | 842 | EF Core Persistence / Migrations | Active |
| `Clovent.Restaurant.Infrastructure` | `Persistence/Migrations/20260915054626_AddSmartPosFeatures.cs` | `20260915054626_AddSmartPosFeatures.cs` | 6,047 B | 130 | EF Core Persistence / Migrations | Active |
| `Clovent.Restaurant.Infrastructure` | `Persistence/RestaurantDbContext.cs` | `RestaurantDbContext.cs` | 3,529 B | 87 | EF Core Persistence / Migrations | Active |
| `Clovent.Restaurant.Infrastructure` | `Persistence/RestaurantDbContextFactory.cs` | `RestaurantDbContextFactory.cs` | 828 B | 17 | EF Core Persistence / Migrations | Active |
| `Clovent.Restaurant.Infrastructure` | `Persistence/RestaurantPersistenceInitializer.cs` | `RestaurantPersistenceInitializer.cs` | 15,759 B | 230 | EF Core Persistence / Migrations | Active |
| `Clovent.Restaurant.Infrastructure` | `Persistence/UnitOfWork.cs` | `UnitOfWork.cs` | 565 B | 11 | EF Core Persistence / Migrations | Active |
| `Clovent.Restaurant.Infrastructure` | `Persistence/UnitOfWorkBehavior.cs` | `UnitOfWorkBehavior.cs` | 767 B | 17 | EF Core Persistence / Migrations | Active |
| `Clovent.Restaurant.Infrastructure` | `Persistence/ValueConverters.cs` | `ValueConverters.cs` | 14,952 B | 217 | EF Core Persistence / Migrations | Active |
| `Clovent.Restaurant.Infrastructure` | `Repositories/ActivityLogEntryRepository.cs` | `ActivityLogEntryRepository.cs` | 926 B | 20 | EF Core Persistence / Migrations | Active |
| `Clovent.Restaurant.Infrastructure` | `Repositories/CustomerLedgerEntryRepository.cs` | `CustomerLedgerEntryRepository.cs` | 1,388 B | 27 | EF Core Persistence / Migrations | Active |
| `Clovent.Restaurant.Infrastructure` | `Repositories/CustomerRepository.cs` | `CustomerRepository.cs` | 5,357 B | 117 | EF Core Persistence / Migrations | Active |
| `Clovent.Restaurant.Infrastructure` | `Repositories/DailySalesSequenceRepository.cs` | `DailySalesSequenceRepository.cs` | 952 B | 18 | EF Core Persistence / Migrations | Active |
| `Clovent.Restaurant.Infrastructure` | `Repositories/DiningAreaRepository.cs` | `DiningAreaRepository.cs` | 1,306 B | 26 | EF Core Persistence / Migrations | Active |
| `Clovent.Restaurant.Infrastructure` | `Repositories/DiscountRepository.cs` | `DiscountRepository.cs` | 1,067 B | 22 | EF Core Persistence / Migrations | Active |
| `Clovent.Restaurant.Infrastructure` | `Repositories/KitchenTicketRepository.cs` | `KitchenTicketRepository.cs` | 1,471 B | 28 | EF Core Persistence / Migrations | Active |
| `Clovent.Restaurant.Infrastructure` | `Repositories/OrderLineRepository.cs` | `OrderLineRepository.cs` | 1,080 B | 22 | EF Core Persistence / Migrations | Active |
| `Clovent.Restaurant.Infrastructure` | `Repositories/OrderNumberSequenceRepository.cs` | `OrderNumberSequenceRepository.cs` | 821 B | 17 | EF Core Persistence / Migrations | Active |
| `Clovent.Restaurant.Infrastructure` | `Repositories/OrderRepository.cs` | `OrderRepository.cs` | 2,004 B | 38 | EF Core Persistence / Migrations | Active |
| `Clovent.Restaurant.Infrastructure` | `Repositories/PaymentMethodRepository.cs` | `PaymentMethodRepository.cs` | 1,043 B | 21 | EF Core Persistence / Migrations | Active |
| `Clovent.Restaurant.Infrastructure` | `Repositories/PaymentRepository.cs` | `PaymentRepository.cs` | 1,338 B | 26 | EF Core Persistence / Migrations | Active |
| `Clovent.Restaurant.Infrastructure` | `Repositories/QuickOrderTemplateRepository.cs` | `QuickOrderTemplateRepository.cs` | 2,066 B | 49 | EF Core Persistence / Migrations | Active |
| `Clovent.Restaurant.Infrastructure` | `Repositories/RecommendationRuleRepository.cs` | `RecommendationRuleRepository.cs` | 1,764 B | 36 | EF Core Persistence / Migrations | Active |
| `Clovent.Restaurant.Infrastructure` | `Repositories/ServiceChargeRepository.cs` | `ServiceChargeRepository.cs` | 1,132 B | 22 | EF Core Persistence / Migrations | Active |
| `Clovent.Restaurant.Infrastructure` | `Repositories/ShiftRepository.cs` | `ShiftRepository.cs` | 3,304 B | 91 | EF Core Persistence / Migrations | Active |
| `Clovent.Restaurant.Infrastructure` | `Repositories/TableRepository.cs` | `TableRepository.cs` | 1,256 B | 26 | EF Core Persistence / Migrations | Active |
| `Clovent.Restaurant.Tests` | `ActivityLogs/ActivityLogEntryTests.cs` | `ActivityLogEntryTests.cs` | 1,515 B | 50 | Unit / Integration Test | Active |
| `Clovent.Restaurant.Tests` | `Clovent.Restaurant.Tests.csproj` | `Clovent.Restaurant.Tests.csproj` | 665 B | 0 | Unit / Integration Test | Active |
| `Clovent.Restaurant.Tests` | `Customers/CustomerDomainTests.cs` | `CustomerDomainTests.cs` | 2,022 B | 62 | Unit / Integration Test | Active |
| `Clovent.Restaurant.Tests` | `DiningAreas/DiningAreaNameTests.cs` | `DiningAreaNameTests.cs` | 748 B | 33 | Unit / Integration Test | Active |
| `Clovent.Restaurant.Tests` | `DiningAreas/DiningAreaTests.cs` | `DiningAreaTests.cs` | 2,389 B | 77 | Unit / Integration Test | Active |
| `Clovent.Restaurant.Tests` | `Discounts/DiscountTests.cs` | `DiscountTests.cs` | 1,487 B | 46 | Unit / Integration Test | Active |
| `Clovent.Restaurant.Tests` | `KitchenTickets/KitchenTicketTests.cs` | `KitchenTicketTests.cs` | 3,117 B | 118 | Unit / Integration Test | Active |
| `Clovent.Restaurant.Tests` | `OrderLines/OrderLineTests.cs` | `OrderLineTests.cs` | 3,635 B | 133 | Unit / Integration Test | Active |
| `Clovent.Restaurant.Tests` | `Orders/OrderNumberSequenceTests.cs` | `OrderNumberSequenceTests.cs` | 1,815 B | 69 | Unit / Integration Test | Active |
| `Clovent.Restaurant.Tests` | `Orders/OrderNumberTests.cs` | `OrderNumberTests.cs` | 906 B | 38 | Unit / Integration Test | Active |
| `Clovent.Restaurant.Tests` | `Orders/OrderTests.cs` | `OrderTests.cs` | 9,507 B | 336 | Unit / Integration Test | Active |
| `Clovent.Restaurant.Tests` | `PaymentMethods/PaymentMethodTests.cs` | `PaymentMethodTests.cs` | 1,743 B | 55 | Unit / Integration Test | Active |
| `Clovent.Restaurant.Tests` | `Payments/PaymentTests.cs` | `PaymentTests.cs` | 1,332 B | 50 | Unit / Integration Test | Active |
| `Clovent.Restaurant.Tests` | `ServiceCharges/ServiceChargeTests.cs` | `ServiceChargeTests.cs` | 1,254 B | 37 | Unit / Integration Test | Active |
| `Clovent.Restaurant.Tests` | `Shifts/ShiftTests.cs` | `ShiftTests.cs` | 7,189 B | 204 | Unit / Integration Test | Active |
| `Clovent.Restaurant.Tests` | `Tables/TableTests.cs` | `TableTests.cs` | 4,983 B | 191 | Unit / Integration Test | Active |
| `Clovent.Restaurant` | `ActivityLogs/ActivityLogEntry.cs` | `ActivityLogEntry.cs` | 3,903 B | 81 | Domain Entity / Aggregate | Active |
| `Clovent.Restaurant` | `ActivityLogs/ActivityLogEntryId.cs` | `ActivityLogEntryId.cs` | 700 B | 16 | Domain Entity / Aggregate | Active |
| `Clovent.Restaurant` | `ActivityLogs/Events/ActivityLogEntryRecorded.cs` | `ActivityLogEntryRecorded.cs` | 305 B | 6 | Domain Entity / Aggregate | Active |
| `Clovent.Restaurant` | `ActivityLogs/IActivityLogEntryRepository.cs` | `IActivityLogEntryRepository.cs` | 579 B | 11 | Domain Entity / Aggregate | Active |
| `Clovent.Restaurant` | `Clovent.Restaurant.csproj` | `Clovent.Restaurant.csproj` | 615 B | 0 | Domain Entity / Aggregate | Active |
| `Clovent.Restaurant` | `Customers/Customer.cs` | `Customer.cs` | 7,221 B | 210 | Domain Entity / Aggregate | Active |
| `Clovent.Restaurant` | `Customers/CustomerId.cs` | `CustomerId.cs` | 656 B | 16 | Domain Entity / Aggregate | Active |
| `Clovent.Restaurant` | `Customers/CustomerLedgerEntry.cs` | `CustomerLedgerEntry.cs` | 2,702 B | 77 | Domain Entity / Aggregate | Active |
| `Clovent.Restaurant` | `Customers/CustomerLedgerEntryId.cs` | `CustomerLedgerEntryId.cs` | 701 B | 16 | Domain Entity / Aggregate | Active |
| `Clovent.Restaurant` | `Customers/ICustomerLedgerEntryRepository.cs` | `ICustomerLedgerEntryRepository.cs` | 805 B | 14 | Domain Entity / Aggregate | Active |
| `Clovent.Restaurant` | `Customers/ICustomerRepository.cs` | `ICustomerRepository.cs` | 2,112 B | 40 | Domain Entity / Aggregate | Active |
| `Clovent.Restaurant` | `DiningAreas/DiningArea.cs` | `DiningArea.cs` | 3,230 B | 82 | Domain Entity / Aggregate | Active |
| `Clovent.Restaurant` | `DiningAreas/DiningAreaId.cs` | `DiningAreaId.cs` | 668 B | 16 | Domain Entity / Aggregate | Active |
| `Clovent.Restaurant` | `DiningAreas/Events/DiningAreaActivated.cs` | `DiningAreaActivated.cs` | 267 B | 6 | Domain Entity / Aggregate | Active |
| `Clovent.Restaurant` | `DiningAreas/Events/DiningAreaCreated.cs` | `DiningAreaCreated.cs` | 387 B | 8 | Domain Entity / Aggregate | Active |
| `Clovent.Restaurant` | `DiningAreas/Events/DiningAreaDeactivated.cs` | `DiningAreaDeactivated.cs` | 267 B | 6 | Domain Entity / Aggregate | Active |
| `Clovent.Restaurant` | `DiningAreas/Events/DiningAreaRenamed.cs` | `DiningAreaRenamed.cs` | 335 B | 7 | Domain Entity / Aggregate | Active |
| `Clovent.Restaurant` | `DiningAreas/IDiningAreaRepository.cs` | `IDiningAreaRepository.cs` | 1,000 B | 19 | Domain Entity / Aggregate | Active |
| `Clovent.Restaurant` | `DiningAreas/ValueObjects/DiningAreaName.cs` | `DiningAreaName.cs` | 1,334 B | 39 | Domain Entity / Aggregate | Active |
| `Clovent.Restaurant` | `Discounts/Discount.cs` | `Discount.cs` | 3,250 B | 73 | Domain Entity / Aggregate | Active |
| `Clovent.Restaurant` | `Discounts/DiscountId.cs` | `DiscountId.cs` | 656 B | 16 | Domain Entity / Aggregate | Active |
| `Clovent.Restaurant` | `Discounts/DiscountType.cs` | `DiscountType.cs` | 325 B | 11 | Domain Entity / Aggregate | Active |
| `Clovent.Restaurant` | `Discounts/Events/DiscountCreated.cs` | `DiscountCreated.cs` | 345 B | 7 | Domain Entity / Aggregate | Active |
| `Clovent.Restaurant` | `Discounts/IDiscountRepository.cs` | `IDiscountRepository.cs` | 739 B | 16 | Domain Entity / Aggregate | Active |
| `Clovent.Restaurant` | `KitchenTickets/Events/KitchenTicketCancelled.cs` | `KitchenTicketCancelled.cs` | 278 B | 6 | Domain Entity / Aggregate | Active |
| `Clovent.Restaurant` | `KitchenTickets/Events/KitchenTicketCreated.cs` | `KitchenTicketCreated.cs` | 340 B | 7 | Domain Entity / Aggregate | Active |
| `Clovent.Restaurant` | `KitchenTickets/Events/KitchenTicketMarkedReady.cs` | `KitchenTicketMarkedReady.cs` | 292 B | 6 | Domain Entity / Aggregate | Active |
| `Clovent.Restaurant` | `KitchenTickets/Events/KitchenTicketServed.cs` | `KitchenTicketServed.cs` | 272 B | 6 | Domain Entity / Aggregate | Active |
| `Clovent.Restaurant` | `KitchenTickets/Events/KitchenTicketStarted.cs` | `KitchenTicketStarted.cs` | 285 B | 6 | Domain Entity / Aggregate | Active |
| `Clovent.Restaurant` | `KitchenTickets/IKitchenTicketRepository.cs` | `IKitchenTicketRepository.cs` | 1,085 B | 19 | Domain Entity / Aggregate | Active |
| `Clovent.Restaurant` | `KitchenTickets/KitchenTicket.cs` | `KitchenTicket.cs` | 5,290 B | 121 | Domain Entity / Aggregate | Active |
| `Clovent.Restaurant` | `KitchenTickets/KitchenTicketId.cs` | `KitchenTicketId.cs` | 686 B | 16 | Domain Entity / Aggregate | Active |
| `Clovent.Restaurant` | `KitchenTickets/KitchenTicketStatus.cs` | `KitchenTicketStatus.cs` | 575 B | 20 | Domain Entity / Aggregate | Active |
| `Clovent.Restaurant` | `OrderLines/Events/OrderLineCreated.cs` | `OrderLineCreated.cs` | 412 B | 8 | Domain Entity / Aggregate | Active |
| `Clovent.Restaurant` | `OrderLines/Events/OrderLineNotesChanged.cs` | `OrderLineNotesChanged.cs` | 284 B | 6 | Domain Entity / Aggregate | Active |
| `Clovent.Restaurant` | `OrderLines/Events/OrderLinePriceOverridden.cs` | `OrderLinePriceOverridden.cs` | 434 B | 12 | Domain Entity / Aggregate | Active |
| `Clovent.Restaurant` | `OrderLines/Events/OrderLineQuantityChanged.cs` | `OrderLineQuantityChanged.cs` | 289 B | 6 | Domain Entity / Aggregate | Active |
| `Clovent.Restaurant` | `OrderLines/Events/OrderLineTransferredToOrder.cs` | `OrderLineTransferredToOrder.cs` | 368 B | 7 | Domain Entity / Aggregate | Active |
| `Clovent.Restaurant` | `OrderLines/Events/OrderLineUnvoided.cs` | `OrderLineUnvoided.cs` | 263 B | 6 | Domain Entity / Aggregate | Active |
| `Clovent.Restaurant` | `OrderLines/Events/OrderLineVoided.cs` | `OrderLineVoided.cs` | 253 B | 6 | Domain Entity / Aggregate | Active |
| `Clovent.Restaurant` | `OrderLines/IOrderLineRepository.cs` | `IOrderLineRepository.cs` | 760 B | 16 | Domain Entity / Aggregate | Active |
| `Clovent.Restaurant` | `OrderLines/OrderLine.cs` | `OrderLine.cs` | 9,017 B | 206 | Domain Entity / Aggregate | Active |
| `Clovent.Restaurant` | `OrderLines/OrderLineId.cs` | `OrderLineId.cs` | 663 B | 16 | Domain Entity / Aggregate | Active |
| `Clovent.Restaurant` | `Orders/Events/OrderCancelled.cs` | `OrderCancelled.cs` | 254 B | 6 | Domain Entity / Aggregate | Active |
| `Clovent.Restaurant` | `Orders/Events/OrderCompleted.cs` | `OrderCompleted.cs` | 263 B | 6 | Domain Entity / Aggregate | Active |
| `Clovent.Restaurant` | `Orders/Events/OrderCreated.cs` | `OrderCreated.cs` | 443 B | 9 | Domain Entity / Aggregate | Active |
| `Clovent.Restaurant` | `Orders/Events/OrderCustomerNotesChanged.cs` | `OrderCustomerNotesChanged.cs` | 291 B | 6 | Domain Entity / Aggregate | Active |
| `Clovent.Restaurant` | `Orders/Events/OrderDailySalesNumberAssigned.cs` | `OrderDailySalesNumberAssigned.cs` | 298 B | 6 | Domain Entity / Aggregate | Active |
| `Clovent.Restaurant` | `Orders/Events/OrderDiscountApplied.cs` | `OrderDiscountApplied.cs` | 340 B | 7 | Domain Entity / Aggregate | Active |
| `Clovent.Restaurant` | `Orders/Events/OrderDiscountRemoved.cs` | `OrderDiscountRemoved.cs` | 342 B | 7 | Domain Entity / Aggregate | Active |
| `Clovent.Restaurant` | `Orders/Events/OrderHeld.cs` | `OrderHeld.cs` | 229 B | 6 | Domain Entity / Aggregate | Active |
| `Clovent.Restaurant` | `Orders/Events/OrderLineAdded.cs` | `OrderLineAdded.cs` | 338 B | 7 | Domain Entity / Aggregate | Active |
| `Clovent.Restaurant` | `Orders/Events/OrderLineRemoved.cs` | `OrderLineRemoved.cs` | 344 B | 7 | Domain Entity / Aggregate | Active |
| `Clovent.Restaurant` | `Orders/Events/OrderNotesChanged.cs` | `OrderNotesChanged.cs` | 268 B | 6 | Domain Entity / Aggregate | Active |
| `Clovent.Restaurant` | `Orders/Events/OrderPaymentRecorded.cs` | `OrderPaymentRecorded.cs` | 341 B | 7 | Domain Entity / Aggregate | Active |
| `Clovent.Restaurant` | `Orders/Events/OrderReopened.cs` | `OrderReopened.cs` | 256 B | 6 | Domain Entity / Aggregate | Active |
| `Clovent.Restaurant` | `Orders/Events/OrderResumed.cs` | `OrderResumed.cs` | 236 B | 6 | Domain Entity / Aggregate | Active |
| `Clovent.Restaurant` | `Orders/Events/OrderServiceChargeApplied.cs` | `OrderServiceChargeApplied.cs` | 370 B | 7 | Domain Entity / Aggregate | Active |
| `Clovent.Restaurant` | `Orders/Events/OrderServiceChargeRemoved.cs` | `OrderServiceChargeRemoved.cs` | 372 B | 7 | Domain Entity / Aggregate | Active |
| `Clovent.Restaurant` | `Orders/Events/OrderTableAssigned.cs` | `OrderTableAssigned.cs` | 345 B | 7 | Domain Entity / Aggregate | Active |
| `Clovent.Restaurant` | `Orders/Events/OrderVoided.cs` | `OrderVoided.cs` | 248 B | 6 | Domain Entity / Aggregate | Active |
| `Clovent.Restaurant` | `Orders/IOrderNumberSequenceRepository.cs` | `IOrderNumberSequenceRepository.cs` | 572 B | 11 | Domain Entity / Aggregate | Active |
| `Clovent.Restaurant` | `Orders/IOrderRepository.cs` | `IOrderRepository.cs` | 1,408 B | 25 | Domain Entity / Aggregate | Active |
| `Clovent.Restaurant` | `Orders/Order.cs` | `Order.cs` | 16,970 B | 386 | Domain Entity / Aggregate | Active |
| `Clovent.Restaurant` | `Orders/OrderId.cs` | `OrderId.cs` | 639 B | 16 | Domain Entity / Aggregate | Active |
| `Clovent.Restaurant` | `Orders/OrderNumberSequence.cs` | `OrderNumberSequence.cs` | 3,897 B | 81 | Domain Entity / Aggregate | Active |
| `Clovent.Restaurant` | `Orders/OrderNumberSequenceId.cs` | `OrderNumberSequenceId.cs` | 710 B | 16 | Domain Entity / Aggregate | Active |
| `Clovent.Restaurant` | `Orders/OrderStatus.cs` | `OrderStatus.cs` | 710 B | 20 | Domain Entity / Aggregate | Active |
| `Clovent.Restaurant` | `Orders/OrderType.cs` | `OrderType.cs` | 341 B | 11 | Domain Entity / Aggregate | Active |
| `Clovent.Restaurant` | `Orders/ValueObjects/OrderNumber.cs` | `OrderNumber.cs` | 1,942 B | 50 | Domain Entity / Aggregate | Active |
| `Clovent.Restaurant` | `PaymentMethods/Events/PaymentMethodActivated.cs` | `PaymentMethodActivated.cs` | 282 B | 6 | Domain Entity / Aggregate | Active |
| `Clovent.Restaurant` | `PaymentMethods/Events/PaymentMethodCreated.cs` | `PaymentMethodCreated.cs` | 356 B | 7 | Domain Entity / Aggregate | Active |
| `Clovent.Restaurant` | `PaymentMethods/Events/PaymentMethodDeactivated.cs` | `PaymentMethodDeactivated.cs` | 282 B | 6 | Domain Entity / Aggregate | Active |
| `Clovent.Restaurant` | `PaymentMethods/Events/PaymentMethodRenamed.cs` | `PaymentMethodRenamed.cs` | 356 B | 7 | Domain Entity / Aggregate | Active |
| `Clovent.Restaurant` | `PaymentMethods/IPaymentMethodRepository.cs` | `IPaymentMethodRepository.cs` | 720 B | 14 | Domain Entity / Aggregate | Active |
| `Clovent.Restaurant` | `PaymentMethods/PaymentMethod.cs` | `PaymentMethod.cs` | 2,885 B | 71 | Domain Entity / Aggregate | Active |
| `Clovent.Restaurant` | `PaymentMethods/PaymentMethodId.cs` | `PaymentMethodId.cs` | 686 B | 16 | Domain Entity / Aggregate | Active |
| `Clovent.Restaurant` | `PaymentMethods/ValueObjects/PaymentMethodName.cs` | `PaymentMethodName.cs` | 1,360 B | 39 | Domain Entity / Aggregate | Active |
| `Clovent.Restaurant` | `Payments/Events/PaymentCreated.cs` | `PaymentCreated.cs` | 389 B | 8 | Domain Entity / Aggregate | Active |
| `Clovent.Restaurant` | `Payments/Events/PaymentVoided.cs` | `PaymentVoided.cs` | 242 B | 6 | Domain Entity / Aggregate | Active |
| `Clovent.Restaurant` | `Payments/IPaymentRepository.cs` | `IPaymentRepository.cs` | 976 B | 20 | Domain Entity / Aggregate | Active |
| `Clovent.Restaurant` | `Payments/Payment.cs` | `Payment.cs` | 3,464 B | 74 | Domain Entity / Aggregate | Active |
| `Clovent.Restaurant` | `Payments/PaymentId.cs` | `PaymentId.cs` | 650 B | 16 | Domain Entity / Aggregate | Active |
| `Clovent.Restaurant` | `QuickOrderTemplates/IQuickOrderTemplateRepository.cs` | `IQuickOrderTemplateRepository.cs` | 1,209 B | 20 | Domain Entity / Aggregate | Active |
| `Clovent.Restaurant` | `QuickOrderTemplates/QuickOrderTemplate.cs` | `QuickOrderTemplate.cs` | 5,811 B | 143 | Domain Entity / Aggregate | Active |
| `Clovent.Restaurant` | `QuickOrderTemplates/QuickOrderTemplateId.cs` | `QuickOrderTemplateId.cs` | 716 B | 16 | Domain Entity / Aggregate | Active |
| `Clovent.Restaurant` | `QuickOrderTemplates/QuickOrderTemplateItem.cs` | `QuickOrderTemplateItem.cs` | 3,151 B | 73 | Domain Entity / Aggregate | Active |
| `Clovent.Restaurant` | `QuickOrderTemplates/QuickOrderTemplateItemId.cs` | `QuickOrderTemplateItemId.cs` | 733 B | 16 | Domain Entity / Aggregate | Active |
| `Clovent.Restaurant` | `RestaurantDomainException.cs` | `RestaurantDomainException.cs` | 13,045 B | 212 | Domain Entity / Aggregate | Active |
| `Clovent.Restaurant` | `RestaurantDomainException.cs.backup-m3-20260817-115714` | `RestaurantDomainException.cs.backup-m3-20260817-115714` | 10,110 B | 0 | Domain Entity / Aggregate | Active |
| `Clovent.Restaurant` | `RestaurantDomainException.cs.backup-m4m5-20260817-121628` | `RestaurantDomainException.cs.backup-m4m5-20260817-121628` | 10,708 B | 0 | Domain Entity / Aggregate | Active |
| `Clovent.Restaurant` | `RestaurantDomainException.cs.backup-vacateguard-20260817-135631` | `RestaurantDomainException.cs.backup-vacateguard-20260817-135631` | 11,737 B | 0 | Domain Entity / Aggregate | Active |
| `Clovent.Restaurant` | `Sales/DailySalesSequence.cs` | `DailySalesSequence.cs` | 1,971 B | 46 | Domain Entity / Aggregate | Active |
| `Clovent.Restaurant` | `Sales/DailySalesSequenceId.cs` | `DailySalesSequenceId.cs` | 702 B | 16 | Domain Entity / Aggregate | Active |
| `Clovent.Restaurant` | `Sales/Events/DailySalesSequenceAdvanced.cs` | `DailySalesSequenceAdvanced.cs` | 401 B | 7 | Domain Entity / Aggregate | Active |
| `Clovent.Restaurant` | `Sales/IDailySalesSequenceRepository.cs` | `IDailySalesSequenceRepository.cs` | 641 B | 13 | Domain Entity / Aggregate | Active |
| `Clovent.Restaurant` | `ServiceCharges/Events/ServiceChargeCreated.cs` | `ServiceChargeCreated.cs` | 380 B | 7 | Domain Entity / Aggregate | Active |
| `Clovent.Restaurant` | `ServiceCharges/IServiceChargeRepository.cs` | `IServiceChargeRepository.cs` | 797 B | 16 | Domain Entity / Aggregate | Active |
| `Clovent.Restaurant` | `ServiceCharges/ServiceCharge.cs` | `ServiceCharge.cs` | 3,274 B | 71 | Domain Entity / Aggregate | Active |
| `Clovent.Restaurant` | `ServiceCharges/ServiceChargeId.cs` | `ServiceChargeId.cs` | 686 B | 16 | Domain Entity / Aggregate | Active |
| `Clovent.Restaurant` | `ServiceCharges/ServiceChargeType.cs` | `ServiceChargeType.cs` | 345 B | 11 | Domain Entity / Aggregate | Active |
| `Clovent.Restaurant` | `Shared/RestaurantStatus.cs` | `RestaurantStatus.cs` | 913 B | 21 | Domain Entity / Aggregate | Active |
| `Clovent.Restaurant` | `Shifts/CashMovement.cs` | `CashMovement.cs` | 2,378 B | 79 | Domain Entity / Aggregate | Active |
| `Clovent.Restaurant` | `Shifts/CashMovementId.cs` | `CashMovementId.cs` | 670 B | 16 | Domain Entity / Aggregate | Active |
| `Clovent.Restaurant` | `Shifts/CashMovementType.cs` | `CashMovementType.cs` | 407 B | 11 | Domain Entity / Aggregate | Active |
| `Clovent.Restaurant` | `Shifts/Events/CashMovementRecorded.cs` | `CashMovementRecorded.cs` | 430 B | 15 | Domain Entity / Aggregate | Active |
| `Clovent.Restaurant` | `Shifts/Events/ShiftClosed.cs` | `ShiftClosed.cs` | 394 B | 14 | Domain Entity / Aggregate | Active |
| `Clovent.Restaurant` | `Shifts/Events/ShiftOpened.cs` | `ShiftOpened.cs` | 560 B | 19 | Domain Entity / Aggregate | Active |
| `Clovent.Restaurant` | `Shifts/IShiftRepository.cs` | `IShiftRepository.cs` | 1,794 B | 39 | Domain Entity / Aggregate | Active |
| `Clovent.Restaurant` | `Shifts/Shift.cs` | `Shift.cs` | 8,492 B | 242 | Domain Entity / Aggregate | Active |
| `Clovent.Restaurant` | `Shifts/ShiftId.cs` | `ShiftId.cs` | 638 B | 16 | Domain Entity / Aggregate | Active |
| `Clovent.Restaurant` | `Shifts/ShiftStatus.cs` | `ShiftStatus.cs` | 463 B | 14 | Domain Entity / Aggregate | Active |
| `Clovent.Restaurant` | `SmartRecommendations/IRecommendationRuleRepository.cs` | `IRecommendationRuleRepository.cs` | 1,067 B | 20 | Domain Entity / Aggregate | Active |
| `Clovent.Restaurant` | `SmartRecommendations/RecommendationRule.cs` | `RecommendationRule.cs` | 7,268 B | 191 | Domain Entity / Aggregate | Active |
| `Clovent.Restaurant` | `SmartRecommendations/RecommendationRuleId.cs` | `RecommendationRuleId.cs` | 717 B | 16 | Domain Entity / Aggregate | Active |
| `Clovent.Restaurant` | `Tables/Events/TableActivated.cs` | `TableActivated.cs` | 242 B | 6 | Domain Entity / Aggregate | Active |
| `Clovent.Restaurant` | `Tables/Events/TableCapacityChanged.cs` | `TableCapacityChanged.cs` | 272 B | 6 | Domain Entity / Aggregate | Active |
| `Clovent.Restaurant` | `Tables/Events/TableCreated.cs` | `TableCreated.cs` | 380 B | 8 | Domain Entity / Aggregate | Active |
| `Clovent.Restaurant` | `Tables/Events/TableDeactivated.cs` | `TableDeactivated.cs` | 242 B | 6 | Domain Entity / Aggregate | Active |
| `Clovent.Restaurant` | `Tables/Events/TableOccupancyChanged.cs` | `TableOccupancyChanged.cs` | 315 B | 6 | Domain Entity / Aggregate | Active |
| `Clovent.Restaurant` | `Tables/ITableRepository.cs` | `ITableRepository.cs` | 952 B | 19 | Domain Entity / Aggregate | Active |
| `Clovent.Restaurant` | `Tables/Table.cs` | `Table.cs` | 8,239 B | 190 | Domain Entity / Aggregate | Active |
| `Clovent.Restaurant` | `Tables/TableId.cs` | `TableId.cs` | 638 B | 16 | Domain Entity / Aggregate | Active |
| `Clovent.Restaurant` | `Tables/TableOccupancyStatus.cs` | `TableOccupancyStatus.cs` | 803 B | 24 | Domain Entity / Aggregate | Active |
| `Clovent.Restaurant` | `Tables/ValueObjects/TableName.cs` | `TableName.cs` | 1,339 B | 41 | Domain Entity / Aggregate | Active |

### 3.3 Supporting Bounded Contexts (Authentication, Identity, Catalog, Inventory, MasterData, Platform, Domain)

| Project | Relative Path | Name | Size | Lines | Category / Layer | Status |
|---|---|---|---|---|---|---|
| `Clovent.Authentication.Application.Tests` | `Clovent.Authentication.Application.Tests.csproj` | `Clovent.Authentication.Application.Tests.csproj` | 879 B | 0 | Configuration / Project Definition | Active |
| `Clovent.Authentication.Application.Tests` | `Credentials/PasswordCommandTests.cs` | `PasswordCommandTests.cs` | 5,088 B | 113 | Test / QA Code | Active |
| `Clovent.Authentication.Application.Tests` | `Credentials/PinCommandTests.cs` | `PinCommandTests.cs` | 4,859 B | 119 | Test / QA Code | Active |
| `Clovent.Authentication.Application.Tests` | `Credentials/RecordCredentialCheckCommandHandlerTests.cs` | `RecordCredentialCheckCommandHandlerTests.cs` | 2,022 B | 52 | Test / QA Code | Active |
| `Clovent.Authentication.Application.Tests` | `DependencyInjection/SessionHandlersDiWiringTests.cs` | `SessionHandlersDiWiringTests.cs` | 3,233 B | 75 | Test / QA Code | Active |
| `Clovent.Authentication.Application.Tests` | `LoginAttempts/GetRecentLoginAttemptsQueryHandlerTests.cs` | `GetRecentLoginAttemptsQueryHandlerTests.cs` | 1,273 B | 29 | Test / QA Code | Active |
| `Clovent.Authentication.Application.Tests` | `LoginAttempts/RecordLoginAttemptCommandHandlerTests.cs` | `RecordLoginAttemptCommandHandlerTests.cs` | 3,854 B | 97 | Test / QA Code | Active |
| `Clovent.Authentication.Application.Tests` | `RefreshSessions/GetRefreshSessionQueryHandlerTests.cs` | `GetRefreshSessionQueryHandlerTests.cs` | 1,338 B | 36 | Test / QA Code | Active |
| `Clovent.Authentication.Application.Tests` | `RefreshSessions/IssueRefreshSessionCommandHandlerTests.cs` | `IssueRefreshSessionCommandHandlerTests.cs` | 1,726 B | 40 | Test / QA Code | Active |
| `Clovent.Authentication.Application.Tests` | `RefreshSessions/RevokeRefreshSessionCommandHandlerTests.cs` | `RevokeRefreshSessionCommandHandlerTests.cs` | 1,459 B | 35 | Test / QA Code | Active |
| `Clovent.Authentication.Application.Tests` | `RefreshSessions/RotateRefreshSessionCommandHandlerTests.cs` | `RotateRefreshSessionCommandHandlerTests.cs` | 1,677 B | 38 | Test / QA Code | Active |
| `Clovent.Authentication.Application.Tests` | `Sessions/ExpireSessionCommandHandlerTests.cs` | `ExpireSessionCommandHandlerTests.cs` | 2,521 B | 57 | Test / QA Code | Active |
| `Clovent.Authentication.Application.Tests` | `Sessions/GetActiveSessionsForUserQueryHandlerTests.cs` | `GetActiveSessionsForUserQueryHandlerTests.cs` | 1,309 B | 34 | Test / QA Code | Active |
| `Clovent.Authentication.Application.Tests` | `Sessions/LogOutSessionCommandHandlerTests.cs` | `LogOutSessionCommandHandlerTests.cs` | 2,448 B | 56 | Test / QA Code | Active |
| `Clovent.Authentication.Application.Tests` | `Sessions/RevokeSessionCommandHandlerTests.cs` | `RevokeSessionCommandHandlerTests.cs` | 2,446 B | 56 | Test / QA Code | Active |
| `Clovent.Authentication.Application.Tests` | `Sessions/SessionTerminationCascadeTests.cs` | `SessionTerminationCascadeTests.cs` | 1,925 B | 48 | Test / QA Code | Active |
| `Clovent.Authentication.Application.Tests` | `Sessions/StartSessionCommandHandlerTests.cs` | `StartSessionCommandHandlerTests.cs` | 2,061 B | 54 | Test / QA Code | Active |
| `Clovent.Authentication.Application.Tests` | `TestSupport/FakeIdentityUserService.cs` | `FakeIdentityUserService.cs` | 979 B | 29 | Test / QA Code | Active |
| `Clovent.Authentication.Application.Tests` | `TestSupport/FakeLoginAttemptRepository.cs` | `FakeLoginAttemptRepository.cs` | 1,499 B | 32 | Test / QA Code | Active |
| `Clovent.Authentication.Application.Tests` | `TestSupport/FakePasswordHasher.cs` | `FakePasswordHasher.cs` | 494 B | 9 | Test / QA Code | Active |
| `Clovent.Authentication.Application.Tests` | `TestSupport/FakePinHasher.cs` | `FakePinHasher.cs` | 507 B | 11 | Test / QA Code | Active |
| `Clovent.Authentication.Application.Tests` | `TestSupport/FakeRefreshSessionRepository.cs` | `FakeRefreshSessionRepository.cs` | 1,002 B | 22 | Test / QA Code | Active |
| `Clovent.Authentication.Application.Tests` | `TestSupport/FakeSessionRepository.cs` | `FakeSessionRepository.cs` | 922 B | 23 | Test / QA Code | Active |
| `Clovent.Authentication.Application.Tests` | `TestSupport/FakeTimeProvider.cs` | `FakeTimeProvider.cs` | 292 B | 10 | Test / QA Code | Active |
| `Clovent.Authentication.Application.Tests` | `TestSupport/FakeUserCredentialsRepository.cs` | `FakeUserCredentialsRepository.cs` | 1,078 B | 24 | Test / QA Code | Active |
| `Clovent.Authentication.Application` | `Clovent.Authentication.Application.csproj` | `Clovent.Authentication.Application.csproj` | 769 B | 0 | Configuration / Project Definition | Active |
| `Clovent.Authentication.Application` | `Credentials/Commands/ChangePasswordCommand.cs` | `ChangePasswordCommand.cs` | 1,811 B | 37 | Source Code | Active |
| `Clovent.Authentication.Application` | `Credentials/Commands/RecordCredentialCheckCommand.cs` | `RecordCredentialCheckCommand.cs` | 1,509 B | 37 | Source Code | Active |
| `Clovent.Authentication.Application` | `Credentials/Commands/ResetPasswordCommand.cs` | `ResetPasswordCommand.cs` | 2,488 B | 50 | Source Code | Active |
| `Clovent.Authentication.Application` | `Credentials/Commands/SetPinCommand.cs` | `SetPinCommand.cs` | 3,080 B | 68 | Source Code | Active |
| `Clovent.Authentication.Application` | `Credentials/Commands/UnlockUserCommand.cs` | `UnlockUserCommand.cs` | 1,258 B | 29 | Source Code | Active |
| `Clovent.Authentication.Application` | `DependencyInjection/ApplicationServiceCollectionExtensions.cs` | `ApplicationServiceCollectionExtensions.cs` | 2,093 B | 36 | Source Code | Active |
| `Clovent.Authentication.Application` | `DependencyInjection/ApplicationServiceCollectionExtensions.cs.backup-di-fix-20260817` | `ApplicationServiceCollectionExtensions.cs.backup-di-fix-20260817` | 1,459 B | 0 | Backup File | Duplicate / Obsolete |
| `Clovent.Authentication.Application` | `IIdentityUserService.cs` | `IIdentityUserService.cs` | 1,326 B | 24 | Source Code | Active |
| `Clovent.Authentication.Application` | `IPasswordHasher.cs` | `IPasswordHasher.cs` | 1,020 B | 21 | Source Code | Active |
| `Clovent.Authentication.Application` | `IPinHasher.cs` | `IPinHasher.cs` | 612 B | 11 | Source Code | Active |
| `Clovent.Authentication.Application` | `IUnitOfWork.cs` | `IUnitOfWork.cs` | 1,160 B | 21 | Source Code | Active |
| `Clovent.Authentication.Application` | `LoginAttempts/Commands/RecordLoginAttemptCommand.cs` | `RecordLoginAttemptCommand.cs` | 2,764 B | 64 | Source Code | Active |
| `Clovent.Authentication.Application` | `LoginAttempts/Dtos/LoginAttemptDto.cs` | `LoginAttemptDto.cs` | 713 B | 20 | Source Code | Active |
| `Clovent.Authentication.Application` | `LoginAttempts/Queries/GetRecentLoginAttemptsQuery.cs` | `GetRecentLoginAttemptsQuery.cs` | 1,185 B | 22 | Source Code | Active |
| `Clovent.Authentication.Application` | `NotFoundException.cs` | `NotFoundException.cs` | 630 B | 16 | Source Code | Active |
| `Clovent.Authentication.Application` | `RefreshSessions/Commands/IssueRefreshSessionCommand.cs` | `IssueRefreshSessionCommand.cs` | 1,485 B | 31 | Source Code | Active |
| `Clovent.Authentication.Application` | `RefreshSessions/Commands/RevokeRefreshSessionCommand.cs` | `RevokeRefreshSessionCommand.cs` | 1,027 B | 22 | Source Code | Active |
| `Clovent.Authentication.Application` | `RefreshSessions/Commands/RotateRefreshSessionCommand.cs` | `RotateRefreshSessionCommand.cs` | 1,467 B | 28 | Source Code | Active |
| `Clovent.Authentication.Application` | `RefreshSessions/Dtos/RefreshSessionDto.cs` | `RefreshSessionDto.cs` | 763 B | 20 | Source Code | Active |
| `Clovent.Authentication.Application` | `RefreshSessions/Queries/GetRefreshSessionQuery.cs` | `GetRefreshSessionQuery.cs` | 1,030 B | 21 | Source Code | Active |
| `Clovent.Authentication.Application` | `Sessions/Commands/ExpireSessionCommand.cs` | `ExpireSessionCommand.cs` | 1,097 B | 26 | Source Code | Active |
| `Clovent.Authentication.Application` | `Sessions/Commands/LogOutSessionCommand.cs` | `LogOutSessionCommand.cs` | 1,083 B | 26 | Source Code | Active |
| `Clovent.Authentication.Application` | `Sessions/Commands/RevokeSessionCommand.cs` | `RevokeSessionCommand.cs` | 1,073 B | 26 | Source Code | Active |
| `Clovent.Authentication.Application` | `Sessions/Commands/StartSessionCommand.cs` | `StartSessionCommand.cs` | 1,343 B | 33 | Source Code | Active |
| `Clovent.Authentication.Application` | `Sessions/Dtos/SessionDto.cs` | `SessionDto.cs` | 734 B | 22 | Source Code | Active |
| `Clovent.Authentication.Application` | `Sessions/Queries/GetActiveSessionsForUserQuery.cs` | `GetActiveSessionsForUserQuery.cs` | 1,007 B | 22 | Source Code | Active |
| `Clovent.Authentication.Application` | `Sessions/SessionTerminationCascade.cs` | `SessionTerminationCascade.cs` | 1,283 B | 25 | Source Code | Active |
| `Clovent.Authentication.Infrastructure.Tests` | `Clovent.Authentication.Infrastructure.Tests.csproj` | `Clovent.Authentication.Infrastructure.Tests.csproj` | 1,299 B | 0 | Configuration / Project Definition | Active |
| `Clovent.Authentication.Infrastructure.Tests` | `Persistence/UnitOfWorkBehaviorTests.cs` | `UnitOfWorkBehaviorTests.cs` | 1,796 B | 58 | Test / QA Code | Active |
| `Clovent.Authentication.Infrastructure.Tests` | `Persistence/UnitOfWorkTests.cs` | `UnitOfWorkTests.cs` | 1,082 B | 31 | Test / QA Code | Active |
| `Clovent.Authentication.Infrastructure.Tests` | `Repositories/LoginAttemptRepositoryFilteringTests.cs` | `LoginAttemptRepositoryFilteringTests.cs` | 2,745 B | 64 | Test / QA Code | Active |
| `Clovent.Authentication.Infrastructure.Tests` | `Repositories/LoginAttemptRepositoryTests.cs` | `LoginAttemptRepositoryTests.cs` | 1,218 B | 32 | Test / QA Code | Active |
| `Clovent.Authentication.Infrastructure.Tests` | `Repositories/RefreshSessionRepositoryTests.cs` | `RefreshSessionRepositoryTests.cs` | 3,166 B | 82 | Test / QA Code | Active |
| `Clovent.Authentication.Infrastructure.Tests` | `Repositories/SessionRepositoryTests.cs` | `SessionRepositoryTests.cs` | 3,414 B | 89 | Test / QA Code | Active |
| `Clovent.Authentication.Infrastructure.Tests` | `Repositories/UserCredentialsRepositoryTests.cs` | `UserCredentialsRepositoryTests.cs` | 3,107 B | 74 | Test / QA Code | Active |
| `Clovent.Authentication.Infrastructure.Tests` | `Security/Pbkdf2PasswordHasherTests.cs` | `Pbkdf2PasswordHasherTests.cs` | 1,191 B | 44 | Test / QA Code | Active |
| `Clovent.Authentication.Infrastructure.Tests` | `Security/Pbkdf2PinHasherTests.cs` | `Pbkdf2PinHasherTests.cs` | 609 B | 25 | Test / QA Code | Active |
| `Clovent.Authentication.Infrastructure.Tests` | `TestSupport/InMemoryTestBase.cs` | `InMemoryTestBase.cs` | 1,404 B | 31 | Test / QA Code | Active |
| `Clovent.Authentication.Infrastructure.Tests` | `TestSupport/SqliteTestBase.cs` | `SqliteTestBase.cs` | 1,674 B | 44 | Test / QA Code | Active |
| `Clovent.Authentication.Infrastructure` | `Clovent.Authentication.Infrastructure.csproj` | `Clovent.Authentication.Infrastructure.csproj` | 1,394 B | 0 | Configuration / Project Definition | Active |
| `Clovent.Authentication.Infrastructure` | `DependencyInjection/InfrastructureServiceCollectionExtensions.cs` | `InfrastructureServiceCollectionExtensions.cs` | 1,828 B | 35 | Source Code | Active |
| `Clovent.Authentication.Infrastructure` | `DependencyInjection/PersistenceServiceCollectionExtensions.cs` | `PersistenceServiceCollectionExtensions.cs` | 3,544 B | 64 | Source Code | Active |
| `Clovent.Authentication.Infrastructure` | `Persistence/AuthenticationDbContext.cs` | `AuthenticationDbContext.cs` | 1,417 B | 35 | Source Code | Active |
| `Clovent.Authentication.Infrastructure` | `Persistence/AuthenticationDbContextFactory.cs` | `AuthenticationDbContextFactory.cs` | 1,252 B | 26 | Source Code | Active |
| `Clovent.Authentication.Infrastructure` | `Persistence/AuthenticationPersistenceInitializer.cs` | `AuthenticationPersistenceInitializer.cs` | 532 B | 12 | Source Code | Active |
| `Clovent.Authentication.Infrastructure` | `Persistence/Configurations/LoginAttemptConfiguration.cs` | `LoginAttemptConfiguration.cs` | 1,434 B | 42 | Source Code | Active |
| `Clovent.Authentication.Infrastructure` | `Persistence/Configurations/RefreshSessionConfiguration.cs` | `RefreshSessionConfiguration.cs` | 1,249 B | 36 | Source Code | Active |
| `Clovent.Authentication.Infrastructure` | `Persistence/Configurations/SessionConfiguration.cs` | `SessionConfiguration.cs` | 1,532 B | 45 | Source Code | Active |
| `Clovent.Authentication.Infrastructure` | `Persistence/Configurations/UserCredentialsConfiguration.cs` | `UserCredentialsConfiguration.cs` | 1,975 B | 51 | Source Code | Active |
| `Clovent.Authentication.Infrastructure` | `Persistence/Migrations/20260727115243_InitialCreate.Designer.cs` | `20260727115243_InitialCreate.Designer.cs` | 5,974 B | 166 | Source Code | Active |
| `Clovent.Authentication.Infrastructure` | `Persistence/Migrations/20260727115243_InitialCreate.cs` | `20260727115243_InitialCreate.cs` | 6,774 B | 151 | Source Code | Active |
| `Clovent.Authentication.Infrastructure` | `Persistence/Migrations/AuthenticationDbContextModelSnapshot.cs` | `AuthenticationDbContextModelSnapshot.cs` | 5,881 B | 163 | Source Code | Active |
| `Clovent.Authentication.Infrastructure` | `Persistence/UnitOfWork.cs` | `UnitOfWork.cs` | 511 B | 11 | Source Code | Active |
| `Clovent.Authentication.Infrastructure` | `Persistence/UnitOfWorkBehavior.cs` | `UnitOfWorkBehavior.cs` | 1,258 B | 27 | Source Code | Active |
| `Clovent.Authentication.Infrastructure` | `Persistence/ValueConverters.cs` | `ValueConverters.cs` | 6,589 B | 119 | Source Code | Active |
| `Clovent.Authentication.Infrastructure` | `Repositories/LoginAttemptRepository.cs` | `LoginAttemptRepository.cs` | 1,755 B | 38 | Source Code | Active |
| `Clovent.Authentication.Infrastructure` | `Repositories/RefreshSessionRepository.cs` | `RefreshSessionRepository.cs` | 1,221 B | 24 | Source Code | Active |
| `Clovent.Authentication.Infrastructure` | `Repositories/SessionRepository.cs` | `SessionRepository.cs` | 1,130 B | 24 | Source Code | Active |
| `Clovent.Authentication.Infrastructure` | `Repositories/UserCredentialsRepository.cs` | `UserCredentialsRepository.cs` | 1,990 B | 46 | Source Code | Active |
| `Clovent.Authentication.Infrastructure` | `Security/Pbkdf2Hash.cs` | `Pbkdf2Hash.cs` | 2,356 B | 59 | Source Code | Active |
| `Clovent.Authentication.Infrastructure` | `Security/Pbkdf2PasswordHasher.cs` | `Pbkdf2PasswordHasher.cs` | 470 B | 13 | Source Code | Active |
| `Clovent.Authentication.Infrastructure` | `Security/Pbkdf2PinHasher.cs` | `Pbkdf2PinHasher.cs` | 435 B | 13 | Source Code | Active |
| `Clovent.Authentication.Tests` | `Clovent.Authentication.Tests.csproj` | `Clovent.Authentication.Tests.csproj` | 673 B | 0 | Configuration / Project Definition | Active |
| `Clovent.Authentication.Tests` | `Credentials/FailedAttemptsTests.cs` | `FailedAttemptsTests.cs` | 1,603 B | 69 | Test / QA Code | Active |
| `Clovent.Authentication.Tests` | `Credentials/PasswordHashTests.cs` | `PasswordHashTests.cs` | 848 B | 35 | Test / QA Code | Active |
| `Clovent.Authentication.Tests` | `Credentials/PasswordHistoryTests.cs` | `PasswordHistoryTests.cs` | 3,245 B | 102 | Test / QA Code | Active |
| `Clovent.Authentication.Tests` | `Credentials/PinHashTests.cs` | `PinHashTests.cs` | 879 B | 38 | Test / QA Code | Active |
| `Clovent.Authentication.Tests` | `Credentials/SecurityStampTests.cs` | `SecurityStampTests.cs` | 1,079 B | 46 | Test / QA Code | Active |
| `Clovent.Authentication.Tests` | `Credentials/UserCredentialsTests.cs` | `UserCredentialsTests.cs` | 3,121 B | 98 | Test / QA Code | Active |
| `Clovent.Authentication.Tests` | `Lockouts/LockoutPolicyTests.cs` | `LockoutPolicyTests.cs` | 1,330 B | 52 | Test / QA Code | Active |
| `Clovent.Authentication.Tests` | `LoginAttempts/LoginAttemptTests.cs` | `LoginAttemptTests.cs` | 1,971 B | 61 | Test / QA Code | Active |
| `Clovent.Authentication.Tests` | `Passwords/PasswordPolicyTests.cs` | `PasswordPolicyTests.cs` | 2,134 B | 80 | Test / QA Code | Active |
| `Clovent.Authentication.Tests` | `Pins/PinPolicyTests.cs` | `PinPolicyTests.cs` | 1,660 B | 66 | Test / QA Code | Active |
| `Clovent.Authentication.Tests` | `RefreshSessions/RefreshSessionTests.cs` | `RefreshSessionTests.cs` | 5,336 B | 161 | Test / QA Code | Active |
| `Clovent.Authentication.Tests` | `Sessions/SessionIdTests.cs` | `SessionIdTests.cs` | 397 B | 19 | Test / QA Code | Active |
| `Clovent.Authentication.Tests` | `Sessions/SessionTests.cs` | `SessionTests.cs` | 3,526 B | 126 | Test / QA Code | Active |
| `Clovent.Authentication.Tests` | `Shared/IpAddressTests.cs` | `IpAddressTests.cs` | 787 B | 33 | Test / QA Code | Active |
| `Clovent.Authentication` | `AuthenticationDomainException.cs` | `AuthenticationDomainException.cs` | 3,202 B | 58 | Source Code | Active |
| `Clovent.Authentication` | `Clovent.Authentication.csproj` | `Clovent.Authentication.csproj` | 455 B | 0 | Configuration / Project Definition | Active |
| `Clovent.Authentication` | `Credentials/Events/PasswordChanged.cs` | `PasswordChanged.cs` | 325 B | 7 | Source Code | Active |
| `Clovent.Authentication` | `Credentials/Events/PinChanged.cs` | `PinChanged.cs` | 315 B | 7 | Source Code | Active |
| `Clovent.Authentication` | `Credentials/Events/PinCleared.cs` | `PinCleared.cs` | 347 B | 7 | Source Code | Active |
| `Clovent.Authentication` | `Credentials/Events/UserCredentialsCreated.cs` | `UserCredentialsCreated.cs` | 350 B | 7 | Source Code | Active |
| `Clovent.Authentication` | `Credentials/FailedAttempts.cs` | `FailedAttempts.cs` | 1,588 B | 42 | Source Code | Active |
| `Clovent.Authentication` | `Credentials/IUserCredentialsRepository.cs` | `IUserCredentialsRepository.cs` | 1,428 B | 28 | Source Code | Active |
| `Clovent.Authentication` | `Credentials/PasswordHash.cs` | `PasswordHash.cs` | 1,251 B | 34 | Source Code | Active |
| `Clovent.Authentication` | `Credentials/PasswordHistory.cs` | `PasswordHistory.cs` | 2,572 B | 58 | Source Code | Active |
| `Clovent.Authentication` | `Credentials/PasswordHistoryEntry.cs` | `PasswordHistoryEntry.cs` | 215 B | 4 | Source Code | Active |
| `Clovent.Authentication` | `Credentials/PinHash.cs` | `PinHash.cs` | 1,287 B | 35 | Source Code | Active |
| `Clovent.Authentication` | `Credentials/SecurityStamp.cs` | `SecurityStamp.cs` | 1,562 B | 40 | Source Code | Active |
| `Clovent.Authentication` | `Credentials/UserCredentials.cs` | `UserCredentials.cs` | 5,441 B | 125 | Source Code | Active |
| `Clovent.Authentication` | `Credentials/UserCredentialsId.cs` | `UserCredentialsId.cs` | 697 B | 16 | Source Code | Active |
| `Clovent.Authentication` | `Lockouts/LockoutPolicy.cs` | `LockoutPolicy.cs` | 2,462 B | 51 | Source Code | Active |
| `Clovent.Authentication` | `LoginAttempts/Events/LoginAttemptRecorded.cs` | `LoginAttemptRecorded.cs` | 382 B | 11 | Source Code | Active |
| `Clovent.Authentication` | `LoginAttempts/ILoginAttemptRepository.cs` | `ILoginAttemptRepository.cs` | 1,421 B | 32 | Source Code | Active |
| `Clovent.Authentication` | `LoginAttempts/LoginAttempt.cs` | `LoginAttempt.cs` | 3,094 B | 75 | Source Code | Active |
| `Clovent.Authentication` | `LoginAttempts/LoginAttemptId.cs` | `LoginAttemptId.cs` | 684 B | 16 | Source Code | Active |
| `Clovent.Authentication` | `LoginAttempts/LoginOutcome.cs` | `LoginOutcome.cs` | 643 B | 20 | Source Code | Active |
| `Clovent.Authentication` | `Passwords/PasswordPolicy.cs` | `PasswordPolicy.cs` | 4,125 B | 93 | Source Code | Active |
| `Clovent.Authentication` | `Passwords/PasswordPolicyResult.cs` | `PasswordPolicyResult.cs` | 607 B | 13 | Source Code | Active |
| `Clovent.Authentication` | `Pins/PinPolicy.cs` | `PinPolicy.cs` | 3,881 B | 97 | Source Code | Active |
| `Clovent.Authentication` | `Pins/PinPolicyResult.cs` | `PinPolicyResult.cs` | 582 B | 13 | Source Code | Active |
| `Clovent.Authentication` | `RefreshSessions/Events/RefreshSessionExpired.cs` | `RefreshSessionExpired.cs` | 334 B | 6 | Source Code | Active |
| `Clovent.Authentication` | `RefreshSessions/Events/RefreshSessionIssued.cs` | `RefreshSessionIssued.cs` | 373 B | 7 | Source Code | Active |
| `Clovent.Authentication` | `RefreshSessions/Events/RefreshSessionRevoked.cs` | `RefreshSessionRevoked.cs` | 300 B | 6 | Source Code | Active |
| `Clovent.Authentication` | `RefreshSessions/Events/RefreshSessionRotated.cs` | `RefreshSessionRotated.cs` | 353 B | 6 | Source Code | Active |
| `Clovent.Authentication` | `RefreshSessions/IRefreshSessionRepository.cs` | `IRefreshSessionRepository.cs` | 958 B | 20 | Source Code | Active |
| `Clovent.Authentication` | `RefreshSessions/RefreshSession.cs` | `RefreshSession.cs` | 5,394 B | 115 | Source Code | Active |
| `Clovent.Authentication` | `RefreshSessions/RefreshSessionId.cs` | `RefreshSessionId.cs` | 696 B | 16 | Source Code | Active |
| `Clovent.Authentication` | `RefreshSessions/RefreshSessionStatus.cs` | `RefreshSessionStatus.cs` | 550 B | 17 | Source Code | Active |
| `Clovent.Authentication` | `Sessions/Events/SessionExpired.cs` | `SessionExpired.cs` | 293 B | 6 | Source Code | Active |
| `Clovent.Authentication` | `Sessions/Events/SessionLoggedOut.cs` | `SessionLoggedOut.cs` | 284 B | 6 | Source Code | Active |
| `Clovent.Authentication` | `Sessions/Events/SessionRevoked.cs` | `SessionRevoked.cs` | 265 B | 6 | Source Code | Active |
| `Clovent.Authentication` | `Sessions/Events/SessionStarted.cs` | `SessionStarted.cs` | 308 B | 7 | Source Code | Active |
| `Clovent.Authentication` | `Sessions/ISessionRepository.cs` | `ISessionRepository.cs` | 872 B | 20 | Source Code | Active |
| `Clovent.Authentication` | `Sessions/Session.cs` | `Session.cs` | 5,436 B | 126 | Source Code | Active |
| `Clovent.Authentication` | `Sessions/SessionId.cs` | `SessionId.cs` | 654 B | 16 | Source Code | Active |
| `Clovent.Authentication` | `Sessions/SessionStatus.cs` | `SessionStatus.cs` | 477 B | 17 | Source Code | Active |
| `Clovent.Authentication` | `Shared/ValueObjects/IpAddress.cs` | `IpAddress.cs` | 1,242 B | 35 | Source Code | Active |
| `Clovent.Catalog.Application.Tests` | `Barcodes/BarcodeHandlerTests.cs` | `BarcodeHandlerTests.cs` | 3,301 B | 80 | Test / QA Code | Active |
| `Clovent.Catalog.Application.Tests` | `Brands/BrandHandlerTests.cs` | `BrandHandlerTests.cs` | 2,284 B | 62 | Test / QA Code | Active |
| `Clovent.Catalog.Application.Tests` | `Categories/ProductCategoryHandlerTests.cs` | `ProductCategoryHandlerTests.cs` | 3,154 B | 74 | Test / QA Code | Active |
| `Clovent.Catalog.Application.Tests` | `Clovent.Catalog.Application.Tests.csproj` | `Clovent.Catalog.Application.Tests.csproj` | 683 B | 0 | Configuration / Project Definition | Active |
| `Clovent.Catalog.Application.Tests` | `Groups/ProductGroupHandlerTests.cs` | `ProductGroupHandlerTests.cs` | 2,494 B | 62 | Test / QA Code | Active |
| `Clovent.Catalog.Application.Tests` | `Prices/ProductPriceHandlerTests.cs` | `ProductPriceHandlerTests.cs` | 3,038 B | 70 | Test / QA Code | Active |
| `Clovent.Catalog.Application.Tests` | `Products/CreateProductWithPriceCommandHandlerTests.cs` | `CreateProductWithPriceCommandHandlerTests.cs` | 3,338 B | 82 | Test / QA Code | Active |
| `Clovent.Catalog.Application.Tests` | `Products/ProductHandlerTests.cs` | `ProductHandlerTests.cs` | 3,637 B | 88 | Test / QA Code | Active |
| `Clovent.Catalog.Application.Tests` | `TestSupport/FakeBarcodeRepository.cs` | `FakeBarcodeRepository.cs` | 1,388 B | 30 | Test / QA Code | Active |
| `Clovent.Catalog.Application.Tests` | `TestSupport/FakeBrandRepository.cs` | `FakeBrandRepository.cs` | 785 B | 22 | Test / QA Code | Active |
| `Clovent.Catalog.Application.Tests` | `TestSupport/FakeProductCategoryRepository.cs` | `FakeProductCategoryRepository.cs` | 927 B | 22 | Test / QA Code | Active |
| `Clovent.Catalog.Application.Tests` | `TestSupport/FakeProductGroupRepository.cs` | `FakeProductGroupRepository.cs` | 855 B | 22 | Test / QA Code | Active |
| `Clovent.Catalog.Application.Tests` | `TestSupport/FakeProductPriceRepository.cs` | `FakeProductPriceRepository.cs` | 1,321 B | 27 | Test / QA Code | Active |
| `Clovent.Catalog.Application.Tests` | `TestSupport/FakeProductRepository.cs` | `FakeProductRepository.cs` | 1,049 B | 26 | Test / QA Code | Active |
| `Clovent.Catalog.Application.Tests` | `TestSupport/FakeProductVariantRepository.cs` | `FakeProductVariantRepository.cs` | 1,425 B | 30 | Test / QA Code | Active |
| `Clovent.Catalog.Application.Tests` | `TestSupport/FakeUnitOfMeasureRepository.cs` | `FakeUnitOfMeasureRepository.cs` | 1,111 B | 26 | Test / QA Code | Active |
| `Clovent.Catalog.Application.Tests` | `UnitsOfMeasure/UnitOfMeasureHandlerTests.cs` | `UnitOfMeasureHandlerTests.cs` | 2,563 B | 62 | Test / QA Code | Active |
| `Clovent.Catalog.Application.Tests` | `Variants/ProductVariantHandlerTests.cs` | `ProductVariantHandlerTests.cs` | 3,013 B | 67 | Test / QA Code | Active |
| `Clovent.Catalog.Application` | `Barcodes/Commands/ActivateBarcodeCommand.cs` | `ActivateBarcodeCommand.cs` | 897 B | 22 | Source Code | Active |
| `Clovent.Catalog.Application` | `Barcodes/Commands/CreateBarcodeCommand.cs` | `CreateBarcodeCommand.cs` | 1,549 B | 39 | Source Code | Active |
| `Clovent.Catalog.Application` | `Barcodes/Commands/DeactivateBarcodeCommand.cs` | `DeactivateBarcodeCommand.cs` | 911 B | 22 | Source Code | Active |
| `Clovent.Catalog.Application` | `Barcodes/Commands/MarkBarcodeAsPrimaryCommand.cs` | `MarkBarcodeAsPrimaryCommand.cs` | 1,503 B | 36 | Source Code | Active |
| `Clovent.Catalog.Application` | `Barcodes/Commands/UnmarkBarcodeAsPrimaryCommand.cs` | `UnmarkBarcodeAsPrimaryCommand.cs` | 971 B | 24 | Source Code | Active |
| `Clovent.Catalog.Application` | `Barcodes/Dtos/BarcodeDto.cs` | `BarcodeDto.cs` | 633 B | 11 | Source Code | Active |
| `Clovent.Catalog.Application` | `Barcodes/Queries/GetBarcodeByIdQuery.cs` | `GetBarcodeByIdQuery.cs` | 872 B | 21 | Source Code | Active |
| `Clovent.Catalog.Application` | `Barcodes/Queries/GetBarcodeByValueQuery.cs` | `GetBarcodeByValueQuery.cs` | 940 B | 22 | Source Code | Active |
| `Clovent.Catalog.Application` | `Barcodes/Queries/ListBarcodesByVariantQuery.cs` | `ListBarcodesByVariantQuery.cs` | 983 B | 21 | Source Code | Active |
| `Clovent.Catalog.Application` | `Brands/Commands/ActivateBrandCommand.cs` | `ActivateBrandCommand.cs` | 853 B | 22 | Source Code | Active |
| `Clovent.Catalog.Application` | `Brands/Commands/CreateBrandCommand.cs` | `CreateBrandCommand.cs` | 812 B | 21 | Source Code | Active |
| `Clovent.Catalog.Application` | `Brands/Commands/DeactivateBrandCommand.cs` | `DeactivateBrandCommand.cs` | 867 B | 22 | Source Code | Active |
| `Clovent.Catalog.Application` | `Brands/Commands/RenameBrandCommand.cs` | `RenameBrandCommand.cs` | 935 B | 23 | Source Code | Active |
| `Clovent.Catalog.Application` | `Brands/Dtos/BrandDto.cs` | `BrandDto.cs` | 506 B | 10 | Source Code | Active |
| `Clovent.Catalog.Application` | `Brands/Queries/GetBrandByIdQuery.cs` | `GetBrandByIdQuery.cs` | 830 B | 21 | Source Code | Active |
| `Clovent.Catalog.Application` | `Brands/Queries/ListBrandsQuery.cs` | `ListBrandsQuery.cs` | 761 B | 19 | Source Code | Active |
| `Clovent.Catalog.Application` | `Categories/Commands/ActivateProductCategoryCommand.cs` | `ActivateProductCategoryCommand.cs` | 1,040 B | 24 | Source Code | Active |
| `Clovent.Catalog.Application` | `Categories/Commands/CreateProductCategoryCommand.cs` | `CreateProductCategoryCommand.cs` | 1,129 B | 25 | Source Code | Active |
| `Clovent.Catalog.Application` | `Categories/Commands/DeactivateProductCategoryCommand.cs` | `DeactivateProductCategoryCommand.cs` | 1,054 B | 24 | Source Code | Active |
| `Clovent.Catalog.Application` | `Categories/Commands/RenameProductCategoryCommand.cs` | `RenameProductCategoryCommand.cs` | 1,136 B | 25 | Source Code | Active |
| `Clovent.Catalog.Application` | `Categories/Commands/SetProductCategoryColorCommand.cs` | `SetProductCategoryColorCommand.cs` | 1,159 B | 24 | Source Code | Active |
| `Clovent.Catalog.Application` | `Categories/Commands/SetProductCategoryParentCommand.cs` | `SetProductCategoryParentCommand.cs` | 1,206 B | 25 | Source Code | Active |
| `Clovent.Catalog.Application` | `Categories/Commands/SetProductCategorySortOrderCommand.cs` | `SetProductCategorySortOrderCommand.cs` | 1,155 B | 24 | Source Code | Active |
| `Clovent.Catalog.Application` | `Categories/Dtos/ProductCategoryDto.cs` | `ProductCategoryDto.cs` | 805 B | 24 | Source Code | Active |
| `Clovent.Catalog.Application` | `Categories/Queries/GetProductCategoryByIdQuery.cs` | `GetProductCategoryByIdQuery.cs` | 1,013 B | 22 | Source Code | Active |
| `Clovent.Catalog.Application` | `Categories/Queries/ListProductCategoriesQuery.cs` | `ListProductCategoriesQuery.cs` | 901 B | 20 | Source Code | Active |
| `Clovent.Catalog.Application` | `Clovent.Catalog.Application.csproj` | `Clovent.Catalog.Application.csproj` | 676 B | 0 | Configuration / Project Definition | Active |
| `Clovent.Catalog.Application` | `DependencyInjection/ApplicationServiceCollectionExtensions.cs` | `ApplicationServiceCollectionExtensions.cs` | 1,160 B | 20 | Source Code | Active |
| `Clovent.Catalog.Application` | `Groups/Commands/ActivateProductGroupCommand.cs` | `ActivateProductGroupCommand.cs` | 970 B | 23 | Source Code | Active |
| `Clovent.Catalog.Application` | `Groups/Commands/CreateProductGroupCommand.cs` | `CreateProductGroupCommand.cs` | 908 B | 22 | Source Code | Active |
| `Clovent.Catalog.Application` | `Groups/Commands/DeactivateProductGroupCommand.cs` | `DeactivateProductGroupCommand.cs` | 984 B | 23 | Source Code | Active |
| `Clovent.Catalog.Application` | `Groups/Commands/RenameProductGroupCommand.cs` | `RenameProductGroupCommand.cs` | 1,059 B | 24 | Source Code | Active |
| `Clovent.Catalog.Application` | `Groups/Dtos/ProductGroupDto.cs` | `ProductGroupDto.cs` | 557 B | 11 | Source Code | Active |
| `Clovent.Catalog.Application` | `Groups/Queries/GetProductGroupByIdQuery.cs` | `GetProductGroupByIdQuery.cs` | 947 B | 22 | Source Code | Active |
| `Clovent.Catalog.Application` | `Groups/Queries/ListProductGroupsQuery.cs` | `ListProductGroupsQuery.cs` | 843 B | 20 | Source Code | Active |
| `Clovent.Catalog.Application` | `IUnitOfWork.cs` | `IUnitOfWork.cs` | 504 B | 8 | Source Code | Active |
| `Clovent.Catalog.Application` | `NotFoundException.cs` | `NotFoundException.cs` | 487 B | 11 | Source Code | Active |
| `Clovent.Catalog.Application` | `Prices/Commands/ActivateProductPriceCommand.cs` | `ActivateProductPriceCommand.cs` | 969 B | 23 | Source Code | Active |
| `Clovent.Catalog.Application` | `Prices/Commands/CreateProductPriceCommand.cs` | `CreateProductPriceCommand.cs` | 1,146 B | 28 | Source Code | Active |
| `Clovent.Catalog.Application` | `Prices/Commands/DeactivateProductPriceCommand.cs` | `DeactivateProductPriceCommand.cs` | 983 B | 23 | Source Code | Active |
| `Clovent.Catalog.Application` | `Prices/Commands/UpdateProductPriceAmountCommand.cs` | `UpdateProductPriceAmountCommand.cs` | 1,030 B | 23 | Source Code | Active |
| `Clovent.Catalog.Application` | `Prices/Dtos/ProductPriceDto.cs` | `ProductPriceDto.cs` | 842 B | 26 | Source Code | Active |
| `Clovent.Catalog.Application` | `Prices/Queries/GetProductPriceByIdQuery.cs` | `GetProductPriceByIdQuery.cs` | 946 B | 22 | Source Code | Active |
| `Clovent.Catalog.Application` | `Prices/Queries/ListActiveProductPricesByTypeQuery.cs` | `ListActiveProductPricesByTypeQuery.cs` | 1,262 B | 26 | Source Code | Active |
| `Clovent.Catalog.Application` | `Prices/Queries/ListProductPricesByVariantQuery.cs` | `ListProductPricesByVariantQuery.cs` | 1,059 B | 21 | Source Code | Active |
| `Clovent.Catalog.Application` | `Products/Commands/ActivateProductCommand.cs` | `ActivateProductCommand.cs` | 897 B | 22 | Source Code | Active |
| `Clovent.Catalog.Application` | `Products/Commands/CreateProductCommand.cs` | `CreateProductCommand.cs` | 1,677 B | 43 | Source Code | Active |
| `Clovent.Catalog.Application` | `Products/Commands/CreateProductWithPriceCommand.cs` | `CreateProductWithPriceCommand.cs` | 4,195 B | 92 | Source Code | Active |
| `Clovent.Catalog.Application` | `Products/Commands/DeactivateProductCommand.cs` | `DeactivateProductCommand.cs` | 911 B | 22 | Source Code | Active |
| `Clovent.Catalog.Application` | `Products/Commands/RenameProductCommand.cs` | `RenameProductCommand.cs` | 983 B | 23 | Source Code | Active |
| `Clovent.Catalog.Application` | `Products/Commands/SetProductBrandCommand.cs` | `SetProductBrandCommand.cs` | 1,005 B | 23 | Source Code | Active |
| `Clovent.Catalog.Application` | `Products/Commands/SetProductCategoryCommand.cs` | `SetProductCategoryCommand.cs` | 1,046 B | 23 | Source Code | Active |
| `Clovent.Catalog.Application` | `Products/Commands/SetProductGroupCommand.cs` | `SetProductGroupCommand.cs` | 1,012 B | 23 | Source Code | Active |
| `Clovent.Catalog.Application` | `Products/Commands/SetProductTaxConfigurationCommand.cs` | `SetProductTaxConfigurationCommand.cs` | 1,152 B | 24 | Source Code | Active |
| `Clovent.Catalog.Application` | `Products/Dtos/ProductDto.cs` | `ProductDto.cs` | 1,008 B | 32 | Source Code | Active |
| `Clovent.Catalog.Application` | `Products/Queries/GetProductByIdQuery.cs` | `GetProductByIdQuery.cs` | 872 B | 21 | Source Code | Active |
| `Clovent.Catalog.Application` | `Products/Queries/GetProductBySkuQuery.cs` | `GetProductBySkuQuery.cs` | 901 B | 22 | Source Code | Active |
| `Clovent.Catalog.Application` | `Products/Queries/ListProductsQuery.cs` | `ListProductsQuery.cs` | 793 B | 19 | Source Code | Active |
| `Clovent.Catalog.Application` | `UnitsOfMeasure/Commands/ActivateUnitOfMeasureCommand.cs` | `ActivateUnitOfMeasureCommand.cs` | 1,008 B | 23 | Source Code | Active |
| `Clovent.Catalog.Application` | `UnitsOfMeasure/Commands/CreateUnitOfMeasureCommand.cs` | `CreateUnitOfMeasureCommand.cs` | 992 B | 22 | Source Code | Active |
| `Clovent.Catalog.Application` | `UnitsOfMeasure/Commands/DeactivateUnitOfMeasureCommand.cs` | `DeactivateUnitOfMeasureCommand.cs` | 1,022 B | 23 | Source Code | Active |
| `Clovent.Catalog.Application` | `UnitsOfMeasure/Commands/RenameUnitOfMeasureCommand.cs` | `RenameUnitOfMeasureCommand.cs` | 1,029 B | 23 | Source Code | Active |
| `Clovent.Catalog.Application` | `UnitsOfMeasure/Dtos/UnitOfMeasureDto.cs` | `UnitOfMeasureDto.cs` | 598 B | 11 | Source Code | Active |
| `Clovent.Catalog.Application` | `UnitsOfMeasure/Queries/GetUnitOfMeasureByIdQuery.cs` | `GetUnitOfMeasureByIdQuery.cs` | 986 B | 22 | Source Code | Active |
| `Clovent.Catalog.Application` | `UnitsOfMeasure/Queries/ListUnitsOfMeasureQuery.cs` | `ListUnitsOfMeasureQuery.cs` | 877 B | 20 | Source Code | Active |
| `Clovent.Catalog.Application` | `Variants/Commands/ActivateProductVariantCommand.cs` | `ActivateProductVariantCommand.cs` | 1,014 B | 23 | Source Code | Active |
| `Clovent.Catalog.Application` | `Variants/Commands/CreateProductVariantCommand.cs` | `CreateProductVariantCommand.cs` | 1,278 B | 31 | Source Code | Active |
| `Clovent.Catalog.Application` | `Variants/Commands/DeactivateProductVariantCommand.cs` | `DeactivateProductVariantCommand.cs` | 1,028 B | 23 | Source Code | Active |
| `Clovent.Catalog.Application` | `Variants/Commands/RenameProductVariantCommand.cs` | `RenameProductVariantCommand.cs` | 1,100 B | 24 | Source Code | Active |
| `Clovent.Catalog.Application` | `Variants/Commands/SetProductVariantSortOrderCommand.cs` | `SetProductVariantSortOrderCommand.cs` | 1,165 B | 23 | Source Code | Active |
| `Clovent.Catalog.Application` | `Variants/Commands/SetProductVariantUnitOfMeasureCommand.cs` | `SetProductVariantUnitOfMeasureCommand.cs` | 1,182 B | 24 | Source Code | Active |
| `Clovent.Catalog.Application` | `Variants/Dtos/ProductVariantDto.cs` | `ProductVariantDto.cs` | 1,409 B | 37 | Source Code | Active |
| `Clovent.Catalog.Application` | `Variants/Queries/GetProductVariantByIdQuery.cs` | `GetProductVariantByIdQuery.cs` | 989 B | 22 | Source Code | Active |
| `Clovent.Catalog.Application` | `Variants/Queries/GetProductVariantBySkuQuery.cs` | `GetProductVariantBySkuQuery.cs` | 990 B | 23 | Source Code | Active |
| `Clovent.Catalog.Application` | `Variants/Queries/ListProductVariantsByProductQuery.cs` | `ListProductVariantsByProductQuery.cs` | 1,033 B | 21 | Source Code | Active |
| `Clovent.Catalog.Application` | `Variants/Queries/ListProductVariantsQuery.cs` | `ListProductVariantsQuery.cs` | 1,534 B | 31 | Source Code | Active |
| `Clovent.Catalog.Infrastructure.Tests` | `Clovent.Catalog.Infrastructure.Tests.csproj` | `Clovent.Catalog.Infrastructure.Tests.csproj` | 1,032 B | 0 | Configuration / Project Definition | Active |
| `Clovent.Catalog.Infrastructure.Tests` | `Persistence/UnitOfWorkBehaviorTests.cs` | `UnitOfWorkBehaviorTests.cs` | 1,775 B | 58 | Test / QA Code | Active |
| `Clovent.Catalog.Infrastructure.Tests` | `Repositories/BarcodeRepositoryTests.cs` | `BarcodeRepositoryTests.cs` | 3,127 B | 85 | Test / QA Code | Active |
| `Clovent.Catalog.Infrastructure.Tests` | `Repositories/BrandRepositoryTests.cs` | `BrandRepositoryTests.cs` | 1,855 B | 57 | Test / QA Code | Active |
| `Clovent.Catalog.Infrastructure.Tests` | `Repositories/ProductCategoryRepositoryTests.cs` | `ProductCategoryRepositoryTests.cs` | 2,889 B | 79 | Test / QA Code | Active |
| `Clovent.Catalog.Infrastructure.Tests` | `Repositories/ProductGroupRepositoryTests.cs` | `ProductGroupRepositoryTests.cs` | 1,960 B | 57 | Test / QA Code | Active |
| `Clovent.Catalog.Infrastructure.Tests` | `Repositories/ProductPriceRepositoryTests.cs` | `ProductPriceRepositoryTests.cs` | 2,670 B | 69 | Test / QA Code | Active |
| `Clovent.Catalog.Infrastructure.Tests` | `Repositories/ProductRepositoryTests.cs` | `ProductRepositoryTests.cs` | 2,975 B | 84 | Test / QA Code | Active |
| `Clovent.Catalog.Infrastructure.Tests` | `Repositories/ProductVariantRepositoryTests.cs` | `ProductVariantRepositoryTests.cs` | 3,354 B | 90 | Test / QA Code | Active |
| `Clovent.Catalog.Infrastructure.Tests` | `Repositories/UnitOfMeasureRepositoryTests.cs` | `UnitOfMeasureRepositoryTests.cs` | 2,691 B | 77 | Test / QA Code | Active |
| `Clovent.Catalog.Infrastructure.Tests` | `TestSupport/SqliteTestBase.cs` | `SqliteTestBase.cs` | 1,179 B | 34 | Test / QA Code | Active |
| `Clovent.Catalog.Infrastructure` | `Clovent.Catalog.Infrastructure.csproj` | `Clovent.Catalog.Infrastructure.csproj` | 1,287 B | 0 | Configuration / Project Definition | Active |
| `Clovent.Catalog.Infrastructure` | `DependencyInjection/InfrastructureServiceCollectionExtensions.cs` | `InfrastructureServiceCollectionExtensions.cs` | 1,170 B | 21 | Source Code | Active |
| `Clovent.Catalog.Infrastructure` | `DependencyInjection/PersistenceServiceCollectionExtensions.cs` | `PersistenceServiceCollectionExtensions.cs` | 2,987 B | 58 | Source Code | Active |
| `Clovent.Catalog.Infrastructure` | `Migrations/20260727230617_InitialCreate.Designer.cs` | `20260727230617_InitialCreate.Designer.cs` | 10,419 B | 291 | Source Code | Active |
| `Clovent.Catalog.Infrastructure` | `Migrations/20260727230617_InitialCreate.cs` | `20260727230617_InitialCreate.cs` | 10,837 B | 237 | Source Code | Active |
| `Clovent.Catalog.Infrastructure` | `Migrations/20260804083813_AddCategoryColorAndSortOrder.Designer.cs` | `20260804083813_AddCategoryColorAndSortOrder.Designer.cs` | 10,801 B | 301 | Source Code | Active |
| `Clovent.Catalog.Infrastructure` | `Migrations/20260804083813_AddCategoryColorAndSortOrder.cs` | `20260804083813_AddCategoryColorAndSortOrder.cs` | 1,760 B | 57 | Source Code | Active |
| `Clovent.Catalog.Infrastructure` | `Migrations/20260821123728_UpdateMonetaryDecimalPrecision.Designer.cs` | `20260821123728_UpdateMonetaryDecimalPrecision.Designer.cs` | 10,805 B | 301 | Source Code | Active |
| `Clovent.Catalog.Infrastructure` | `Migrations/20260821123728_UpdateMonetaryDecimalPrecision.cs` | `20260821123728_UpdateMonetaryDecimalPrecision.cs` | 1,387 B | 44 | Source Code | Active |
| `Clovent.Catalog.Infrastructure` | `Migrations/CatalogDbContextModelSnapshot.cs` | `CatalogDbContextModelSnapshot.cs` | 10,671 B | 298 | Source Code | Active |
| `Clovent.Catalog.Infrastructure` | `Persistence/CatalogDbContext.cs` | `CatalogDbContext.cs` | 1,872 B | 50 | Source Code | Active |
| `Clovent.Catalog.Infrastructure` | `Persistence/CatalogDbContextFactory.cs` | `CatalogDbContextFactory.cs` | 807 B | 17 | Source Code | Active |
| `Clovent.Catalog.Infrastructure` | `Persistence/CatalogPersistenceInitializer.cs` | `CatalogPersistenceInitializer.cs` | 504 B | 12 | Source Code | Active |
| `Clovent.Catalog.Infrastructure` | `Persistence/Configurations/BarcodeConfiguration.cs` | `BarcodeConfiguration.cs` | 1,350 B | 41 | Source Code | Active |
| `Clovent.Catalog.Infrastructure` | `Persistence/Configurations/BrandConfiguration.cs` | `BrandConfiguration.cs` | 1,025 B | 33 | Source Code | Active |
| `Clovent.Catalog.Infrastructure` | `Persistence/Configurations/ProductCategoryConfiguration.cs` | `ProductCategoryConfiguration.cs` | 1,348 B | 39 | Source Code | Active |
| `Clovent.Catalog.Infrastructure` | `Persistence/Configurations/ProductConfiguration.cs` | `ProductConfiguration.cs` | 1,896 B | 56 | Source Code | Active |
| `Clovent.Catalog.Infrastructure` | `Persistence/Configurations/ProductGroupConfiguration.cs` | `ProductGroupConfiguration.cs` | 1,074 B | 33 | Source Code | Active |
| `Clovent.Catalog.Infrastructure` | `Persistence/Configurations/ProductPriceConfiguration.cs` | `ProductPriceConfiguration.cs` | 1,520 B | 46 | Source Code | Active |
| `Clovent.Catalog.Infrastructure` | `Persistence/Configurations/ProductVariantConfiguration.cs` | `ProductVariantConfiguration.cs` | 1,666 B | 50 | Source Code | Active |
| `Clovent.Catalog.Infrastructure` | `Persistence/Configurations/UnitOfMeasureConfiguration.cs` | `UnitOfMeasureConfiguration.cs` | 1,209 B | 36 | Source Code | Active |
| `Clovent.Catalog.Infrastructure` | `Persistence/UnitOfWork.cs` | `UnitOfWork.cs` | 556 B | 11 | Source Code | Active |
| `Clovent.Catalog.Infrastructure` | `Persistence/UnitOfWorkBehavior.cs` | `UnitOfWorkBehavior.cs` | 764 B | 17 | Source Code | Active |
| `Clovent.Catalog.Infrastructure` | `Persistence/ValueConverters.cs` | `ValueConverters.cs` | 6,990 B | 128 | Source Code | Active |
| `Clovent.Catalog.Infrastructure` | `Repositories/BarcodeRepository.cs` | `BarcodeRepository.cs` | 1,558 B | 31 | Source Code | Active |
| `Clovent.Catalog.Infrastructure` | `Repositories/BrandRepository.cs` | `BrandRepository.cs` | 927 B | 21 | Source Code | Active |
| `Clovent.Catalog.Infrastructure` | `Repositories/ProductCategoryRepository.cs` | `ProductCategoryRepository.cs` | 1,040 B | 21 | Source Code | Active |
| `Clovent.Catalog.Infrastructure` | `Repositories/ProductGroupRepository.cs` | `ProductGroupRepository.cs` | 997 B | 21 | Source Code | Active |
| `Clovent.Catalog.Infrastructure` | `Repositories/ProductPriceRepository.cs` | `ProductPriceRepository.cs` | 1,473 B | 26 | Source Code | Active |
| `Clovent.Catalog.Infrastructure` | `Repositories/ProductRepository.cs` | `ProductRepository.cs` | 1,204 B | 26 | Source Code | Active |
| `Clovent.Catalog.Infrastructure` | `Repositories/ProductVariantRepository.cs` | `ProductVariantRepository.cs` | 1,602 B | 31 | Source Code | Active |
| `Clovent.Catalog.Infrastructure` | `Repositories/UnitOfMeasureRepository.cs` | `UnitOfMeasureRepository.cs` | 1,302 B | 26 | Source Code | Active |
| `Clovent.Catalog.Tests` | `Barcodes/BarcodeTests.cs` | `BarcodeTests.cs` | 2,249 B | 74 | Test / QA Code | Active |
| `Clovent.Catalog.Tests` | `Brands/BrandTests.cs` | `BrandTests.cs` | 1,093 B | 40 | Test / QA Code | Active |
| `Clovent.Catalog.Tests` | `Categories/ProductCategoryTests.cs` | `ProductCategoryTests.cs` | 2,150 B | 68 | Test / QA Code | Active |
| `Clovent.Catalog.Tests` | `Clovent.Catalog.Tests.csproj` | `Clovent.Catalog.Tests.csproj` | 659 B | 0 | Configuration / Project Definition | Active |
| `Clovent.Catalog.Tests` | `Groups/ProductGroupTests.cs` | `ProductGroupTests.cs` | 1,213 B | 40 | Test / QA Code | Active |
| `Clovent.Catalog.Tests` | `Prices/ProductPriceTests.cs` | `ProductPriceTests.cs` | 2,124 B | 66 | Test / QA Code | Active |
| `Clovent.Catalog.Tests` | `Products/ProductTests.cs` | `ProductTests.cs` | 2,734 B | 88 | Test / QA Code | Active |
| `Clovent.Catalog.Tests` | `Shared/SkuTests.cs` | `SkuTests.cs` | 464 B | 22 | Test / QA Code | Active |
| `Clovent.Catalog.Tests` | `UnitsOfMeasure/UnitOfMeasureTests.cs` | `UnitOfMeasureTests.cs` | 1,644 B | 58 | Test / QA Code | Active |
| `Clovent.Catalog.Tests` | `Variants/ProductVariantTests.cs` | `ProductVariantTests.cs` | 2,064 B | 57 | Test / QA Code | Active |
| `Clovent.Catalog` | `Barcodes/Barcode.cs` | `Barcode.cs` | 3,932 B | 98 | Source Code | Active |
| `Clovent.Catalog` | `Barcodes/BarcodeId.cs` | `BarcodeId.cs` | 647 B | 16 | Source Code | Active |
| `Clovent.Catalog` | `Barcodes/Events/BarcodeActivated.cs` | `BarcodeActivated.cs` | 249 B | 6 | Source Code | Active |
| `Clovent.Catalog` | `Barcodes/Events/BarcodeCreated.cs` | `BarcodeCreated.cs` | 377 B | 8 | Source Code | Active |
| `Clovent.Catalog` | `Barcodes/Events/BarcodeDeactivated.cs` | `BarcodeDeactivated.cs` | 249 B | 6 | Source Code | Active |
| `Clovent.Catalog` | `Barcodes/Events/BarcodePrimaryChanged.cs` | `BarcodePrimaryChanged.cs` | 279 B | 6 | Source Code | Active |
| `Clovent.Catalog` | `Barcodes/IBarcodeRepository.cs` | `IBarcodeRepository.cs` | 1,181 B | 23 | Source Code | Active |
| `Clovent.Catalog` | `Barcodes/ValueObjects/BarcodeValue.cs` | `BarcodeValue.cs` | 1,644 B | 48 | Source Code | Active |
| `Clovent.Catalog` | `Brands/Brand.cs` | `Brand.cs` | 2,526 B | 71 | Source Code | Active |
| `Clovent.Catalog` | `Brands/BrandId.cs` | `BrandId.cs` | 635 B | 16 | Source Code | Active |
| `Clovent.Catalog` | `Brands/Events/BrandActivated.cs` | `BrandActivated.cs` | 239 B | 6 | Source Code | Active |
| `Clovent.Catalog` | `Brands/Events/BrandCreated.cs` | `BrandCreated.cs` | 294 B | 7 | Source Code | Active |
| `Clovent.Catalog` | `Brands/Events/BrandDeactivated.cs` | `BrandDeactivated.cs` | 239 B | 6 | Source Code | Active |
| `Clovent.Catalog` | `Brands/Events/BrandRenamed.cs` | `BrandRenamed.cs` | 290 B | 7 | Source Code | Active |
| `Clovent.Catalog` | `Brands/IBrandRepository.cs` | `IBrandRepository.cs` | 641 B | 14 | Source Code | Active |
| `Clovent.Catalog` | `Brands/ValueObjects/BrandName.cs` | `BrandName.cs` | 1,269 B | 39 | Source Code | Active |
| `Clovent.Catalog` | `CatalogDomainException.cs` | `CatalogDomainException.cs` | 5,004 B | 95 | Source Code | Active |
| `Clovent.Catalog` | `Categories/Events/ProductCategoryActivated.cs` | `ProductCategoryActivated.cs` | 283 B | 6 | Source Code | Active |
| `Clovent.Catalog` | `Categories/Events/ProductCategoryColorChanged.cs` | `ProductCategoryColorChanged.cs` | 311 B | 6 | Source Code | Active |
| `Clovent.Catalog` | `Categories/Events/ProductCategoryCreated.cs` | `ProductCategoryCreated.cs` | 352 B | 7 | Source Code | Active |
| `Clovent.Catalog` | `Categories/Events/ProductCategoryDeactivated.cs` | `ProductCategoryDeactivated.cs` | 283 B | 6 | Source Code | Active |
| `Clovent.Catalog` | `Categories/Events/ProductCategoryParentChanged.cs` | `ProductCategoryParentChanged.cs` | 336 B | 6 | Source Code | Active |
| `Clovent.Catalog` | `Categories/Events/ProductCategoryRenamed.cs` | `ProductCategoryRenamed.cs` | 348 B | 7 | Source Code | Active |
| `Clovent.Catalog` | `Categories/Events/ProductCategorySortOrderChanged.cs` | `ProductCategorySortOrderChanged.cs` | 322 B | 6 | Source Code | Active |
| `Clovent.Catalog` | `Categories/IProductCategoryRepository.cs` | `IProductCategoryRepository.cs` | 717 B | 14 | Source Code | Active |
| `Clovent.Catalog` | `Categories/ProductCategory.cs` | `ProductCategory.cs` | 5,965 B | 134 | Source Code | Active |
| `Clovent.Catalog` | `Categories/ProductCategoryId.cs` | `ProductCategoryId.cs` | 689 B | 16 | Source Code | Active |
| `Clovent.Catalog` | `Categories/ValueObjects/ProductCategoryName.cs` | `ProductCategoryName.cs` | 1,345 B | 39 | Source Code | Active |
| `Clovent.Catalog` | `Clovent.Catalog.csproj` | `Clovent.Catalog.csproj` | 459 B | 0 | Configuration / Project Definition | Active |
| `Clovent.Catalog` | `Groups/Events/ProductGroupActivated.cs` | `ProductGroupActivated.cs` | 267 B | 6 | Source Code | Active |
| `Clovent.Catalog` | `Groups/Events/ProductGroupCreated.cs` | `ProductGroupCreated.cs` | 329 B | 7 | Source Code | Active |
| `Clovent.Catalog` | `Groups/Events/ProductGroupDeactivated.cs` | `ProductGroupDeactivated.cs` | 267 B | 6 | Source Code | Active |
| `Clovent.Catalog` | `Groups/Events/ProductGroupRenamed.cs` | `ProductGroupRenamed.cs` | 325 B | 7 | Source Code | Active |
| `Clovent.Catalog` | `Groups/IProductGroupRepository.cs` | `IProductGroupRepository.cs` | 683 B | 14 | Source Code | Active |
| `Clovent.Catalog` | `Groups/ProductGroup.cs` | `ProductGroup.cs` | 2,740 B | 71 | Source Code | Active |
| `Clovent.Catalog` | `Groups/ProductGroupId.cs` | `ProductGroupId.cs` | 670 B | 16 | Source Code | Active |
| `Clovent.Catalog` | `Groups/ValueObjects/ProductGroupName.cs` | `ProductGroupName.cs` | 1,319 B | 39 | Source Code | Active |
| `Clovent.Catalog` | `Prices/Events/ProductPriceActivated.cs` | `ProductPriceActivated.cs` | 267 B | 6 | Source Code | Active |
| `Clovent.Catalog` | `Prices/Events/ProductPriceAmountChanged.cs` | `ProductPriceAmountChanged.cs` | 290 B | 6 | Source Code | Active |
| `Clovent.Catalog` | `Prices/Events/ProductPriceCreated.cs` | `ProductPriceCreated.cs` | 427 B | 8 | Source Code | Active |
| `Clovent.Catalog` | `Prices/Events/ProductPriceDeactivated.cs` | `ProductPriceDeactivated.cs` | 267 B | 6 | Source Code | Active |
| `Clovent.Catalog` | `Prices/IProductPriceRepository.cs` | `IProductPriceRepository.cs` | 1,569 B | 28 | Source Code | Active |
| `Clovent.Catalog` | `Prices/PriceType.cs` | `PriceType.cs` | 331 B | 11 | Source Code | Active |
| `Clovent.Catalog` | `Prices/ProductPrice.cs` | `ProductPrice.cs` | 4,653 B | 113 | Source Code | Active |
| `Clovent.Catalog` | `Prices/ProductPriceId.cs` | `ProductPriceId.cs` | 670 B | 16 | Source Code | Active |
| `Clovent.Catalog` | `Products/Events/ProductActivated.cs` | `ProductActivated.cs` | 249 B | 6 | Source Code | Active |
| `Clovent.Catalog` | `Products/Events/ProductBrandAssigned.cs` | `ProductBrandAssigned.cs` | 303 B | 7 | Source Code | Active |
| `Clovent.Catalog` | `Products/Events/ProductCategoryAssigned.cs` | `ProductCategoryAssigned.cs` | 326 B | 7 | Source Code | Active |
| `Clovent.Catalog` | `Products/Events/ProductCreated.cs` | `ProductCreated.cs` | 435 B | 9 | Source Code | Active |
| `Clovent.Catalog` | `Products/Events/ProductDeactivated.cs` | `ProductDeactivated.cs` | 249 B | 6 | Source Code | Active |
| `Clovent.Catalog` | `Products/Events/ProductGroupAssigned.cs` | `ProductGroupAssigned.cs` | 310 B | 7 | Source Code | Active |
| `Clovent.Catalog` | `Products/Events/ProductRenamed.cs` | `ProductRenamed.cs` | 304 B | 7 | Source Code | Active |
| `Clovent.Catalog` | `Products/Events/ProductTaxConfigurationChanged.cs` | `ProductTaxConfigurationChanged.cs` | 357 B | 7 | Source Code | Active |
| `Clovent.Catalog` | `Products/IProductRepository.cs` | `IProductRepository.cs` | 899 B | 19 | Source Code | Active |
| `Clovent.Catalog` | `Products/Product.cs` | `Product.cs` | 6,001 B | 164 | Source Code | Active |
| `Clovent.Catalog` | `Products/ProductId.cs` | `ProductId.cs` | 647 B | 16 | Source Code | Active |
| `Clovent.Catalog` | `Products/ValueObjects/ProductName.cs` | `ProductName.cs` | 1,301 B | 39 | Source Code | Active |
| `Clovent.Catalog` | `Products/ValueObjects/TaxConfiguration.cs` | `TaxConfiguration.cs` | 1,918 B | 49 | Source Code | Active |
| `Clovent.Catalog` | `Shared/CatalogStatus.cs` | `CatalogStatus.cs` | 769 B | 20 | Source Code | Active |
| `Clovent.Catalog` | `Shared/ValueObjects/Sku.cs` | `Sku.cs` | 2,011 B | 54 | Source Code | Active |
| `Clovent.Catalog` | `UnitsOfMeasure/Events/UnitOfMeasureActivated.cs` | `UnitOfMeasureActivated.cs` | 279 B | 6 | Source Code | Active |
| `Clovent.Catalog` | `UnitsOfMeasure/Events/UnitOfMeasureCreated.cs` | `UnitOfMeasureCreated.cs` | 364 B | 7 | Source Code | Active |
| `Clovent.Catalog` | `UnitsOfMeasure/Events/UnitOfMeasureDeactivated.cs` | `UnitOfMeasureDeactivated.cs` | 279 B | 6 | Source Code | Active |
| `Clovent.Catalog` | `UnitsOfMeasure/Events/UnitOfMeasureRenamed.cs` | `UnitOfMeasureRenamed.cs` | 299 B | 6 | Source Code | Active |
| `Clovent.Catalog` | `UnitsOfMeasure/IUnitOfMeasureRepository.cs` | `IUnitOfMeasureRepository.cs` | 957 B | 19 | Source Code | Active |
| `Clovent.Catalog` | `UnitsOfMeasure/UnitOfMeasure.cs` | `UnitOfMeasure.cs` | 3,675 B | 93 | Source Code | Active |
| `Clovent.Catalog` | `UnitsOfMeasure/UnitOfMeasureId.cs` | `UnitOfMeasureId.cs` | 683 B | 16 | Source Code | Active |
| `Clovent.Catalog` | `UnitsOfMeasure/ValueObjects/UnitOfMeasureCode.cs` | `UnitOfMeasureCode.cs` | 1,983 B | 53 | Source Code | Active |
| `Clovent.Catalog` | `Variants/Events/ProductVariantActivated.cs` | `ProductVariantActivated.cs` | 277 B | 6 | Source Code | Active |
| `Clovent.Catalog` | `Variants/Events/ProductVariantCreated.cs` | `ProductVariantCreated.cs` | 441 B | 9 | Source Code | Active |
| `Clovent.Catalog` | `Variants/Events/ProductVariantDeactivated.cs` | `ProductVariantDeactivated.cs` | 277 B | 6 | Source Code | Active |
| `Clovent.Catalog` | `Variants/Events/ProductVariantRenamed.cs` | `ProductVariantRenamed.cs` | 332 B | 7 | Source Code | Active |
| `Clovent.Catalog` | `Variants/Events/ProductVariantSortOrderChanged.cs` | `ProductVariantSortOrderChanged.cs` | 316 B | 6 | Source Code | Active |
| `Clovent.Catalog` | `Variants/Events/ProductVariantUnitOfMeasureChanged.cs` | `ProductVariantUnitOfMeasureChanged.cs` | 371 B | 7 | Source Code | Active |
| `Clovent.Catalog` | `Variants/IProductVariantRepository.cs` | `IProductVariantRepository.cs` | 1,191 B | 23 | Source Code | Active |
| `Clovent.Catalog` | `Variants/ProductVariant.cs` | `ProductVariant.cs` | 5,042 B | 116 | Source Code | Active |
| `Clovent.Catalog` | `Variants/ProductVariantId.cs` | `ProductVariantId.cs` | 682 B | 16 | Source Code | Active |
| `Clovent.Catalog` | `Variants/ValueObjects/VariantName.cs` | `VariantName.cs` | 1,316 B | 39 | Source Code | Active |
| `Clovent.Domain.Tests` | `AggregateRootTests.cs` | `AggregateRootTests.cs` | 1,103 B | 46 | Test / QA Code | Active |
| `Clovent.Domain.Tests` | `Clovent.Domain.Tests.csproj` | `Clovent.Domain.Tests.csproj` | 657 B | 0 | Configuration / Project Definition | Active |
| `Clovent.Domain.Tests` | `EntityTests.cs` | `EntityTests.cs` | 1,281 B | 58 | Test / QA Code | Active |
| `Clovent.Domain.Tests` | `TestSupport/TestAggregate.cs` | `TestAggregate.cs` | 366 B | 12 | Test / QA Code | Active |
| `Clovent.Domain.Tests` | `TestSupport/TestEntity.cs` | `TestEntity.cs` | 269 B | 13 | Test / QA Code | Active |
| `Clovent.Domain.Tests` | `TestSupport/TestValueObject.cs` | `TestValueObject.cs` | 709 B | 33 | Test / QA Code | Active |
| `Clovent.Domain.Tests` | `ValueObjectTests.cs` | `ValueObjectTests.cs` | 1,361 B | 63 | Test / QA Code | Active |
| `Clovent.Domain` | `AggregateRoot.cs` | `AggregateRoot.cs` | 1,417 B | 31 | Source Code | Active |
| `Clovent.Domain` | `Clovent.Domain.csproj` | `Clovent.Domain.csproj` | 271 B | 0 | Configuration / Project Definition | Active |
| `Clovent.Domain` | `DomainException.cs` | `DomainException.cs` | 595 B | 15 | Source Code | Active |
| `Clovent.Domain` | `Entity.cs` | `Entity.cs` | 1,502 B | 36 | Source Code | Active |
| `Clovent.Domain` | `IDomainEvent.cs` | `IDomainEvent.cs` | 532 B | 14 | Source Code | Active |
| `Clovent.Domain` | `ValueObject.cs` | `ValueObject.cs` | 1,458 B | 34 | Source Code | Active |
| `Clovent.Identity.Application.Tests` | `Authorization/AuthorizationPolicyProviderTests.cs` | `AuthorizationPolicyProviderTests.cs` | 1,338 B | 47 | Test / QA Code | Active |
| `Clovent.Identity.Application.Tests` | `Authorization/AuthorizationServiceTests.cs` | `AuthorizationServiceTests.cs` | 6,320 B | 167 | Test / QA Code | Active |
| `Clovent.Identity.Application.Tests` | `Authorization/PolicyWrapperTests.cs` | `PolicyWrapperTests.cs` | 1,794 B | 56 | Test / QA Code | Active |
| `Clovent.Identity.Application.Tests` | `Branches/BranchHandlerTests.cs` | `BranchHandlerTests.cs` | 5,143 B | 119 | Test / QA Code | Active |
| `Clovent.Identity.Application.Tests` | `Clovent.Identity.Application.Tests.csproj` | `Clovent.Identity.Application.Tests.csproj` | 685 B | 0 | Configuration / Project Definition | Active |
| `Clovent.Identity.Application.Tests` | `Companies/CompanyHandlerTests.cs` | `CompanyHandlerTests.cs` | 4,268 B | 100 | Test / QA Code | Active |
| `Clovent.Identity.Application.Tests` | `Organizations/OrganizationHandlerTests.cs` | `OrganizationHandlerTests.cs` | 4,158 B | 99 | Test / QA Code | Active |
| `Clovent.Identity.Application.Tests` | `Permissions/PermissionHandlerTests.cs` | `PermissionHandlerTests.cs` | 897 B | 23 | Test / QA Code | Active |
| `Clovent.Identity.Application.Tests` | `Roles/RoleHandlerTests.cs` | `RoleHandlerTests.cs` | 4,137 B | 102 | Test / QA Code | Active |
| `Clovent.Identity.Application.Tests` | `TestSupport/FakeBranchRepository.cs` | `FakeBranchRepository.cs` | 915 B | 23 | Test / QA Code | Active |
| `Clovent.Identity.Application.Tests` | `TestSupport/FakeCompanyRepository.cs` | `FakeCompanyRepository.cs` | 966 B | 23 | Test / QA Code | Active |
| `Clovent.Identity.Application.Tests` | `TestSupport/FakeOrganizationRepository.cs` | `FakeOrganizationRepository.cs` | 941 B | 22 | Test / QA Code | Active |
| `Clovent.Identity.Application.Tests` | `TestSupport/FakePermissionCache.cs` | `FakePermissionCache.cs` | 989 B | 30 | Test / QA Code | Active |
| `Clovent.Identity.Application.Tests` | `TestSupport/FakePermissionRepository.cs` | `FakePermissionRepository.cs` | 1,133 B | 26 | Test / QA Code | Active |
| `Clovent.Identity.Application.Tests` | `TestSupport/FakeRoleRepository.cs` | `FakeRoleRepository.cs` | 977 B | 26 | Test / QA Code | Active |
| `Clovent.Identity.Application.Tests` | `TestSupport/FakeUserRepository.cs` | `FakeUserRepository.cs` | 2,271 B | 56 | Test / QA Code | Active |
| `Clovent.Identity.Application.Tests` | `TestSupport/RecordingAuthorizationService.cs` | `RecordingAuthorizationService.cs` | 1,173 B | 25 | Test / QA Code | Active |
| `Clovent.Identity.Application.Tests` | `Users/UserHandlerTests.cs` | `UserHandlerTests.cs` | 7,358 B | 181 | Test / QA Code | Active |
| `Clovent.Identity.Application` | `Authorization/AuthorizationPolicy.cs` | `AuthorizationPolicy.cs` | 554 B | 10 | Source Code | Active |
| `Clovent.Identity.Application` | `Authorization/AuthorizationPolicyProvider.cs` | `AuthorizationPolicyProvider.cs` | 806 B | 20 | Source Code | Active |
| `Clovent.Identity.Application` | `Authorization/AuthorizationService.cs` | `AuthorizationService.cs` | 3,275 B | 94 | Source Code | Active |
| `Clovent.Identity.Application` | `Authorization/FeatureAuthorizationPolicy.cs` | `FeatureAuthorizationPolicy.cs` | 682 B | 12 | Source Code | Active |
| `Clovent.Identity.Application` | `Authorization/IAuthorizationPolicyProvider.cs` | `IAuthorizationPolicyProvider.cs` | 666 B | 11 | Source Code | Active |
| `Clovent.Identity.Application` | `Authorization/IAuthorizationService.cs` | `IAuthorizationService.cs` | 1,642 B | 25 | Source Code | Active |
| `Clovent.Identity.Application` | `Authorization/IFeatureAuthorizationPolicy.cs` | `IFeatureAuthorizationPolicy.cs` | 492 B | 8 | Source Code | Active |
| `Clovent.Identity.Application` | `Authorization/IMenuAuthorizationPolicy.cs` | `IMenuAuthorizationPolicy.cs` | 503 B | 8 | Source Code | Active |
| `Clovent.Identity.Application` | `Authorization/IModuleAuthorizationPolicy.cs` | `IModuleAuthorizationPolicy.cs` | 493 B | 8 | Source Code | Active |
| `Clovent.Identity.Application` | `Authorization/IPermissionCache.cs` | `IPermissionCache.cs` | 1,296 B | 23 | Source Code | Active |
| `Clovent.Identity.Application` | `Authorization/MenuAuthorizationPolicy.cs` | `MenuAuthorizationPolicy.cs` | 675 B | 12 | Source Code | Active |
| `Clovent.Identity.Application` | `Authorization/ModuleAuthorizationPolicy.cs` | `ModuleAuthorizationPolicy.cs` | 716 B | 12 | Source Code | Active |
| `Clovent.Identity.Application` | `Branches/Commands/ActivateBranchCommand.cs` | `ActivateBranchCommand.cs` | 898 B | 24 | Source Code | Active |
| `Clovent.Identity.Application` | `Branches/Commands/CreateBranchCommand.cs` | `CreateBranchCommand.cs` | 1,953 B | 47 | Source Code | Active |
| `Clovent.Identity.Application` | `Branches/Commands/DeactivateBranchCommand.cs` | `DeactivateBranchCommand.cs` | 912 B | 24 | Source Code | Active |
| `Clovent.Identity.Application` | `Branches/Commands/RenameBranchCommand.cs` | `RenameBranchCommand.cs` | 984 B | 25 | Source Code | Active |
| `Clovent.Identity.Application` | `Branches/Commands/SetBranchAddressCommand.cs` | `SetBranchAddressCommand.cs` | 1,447 B | 34 | Source Code | Active |
| `Clovent.Identity.Application` | `Branches/Dtos/BranchDto.cs` | `BranchDto.cs` | 889 B | 30 | Source Code | Active |
| `Clovent.Identity.Application` | `Branches/Queries/GetBranchByIdQuery.cs` | `GetBranchByIdQuery.cs` | 873 B | 22 | Source Code | Active |
| `Clovent.Identity.Application` | `Branches/Queries/ListBranchesByCompanyQuery.cs` | `ListBranchesByCompanyQuery.cs` | 974 B | 21 | Source Code | Active |
| `Clovent.Identity.Application` | `Clovent.Identity.Application.csproj` | `Clovent.Identity.Application.csproj` | 678 B | 0 | Configuration / Project Definition | Active |
| `Clovent.Identity.Application` | `Companies/Commands/ActivateCompanyCommand.cs` | `ActivateCompanyCommand.cs` | 922 B | 24 | Source Code | Active |
| `Clovent.Identity.Application` | `Companies/Commands/CreateCompanyCommand.cs` | `CreateCompanyCommand.cs` | 1,500 B | 32 | Source Code | Active |
| `Clovent.Identity.Application` | `Companies/Commands/DeactivateCompanyCommand.cs` | `DeactivateCompanyCommand.cs` | 936 B | 24 | Source Code | Active |
| `Clovent.Identity.Application` | `Companies/Commands/RenameCompanyCommand.cs` | `RenameCompanyCommand.cs` | 1,010 B | 25 | Source Code | Active |
| `Clovent.Identity.Application` | `Companies/Commands/SetCompanyTaxIdCommand.cs` | `SetCompanyTaxIdCommand.cs` | 1,053 B | 25 | Source Code | Active |
| `Clovent.Identity.Application` | `Companies/Dtos/CompanyDto.cs` | `CompanyDto.cs` | 794 B | 24 | Source Code | Active |
| `Clovent.Identity.Application` | `Companies/Queries/GetCompanyByIdQuery.cs` | `GetCompanyByIdQuery.cs` | 896 B | 22 | Source Code | Active |
| `Clovent.Identity.Application` | `Companies/Queries/ListCompaniesByOrganizationQuery.cs` | `ListCompaniesByOrganizationQuery.cs` | 1,046 B | 21 | Source Code | Active |
| `Clovent.Identity.Application` | `DependencyInjection/ApplicationServiceCollectionExtensions.cs` | `ApplicationServiceCollectionExtensions.cs` | 2,505 B | 40 | Source Code | Active |
| `Clovent.Identity.Application` | `IUnitOfWork.cs` | `IUnitOfWork.cs` | 808 B | 17 | Source Code | Active |
| `Clovent.Identity.Application` | `NotFoundException.cs` | `NotFoundException.cs` | 643 B | 16 | Source Code | Active |
| `Clovent.Identity.Application` | `Organizations/Commands/ActivateOrganizationCommand.cs` | `ActivateOrganizationCommand.cs` | 1,061 B | 25 | Source Code | Active |
| `Clovent.Identity.Application` | `Organizations/Commands/CreateOrganizationCommand.cs` | `CreateOrganizationCommand.cs` | 1,139 B | 26 | Source Code | Active |
| `Clovent.Identity.Application` | `Organizations/Commands/DeactivateOrganizationCommand.cs` | `DeactivateOrganizationCommand.cs` | 1,075 B | 25 | Source Code | Active |
| `Clovent.Identity.Application` | `Organizations/Commands/RenameOrganizationCommand.cs` | `RenameOrganizationCommand.cs` | 1,157 B | 26 | Source Code | Active |
| `Clovent.Identity.Application` | `Organizations/Commands/SetOrganizationTaxIdCommand.cs` | `SetOrganizationTaxIdCommand.cs` | 1,192 B | 26 | Source Code | Active |
| `Clovent.Identity.Application` | `Organizations/Dtos/OrganizationDto.cs` | `OrganizationDto.cs` | 807 B | 22 | Source Code | Active |
| `Clovent.Identity.Application` | `Organizations/Queries/GetOrganizationByIdQuery.cs` | `GetOrganizationByIdQuery.cs` | 1,008 B | 22 | Source Code | Active |
| `Clovent.Identity.Application` | `Organizations/Queries/ListOrganizationsQuery.cs` | `ListOrganizationsQuery.cs` | 904 B | 20 | Source Code | Active |
| `Clovent.Identity.Application` | `Permissions/Dtos/PermissionDto.cs` | `PermissionDto.cs` | 622 B | 18 | Source Code | Active |
| `Clovent.Identity.Application` | `Permissions/Queries/ListPermissionsQuery.cs` | `ListPermissionsQuery.cs` | 942 B | 20 | Source Code | Active |
| `Clovent.Identity.Application` | `Roles/Commands/AssignPermissionToRoleCommand.cs` | `AssignPermissionToRoleCommand.cs` | 1,265 B | 29 | Source Code | Active |
| `Clovent.Identity.Application` | `Roles/Commands/CreateRoleCommand.cs` | `CreateRoleCommand.cs` | 1,043 B | 28 | Source Code | Active |
| `Clovent.Identity.Application` | `Roles/Commands/RemovePermissionFromRoleCommand.cs` | `RemovePermissionFromRoleCommand.cs` | 1,024 B | 25 | Source Code | Active |
| `Clovent.Identity.Application` | `Roles/Commands/RenameRoleCommand.cs` | `RenameRoleCommand.cs` | 928 B | 25 | Source Code | Active |
| `Clovent.Identity.Application` | `Roles/Dtos/RoleDto.cs` | `RoleDto.cs` | 595 B | 18 | Source Code | Active |
| `Clovent.Identity.Application` | `Roles/Queries/GetRoleByIdQuery.cs` | `GetRoleByIdQuery.cs` | 824 B | 22 | Source Code | Active |
| `Clovent.Identity.Application` | `Roles/Queries/ListRolesQuery.cs` | `ListRolesQuery.cs` | 761 B | 20 | Source Code | Active |
| `Clovent.Identity.Application` | `Users/Commands/ActivateUserCommand.cs` | `ActivateUserCommand.cs` | 847 B | 24 | Source Code | Active |
| `Clovent.Identity.Application` | `Users/Commands/AssignUserBranchCommand.cs` | `AssignUserBranchCommand.cs` | 1,221 B | 29 | Source Code | Active |
| `Clovent.Identity.Application` | `Users/Commands/AssignUserCompanyCommand.cs` | `AssignUserCompanyCommand.cs` | 1,240 B | 29 | Source Code | Active |
| `Clovent.Identity.Application` | `Users/Commands/AssignUserToRoleCommand.cs` | `AssignUserToRoleCommand.cs` | 1,155 B | 29 | Source Code | Active |
| `Clovent.Identity.Application` | `Users/Commands/CreateUserCommand.cs` | `CreateUserCommand.cs` | 1,444 B | 33 | Source Code | Active |
| `Clovent.Identity.Application` | `Users/Commands/DeactivateUserCommand.cs` | `DeactivateUserCommand.cs` | 861 B | 24 | Source Code | Active |
| `Clovent.Identity.Application` | `Users/Commands/RemoveUserFromRoleCommand.cs` | `RemoveUserFromRoleCommand.cs` | 958 B | 25 | Source Code | Active |
| `Clovent.Identity.Application` | `Users/Commands/UpdateUserCommand.cs` | `UpdateUserCommand.cs` | 1,059 B | 25 | Source Code | Active |
| `Clovent.Identity.Application` | `Users/Dtos/UserDto.cs` | `UserDto.cs` | 844 B | 28 | Source Code | Active |
| `Clovent.Identity.Application` | `Users/Queries/GetUserByIdQuery.cs` | `GetUserByIdQuery.cs` | 824 B | 22 | Source Code | Active |
| `Clovent.Identity.Application` | `Users/Queries/SearchUsersQuery.cs` | `SearchUsersQuery.cs` | 1,471 B | 39 | Source Code | Active |
| `Clovent.Identity.Infrastructure.Tests` | `Caching/MemoryPermissionCacheTests.cs` | `MemoryPermissionCacheTests.cs` | 1,460 B | 54 | Test / QA Code | Active |
| `Clovent.Identity.Infrastructure.Tests` | `Clovent.Identity.Infrastructure.Tests.csproj` | `Clovent.Identity.Infrastructure.Tests.csproj` | 1,040 B | 0 | Configuration / Project Definition | Active |
| `Clovent.Identity.Infrastructure.Tests` | `Persistence/UnitOfWorkBehaviorTests.cs` | `UnitOfWorkBehaviorTests.cs` | 1,778 B | 58 | Test / QA Code | Active |
| `Clovent.Identity.Infrastructure.Tests` | `Repositories/BranchRepositoryTests.cs` | `BranchRepositoryTests.cs` | 3,184 B | 88 | Test / QA Code | Active |
| `Clovent.Identity.Infrastructure.Tests` | `Repositories/CompanyRepositoryTests.cs` | `CompanyRepositoryTests.cs` | 3,184 B | 87 | Test / QA Code | Active |
| `Clovent.Identity.Infrastructure.Tests` | `Repositories/OrganizationRepositoryTests.cs` | `OrganizationRepositoryTests.cs` | 3,862 B | 108 | Test / QA Code | Active |
| `Clovent.Identity.Infrastructure.Tests` | `Repositories/PermissionRepositoryTests.cs` | `PermissionRepositoryTests.cs` | 2,064 B | 58 | Test / QA Code | Active |
| `Clovent.Identity.Infrastructure.Tests` | `Repositories/RoleRepositoryTests.cs` | `RoleRepositoryTests.cs` | 2,494 B | 74 | Test / QA Code | Active |
| `Clovent.Identity.Infrastructure.Tests` | `Repositories/UserRepositoryTests.cs` | `UserRepositoryTests.cs` | 5,690 B | 166 | Test / QA Code | Active |
| `Clovent.Identity.Infrastructure.Tests` | `TestSupport/SqliteTestBase.cs` | `SqliteTestBase.cs` | 1,191 B | 34 | Test / QA Code | Active |
| `Clovent.Identity.Infrastructure` | `Caching/MemoryPermissionCache.cs` | `MemoryPermissionCache.cs` | 1,356 B | 34 | Source Code | Active |
| `Clovent.Identity.Infrastructure` | `Clovent.Identity.Infrastructure.csproj` | `Clovent.Identity.Infrastructure.csproj` | 1,380 B | 0 | Configuration / Project Definition | Active |
| `Clovent.Identity.Infrastructure` | `DependencyInjection/InfrastructureServiceCollectionExtensions.cs` | `InfrastructureServiceCollectionExtensions.cs` | 1,849 B | 35 | Source Code | Active |
| `Clovent.Identity.Infrastructure` | `DependencyInjection/PersistenceServiceCollectionExtensions.cs` | `PersistenceServiceCollectionExtensions.cs` | 3,396 B | 64 | Source Code | Active |
| `Clovent.Identity.Infrastructure` | `Persistence/Configurations/BranchConfiguration.cs` | `BranchConfiguration.cs` | 1,525 B | 46 | Source Code | Active |
| `Clovent.Identity.Infrastructure` | `Persistence/Configurations/CompanyConfiguration.cs` | `CompanyConfiguration.cs` | 1,662 B | 46 | Source Code | Active |
| `Clovent.Identity.Infrastructure` | `Persistence/Configurations/OrganizationConfiguration.cs` | `OrganizationConfiguration.cs` | 1,516 B | 41 | Source Code | Active |
| `Clovent.Identity.Infrastructure` | `Persistence/Configurations/PermissionConfiguration.cs` | `PermissionConfiguration.cs` | 1,139 B | 33 | Source Code | Active |
| `Clovent.Identity.Infrastructure` | `Persistence/Configurations/RoleConfiguration.cs` | `RoleConfiguration.cs` | 1,453 B | 39 | Source Code | Active |
| `Clovent.Identity.Infrastructure` | `Persistence/Configurations/UserConfiguration.cs` | `UserConfiguration.cs` | 2,220 B | 62 | Source Code | Active |
| `Clovent.Identity.Infrastructure` | `Persistence/IdentityDbContext.cs` | `IdentityDbContext.cs` | 1,768 B | 45 | Source Code | Active |
| `Clovent.Identity.Infrastructure` | `Persistence/IdentityDbContextFactory.cs` | `IdentityDbContextFactory.cs` | 820 B | 17 | Source Code | Active |
| `Clovent.Identity.Infrastructure` | `Persistence/IdentityPersistenceInitializer.cs` | `IdentityPersistenceInitializer.cs` | 508 B | 12 | Source Code | Active |
| `Clovent.Identity.Infrastructure` | `Persistence/Migrations/20260727130725_InitialCreate.Designer.cs` | `20260727130725_InitialCreate.Designer.cs` | 4,318 B | 124 | Source Code | Active |
| `Clovent.Identity.Infrastructure` | `Persistence/Migrations/20260727130725_InitialCreate.cs` | `20260727130725_InitialCreate.cs` | 4,368 B | 110 | Source Code | Active |
| `Clovent.Identity.Infrastructure` | `Persistence/Migrations/20260727161915_OrganizationCompanyBranch.Designer.cs` | `20260727161915_OrganizationCompanyBranch.Designer.cs` | 7,888 B | 222 | Source Code | Active |
| `Clovent.Identity.Infrastructure` | `Persistence/Migrations/20260727161915_OrganizationCompanyBranch.cs` | `20260727161915_OrganizationCompanyBranch.cs` | 4,220 B | 95 | Source Code | Active |
| `Clovent.Identity.Infrastructure` | `Persistence/Migrations/20260729051438_AddUserCompanyBranch.Designer.cs` | `20260729051438_AddUserCompanyBranch.Designer.cs` | 8,107 B | 228 | Source Code | Active |
| `Clovent.Identity.Infrastructure` | `Persistence/Migrations/20260729051438_AddUserCompanyBranch.cs` | `20260729051438_AddUserCompanyBranch.cs` | 1,265 B | 43 | Source Code | Active |
| `Clovent.Identity.Infrastructure` | `Persistence/Migrations/IdentityDbContextModelSnapshot.cs` | `IdentityDbContextModelSnapshot.cs` | 7,994 B | 225 | Source Code | Active |
| `Clovent.Identity.Infrastructure` | `Persistence/UnitOfWork.cs` | `UnitOfWork.cs` | 564 B | 11 | Source Code | Active |
| `Clovent.Identity.Infrastructure` | `Persistence/UnitOfWorkBehavior.cs` | `UnitOfWorkBehavior.cs` | 989 B | 24 | Source Code | Active |
| `Clovent.Identity.Infrastructure` | `Persistence/ValueConverters.cs` | `ValueConverters.cs` | 11,768 B | 197 | Source Code | Active |
| `Clovent.Identity.Infrastructure` | `Repositories/BranchRepository.cs` | `BranchRepository.cs` | 1,048 B | 22 | Source Code | Active |
| `Clovent.Identity.Infrastructure` | `Repositories/CompanyRepository.cs` | `CompanyRepository.cs` | 1,090 B | 22 | Source Code | Active |
| `Clovent.Identity.Infrastructure` | `Repositories/OrganizationRepository.cs` | `OrganizationRepository.cs` | 1,022 B | 21 | Source Code | Active |
| `Clovent.Identity.Infrastructure` | `Repositories/PermissionRepository.cs` | `PermissionRepository.cs` | 1,269 B | 26 | Source Code | Active |
| `Clovent.Identity.Infrastructure` | `Repositories/RoleRepository.cs` | `RoleRepository.cs` | 1,167 B | 26 | Source Code | Active |
| `Clovent.Identity.Infrastructure` | `Repositories/UserRepository.cs` | `UserRepository.cs` | 2,878 B | 72 | Source Code | Active |
| `Clovent.Identity.Tests` | `Branches/BranchNameTests.cs` | `BranchNameTests.cs` | 620 B | 27 | Test / QA Code | Active |
| `Clovent.Identity.Tests` | `Branches/BranchTests.cs` | `BranchTests.cs` | 3,018 B | 96 | Test / QA Code | Active |
| `Clovent.Identity.Tests` | `Clovent.Identity.Tests.csproj` | `Clovent.Identity.Tests.csproj` | 661 B | 0 | Configuration / Project Definition | Active |
| `Clovent.Identity.Tests` | `Companies/CompanyNameTests.cs` | `CompanyNameTests.cs` | 632 B | 27 | Test / QA Code | Active |
| `Clovent.Identity.Tests` | `Companies/CompanyTests.cs` | `CompanyTests.cs` | 3,327 B | 103 | Test / QA Code | Active |
| `Clovent.Identity.Tests` | `Organizations/OrganizationNameTests.cs` | `OrganizationNameTests.cs` | 656 B | 27 | Test / QA Code | Active |
| `Clovent.Identity.Tests` | `Organizations/OrganizationTests.cs` | `OrganizationTests.cs` | 4,198 B | 128 | Test / QA Code | Active |
| `Clovent.Identity.Tests` | `Permissions/PermissionCodeTests.cs` | `PermissionCodeTests.cs` | 1,083 B | 42 | Test / QA Code | Active |
| `Clovent.Identity.Tests` | `Permissions/PermissionTests.cs` | `PermissionTests.cs` | 1,087 B | 36 | Test / QA Code | Active |
| `Clovent.Identity.Tests` | `Roles/RoleNameTests.cs` | `RoleNameTests.cs` | 793 B | 35 | Test / QA Code | Active |
| `Clovent.Identity.Tests` | `Roles/RoleTests.cs` | `RoleTests.cs` | 2,437 B | 86 | Test / QA Code | Active |
| `Clovent.Identity.Tests` | `Shared/AddressTests.cs` | `AddressTests.cs` | 1,348 B | 39 | Test / QA Code | Active |
| `Clovent.Identity.Tests` | `Shared/TaxIdTests.cs` | `TaxIdTests.cs` | 712 B | 33 | Test / QA Code | Active |
| `Clovent.Identity.Tests` | `Users/UserIdTests.cs` | `UserIdTests.cs` | 535 B | 27 | Test / QA Code | Active |
| `Clovent.Identity.Tests` | `Users/UserTests.cs` | `UserTests.cs` | 4,746 B | 190 | Test / QA Code | Active |
| `Clovent.Identity.Tests` | `Users/ValueObjects/DisplayNameTests.cs` | `DisplayNameTests.cs` | 1,005 B | 43 | Test / QA Code | Active |
| `Clovent.Identity.Tests` | `Users/ValueObjects/EmailTests.cs` | `EmailTests.cs` | 1,347 B | 54 | Test / QA Code | Active |
| `Clovent.Identity.Tests` | `Users/ValueObjects/PersonNameTests.cs` | `PersonNameTests.cs` | 1,094 B | 43 | Test / QA Code | Active |
| `Clovent.Identity.Tests` | `Users/ValueObjects/PhoneNumberTests.cs` | `PhoneNumberTests.cs` | 753 B | 31 | Test / QA Code | Active |
| `Clovent.Identity.Tests` | `Users/ValueObjects/UserNameTests.cs` | `UserNameTests.cs` | 1,077 B | 46 | Test / QA Code | Active |
| `Clovent.Identity` | `Branches/Branch.cs` | `Branch.cs` | 3,523 B | 94 | Source Code | Active |
| `Clovent.Identity` | `Branches/BranchId.cs` | `BranchId.cs` | 643 B | 16 | Source Code | Active |
| `Clovent.Identity` | `Branches/BranchStatus.cs` | `BranchStatus.cs` | 311 B | 11 | Source Code | Active |
| `Clovent.Identity` | `Branches/Events/BranchActivated.cs` | `BranchActivated.cs` | 246 B | 6 | Source Code | Active |
| `Clovent.Identity` | `Branches/Events/BranchAddressChanged.cs` | `BranchAddressChanged.cs` | 313 B | 7 | Source Code | Active |
| `Clovent.Identity` | `Branches/Events/BranchCreated.cs` | `BranchCreated.cs` | 390 B | 8 | Source Code | Active |
| `Clovent.Identity` | `Branches/Events/BranchDeactivated.cs` | `BranchDeactivated.cs` | 246 B | 6 | Source Code | Active |
| `Clovent.Identity` | `Branches/Events/BranchRenamed.cs` | `BranchRenamed.cs` | 305 B | 7 | Source Code | Active |
| `Clovent.Identity` | `Branches/IBranchRepository.cs` | `IBranchRepository.cs` | 866 B | 20 | Source Code | Active |
| `Clovent.Identity` | `Branches/ValueObjects/BranchName.cs` | `BranchName.cs` | 1,293 B | 39 | Source Code | Active |
| `Clovent.Identity` | `Clovent.Identity.csproj` | `Clovent.Identity.csproj` | 376 B | 0 | Configuration / Project Definition | Active |
| `Clovent.Identity` | `Companies/Company.cs` | `Company.cs` | 4,887 B | 123 | Source Code | Active |
| `Clovent.Identity` | `Companies/CompanyId.cs` | `CompanyId.cs` | 649 B | 16 | Source Code | Active |
| `Clovent.Identity` | `Companies/CompanyStatus.cs` | `CompanyStatus.cs` | 290 B | 11 | Source Code | Active |
| `Clovent.Identity` | `Companies/Events/BranchAddedToCompany.cs` | `BranchAddedToCompany.cs` | 325 B | 7 | Source Code | Active |
| `Clovent.Identity` | `Companies/Events/BranchRemovedFromCompany.cs` | `BranchRemovedFromCompany.cs` | 333 B | 7 | Source Code | Active |
| `Clovent.Identity` | `Companies/Events/CompanyActivated.cs` | `CompanyActivated.cs` | 251 B | 6 | Source Code | Active |
| `Clovent.Identity` | `Companies/Events/CompanyCreated.cs` | `CompanyCreated.cs` | 417 B | 8 | Source Code | Active |
| `Clovent.Identity` | `Companies/Events/CompanyDeactivated.cs` | `CompanyDeactivated.cs` | 251 B | 6 | Source Code | Active |
| `Clovent.Identity` | `Companies/Events/CompanyRenamed.cs` | `CompanyRenamed.cs` | 312 B | 7 | Source Code | Active |
| `Clovent.Identity` | `Companies/ICompanyRepository.cs` | `ICompanyRepository.cs` | 901 B | 20 | Source Code | Active |
| `Clovent.Identity` | `Companies/ValueObjects/CompanyName.cs` | `CompanyName.cs` | 1,286 B | 39 | Source Code | Active |
| `Clovent.Identity` | `IdentityDomainException.cs` | `IdentityDomainException.cs` | 6,403 B | 107 | Source Code | Active |
| `Clovent.Identity` | `Organizations/Events/CompanyAddedToOrganization.cs` | `CompanyAddedToOrganization.cs` | 355 B | 7 | Source Code | Active |
| `Clovent.Identity` | `Organizations/Events/CompanyRemovedFromOrganization.cs` | `CompanyRemovedFromOrganization.cs` | 363 B | 7 | Source Code | Active |
| `Clovent.Identity` | `Organizations/Events/OrganizationActivated.cs` | `OrganizationActivated.cs` | 276 B | 6 | Source Code | Active |
| `Clovent.Identity` | `Organizations/Events/OrganizationCreated.cs` | `OrganizationCreated.cs` | 345 B | 7 | Source Code | Active |
| `Clovent.Identity` | `Organizations/Events/OrganizationDeactivated.cs` | `OrganizationDeactivated.cs` | 276 B | 6 | Source Code | Active |
| `Clovent.Identity` | `Organizations/Events/OrganizationRenamed.cs` | `OrganizationRenamed.cs` | 346 B | 7 | Source Code | Active |
| `Clovent.Identity` | `Organizations/IOrganizationRepository.cs` | `IOrganizationRepository.cs` | 837 B | 18 | Source Code | Active |
| `Clovent.Identity` | `Organizations/Organization.cs` | `Organization.cs` | 5,236 B | 126 | Source Code | Active |
| `Clovent.Identity` | `Organizations/OrganizationId.cs` | `OrganizationId.cs` | 679 B | 16 | Source Code | Active |
| `Clovent.Identity` | `Organizations/OrganizationStatus.cs` | `OrganizationStatus.cs` | 346 B | 11 | Source Code | Active |
| `Clovent.Identity` | `Organizations/ValueObjects/OrganizationName.cs` | `OrganizationName.cs` | 1,332 B | 39 | Source Code | Active |
| `Clovent.Identity` | `Permissions/Events/PermissionCreated.cs` | `PermissionCreated.cs` | 331 B | 7 | Source Code | Active |
| `Clovent.Identity` | `Permissions/IPermissionRepository.cs` | `IPermissionRepository.cs` | 1,067 B | 23 | Source Code | Active |
| `Clovent.Identity` | `Permissions/Permission.cs` | `Permission.cs` | 2,244 B | 56 | Source Code | Active |
| `Clovent.Identity` | `Permissions/PermissionId.cs` | `PermissionId.cs` | 666 B | 16 | Source Code | Active |
| `Clovent.Identity` | `Permissions/ValueObjects/PermissionCode.cs` | `PermissionCode.cs` | 1,709 B | 46 | Source Code | Active |
| `Clovent.Identity` | `Roles/Events/PermissionAssignedToRole.cs` | `PermissionAssignedToRole.cs` | 331 B | 7 | Source Code | Active |
| `Clovent.Identity` | `Roles/Events/PermissionRemovedFromRole.cs` | `PermissionRemovedFromRole.cs` | 336 B | 7 | Source Code | Active |
| `Clovent.Identity` | `Roles/Events/RoleCreated.cs` | `RoleCreated.cs` | 289 B | 7 | Source Code | Active |
| `Clovent.Identity` | `Roles/Events/RoleRenamed.cs` | `RoleRenamed.cs` | 292 B | 7 | Source Code | Active |
| `Clovent.Identity` | `Roles/IRoleRepository.cs` | `IRoleRepository.cs` | 977 B | 23 | Source Code | Active |
| `Clovent.Identity` | `Roles/Role.cs` | `Role.cs` | 3,343 B | 83 | Source Code | Active |
| `Clovent.Identity` | `Roles/RoleId.cs` | `RoleId.cs` | 630 B | 16 | Source Code | Active |
| `Clovent.Identity` | `Roles/ValueObjects/RoleName.cs` | `RoleName.cs` | 1,295 B | 39 | Source Code | Active |
| `Clovent.Identity` | `Shared/ValueObjects/Address.cs` | `Address.cs` | 2,385 B | 71 | Source Code | Active |
| `Clovent.Identity` | `Shared/ValueObjects/TaxId.cs` | `TaxId.cs` | 1,624 B | 45 | Source Code | Active |
| `Clovent.Identity` | `Users/Events/UserActivated.cs` | `UserActivated.cs` | 267 B | 6 | Source Code | Active |
| `Clovent.Identity` | `Users/Events/UserBranchAssigned.cs` | `UserBranchAssigned.cs` | 313 B | 7 | Source Code | Active |
| `Clovent.Identity` | `Users/Events/UserCompanyAssigned.cs` | `UserCompanyAssigned.cs` | 318 B | 7 | Source Code | Active |
| `Clovent.Identity` | `Users/Events/UserCreated.cs` | `UserCreated.cs` | 353 B | 7 | Source Code | Active |
| `Clovent.Identity` | `Users/Events/UserDeactivated.cs` | `UserDeactivated.cs` | 256 B | 6 | Source Code | Active |
| `Clovent.Identity` | `Users/Events/UserDisplayNameChanged.cs` | `UserDisplayNameChanged.cs` | 334 B | 7 | Source Code | Active |
| `Clovent.Identity` | `Users/Events/UserLocked.cs` | `UserLocked.cs` | 229 B | 6 | Source Code | Active |
| `Clovent.Identity` | `Users/Events/UserRoleAssigned.cs` | `UserRoleAssigned.cs` | 302 B | 7 | Source Code | Active |
| `Clovent.Identity` | `Users/Events/UserRoleRemoved.cs` | `UserRoleRemoved.cs` | 302 B | 7 | Source Code | Active |
| `Clovent.Identity` | `Users/Events/UserUnlocked.cs` | `UserUnlocked.cs` | 285 B | 6 | Source Code | Active |
| `Clovent.Identity` | `Users/IUserRepository.cs` | `IUserRepository.cs` | 1,519 B | 37 | Source Code | Active |
| `Clovent.Identity` | `Users/User.cs` | `User.cs` | 7,873 B | 187 | Source Code | Active |
| `Clovent.Identity` | `Users/UserId.cs` | `UserId.cs` | 630 B | 16 | Source Code | Active |
| `Clovent.Identity` | `Users/UserStatus.cs` | `UserStatus.cs` | 523 B | 17 | Source Code | Active |
| `Clovent.Identity` | `Users/ValueObjects/DisplayName.cs` | `DisplayName.cs` | 1,388 B | 42 | Source Code | Active |
| `Clovent.Identity` | `Users/ValueObjects/Email.cs` | `Email.cs` | 1,555 B | 45 | Source Code | Active |
| `Clovent.Identity` | `Users/ValueObjects/PersonName.cs` | `PersonName.cs` | 1,950 B | 54 | Source Code | Active |
| `Clovent.Identity` | `Users/ValueObjects/PhoneNumber.cs` | `PhoneNumber.cs` | 1,425 B | 40 | Source Code | Active |
| `Clovent.Identity` | `Users/ValueObjects/UserName.cs` | `UserName.cs` | 1,754 B | 49 | Source Code | Active |
| `Clovent.Inventory.Application.Tests` | `Adjustments/StockAdjustmentHandlerTests.cs` | `StockAdjustmentHandlerTests.cs` | 5,266 B | 106 | Test / QA Code | Active |
| `Clovent.Inventory.Application.Tests` | `Clovent.Inventory.Application.Tests.csproj` | `Clovent.Inventory.Application.Tests.csproj` | 687 B | 0 | Configuration / Project Definition | Active |
| `Clovent.Inventory.Application.Tests` | `TestSupport/FakeInventoryTransactionRepository.cs` | `FakeInventoryTransactionRepository.cs` | 2,125 B | 35 | Test / QA Code | Active |
| `Clovent.Inventory.Application.Tests` | `TestSupport/FakeInventoryTransactionRepository.cs.backup-h2-20260817-113226` | `FakeInventoryTransactionRepository.cs.backup-h2-20260817-113226` | 1,770 B | 0 | Backup File | Duplicate / Obsolete |
| `Clovent.Inventory.Application.Tests` | `TestSupport/FakeStockAdjustmentRepository.cs` | `FakeStockAdjustmentRepository.cs` | 1,268 B | 26 | Test / QA Code | Active |
| `Clovent.Inventory.Application.Tests` | `TestSupport/FakeStockTransferRepository.cs` | `FakeStockTransferRepository.cs` | 905 B | 22 | Test / QA Code | Active |
| `Clovent.Inventory.Application.Tests` | `TestSupport/FakeWarehouseStockRepository.cs` | `FakeWarehouseStockRepository.cs` | 1,539 B | 30 | Test / QA Code | Active |
| `Clovent.Inventory.Application.Tests` | `Transactions/InventoryTransactionQueryTests.cs` | `InventoryTransactionQueryTests.cs` | 2,953 B | 64 | Test / QA Code | Active |
| `Clovent.Inventory.Application.Tests` | `Transfers/StockTransferHandlerTests.cs` | `StockTransferHandlerTests.cs` | 4,363 B | 97 | Test / QA Code | Active |
| `Clovent.Inventory.Application.Tests` | `WarehouseStocks/WarehouseStockHandlerTests.cs` | `WarehouseStockHandlerTests.cs` | 6,328 B | 134 | Test / QA Code | Active |
| `Clovent.Inventory.Application` | `Adjustments/Commands/ApplyStockAdjustmentCommand.cs` | `ApplyStockAdjustmentCommand.cs` | 2,873 B | 64 | Source Code | Active |
| `Clovent.Inventory.Application` | `Adjustments/Commands/CancelStockAdjustmentCommand.cs` | `CancelStockAdjustmentCommand.cs` | 1,069 B | 23 | Source Code | Active |
| `Clovent.Inventory.Application` | `Adjustments/Commands/CreateStockAdjustmentCommand.cs` | `CreateStockAdjustmentCommand.cs` | 1,285 B | 31 | Source Code | Active |
| `Clovent.Inventory.Application` | `Adjustments/Dtos/StockAdjustmentDto.cs` | `StockAdjustmentDto.cs` | 974 B | 28 | Source Code | Active |
| `Clovent.Inventory.Application` | `Adjustments/Queries/GetStockAdjustmentByIdQuery.cs` | `GetStockAdjustmentByIdQuery.cs` | 1,026 B | 22 | Source Code | Active |
| `Clovent.Inventory.Application` | `Adjustments/Queries/ListStockAdjustmentsByWarehouseQuery.cs` | `ListStockAdjustmentsByWarehouseQuery.cs` | 1,084 B | 21 | Source Code | Active |
| `Clovent.Inventory.Application` | `Adjustments/Queries/ListStockAdjustmentsQuery.cs` | `ListStockAdjustmentsQuery.cs` | 907 B | 20 | Source Code | Active |
| `Clovent.Inventory.Application` | `Clovent.Inventory.Application.csproj` | `Clovent.Inventory.Application.csproj` | 680 B | 0 | Configuration / Project Definition | Active |
| `Clovent.Inventory.Application` | `DependencyInjection/ApplicationServiceCollectionExtensions.cs` | `ApplicationServiceCollectionExtensions.cs` | 1,134 B | 20 | Source Code | Active |
| `Clovent.Inventory.Application` | `IUnitOfWork.cs` | `IUnitOfWork.cs` | 503 B | 8 | Source Code | Active |
| `Clovent.Inventory.Application` | `NotFoundException.cs` | `NotFoundException.cs` | 486 B | 11 | Source Code | Active |
| `Clovent.Inventory.Application` | `Transactions/Dtos/InventoryTransactionDto.cs` | `InventoryTransactionDto.cs` | 1,025 B | 28 | Source Code | Active |
| `Clovent.Inventory.Application` | `Transactions/Queries/GetInventoryTransactionByIdQuery.cs` | `GetInventoryTransactionByIdQuery.cs` | 1,111 B | 22 | Source Code | Active |
| `Clovent.Inventory.Application` | `Transactions/Queries/ListInventoryTransactionsByProductQuery.cs` | `ListInventoryTransactionsByProductQuery.cs` | 1,224 B | 21 | Source Code | Active |
| `Clovent.Inventory.Application` | `Transactions/Queries/ListInventoryTransactionsByReferenceQuery.cs` | `ListInventoryTransactionsByReferenceQuery.cs` | 1,383 B | 26 | Source Code | Active |
| `Clovent.Inventory.Application` | `Transactions/Queries/ListInventoryTransactionsByWarehouseQuery.cs` | `ListInventoryTransactionsByWarehouseQuery.cs` | 1,139 B | 21 | Source Code | Active |
| `Clovent.Inventory.Application` | `Transactions/Queries/ListRecentInventoryTransactionsQuery.cs` | `ListRecentInventoryTransactionsQuery.cs` | 1,163 B | 20 | Source Code | Active |
| `Clovent.Inventory.Application` | `Transfers/Commands/CancelStockTransferCommand.cs` | `CancelStockTransferCommand.cs` | 1,027 B | 23 | Source Code | Active |
| `Clovent.Inventory.Application` | `Transfers/Commands/CompleteStockTransferCommand.cs` | `CompleteStockTransferCommand.cs` | 3,236 B | 58 | Source Code | Active |
| `Clovent.Inventory.Application` | `Transfers/Commands/CreateStockTransferCommand.cs` | `CreateStockTransferCommand.cs` | 1,236 B | 30 | Source Code | Active |
| `Clovent.Inventory.Application` | `Transfers/Dtos/StockTransferDto.cs` | `StockTransferDto.cs` | 919 B | 26 | Source Code | Active |
| `Clovent.Inventory.Application` | `Transfers/Queries/GetStockTransferByIdQuery.cs` | `GetStockTransferByIdQuery.cs` | 984 B | 22 | Source Code | Active |
| `Clovent.Inventory.Application` | `Transfers/Queries/ListStockTransfersQuery.cs` | `ListStockTransfersQuery.cs` | 875 B | 20 | Source Code | Active |
| `Clovent.Inventory.Application` | `WarehouseStocks/Commands/CreateWarehouseStockCommand.cs` | `CreateWarehouseStockCommand.cs` | 1,347 B | 35 | Source Code | Active |
| `Clovent.Inventory.Application` | `WarehouseStocks/Commands/IssueStockCommand.cs` | `IssueStockCommand.cs` | 2,166 B | 51 | Source Code | Active |
| `Clovent.Inventory.Application` | `WarehouseStocks/Commands/IssueStockCommand.cs.backup-h2-20260817-113226` | `IssueStockCommand.cs.backup-h2-20260817-113226` | 1,484 B | 0 | Backup File | Duplicate / Obsolete |
| `Clovent.Inventory.Application` | `WarehouseStocks/Commands/OpenOrReceiveStockCommand.cs` | `OpenOrReceiveStockCommand.cs` | 2,629 B | 55 | Source Code | Active |
| `Clovent.Inventory.Application` | `WarehouseStocks/Commands/ReceiveStockCommand.cs` | `ReceiveStockCommand.cs` | 1,657 B | 34 | Source Code | Active |
| `Clovent.Inventory.Application` | `WarehouseStocks/Commands/ReleaseStockReservationCommand.cs` | `ReleaseStockReservationCommand.cs` | 1,112 B | 23 | Source Code | Active |
| `Clovent.Inventory.Application` | `WarehouseStocks/Commands/ReserveStockCommand.cs` | `ReserveStockCommand.cs` | 1,119 B | 22 | Source Code | Active |
| `Clovent.Inventory.Application` | `WarehouseStocks/Commands/SetNegativeStockPolicyCommand.cs` | `SetNegativeStockPolicyCommand.cs` | 1,134 B | 23 | Source Code | Active |
| `Clovent.Inventory.Application` | `WarehouseStocks/Commands/SetWarehouseStockLevelsCommand.cs` | `SetWarehouseStockLevelsCommand.cs` | 1,160 B | 23 | Source Code | Active |
| `Clovent.Inventory.Application` | `WarehouseStocks/Dtos/WarehouseStockDto.cs` | `WarehouseStockDto.cs` | 1,081 B | 32 | Source Code | Active |
| `Clovent.Inventory.Application` | `WarehouseStocks/Queries/GetWarehouseStockByIdQuery.cs` | `GetWarehouseStockByIdQuery.cs` | 1,020 B | 22 | Source Code | Active |
| `Clovent.Inventory.Application` | `WarehouseStocks/Queries/GetWarehouseStockByWarehouseAndVariantQuery.cs` | `GetWarehouseStockByWarehouseAndVariantQuery.cs` | 1,250 B | 24 | Source Code | Active |
| `Clovent.Inventory.Application` | `WarehouseStocks/Queries/ListWarehouseStocksByWarehouseQuery.cs` | `ListWarehouseStocksByWarehouseQuery.cs` | 1,069 B | 21 | Source Code | Active |
| `Clovent.Inventory.Application` | `WarehouseStocks/Queries/ListWarehouseStocksQuery.cs` | `ListWarehouseStocksQuery.cs` | 919 B | 20 | Source Code | Active |
| `Clovent.Inventory.Infrastructure.Tests` | `Clovent.Inventory.Infrastructure.Tests.csproj` | `Clovent.Inventory.Infrastructure.Tests.csproj` | 1,036 B | 0 | Configuration / Project Definition | Active |
| `Clovent.Inventory.Infrastructure.Tests` | `Persistence/UnitOfWorkBehaviorTests.cs` | `UnitOfWorkBehaviorTests.cs` | 1,781 B | 58 | Test / QA Code | Active |
| `Clovent.Inventory.Infrastructure.Tests` | `Repositories/InventoryTransactionRepositoryTests.cs` | `InventoryTransactionRepositoryTests.cs` | 4,080 B | 90 | Test / QA Code | Active |
| `Clovent.Inventory.Infrastructure.Tests` | `Repositories/StockAdjustmentRepositoryTests.cs` | `StockAdjustmentRepositoryTests.cs` | 3,636 B | 85 | Test / QA Code | Active |
| `Clovent.Inventory.Infrastructure.Tests` | `Repositories/StockTransferRepositoryTests.cs` | `StockTransferRepositoryTests.cs` | 2,428 B | 63 | Test / QA Code | Active |
| `Clovent.Inventory.Infrastructure.Tests` | `Repositories/WarehouseStockRepositoryTests.cs` | `WarehouseStockRepositoryTests.cs` | 3,406 B | 90 | Test / QA Code | Active |
| `Clovent.Inventory.Infrastructure.Tests` | `TestSupport/SqliteTestBase.cs` | `SqliteTestBase.cs` | 1,191 B | 34 | Test / QA Code | Active |
| `Clovent.Inventory.Infrastructure` | `Clovent.Inventory.Infrastructure.csproj` | `Clovent.Inventory.Infrastructure.csproj` | 1,295 B | 0 | Configuration / Project Definition | Active |
| `Clovent.Inventory.Infrastructure` | `DependencyInjection/InfrastructureServiceCollectionExtensions.cs` | `InfrastructureServiceCollectionExtensions.cs` | 1,176 B | 21 | Source Code | Active |
| `Clovent.Inventory.Infrastructure` | `DependencyInjection/PersistenceServiceCollectionExtensions.cs` | `PersistenceServiceCollectionExtensions.cs` | 2,623 B | 50 | Source Code | Active |
| `Clovent.Inventory.Infrastructure` | `Migrations/20260727231859_InitialCreate.Designer.cs` | `20260727231859_InitialCreate.Designer.cs` | 7,277 B | 196 | Source Code | Active |
| `Clovent.Inventory.Infrastructure` | `Migrations/20260727231859_InitialCreate.cs` | `20260727231859_InitialCreate.cs` | 7,558 B | 149 | Source Code | Active |
| `Clovent.Inventory.Infrastructure` | `Migrations/InventoryDbContextModelSnapshot.cs` | `InventoryDbContextModelSnapshot.cs` | 7,179 B | 193 | Source Code | Active |
| `Clovent.Inventory.Infrastructure` | `Persistence/Configurations/InventoryTransactionConfiguration.cs` | `InventoryTransactionConfiguration.cs` | 1,696 B | 46 | Source Code | Active |
| `Clovent.Inventory.Infrastructure` | `Persistence/Configurations/StockAdjustmentConfiguration.cs` | `StockAdjustmentConfiguration.cs` | 1,607 B | 46 | Source Code | Active |
| `Clovent.Inventory.Infrastructure` | `Persistence/Configurations/StockTransferConfiguration.cs` | `StockTransferConfiguration.cs` | 1,489 B | 43 | Source Code | Active |
| `Clovent.Inventory.Infrastructure` | `Persistence/Configurations/WarehouseStockConfiguration.cs` | `WarehouseStockConfiguration.cs` | 1,726 B | 43 | Source Code | Active |
| `Clovent.Inventory.Infrastructure` | `Persistence/InventoryDbContext.cs` | `InventoryDbContext.cs` | 1,386 B | 34 | Source Code | Active |
| `Clovent.Inventory.Infrastructure` | `Persistence/InventoryDbContextFactory.cs` | `InventoryDbContextFactory.cs` | 821 B | 17 | Source Code | Active |
| `Clovent.Inventory.Infrastructure` | `Persistence/InventoryPersistenceInitializer.cs` | `InventoryPersistenceInitializer.cs` | 512 B | 12 | Source Code | Active |
| `Clovent.Inventory.Infrastructure` | `Persistence/UnitOfWork.cs` | `UnitOfWork.cs` | 561 B | 11 | Source Code | Active |
| `Clovent.Inventory.Infrastructure` | `Persistence/UnitOfWorkBehavior.cs` | `UnitOfWorkBehavior.cs` | 765 B | 17 | Source Code | Active |
| `Clovent.Inventory.Infrastructure` | `Persistence/ValueConverters.cs` | `ValueConverters.cs` | 3,007 B | 54 | Source Code | Active |
| `Clovent.Inventory.Infrastructure` | `Repositories/InventoryTransactionRepository.cs` | `InventoryTransactionRepository.cs` | 2,250 B | 37 | Source Code | Active |
| `Clovent.Inventory.Infrastructure` | `Repositories/InventoryTransactionRepository.cs.backup-h2-20260817-113226` | `InventoryTransactionRepository.cs.backup-h2-20260817-113226` | 1,871 B | 0 | Backup File | Duplicate / Obsolete |
| `Clovent.Inventory.Infrastructure` | `Repositories/StockAdjustmentRepository.cs` | `StockAdjustmentRepository.cs` | 1,381 B | 26 | Source Code | Active |
| `Clovent.Inventory.Infrastructure` | `Repositories/StockTransferRepository.cs` | `StockTransferRepository.cs` | 1,024 B | 21 | Source Code | Active |
| `Clovent.Inventory.Infrastructure` | `Repositories/WarehouseStockRepository.cs` | `WarehouseStockRepository.cs` | 1,742 B | 31 | Source Code | Active |
| `Clovent.Inventory.Tests` | `Adjustments/StockAdjustmentTests.cs` | `StockAdjustmentTests.cs` | 2,084 B | 62 | Test / QA Code | Active |
| `Clovent.Inventory.Tests` | `Clovent.Inventory.Tests.csproj` | `Clovent.Inventory.Tests.csproj` | 663 B | 0 | Configuration / Project Definition | Active |
| `Clovent.Inventory.Tests` | `Transactions/InventoryTransactionTests.cs` | `InventoryTransactionTests.cs` | 1,750 B | 46 | Test / QA Code | Active |
| `Clovent.Inventory.Tests` | `Transfers/StockTransferTests.cs` | `StockTransferTests.cs` | 2,261 B | 65 | Test / QA Code | Active |
| `Clovent.Inventory.Tests` | `WarehouseStocks/WarehouseStockTests.cs` | `WarehouseStockTests.cs` | 3,256 B | 115 | Test / QA Code | Active |
| `Clovent.Inventory` | `Adjustments/Events/StockAdjustmentApplied.cs` | `StockAdjustmentApplied.cs` | 297 B | 6 | Source Code | Active |
| `Clovent.Inventory` | `Adjustments/Events/StockAdjustmentCancelled.cs` | `StockAdjustmentCancelled.cs` | 290 B | 6 | Source Code | Active |
| `Clovent.Inventory` | `Adjustments/Events/StockAdjustmentCreated.cs` | `StockAdjustmentCreated.cs` | 491 B | 14 | Source Code | Active |
| `Clovent.Inventory` | `Adjustments/IStockAdjustmentRepository.cs` | `IStockAdjustmentRepository.cs` | 976 B | 19 | Source Code | Active |
| `Clovent.Inventory` | `Adjustments/StockAdjustment.cs` | `StockAdjustment.cs` | 5,418 B | 121 | Source Code | Active |
| `Clovent.Inventory` | `Adjustments/StockAdjustmentId.cs` | `StockAdjustmentId.cs` | 692 B | 16 | Source Code | Active |
| `Clovent.Inventory` | `Adjustments/StockAdjustmentStatus.cs` | `StockAdjustmentStatus.cs` | 417 B | 14 | Source Code | Active |
| `Clovent.Inventory` | `Adjustments/StockAdjustmentType.cs` | `StockAdjustmentType.cs` | 289 B | 11 | Source Code | Active |
| `Clovent.Inventory` | `Clovent.Inventory.csproj` | `Clovent.Inventory.csproj` | 536 B | 0 | Configuration / Project Definition | Active |
| `Clovent.Inventory` | `InventoryDomainException.cs` | `InventoryDomainException.cs` | 2,448 B | 44 | Source Code | Active |
| `Clovent.Inventory` | `Transactions/Events/InventoryTransactionRecorded.cs` | `InventoryTransactionRecorded.cs` | 532 B | 14 | Source Code | Active |
| `Clovent.Inventory` | `Transactions/IInventoryTransactionRepository.cs` | `IInventoryTransactionRepository.cs` | 2,003 B | 32 | Source Code | Active |
| `Clovent.Inventory` | `Transactions/IInventoryTransactionRepository.cs.backup-h2-20260817-113226` | `IInventoryTransactionRepository.cs.backup-h2-20260817-113226` | 1,436 B | 0 | Backup File | Duplicate / Obsolete |
| `Clovent.Inventory` | `Transactions/InventoryTransaction.cs` | `InventoryTransaction.cs` | 3,728 B | 85 | Source Code | Active |
| `Clovent.Inventory` | `Transactions/InventoryTransactionId.cs` | `InventoryTransactionId.cs` | 719 B | 16 | Source Code | Active |
| `Clovent.Inventory` | `Transactions/InventoryTransactionType.cs` | `InventoryTransactionType.cs` | 1,179 B | 30 | Source Code | Active |
| `Clovent.Inventory` | `Transfers/Events/StockTransferCancelled.cs` | `StockTransferCancelled.cs` | 280 B | 6 | Source Code | Active |
| `Clovent.Inventory` | `Transfers/Events/StockTransferCompleted.cs` | `StockTransferCompleted.cs` | 272 B | 6 | Source Code | Active |
| `Clovent.Inventory` | `Transfers/Events/StockTransferCreated.cs` | `StockTransferCreated.cs` | 487 B | 14 | Source Code | Active |
| `Clovent.Inventory` | `Transfers/IStockTransferRepository.cs` | `IStockTransferRepository.cs` | 692 B | 14 | Source Code | Active |
| `Clovent.Inventory` | `Transfers/StockTransfer.cs` | `StockTransfer.cs` | 4,718 B | 100 | Source Code | Active |
| `Clovent.Inventory` | `Transfers/StockTransferId.cs` | `StockTransferId.cs` | 680 B | 16 | Source Code | Active |
| `Clovent.Inventory` | `Transfers/StockTransferStatus.cs` | `StockTransferStatus.cs` | 436 B | 14 | Source Code | Active |
| `Clovent.Inventory` | `WarehouseStocks/Events/NegativeStockPolicyChanged.cs` | `NegativeStockPolicyChanged.cs` | 332 B | 6 | Source Code | Active |
| `Clovent.Inventory` | `WarehouseStocks/Events/StockIssued.cs` | `StockIssued.cs` | 306 B | 6 | Source Code | Active |
| `Clovent.Inventory` | `WarehouseStocks/Events/StockLevelsChanged.cs` | `StockLevelsChanged.cs` | 351 B | 6 | Source Code | Active |
| `Clovent.Inventory` | `WarehouseStocks/Events/StockReceived.cs` | `StockReceived.cs` | 308 B | 6 | Source Code | Active |
| `Clovent.Inventory` | `WarehouseStocks/Events/StockReservationReleased.cs` | `StockReservationReleased.cs` | 330 B | 6 | Source Code | Active |
| `Clovent.Inventory` | `WarehouseStocks/Events/StockReserved.cs` | `StockReserved.cs` | 314 B | 6 | Source Code | Active |
| `Clovent.Inventory` | `WarehouseStocks/Events/WarehouseStockCreated.cs` | `WarehouseStockCreated.cs` | 426 B | 8 | Source Code | Active |
| `Clovent.Inventory` | `WarehouseStocks/IWarehouseStockRepository.cs` | `IWarehouseStockRepository.cs` | 1,328 B | 23 | Source Code | Active |
| `Clovent.Inventory` | `WarehouseStocks/WarehouseStock.cs` | `WarehouseStock.cs` | 8,319 B | 181 | Source Code | Active |
| `Clovent.Inventory` | `WarehouseStocks/WarehouseStockId.cs` | `WarehouseStockId.cs` | 691 B | 16 | Source Code | Active |
| `Clovent.MasterData.Application.Tests` | `Clovent.MasterData.Application.Tests.csproj` | `Clovent.MasterData.Application.Tests.csproj` | 689 B | 0 | Configuration / Project Definition | Active |
| `Clovent.MasterData.Application.Tests` | `Currencies/CurrencyHandlerTests.cs` | `CurrencyHandlerTests.cs` | 2,476 B | 62 | Test / QA Code | Active |
| `Clovent.MasterData.Application.Tests` | `Departments/DepartmentHandlerTests.cs` | `DepartmentHandlerTests.cs` | 3,099 B | 75 | Test / QA Code | Active |
| `Clovent.MasterData.Application.Tests` | `FiscalYears/FiscalYearHandlerTests.cs` | `FiscalYearHandlerTests.cs` | 3,634 B | 84 | Test / QA Code | Active |
| `Clovent.MasterData.Application.Tests` | `Languages/LanguageHandlerTests.cs` | `LanguageHandlerTests.cs` | 2,472 B | 62 | Test / QA Code | Active |
| `Clovent.MasterData.Application.Tests` | `Settings/BusinessSettingsHandlerTests.cs` | `BusinessSettingsHandlerTests.cs` | 3,647 B | 80 | Test / QA Code | Active |
| `Clovent.MasterData.Application.Tests` | `Terminals/TerminalHandlerTests.cs` | `TerminalHandlerTests.cs` | 3,137 B | 75 | Test / QA Code | Active |
| `Clovent.MasterData.Application.Tests` | `TestSupport/FakeBusinessSettingsRepository.cs` | `FakeBusinessSettingsRepository.cs` | 1,005 B | 23 | Test / QA Code | Active |
| `Clovent.MasterData.Application.Tests` | `TestSupport/FakeCurrencyRepository.cs` | `FakeCurrencyRepository.cs` | 1,056 B | 25 | Test / QA Code | Active |
| `Clovent.MasterData.Application.Tests` | `TestSupport/FakeDepartmentRepository.cs` | `FakeDepartmentRepository.cs` | 995 B | 23 | Test / QA Code | Active |
| `Clovent.MasterData.Application.Tests` | `TestSupport/FakeFiscalYearRepository.cs` | `FakeFiscalYearRepository.cs` | 1,030 B | 23 | Test / QA Code | Active |
| `Clovent.MasterData.Application.Tests` | `TestSupport/FakeLanguageRepository.cs` | `FakeLanguageRepository.cs` | 1,049 B | 25 | Test / QA Code | Active |
| `Clovent.MasterData.Application.Tests` | `TestSupport/FakeTerminalRepository.cs` | `FakeTerminalRepository.cs` | 951 B | 23 | Test / QA Code | Active |
| `Clovent.MasterData.Application.Tests` | `TestSupport/FakeTimeZoneRepository.cs` | `FakeTimeZoneRepository.cs` | 1,090 B | 25 | Test / QA Code | Active |
| `Clovent.MasterData.Application.Tests` | `TestSupport/FakeWarehouseRepository.cs` | `FakeWarehouseRepository.cs` | 1,166 B | 26 | Test / QA Code | Active |
| `Clovent.MasterData.Application.Tests` | `TimeZones/TimeZoneEntryHandlerTests.cs` | `TimeZoneEntryHandlerTests.cs` | 2,548 B | 62 | Test / QA Code | Active |
| `Clovent.MasterData.Application.Tests` | `Warehouses/WarehouseHandlerTests.cs` | `WarehouseHandlerTests.cs` | 3,185 B | 75 | Test / QA Code | Active |
| `Clovent.MasterData.Application` | `Clovent.MasterData.Application.csproj` | `Clovent.MasterData.Application.csproj` | 682 B | 0 | Configuration / Project Definition | Active |
| `Clovent.MasterData.Application` | `Currencies/Commands/ActivateCurrencyCommand.cs` | `ActivateCurrencyCommand.cs` | 952 B | 24 | Source Code | Active |
| `Clovent.MasterData.Application` | `Currencies/Commands/CreateCurrencyCommand.cs` | `CreateCurrencyCommand.cs` | 974 B | 23 | Source Code | Active |
| `Clovent.MasterData.Application` | `Currencies/Commands/DeactivateCurrencyCommand.cs` | `DeactivateCurrencyCommand.cs` | 966 B | 24 | Source Code | Active |
| `Clovent.MasterData.Application` | `Currencies/Dtos/CurrencyDto.cs` | `CurrencyDto.cs` | 742 B | 24 | Source Code | Active |
| `Clovent.MasterData.Application` | `Currencies/Queries/GetCurrencyByIdQuery.cs` | `GetCurrencyByIdQuery.cs` | 925 B | 22 | Source Code | Active |
| `Clovent.MasterData.Application` | `Currencies/Queries/ListCurrenciesQuery.cs` | `ListCurrenciesQuery.cs` | 863 B | 20 | Source Code | Active |
| `Clovent.MasterData.Application` | `Departments/Commands/ActivateDepartmentCommand.cs` | `ActivateDepartmentCommand.cs` | 997 B | 24 | Source Code | Active |
| `Clovent.MasterData.Application` | `Departments/Commands/CreateDepartmentCommand.cs` | `CreateDepartmentCommand.cs` | 1,055 B | 25 | Source Code | Active |
| `Clovent.MasterData.Application` | `Departments/Commands/DeactivateDepartmentCommand.cs` | `DeactivateDepartmentCommand.cs` | 1,011 B | 24 | Source Code | Active |
| `Clovent.MasterData.Application` | `Departments/Commands/RenameDepartmentCommand.cs` | `RenameDepartmentCommand.cs` | 1,092 B | 25 | Source Code | Active |
| `Clovent.MasterData.Application` | `Departments/Dtos/DepartmentDto.cs` | `DepartmentDto.cs` | 681 B | 20 | Source Code | Active |
| `Clovent.MasterData.Application` | `Departments/Queries/GetDepartmentByIdQuery.cs` | `GetDepartmentByIdQuery.cs` | 968 B | 22 | Source Code | Active |
| `Clovent.MasterData.Application` | `Departments/Queries/ListDepartmentsByBranchQuery.cs` | `ListDepartmentsByBranchQuery.cs` | 1,031 B | 21 | Source Code | Active |
| `Clovent.MasterData.Application` | `DependencyInjection/ApplicationServiceCollectionExtensions.cs` | `ApplicationServiceCollectionExtensions.cs` | 1,241 B | 24 | Source Code | Active |
| `Clovent.MasterData.Application` | `FiscalYears/Commands/CloseFiscalYearCommand.cs` | `CloseFiscalYearCommand.cs` | 1,007 B | 24 | Source Code | Active |
| `Clovent.MasterData.Application` | `FiscalYears/Commands/CreateFiscalYearCommand.cs` | `CreateFiscalYearCommand.cs` | 1,206 B | 29 | Source Code | Active |
| `Clovent.MasterData.Application` | `FiscalYears/Commands/RenameFiscalYearCommand.cs` | `RenameFiscalYearCommand.cs` | 1,093 B | 25 | Source Code | Active |
| `Clovent.MasterData.Application` | `FiscalYears/Dtos/FiscalYearDto.cs` | `FiscalYearDto.cs` | 797 B | 24 | Source Code | Active |
| `Clovent.MasterData.Application` | `FiscalYears/Queries/GetFiscalYearByIdQuery.cs` | `GetFiscalYearByIdQuery.cs` | 969 B | 22 | Source Code | Active |
| `Clovent.MasterData.Application` | `FiscalYears/Queries/ListFiscalYearsByOrganizationQuery.cs` | `ListFiscalYearsByOrganizationQuery.cs` | 1,097 B | 21 | Source Code | Active |
| `Clovent.MasterData.Application` | `IUnitOfWork.cs` | `IUnitOfWork.cs` | 618 B | 14 | Source Code | Active |
| `Clovent.MasterData.Application` | `Languages/Commands/ActivateLanguageCommand.cs` | `ActivateLanguageCommand.cs` | 949 B | 24 | Source Code | Active |
| `Clovent.MasterData.Application` | `Languages/Commands/CreateLanguageCommand.cs` | `CreateLanguageCommand.cs` | 937 B | 23 | Source Code | Active |
| `Clovent.MasterData.Application` | `Languages/Commands/DeactivateLanguageCommand.cs` | `DeactivateLanguageCommand.cs` | 963 B | 24 | Source Code | Active |
| `Clovent.MasterData.Application` | `Languages/Dtos/LanguageDto.cs` | `LanguageDto.cs` | 693 B | 22 | Source Code | Active |
| `Clovent.MasterData.Application` | `Languages/Queries/GetLanguageByIdQuery.cs` | `GetLanguageByIdQuery.cs` | 922 B | 22 | Source Code | Active |
| `Clovent.MasterData.Application` | `Languages/Queries/ListLanguagesQuery.cs` | `ListLanguagesQuery.cs` | 853 B | 20 | Source Code | Active |
| `Clovent.MasterData.Application` | `NotFoundException.cs` | `NotFoundException.cs` | 502 B | 14 | Source Code | Active |
| `Clovent.MasterData.Application` | `Settings/Commands/CreateBusinessSettingsCommand.cs` | `CreateBusinessSettingsCommand.cs` | 1,498 B | 37 | Source Code | Active |
| `Clovent.MasterData.Application` | `Settings/Commands/UpdateBusinessSettingsCommand.cs` | `UpdateBusinessSettingsCommand.cs` | 1,711 B | 39 | Source Code | Active |
| `Clovent.MasterData.Application` | `Settings/Dtos/BusinessSettingsDto.cs` | `BusinessSettingsDto.cs` | 1,019 B | 28 | Source Code | Active |
| `Clovent.MasterData.Application` | `Settings/Queries/GetBusinessSettingsByOrganizationQuery.cs` | `GetBusinessSettingsByOrganizationQuery.cs` | 1,158 B | 23 | Source Code | Active |
| `Clovent.MasterData.Application` | `Terminals/Commands/ActivateTerminalCommand.cs` | `ActivateTerminalCommand.cs` | 949 B | 24 | Source Code | Active |
| `Clovent.MasterData.Application` | `Terminals/Commands/CreateTerminalCommand.cs` | `CreateTerminalCommand.cs` | 1,103 B | 26 | Source Code | Active |
| `Clovent.MasterData.Application` | `Terminals/Commands/DeactivateTerminalCommand.cs` | `DeactivateTerminalCommand.cs` | 963 B | 24 | Source Code | Active |
| `Clovent.MasterData.Application` | `Terminals/Commands/RenameTerminalCommand.cs` | `RenameTerminalCommand.cs` | 1,040 B | 25 | Source Code | Active |
| `Clovent.MasterData.Application` | `Terminals/Dtos/TerminalDto.cs` | `TerminalDto.cs` | 699 B | 22 | Source Code | Active |
| `Clovent.MasterData.Application` | `Terminals/Queries/GetTerminalByIdQuery.cs` | `GetTerminalByIdQuery.cs` | 922 B | 22 | Source Code | Active |
| `Clovent.MasterData.Application` | `Terminals/Queries/ListTerminalsByBranchQuery.cs` | `ListTerminalsByBranchQuery.cs` | 995 B | 21 | Source Code | Active |
| `Clovent.MasterData.Application` | `TimeZones/Commands/ActivateTimeZoneEntryCommand.cs` | `ActivateTimeZoneEntryCommand.cs` | 1,017 B | 24 | Source Code | Active |
| `Clovent.MasterData.Application` | `TimeZones/Commands/CreateTimeZoneEntryCommand.cs` | `CreateTimeZoneEntryCommand.cs` | 1,000 B | 23 | Source Code | Active |
| `Clovent.MasterData.Application` | `TimeZones/Commands/DeactivateTimeZoneEntryCommand.cs` | `DeactivateTimeZoneEntryCommand.cs` | 1,031 B | 24 | Source Code | Active |
| `Clovent.MasterData.Application` | `TimeZones/Dtos/TimeZoneEntryDto.cs` | `TimeZoneEntryDto.cs` | 729 B | 22 | Source Code | Active |
| `Clovent.MasterData.Application` | `TimeZones/Queries/GetTimeZoneEntryByIdQuery.cs` | `GetTimeZoneEntryByIdQuery.cs` | 993 B | 22 | Source Code | Active |
| `Clovent.MasterData.Application` | `TimeZones/Queries/ListTimeZoneEntriesQuery.cs` | `ListTimeZoneEntriesQuery.cs` | 906 B | 20 | Source Code | Active |
| `Clovent.MasterData.Application` | `Warehouses/Commands/ActivateWarehouseCommand.cs` | `ActivateWarehouseCommand.cs` | 973 B | 24 | Source Code | Active |
| `Clovent.MasterData.Application` | `Warehouses/Commands/CreateWarehouseCommand.cs` | `CreateWarehouseCommand.cs` | 1,125 B | 26 | Source Code | Active |
| `Clovent.MasterData.Application` | `Warehouses/Commands/DeactivateWarehouseCommand.cs` | `DeactivateWarehouseCommand.cs` | 987 B | 24 | Source Code | Active |
| `Clovent.MasterData.Application` | `Warehouses/Commands/RenameWarehouseCommand.cs` | `RenameWarehouseCommand.cs` | 1,066 B | 25 | Source Code | Active |
| `Clovent.MasterData.Application` | `Warehouses/Dtos/WarehouseDto.cs` | `WarehouseDto.cs` | 714 B | 22 | Source Code | Active |
| `Clovent.MasterData.Application` | `Warehouses/Queries/GetWarehouseByIdQuery.cs` | `GetWarehouseByIdQuery.cs` | 945 B | 22 | Source Code | Active |
| `Clovent.MasterData.Application` | `Warehouses/Queries/ListAllWarehousesQuery.cs` | `ListAllWarehousesQuery.cs` | 1,014 B | 20 | Source Code | Active |
| `Clovent.MasterData.Application` | `Warehouses/Queries/ListWarehousesByBranchQuery.cs` | `ListWarehousesByBranchQuery.cs` | 1,013 B | 21 | Source Code | Active |
| `Clovent.MasterData.Infrastructure.Tests` | `Clovent.MasterData.Infrastructure.Tests.csproj` | `Clovent.MasterData.Infrastructure.Tests.csproj` | 1,038 B | 0 | Configuration / Project Definition | Active |
| `Clovent.MasterData.Infrastructure.Tests` | `Persistence/UnitOfWorkBehaviorTests.cs` | `UnitOfWorkBehaviorTests.cs` | 1,784 B | 58 | Test / QA Code | Active |
| `Clovent.MasterData.Infrastructure.Tests` | `Repositories/BusinessSettingsRepositoryTests.cs` | `BusinessSettingsRepositoryTests.cs` | 3,665 B | 96 | Test / QA Code | Active |
| `Clovent.MasterData.Infrastructure.Tests` | `Repositories/CurrencyRepositoryTests.cs` | `CurrencyRepositoryTests.cs` | 2,773 B | 78 | Test / QA Code | Active |
| `Clovent.MasterData.Infrastructure.Tests` | `Repositories/DepartmentRepositoryTests.cs` | `DepartmentRepositoryTests.cs` | 2,557 B | 69 | Test / QA Code | Active |
| `Clovent.MasterData.Infrastructure.Tests` | `Repositories/FiscalYearRepositoryTests.cs` | `FiscalYearRepositoryTests.cs` | 3,565 B | 96 | Test / QA Code | Active |
| `Clovent.MasterData.Infrastructure.Tests` | `Repositories/LanguageRepositoryTests.cs` | `LanguageRepositoryTests.cs` | 2,710 B | 77 | Test / QA Code | Active |
| `Clovent.MasterData.Infrastructure.Tests` | `Repositories/TerminalRepositoryTests.cs` | `TerminalRepositoryTests.cs` | 2,620 B | 70 | Test / QA Code | Active |
| `Clovent.MasterData.Infrastructure.Tests` | `Repositories/TimeZoneRepositoryTests.cs` | `TimeZoneRepositoryTests.cs` | 2,767 B | 77 | Test / QA Code | Active |
| `Clovent.MasterData.Infrastructure.Tests` | `Repositories/WarehouseRepositoryTests.cs` | `WarehouseRepositoryTests.cs` | 2,646 B | 70 | Test / QA Code | Active |
| `Clovent.MasterData.Infrastructure.Tests` | `TestSupport/SqliteTestBase.cs` | `SqliteTestBase.cs` | 1,197 B | 34 | Test / QA Code | Active |
| `Clovent.MasterData.Infrastructure` | `Clovent.MasterData.Infrastructure.csproj` | `Clovent.MasterData.Infrastructure.csproj` | 1,299 B | 0 | Configuration / Project Definition | Active |
| `Clovent.MasterData.Infrastructure` | `DependencyInjection/InfrastructureServiceCollectionExtensions.cs` | `InfrastructureServiceCollectionExtensions.cs` | 1,486 B | 29 | Source Code | Active |
| `Clovent.MasterData.Infrastructure` | `DependencyInjection/PersistenceServiceCollectionExtensions.cs` | `PersistenceServiceCollectionExtensions.cs` | 3,495 B | 67 | Source Code | Active |
| `Clovent.MasterData.Infrastructure` | `Migrations/20260727162427_InitialCreate.Designer.cs` | `20260727162427_InitialCreate.Designer.cs` | 11,005 B | 306 | Source Code | Active |
| `Clovent.MasterData.Infrastructure` | `Migrations/20260727162427_InitialCreate.cs` | `20260727162427_InitialCreate.cs` | 11,402 B | 246 | Source Code | Active |
| `Clovent.MasterData.Infrastructure` | `Migrations/MasterDataDbContextModelSnapshot.cs` | `MasterDataDbContextModelSnapshot.cs` | 10,908 B | 303 | Source Code | Active |
| `Clovent.MasterData.Infrastructure` | `Persistence/Configurations/BusinessSettingsConfiguration.cs` | `BusinessSettingsConfiguration.cs` | 1,709 B | 46 | Source Code | Active |
| `Clovent.MasterData.Infrastructure` | `Persistence/Configurations/CurrencyConfiguration.cs` | `CurrencyConfiguration.cs` | 1,311 B | 38 | Source Code | Active |
| `Clovent.MasterData.Infrastructure` | `Persistence/Configurations/DepartmentConfiguration.cs` | `DepartmentConfiguration.cs` | 1,249 B | 38 | Source Code | Active |
| `Clovent.MasterData.Infrastructure` | `Persistence/Configurations/FiscalYearConfiguration.cs` | `FiscalYearConfiguration.cs` | 1,380 B | 41 | Source Code | Active |
| `Clovent.MasterData.Infrastructure` | `Persistence/Configurations/LanguageConfiguration.cs` | `LanguageConfiguration.cs` | 1,253 B | 37 | Source Code | Active |
| `Clovent.MasterData.Infrastructure` | `Persistence/Configurations/TerminalConfiguration.cs` | `TerminalConfiguration.cs` | 1,393 B | 43 | Source Code | Active |
| `Clovent.MasterData.Infrastructure` | `Persistence/Configurations/TimeZoneEntryConfiguration.cs` | `TimeZoneEntryConfiguration.cs` | 1,279 B | 37 | Source Code | Active |
| `Clovent.MasterData.Infrastructure` | `Persistence/Configurations/WarehouseConfiguration.cs` | `WarehouseConfiguration.cs` | 1,401 B | 43 | Source Code | Active |
| `Clovent.MasterData.Infrastructure` | `Persistence/MasterDataDbContext.cs` | `MasterDataDbContext.cs` | 1,908 B | 50 | Source Code | Active |
| `Clovent.MasterData.Infrastructure` | `Persistence/MasterDataDbContextFactory.cs` | `MasterDataDbContextFactory.cs` | 829 B | 17 | Source Code | Active |
| `Clovent.MasterData.Infrastructure` | `Persistence/MasterDataPersistenceInitializer.cs` | `MasterDataPersistenceInitializer.cs` | 516 B | 12 | Source Code | Active |
| `Clovent.MasterData.Infrastructure` | `Persistence/UnitOfWork.cs` | `UnitOfWork.cs` | 566 B | 11 | Source Code | Active |
| `Clovent.MasterData.Infrastructure` | `Persistence/UnitOfWorkBehavior.cs` | `UnitOfWorkBehavior.cs` | 786 B | 21 | Source Code | Active |
| `Clovent.MasterData.Infrastructure` | `Persistence/ValueConverters.cs` | `ValueConverters.cs` | 5,667 B | 104 | Source Code | Active |
| `Clovent.MasterData.Infrastructure` | `Repositories/BusinessSettingsRepository.cs` | `BusinessSettingsRepository.cs` | 1,153 B | 22 | Source Code | Active |
| `Clovent.MasterData.Infrastructure` | `Repositories/CurrencyRepository.cs` | `CurrencyRepository.cs` | 1,206 B | 25 | Source Code | Active |
| `Clovent.MasterData.Infrastructure` | `Repositories/DepartmentRepository.cs` | `DepartmentRepository.cs` | 1,098 B | 22 | Source Code | Active |
| `Clovent.MasterData.Infrastructure` | `Repositories/FiscalYearRepository.cs` | `FiscalYearRepository.cs` | 1,133 B | 22 | Source Code | Active |
| `Clovent.MasterData.Infrastructure` | `Repositories/LanguageRepository.cs` | `LanguageRepository.cs` | 1,201 B | 25 | Source Code | Active |
| `Clovent.MasterData.Infrastructure` | `Repositories/TerminalRepository.cs` | `TerminalRepository.cs` | 1,072 B | 22 | Source Code | Active |
| `Clovent.MasterData.Infrastructure` | `Repositories/TimeZoneRepository.cs` | `TimeZoneRepository.cs` | 1,262 B | 25 | Source Code | Active |
| `Clovent.MasterData.Infrastructure` | `Repositories/WarehouseRepository.cs` | `WarehouseRepository.cs` | 1,291 B | 26 | Source Code | Active |
| `Clovent.MasterData.Tests` | `Clovent.MasterData.Tests.csproj` | `Clovent.MasterData.Tests.csproj` | 665 B | 0 | Configuration / Project Definition | Active |
| `Clovent.MasterData.Tests` | `Currencies/CurrencyTests.cs` | `CurrencyTests.cs` | 1,928 B | 67 | Test / QA Code | Active |
| `Clovent.MasterData.Tests` | `Departments/DepartmentTests.cs` | `DepartmentTests.cs` | 2,524 B | 78 | Test / QA Code | Active |
| `Clovent.MasterData.Tests` | `FiscalYears/FiscalYearTests.cs` | `FiscalYearTests.cs` | 2,331 B | 65 | Test / QA Code | Active |
| `Clovent.MasterData.Tests` | `Languages/LanguageTests.cs` | `LanguageTests.cs` | 1,158 B | 42 | Test / QA Code | Active |
| `Clovent.MasterData.Tests` | `Settings/BusinessSettingsTests.cs` | `BusinessSettingsTests.cs` | 2,231 B | 55 | Test / QA Code | Active |
| `Clovent.MasterData.Tests` | `Shared/EntityCodeTests.cs` | `EntityCodeTests.cs` | 728 B | 31 | Test / QA Code | Active |
| `Clovent.MasterData.Tests` | `Terminals/TerminalTests.cs` | `TerminalTests.cs` | 1,640 B | 48 | Test / QA Code | Active |
| `Clovent.MasterData.Tests` | `TimeZones/TimeZoneEntryTests.cs` | `TimeZoneEntryTests.cs` | 1,215 B | 38 | Test / QA Code | Active |
| `Clovent.MasterData.Tests` | `Warehouses/WarehouseTests.cs` | `WarehouseTests.cs` | 1,614 B | 47 | Test / QA Code | Active |
| `Clovent.MasterData` | `Clovent.MasterData.csproj` | `Clovent.MasterData.csproj` | 455 B | 0 | Configuration / Project Definition | Active |
| `Clovent.MasterData` | `Currencies/Currency.cs` | `Currency.cs` | 4,144 B | 100 | Source Code | Active |
| `Clovent.MasterData` | `Currencies/CurrencyCode.cs` | `CurrencyCode.cs` | 1,412 B | 40 | Source Code | Active |
| `Clovent.MasterData` | `Currencies/CurrencyId.cs` | `CurrencyId.cs` | 657 B | 16 | Source Code | Active |
| `Clovent.MasterData` | `Currencies/Events/CurrencyCreated.cs` | `CurrencyCreated.cs` | 287 B | 6 | Source Code | Active |
| `Clovent.MasterData` | `Currencies/ICurrencyRepository.cs` | `ICurrencyRepository.cs` | 893 B | 17 | Source Code | Active |
| `Clovent.MasterData` | `Departments/Department.cs` | `Department.cs` | 3,366 B | 83 | Source Code | Active |
| `Clovent.MasterData` | `Departments/DepartmentId.cs` | `DepartmentId.cs` | 668 B | 16 | Source Code | Active |
| `Clovent.MasterData` | `Departments/Events/DepartmentActivated.cs` | `DepartmentActivated.cs` | 267 B | 6 | Source Code | Active |
| `Clovent.MasterData` | `Departments/Events/DepartmentCreated.cs` | `DepartmentCreated.cs` | 387 B | 8 | Source Code | Active |
| `Clovent.MasterData` | `Departments/Events/DepartmentDeactivated.cs` | `DepartmentDeactivated.cs` | 267 B | 6 | Source Code | Active |
| `Clovent.MasterData` | `Departments/Events/DepartmentRenamed.cs` | `DepartmentRenamed.cs` | 335 B | 7 | Source Code | Active |
| `Clovent.MasterData` | `Departments/IDepartmentRepository.cs` | `IDepartmentRepository.cs` | 766 B | 16 | Source Code | Active |
| `Clovent.MasterData` | `Departments/ValueObjects/DepartmentName.cs` | `DepartmentName.cs` | 1,352 B | 39 | Source Code | Active |
| `Clovent.MasterData` | `FiscalYears/Events/FiscalYearClosed.cs` | `FiscalYearClosed.cs` | 257 B | 6 | Source Code | Active |
| `Clovent.MasterData` | `FiscalYears/Events/FiscalYearCreated.cs` | `FiscalYearCreated.cs` | 442 B | 8 | Source Code | Active |
| `Clovent.MasterData` | `FiscalYears/Events/FiscalYearRenamed.cs` | `FiscalYearRenamed.cs` | 335 B | 7 | Source Code | Active |
| `Clovent.MasterData` | `FiscalYears/FiscalYear.cs` | `FiscalYear.cs` | 3,654 B | 88 | Source Code | Active |
| `Clovent.MasterData` | `FiscalYears/FiscalYearId.cs` | `FiscalYearId.cs` | 668 B | 16 | Source Code | Active |
| `Clovent.MasterData` | `FiscalYears/FiscalYearStatus.cs` | `FiscalYearStatus.cs` | 653 B | 17 | Source Code | Active |
| `Clovent.MasterData` | `FiscalYears/IFiscalYearRepository.cs` | `IFiscalYearRepository.cs` | 799 B | 16 | Source Code | Active |
| `Clovent.MasterData` | `FiscalYears/ValueObjects/FiscalYearName.cs` | `FiscalYearName.cs` | 1,335 B | 39 | Source Code | Active |
| `Clovent.MasterData` | `Languages/Events/LanguageCreated.cs` | `LanguageCreated.cs` | 286 B | 6 | Source Code | Active |
| `Clovent.MasterData` | `Languages/ILanguageRepository.cs` | `ILanguageRepository.cs` | 893 B | 17 | Source Code | Active |
| `Clovent.MasterData` | `Languages/Language.cs` | `Language.cs` | 3,278 B | 84 | Source Code | Active |
| `Clovent.MasterData` | `Languages/LanguageCode.cs` | `LanguageCode.cs` | 1,402 B | 40 | Source Code | Active |
| `Clovent.MasterData` | `Languages/LanguageId.cs` | `LanguageId.cs` | 656 B | 16 | Source Code | Active |
| `Clovent.MasterData` | `MasterDataDomainException.cs` | `MasterDataDomainException.cs` | 4,231 B | 79 | Source Code | Active |
| `Clovent.MasterData` | `Settings/BusinessSettings.cs` | `BusinessSettings.cs` | 5,126 B | 122 | Source Code | Active |
| `Clovent.MasterData` | `Settings/BusinessSettingsId.cs` | `BusinessSettingsId.cs` | 695 B | 16 | Source Code | Active |
| `Clovent.MasterData` | `Settings/Events/BusinessSettingsCreated.cs` | `BusinessSettingsCreated.cs` | 380 B | 7 | Source Code | Active |
| `Clovent.MasterData` | `Settings/Events/BusinessSettingsUpdated.cs` | `BusinessSettingsUpdated.cs` | 294 B | 6 | Source Code | Active |
| `Clovent.MasterData` | `Settings/IBusinessSettingsRepository.cs` | `IBusinessSettingsRepository.cs` | 884 B | 16 | Source Code | Active |
| `Clovent.MasterData` | `Shared/MasterDataStatus.cs` | `MasterDataStatus.cs` | 1,176 B | 25 | Source Code | Active |
| `Clovent.MasterData` | `Shared/ValueObjects/EntityCode.cs` | `EntityCode.cs` | 2,231 B | 56 | Source Code | Active |
| `Clovent.MasterData` | `Terminals/Events/TerminalActivated.cs` | `TerminalActivated.cs` | 257 B | 6 | Source Code | Active |
| `Clovent.MasterData` | `Terminals/Events/TerminalCreated.cs` | `TerminalCreated.cs` | 436 B | 9 | Source Code | Active |
| `Clovent.MasterData` | `Terminals/Events/TerminalDeactivated.cs` | `TerminalDeactivated.cs` | 257 B | 6 | Source Code | Active |
| `Clovent.MasterData` | `Terminals/Events/TerminalRenamed.cs` | `TerminalRenamed.cs` | 321 B | 7 | Source Code | Active |
| `Clovent.MasterData` | `Terminals/ITerminalRepository.cs` | `ITerminalRepository.cs` | 744 B | 16 | Source Code | Active |
| `Clovent.MasterData` | `Terminals/Terminal.cs` | `Terminal.cs` | 3,597 B | 89 | Source Code | Active |
| `Clovent.MasterData` | `Terminals/TerminalId.cs` | `TerminalId.cs` | 656 B | 16 | Source Code | Active |
| `Clovent.MasterData` | `Terminals/ValueObjects/TerminalName.cs` | `TerminalName.cs` | 1,324 B | 39 | Source Code | Active |
| `Clovent.MasterData` | `TimeZones/Events/TimeZoneEntryCreated.cs` | `TimeZoneEntryCreated.cs` | 302 B | 6 | Source Code | Active |
| `Clovent.MasterData` | `TimeZones/ITimeZoneRepository.cs` | `ITimeZoneRepository.cs` | 947 B | 17 | Source Code | Active |
| `Clovent.MasterData` | `TimeZones/IanaId.cs` | `IanaId.cs` | 1,527 B | 44 | Source Code | Active |
| `Clovent.MasterData` | `TimeZones/TimeZoneEntry.cs` | `TimeZoneEntry.cs` | 3,995 B | 89 | Source Code | Active |
| `Clovent.MasterData` | `TimeZones/TimeZoneEntryId.cs` | `TimeZoneEntryId.cs` | 681 B | 16 | Source Code | Active |
| `Clovent.MasterData` | `Warehouses/Events/WarehouseActivated.cs` | `WarehouseActivated.cs` | 262 B | 6 | Source Code | Active |
| `Clovent.MasterData` | `Warehouses/Events/WarehouseCreated.cs` | `WarehouseCreated.cs` | 443 B | 9 | Source Code | Active |
| `Clovent.MasterData` | `Warehouses/Events/WarehouseDeactivated.cs` | `WarehouseDeactivated.cs` | 262 B | 6 | Source Code | Active |
| `Clovent.MasterData` | `Warehouses/Events/WarehouseRenamed.cs` | `WarehouseRenamed.cs` | 328 B | 7 | Source Code | Active |
| `Clovent.MasterData` | `Warehouses/IWarehouseRepository.cs` | `IWarehouseRepository.cs` | 1,122 B | 19 | Source Code | Active |
| `Clovent.MasterData` | `Warehouses/ValueObjects/WarehouseName.cs` | `WarehouseName.cs` | 1,294 B | 39 | Source Code | Active |
| `Clovent.MasterData` | `Warehouses/Warehouse.cs` | `Warehouse.cs` | 3,372 B | 82 | Source Code | Active |
| `Clovent.MasterData` | `Warehouses/WarehouseId.cs` | `WarehouseId.cs` | 662 B | 16 | Source Code | Active |
| `Clovent.Platform.Tests` | `Bootstrap/ApplicationBootstrapperTests.cs` | `ApplicationBootstrapperTests.cs` | 3,389 B | 105 | Test / QA Code | Active |
| `Clovent.Platform.Tests` | `Clovent.Platform.Tests.csproj` | `Clovent.Platform.Tests.csproj` | 661 B | 0 | Configuration / Project Definition | Active |
| `Clovent.Platform.Tests` | `Configuration/PlatformConfigurationTests.cs` | `PlatformConfigurationTests.cs` | 2,645 B | 78 | Test / QA Code | Active |
| `Clovent.Platform.Tests` | `Execution/ExecutionContextAccessorTests.cs` | `ExecutionContextAccessorTests.cs` | 4,147 B | 131 | Test / QA Code | Active |
| `Clovent.Platform.Tests` | `Logging/FileLoggerProviderTests.cs` | `FileLoggerProviderTests.cs` | 4,371 B | 111 | Test / QA Code | Active |
| `Clovent.Platform.Tests` | `Modules/ModuleRegistryTests.cs` | `ModuleRegistryTests.cs` | 2,225 B | 65 | Test / QA Code | Active |
| `Clovent.Platform.Tests` | `TestResults/1d026508-0336-4376-a027-7d393974041a/coverage.cobertura.xml` | `coverage.cobertura.xml` | 40,668 B | 746 | Test / QA Code | Active |
| `Clovent.Platform.Tests` | `TestSupport/FakeModules.cs` | `FakeModules.cs` | 2,130 B | 76 | Test / QA Code | Active |
| `Clovent.Platform.Tests` | `TestSupport/TempDirectory.cs` | `TempDirectory.cs` | 811 B | 31 | Test / QA Code | Active |
| `Clovent.Platform` | `Bootstrap/ApplicationBootstrapper.cs` | `ApplicationBootstrapper.cs` | 7,938 B | 183 | Source Code | Active |
| `Clovent.Platform` | `Bootstrap/IPersistenceInitializer.cs` | `IPersistenceInitializer.cs` | 1,099 B | 21 | Source Code | Active |
| `Clovent.Platform` | `Bootstrap/IStartupTask.cs` | `IStartupTask.cs` | 845 B | 18 | Source Code | Active |
| `Clovent.Platform` | `Clovent.Platform.csproj` | `Clovent.Platform.csproj` | 481 B | 0 | Configuration / Project Definition | Active |
| `Clovent.Platform` | `Configuration/OptionsRegistrationExtensions.cs` | `OptionsRegistrationExtensions.cs` | 2,009 B | 42 | Source Code | Active |
| `Clovent.Platform` | `Configuration/PlatformConfiguration.cs` | `PlatformConfiguration.cs` | 3,261 B | 67 | Source Code | Active |
| `Clovent.Platform` | `Configuration/PlatformOptions.cs` | `PlatformOptions.cs` | 1,960 B | 37 | Source Code | Active |
| `Clovent.Platform` | `DependencyInjection/ApplicationServiceCollectionExtensions.cs` | `ApplicationServiceCollectionExtensions.cs` | 1,539 B | 33 | Source Code | Active |
| `Clovent.Platform` | `DependencyInjection/InfrastructureServiceCollectionExtensions.cs` | `InfrastructureServiceCollectionExtensions.cs` | 1,916 B | 39 | Source Code | Active |
| `Clovent.Platform` | `DependencyInjection/PersistenceServiceCollectionExtensions.cs` | `PersistenceServiceCollectionExtensions.cs` | 1,462 B | 30 | Source Code | Active |
| `Clovent.Platform` | `DependencyInjection/PlatformServiceCollectionExtensions.cs` | `PlatformServiceCollectionExtensions.cs` | 1,485 B | 32 | Source Code | Active |
| `Clovent.Platform` | `Execution/ExecutionContextAccessor.cs` | `ExecutionContextAccessor.cs` | 1,712 B | 46 | Source Code | Active |
| `Clovent.Platform` | `Execution/ExecutionContextScope.cs` | `ExecutionContextScope.cs` | 3,013 B | 73 | Source Code | Active |
| `Clovent.Platform` | `Execution/ExecutionContextServiceCollectionExtensions.cs` | `ExecutionContextServiceCollectionExtensions.cs` | 1,043 B | 23 | Source Code | Active |
| `Clovent.Platform` | `Execution/IExecutionContext.cs` | `IExecutionContext.cs` | 2,946 B | 55 | Source Code | Active |
| `Clovent.Platform` | `Execution/IExecutionContextAccessor.cs` | `IExecutionContextAccessor.cs` | 1,357 B | 27 | Source Code | Active |
| `Clovent.Platform` | `Execution/PlatformExecutionContext.cs` | `PlatformExecutionContext.cs` | 1,735 B | 56 | Source Code | Active |
| `Clovent.Platform` | `Logging/FileLogger.cs` | `FileLogger.cs` | 1,026 B | 34 | Source Code | Active |
| `Clovent.Platform` | `Logging/FileLoggerOptions.cs` | `FileLoggerOptions.cs` | 1,323 B | 31 | Source Code | Active |
| `Clovent.Platform` | `Logging/FileLoggerProvider.cs` | `FileLoggerProvider.cs` | 6,855 B | 197 | Source Code | Active |
| `Clovent.Platform` | `Logging/FileLoggingServiceCollectionExtensions.cs` | `FileLoggingServiceCollectionExtensions.cs` | 1,132 B | 28 | Source Code | Active |
| `Clovent.Platform` | `Modules/IModule.cs` | `IModule.cs` | 1,490 B | 31 | Source Code | Active |
| `Clovent.Platform` | `Modules/ModuleRegistry.cs` | `ModuleRegistry.cs` | 1,860 B | 44 | Source Code | Active |
| `Clovent.Platform` | `Modules/ModuleServiceCollectionExtensions.cs` | `ModuleServiceCollectionExtensions.cs` | 1,910 B | 43 | Source Code | Active |

### 3.4 Root Workspace, Documentation, Backups, QA, Tools, and Scratch Files

| Directory / Group | Relative Path | Name | Size | Lines | Purpose / Category | Status |
|---|---|---|---|---|---|---|
| `.claude` | `.claude/scheduled_tasks.lock` | `scheduled_tasks.lock` | 91 B | 0 | Source Code | Active |
| `.claude` | `.claude/settings.local.json` | `settings.local.json` | 140 B | 9 | Configuration / Project Definition | Active |
| `.gitignore` | `.gitignore` | `.gitignore` | 435 B | 0 | Source Code | Active |
| `.obsidian` | `.obsidian/app.json` | `app.json` | 2 B | 1 | Configuration / Project Definition | Active |
| `.obsidian` | `.obsidian/appearance.json` | `appearance.json` | 2 B | 1 | Configuration / Project Definition | Active |
| `.obsidian` | `.obsidian/core-plugins.json` | `core-plugins.json` | 696 B | 33 | Configuration / Project Definition | Active |
| `.obsidian` | `.obsidian/graph.json` | `graph.json` | 493 B | 22 | Configuration / Project Definition | Active |
| `.obsidian` | `.obsidian/workspace.json` | `workspace.json` | 7,169 B | 225 | Configuration / Project Definition | Active |
| `00 Vision` | `00 Vision/00.01 Vision.md` | `00.01 Vision.md` | 0 B | 0 | Documentation | Active |
| `00 Vision` | `00 Vision/00.02 Mission.md` | `00.02 Mission.md` | 0 B | 0 | Documentation | Active |
| `00 Vision` | `00 Vision/00.03 Product Philosophy.md` | `00.03 Product Philosophy.md` | 2,084 B | 109 | Documentation | Active |
| `00 Vision` | `00 Vision/00.04 Business Goals.md` | `00.04 Business Goals.md` | 0 B | 0 | Documentation | Active |
| `00 Vision` | `00 Vision/00.04 Success Criteria.md` | `00.04 Success Criteria.md` | 1,883 B | 72 | Documentation | Active |
| `00 Vision` | `00 Vision/00.05 Success Criteria.md` | `00.05 Success Criteria.md` | 0 B | 0 | Documentation | Active |
| `00 Vision` | `00 Vision/00.06 Non Goals.md` | `00.06 Non Goals.md` | 0 B | 0 | Documentation | Active |
| `01 Product Strategy` | `01 Product Strategy/01.01 Product Strategy.md` | `01.01 Product Strategy.md` | 0 B | 0 | Documentation | Active |
| `01 Product Strategy` | `01 Product Strategy/01.01 Target Market.md` | `01.01 Target Market.md` | 1,029 B | 81 | Documentation | Active |
| `01 Product Strategy` | `01 Product Strategy/01.02 Product Scope.md` | `01.02 Product Scope.md` | 758 B | 58 | Documentation | Active |
| `01 Product Strategy` | `01 Product Strategy/01.03 Product Roadmap.md` | `01.03 Product Roadmap.md` | 1,518 B | 115 | Documentation | Active |
| `01 Product Strategy` | `01 Product Strategy/01.04 Product Modules.md` | `01.04 Product Modules.md` | 849 B | 28 | Documentation | Active |
| `02 Business Analysis` | `02 Business Analysis/02.01 Business Scope.md` | `02.01 Business Scope.md` | 0 B | 0 | Documentation | Active |
| `03 SDLC` | `03 SDLC/03.01 Software Development Life Cycle.md` | `03.01 Software Development Life Cycle.md` | 0 B | 0 | Documentation | Active |
| `04 UI UX Standards` | `04 UI UX Standards/04.01 UI Standards.md` | `04.01 UI Standards.md` | 0 B | 0 | Documentation | Active |
| `04 UI UX Standards` | `04 UI UX Standards/04.02 UX Standards.md` | `04.02 UX Standards.md` | 0 B | 0 | Documentation | Active |
| `04 UI UX Standards` | `04 UI UX Standards/04.03 Design System.md` | `04.03 Design System.md` | 0 B | 0 | Documentation | Active |
| `05 Software Architecture` | `05 Software Architecture/05.01 Architecture Overview.md` | `05.01 Architecture Overview.md` | 1,838 B | 40 | Documentation | Active |
| `06 Coding Standards` | `06 Coding Standards/06.01 Coding Standards.md` | `06.01 Coding Standards.md` | 869 B | 16 | Documentation | Active |
| `07 Domain Driven Design` | `07 Domain Driven Design/07.01 Domain Model.md` | `07.01 Domain Model.md` | 550 B | 14 | Documentation | Active |
| `08 Database Design` | `08 Database Design/08.01 Database Standards.md` | `08.01 Database Standards.md` | 0 B | 0 | Documentation | Active |
| `09 Security` | `09 Security/09.01 Security Overview.md` | `09.01 Security Overview.md` | 0 B | 0 | Documentation | Active |
| `09 Security` | `09 Security/09.02 Identity Package Inventory.md` | `09.02 Identity Package Inventory.md` | 10,880 B | 117 | Documentation | Active |
| `10 AI Architecture` | `10 AI Architecture/10.01 AI Architecture.md` | `10.01 AI Architecture.md` | 0 B | 0 | Documentation | Active |
| `11 Platform Services` | `11 Platform Services/11.01 Platform Services.md` | `11.01 Platform Services.md` | 0 B | 0 | Documentation | Active |
| `12 Restaurant POS` | `12 Restaurant POS/12.01 Restaurant POS Overview.md` | `12.01 Restaurant POS Overview.md` | 40,679 B | 458 | Documentation | Active |
| `13 ADR` | `13 ADR/ADR-001.md` | `ADR-001.md` | 738 B | 20 | Documentation | Active |
| `Clovent Business Operating System.md` | `Clovent Business Operating System.md` | `Clovent Business Operating System.md` | 204 B | 5 | Documentation / Report / Diagnostic | Active |
| `Clovent.BusinessOperatingSystem.slnx` | `Clovent.BusinessOperatingSystem.slnx` | `Clovent.BusinessOperatingSystem.slnx` | 3,969 B | 0 | Configuration / Project Definition | Active |
| `Foundation` | `Foundation/01-Bootstrap-Backup-Part1.ps1` | `01-Bootstrap-Backup-Part1.ps1` | 1,987 B | 94 | Automation / QA Script | Active |
| `Foundation` | `Foundation/01-Bootstrap.ps1` | `01-Bootstrap.ps1` | 347 B | 17 | Automation / QA Script | Active |
| `Foundation` | `Foundation/Bootstrap/Build.ps1` | `Build.ps1` | 727 B | 44 | Automation / QA Script | Active |
| `Foundation` | `Foundation/Bootstrap/Common.ps1` | `Common.ps1` | 1,656 B | 89 | Automation / QA Script | Active |
| `Foundation` | `Foundation/Bootstrap/Environment.ps1` | `Environment.ps1` | 425 B | 28 | Automation / QA Script | Active |
| `Foundation` | `Foundation/Bootstrap/Projects.ps1` | `Projects.ps1` | 1,222 B | 60 | Automation / QA Script | Active |
| `Foundation` | `Foundation/Bootstrap/Reports.ps1` | `Reports.ps1` | 304 B | 19 | Automation / QA Script | Active |
| `Foundation` | `Foundation/Bootstrap/Repository.ps1` | `Repository.ps1` | 140 B | 7 | Automation / QA Script | Active |
| `Foundation` | `Foundation/Bootstrap/Solution.ps1` | `Solution.ps1` | 455 B | 32 | Automation / QA Script | Active |
| `Foundation` | `Foundation/Bootstrap/Tools.ps1` | `Tools.ps1` | 83 B | 5 | Automation / QA Script | Active |
| `Foundation` | `Foundation/Logs/Bootstrap.log` | `Bootstrap.log` | 4,904 B | 0 | Source Code | Active |
| `Images` | `Images/200x200/ALOO-CHICKEN-QORMA.jpg.jpeg` | `ALOO-CHICKEN-QORMA.jpg.jpeg` | 118,605 B | 0 | Image / Asset | Active |
| `Images` | `Images/200x200/ALOO-PALAK.jpg.jpeg` | `ALOO-PALAK.jpg.jpeg` | 102,999 B | 0 | Image / Asset | Active |
| `Images` | `Images/200x200/CHICKEN-BIRYANIs.jpg.jpeg` | `CHICKEN-BIRYANIs.jpg.jpeg` | 115,837 B | 0 | Image / Asset | Active |
| `Images` | `Images/200x200/Chicken-Haleem.jpg.jpeg` | `Chicken-Haleem.jpg.jpeg` | 94,652 B | 0 | Image / Asset | Active |
| `Images` | `Images/200x200/Desi-Chicken-Karahi.jpg.jpeg` | `Desi-Chicken-Karahi.jpg.jpeg` | 129,197 B | 0 | Image / Asset | Active |
| `Images` | `Images/200x200/SALAD-RAITA.jpg.jpg.jpeg` | `SALAD-RAITA.jpg.jpg.jpeg` | 86,691 B | 0 | Image / Asset | Active |
| `Images` | `Images/200x200/WHITE-DAAL-MASH.jpg.jpeg` | `WHITE-DAAL-MASH.jpg.jpeg` | 91,588 B | 0 | Image / Asset | Active |
| `Images` | `Images/600x600/ALOO-CHICKEN-QORMA.jpg.jpeg` | `ALOO-CHICKEN-QORMA.jpg.jpeg` | 472,700 B | 0 | Image / Asset | Active |
| `Images` | `Images/600x600/ALOO-PALAK.jpg.jpeg` | `ALOO-PALAK.jpg.jpeg` | 368,729 B | 0 | Image / Asset | Active |
| `Images` | `Images/600x600/CHICKEN-BIRYANI.jpg.jpeg` | `CHICKEN-BIRYANI.jpg.jpeg` | 453,441 B | 0 | Image / Asset | Active |
| `Images` | `Images/600x600/Chicken-Haleem.jpg.jpeg` | `Chicken-Haleem.jpg.jpeg` | 362,317 B | 0 | Image / Asset | Active |
| `Images` | `Images/600x600/Desi-Chicken-Karahi.jpg.jpeg` | `Desi-Chicken-Karahi.jpg.jpeg` | 586,376 B | 0 | Image / Asset | Active |
| `Images` | `Images/600x600/MURGH-CHANAY.jpg.jpeg` | `MURGH-CHANAY.jpg.jpeg` | 358,977 B | 0 | Image / Asset | Active |
| `Images` | `Images/600x600/WHITE-DAAL-MASH.jpg.jpeg` | `WHITE-DAAL-MASH.jpg.jpeg` | 349,824 B | 0 | Image / Asset | Active |
| `Images` | `Images/600x600/salad.jpg.jpeg` | `salad.jpg.jpeg` | 271,398 B | 0 | Image / Asset | Active |
| `Images` | `Images/pos_1366.png` | `pos_1366.png` | 590,591 B | 0 | Image / Asset | Active |
| `Images` | `Images/pos_center.png` | `pos_center.png` | 635,678 B | 0 | Image / Asset | Active |
| `Images` | `Images/pos_final_1920.png` | `pos_final_1920.png` | 911,870 B | 0 | Image / Asset | Active |
| `Images` | `Images/pos_full.png` | `pos_full.png` | 351,976 B | 0 | Image / Asset | Active |
| `Images` | `Images/pos_full2.png` | `pos_full2.png` | 1,154,696 B | 0 | Image / Asset | Active |
| `Images` | `Images/pos_grid.png` | `pos_grid.png` | 208,689 B | 0 | Image / Asset | Active |
| `Images` | `Images/pos_left.png` | `pos_left.png` | 95,954 B | 0 | Image / Asset | Active |
| `Images` | `Images/pos_right.png` | `pos_right.png` | 115,927 B | 0 | Image / Asset | Active |
| `Images` | `Images/pos_test.png` | `pos_test.png` | 35,902 B | 0 | Image / Asset | Active |
| `Images` | `Images/pos_test_login.png` | `pos_test_login.png` | 35,902 B | 0 | Image / Asset | Active |
| `Images` | `Images/pos_test_run.png` | `pos_test_run.png` | 35,902 B | 0 | Image / Asset | Active |
| `Restaurant_POS_Live_UI_Acceptance_Report.md` | `Restaurant_POS_Live_UI_Acceptance_Report.md` | `Restaurant_POS_Live_UI_Acceptance_Report.md` | 6,622 B | 73 | Documentation / Report / Diagnostic | Active |
| `Restaurant_POS_Menu_Category_Assignment_Report.md` | `Restaurant_POS_Menu_Category_Assignment_Report.md` | `Restaurant_POS_Menu_Category_Assignment_Report.md` | 4,906 B | 81 | Documentation / Report / Diagnostic | Active |
| `Restaurant_POS_Menu_Category_Count_Discrepancy_Report.md` | `Restaurant_POS_Menu_Category_Count_Discrepancy_Report.md` | `Restaurant_POS_Menu_Category_Count_Discrepancy_Report.md` | 5,051 B | 86 | Documentation / Report / Diagnostic | Active |
| `Restaurant_POS_Menu_Category_Data_Synchronization_Report.md` | `Restaurant_POS_Menu_Category_Data_Synchronization_Report.md` | `Restaurant_POS_Menu_Category_Data_Synchronization_Report.md` | 7,730 B | 159 | Documentation / Report / Diagnostic | Active |
| `Restaurant_POS_Menu_Category_Seed_Regression_Report.md` | `Restaurant_POS_Menu_Category_Seed_Regression_Report.md` | `Restaurant_POS_Menu_Category_Seed_Regression_Report.md` | 6,811 B | 104 | Documentation / Report / Diagnostic | Active |
| `Restaurant_POS_Record_Payment_Bug_Report.md` | `Restaurant_POS_Record_Payment_Bug_Report.md` | `Restaurant_POS_Record_Payment_Bug_Report.md` | 7,617 B | 138 | Documentation / Report / Diagnostic | Active |
| `Restaurant_POS_Smart_Intelligence_Implementation_Report.md` | `Restaurant_POS_Smart_Intelligence_Implementation_Report.md` | `Restaurant_POS_Smart_Intelligence_Implementation_Report.md` | 19,344 B | 286 | Documentation / Report / Diagnostic | Active |
| `Restaurant_POS_Table_Occupancy_Bug_Report.md` | `Restaurant_POS_Table_Occupancy_Bug_Report.md` | `Restaurant_POS_Table_Occupancy_Bug_Report.md` | 3,168 B | 41 | Documentation / Report / Diagnostic | Active |
| `Templates` | `Templates/ADR.template.md` | `ADR.template.md` | 0 B | 0 | Documentation / Report / Diagnostic | Active |
| `Templates` | `Templates/APIContract.template.md` | `APIContract.template.md` | 0 B | 0 | Documentation / Report / Diagnostic | Active |
| `Templates` | `Templates/BusinessRequirement.template.md` | `BusinessRequirement.template.md` | 0 B | 0 | Documentation / Report / Diagnostic | Active |
| `Templates` | `Templates/DatabaseTable.template.md` | `DatabaseTable.template.md` | 0 B | 0 | Documentation / Report / Diagnostic | Active |
| `Templates` | `Templates/Document.template.md` | `Document.template.md` | 0 B | 0 | Documentation / Report / Diagnostic | Active |
| `Templates` | `Templates/Prompt.template.md` | `Prompt.template.md` | 0 B | 0 | Documentation / Report / Diagnostic | Active |
| `Templates` | `Templates/ScreenSpecification.template.md` | `ScreenSpecification.template.md` | 0 B | 0 | Documentation / Report / Diagnostic | Active |
| `Templates` | `Templates/TestCase.template.md` | `TestCase.template.md` | 0 B | 0 | Documentation / Report / Diagnostic | Active |
| `Templates` | `Templates/UseCase.template.md` | `UseCase.template.md` | 0 B | 0 | Documentation / Report / Diagnostic | Active |
| `Templates` | `Templates/UserStory.template.md` | `UserStory.template.md` | 0 B | 0 | Documentation / Report / Diagnostic | Active |
| `Templates` | `Templates/Workflow.template.md` | `Workflow.template.md` | 0 B | 0 | Documentation / Report / Diagnostic | Active |
| `Tools` | `Tools/Clovent.CLI/Authentication.cbospkg` | `Authentication.cbospkg` | 382 B | 0 | Source Code | Active |
| `Tools` | `Tools/Clovent.CLI/Clovent.CBOS.Desktop/Clovent.CBOS.Desktop.csproj` | `Clovent.CBOS.Desktop.csproj` | 1,137 B | 0 | Configuration / Project Definition | Active |
| `Tools` | `Tools/Clovent.CLI/Clovent.CBOS.Desktop/Clovent.CBOS.Desktop.csproj.user` | `Clovent.CBOS.Desktop.csproj.user` | 278 B | 8 | Configuration / Project Definition | Active |
| `Tools` | `Tools/Clovent.CLI/Clovent.CBOS.Desktop/Configuration/AppConfiguration.cs` | `AppConfiguration.cs` | 0 B | 0 | Source Code | Unused Empty Stub |
| `Tools` | `Tools/Clovent.CLI/Clovent.CBOS.Desktop/Configuration/AppSettings.cs` | `AppSettings.cs` | 0 B | 0 | Source Code | Unused Empty Stub |
| `Tools` | `Tools/Clovent.CLI/Clovent.CBOS.Desktop/Configuration/DatabaseOptions.cs` | `DatabaseOptions.cs` | 0 B | 0 | Source Code | Unused Empty Stub |
| `Tools` | `Tools/Clovent.CLI/Clovent.CBOS.Desktop/Form1.Designer.cs` | `Form1.Designer.cs` | 1,065 B | 38 | Source Code | Active |
| `Tools` | `Tools/Clovent.CLI/Clovent.CBOS.Desktop/Form1.cs` | `Form1.cs` | 142 B | 9 | Source Code | Active |
| `Tools` | `Tools/Clovent.CLI/Clovent.CBOS.Desktop/Form1.resx` | `Form1.resx` | 5,817 B | 120 | UI Resource | Active |
| `Tools` | `Tools/Clovent.CLI/Clovent.CBOS.Desktop/Forms/Dashboard/MainForm.Designer.cs` | `MainForm.Designer.cs` | 0 B | 0 | Source Code | Unused Empty Stub |
| `Tools` | `Tools/Clovent.CLI/Clovent.CBOS.Desktop/Forms/Dashboard/MainForm.cs` | `MainForm.cs` | 0 B | 0 | Source Code | Unused Empty Stub |
| `Tools` | `Tools/Clovent.CLI/Clovent.CBOS.Desktop/Forms/Login/LoginForm.Designer.cs` | `LoginForm.Designer.cs` | 0 B | 0 | Source Code | Unused Empty Stub |
| `Tools` | `Tools/Clovent.CLI/Clovent.CBOS.Desktop/Forms/Login/LoginForm.cs` | `LoginForm.cs` | 0 B | 0 | Source Code | Unused Empty Stub |
| `Tools` | `Tools/Clovent.CLI/Clovent.CBOS.Desktop/Infrastructure/ApplicationBootstrapper.cs` | `ApplicationBootstrapper.cs` | 0 B | 0 | Source Code | Unused Empty Stub |
| `Tools` | `Tools/Clovent.CLI/Clovent.CBOS.Desktop/Infrastructure/ApplicationHost.cs` | `ApplicationHost.cs` | 0 B | 0 | Source Code | Unused Empty Stub |
| `Tools` | `Tools/Clovent.CLI/Clovent.CBOS.Desktop/Infrastructure/ApplicationServices.cs` | `ApplicationServices.cs` | 0 B | 0 | Source Code | Unused Empty Stub |
| `Tools` | `Tools/Clovent.CLI/Clovent.CBOS.Desktop/Infrastructure/DatabaseInitializer.cs` | `DatabaseInitializer.cs` | 0 B | 0 | Source Code | Unused Empty Stub |
| `Tools` | `Tools/Clovent.CLI/Clovent.CBOS.Desktop/Infrastructure/DependencyInjection.cs` | `DependencyInjection.cs` | 0 B | 0 | Source Code | Unused Empty Stub |
| `Tools` | `Tools/Clovent.CLI/Clovent.CBOS.Desktop/Infrastructure/ServiceCollectionExtensions.cs` | `ServiceCollectionExtensions.cs` | 0 B | 0 | Source Code | Unused Empty Stub |
| `Tools` | `Tools/Clovent.CLI/Clovent.CBOS.Desktop/Infrastructure/ServiceRegistration.cs` | `ServiceRegistration.cs` | 0 B | 0 | Source Code | Unused Empty Stub |
| `Tools` | `Tools/Clovent.CLI/Clovent.CBOS.Desktop/Program.cs` | `Program.cs` | 542 B | 24 | Source Code | Active |
| `Tools` | `Tools/Clovent.CLI/Clovent.CBOS.Desktop/Services/AuthenticationService.cs` | `AuthenticationService.cs` | 0 B | 0 | Source Code | Unused Empty Stub |
| `Tools` | `Tools/Clovent.CLI/Clovent.CBOS.Desktop/Services/DialogService.cs` | `DialogService.cs` | 0 B | 0 | Source Code | Unused Empty Stub |
| `Tools` | `Tools/Clovent.CLI/Clovent.CBOS.Desktop/Services/MessageService.cs` | `MessageService.cs` | 0 B | 0 | Source Code | Unused Empty Stub |
| `Tools` | `Tools/Clovent.CLI/Clovent.CBOS.Desktop/Services/NavigationService.cs` | `NavigationService.cs` | 0 B | 0 | Source Code | Unused Empty Stub |
| `Tools` | `Tools/Clovent.CLI/Clovent.CBOS.Desktop/Services/SessionService.cs` | `SessionService.cs` | 0 B | 0 | Source Code | Unused Empty Stub |
| `Tools` | `Tools/Clovent.CLI/Clovent.CBOS.Desktop/Session/UserSession.cs` | `UserSession.cs` | 0 B | 0 | Source Code | Unused Empty Stub |
| `Tools` | `Tools/Clovent.CLI/Clovent.CLI.slnx` | `Clovent.CLI.slnx` | 1,133 B | 0 | Configuration / Project Definition | Active |
| `Tools` | `Tools/Clovent.CLI/Clovent.CLI/Clovent.CLI.csproj` | `Clovent.CLI.csproj` | 1,418 B | 0 | Configuration / Project Definition | Active |
| `Tools` | `Tools/Clovent.CLI/Clovent.CLI/Commands/Bootstrap/BootstrapCommand.cs` | `BootstrapCommand.cs` | 1,042 B | 43 | Source Code | Active |
| `Tools` | `Tools/Clovent.CLI/Clovent.CLI/Commands/Build/BuildCommand.cs` | `BuildCommand.cs` | 353 B | 15 | Source Code | Active |
| `Tools` | `Tools/Clovent.CLI/Clovent.CLI/Commands/Docs/DocsCommand.cs` | `DocsCommand.cs` | 350 B | 15 | Source Code | Active |
| `Tools` | `Tools/Clovent.CLI/Clovent.CLI/Commands/Entity/NewEntityCommand.cs` | `NewEntityCommand.cs` | 2,601 B | 92 | Source Code | Active |
| `Tools` | `Tools/Clovent.CLI/Clovent.CLI/Commands/Init/InitCommand.cs` | `InitCommand.cs` | 350 B | 15 | Source Code | Active |
| `Tools` | `Tools/Clovent.CLI/Clovent.CLI/Commands/Migrate/MigrateCommand.cs` | `MigrateCommand.cs` | 359 B | 15 | Source Code | Active |
| `Tools` | `Tools/Clovent.CLI/Clovent.CLI/Commands/Module/ModuleCommand.cs` | `ModuleCommand.cs` | 356 B | 15 | Source Code | Active |
| `Tools` | `Tools/Clovent.CLI/Clovent.CLI/Commands/New/NewModuleCommand.cs` | `NewModuleCommand.cs` | 1,259 B | 51 | Source Code | Active |
| `Tools` | `Tools/Clovent.CLI/Clovent.CLI/Commands/Shared/CommandBase.cs` | `CommandBase.cs` | 555 B | 19 | Source Code | Active |
| `Tools` | `Tools/Clovent.CLI/Clovent.CLI/Commands/Shared/CommandSettingsBase.cs` | `CommandSettingsBase.cs` | 136 B | 7 | Source Code | Active |
| `Tools` | `Tools/Clovent.CLI/Clovent.CLI/Commands/Shared/DoctorCommand.cs` | `DoctorCommand.cs` | 985 B | 38 | Source Code | Active |
| `Tools` | `Tools/Clovent.CLI/Clovent.CLI/Commands/Shared/ValidationHelper.cs` | `ValidationHelper.cs` | 344 B | 13 | Source Code | Active |
| `Tools` | `Tools/Clovent.CLI/Clovent.CLI/Commands/Validate/ValidateCommand.cs` | `ValidateCommand.cs` | 362 B | 15 | Source Code | Active |
| `Tools` | `Tools/Clovent.CLI/Clovent.CLI/Commands/Version/VersionCommand.cs` | `VersionCommand.cs` | 359 B | 15 | Source Code | Active |
| `Tools` | `Tools/Clovent.CLI/Clovent.CLI/Configuration/CliConfiguration.cs` | `CliConfiguration.cs` | 3 B | 1 | Source Code | Unused Empty Stub |
| `Tools` | `Tools/Clovent.CLI/Clovent.CLI/DependencyInjection/ServiceRegistration.cs` | `ServiceRegistration.cs` | 1,026 B | 32 | Source Code | Active |
| `Tools` | `Tools/Clovent.CLI/Clovent.CLI/Extensions/CommandRegistration.cs` | `CommandRegistration.cs` | 438 B | 18 | Source Code | Active |
| `Tools` | `Tools/Clovent.CLI/Clovent.CLI/Extensions/ServiceCollectionExtensions.cs` | `ServiceCollectionExtensions.cs` | 3 B | 1 | Source Code | Unused Empty Stub |
| `Tools` | `Tools/Clovent.CLI/Clovent.CLI/Infrastructure/CliHost.cs` | `CliHost.cs` | 3 B | 1 | Source Code | Unused Empty Stub |
| `Tools` | `Tools/Clovent.CLI/Clovent.CLI/Models/Entity/EntitySettings.cs` | `EntitySettings.cs` | 804 B | 23 | Source Code | Active |
| `Tools` | `Tools/Clovent.CLI/Clovent.CLI/Models/Module/ModuleSettings.cs` | `ModuleSettings.cs` | 437 B | 15 | Source Code | Active |
| `Tools` | `Tools/Clovent.CLI/Clovent.CLI/Program.cs` | `Program.cs` | 1,113 B | 36 | Source Code | Active |
| `Tools` | `Tools/Clovent.CLI/Clovent.CLI/Services/ConsoleService.cs` | `ConsoleService.cs` | 3 B | 1 | Source Code | Unused Empty Stub |
| `Tools` | `Tools/Clovent.CLI/Clovent.CLI/Services/DocumentGenerator.cs` | `DocumentGenerator.cs` | 3 B | 1 | Source Code | Unused Empty Stub |
| `Tools` | `Tools/Clovent.CLI/Clovent.CLI/Services/FileService.cs` | `FileService.cs` | 3 B | 1 | Source Code | Unused Empty Stub |
| `Tools` | `Tools/Clovent.CLI/Clovent.CLI/Services/TemplateService.cs` | `TemplateService.cs` | 3 B | 1 | Source Code | Unused Empty Stub |
| `Tools` | `Tools/Clovent.CLI/Clovent.CLI/Templates/Module/ADR.md.template` | `ADR.md.template` | 0 B | 0 | Source Code | Unused Empty Stub |
| `Tools` | `Tools/Clovent.CLI/Clovent.CLI/Templates/Module/Module.csproj.template` | `Module.csproj.template` | 0 B | 0 | Source Code | Unused Empty Stub |
| `Tools` | `Tools/Clovent.CLI/Clovent.CLI/Templates/Module/ModuleRegistration.template` | `ModuleRegistration.template` | 0 B | 0 | Source Code | Unused Empty Stub |
| `Tools` | `Tools/Clovent.CLI/Clovent.CLI/Templates/Module/README.md` | `README.md` | 0 B | 0 | Documentation / Report / Diagnostic | Unused Empty Stub |
| `Tools` | `Tools/Clovent.CLI/Clovent.CLI/TypeRegistrar.cs` | `TypeRegistrar.cs` | 825 B | 34 | Source Code | Active |
| `Tools` | `Tools/Clovent.CLI/Clovent.CLI/TypeResolver.cs` | `TypeResolver.cs` | 526 B | 27 | Source Code | Active |
| `Tools` | `Tools/Clovent.CLI/Clovent.PackageManager.Abstractions/Class1.cs` | `Class1.cs` | 82 B | 6 | Source Code | Active |
| `Tools` | `Tools/Clovent.CLI/Clovent.PackageManager.Abstractions/Clovent.PackageManager.Abstractions.csproj` | `Clovent.PackageManager.Abstractions.csproj` | 219 B | 0 | Configuration / Project Definition | Active |
| `Tools` | `Tools/Clovent.CLI/Clovent.PackageManager.Abstractions/IPackageInstaller.cs` | `IPackageInstaller.cs` | 200 B | 10 | Source Code | Active |
| `Tools` | `Tools/Clovent.CLI/Clovent.PackageManager.Core/Backup/BackupService.cs` | `BackupService.cs` | 294 B | 13 | Source Code | Active |
| `Tools` | `Tools/Clovent.CLI/Clovent.PackageManager.Core/Builder/PackageBuilder.cs` | `PackageBuilder.cs` | 756 B | 30 | Source Code | Active |
| `Tools` | `Tools/Clovent.CLI/Clovent.PackageManager.Core/Class1.cs` | `Class1.cs` | 74 B | 6 | Source Code | Active |
| `Tools` | `Tools/Clovent.CLI/Clovent.PackageManager.Core/Clovent.PackageManager.Core.csproj` | `Clovent.PackageManager.Core.csproj` | 370 B | 0 | Configuration / Project Definition | Active |
| `Tools` | `Tools/Clovent.CLI/Clovent.PackageManager.Core/Dependencies/DependencyResolver.cs` | `DependencyResolver.cs` | 650 B | 22 | Source Code | Active |
| `Tools` | `Tools/Clovent.CLI/Clovent.PackageManager.Core/Models/PackageManifest.cs` | `PackageManifest.cs` | 437 B | 18 | Source Code | Active |
| `Tools` | `Tools/Clovent.CLI/Clovent.PackageManager.Core/Packages/PackageReader.cs` | `PackageReader.cs` | 462 B | 19 | Source Code | Active |
| `Tools` | `Tools/Clovent.CLI/Clovent.PackageManager.Core/Readers/ManifestReader.cs` | `ManifestReader.cs` | 888 B | 30 | Source Code | Active |
| `Tools` | `Tools/Clovent.CLI/Clovent.PackageManager.Core/Registry/InstalledPackage.cs` | `InstalledPackage.cs` | 245 B | 10 | Source Code | Active |
| `Tools` | `Tools/Clovent.CLI/Clovent.PackageManager.Core/Registry/PackageRegistry.cs` | `PackageRegistry.cs` | 780 B | 30 | Source Code | Active |
| `Tools` | `Tools/Clovent.CLI/Clovent.PackageManager.Core/Services/PackageInstaller.cs` | `PackageInstaller.cs` | 3,155 B | 109 | Source Code | Active |
| `Tools` | `Tools/Clovent.CLI/Clovent.PackageManager.Core/Versioning/VersionComparer.cs` | `VersionComparer.cs` | 687 B | 21 | Source Code | Active |
| `Tools` | `Tools/Clovent.CLI/Clovent.PackageManager.Tests/Clovent.PackageManager.Tests.csproj` | `Clovent.PackageManager.Tests.csproj` | 634 B | 0 | Configuration / Project Definition | Active |
| `Tools` | `Tools/Clovent.CLI/Clovent.PackageManager.Tests/UnitTest1.cs` | `UnitTest1.cs` | 129 B | 10 | Test / QA Code | Active |
| `Tools` | `Tools/Clovent.CLI/Clovent.PackageManager.slnx` | `Clovent.PackageManager.slnx` | 373 B | 0 | Configuration / Project Definition | Active |
| `Tools` | `Tools/Clovent.CLI/Clovent.PackageManager/Clovent.PackageManager.csproj` | `Clovent.PackageManager.csproj` | 506 B | 0 | Configuration / Project Definition | Active |
| `Tools` | `Tools/Clovent.CLI/Clovent.PackageManager/Program.cs` | `Program.cs` | 1,317 B | 61 | Source Code | Active |
| `Tools` | `Tools/Clovent.CLI/README.md` | `README.md` | 250 B | 13 | Documentation / Report / Diagnostic | Active |
| `Tools` | `Tools/Clovent.CLI/Test.cbospkg` | `Test.cbospkg` | 380 B | 0 | Source Code | Active |
| `Tools` | `Tools/Clovent.CLI/docs/AuthenticationFoundation.docx` | `AuthenticationFoundation.docx` | 36,738 B | 0 | Source Code | Active |
| `Tools` | `Tools/Clovent.CLI/installed.json` | `installed.json` | 119 B | 7 | Configuration / Project Definition | Active |
| `Tools` | `Tools/Clovent.CLI/manifest.json` | `manifest.json` | 139 B | 5 | Configuration / Project Definition | Active |
| `Tools` | `Tools/Clovent.CLI/scripts/Install.ps1` | `Install.ps1` | 409 B | 15 | Automation / QA Script | Active |
| `Tools` | `Tools/Clovent.CLI/scripts/Rollback.ps1` | `Rollback.ps1` | 35 B | 1 | Automation / QA Script | Active |
| `Tools` | `Tools/Clovent.CLI/scripts/Verify.ps1` | `Verify.ps1` | 95 B | 3 | Automation / QA Script | Active |
| `Tools` | `Tools/Clovent.CLI/src/Clovent.Configuration/Class1.cs` | `Class1.cs` | 68 B | 6 | Source Code | Active |
| `Tools` | `Tools/Clovent.CLI/src/Clovent.Configuration/Clovent.Configuration.csproj` | `Clovent.Configuration.csproj` | 219 B | 0 | Configuration / Project Definition | Active |
| `Tools` | `Tools/Clovent.CLI/src/Clovent.Core/Clovent.Core.csproj` | `Clovent.Core.csproj` | 219 B | 0 | Configuration / Project Definition | Active |
| `Tools` | `Tools/Clovent.CLI/src/Clovent.Core/Interfaces/IBootstrapService.cs` | `IBootstrapService.cs` | 190 B | 9 | Source Code | Active |
| `Tools` | `Tools/Clovent.CLI/src/Clovent.Core/Interfaces/IDoctorService.cs` | `IDoctorService.cs` | 145 B | 8 | Source Code | Active |
| `Tools` | `Tools/Clovent.CLI/src/Clovent.Core/Models/BootstrapOptions.cs` | `BootstrapOptions.cs` | 224 B | 10 | Source Code | Active |
| `Tools` | `Tools/Clovent.CLI/src/Clovent.Core/Models/EntityGenerationOptions.cs` | `EntityGenerationOptions.cs` | 761 B | 21 | Source Code | Active |
| `Tools` | `Tools/Clovent.CLI/src/Clovent.Core/Models/ModuleGenerationOptions.cs` | `ModuleGenerationOptions.cs` | 282 B | 9 | Source Code | Active |
| `Tools` | `Tools/Clovent.CLI/src/Clovent.Core/Results/BootstrapResult.cs` | `BootstrapResult.cs` | 211 B | 10 | Source Code | Active |
| `Tools` | `Tools/Clovent.CLI/src/Clovent.Core/Results/DoctorResult.cs` | `DoctorResult.cs` | 370 B | 14 | Source Code | Active |
| `Tools` | `Tools/Clovent.CLI/src/Clovent.Core/Results/GenerationResult.cs` | `GenerationResult.cs` | 791 B | 24 | Source Code | Active |
| `Tools` | `Tools/Clovent.CLI/src/Clovent.Core/Services/BootstrapService.cs` | `BootstrapService.cs` | 913 B | 38 | Source Code | Active |
| `Tools` | `Tools/Clovent.CLI/src/Clovent.Core/Services/DoctorService.cs` | `DoctorService.cs` | 1,378 B | 42 | Source Code | Active |
| `Tools` | `Tools/Clovent.CLI/src/Clovent.Documents/Clovent.Documents.csproj` | `Clovent.Documents.csproj` | 324 B | 0 | Configuration / Project Definition | Active |
| `Tools` | `Tools/Clovent.CLI/src/Clovent.Documents/DocumentGenerator.cs` | `DocumentGenerator.cs` | 796 B | 23 | Source Code | Active |
| `Tools` | `Tools/Clovent.CLI/src/Clovent.Documents/IDocumentGenerator.cs` | `IDocumentGenerator.cs` | 314 B | 9 | Source Code | Active |
| `Tools` | `Tools/Clovent.CLI/src/Clovent.Documents/IObsidianVaultManager.cs` | `IObsidianVaultManager.cs` | 257 B | 8 | Source Code | Active |
| `Tools` | `Tools/Clovent.CLI/src/Clovent.Documents/ObsidianVaultManager.cs` | `ObsidianVaultManager.cs` | 1,621 B | 54 | Source Code | Active |
| `Tools` | `Tools/Clovent.CLI/src/Clovent.Generator/Clovent.Generator.csproj` | `Clovent.Generator.csproj` | 654 B | 0 | Configuration / Project Definition | Active |
| `Tools` | `Tools/Clovent.CLI/src/Clovent.Generator/Services/Entity/EntityGenerator.cs` | `EntityGenerator.cs` | 5,088 B | 114 | Source Code | Active |
| `Tools` | `Tools/Clovent.CLI/src/Clovent.Generator/Services/Entity/IEntityGenerator.cs` | `IEntityGenerator.cs` | 204 B | 9 | Source Code | Active |
| `Tools` | `Tools/Clovent.CLI/src/Clovent.Generator/Services/Module/IModuleGenerator.cs` | `IModuleGenerator.cs` | 204 B | 9 | Source Code | Active |
| `Tools` | `Tools/Clovent.CLI/src/Clovent.Generator/Services/Module/ModuleGenerator.cs` | `ModuleGenerator.cs` | 2,480 B | 80 | Source Code | Active |
| `Tools` | `Tools/Clovent.CLI/src/Clovent.Modules.Identity/Application/Authentication/Requests/LoginRequest.cs` | `LoginRequest.cs` | 152 B | 5 | Source Code | Active |
| `Tools` | `Tools/Clovent.CLI/src/Clovent.Modules.Identity/Application/Authentication/Responses/LoginResponse.cs` | `LoginResponse.cs` | 250 B | 10 | Source Code | Active |
| `Tools` | `Tools/Clovent.CLI/src/Clovent.Modules.Identity/Application/Authentication/Services/IAuthenticationService.cs` | `IAuthenticationService.cs` | 314 B | 9 | Source Code | Active |
| `Tools` | `Tools/Clovent.CLI/src/Clovent.Modules.Identity/Application/Authentication/Services/IPasswordHasher.cs` | `IPasswordHasher.cs` | 192 B | 8 | Source Code | Active |
| `Tools` | `Tools/Clovent.CLI/src/Clovent.Modules.Identity/Clovent.Modules.Identity.csproj` | `Clovent.Modules.Identity.csproj` | 1,628 B | 0 | Configuration / Project Definition | Active |
| `Tools` | `Tools/Clovent.CLI/src/Clovent.Modules.Identity/Domain/Entities/Branch.cs` | `Branch.cs` | 831 B | 37 | Source Code | Active |
| `Tools` | `Tools/Clovent.CLI/src/Clovent.Modules.Identity/Domain/Entities/Company.cs` | `Company.cs` | 738 B | 34 | Source Code | Active |
| `Tools` | `Tools/Clovent.CLI/src/Clovent.Modules.Identity/Domain/Entities/Permission.cs` | `Permission.cs` | 820 B | 35 | Source Code | Active |
| `Tools` | `Tools/Clovent.CLI/src/Clovent.Modules.Identity/Domain/Entities/Role.cs` | `Role.cs` | 1,111 B | 48 | Source Code | Active |
| `Tools` | `Tools/Clovent.CLI/src/Clovent.Modules.Identity/Domain/Entities/RolePermission.cs` | `RolePermission.cs` | 441 B | 20 | Source Code | Active |
| `Tools` | `Tools/Clovent.CLI/src/Clovent.Modules.Identity/Domain/Entities/User.cs` | `User.cs` | 1,662 B | 68 | Source Code | Active |
| `Tools` | `Tools/Clovent.CLI/src/Clovent.Modules.Identity/Domain/Entities/UserRole.cs` | `UserRole.cs` | 399 B | 20 | Source Code | Active |
| `Tools` | `Tools/Clovent.CLI/src/Clovent.Modules.Identity/Domain/ValueObjects/IdentityIds.cs` | `IdentityIds.cs` | 889 B | 31 | Source Code | Active |
| `Tools` | `Tools/Clovent.CLI/src/Clovent.Modules.Identity/Infrastructure/Security/AuthenticationService.cs` | `AuthenticationService.cs` | 579 B | 17 | Source Code | Active |
| `Tools` | `Tools/Clovent.CLI/src/Clovent.Modules.Identity/Infrastructure/Security/PasswordHasher.cs` | `PasswordHasher.cs` | 418 B | 16 | Source Code | Active |
| `Tools` | `Tools/Clovent.CLI/src/Clovent.Modules.Identity/Persistence/Configurations/BranchConfiguration.cs` | `BranchConfiguration.cs` | 398 B | 13 | Source Code | Active |
| `Tools` | `Tools/Clovent.CLI/src/Clovent.Modules.Identity/Persistence/Configurations/CompanyConfiguration.cs` | `CompanyConfiguration.cs` | 402 B | 13 | Source Code | Active |
| `Tools` | `Tools/Clovent.CLI/src/Clovent.Modules.Identity/Persistence/Configurations/PermissionConfiguration.cs` | `PermissionConfiguration.cs` | 413 B | 13 | Source Code | Active |
| `Tools` | `Tools/Clovent.CLI/src/Clovent.Modules.Identity/Persistence/Configurations/RoleConfiguration.cs` | `RoleConfiguration.cs` | 389 B | 13 | Source Code | Active |
| `Tools` | `Tools/Clovent.CLI/src/Clovent.Modules.Identity/Persistence/Configurations/RolePermissionConfiguration.cs` | `RolePermissionConfiguration.cs` | 429 B | 13 | Source Code | Active |
| `Tools` | `Tools/Clovent.CLI/src/Clovent.Modules.Identity/Persistence/Configurations/UserConfiguration.cs` | `UserConfiguration.cs` | 389 B | 13 | Source Code | Active |
| `Tools` | `Tools/Clovent.CLI/src/Clovent.Modules.Identity/Persistence/Configurations/UserRoleConfiguration.cs` | `UserRoleConfiguration.cs` | 405 B | 13 | Source Code | Active |
| `Tools` | `Tools/Clovent.CLI/src/Clovent.Modules.Identity/Persistence/DependencyInjection/DependencyInjection.cs` | `DependencyInjection.cs` | 491 B | 19 | Source Code | Active |
| `Tools` | `Tools/Clovent.CLI/src/Clovent.Modules.Identity/Persistence/DesignTimeDbContextFactory.cs` | `DesignTimeDbContextFactory.cs` | 1,095 B | 28 | Source Code | Active |
| `Tools` | `Tools/Clovent.CLI/src/Clovent.Modules.Identity/Persistence/IdentityDbContext.cs` | `IdentityDbContext.cs` | 1,125 B | 40 | Source Code | Active |
| `Tools` | `Tools/Clovent.CLI/src/Clovent.Modules.Identity/Persistence/Migrations/20260726154721_InitialIdentity.Designer.cs` | `20260726154721_InitialIdentity.Designer.cs` | 7,938 B | 222 | Source Code | Active |
| `Tools` | `Tools/Clovent.CLI/src/Clovent.Modules.Identity/Persistence/Migrations/20260726154721_InitialIdentity.cs` | `20260726154721_InitialIdentity.cs` | 6,943 B | 151 | Source Code | Active |
| `Tools` | `Tools/Clovent.CLI/src/Clovent.Modules.Identity/Persistence/Migrations/IdentityDbContextModelSnapshot.cs` | `IdentityDbContextModelSnapshot.cs` | 7,835 B | 219 | Source Code | Active |
| `Tools` | `Tools/Clovent.CLI/src/Clovent.Modules.Identity/Persistence/Seeder/DatabaseSeeder.cs` | `DatabaseSeeder.cs` | 261 B | 11 | Source Code | Active |
| `Tools` | `Tools/Clovent.CLI/src/Clovent.Modules.Identity/appsettings.json` | `appsettings.json` | 162 B | 5 | Configuration / Project Definition | Active |
| `Tools` | `Tools/Clovent.CLI/src/Clovent.Shared/Class1.cs` | `Class1.cs` | 61 B | 6 | Source Code | Active |
| `Tools` | `Tools/Clovent.CLI/src/Clovent.Shared/Clovent.Shared.csproj` | `Clovent.Shared.csproj` | 306 B | 0 | Configuration / Project Definition | Active |
| `Tools` | `Tools/Clovent.CLI/src/Clovent.Shared/Domain/DomainPrimitives.cs` | `DomainPrimitives.cs` | 590 B | 26 | Source Code | Active |
| `Tools` | `Tools/Clovent.CLI/src/Clovent.Templates/Clovent.Templates.csproj` | `Clovent.Templates.csproj` | 308 B | 0 | Configuration / Project Definition | Active |
| `Tools` | `Tools/Clovent.CLI/src/Clovent.Templates/ITemplateEngine.cs` | `ITemplateEngine.cs` | 862 B | 18 | Source Code | Active |
| `Tools` | `Tools/Clovent.CLI/src/Clovent.Templates/TemplateEngine.cs` | `TemplateEngine.cs` | 14,296 B | 288 | Source Code | Active |
| `Tools` | `Tools/Clovent.CLI/tests/Clovent.Core.Tests/BootstrapAndDoctorServiceTests.cs` | `BootstrapAndDoctorServiceTests.cs` | 1,471 B | 47 | Test / QA Code | Active |
| `Tools` | `Tools/Clovent.CLI/tests/Clovent.Core.Tests/Clovent.Core.Tests.csproj` | `Clovent.Core.Tests.csproj` | 746 B | 0 | Configuration / Project Definition | Active |
| `Tools` | `Tools/Clovent.CLI/tests/Clovent.Generator.Tests/Clovent.Generator.Tests.csproj` | `Clovent.Generator.Tests.csproj` | 1,019 B | 0 | Configuration / Project Definition | Active |
| `Tools` | `Tools/Clovent.CLI/tests/Clovent.Generator.Tests/EntityGeneratorTests.cs` | `EntityGeneratorTests.cs` | 2,116 B | 53 | Test / QA Code | Active |
| `Tools` | `Tools/Clovent.CLI/tests/Clovent.Generator.Tests/ModuleGeneratorTests.cs` | `ModuleGeneratorTests.cs` | 1,624 B | 48 | Test / QA Code | Active |
| `Tools` | `Tools/Clovent.CLI/tests/Clovent.Generator.Tests/TemplateEngineTests.cs` | `TemplateEngineTests.cs` | 1,666 B | 48 | Test / QA Code | Active |
| `Tools` | `Tools/PowerShell/Bootstrap.ps1` | `Bootstrap.ps1` | 1,120 B | 44 | Automation / QA Script | Active |
| `backups` | `backups/config-security-fix-2026-08-17/Clovent.Desktop.Tests.csproj.bak` | `Clovent.Desktop.Tests.csproj.bak` | 711 B | 0 | Backup File | Backup / Obsolete |
| `backups` | `backups/config-security-fix-2026-08-17/Clovent.Desktop.csproj.bak` | `Clovent.Desktop.csproj.bak` | 4,228 B | 0 | Backup File | Backup / Obsolete |
| `backups` | `backups/config-security-fix-2026-08-17/appsettings.json.bak` | `appsettings.json.bak` | 1,180 B | 0 | Backup File | Backup / Obsolete |
| `backups` | `backups/diagnostic-scaffold-deletion-2026-08-17/DiagnosticDerivedEmptyForm.Designer.cs` | `DiagnosticDerivedEmptyForm.Designer.cs` | 1,586 B | 56 | Backup File | Backup / Obsolete |
| `backups` | `backups/diagnostic-scaffold-deletion-2026-08-17/DiagnosticDerivedEmptyForm.cs` | `DiagnosticDerivedEmptyForm.cs` | 194 B | 9 | Backup File | Backup / Obsolete |
| `backups` | `backups/diagnostic-scaffold-deletion-2026-08-17/DiagnosticDerivedEmptyForm.resx` | `DiagnosticDerivedEmptyForm.resx` | 5,748 B | 120 | UI Resource | Backup / Obsolete |
| `backups` | `backups/diagnostic-scaffold-deletion-2026-08-17/DiagnosticDerivedWinForm.Designer.cs` | `DiagnosticDerivedWinForm.Designer.cs` | 641 B | 23 | Backup File | Backup / Obsolete |
| `backups` | `backups/diagnostic-scaffold-deletion-2026-08-17/DiagnosticDerivedWinForm.cs` | `DiagnosticDerivedWinForm.cs` | 190 B | 9 | Backup File | Backup / Obsolete |
| `backups` | `backups/diagnostic-scaffold-deletion-2026-08-17/DiagnosticDerivedWinForm.resx` | `DiagnosticDerivedWinForm.resx` | 5,817 B | 120 | UI Resource | Backup / Obsolete |
| `backups` | `backups/diagnostic-scaffold-deletion-2026-08-17/DiagnosticMasterDataBaseForm.Designer.cs` | `DiagnosticMasterDataBaseForm.Designer.cs` | 1,329 B | 41 | Backup File | Backup / Obsolete |
| `backups` | `backups/diagnostic-scaffold-deletion-2026-08-17/DiagnosticMasterDataBaseForm.cs` | `DiagnosticMasterDataBaseForm.cs` | 253 B | 10 | Backup File | Backup / Obsolete |
| `backups` | `backups/diagnostic-scaffold-deletion-2026-08-17/DiagnosticMasterDataBaseForm.resx` | `DiagnosticMasterDataBaseForm.resx` | 5,817 B | 120 | UI Resource | Backup / Obsolete |
| `backups` | `backups/diagnostic-scaffold-deletion-2026-08-17/DiagnosticMasterDataContentForm.Designer.cs` | `DiagnosticMasterDataContentForm.Designer.cs` | 1,447 B | 43 | Backup File | Backup / Obsolete |
| `backups` | `backups/diagnostic-scaffold-deletion-2026-08-17/DiagnosticMasterDataContentForm.cs` | `DiagnosticMasterDataContentForm.cs` | 259 B | 10 | Backup File | Backup / Obsolete |
| `backups` | `backups/diagnostic-scaffold-deletion-2026-08-17/DiagnosticMasterDataContentForm.resx` | `DiagnosticMasterDataContentForm.resx` | 5,817 B | 120 | UI Resource | Backup / Obsolete |
| `backups` | `backups/diagnostic-scaffold-deletion-2026-08-17/DiagnosticWinForm.Designer.cs` | `DiagnosticWinForm.Designer.cs` | 678 B | 26 | Backup File | Backup / Obsolete |
| `backups` | `backups/diagnostic-scaffold-deletion-2026-08-17/DiagnosticWinForm.cs` | `DiagnosticWinForm.cs` | 241 B | 12 | Backup File | Backup / Obsolete |
| `backups` | `backups/diagnostic-scaffold-deletion-2026-08-17/DiagnosticWinForm.resx` | `DiagnosticWinForm.resx` | 5,817 B | 120 | UI Resource | Backup / Obsolete |
| `backups` | `backups/diagnostic-scaffold-deletion-2026-08-17/MinimalBaseForm.Designer.cs` | `MinimalBaseForm.Designer.cs` | 623 B | 23 | Backup File | Backup / Obsolete |
| `backups` | `backups/diagnostic-scaffold-deletion-2026-08-17/MinimalBaseForm.cs` | `MinimalBaseForm.cs` | 170 B | 9 | Backup File | Backup / Obsolete |
| `backups` | `backups/diagnostic-scaffold-deletion-2026-08-17/MinimalBaseForm.resx` | `MinimalBaseForm.resx` | 5,817 B | 120 | UI Resource | Backup / Obsolete |
| `backups` | `backups/diagnostic-scaffold-deletion-2026-08-17/MinimalDerivedForm.Designer.cs` | `MinimalDerivedForm.Designer.cs` | 629 B | 23 | Backup File | Backup / Obsolete |
| `backups` | `backups/diagnostic-scaffold-deletion-2026-08-17/MinimalDerivedForm.cs` | `MinimalDerivedForm.cs` | 176 B | 9 | Backup File | Backup / Obsolete |
| `backups` | `backups/diagnostic-scaffold-deletion-2026-08-17/MinimalDerivedForm.resx` | `MinimalDerivedForm.resx` | 5,817 B | 120 | UI Resource | Backup / Obsolete |
| `backups` | `backups/diagnostic-scaffold-deletion-2026-08-17/MinimalDiagnosticForms.resx` | `MinimalDiagnosticForms.resx` | 5,817 B | 120 | UI Resource | Backup / Obsolete |
| `backups` | `backups/diagnostic-scaffold-deletion-2026-08-17/MinimalXtraForm.Designer.cs` | `MinimalXtraForm.Designer.cs` | 623 B | 23 | Backup File | Backup / Obsolete |
| `backups` | `backups/diagnostic-scaffold-deletion-2026-08-17/MinimalXtraForm.cs` | `MinimalXtraForm.cs` | 186 B | 9 | Backup File | Backup / Obsolete |
| `backups` | `backups/diagnostic-scaffold-deletion-2026-08-17/MinimalXtraForm.resx` | `MinimalXtraForm.resx` | 5,817 B | 120 | UI Resource | Backup / Obsolete |
| `backups` | `backups/diagnostic-scaffold-deletion-2026-08-17/TestDerivedForm.Designer.cs` | `TestDerivedForm.Designer.cs` | 623 B | 23 | Backup File | Backup / Obsolete |
| `backups` | `backups/diagnostic-scaffold-deletion-2026-08-17/TestDerivedForm.cs` | `TestDerivedForm.cs` | 240 B | 11 | Backup File | Backup / Obsolete |
| `backups` | `backups/logging-2026-08-17/Program.cs.bak` | `Program.cs.bak` | 15,968 B | 0 | Backup File | Backup / Obsolete |
| `backups` | `backups/logging-2026-08-17/appsettings.json.bak` | `appsettings.json.bak` | 1,183 B | 0 | Backup File | Backup / Obsolete |
| `backups` | `backups/rootcause-2026-08-18/FileLoggerProvider.cs.bak` | `FileLoggerProvider.cs.bak` | 6,564 B | 0 | Backup File | Backup / Obsolete |
| `backups` | `backups/rootcause-2026-08-18/RestaurantPosForm.Designer.cs.bak` | `RestaurantPosForm.Designer.cs.bak` | 109,304 B | 0 | Backup File | Backup / Obsolete |
| `backups` | `backups/rootcause-2026-08-18/RestaurantPosForm.Designer.cs.pre-sortfix.bak` | `RestaurantPosForm.Designer.cs.pre-sortfix.bak` | 107,099 B | 0 | Backup File | Backup / Obsolete |
| `backups` | `backups/rootcause-2026-08-18/RestaurantPosForm.cs.bak` | `RestaurantPosForm.cs.bak` | 88,167 B | 0 | Backup File | Backup / Obsolete |
| `cart_diagnostics.txt` | `cart_diagnostics.txt` | `cart_diagnostics.txt` | 182,842 B | 699 | Documentation / Report / Diagnostic | Active |
| `click.ps1` | `click.ps1` | `click.ps1` | 573 B | 8 | Automation / QA Script | Active |
| `docs` | `docs/README.md` | `README.md` | 4,693 B | 53 | Documentation | Active |
| `docs` | `docs/architecture/AuthenticationDomain.md` | `AuthenticationDomain.md` | 27,424 B | 247 | Documentation | Active |
| `docs` | `docs/architecture/AuthenticationInfrastructure.md` | `AuthenticationInfrastructure.md` | 21,007 B | 193 | Documentation | Active |
| `docs` | `docs/architecture/AuthenticationIntegration.md` | `AuthenticationIntegration.md` | 11,100 B | 79 | Documentation | Active |
| `docs` | `docs/architecture/Authorization.md` | `Authorization.md` | 11,138 B | 71 | Documentation | Active |
| `docs` | `docs/architecture/CatalogArchitecture.md` | `CatalogArchitecture.md` | 8,205 B | 80 | Documentation | Active |
| `docs` | `docs/architecture/Dashboard.md` | `Dashboard.md` | 10,753 B | 84 | Documentation | Active |
| `docs` | `docs/architecture/DesktopAdministration.md` | `DesktopAdministration.md` | 13,489 B | 94 | Documentation | Active |
| `docs` | `docs/architecture/DesktopBootstrap.md` | `DesktopBootstrap.md` | 35,041 B | 236 | Documentation | Active |
| `docs` | `docs/architecture/DesktopShellArchitecture.md` | `DesktopShellArchitecture.md` | 20,072 B | 218 | Documentation | Active |
| `docs` | `docs/architecture/DesktopUILayout.md` | `DesktopUILayout.md` | 39,194 B | 189 | Documentation | Active |
| `docs` | `docs/architecture/EndOfDayReporting.md` | `EndOfDayReporting.md` | 3,250 B | 52 | Documentation | Active |
| `docs` | `docs/architecture/IdentityDomain.md` | `IdentityDomain.md` | 19,389 B | 207 | Documentation | Active |
| `docs` | `docs/architecture/InventoryArchitecture.md` | `InventoryArchitecture.md` | 8,530 B | 78 | Documentation | Active |
| `docs` | `docs/architecture/KitchenWorkflow.md` | `KitchenWorkflow.md` | 4,240 B | 46 | Documentation | Active |
| `docs` | `docs/architecture/MasterData.md` | `MasterData.md` | 10,922 B | 90 | Documentation | Active |
| `docs` | `docs/architecture/OrderLifecycle.md` | `OrderLifecycle.md` | 8,226 B | 78 | Documentation | Active |
| `docs` | `docs/architecture/OrganizationArchitecture.md` | `OrganizationArchitecture.md` | 10,015 B | 77 | Documentation | Active |
| `docs` | `docs/architecture/PlatformFoundation.md` | `PlatformFoundation.md` | 18,010 B | 252 | Documentation | Active |
| `docs` | `docs/architecture/PlatformVersioning.md` | `PlatformVersioning.md` | 8,720 B | 69 | Documentation | Active |
| `docs` | `docs/architecture/RestaurantPOSArchitecture.md` | `RestaurantPOSArchitecture.md` | 105,844 B | 723 | Documentation | Active |
| `docs` | `docs/architecture/TechnicalDebt.md` | `TechnicalDebt.md` | 4,953 B | 59 | Documentation | Active |
| `docs` | `docs/architecture/WarehouseManagement.md` | `WarehouseManagement.md` | 9,388 B | 74 | Documentation | Active |
| `docs` | `docs/architecture/adr/ADR-001-RestaurantPOS-SingleForm.md` | `ADR-001-RestaurantPOS-SingleForm.md` | 2,851 B | 28 | Documentation | Active |
| `docs` | `docs/architecture/adr/ADR-002-Payment-Tender-Strip.md` | `ADR-002-Payment-Tender-Strip.md` | 3,437 B | 40 | Documentation | Active |
| `docs` | `docs/architecture/adr/ADR-003-Designer-Safe-WinForms.md` | `ADR-003-Designer-Safe-WinForms.md` | 4,353 B | 54 | Documentation | Active |
| `docs` | `docs/architecture/adr/ADR-004-Responsive-POS-Layout.md` | `ADR-004-Responsive-POS-Layout.md` | 3,062 B | 42 | Documentation | Active |
| `docs` | `docs/architecture/adr/ADR-005-Customer-Credit-Workflow.md` | `ADR-005-Customer-Credit-Workflow.md` | 2,617 B | 36 | Documentation | Active |
| `docs` | `docs/architecture/adr/ADR-006-Customer-Management-and-Ledger.md` | `ADR-006-Customer-Management-and-Ledger.md` | 12,154 B | 98 | Documentation | Active |
| `docs` | `docs/architecture/adr/ADR-007-Designer-CodeDom-Constraints.md` | `ADR-007-Designer-CodeDom-Constraints.md` | 7,581 B | 151 | Documentation | Active |
| `docs` | `docs/changelog/RestaurantPOS.md` | `RestaurantPOS.md` | 32,094 B | 419 | Documentation | Active |
| `docs` | `docs/reviews/Milestone-03-Architecture-Review.md` | `Milestone-03-Architecture-Review.md` | 41,950 B | 413 | Documentation | Active |
| `docs` | `docs/testing/RestaurantPOSManualQA.md` | `RestaurantPOSManualQA.md` | 37,100 B | 333 | Documentation | Active |
| `docs` | `docs/testing/RestaurantPOSTesting.md` | `RestaurantPOSTesting.md` | 13,144 B | 137 | Documentation | Active |
| `enum.ps1` | `enum.ps1` | `enum.ps1` | 3,129 B | 61 | Automation / QA Script | Active |
| `fg.ps1` | `fg.ps1` | `fg.ps1` | 561 B | 9 | Automation / QA Script | Active |
| `find.ps1` | `find.ps1` | `find.ps1` | 1,466 B | 26 | Automation / QA Script | Active |
| `login.ps1` | `login.ps1` | `login.ps1` | 1,865 B | 35 | Automation / QA Script | Active |
| `pclick.ps1` | `pclick.ps1` | `pclick.ps1` | 793 B | 11 | Automation / QA Script | Active |
| `qa` | `qa/QA_01_Startup.png` | `QA_01_Startup.png` | 68,583 B | 0 | Image / Asset | Active |
| `qa` | `qa/QA_02_POS_initial.png` | `QA_02_POS_initial.png` | 955,903 B | 0 | Image / Asset | Active |
| `qa` | `qa/accept_01_pos_startup_native.png` | `accept_01_pos_startup_native.png` | 949,919 B | 0 | Image / Asset | Active |
| `qa` | `qa/accept_02_pos_1024x768.png` | `accept_02_pos_1024x768.png` | 478,327 B | 0 | Image / Asset | Active |
| `qa` | `qa/accept_03_pos_1366x768.png` | `accept_03_pos_1366x768.png` | 632,694 B | 0 | Image / Asset | Active |
| `qa` | `qa/accept_04_pos_1920x1080.png` | `accept_04_pos_1920x1080.png` | 632,694 B | 0 | Image / Asset | Active |
| `qa` | `qa/accept_4k250_held.png` | `accept_4k250_held.png` | 725,640 B | 0 | Image / Asset | Active |
| `qa` | `qa/accept_recall87.txt` | `accept_recall87.txt` | 18,784 B | 338 | Documentation / Report / Diagnostic | Active |
| `qa` | `qa/accept_recall87b.txt` | `accept_recall87b.txt` | 18,784 B | 338 | Documentation / Report / Diagnostic | Active |
| `qa` | `qa/capture_live_recall.ps1` | `capture_live_recall.ps1` | 8,908 B | 223 | Automation / QA Script | Active |
| `qa` | `qa/capture_open_tab.ps1` | `capture_open_tab.ps1` | 5,240 B | 110 | Automation / QA Script | Active |
| `qa` | `qa/ck.ps1` | `ck.ps1` | 791 B | 15 | Automation / QA Script | Active |
| `qa` | `qa/click.ps1` | `click.ps1` | 787 B | 12 | Automation / QA Script | Active |
| `qa` | `qa/click2.ps1` | `click2.ps1` | 658 B | 11 | Automation / QA Script | Active |
| `qa` | `qa/crop.ps1` | `crop.ps1` | 683 B | 10 | Automation / QA Script | Active |
| `qa` | `qa/current_screen.png` | `current_screen.png` | 35,902 B | 0 | Image / Asset | Active |
| `qa` | `qa/editval.ps1` | `editval.ps1` | 975 B | 16 | Automation / QA Script | Active |
| `qa` | `qa/enumwin.ps1` | `enumwin.ps1` | 1,524 B | 20 | Automation / QA Script | Active |
| `qa` | `qa/final2_4k250_held.png` | `final2_4k250_held.png` | 726,097 B | 0 | Image / Asset | Active |
| `qa` | `qa/final_1024_recall.png` | `final_1024_recall.png` | 37,895 B | 0 | Image / Asset | Active |
| `qa` | `qa/final_1080_recall.png` | `final_1080_recall.png` | 53,341 B | 0 | Image / Asset | Active |
| `qa` | `qa/final_1366_recall.png` | `final_1366_recall.png` | 42,996 B | 0 | Image / Asset | Active |
| `qa` | `qa/final_4k_recall.png` | `final_4k_recall.png` | 100,469 B | 0 | Image / Asset | Active |
| `qa` | `qa/final_acceptance_qa.ps1` | `final_acceptance_qa.ps1` | 13,771 B | 304 | Automation / QA Script | Active |
| `qa` | `qa/find.ps1` | `find.ps1` | 1,074 B | 16 | Automation / QA Script | Active |
| `qa` | `qa/find_button_coords.ps1` | `find_button_coords.ps1` | 1,069 B | 22 | Automation / QA Script | Active |
| `qa` | `qa/force_size.ps1` | `force_size.ps1` | 1,389 B | 12 | Automation / QA Script | Active |
| `qa` | `qa/invoke.ps1` | `invoke.ps1` | 1,163 B | 23 | Automation / QA Script | Active |
| `qa` | `qa/list_pos_elements.ps1` | `list_pos_elements.ps1` | 1,574 B | 37 | Automation / QA Script | Active |
| `qa` | `qa/live-pos-evidence/T01_qty_minus_floor.png` | `T01_qty_minus_floor.png` | 89,075 B | 0 | Image / Asset | Active |
| `qa` | `qa/live_01_establish_control.png` | `live_01_establish_control.png` | 44,324 B | 0 | Image / Asset | Active |
| `qa` | `qa/live_02_t2_notes.png` | `live_02_t2_notes.png` | 44,268 B | 0 | Image / Asset | Active |
| `qa` | `qa/live_03_hold.png` | `live_03_hold.png` | 44,324 B | 0 | Image / Asset | Active |
| `qa` | `qa/live_04_recall.png` | `live_04_recall.png` | 44,268 B | 0 | Image / Asset | Active |
| `qa` | `qa/live_05_duplicate.png` | `live_05_duplicate.png` | 44,324 B | 0 | Image / Asset | Active |
| `qa` | `qa/live_06_delete_void.png` | `live_06_delete_void.png` | 44,268 B | 0 | Image / Asset | Active |
| `qa` | `qa/live_07_customer.png` | `live_07_customer.png` | 44,324 B | 0 | Image / Asset | Active |
| `qa` | `qa/live_08_dinein_table.png` | `live_08_dinein_table.png` | 44,268 B | 0 | Image / Asset | Active |
| `qa` | `qa/live_09_active_orders.png` | `live_09_active_orders.png` | 44,324 B | 0 | Image / Asset | Active |
| `qa` | `qa/live_11_clear.png` | `live_11_clear.png` | 138,355 B | 0 | Image / Asset | Active |
| `qa` | `qa/live_12_cancel.png` | `live_12_cancel.png` | 138,399 B | 0 | Image / Asset | Active |
| `qa` | `qa/live_20_place_order.png` | `live_20_place_order.png` | 138,355 B | 0 | Image / Asset | Active |
| `qa` | `qa/live_21_sales_history.png` | `live_21_sales_history.png` | 138,399 B | 0 | Image / Asset | Active |
| `qa` | `qa/live_24_1024x768.png` | `live_24_1024x768.png` | 44,268 B | 0 | Image / Asset | Active |
| `qa` | `qa/live_25_1366x768.png` | `live_25_1366x768.png` | 44,324 B | 0 | Image / Asset | Active |
| `qa` | `qa/live_26_maximized.png` | `live_26_maximized.png` | 44,268 B | 0 | Image / Asset | Active |
| `qa` | `qa/live_27_auto_dinein_table.png` | `live_27_auto_dinein_table.png` | 140,811 B | 0 | Image / Asset | Active |
| `qa` | `qa/live_login.ps1` | `live_login.ps1` | 2,067 B | 36 | Automation / QA Script | Active |
| `qa` | `qa/live_login2.ps1` | `live_login2.ps1` | 2,291 B | 41 | Automation / QA Script | Active |
| `qa` | `qa/live_login3.ps1` | `live_login3.ps1` | 1,398 B | 25 | Automation / QA Script | Active |
| `qa` | `qa/live_login4.ps1` | `live_login4.ps1` | 2,998 B | 57 | Automation / QA Script | Active |
| `qa` | `qa/live_pos_uia.txt` | `live_pos_uia.txt` | 5,167 B | 180 | Documentation / Report / Diagnostic | Active |
| `qa` | `qa/live_pw_field.png` | `live_pw_field.png` | 5,910 B | 0 | Image / Asset | Active |
| `qa` | `qa/live_ui/ClickTool.cs` | `ClickTool.cs` | 1,694 B | 59 | Source Code | Active |
| `qa` | `qa/live_ui/PIN_00_admin_login.png` | `PIN_00_admin_login.png` | 72,290 B | 0 | Image / Asset | Active |
| `qa` | `qa/live_ui/PIN_01_admin_login.png` | `PIN_01_admin_login.png` | 73,538 B | 0 | Image / Asset | Active |
| `qa` | `qa/live_ui/PIN_02_backoffice_main.png` | `PIN_02_backoffice_main.png` | 74,671 B | 0 | Image / Asset | Active |
| `qa` | `qa/live_ui/PIN_02_bo_max.png` | `PIN_02_bo_max.png` | 292,242 B | 0 | Image / Asset | Active |
| `qa` | `qa/live_ui/PIN_03_admin_tab.png` | `PIN_03_admin_tab.png` | 292,957 B | 0 | Image / Asset | Active |
| `qa` | `qa/live_ui/PIN_04_users_screen.png` | `PIN_04_users_screen.png` | 220,929 B | 0 | Image / Asset | Active |
| `qa` | `qa/live_ui/PIN_05_setpin_dialog.png` | `PIN_05_setpin_dialog.png` | 11,297,112 B | 0 | Image / Asset | Active |
| `qa` | `qa/live_ui/PIN_06_pin_entered.png` | `PIN_06_pin_entered.png` | 266,202 B | 0 | Image / Asset | Active |
| `qa` | `qa/live_ui/PIN_07_pin_only_entered.png` | `PIN_07_pin_only_entered.png` | 87,674 B | 0 | Image / Asset | Active |
| `qa` | `qa/live_ui/PIN_08_admin_relogin.png` | `PIN_08_admin_relogin.png` | 88,511 B | 0 | Image / Asset | Active |
| `qa` | `qa/live_ui/PIN_09_admin_pin_set.png` | `PIN_09_admin_pin_set.png` | 276,636 B | 0 | Image / Asset | Active |
| `qa` | `qa/live_ui/PIN_10_pin_only_pos.png` | `PIN_10_pin_only_pos.png` | 87,674 B | 0 | Image / Asset | Active |
| `qa` | `qa/live_ui/PIN_11_pos_as_pin_user.png` | `PIN_11_pos_as_pin_user.png` | 742,729 B | 0 | Image / Asset | Active |
| `qa` | `qa/live_ui/PIN_12_invalid_pin.png` | `PIN_12_invalid_pin.png` | 72,704 B | 0 | Image / Asset | Active |
| `qa` | `qa/live_ui/PIN_13_password_regression.png` | `PIN_13_password_regression.png` | 76,123 B | 0 | Image / Asset | Active |
| `qa` | `qa/live_ui/PIN_14_bo_admin.png` | `PIN_14_bo_admin.png` | 73,483 B | 0 | Image / Asset | Active |
| `qa` | `qa/live_ui/PIN_15_clearpin_confirm.png` | `PIN_15_clearpin_confirm.png` | 207,781 B | 0 | Image / Asset | Active |
| `qa` | `qa/live_ui/PIN_15_pin_cleared.png` | `PIN_15_pin_cleared.png` | 200,165 B | 0 | Image / Asset | Active |
| `qa` | `qa/live_ui/PIN_16_cleared_pin_fails.png` | `PIN_16_cleared_pin_fails.png` | 72,740 B | 0 | Image / Asset | Active |
| `qa` | `qa/live_ui/PIN_dbg_login.png` | `PIN_dbg_login.png` | 106,523 B | 0 | Image / Asset | Active |
| `qa` | `qa/live_ui/QA_01_Login.png` | `QA_01_Login.png` | 1,287,508 B | 0 | Image / Asset | Active |
| `qa` | `qa/live_ui/QA_02_POS_Startup.png` | `QA_02_POS_Startup.png` | 1,287,497 B | 0 | Image / Asset | Active |
| `qa` | `qa/live_ui/QA_05_Quantity.png` | `QA_05_Quantity.png` | 1,293,164 B | 0 | Image / Asset | Active |
| `qa` | `qa/live_ui/QA_06_Notes.png` | `QA_06_Notes.png` | 1,293,749 B | 0 | Image / Asset | Active |
| `qa` | `qa/live_ui/T01_login.png` | `T01_login.png` | 74,598 B | 0 | Image / Asset | Active |
| `qa` | `qa/live_ui/T01_login_state.png` | `T01_login_state.png` | 116,866 B | 0 | Image / Asset | Active |
| `qa` | `qa/live_ui/T02_pos_startup.png` | `T02_pos_startup.png` | 2,809,954 B | 0 | Image / Asset | Active |
| `qa` | `qa/live_ui/T03_new_takeaway.png` | `T03_new_takeaway.png` | 2,807,969 B | 0 | Image / Asset | Active |
| `qa` | `qa/live_ui/T22_clear_after.png` | `T22_clear_after.png` | 2,807,969 B | 0 | Image / Asset | Active |
| `qa` | `qa/live_ui/T22_clear_confirm.png` | `T22_clear_confirm.png` | 2,849,806 B | 0 | Image / Asset | Active |
| `qa` | `qa/live_ui/btns.ps1` | `btns.ps1` | 1,019 B | 21 | Automation / QA Script | Active |
| `qa` | `qa/live_ui/cap.ps1` | `cap.ps1` | 1,004 B | 22 | Automation / QA Script | Active |
| `qa` | `qa/live_ui/dbg_login.ps1` | `dbg_login.ps1` | 1,957 B | 34 | Automation / QA Script | Active |
| `qa` | `qa/live_ui/dologin.ps1` | `dologin.ps1` | 3,494 B | 67 | Automation / QA Script | Active |
| `qa` | `qa/live_ui/drv.ps1` | `drv.ps1` | 5,459 B | 116 | Automation / QA Script | Active |
| `qa` | `qa/live_ui/flow.ps1` | `flow.ps1` | 5,717 B | 101 | Automation / QA Script | Active |
| `qa` | `qa/live_ui/login2.ps1` | `login2.ps1` | 2,148 B | 47 | Automation / QA Script | Active |
| `qa` | `qa/live_ui/ocr.ps1` | `ocr.ps1` | 2,400 B | 42 | Automation / QA Script | Active |
| `qa` | `qa/live_ui/pinlogin.ps1` | `pinlogin.ps1` | 2,664 B | 48 | Automation / QA Script | Active |
| `qa` | `qa/live_ui/t.png` | `t.png` | 74,671 B | 0 | Image / Asset | Active |
| `qa` | `qa/live_ui/where.ps1` | `where.ps1` | 1,149 B | 22 | Automation / QA Script | Active |
| `qa` | `qa/live_user_field.png` | `live_user_field.png` | 5,910 B | 0 | Image / Asset | Active |
| `qa` | `qa/login_after.png` | `login_after.png` | 432,745 B | 0 | Image / Asset | Active |
| `qa` | `qa/login_after_s.png` | `login_after_s.png` | 161,007 B | 0 | Image / Asset | Active |
| `qa` | `qa/login_center.png` | `login_center.png` | 43,227 B | 0 | Image / Asset | Active |
| `qa` | `qa/login_fresh.png` | `login_fresh.png` | 430,001 B | 0 | Image / Asset | Active |
| `qa` | `qa/login_fresh2.png` | `login_fresh2.png` | 430,077 B | 0 | Image / Asset | Active |
| `qa` | `qa/login_fresh_s.png` | `login_fresh_s.png` | 124,905 B | 0 | Image / Asset | Active |
| `qa` | `qa/login_full_s.png` | `login_full_s.png` | 156,483 B | 0 | Image / Asset | Active |
| `qa` | `qa/login_full_s2.png` | `login_full_s2.png` | 156,568 B | 0 | Image / Asset | Active |
| `qa` | `qa/login_state.png` | `login_state.png` | 1,291,743 B | 0 | Image / Asset | Active |
| `qa` | `qa/login_state2.png` | `login_state2.png` | 1,997,517 B | 0 | Image / Asset | Active |
| `qa` | `qa/login_state2_s.png` | `login_state2_s.png` | 578,561 B | 0 | Image / Asset | Active |
| `qa` | `qa/login_state_s.png` | `login_state_s.png` | 358,836 B | 0 | Image / Asset | Active |
| `qa` | `qa/login_uia.ps1` | `login_uia.ps1` | 1,233 B | 19 | Automation / QA Script | Active |
| `qa` | `qa/manual_login.ps1` | `manual_login.ps1` | 1,325 B | 25 | Automation / QA Script | Active |
| `qa` | `qa/ocr.ps1` | `ocr.ps1` | 1,690 B | 28 | Automation / QA Script | Active |
| `qa` | `qa/ocr2.ps1` | `ocr2.ps1` | 1,610 B | 30 | Automation / QA Script | Active |
| `qa` | `qa/ocr_all.ps1` | `ocr_all.ps1` | 2,064 B | 34 | Automation / QA Script | Active |
| `qa` | `qa/pid.txt` | `pid.txt` | 5 B | 1 | Documentation / Report / Diagnostic | Active |
| `qa` | `qa/pmclick.ps1` | `pmclick.ps1` | 735 B | 8 | Automation / QA Script | Active |
| `qa` | `qa/qa_01_startup_pos.png` | `qa_01_startup_pos.png` | 950,044 B | 0 | Image / Asset | Active |
| `qa` | `qa/qa_02_recall_held.png` | `qa_02_recall_held.png` | 32,761 B | 0 | Image / Asset | Active |
| `qa` | `qa/qa_03_recall_closed.png` | `qa_03_recall_closed.png` | 73,917 B | 0 | Image / Asset | Active |
| `qa` | `qa/qa_04_recall_voided.png` | `qa_04_recall_voided.png` | 113,993 B | 0 | Image / Asset | Active |
| `qa` | `qa/qa_05_recall_held_again.png` | `qa_05_recall_held_again.png` | 101,940 B | 0 | Image / Asset | Active |
| `qa` | `qa/qa_activeorders_collapsed.png` | `qa_activeorders_collapsed.png` | 1,087,682 B | 0 | Image / Asset | Active |
| `qa` | `qa/qa_activeorders_expanded.png` | `qa_activeorders_expanded.png` | 1,194,391 B | 0 | Image / Asset | Active |
| `qa` | `qa/qa_ao_ci_crop.png` | `qa_ao_ci_crop.png` | 29,361 B | 0 | Image / Asset | Active |
| `qa` | `qa/qa_ao_collapsed.png` | `qa_ao_collapsed.png` | 482,260 B | 0 | Image / Asset | Active |
| `qa` | `qa/qa_ao_collapsed_after_interaction.png` | `qa_ao_collapsed_after_interaction.png` | 1,088,558 B | 0 | Image / Asset | Active |
| `qa` | `qa/qa_ao_collapsed_crop.png` | `qa_ao_collapsed_crop.png` | 340,577 B | 0 | Image / Asset | Active |
| `qa` | `qa/qa_ao_eai_crop.png` | `qa_ao_eai_crop.png` | 36,170 B | 0 | Image / Asset | Active |
| `qa` | `qa/qa_ao_exp_after_interaction.png` | `qa_ao_exp_after_interaction.png` | 1,194,415 B | 0 | Image / Asset | Active |
| `qa` | `qa/qa_ao_exp_crop.png` | `qa_ao_exp_crop.png` | 45,176 B | 0 | Image / Asset | Active |
| `qa` | `qa/qa_bottom_strip.png` | `qa_bottom_strip.png` | 139,130 B | 0 | Image / Asset | Active |
| `qa` | `qa/qa_category_arrows.png` | `qa_category_arrows.png` | 29,847 B | 0 | Image / Asset | Active |
| `qa` | `qa/qa_final_1366x768.png` | `qa_final_1366x768.png` | 398,488 B | 0 | Image / Asset | Active |
| `qa` | `qa/qa_final_active_orders.png` | `qa_final_active_orders.png` | 1,188,442 B | 0 | Image / Asset | Active |
| `qa` | `qa/qa_final_ao_crop.png` | `qa_final_ao_crop.png` | 146,224 B | 0 | Image / Asset | Active |
| `qa` | `qa/qa_final_cart_7_items.png` | `qa_final_cart_7_items.png` | 44,079 B | 0 | Image / Asset | Active |
| `qa` | `qa/qa_final_grid.png` | `qa_final_grid.png` | 1,188,442 B | 0 | Image / Asset | Active |
| `qa` | `qa/qa_final_grid_crop.png` | `qa_final_grid_crop.png` | 536,306 B | 0 | Image / Asset | Active |
| `qa` | `qa/qa_final_header.png` | `qa_final_header.png` | 22,092 B | 0 | Image / Asset | Active |
| `qa` | `qa/qa_final_list.png` | `qa_final_list.png` | 524,377 B | 0 | Image / Asset | Active |
| `qa` | `qa/qa_final_list_full.png` | `qa_final_list_full.png` | 1,188,442 B | 0 | Image / Asset | Active |
| `qa` | `qa/qa_final_max_header.png` | `qa_final_max_header.png` | 10,760 B | 0 | Image / Asset | Active |
| `qa` | `qa/qa_final_maximized.png` | `qa_final_maximized.png` | 887,688 B | 0 | Image / Asset | Active |
| `qa` | `qa/qa_final_restore_header.png` | `qa_final_restore_header.png` | 11,509 B | 0 | Image / Asset | Active |
| `qa` | `qa/qa_final_restored.png` | `qa_final_restored.png` | 1,197,821 B | 0 | Image / Asset | Active |
| `qa` | `qa/qa_final_right_crop.png` | `qa_final_right_crop.png` | 199,737 B | 0 | Image / Asset | Active |
| `qa` | `qa/qa_final_right_panel.png` | `qa_final_right_panel.png` | 1,188,208 B | 0 | Image / Asset | Active |
| `qa` | `qa/qa_fresh_full.png` | `qa_fresh_full.png` | 963,479 B | 0 | Image / Asset | Active |
| `qa` | `qa/qa_fresh_full_s.png` | `qa_fresh_full_s.png` | 434,880 B | 0 | Image / Asset | Active |
| `qa` | `qa/qa_fresh_left.png` | `qa_fresh_left.png` | 580,016 B | 0 | Image / Asset | Active |
| `qa` | `qa/qa_fresh_right.png` | `qa_fresh_right.png` | 350,274 B | 0 | Image / Asset | Active |
| `qa` | `qa/qa_gridview_try1.png` | `qa_gridview_try1.png` | 974,684 B | 0 | Image / Asset | Active |
| `qa` | `qa/qa_gv_crop.png` | `qa_gv_crop.png` | 384,347 B | 0 | Image / Asset | Active |
| `qa` | `qa/qa_header_crop.png` | `qa_header_crop.png` | 30,655 B | 0 | Image / Asset | Active |
| `qa` | `qa/qa_header_zoom.png` | `qa_header_zoom.png` | 12,639 B | 0 | Image / Asset | Active |
| `qa` | `qa/qa_listview.png` | `qa_listview.png` | 1,193,236 B | 0 | Image / Asset | Active |
| `qa` | `qa/qa_lv_crop.png` | `qa_lv_crop.png` | 488,896 B | 0 | Image / Asset | Active |
| `qa` | `qa/qa_maxB_header.png` | `qa_maxB_header.png` | 11,430 B | 0 | Image / Asset | Active |
| `qa` | `qa/qa_narrow_1040.png` | `qa_narrow_1040.png` | 1,097,855 B | 0 | Image / Asset | Active |
| `qa` | `qa/qa_narrow_header.png` | `qa_narrow_header.png` | 22,489 B | 0 | Image / Asset | Active |
| `qa` | `qa/qa_now.png` | `qa_now.png` | 1,193,297 B | 0 | Image / Asset | Active |
| `qa` | `qa/qa_open_tab.png` | `qa_open_tab.png` | 172,824 B | 0 | Image / Asset | Active |
| `qa` | `qa/qa_open_tab_selected.png` | `qa_open_tab_selected.png` | 172,824 B | 0 | Image / Asset | Active |
| `qa` | `qa/qa_orderline_arrows.png` | `qa_orderline_arrows.png` | 11,744 B | 0 | Image / Asset | Active |
| `qa` | `qa/qa_pay_crop.png` | `qa_pay_crop.png` | 29,188 B | 0 | Image / Asset | Active |
| `qa` | `qa/qa_payment_interaction.png` | `qa_payment_interaction.png` | 1,190,386 B | 0 | Image / Asset | Active |
| `qa` | `qa/qa_persist2.png` | `qa_persist2.png` | 437,257 B | 0 | Image / Asset | Active |
| `qa` | `qa/qa_persist_collapsed.png` | `qa_persist_collapsed.png` | 401,670 B | 0 | Image / Asset | Active |
| `qa` | `qa/qa_pos.ps1` | `qa_pos.ps1` | 3,545 B | 80 | Automation / QA Script | Active |
| `qa` | `qa/qa_pos_1366_baseline.png` | `qa_pos_1366_baseline.png` | 1,193,541 B | 0 | Image / Asset | Active |
| `qa` | `qa/qa_pos_iteration1_1366.png` | `qa_pos_iteration1_1366.png` | 869,111 B | 0 | Image / Asset | Active |
| `qa` | `qa/qa_pos_latest_1366.png` | `qa_pos_latest_1366.png` | 382,145 B | 0 | Image / Asset | Active |
| `qa` | `qa/qa_pos_right_rail.png` | `qa_pos_right_rail.png` | 12,393 B | 0 | Image / Asset | Active |
| `qa` | `qa/qa_product_scroll.png` | `qa_product_scroll.png` | 834,307 B | 0 | Image / Asset | Active |
| `qa` | `qa/qa_record_split.png` | `qa_record_split.png` | 57,684 B | 0 | Image / Asset | Active |
| `qa` | `qa/qa_resize_A_1366.png` | `qa_resize_A_1366.png` | 1,190,224 B | 0 | Image / Asset | Active |
| `qa` | `qa/qa_resize_B_maximized.png` | `qa_resize_B_maximized.png` | 880,617 B | 0 | Image / Asset | Active |
| `qa` | `qa/qa_resize_C_restored.png` | `qa_resize_C_restored.png` | 1,190,224 B | 0 | Image / Asset | Active |
| `qa` | `qa/qa_restoreC_header.png` | `qa_restoreC_header.png` | 12,329 B | 0 | Image / Asset | Active |
| `qa` | `qa/qa_right_panel.png` | `qa_right_panel.png` | 258,903 B | 0 | Image / Asset | Active |
| `qa` | `qa/qa_search_murgh.png` | `qa_search_murgh.png` | 1,193,850 B | 0 | Image / Asset | Active |
| `qa` | `qa/qa_ta_full.png` | `qa_ta_full.png` | 696,760 B | 0 | Image / Asset | Active |
| `qa` | `qa/qa_ta_header.png` | `qa_ta_header.png` | 19,885 B | 0 | Image / Asset | Active |
| `qa` | `qa/qa_takeaway_click.png` | `qa_takeaway_click.png` | 1,193,592 B | 0 | Image / Asset | Active |
| `qa` | `qa/qa_tmp_full.png` | `qa_tmp_full.png` | 1,193,541 B | 0 | Image / Asset | Active |
| `qa` | `qa/qa_v2_full.png` | `qa_v2_full.png` | 944,013 B | 0 | Image / Asset | Active |
| `qa` | `qa/qa_v3_full.png` | `qa_v3_full.png` | 454,124 B | 0 | Image / Asset | Active |
| `qa` | `qa/qa_v3_right.png` | `qa_v3_right.png` | 130,407 B | 0 | Image / Asset | Active |
| `qa` | `qa/qa_v3_right_s.png` | `qa_v3_right_s.png` | 349,900 B | 0 | Image / Asset | Active |
| `qa` | `qa/qa_v4_full.png` | `qa_v4_full.png` | 460,246 B | 0 | Image / Asset | Active |
| `qa` | `qa/r1_full.png` | `r1_full.png` | 1,033,689 B | 0 | Image / Asset | Active |
| `qa` | `qa/r1_right.png` | `r1_right.png` | 54,000 B | 0 | Image / Asset | Active |
| `qa` | `qa/r1_small.png` | `r1_small.png` | 501,915 B | 0 | Image / Asset | Active |
| `qa` | `qa/r1_win.png` | `r1_win.png` | 141,127 B | 0 | Image / Asset | Active |
| `qa` | `qa/r2_full.png` | `r2_full.png` | 1,009,859 B | 0 | Image / Asset | Active |
| `qa` | `qa/r2_right.png` | `r2_right.png` | 425,836 B | 0 | Image / Asset | Active |
| `qa` | `qa/r3_right.png` | `r3_right.png` | 244,327 B | 0 | Image / Asset | Active |
| `qa` | `qa/real2_1024_held.png` | `real2_1024_held.png` | 111,015 B | 0 | Image / Asset | Active |
| `qa` | `qa/real2_1080_held.png` | `real2_1080_held.png` | 234,947 B | 0 | Image / Asset | Active |
| `qa` | `qa/real2_1366_held.png` | `real2_1366_held.png` | 150,404 B | 0 | Image / Asset | Active |
| `qa` | `qa/real_1080_held.png` | `real_1080_held.png` | 181,814 B | 0 | Image / Asset | Active |
| `qa` | `qa/real_4k250_held.png` | `real_4k250_held.png` | 742,270 B | 0 | Image / Asset | Active |
| `qa` | `qa/recall_runtime_diagnostics.txt` | `recall_runtime_diagnostics.txt` | 324,697 B | 8004 | Documentation / Report / Diagnostic | Active |
| `qa` | `qa/rev_full.png` | `rev_full.png` | 308,026 B | 0 | Image / Asset | Active |
| `qa` | `qa/run_qa_flow.ps1` | `run_qa_flow.ps1` | 10,150 B | 241 | Automation / QA Script | Active |
| `qa` | `qa/run_recall_qa.ps1` | `run_recall_qa.ps1` | 8,509 B | 239 | Automation / QA Script | Active |
| `qa` | `qa/runtime_1024_held.png` | `runtime_1024_held.png` | 32,761 B | 0 | Image / Asset | Active |
| `qa` | `qa/runtime_1024_pos_composite.png` | `runtime_1024_pos_composite.png` | 37,895 B | 0 | Image / Asset | Active |
| `qa` | `qa/runtime_1366_held.png` | `runtime_1366_held.png` | 35,205 B | 0 | Image / Asset | Active |
| `qa` | `qa/runtime_1366_pos_composite.png` | `runtime_1366_pos_composite.png` | 42,996 B | 0 | Image / Asset | Active |
| `qa` | `qa/runtime_1920_closed.png` | `runtime_1920_closed.png` | 32,938 B | 0 | Image / Asset | Active |
| `qa` | `qa/runtime_1920_held.png` | `runtime_1920_held.png` | 36,220 B | 0 | Image / Asset | Active |
| `qa` | `qa/runtime_1920_open.png` | `runtime_1920_open.png` | 17,729 B | 0 | Image / Asset | Active |
| `qa` | `qa/runtime_1920_pos_composite.png` | `runtime_1920_pos_composite.png` | 53,341 B | 0 | Image / Asset | Active |
| `qa` | `qa/runtime_1920_voided.png` | `runtime_1920_voided.png` | 32,363 B | 0 | Image / Asset | Active |
| `qa` | `qa/runtime_empty_state.png` | `runtime_empty_state.png` | 17,390 B | 0 | Image / Asset | Active |
| `qa` | `qa/runtime_search_customer.png` | `runtime_search_customer.png` | 29,336 B | 0 | Image / Asset | Active |
| `qa` | `qa/screen_info.ps1` | `screen_info.ps1` | 386 B | 9 | Automation / QA Script | Active |
| `qa` | `qa/setres.ps1` | `setres.ps1` | 1,895 B | 33 | Automation / QA Script | Active |
| `qa` | `qa/shot_closed.png` | `shot_closed.png` | 1,133,652 B | 0 | Image / Asset | Active |
| `qa` | `qa/shot_closed_crop.png` | `shot_closed_crop.png` | 50,677 B | 0 | Image / Asset | Active |
| `qa` | `qa/shot_voided.png` | `shot_voided.png` | 1,150,377 B | 0 | Image / Asset | Active |
| `qa` | `qa/shot_voided_crop.png` | `shot_voided_crop.png` | 40,920 B | 0 | Image / Asset | Active |
| `qa` | `qa/shots/T2_current.png` | `T2_current.png` | 740,931 B | 0 | Image / Asset | Active |
| `qa` | `qa/shots/T2_step10.png` | `T2_step10.png` | 6,996,068 B | 0 | Image / Asset | Active |
| `qa` | `qa/shots/T2_step11.png` | `T2_step11.png` | 6,996,193 B | 0 | Image / Asset | Active |
| `qa` | `qa/shots/T2_step2.png` | `T2_step2.png` | 740,919 B | 0 | Image / Asset | Active |
| `qa` | `qa/shots/T2_step3.png` | `T2_step3.png` | 742,029 B | 0 | Image / Asset | Active |
| `qa` | `qa/shots/T2_step4.png` | `T2_step4.png` | 742,041 B | 0 | Image / Asset | Active |
| `qa` | `qa/shots/T2_step5.png` | `T2_step5.png` | 742,041 B | 0 | Image / Asset | Active |
| `qa` | `qa/shots/T2_step6.png` | `T2_step6.png` | 976,294 B | 0 | Image / Asset | Active |
| `qa` | `qa/shots/T2_step6_half.png` | `T2_step6_half.png` | 364,900 B | 0 | Image / Asset | Active |
| `qa` | `qa/shots/T2_step7.png` | `T2_step7.png` | 975,983 B | 0 | Image / Asset | Active |
| `qa` | `qa/shots/T2_step8.png` | `T2_step8.png` | 7,225,225 B | 0 | Image / Asset | Active |
| `qa` | `qa/shots/T2_step9.png` | `T2_step9.png` | 6,996,193 B | 0 | Image / Asset | Active |
| `qa` | `qa/shots/crop_afternotes.png` | `crop_afternotes.png` | 23,847 B | 0 | Image / Asset | Active |
| `qa` | `qa/shots/crop_cart.png` | `crop_cart.png` | 135,336 B | 0 | Image / Asset | Active |
| `qa` | `qa/shots/crop_cartpanel2.png` | `crop_cartpanel2.png` | 2,468,757 B | 0 | Image / Asset | Active |
| `qa` | `qa/shots/crop_dialog.png` | `crop_dialog.png` | 260,377 B | 0 | Image / Asset | Active |
| `qa` | `qa/shots/crop_dlg_top.png` | `crop_dlg_top.png` | 85,901 B | 0 | Image / Asset | Active |
| `qa` | `qa/shots/crop_leftpanel.png` | `crop_leftpanel.png` | 48,798 B | 0 | Image / Asset | Active |
| `qa` | `qa/shots/crop_line.png` | `crop_line.png` | 19,278 B | 0 | Image / Asset | Active |
| `qa` | `qa/shots/crop_line_2x.png` | `crop_line_2x.png` | 57,925 B | 0 | Image / Asset | Active |
| `qa` | `qa/shots/crop_noteclick.png` | `crop_noteclick.png` | 1,158,703 B | 0 | Image / Asset | Active |
| `qa` | `qa/shots/crop_open.png` | `crop_open.png` | 50,596 B | 0 | Image / Asset | Active |
| `qa` | `qa/shots/crop_orders.png` | `crop_orders.png` | 72,329 B | 0 | Image / Asset | Active |
| `qa` | `qa/shots/crop_search_result.png` | `crop_search_result.png` | 52,139 B | 0 | Image / Asset | Active |
| `qa` | `qa/tableswitch_01_t01.png` | `tableswitch_01_t01.png` | 49,126 B | 0 | Image / Asset | Active |
| `qa` | `qa/test_launch.ps1` | `test_launch.ps1` | 527 B | 9 | Automation / QA Script | Active |
| `qa` | `qa/test_modal.ps1` | `test_modal.ps1` | 1,636 B | 41 | Automation / QA Script | Active |
| `qa` | `qa/test_recall_msg.ps1` | `test_recall_msg.ps1` | 3,006 B | 67 | Automation / QA Script | Active |
| `qa` | `qa/test_screen.png` | `test_screen.png` | 35,902 B | 0 | Image / Asset | Active |
| `qa` | `qa/test_write.ps1` | `test_write.ps1` | 27 B | 1 | Automation / QA Script | Active |
| `qa` | `qa/tmp.jpg` | `tmp.jpg` | 20,852 B | 0 | Image / Asset | Active |
| `qa` | `qa/tmp2.jpg` | `tmp2.jpg` | 20,852 B | 0 | Image / Asset | Active |
| `qa` | `qa/ts_full.png` | `ts_full.png` | 5,519,456 B | 0 | Image / Asset | Active |
| `qa` | `qa/ts_full_small.png` | `ts_full_small.png` | 1,187,251 B | 0 | Image / Asset | Active |
| `qa` | `qa/ts_now.png` | `ts_now.png` | 1,887,282 B | 0 | Image / Asset | Active |
| `qa` | `qa/ts_now_small.png` | `ts_now_small.png` | 2,363 B | 0 | Image / Asset | Active |
| `qa` | `qa/ts_screen.png` | `ts_screen.png` | 1,190,917 B | 0 | Image / Asset | Active |
| `qa` | `qa/ts_screen2.png` | `ts_screen2.png` | 5,631,743 B | 0 | Image / Asset | Active |
| `qa` | `qa/ts_small.png` | `ts_small.png` | 514,035 B | 0 | Image / Asset | Active |
| `qa` | `qa/type.ps1` | `type.ps1` | 935 B | 14 | Automation / QA Script | Active |
| `qa` | `qa/uia_add.txt` | `uia_add.txt` | 40,104 B | 696 | Documentation / Report / Diagnostic | Active |
| `qa` | `qa/uia_final.txt` | `uia_final.txt` | 39,699 B | 689 | Documentation / Report / Diagnostic | Active |
| `qa` | `qa/uia_qa1.txt` | `uia_qa1.txt` | 17,648 B | 319 | Documentation / Report / Diagnostic | Active |
| `qa` | `qa/uia_qa10.txt` | `uia_qa10.txt` | 19,928 B | 362 | Documentation / Report / Diagnostic | Active |
| `qa` | `qa/uia_qa11.txt` | `uia_qa11.txt` | 20,622 B | 374 | Documentation / Report / Diagnostic | Active |
| `qa` | `qa/uia_qa12.txt` | `uia_qa12.txt` | 21,015 B | 381 | Documentation / Report / Diagnostic | Active |
| `qa` | `qa/uia_qa13.txt` | `uia_qa13.txt` | 20,678 B | 375 | Documentation / Report / Diagnostic | Active |
| `qa` | `qa/uia_qa2.txt` | `uia_qa2.txt` | 19,625 B | 354 | Documentation / Report / Diagnostic | Active |
| `qa` | `qa/uia_qa3.txt` | `uia_qa3.txt` | 17,648 B | 319 | Documentation / Report / Diagnostic | Active |
| `qa` | `qa/uia_qa4.txt` | `uia_qa4.txt` | 18,307 B | 332 | Documentation / Report / Diagnostic | Active |
| `qa` | `qa/uia_qa5.txt` | `uia_qa5.txt` | 17,712 B | 320 | Documentation / Report / Diagnostic | Active |
| `qa` | `qa/uia_qa6.txt` | `uia_qa6.txt` | 17,712 B | 320 | Documentation / Report / Diagnostic | Active |
| `qa` | `qa/uia_qa7.txt` | `uia_qa7.txt` | 19,652 B | 355 | Documentation / Report / Diagnostic | Active |
| `qa` | `qa/uia_qa8.txt` | `uia_qa8.txt` | 19,652 B | 355 | Documentation / Report / Diagnostic | Active |
| `qa` | `qa/uia_qa9.txt` | `uia_qa9.txt` | 19,929 B | 362 | Documentation / Report / Diagnostic | Active |
| `qa` | `qa/uia_restore.txt` | `uia_restore.txt` | 40,498 B | 703 | Documentation / Report / Diagnostic | Active |
| `qa` | `qa/uia_tree.txt` | `uia_tree.txt` | 40,433 B | 702 | Documentation / Report / Diagnostic | Active |
| `qa` | `qa/uia_tree10.txt` | `uia_tree10.txt` | 40,423 B | 702 | Documentation / Report / Diagnostic | Active |
| `qa` | `qa/uia_tree2.txt` | `uia_tree2.txt` | 40,435 B | 702 | Documentation / Report / Diagnostic | Active |
| `qa` | `qa/uia_tree3.txt` | `uia_tree3.txt` | 40,435 B | 702 | Documentation / Report / Diagnostic | Active |
| `qa` | `qa/uia_tree4.txt` | `uia_tree4.txt` | 40,436 B | 702 | Documentation / Report / Diagnostic | Active |
| `qa` | `qa/uia_tree5.txt` | `uia_tree5.txt` | 40,435 B | 702 | Documentation / Report / Diagnostic | Active |
| `qa` | `qa/uia_tree6.txt` | `uia_tree6.txt` | 40,044 B | 695 | Documentation / Report / Diagnostic | Active |
| `qa` | `qa/uia_tree7.txt` | `uia_tree7.txt` | 40,432 B | 702 | Documentation / Report / Diagnostic | Active |
| `qa` | `qa/uia_tree8.txt` | `uia_tree8.txt` | 40,444 B | 702 | Documentation / Report / Diagnostic | Active |
| `qa` | `qa/uia_tree9.txt` | `uia_tree9.txt` | 35,875 B | 616 | Documentation / Report / Diagnostic | Active |
| `qa` | `qa/uia_utf8.txt` | `uia_utf8.txt` | 40,433 B | 702 | Documentation / Report / Diagnostic | Active |
| `qa` | `qa/uia_v2_1024.txt` | `uia_v2_1024.txt` | 17,680 B | 328 | Documentation / Report / Diagnostic | Active |
| `qa` | `qa/uia_v2_1024c.txt` | `uia_v2_1024c.txt` | 17,966 B | 336 | Documentation / Report / Diagnostic | Active |
| `qa` | `qa/uia_v2_1080.txt` | `uia_v2_1080.txt` | 19,533 B | 359 | Documentation / Report / Diagnostic | Active |
| `qa` | `qa/uia_v2_1366.txt` | `uia_v2_1366.txt` | 19,340 B | 359 | Documentation / Report / Diagnostic | Active |
| `qa` | `qa/uia_v2_4k.txt` | `uia_v2_4k.txt` | 19,974 B | 360 | Documentation / Report / Diagnostic | Active |
| `qa` | `qa/uia_v3_4k250.txt` | `uia_v3_4k250.txt` | 19,833 B | 357 | Documentation / Report / Diagnostic | Active |
| `qa` | `qa/uia_v3_cancel.txt` | `uia_v3_cancel.txt` | 19,012 B | 344 | Documentation / Report / Diagnostic | Active |
| `qa` | `qa/v3_btn_dinein.png` | `v3_btn_dinein.png` | 4,122 B | 0 | Image / Asset | Active |
| `qa` | `qa/v3_close_stuck.ps1` | `v3_close_stuck.ps1` | 424 B | 8 | Automation / QA Script | Active |
| `qa` | `qa/v3_common.ps1` | `v3_common.ps1` | 6,954 B | 142 | Automation / QA Script | Active |
| `qa` | `qa/v3_crop.ps1` | `v3_crop.ps1` | 549 B | 10 | Automation / QA Script | Active |
| `qa` | `qa/v3_diag2_postmsg.ps1` | `v3_diag2_postmsg.ps1` | 3,518 B | 74 | Automation / QA Script | Active |
| `qa` | `qa/v3_diag3_enabled.ps1` | `v3_diag3_enabled.ps1` | 1,260 B | 18 | Automation / QA Script | Active |
| `qa` | `qa/v3_diag4_enumwin.ps1` | `v3_diag4_enumwin.ps1` | 2,290 B | 40 | Automation / QA Script | Active |
| `qa` | `qa/v3_diag5_invoke.ps1` | `v3_diag5_invoke.ps1` | 2,149 B | 39 | Automation / QA Script | Active |
| `qa` | `qa/v3_diag6_takeaway.ps1` | `v3_diag6_takeaway.ps1` | 2,665 B | 59 | Automation / QA Script | Active |
| `qa` | `qa/v3_diag_dinein.ps1` | `v3_diag_dinein.ps1` | 3,222 B | 69 | Automation / QA Script | Active |
| `qa` | `qa/v3_dismiss_modals.ps1` | `v3_dismiss_modals.ps1` | 4,116 B | 82 | Automation / QA Script | Active |
| `qa` | `qa/v3_final_1024x768_active.png` | `v3_final_1024x768_active.png` | 547,521 B | 0 | Image / Asset | Active |
| `qa` | `qa/v3_final_1366x768_active.png` | `v3_final_1366x768_active.png` | 635,116 B | 0 | Image / Asset | Active |
| `qa` | `qa/v3_final_1920x1080_active.png` | `v3_final_1920x1080_active.png` | 1,045,672 B | 0 | Image / Asset | Active |
| `qa` | `qa/v3_final_4k250_maximized.png` | `v3_final_4k250_maximized.png` | 943,668 B | 0 | Image / Asset | Active |
| `qa` | `qa/v3_inv1.sql` | `v3_inv1.sql` | 315 B | 5 | Database / SQL | Active |
| `qa` | `qa/v3_inv2.sql` | `v3_inv2.sql` | 1,060 B | 27 | Database / SQL | Active |
| `qa` | `qa/v3_inv_afterhold.png` | `v3_inv_afterhold.png` | 948,087 B | 0 | Image / Asset | Active |
| `qa` | `qa/v3_inv_afterrecall.png` | `v3_inv_afterrecall.png` | 954,226 B | 0 | Image / Asset | Active |
| `qa` | `qa/v3_inv_afterrecall_uia.txt` | `v3_inv_afterrecall_uia.txt` | 5,371 B | 186 | Documentation / Report / Diagnostic | Active |
| `qa` | `qa/v3_inv_closed_grid.png` | `v3_inv_closed_grid.png` | 49,333 B | 0 | Image / Asset | Active |
| `qa` | `qa/v3_inv_hold_uia.txt` | `v3_inv_hold_uia.txt` | 5,371 B | 186 | Documentation / Report / Diagnostic | Active |
| `qa` | `qa/v3_inv_phase1_orders.ps1` | `v3_inv_phase1_orders.ps1` | 1,436 B | 42 | Automation / QA Script | Active |
| `qa` | `qa/v3_inv_phase2_recall.ps1` | `v3_inv_phase2_recall.ps1` | 3,128 B | 75 | Automation / QA Script | Active |
| `qa` | `qa/v3_inv_phase3_regression.ps1` | `v3_inv_phase3_regression.ps1` | 3,045 B | 72 | Automation / QA Script | Active |
| `qa` | `qa/v3_inv_phase4_hold.ps1` | `v3_inv_phase4_hold.ps1` | 2,744 B | 74 | Automation / QA Script | Active |
| `qa` | `qa/v3_inv_readonly_viewer.png` | `v3_inv_readonly_viewer.png` | 24,973 B | 0 | Image / Asset | Active |
| `qa` | `qa/v3_inv_recall_closed.png` | `v3_inv_recall_closed.png` | 135,308 B | 0 | Image / Asset | Active |
| `qa` | `qa/v3_inv_recall_ord95.png` | `v3_inv_recall_ord95.png` | 107,587 B | 0 | Image / Asset | Active |
| `qa` | `qa/v3_inv_reg_held.png` | `v3_inv_reg_held.png` | 107,405 B | 0 | Image / Asset | Active |
| `qa` | `qa/v3_inv_reg_open.png` | `v3_inv_reg_open.png` | 108,828 B | 0 | Image / Asset | Active |
| `qa` | `qa/v3_inv_reg_premeasure_uia.txt` | `v3_inv_reg_premeasure_uia.txt` | 6,208 B | 207 | Documentation / Report / Diagnostic | Active |
| `qa` | `qa/v3_inv_reg_voided.png` | `v3_inv_reg_voided.png` | 108,828 B | 0 | Image / Asset | Active |
| `qa` | `qa/v3_inv_two_orders_done.png` | `v3_inv_two_orders_done.png` | 950,049 B | 0 | Image / Asset | Active |
| `qa` | `qa/v3_kill_modal.ps1` | `v3_kill_modal.ps1` | 788 B | 18 | Automation / QA Script | Active |
| `qa` | `qa/v3_modal_now.png` | `v3_modal_now.png` | 785,708 B | 0 | Image / Asset | Active |
| `qa` | `qa/v3_now_topleft.png` | `v3_now_topleft.png` | 29,978 B | 0 | Image / Asset | Active |
| `qa` | `qa/v3_phase10_dinein.ps1` | `v3_phase10_dinein.ps1` | 1,489 B | 43 | Automation / QA Script | Active |
| `qa` | `qa/v3_phase10b_table.ps1` | `v3_phase10b_table.ps1` | 1,726 B | 40 | Automation / QA Script | Active |
| `qa` | `qa/v3_phase11_raildd.ps1` | `v3_phase11_raildd.ps1` | 1,129 B | 29 | Automation / QA Script | Active |
| `qa` | `qa/v3_phase12_finalsizes.ps1` | `v3_phase12_finalsizes.ps1` | 1,368 B | 34 | Automation / QA Script | Active |
| `qa` | `qa/v3_phase1_sizes.ps1` | `v3_phase1_sizes.ps1` | 6,426 B | 146 | Automation / QA Script | Active |
| `qa` | `qa/v3_phase2_workflow.ps1` | `v3_phase2_workflow.ps1` | 7,968 B | 184 | Automation / QA Script | Active |
| `qa` | `qa/v3_phase2b_workflow.ps1` | `v3_phase2b_workflow.ps1` | 9,315 B | 217 | Automation / QA Script | Active |
| `qa` | `qa/v3_phase2c_workflow.ps1` | `v3_phase2c_workflow.ps1` | 7,990 B | 187 | Automation / QA Script | Active |
| `qa` | `qa/v3_phase3_items.ps1` | `v3_phase3_items.ps1` | 848 B | 26 | Automation / QA Script | Active |
| `qa` | `qa/v3_phase4_hold_recall.ps1` | `v3_phase4_hold_recall.ps1` | 2,097 B | 54 | Automation / QA Script | Active |
| `qa` | `qa/v3_phase4b_recall_dump.ps1` | `v3_phase4b_recall_dump.ps1` | 1,578 B | 34 | Automation / QA Script | Active |
| `qa` | `qa/v3_phase4c_recalldump2.ps1` | `v3_phase4c_recalldump2.ps1` | 1,584 B | 32 | Automation / QA Script | Active |
| `qa` | `qa/v3_phase5_tabs_search.ps1` | `v3_phase5_tabs_search.ps1` | 3,198 B | 76 | Automation / QA Script | Active |
| `qa` | `qa/v3_phase6_search_recall.ps1` | `v3_phase6_search_recall.ps1` | 3,648 B | 78 | Automation / QA Script | Active |
| `qa` | `qa/v3_phase7_clear_retest.ps1` | `v3_phase7_clear_retest.ps1` | 2,590 B | 64 | Automation / QA Script | Active |
| `qa` | `qa/v3_phase8_cancel.ps1` | `v3_phase8_cancel.ps1` | 4,186 B | 98 | Automation / QA Script | Active |
| `qa` | `qa/v3_phase8b_confirm.ps1` | `v3_phase8b_confirm.ps1` | 1,916 B | 48 | Automation / QA Script | Active |
| `qa` | `qa/v3_phase9_payment.ps1` | `v3_phase9_payment.ps1` | 2,979 B | 74 | Automation / QA Script | Active |
| `qa` | `qa/v3_pid.txt` | `v3_pid.txt` | 6 B | 1 | Documentation / Report / Diagnostic | Active |
| `qa` | `qa/v3_pos_1024_right.png` | `v3_pos_1024_right.png` | 67,737 B | 0 | Image / Asset | Active |
| `qa` | `qa/v3_pos_1024_topbar.png` | `v3_pos_1024_topbar.png` | 34,529 B | 0 | Image / Asset | Active |
| `qa` | `qa/v3_pos_1024x768.png` | `v3_pos_1024x768.png` | 544,746 B | 0 | Image / Asset | Active |
| `qa` | `qa/v3_pos_1366x768.png` | `v3_pos_1366x768.png` | 633,978 B | 0 | Image / Asset | Active |
| `qa` | `qa/v3_pos_1920x1080.png` | `v3_pos_1920x1080.png` | 1,046,181 B | 0 | Image / Asset | Active |
| `qa` | `qa/v3_pos_4k250_maximized.png` | `v3_pos_4k250_maximized.png` | 955,625 B | 0 | Image / Asset | Active |
| `qa` | `qa/v3_pos_final_maximized.png` | `v3_pos_final_maximized.png` | 942,952 B | 0 | Image / Asset | Active |
| `qa` | `qa/v3_screen_now.png` | `v3_screen_now.png` | 18,628,173 B | 0 | Image / Asset | Active |
| `qa` | `qa/v3_screen_now.ps1` | `v3_screen_now.ps1` | 620 B | 12 | Automation / QA Script | Active |
| `qa` | `qa/v3_sql1.sql` | `v3_sql1.sql` | 420 B | 5 | Database / SQL | Active |
| `qa` | `qa/v3_sql2.sql` | `v3_sql2.sql` | 318 B | 4 | Database / SQL | Active |
| `qa` | `qa/v3_sql3.sql` | `v3_sql3.sql` | 776 B | 11 | Database / SQL | Active |
| `qa` | `qa/v3_sql4.sql` | `v3_sql4.sql` | 231 B | 3 | Database / SQL | Active |
| `qa` | `qa/v3_uia_dump.ps1` | `v3_uia_dump.ps1` | 1,247 B | 22 | Automation / QA Script | Active |
| `qa` | `qa/v3_uia_full.txt` | `v3_uia_full.txt` | 14,653 B | 321 | Documentation / Report / Diagnostic | Active |
| `qa` | `qa/v3_uia_pos.txt` | `v3_uia_pos.txt` | 19,680 B | 307 | Documentation / Report / Diagnostic | Active |
| `qa` | `qa/v3_w10_dinein_popup.png` | `v3_w10_dinein_popup.png` | 1,251,499 B | 0 | Image / Asset | Active |
| `qa` | `qa/v3_w10_popup_crop.png` | `v3_w10_popup_crop.png` | 11,789 B | 0 | Image / Asset | Active |
| `qa` | `qa/v3_w10b_dinein_order.png` | `v3_w10b_dinein_order.png` | 943,683 B | 0 | Image / Asset | Active |
| `qa` | `qa/v3_w10b_dinein_uia.txt` | `v3_w10b_dinein_uia.txt` | 5,236 B | 182 | Documentation / Report / Diagnostic | Active |
| `qa` | `qa/v3_w10b_header.png` | `v3_w10b_header.png` | 15,199 B | 0 | Image / Asset | Active |
| `qa` | `qa/v3_w11_dd_crop.png` | `v3_w11_dd_crop.png` | 112,654 B | 0 | Image / Asset | Active |
| `qa` | `qa/v3_w11_rail_dropdown.png` | `v3_w11_rail_dropdown.png` | 943,470 B | 0 | Image / Asset | Active |
| `qa` | `qa/v3_w2_afterhold.png` | `v3_w2_afterhold.png` | 945,314 B | 0 | Image / Asset | Active |
| `qa` | `qa/v3_w2_afterhold_uia.txt` | `v3_w2_afterhold_uia.txt` | 15,190 B | 332 | Documentation / Report / Diagnostic | Active |
| `qa` | `qa/v3_w2_cart.png` | `v3_w2_cart.png` | 945,314 B | 0 | Image / Asset | Active |
| `qa` | `qa/v3_w2_cart_uia.txt` | `v3_w2_cart_uia.txt` | 15,190 B | 332 | Documentation / Report / Diagnostic | Active |
| `qa` | `qa/v3_w2_neworder.png` | `v3_w2_neworder.png` | 942,605 B | 0 | Image / Asset | Active |
| `qa` | `qa/v3_w2_neworder_uia.txt` | `v3_w2_neworder_uia.txt` | 15,042 B | 330 | Documentation / Report / Diagnostic | Active |
| `qa` | `qa/v3_w2_table_selected.png` | `v3_w2_table_selected.png` | 942,605 B | 0 | Image / Asset | Active |
| `qa` | `qa/v3_w2_table_uia.txt` | `v3_w2_table_uia.txt` | 15,042 B | 330 | Documentation / Report / Diagnostic | Active |
| `qa` | `qa/v3_w3_cart5.png` | `v3_w3_cart5.png` | 978,177 B | 0 | Image / Asset | Active |
| `qa` | `qa/v3_w3_cart5_summary.txt` | `v3_w3_cart5_summary.txt` | 5 B | 1 | Documentation / Report / Diagnostic | Active |
| `qa` | `qa/v3_w3_cart5_uia.txt` | `v3_w3_cart5_uia.txt` | 5,881 B | 202 | Documentation / Report / Diagnostic | Active |
| `qa` | `qa/v3_w3_paybtns.png` | `v3_w3_paybtns.png` | 29,206 B | 0 | Image / Asset | Active |
| `qa` | `qa/v3_w3_right.png` | `v3_w3_right.png` | 96,278 B | 0 | Image / Asset | Active |
| `qa` | `qa/v3_w4_afterhold.png` | `v3_w4_afterhold.png` | 955,272 B | 0 | Image / Asset | Active |
| `qa` | `qa/v3_w4_afterhold_uia.txt` | `v3_w4_afterhold_uia.txt` | 5,177 B | 180 | Documentation / Report / Diagnostic | Active |
| `qa` | `qa/v3_w4_recall_held.png` | `v3_w4_recall_held.png` | 24,973 B | 0 | Image / Asset | Active |
| `qa` | `qa/v3_w4_recall_uia.txt` | `v3_w4_recall_uia.txt` | 903 B | 18 | Documentation / Report / Diagnostic | Active |
| `qa` | `qa/v3_w5_open_footer.png` | `v3_w5_open_footer.png` | 409 B | 0 | Image / Asset | Active |
| `qa` | `qa/v3_w5_recall_1140x680.png` | `v3_w5_recall_1140x680.png` | 131,256 B | 0 | Image / Asset | Active |
| `qa` | `qa/v3_w5_recall_950x640.png` | `v3_w5_recall_950x640.png` | 127,421 B | 0 | Image / Asset | Active |
| `qa` | `qa/v3_w5_recall_closed.png` | `v3_w5_recall_closed.png` | 74,038 B | 0 | Image / Asset | Active |
| `qa` | `qa/v3_w5_recall_held_back.png` | `v3_w5_recall_held_back.png` | 135,607 B | 0 | Image / Asset | Active |
| `qa` | `qa/v3_w5_recall_open.png` | `v3_w5_recall_open.png` | 172,193 B | 0 | Image / Asset | Active |
| `qa` | `qa/v3_w5_recall_voided.png` | `v3_w5_recall_voided.png` | 75,165 B | 0 | Image / Asset | Active |
| `qa` | `qa/v3_w5_voided_grid.png` | `v3_w5_voided_grid.png` | 19,543 B | 0 | Image / Asset | Active |
| `qa` | `qa/v3_w6_afterrecall.png` | `v3_w6_afterrecall.png` | 978,371 B | 0 | Image / Asset | Active |
| `qa` | `qa/v3_w6_afterrecall_uia.txt` | `v3_w6_afterrecall_uia.txt` | 5,882 B | 202 | Documentation / Report / Diagnostic | Active |
| `qa` | `qa/v3_w6_search_cleared.png` | `v3_w6_search_cleared.png` | 131,405 B | 0 | Image / Asset | Active |
| `qa` | `qa/v3_w6_search_ord92.png` | `v3_w6_search_ord92.png` | 116,839 B | 0 | Image / Asset | Active |
| `qa` | `qa/v3_w7_afterclear.png` | `v3_w7_afterclear.png` | 955,369 B | 0 | Image / Asset | Active |
| `qa` | `qa/v3_w7_afterclear_uia.txt` | `v3_w7_afterclear_uia.txt` | 5,178 B | 180 | Documentation / Report / Diagnostic | Active |
| `qa` | `qa/v3_w7_afterrecall2.png` | `v3_w7_afterrecall2.png` | 978,525 B | 0 | Image / Asset | Active |
| `qa` | `qa/v3_w7_afterrecall2_uia.txt` | `v3_w7_afterrecall2_uia.txt` | 5,882 B | 202 | Documentation / Report / Diagnostic | Active |
| `qa` | `qa/v3_w7_recall_again.png` | `v3_w7_recall_again.png` | 135,742 B | 0 | Image / Asset | Active |
| `qa` | `qa/v3_w8_cancel_prompt.png` | `v3_w8_cancel_prompt.png` | 1,652 B | 0 | Image / Asset | Active |
| `qa` | `qa/v3_w8_cancelled.png` | `v3_w8_cancelled.png` | 978,219 B | 0 | Image / Asset | Active |
| `qa` | `qa/v3_w8_cancelled_uia.txt` | `v3_w8_cancelled_uia.txt` | 5,882 B | 202 | Documentation / Report / Diagnostic | Active |
| `qa` | `qa/v3_w8_declined_uia.txt` | `v3_w8_declined_uia.txt` | 6,158 B | 210 | Documentation / Report / Diagnostic | Active |
| `qa` | `qa/v3_w8b_cancelled.png` | `v3_w8b_cancelled.png` | 937,102 B | 0 | Image / Asset | Active |
| `qa` | `qa/v3_w8b_cancelled_uia.txt` | `v3_w8b_cancelled_uia.txt` | 5,035 B | 176 | Documentation / Report / Diagnostic | Active |
| `qa` | `qa/v3_w8b_prompt.png` | `v3_w8b_prompt.png` | 1,652 B | 0 | Image / Asset | Active |
| `qa` | `qa/v3_w9_neworder.png` | `v3_w9_neworder.png` | 962,648 B | 0 | Image / Asset | Active |
| `qa` | `qa/v3_w9_paid.png` | `v3_w9_paid.png` | 937,123 B | 0 | Image / Asset | Active |
| `qa` | `qa/v3_w9_paid_uia.txt` | `v3_w9_paid_uia.txt` | 5,035 B | 176 | Documentation / Report / Diagnostic | Active |
| `qa` | `qa/v3_w9_recall_voided_ord92.png` | `v3_w9_recall_voided_ord92.png` | 132,900 B | 0 | Image / Asset | Active |
| `qa` | `qa/v3_w9_tender500.png` | `v3_w9_tender500.png` | 961,517 B | 0 | Image / Asset | Active |
| `qa` | `qa/v3_w9_tender500_uia.txt` | `v3_w9_tender500_uia.txt` | 5,392 B | 186 | Documentation / Report / Diagnostic | Active |
| `qa` | `qa/v3_w9_voided_crop.png` | `v3_w9_voided_crop.png` | 28,718 B | 0 | Image / Asset | Active |
| `qa` | `qa/win32_pos.txt` | `win32_pos.txt` | 30,951 B | 265 | Documentation / Report / Diagnostic | Active |
| `qa` | `qa/win32_t2a.txt` | `win32_t2a.txt` | 30,960 B | 265 | Documentation / Report / Diagnostic | Active |
| `qa` | `qa/win32_t2b.txt` | `win32_t2b.txt` | 31,698 B | 271 | Documentation / Report / Diagnostic | Active |
| `qauia_pos.txt` | `qauia_pos.txt` | `qauia_pos.txt` | 8,192 B | 78 | Documentation / Report / Diagnostic | Active |
| `qauia_pos_tree.txt` | `qauia_pos_tree.txt` | `qauia_pos_tree.txt` | 14,313 B | 263 | Documentation / Report / Diagnostic | Active |
| `qauia_tree.txt` | `qauia_tree.txt` | `qauia_tree.txt` | 40,433 B | 702 | Documentation / Report / Diagnostic | Active |
| `qawin32_pos.txt` | `qawin32_pos.txt` | `qawin32_pos.txt` | 30,951 B | 265 | Documentation / Report / Diagnostic | Active |
| `scratch` | `scratch/DesignerTest/DesignerTest.csproj` | `DesignerTest.csproj` | 425 B | 0 | Configuration / Project Definition | Scratch / Sandbox |
| `scratch` | `scratch/DesignerTest/DesignerTest.csproj.user` | `DesignerTest.csproj.user` | 278 B | 8 | Configuration / Project Definition | Scratch / Sandbox |
| `scratch` | `scratch/DesignerTest/Form1.Designer.cs` | `Form1.Designer.cs` | 1,057 B | 38 | Source Code | Scratch / Sandbox |
| `scratch` | `scratch/DesignerTest/Form1.cs` | `Form1.cs` | 134 B | 9 | Source Code | Scratch / Sandbox |
| `scratch` | `scratch/DesignerTest/Program.cs` | `Program.cs` | 1,309 B | 45 | Source Code | Scratch / Sandbox |
| `scratch` | `scratch/dialog_crop.png` | `dialog_crop.png` | 42,338 B | 0 | Image / Asset | Scratch / Sandbox |
| `scratch` | `scratch/dialog_exact.png` | `dialog_exact.png` | 42,475 B | 0 | Image / Asset | Scratch / Sandbox |
| `scratch` | `scratch/menu_inspection.txt` | `menu_inspection.txt` | 1,957 B | 20 | Documentation / Report / Diagnostic | Scratch / Sandbox |
| `shoot.ps1` | `shoot.ps1` | `shoot.ps1` | 619 B | 12 | Automation / QA Script | Active |
| `shot_login.png` | `shot_login.png` | `shot_login.png` | 121,292 B | 0 | Image / Asset | Active |
| `shot_login2.png` | `shot_login2.png` | `shot_login2.png` | 121,292 B | 0 | Image / Asset | Active |
| `test_screen.png` | `test_screen.png` | `test_screen.png` | 35,902 B | 0 | Image / Asset | Active |
| `type.ps1` | `type.ps1` | `type.ps1` | 135 B | 4 | Automation / QA Script | Active |
| `uia.ps1` | `uia.ps1` | `uia.ps1` | 1,299 B | 23 | Automation / QA Script | Active |
| `uia_dump.ps1` | `uia_dump.ps1` | `uia_dump.ps1` | 1,163 B | 24 | Automation / QA Script | Active |
| `uia_set.ps1` | `uia_set.ps1` | `uia_set.ps1` | 1,283 B | 23 | Automation / QA Script | Active |

---

## 4. Dependency & Reference Map

### 4.1 Architectural Dependency Inversion
The solution strictly respects Clean Architecture principles. Dependencies point inward from Presentation and Infrastructure toward Application and Domain kernels:

```text
Clovent.Desktop (WinForms / DevExpress UI Host)
  ├──► Clovent.Platform (Generic Host, Logging, Diagnostics)
  ├──► Clovent.Authentication.Infrastructure ──► Clovent.Authentication.Application ──► Clovent.Authentication (Domain)
  ├──► Clovent.Identity.Infrastructure       ──► Clovent.Identity.Application       ──► Clovent.Identity (Domain)
  ├──► Clovent.MasterData.Infrastructure     ──► Clovent.MasterData.Application     ──► Clovent.MasterData (Domain)
  ├──► Clovent.Catalog.Infrastructure        ──► Clovent.Catalog.Application        ──► Clovent.Catalog (Domain)
  ├──► Clovent.Inventory.Infrastructure      ──► Clovent.Inventory.Application      ──► Clovent.Inventory (Domain)
  └──► Clovent.Restaurant.Infrastructure     ──► Clovent.Restaurant.Application     ──► Clovent.Restaurant (Domain)
                                                                                          │
                                                                                          ▼
                                                                                    Clovent.Domain (Shared Kernel)
```

### 4.2 Key Third-Party and External Library Dependencies
- **UI & Controls Suite:** `DevExpress.Win` (`v26.1.4-pre-26179`) — Ribbon control, GridControl, custom vector skins, flyout panels, and POS touch layouts.
- **Reporting & Thermal Receipts:** `DevExpress.Reporting.Core` (`v26.1.4-pre-26179`) — Receipt formatting, thermal printing documents, and invoice previews.
- **In-Process CQRS Dispatcher:** `MediatR` (`v12.4.1`) — Decouples UI controllers and command issuers from business handlers across all Application layers.
- **Data Access & Relational ORM:** `Microsoft.EntityFrameworkCore` (`v10.0.10`), `Microsoft.EntityFrameworkCore.SqlServer` (Production), `Microsoft.EntityFrameworkCore.Sqlite` (In-memory testing).
- **Host & Dependency Injection:** `Microsoft.Extensions.Hosting`, `Microsoft.Extensions.DependencyInjection` (`v10.0.10`).
- **Security & Cryptography:** `BCrypt.Net-Next` (`v4.2.0`) — Secure salted hashing for passwords and staff POS PINs.
- **Automated Testing:** `xunit` (`v2.9.3`), `xunit.runner.visualstudio` (`v3.1.4`), `coverlet.collector` (`v6.0.4`).
- **CLI Formatting:** `Spectre.Console` (`v0.57.2`) — Rich terminal UI for developer tooling in `Tools/Clovent.CLI`.

---

## 5. Duplicate / Similar File Detection

The audit performed filename collision checks, extension analysis, and content hash inspections across all non-generated files. The following duplicate and near-duplicate groups were identified:

### 5.1 In-Place Snapshot Backups Beside Active Source Code
Between 2026-08-17 and 2026-08-18, multiple manual backup files were created inside the active `src/Clovent.Desktop/` tree:

| Active File | Duplicate / Backup File | Type | Reason for Duplication |
|---|---|---|---|
| `src/Clovent.Desktop/Restaurant/Orders/RestaurantPosForm.cs` | `...RestaurantPosForm.cs.backup-castfix-20260817-104258`<br/>`...RestaurantPosForm.cs.backup-cleanup-20260817-153334`<br/>`...RestaurantPosForm.cs.backup-parenting-20260817-121628`<br/>`...RestaurantPosForm.cs.backup-paymentfix-20260817-104258`<br/>`...RestaurantPosForm.cs.backup-readiness-20260818`<br/>`...RestaurantPosForm.cs.backup-ui-fixes-20260818` | C# Code | 6 successive historical snapshots of POS form event handlers and payment bindings. All consolidated into active `RestaurantPosForm.cs`. |
| `src/Clovent.Desktop/Restaurant/Orders/RestaurantPosForm.Designer.cs` | `...RestaurantPosForm.Designer.cs.backup-20260817-111140`<br/>`...backup-20260817-113226`<br/>`...backup-barcode-20260817-115714`<br/>`...backup-capturefix-20260817-135631`<br/>`...backup-cleanup-20260817-153334`<br/>`...backup-parenting-20260817-121628`<br/>`...backup-pre-sortfix.bak`<br/>`...backup-readiness-20260818`<br/>`...backup-runtime-test.bak`<br/>`...backup-ui-fixes-20260818` | C# Designer | 10 redundant designer layout snapshots. Active `RestaurantPosForm.Designer.cs` contains the final touch UI layout. |
| `src/Clovent.Desktop/Program.cs` | `src/Clovent.Desktop/Program.cs.backup-orderhistory-20260817-124222` | C# Code | Snapshot taken before Order History service registration. |
| `src/Clovent.Desktop/Forms/Shell/MainForm.Designer.cs` | `...MainForm.Designer.cs.backup-orderhistory-20260817-124222` | C# Designer | Snapshot taken before Order History menu button addition. |
| `src/Clovent.Desktop/Forms/Identity/LoginForm.cs` | `...LoginForm.cs.backup-ui-fixes-20260818` | C# Code | Snapshot taken before login screen styling adjustments. |
| `src/Clovent.Desktop/DependencyInjection/DesktopServiceCollectionExtensions.cs` | `...DesktopServiceCollectionExtensions.cs.backup-orderhistory-20260817-124222` | C# Code | Snapshot taken before order history registration. |
| `src/Clovent.Desktop/Seed/DevelopmentAuthorizationSeedStartupTask.cs` | `...DevelopmentAuthorizationSeedStartupTask.cs.backup-orderhistory-20260817-124222` | C# Code | Snapshot taken before user seed adjustments. |
| `src/Clovent.Authentication.Application/.../ApplicationServiceCollectionExtensions.cs` | `...ApplicationServiceCollectionExtensions.cs.backup-m4m5-20260817-121628` | C# Code | Snapshot taken during Milestone 4/5 development. |

### 5.2 Standalone Backup Directory (`backups/`)
Contains 9 historical `.bak` files:
- `backups/config-security-fix-2026-08-17/appsettings.json.bak`
- `backups/config-security-fix-2026-08-17/Clovent.Desktop.csproj.bak`
- `backups/config-security-fix-2026-08-17/Clovent.Desktop.Tests.csproj.bak`
- `backups/logging-2026-08-17/appsettings.json.bak`
- `backups/logging-2026-08-17/Program.cs.bak`
- `backups/rootcause-2026-08-18/FileLoggerProvider.cs.bak`
- `backups/rootcause-2026-08-18/RestaurantPosForm.cs.bak`
- `backups/rootcause-2026-08-18/RestaurantPosForm.Designer.cs.bak`
- `backups/rootcause-2026-08-18/RestaurantPosForm.Designer.cs.pre-sortfix.bak`

### 5.3 Duplicate Class Stubs across Tool Projects
`Tools/Clovent.CLI/Clovent.CBOS.Desktop` contains 20 class stubs that duplicate classes in `src/Clovent.Desktop` (e.g. `MainForm.cs`, `LoginForm.cs`, `ApplicationBootstrapper.cs`, `AuthenticationService.cs`). All are 0 bytes or 3 bytes (empty files).

---

## 6. Unused / Possibly Unused Files

Static reference analysis was conducted by checking compilation definitions, namespaces, DI container registrations, and cross-project references:

| File Path | Status | Confidence | Reason / Analysis | Runtime Verification Required? |
|---|---|---|---|---|
| `src/Clovent.Desktop/**/*.backup-*` (19 files) | UNUSED (Historical Backup) | **HIGH** | Timestamped snapshot files not compiled by Roslyn. | No |
| `backups/**` (9 files) | UNUSED (External Archive) | **HIGH** | Out-of-tree backup folder from August 2026. | No |
| `cart_diagnostics.txt` (Root) | UNUSED (Temporary Log) | **HIGH** | 182 KB diagnostic dump from 2026-08-17. | No |
| Root test scripts (`click.ps1`, `enum.ps1`, `login.ps1`, etc. - 10 files) | UNUSED BY PRODUCTION | **HIGH** | Ad-hoc PowerShell scripts used for manual testing. | No |
| Root screenshots (`shot_login.png`, `shot_login2.png`, `test_screen.png`) | UNUSED (Test Artifacts) | **HIGH** | Static PNG captures from past QA sessions. | No |
| `scratch/DesignerTest/**` (5 files) | UNUSED (Scratch Sandbox) | **HIGH** | Standalone test project, not part of any solution. | No |
| `Tools/Clovent.CLI/Clovent.CBOS.Desktop/**` (20 files) | UNUSED (Empty Stubs) | **HIGH** | 0-byte/3-byte files in uncompleted prototype project. | No |
| `Tools/Clovent.CLI/Clovent.CLI/Services/**` (7 files) | UNUSED (Empty Stubs) | **HIGH** | 3-byte stub files (`ConsoleService.cs`, `FileService.cs`, etc.). | No |
| `src/Clovent.Desktop.UiQa/**` (2 files) | QA UTILITY ONLY | **MEDIUM** | Standalone UI smoke test runner; not listed in main `.slnx`. | No (Separate runner) |
| `UniversalSearchDropdown.cs` (`src/Clovent.Desktop/...`) | ACTIVELY USED | **LOW** | Embedded custom control in POS form; bound dynamically. | **Yes — Requires runtime verification** |
| `CustomerReorderDialogs.cs` (`src/Clovent.Desktop/...`) | ACTIVELY USED | **LOW** | Customer repeat order flyout; invoked on user click. | **Yes — Requires runtime verification** |
| `RestaurantPulseForm.cs` (`src/Clovent.Desktop/...`) | ACTIVELY USED | **LOW** | Analytics dashboard; launched via navigation ribbon. | **Yes — Requires runtime verification** |

---

## 7. Generated / Build Artifacts

A total of **11,085 files** in the repository represent compiler outputs, IDE caches, or version control databases:

### 7.1 Breakdown of Generated Artifacts by Directory Group

| Generated Directory Group | File Count | Approximate Size | Description |
|---|---|---|---|
| `Tools/Clovent.CLI/[bin/obj]` | **744** | 29.33 MB | Compiled DLLs, PDBs, intermediate obj files, cache files |
| `src/Clovent.Desktop.Tests/[bin/obj]` | **681** | 296.38 MB | Compiled DLLs, PDBs, intermediate obj files, cache files |
| `src/Clovent.Desktop/[bin/obj]` | **635** | 289.96 MB | Compiled DLLs, PDBs, intermediate obj files, cache files |
| `src/Clovent.Restaurant.Infrastructure.Tests/[bin/obj]` | **501** | 127.02 MB | Compiled DLLs, PDBs, intermediate obj files, cache files |
| `src/Clovent.Inventory.Infrastructure.Tests/[bin/obj]` | **471** | 122.18 MB | Compiled DLLs, PDBs, intermediate obj files, cache files |
| `src/Clovent.Catalog.Infrastructure.Tests/[bin/obj]` | **465** | 122.43 MB | Compiled DLLs, PDBs, intermediate obj files, cache files |
| `src/Clovent.Authentication.Infrastructure.Tests/[bin/obj]` | **461** | 121.79 MB | Compiled DLLs, PDBs, intermediate obj files, cache files |
| `src/Clovent.MasterData.Infrastructure.Tests/[bin/obj]` | **459** | 121.73 MB | Compiled DLLs, PDBs, intermediate obj files, cache files |
| `.git` | **455** | 278.23 MB | Compiled DLLs, PDBs, intermediate obj files, cache files |
| `src/Clovent.Identity.Infrastructure.Tests/[bin/obj]` | **453** | 121.41 MB | Compiled DLLs, PDBs, intermediate obj files, cache files |
| `.tmp.driveupload` | **315** | 143.13 MB | Compiled DLLs, PDBs, intermediate obj files, cache files |
| `src/Clovent.Platform.Tests/[bin/obj]` | **279** | 15.23 MB | Compiled DLLs, PDBs, intermediate obj files, cache files |
| `src/Clovent.Restaurant.Application.Tests/[bin/obj]` | **275** | 18.29 MB | Compiled DLLs, PDBs, intermediate obj files, cache files |
| `src/Clovent.Desktop.UiQa/[bin/obj]` | **260** | 141.66 MB | Compiled DLLs, PDBs, intermediate obj files, cache files |
| `scratch/DesignerTest/[bin/obj]` | **257** | 131.35 MB | Compiled DLLs, PDBs, intermediate obj files, cache files |
| `src/Clovent.Inventory.Application.Tests/[bin/obj]` | **257** | 13.23 MB | Compiled DLLs, PDBs, intermediate obj files, cache files |
| `src/Clovent.Catalog.Application.Tests/[bin/obj]` | **251** | 13.39 MB | Compiled DLLs, PDBs, intermediate obj files, cache files |
| `src/Clovent.Authentication.Application.Tests/[bin/obj]` | **249** | 12.77 MB | Compiled DLLs, PDBs, intermediate obj files, cache files |
| `src/Clovent.MasterData.Application.Tests/[bin/obj]` | **245** | 12.70 MB | Compiled DLLs, PDBs, intermediate obj files, cache files |
| `src/Clovent.Inventory.Tests/[bin/obj]` | **241** | 12.17 MB | Compiled DLLs, PDBs, intermediate obj files, cache files |
| `src/Clovent.Restaurant.Tests/[bin/obj]` | **241** | 12.95 MB | Compiled DLLs, PDBs, intermediate obj files, cache files |
| `src/Clovent.Identity.Application.Tests/[bin/obj]` | **239** | 12.46 MB | Compiled DLLs, PDBs, intermediate obj files, cache files |
| `src/Clovent.Catalog.Tests/[bin/obj]` | **235** | 11.96 MB | Compiled DLLs, PDBs, intermediate obj files, cache files |
| `src/Clovent.Authentication.Tests/[bin/obj]` | **229** | 11.46 MB | Compiled DLLs, PDBs, intermediate obj files, cache files |
| `src/Clovent.MasterData.Tests/[bin/obj]` | **229** | 11.46 MB | Compiled DLLs, PDBs, intermediate obj files, cache files |
| `src/Clovent.Identity.Tests/[bin/obj]` | **223** | 11.15 MB | Compiled DLLs, PDBs, intermediate obj files, cache files |
| `src/Clovent.Domain.Tests/[bin/obj]` | **217** | 10.56 MB | Compiled DLLs, PDBs, intermediate obj files, cache files |
| `src/Clovent.Restaurant.Infrastructure/[bin/obj]` | **155** | 11.93 MB | Compiled DLLs, PDBs, intermediate obj files, cache files |
| `src/Clovent.Catalog.Infrastructure/[bin/obj]` | **131** | 7.66 MB | Compiled DLLs, PDBs, intermediate obj files, cache files |
| `src/Clovent.Restaurant.Application/[bin/obj]` | **94** | 7.46 MB | Compiled DLLs, PDBs, intermediate obj files, cache files |
| `src/Clovent.Inventory.Infrastructure/[bin/obj]` | **92** | 3.39 MB | Compiled DLLs, PDBs, intermediate obj files, cache files |
| `src/Clovent.Authentication.Infrastructure/[bin/obj]` | **80** | 2.52 MB | Compiled DLLs, PDBs, intermediate obj files, cache files |
| `src/Clovent.MasterData.Infrastructure/[bin/obj]` | **80** | 2.89 MB | Compiled DLLs, PDBs, intermediate obj files, cache files |
| `src/Clovent.Inventory.Application/[bin/obj]` | **76** | 2.59 MB | Compiled DLLs, PDBs, intermediate obj files, cache files |
| `src/Clovent.Identity.Infrastructure/[bin/obj]` | **74** | 2.56 MB | Compiled DLLs, PDBs, intermediate obj files, cache files |
| `src/Clovent.Catalog.Application/[bin/obj]` | **70** | 3.03 MB | Compiled DLLs, PDBs, intermediate obj files, cache files |
| `src/Clovent.Inventory/[bin/obj]` | **70** | 1.98 MB | Compiled DLLs, PDBs, intermediate obj files, cache files |
| `src/Clovent.Restaurant/[bin/obj]` | **70** | 3.28 MB | Compiled DLLs, PDBs, intermediate obj files, cache files |
| `src/Clovent.Authentication.Application/[bin/obj]` | **64** | 1.52 MB | Compiled DLLs, PDBs, intermediate obj files, cache files |
| `src/Clovent.Catalog/[bin/obj]` | **64** | 2.10 MB | Compiled DLLs, PDBs, intermediate obj files, cache files |
| `src/Clovent.MasterData.Application/[bin/obj]` | **64** | 2.09 MB | Compiled DLLs, PDBs, intermediate obj files, cache files |
| `src/Clovent.Authentication/[bin/obj]` | **58** | 1.24 MB | Compiled DLLs, PDBs, intermediate obj files, cache files |
| `src/Clovent.Identity.Application/[bin/obj]` | **58** | 1.74 MB | Compiled DLLs, PDBs, intermediate obj files, cache files |
| `src/Clovent.MasterData/[bin/obj]` | **58** | 1.45 MB | Compiled DLLs, PDBs, intermediate obj files, cache files |
| `src/Clovent.Identity/[bin/obj]` | **52** | 1.18 MB | Compiled DLLs, PDBs, intermediate obj files, cache files |
| `src/Clovent.Platform/[bin/obj]` | **44** | 0.70 MB | Compiled DLLs, PDBs, intermediate obj files, cache files |
| `src/Clovent.Domain/[bin/obj]` | **42** | 0.17 MB | Compiled DLLs, PDBs, intermediate obj files, cache files |
| `.vs` | **21** | 22.08 MB | Compiled DLLs, PDBs, intermediate obj files, cache files |
| `qa` | **1** | 0.00 MB | Compiled DLLs, PDBs, intermediate obj files, cache files |

### 7.2 Binary Executables in Non-Bin Directories
- `qa/live_ui/ClickTool.exe` (13 KB): Compiled input simulator executable. Its source file `qa/live_ui/ClickTool.cs` is tracked. The `.exe` binary can be compiled on demand.

---

## 8. Database & SQL Files

The system employs **Entity Framework Core 10** code-first migrations for schema evolution, supported by SQL Server (production) and SQLite (testing and QA).

### 8.1 Standalone SQL Inspection Scripts (`qa/`)

| File Path | Size | Purpose | Scripts / Queries Contained |
|---|---|---|---|
| `qa/v3_sql1.sql` | 420 B | QA Acceptance Script | Validates table occupancy status (`RestaurantTables.Status`) |
| `qa/v3_sql2.sql` | 318 B | QA Acceptance Script | Verifies default walk-in customer record (`RestaurantCustomers.IsDefault`) |
| `qa/v3_sql3.sql` | 776 B | QA Acceptance Script | Validates order line items, modifiers, and subtotals |
| `qa/v3_sql4.sql` | 231 B | QA Acceptance Script | Queries split payment tenders and distribution |
| `qa/v3_inv1.sql` | 315 B | QA Acceptance Script | Inspects stock levels for inventory decrements |
| `qa/v3_inv2.sql` | 1,060 B | QA Acceptance Script | Multi-warehouse inventory balance and adjustment log check |

### 8.2 EF Core Migrations Inventory
- `src/Clovent.Restaurant.Infrastructure/Persistence/Migrations/20260912025646_AddShiftManagement.cs` (Tables: `RestaurantShifts`, `RestaurantCashMovements`)
- `src/Clovent.Restaurant.Infrastructure/Persistence/Migrations/20260914160000_AddCustomerIsDefault.cs` (Column: `RestaurantCustomers.IsDefault`)
- `src/Clovent.Restaurant.Infrastructure/Persistence/Migrations/20260915054626_AddSmartPosFeatures.cs` (Tables: `RestaurantQuickOrderTemplates`, `RestaurantRecommendationRules`)
- Supporting migrations in `Clovent.Authentication`, `Clovent.Identity`, `Clovent.Catalog`, `Clovent.Inventory`, `Clovent.MasterData`.

---

## 9. Restaurant POS Feature Mapping

Specialized audit mapping of every file participating in core Restaurant POS capabilities:

### Restaurant POS Main Screen
- `src/Clovent.Desktop/Restaurant/Orders/RestaurantPosForm.cs` & `.Designer.cs` & `.resx`
- `src/Clovent.Desktop/Restaurant/Orders/SmartPosState.cs`
- `src/Clovent.Desktop/Restaurant/Orders/RestaurantPosDesignDataProvider.cs`

### Dine-In & Table Management
- `src/Clovent.Desktop/Restaurant/Orders/TablePickerEdit.cs` (Table selection flyout)
- `src/Clovent.Desktop/Restaurant/Orders/TableSelectionDineInPolicy.cs` (Automatic table selection)
- `src/Clovent.Desktop/Restaurant/Orders/TableTransferDialog.cs` & `.Designer.cs` & `.resx`
- `src/Clovent.Desktop/Restaurant/Tables/TableManagementView.cs` & `.Designer.cs`
- `src/Clovent.Desktop/Restaurant/Tables/TableEditForm.cs` & `.Designer.cs` & `.resx`
- `src/Clovent.Restaurant/Tables/RestaurantTable.cs`
- `src/Clovent.Restaurant.Application/Tables/Commands/UpdateTableStatusCommand.cs`
- `src/Clovent.Restaurant.Application/Orders/Commands/TransferOrderTableCommand.cs`

### Take Away & Quick Orders
- `src/Clovent.Desktop/Restaurant/SmartPos/QuickOrderTemplatesView.cs` & `.Designer.cs`
- `src/Clovent.Desktop/Restaurant/SmartPos/QuickOrderTemplateEditForm.cs`
- `src/Clovent.Desktop/Restaurant/Orders/QuickOrderPreviewDialog.cs`
- `src/Clovent.Restaurant/QuickOrderTemplates/QuickOrderTemplate.cs`
- `src/Clovent.Restaurant.Application/QuickOrderTemplates/Commands/ApplyQuickOrderTemplateCommand.cs`

### Customers & Loyalty
- `src/Clovent.Desktop/Restaurant/Customers/CustomersView.cs` & `.Designer.cs`
- `src/Clovent.Desktop/Restaurant/Customers/CustomerEditForm.cs` & `.Designer.cs`
- `src/Clovent.Desktop/Restaurant/Orders/CustomerReorderDialogs.cs`
- `src/Clovent.Restaurant/Customers/Customer.cs` & `ICustomerRepository.cs`
- `src/Clovent.Restaurant.Application/Customers/Commands/CreateCustomerCommand.cs`
- `src/Clovent.Restaurant.Application/Customers/Commands/SetDefaultCustomerCommand.cs`
- `src/Clovent.Restaurant.Application/Customers/Queries/GetDefaultCustomerQuery.cs`

### Orders, Items, Hold & Recall
- `src/Clovent.Restaurant/Orders/Order.cs`, `OrderItem.cs`, `IOrderRepository.cs`
- `src/Clovent.Restaurant.Application/Orders/Commands/CreateOrderCommand.cs`
- `src/Clovent.Restaurant.Application/Orders/Commands/AddOrderItemCommand.cs`
- `src/Clovent.Restaurant.Application/Orders/Commands/HoldOrderCommand.cs`
- `src/Clovent.Restaurant.Application/Orders/Commands/RecallOrderCommand.cs`
- `src/Clovent.Restaurant.Application/Orders/Commands/CancelOrderCommand.cs`
- `src/Clovent.Desktop/Restaurant/Orders/RecallOrderDialog.cs` & `.Designer.cs`
- `src/Clovent.Desktop/Restaurant/Orders/RunningOrdersView.cs` & `.Designer.cs`

### Payments, Split Payment & Tender
- `src/Clovent.Desktop/Restaurant/Orders/SplitPaymentDialog.cs`
- `src/Clovent.Restaurant/Orders/OrderPayment.cs`
- `src/Clovent.Restaurant.Application/Orders/Commands/RecordOrderPaymentCommand.cs`
- `src/Clovent.Restaurant.Infrastructure/Persistence/Configurations/OrderPaymentConfiguration.cs`

### Discounts & Service Charges
- `src/Clovent.Desktop/Restaurant/Orders/ServiceChargeDialog.cs` & `.Designer.cs` & `.resx`
- `src/Clovent.Restaurant.Application/Orders/Commands/ApplyServiceChargeCommand.cs`
- `src/Clovent.Restaurant.Application/Orders/Commands/ApplyDiscountCommand.cs`

### Printing & Kitchen Receipts
- `src/Clovent.Desktop/Restaurant/Orders/ReceiptFormatter.cs`
- `src/Clovent.Desktop/Restaurant/Orders/ReceiptPrintDocument.cs`
- `src/Clovent.Desktop/Restaurant/Orders/ReceiptPreviewForm.cs` & `.Designer.cs` & `.resx`

### Shifts & Cash Drawer Operations
- `src/Clovent.Desktop/Restaurant/Shifts/OpenShiftDialog.cs`
- `src/Clovent.Desktop/Restaurant/Shifts/CloseShiftDialog.cs`
- `src/Clovent.Desktop/Restaurant/Shifts/CashMovementDialog.cs`
- `src/Clovent.Desktop/Restaurant/Shifts/ShiftDetailDialog.cs`
- `src/Clovent.Desktop/Restaurant/Shifts/ShiftHistoryView.cs`
- `src/Clovent.Restaurant/Shifts/Shift.cs`, `CashMovement.cs`, `IShiftRepository.cs`

---

## 10. Test / QA Files

### 10.1 Automated Test Projects (18 Projects)

| Test Project | Test Class Count | Focus Area | Technology |
|---|---|---|---|
| **Clovent.PackageManager.Tests** | 0 classes | Unit & Integration tests | xUnit, Coverlet, EF Core In-Memory / SQLite |
| **Clovent.Core.Tests** | 1 classes | Unit & Integration tests | xUnit, Coverlet, EF Core In-Memory / SQLite |
| **Clovent.Generator.Tests** | 3 classes | Unit & Integration tests | xUnit, Coverlet, EF Core In-Memory / SQLite |
| **Clovent.Authentication.Application.Tests** | 16 classes | Unit & Integration tests | xUnit, Coverlet, EF Core In-Memory / SQLite |
| **Clovent.Authentication.Infrastructure.Tests** | 9 classes | Unit & Integration tests | xUnit, Coverlet, EF Core In-Memory / SQLite |
| **Clovent.Authentication.Tests** | 14 classes | Unit & Integration tests | xUnit, Coverlet, EF Core In-Memory / SQLite |
| **Clovent.Catalog.Application.Tests** | 9 classes | Unit & Integration tests | xUnit, Coverlet, EF Core In-Memory / SQLite |
| **Clovent.Catalog.Infrastructure.Tests** | 9 classes | Unit & Integration tests | xUnit, Coverlet, EF Core In-Memory / SQLite |
| **Clovent.Catalog.Tests** | 9 classes | Unit & Integration tests | xUnit, Coverlet, EF Core In-Memory / SQLite |
| **Clovent.Desktop.Tests** | 43 classes | Unit & Integration tests | xUnit, Coverlet, EF Core In-Memory / SQLite |
| **Clovent.Desktop.UiQa** | 0 classes | Unit & Integration tests | xUnit, Coverlet, EF Core In-Memory / SQLite |
| **Clovent.Domain.Tests** | 3 classes | Unit & Integration tests | xUnit, Coverlet, EF Core In-Memory / SQLite |
| **Clovent.Identity.Application.Tests** | 9 classes | Unit & Integration tests | xUnit, Coverlet, EF Core In-Memory / SQLite |
| **Clovent.Identity.Infrastructure.Tests** | 8 classes | Unit & Integration tests | xUnit, Coverlet, EF Core In-Memory / SQLite |
| **Clovent.Identity.Tests** | 19 classes | Unit & Integration tests | xUnit, Coverlet, EF Core In-Memory / SQLite |
| **Clovent.Inventory.Application.Tests** | 4 classes | Unit & Integration tests | xUnit, Coverlet, EF Core In-Memory / SQLite |
| **Clovent.Inventory.Infrastructure.Tests** | 5 classes | Unit & Integration tests | xUnit, Coverlet, EF Core In-Memory / SQLite |
| **Clovent.Inventory.Tests** | 4 classes | Unit & Integration tests | xUnit, Coverlet, EF Core In-Memory / SQLite |
| **Clovent.MasterData.Application.Tests** | 8 classes | Unit & Integration tests | xUnit, Coverlet, EF Core In-Memory / SQLite |
| **Clovent.MasterData.Infrastructure.Tests** | 9 classes | Unit & Integration tests | xUnit, Coverlet, EF Core In-Memory / SQLite |
| **Clovent.MasterData.Tests** | 9 classes | Unit & Integration tests | xUnit, Coverlet, EF Core In-Memory / SQLite |
| **Clovent.Platform.Tests** | 5 classes | Unit & Integration tests | xUnit, Coverlet, EF Core In-Memory / SQLite |
| **Clovent.Restaurant.Application.Tests** | 28 classes | Unit & Integration tests | xUnit, Coverlet, EF Core In-Memory / SQLite |
| **Clovent.Restaurant.Infrastructure.Tests** | 14 classes | Unit & Integration tests | xUnit, Coverlet, EF Core In-Memory / SQLite |
| **Clovent.Restaurant.Tests** | 15 classes | Unit & Integration tests | xUnit, Coverlet, EF Core In-Memory / SQLite |

### 10.2 Interactive QA Automation Suite (`qa/`)
- **`qa/live_ui/`**: 15 PowerShell orchestration scripts driving interactive UI automation (`dologin.ps1`, `flow.ps1`, `drv.ps1`, `btns.ps1`).
- **`qa/live_ui/ClickTool.cs` & `.exe`**: Native Win32 `SendInput` mouse simulator for pixel-accurate DevExpress button interactions.
- **Screenshot Baselines**: Over 400 reference captures tracking UI states across Dine-In, Quick Orders, Tender, PIN entry, and Split Payment workflows.

---

## 11. Configuration / Environment Files

Sensitive credentials and secrets have been redacted in accordance with the security policy:

| Configuration File | Relative Path | Purpose | Sensitive Information Status |
|---|---|---|---|
| `appsettings.json` | `src/Clovent.Desktop/appsettings.json` | Main application configuration & database connection strings | Database credentials: `Server=...;User Id=...;Password=[REDACTED]` |
| `appsettings.Development.json` | `src/Clovent.Desktop/appsettings.Development.json` | Development overrides & detailed logging | No secrets detected |
| `launchSettings.json` | `src/Clovent.Desktop/Properties/launchSettings.json` | Visual Studio runtime profiles | No secrets detected |
| `appsettings.json` | `Tools/Clovent.CLI/src/Clovent.Modules.Identity/appsettings.json` | Tooling database target | Database connection: `[REDACTED]` |
| `manifest.json` & `installed.json` | `Tools/Clovent.CLI/` | CLI package registry manifests | No secrets detected |
| `.editorconfig` | Root & Subdirectories | Compiler analysis and code formatting rules | No secrets detected |
| `.gitignore` | Root `.gitignore` | Git source control exclusions | No secrets detected |

---

## 12. Git / Source Control Inspection

### 12.1 `.gitignore` Analysis
The root `.gitignore` properly specifies exclusions for `bin/`, `obj/`, `.vs/`, and user-specific files.

### 12.2 Suspicious / Untracked Files Found in Working Tree
- **Committed / Untracked Binary Executable:** `qa/live_ui/ClickTool.exe` is present in the working tree. Binaries should generally not be tracked in git; it should be compiled from `ClickTool.cs` during test setup.
- **In-place Backup Files (`.backup-*`):** 19 files inside `src/Clovent.Desktop/` are tracked or pending untracked in git working copy.
- **Root Ad-hoc Scripts & Screenshots:** 10 `.ps1` files and 3 `.png` files in the root folder are untracked artifacts from manual testing sessions.
- **Cloud Sync Folders:** `.tmp.driveupload/` and `.tmp.drivedownload/` are temporary Google Drive / cloud storage sync artifacts that should be added to `.gitignore`.

---

## 13. System Architecture Overview

Clovent Business Operating System (CBOS) is structured according to **Clean Architecture** and **Domain-Driven Design (DDD)** principles:

1. **Presentation Layer (`src/Clovent.Desktop`):**
   - Windows Forms desktop application leveraging DevExpress v26.1 UI component suite.
   - Built around an MDI / Ribbon Shell (`MainForm`) hosting touch-optimized POS modules, visual table designers, cashier drawers, customer management, and analytics.

2. **Application Layer (`src/Clovent.*.Application`):**
   - Pure C# CQRS implementation using MediatR.
   - Contains Commands (e.g. `CreateOrderCommand`, `RecordOrderPaymentCommand`), Queries, and Handlers.
   - Decoupled from UI and database concerns.

3. **Domain Layer (`src/Clovent.*` Domain Projects & `Clovent.Domain`):**
   - Aggregate Roots (`Order`, `Customer`, `RestaurantTable`, `Shift`, `Product`).
   - Domain Events, Value Objects, and Business Invariants with zero external framework dependencies.

4. **Infrastructure Layer (`src/Clovent.*.Infrastructure`):**
   - Entity Framework Core 10 implementations for repositories and database contexts.
   - Dual provider support: SQL Server for production and SQLite for local integration testing.

5. **Cross-Cutting Platform (`src/Clovent.Platform`):**
   - Microsoft Generic Host builder, file-based logging providers, pipeline behaviors, and validation.

---

## 14. Cleanup Candidates — DO NOT DELETE

> **NOTICE:** This section categorizes files for developer review. No cleanup actions have been executed.

### Category A: Definitely Generated
- **11,085 files** in `bin/`, `obj/`, `.vs/`, `.git/`, `.tmp.driveupload/`, and `qa/live_ui/ClickTool.exe`.

### Category B: Definitely Temporary
- `cart_diagnostics.txt` (Root directory, 182 KB diagnostic dump from 2026-08-17).
- Root temporary screenshots: `shot_login.png`, `shot_login2.png`, `test_screen.png`.
- Temporary UIA dump text files in root: `qauia_pos.txt`, `qauia_pos_tree.txt`, `qauia_tree.txt`, `qawin32_pos.txt`.

### Category C: Probably Obsolete
- **19 in-place `.backup-*` files** in `src/Clovent.Desktop/` created during August 2026 debugging sessions.
- **9 `.bak` backup files** in `backups/` directory.

### Category D: Possible Duplicate
- Historical POS form versions: `RestaurantPosForm.cs.backup-*` (6 copies) and `RestaurantPosForm.Designer.cs.backup-*` (10 copies).
- 20 duplicate class stubs in `Tools/Clovent.CLI/Clovent.CBOS.Desktop` mirroring `Clovent.Desktop` class names.

### Category E: Possibly Unused
- 10 root-level test scripts (`click.ps1`, `enum.ps1`, `fg.ps1`, `find.ps1`, `login.ps1`, `pclick.ps1`, `shoot.ps1`, `type.ps1`, `uia.ps1`, `uia_set.ps1`).
- `scratch/DesignerTest/` (Standalone scratch project).
- 7 empty stub files in `Tools/Clovent.CLI/Clovent.CLI/Services/`.

### Category F: Required / Keep
- All 43 projects under `src/` referenced by `Clovent.BusinessOperatingSystem.slnx`.
- All production source code, designers, resource files, and EF Core migrations.
- All 18 automated test projects.
- Core documentation (`00 Vision/` through `13 ADR/`, `docs/`, `Foundation/`).

### Category G: Cannot Determine Without Runtime Verification
- `UniversalSearchDropdown.cs`: Dynamic UI dropdown control in POS.
- `CustomerReorderDialogs.cs`: Event-driven reorder dialog.
- `RestaurantPulseForm.cs`: Analytics window launched via dynamic navigation command.

---

## 15. Final Statistical Summary

### 15.1 Global Counts
- **Total Solutions:** 3 (`Clovent.BusinessOperatingSystem.slnx`, `Clovent.CLI.slnx`, `Clovent.PackageManager.slnx`)
- **Total Projects:** 59 (.csproj files)
- **Total Source Files:** 1,419 (.cs code-behind & classes)
- **Total Test Files:** 331 (test classes and QA scripts)
- **Total Configuration Files:** 77 (appsettings, .csproj, manifests)
- **Total SQL / Database Files:** 6 (.sql scripts in qa/)
- **Total Resource Files:** 69 (.resx WinForms resources)
- **Total Documentation Files:** 154 (.md and specs)
- **Total Generated / Binary Files:** 11,085 (bin, obj, .vs, caches)
- **Total Other Files:** 376 (images, backup snapshots)

### 15.2 Cleanup Review Metrics
- **Potential Cleanup Candidates:** 172 items (Redundant backups, empty stubs, root test scripts, scratch sandbox)
- **Potential Duplicates:** 79 items (In-place and out-of-tree backup duplicates)
- **Potential Obsolete Files:** 28 items (Historical snapshot files from August 2026)
- **Potential Unused Files:** 125 items (Empty tool stubs, unreferenced scratch files, ad-hoc root scripts)
- **Potential Generated Files:** 11,085 items (Safe to clean with `dotnet clean`)

---

### Verification & Safety Declaration
- **Report File:** `D:\Clovent Business Operating System\PROJECT_STRUCTURE_AUDIT.md`
- **Filesystem Integrity Check:** PASSED. No production source code, database schemas, package references, or configurations have been altered.
