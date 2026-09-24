# PROJECT STRUCTURE AUDIT REPORT — CLOVENT BUSINESS OPERATING SYSTEM

> **Audit Execution Mode:** READ-ONLY
> **Audit Scope:** Entire Solution, Workspace Root, Tools, Backups, QA, and Source Trees
> **Target Output File:** `PROJECT_STRUCTURE_REPORT.md` (Root level, outside project source trees)
> **Safety Guarantee:** Confirmed NO existing project files, source codes, packages, configurations, databases, or project assets were modified, deleted, moved, or refactored.

---

## Executive Summary Dashboard

| Metric | Count | Description |
|---|---|---|
| **Total Solution Files** | **3** | `Clovent.BusinessOperatingSystem.slnx`, `Clovent.CLI.slnx`, `Clovent.PackageManager.slnx` |
| **Total Discovered Projects (.csproj)** | **59** | 43 Main Business projects, 11 CLI/Generator projects, 4 Package Manager projects, 1 Scratch Sandbox project |
| **Total Folders (Recursive)** | **3,232** | 907 non-generated source/doc/qa directories; 2,325 build/git/cache directories |
| **Total Files (Recursive)** | **13,622** | Full workspace file inventory |
| **Active Non-Generated Files** | **2,537** | C# code, designers, resx, configs, docs, diagrams, QA scripts, tests |
| **Build / Generated Artifacts** | **11,085** | `bin/`, `obj/`, `.vs/`, `.git/`, temporary download/upload caches |
| **Duplicate / Backup Files** | **79** | Dated snapshot backups, `.bak`, `.backup-*` inline files |
| **Potentially Unused / Candidate Files** | **172** | Dead CLI stubs, root test scripts, scratch sandbox, obsolete backup snapshots |

---

## 1. Solution Structure

The workspace contains three `.slnx` solution definitions and one experimental scratch project:

1. **`Clovent.BusinessOperatingSystem.slnx` (Primary Enterprise Solution)**
   - **Location:** `Clovent.BusinessOperatingSystem.slnx`
   - **Solution Type:** Visual Studio XML-based Solution (.slnx format)
   - **Total Projects in Solution:** 43 projects (All hosted under `src/`)
   - **Bounded Contexts:** Core Domain, Platform, Authentication, Identity, MasterData, Catalog, Inventory, Restaurant (POS), Desktop (WinForms + DevExpress Presentation)
   - **Target Framework:** `net10.0` for Domain/Application/Infrastructure/Platform; `net10.0-windows` for Desktop presentation

2. **`Tools/Clovent.CLI/Clovent.CLI.slnx` (Code Scaffolding & Generator Solution)**
   - **Location:** `Tools/Clovent.CLI/Clovent.CLI.slnx`
   - **Total Projects in Solution:** 11 projects (7 internal tool projects, 2 generator test projects, 2 cross-references to main `src/`)
   - **Target Framework:** `net10.0`, `net10.0-windows` for CBOS.Desktop prototype

3. **`Tools/Clovent.CLI/Clovent.PackageManager.slnx` (Modular Package Manager Solution)**
   - **Location:** `Tools/Clovent.CLI/Clovent.PackageManager.slnx`
   - **Total Projects in Solution:** 4 projects (`Abstractions`, `Core`, `Tests`, `Host CLI`)
   - **Target Framework:** `net10.0`

4. **Standalone Scratch Sandbox (Not in any solution):**
   - `scratch/DesignerTest/DesignerTest.csproj` (net10.0-windows WinForms sandbox)
   - `src/Clovent.Desktop.UiQa/Clovent.Desktop.UiQa.csproj` (net10.0-windows Live QA UI test harness executable, standalone)

### 1.1 Complete Project Inventory & Specification

| Project Name | Path (.csproj) | Target Framework | Output Type | Windows Forms? | Project References | Key Package Dependencies |
|---|---|---|---|---|---|---|
| **Clovent.CBOS.Desktop** | `Tools/Clovent.CLI/Clovent.CBOS.Desktop/Clovent.CBOS.Desktop.csproj` | `net10.0-windows` | `WinExe` | Yes | *None* | Microsoft.EntityFrameworkCore.Design `v10.0.10`<br/>Microsoft.EntityFrameworkCore.SqlServer `v10.0.10`<br/>Microsoft.Extensions.Configuration `v10.0.10`<br/>Microsoft.Extensions.Configuration.Json `v10.0.10`<br/>*+3 more* |
| **Clovent.CLI** | `Tools/Clovent.CLI/Clovent.CLI/Clovent.CLI.csproj` | `net10.0` | `Exe` | No | Clovent.Core<br/>Clovent.Generator<br/>Clovent.Documents<br/>Clovent.Configuration<br/>Clovent.Shared | Microsoft.Extensions.Configuration `v10.0.10`<br/>Microsoft.Extensions.Configuration.Json `v10.0.10`<br/>Microsoft.Extensions.DependencyInjection `v10.0.10`<br/>Microsoft.Extensions.Hosting `v10.0.10`<br/>*+4 more* |
| **Clovent.PackageManager.Abstractions** | `Tools/Clovent.CLI/Clovent.PackageManager.Abstractions/Clovent.PackageManager.Abstractions.csproj` | `net10.0` | `Library` | No | *None* | *None* |
| **Clovent.PackageManager.Core** | `Tools/Clovent.CLI/Clovent.PackageManager.Core/Clovent.PackageManager.Core.csproj` | `net10.0` | `Library` | No | Clovent.PackageManager.Abstractions | *None* |
| **Clovent.PackageManager.Tests** | `Tools/Clovent.CLI/Clovent.PackageManager.Tests/Clovent.PackageManager.Tests.csproj` | `net10.0` | `Library` | No | *None* | coverlet.collector `v6.0.4`<br/>Microsoft.NET.Test.Sdk `v17.14.1`<br/>xunit `v2.9.3`<br/>xunit.runner.visualstudio `v3.1.4` |
| **Clovent.PackageManager** | `Tools/Clovent.CLI/Clovent.PackageManager/Clovent.PackageManager.csproj` | `net10.0` | `Exe` | No | Clovent.PackageManager.Core<br/>Clovent.PackageManager.Abstractions | *None* |
| **Clovent.Configuration** | `Tools/Clovent.CLI/src/Clovent.Configuration/Clovent.Configuration.csproj` | `net10.0` | `Library` | No | *None* | *None* |
| **Clovent.Core** | `Tools/Clovent.CLI/src/Clovent.Core/Clovent.Core.csproj` | `net10.0` | `Library` | No | *None* | *None* |
| **Clovent.Documents** | `Tools/Clovent.CLI/src/Clovent.Documents/Clovent.Documents.csproj` | `net10.0` | `Library` | No | Clovent.Core | *None* |
| **Clovent.Generator** | `Tools/Clovent.CLI/src/Clovent.Generator/Clovent.Generator.csproj` | `net10.0` | `Library` | No | Clovent.Core<br/>Clovent.Documents<br/>Clovent.Templates<br/>Clovent.Configuration<br/>Clovent.Shared | *None* |
| **Clovent.Modules.Identity** | `Tools/Clovent.CLI/src/Clovent.Modules.Identity/Clovent.Modules.Identity.csproj` | `net10.0` | `Library` | No | Clovent.Core<br/>Clovent.Shared | BCrypt.Net-Next `v4.2.0`<br/>MediatR `v12.4.1`<br/>Microsoft.EntityFrameworkCore `v10.0.10`<br/>Microsoft.Extensions.Configuration `v10.0.10`<br/>*+5 more* |
| **Clovent.Shared** | `Tools/Clovent.CLI/src/Clovent.Shared/Clovent.Shared.csproj` | `net10.0` | `Library` | No | *None* | MediatR.Contracts `v2.0.1` |
| **Clovent.Templates** | `Tools/Clovent.CLI/src/Clovent.Templates/Clovent.Templates.csproj` | `net10.0` | `Library` | No | Clovent.Core | *None* |
| **Clovent.Core.Tests** | `Tools/Clovent.CLI/tests/Clovent.Core.Tests/Clovent.Core.Tests.csproj` | `net10.0` | `Library` | No | Clovent.Core | coverlet.collector `v6.0.4`<br/>Microsoft.NET.Test.Sdk `v17.14.1`<br/>xunit `v2.9.3`<br/>xunit.runner.visualstudio `v3.1.4` |
| **Clovent.Generator.Tests** | `Tools/Clovent.CLI/tests/Clovent.Generator.Tests/Clovent.Generator.Tests.csproj` | `net10.0` | `Library` | No | Clovent.Generator<br/>Clovent.Core<br/>Clovent.Templates<br/>Clovent.Documents | Microsoft.NET.Test.Sdk `v17.13.0`<br/>xunit `v2.9.3`<br/>xunit.runner.visualstudio `v3.0.2` |
| **DesignerTest** | `scratch/DesignerTest/DesignerTest.csproj` | `net10.0-windows` | `WinExe` | Yes | Clovent.Desktop | *None* |
| **Clovent.Authentication.Application.Tests** | `src/Clovent.Authentication.Application.Tests/Clovent.Authentication.Application.Tests.csproj` | `net10.0` | `Library` | No | Clovent.Authentication.Application | Microsoft.NET.Test.Sdk `v17.14.1`<br/>Microsoft.Extensions.Configuration `v10.0.10`<br/>Microsoft.Extensions.DependencyInjection `v10.0.10`<br/>xunit `v2.9.3`<br/>*+2 more* |
| **Clovent.Authentication.Application** | `src/Clovent.Authentication.Application/Clovent.Authentication.Application.csproj` | `net10.0` | `Library` | No | Clovent.Authentication<br/>Clovent.Identity | MediatR `v12.4.1`<br/>Microsoft.Extensions.Configuration.Abstractions `v10.0.10`<br/>Microsoft.Extensions.DependencyInjection.Abstractions `v10.0.10` |
| **Clovent.Authentication.Infrastructure.Tests** | `src/Clovent.Authentication.Infrastructure.Tests/Clovent.Authentication.Infrastructure.Tests.csproj` | `net10.0` | `Library` | No | Clovent.Authentication.Infrastructure | Microsoft.NET.Test.Sdk `v17.14.1`<br/>xunit `v2.9.3`<br/>xunit.runner.visualstudio `v3.1.4`<br/>coverlet.collector `v6.0.4`<br/>*+3 more* |
| **Clovent.Authentication.Infrastructure** | `src/Clovent.Authentication.Infrastructure/Clovent.Authentication.Infrastructure.csproj` | `net10.0` | `Library` | No | Clovent.Authentication<br/>Clovent.Authentication.Application<br/>Clovent.Identity<br/>Clovent.Platform | Microsoft.EntityFrameworkCore `v10.0.10`<br/>Microsoft.EntityFrameworkCore.SqlServer `v10.0.10`<br/>Microsoft.EntityFrameworkCore.Design `v10.0.10`<br/>Microsoft.Extensions.Configuration.Abstractions `v10.0.10`<br/>*+2 more* |
| **Clovent.Authentication.Tests** | `src/Clovent.Authentication.Tests/Clovent.Authentication.Tests.csproj` | `net10.0` | `Library` | No | Clovent.Authentication | Microsoft.NET.Test.Sdk `v17.14.1`<br/>xunit `v2.9.3`<br/>xunit.runner.visualstudio `v3.1.4`<br/>coverlet.collector `v6.0.4` |
| **Clovent.Authentication** | `src/Clovent.Authentication/Clovent.Authentication.csproj` | `net10.0` | `Library` | No | Clovent.Domain<br/>Clovent.Identity | *None* |
| **Clovent.Catalog.Application.Tests** | `src/Clovent.Catalog.Application.Tests/Clovent.Catalog.Application.Tests.csproj` | `net10.0` | `Library` | No | Clovent.Catalog.Application | Microsoft.NET.Test.Sdk `v17.14.1`<br/>xunit `v2.9.3`<br/>xunit.runner.visualstudio `v3.1.4`<br/>coverlet.collector `v6.0.4` |
| **Clovent.Catalog.Application** | `src/Clovent.Catalog.Application/Clovent.Catalog.Application.csproj` | `net10.0` | `Library` | No | Clovent.Catalog | MediatR `v12.4.1`<br/>Microsoft.Extensions.DependencyInjection.Abstractions `v10.0.10`<br/>Microsoft.Extensions.Configuration.Abstractions `v10.0.10` |
| **Clovent.Catalog.Infrastructure.Tests** | `src/Clovent.Catalog.Infrastructure.Tests/Clovent.Catalog.Infrastructure.Tests.csproj` | `net10.0` | `Library` | No | Clovent.Catalog.Infrastructure | Microsoft.NET.Test.Sdk `v17.14.1`<br/>xunit `v2.9.3`<br/>xunit.runner.visualstudio `v3.1.4`<br/>coverlet.collector `v6.0.4`<br/>*+2 more* |
| **Clovent.Catalog.Infrastructure** | `src/Clovent.Catalog.Infrastructure/Clovent.Catalog.Infrastructure.csproj` | `net10.0` | `Library` | No | Clovent.Catalog<br/>Clovent.Catalog.Application<br/>Clovent.Platform | Microsoft.EntityFrameworkCore `v10.0.10`<br/>Microsoft.EntityFrameworkCore.SqlServer `v10.0.10`<br/>Microsoft.EntityFrameworkCore.Design `v10.0.10`<br/>Microsoft.Extensions.Configuration.Abstractions `v10.0.10`<br/>*+2 more* |
| **Clovent.Catalog.Tests** | `src/Clovent.Catalog.Tests/Clovent.Catalog.Tests.csproj` | `net10.0` | `Library` | No | Clovent.Catalog | Microsoft.NET.Test.Sdk `v17.14.1`<br/>xunit `v2.9.3`<br/>xunit.runner.visualstudio `v3.1.4`<br/>coverlet.collector `v6.0.4` |
| **Clovent.Catalog** | `src/Clovent.Catalog/Clovent.Catalog.csproj` | `net10.0` | `Library` | No | Clovent.Domain<br/>Clovent.MasterData | *None* |
| **Clovent.Desktop.Tests** | `src/Clovent.Desktop.Tests/Clovent.Desktop.Tests.csproj` | `net10.0-windows` | `Library` | Yes | Clovent.Desktop | Microsoft.NET.Test.Sdk `v17.14.1`<br/>xunit `v2.9.3`<br/>xunit.runner.visualstudio `v3.1.4`<br/>coverlet.collector `v6.0.4` |
| **Clovent.Desktop.UiQa** | `src/Clovent.Desktop.UiQa/Clovent.Desktop.UiQa.csproj` | `net10.0-windows` | `Exe` | Yes | Clovent.Desktop | *None* |
| **Clovent.Desktop** | `src/Clovent.Desktop/Clovent.Desktop.csproj` | `net10.0-windows` | `WinExe` | Yes | Clovent.Platform<br/>Clovent.Authentication.Application<br/>Clovent.Authentication.Infrastructure<br/>Clovent.Identity<br/>Clovent.Identity.Application<br/>Clovent.Identity.Infrastructure<br/>Clovent.MasterData<br/>Clovent.MasterData.Application<br/>Clovent.MasterData.Infrastructure<br/>Clovent.Catalog<br/>Clovent.Catalog.Application<br/>Clovent.Catalog.Infrastructure<br/>Clovent.Inventory<br/>Clovent.Inventory.Application<br/>Clovent.Inventory.Infrastructure<br/>Clovent.Restaurant<br/>Clovent.Restaurant.Application<br/>Clovent.Restaurant.Infrastructure | DevExpress.Reporting.Core `v26.1.4-pre-26179`<br/>DevExpress.Win `v26.1.4-pre-26179`<br/>DevExpress.Images `v26.1.4-pre-26179`<br/>Microsoft.Extensions.Hosting `v10.0.10` |
| **Clovent.Domain.Tests** | `src/Clovent.Domain.Tests/Clovent.Domain.Tests.csproj` | `net10.0` | `Library` | No | Clovent.Domain | Microsoft.NET.Test.Sdk `v17.14.1`<br/>xunit `v2.9.3`<br/>xunit.runner.visualstudio `v3.1.4`<br/>coverlet.collector `v6.0.4` |
| **Clovent.Domain** | `src/Clovent.Domain/Clovent.Domain.csproj` | `net10.0` | `Library` | No | *None* | *None* |
| **Clovent.Identity.Application.Tests** | `src/Clovent.Identity.Application.Tests/Clovent.Identity.Application.Tests.csproj` | `net10.0` | `Library` | No | Clovent.Identity.Application | Microsoft.NET.Test.Sdk `v17.14.1`<br/>xunit `v2.9.3`<br/>xunit.runner.visualstudio `v3.1.4`<br/>coverlet.collector `v6.0.4` |
| **Clovent.Identity.Application** | `src/Clovent.Identity.Application/Clovent.Identity.Application.csproj` | `net10.0` | `Library` | No | Clovent.Identity | MediatR `v12.4.1`<br/>Microsoft.Extensions.DependencyInjection.Abstractions `v10.0.10`<br/>Microsoft.Extensions.Configuration.Abstractions `v10.0.10` |
| **Clovent.Identity.Infrastructure.Tests** | `src/Clovent.Identity.Infrastructure.Tests/Clovent.Identity.Infrastructure.Tests.csproj` | `net10.0` | `Library` | No | Clovent.Identity.Infrastructure | Microsoft.NET.Test.Sdk `v17.14.1`<br/>xunit `v2.9.3`<br/>xunit.runner.visualstudio `v3.1.4`<br/>coverlet.collector `v6.0.4`<br/>*+2 more* |
| **Clovent.Identity.Infrastructure** | `src/Clovent.Identity.Infrastructure/Clovent.Identity.Infrastructure.csproj` | `net10.0` | `Library` | No | Clovent.Identity<br/>Clovent.Identity.Application<br/>Clovent.Platform | Microsoft.EntityFrameworkCore `v10.0.10`<br/>Microsoft.EntityFrameworkCore.SqlServer `v10.0.10`<br/>Microsoft.EntityFrameworkCore.Design `v10.0.10`<br/>Microsoft.Extensions.Configuration.Abstractions `v10.0.10`<br/>*+3 more* |
| **Clovent.Identity.Tests** | `src/Clovent.Identity.Tests/Clovent.Identity.Tests.csproj` | `net10.0` | `Library` | No | Clovent.Identity | Microsoft.NET.Test.Sdk `v17.14.1`<br/>xunit `v2.9.3`<br/>xunit.runner.visualstudio `v3.1.4`<br/>coverlet.collector `v6.0.4` |
| **Clovent.Identity** | `src/Clovent.Identity/Clovent.Identity.csproj` | `net10.0` | `Library` | No | Clovent.Domain | *None* |
| **Clovent.Inventory.Application.Tests** | `src/Clovent.Inventory.Application.Tests/Clovent.Inventory.Application.Tests.csproj` | `net10.0` | `Library` | No | Clovent.Inventory.Application | Microsoft.NET.Test.Sdk `v17.14.1`<br/>xunit `v2.9.3`<br/>xunit.runner.visualstudio `v3.1.4`<br/>coverlet.collector `v6.0.4` |
| **Clovent.Inventory.Application** | `src/Clovent.Inventory.Application/Clovent.Inventory.Application.csproj` | `net10.0` | `Library` | No | Clovent.Inventory | MediatR `v12.4.1`<br/>Microsoft.Extensions.DependencyInjection.Abstractions `v10.0.10`<br/>Microsoft.Extensions.Configuration.Abstractions `v10.0.10` |
| **Clovent.Inventory.Infrastructure.Tests** | `src/Clovent.Inventory.Infrastructure.Tests/Clovent.Inventory.Infrastructure.Tests.csproj` | `net10.0` | `Library` | No | Clovent.Inventory.Infrastructure | Microsoft.NET.Test.Sdk `v17.14.1`<br/>xunit `v2.9.3`<br/>xunit.runner.visualstudio `v3.1.4`<br/>coverlet.collector `v6.0.4`<br/>*+2 more* |
| **Clovent.Inventory.Infrastructure** | `src/Clovent.Inventory.Infrastructure/Clovent.Inventory.Infrastructure.csproj` | `net10.0` | `Library` | No | Clovent.Inventory<br/>Clovent.Inventory.Application<br/>Clovent.Platform | Microsoft.EntityFrameworkCore `v10.0.10`<br/>Microsoft.EntityFrameworkCore.SqlServer `v10.0.10`<br/>Microsoft.EntityFrameworkCore.Design `v10.0.10`<br/>Microsoft.Extensions.Configuration.Abstractions `v10.0.10`<br/>*+2 more* |
| **Clovent.Inventory.Tests** | `src/Clovent.Inventory.Tests/Clovent.Inventory.Tests.csproj` | `net10.0` | `Library` | No | Clovent.Inventory | Microsoft.NET.Test.Sdk `v17.14.1`<br/>xunit `v2.9.3`<br/>xunit.runner.visualstudio `v3.1.4`<br/>coverlet.collector `v6.0.4` |
| **Clovent.Inventory** | `src/Clovent.Inventory/Clovent.Inventory.csproj` | `net10.0` | `Library` | No | Clovent.Domain<br/>Clovent.MasterData<br/>Clovent.Catalog | *None* |
| **Clovent.MasterData.Application.Tests** | `src/Clovent.MasterData.Application.Tests/Clovent.MasterData.Application.Tests.csproj` | `net10.0` | `Library` | No | Clovent.MasterData.Application | Microsoft.NET.Test.Sdk `v17.14.1`<br/>xunit `v2.9.3`<br/>xunit.runner.visualstudio `v3.1.4`<br/>coverlet.collector `v6.0.4` |
| **Clovent.MasterData.Application** | `src/Clovent.MasterData.Application/Clovent.MasterData.Application.csproj` | `net10.0` | `Library` | No | Clovent.MasterData | MediatR `v12.4.1`<br/>Microsoft.Extensions.DependencyInjection.Abstractions `v10.0.10`<br/>Microsoft.Extensions.Configuration.Abstractions `v10.0.10` |
| **Clovent.MasterData.Infrastructure.Tests** | `src/Clovent.MasterData.Infrastructure.Tests/Clovent.MasterData.Infrastructure.Tests.csproj` | `net10.0` | `Library` | No | Clovent.MasterData.Infrastructure | Microsoft.NET.Test.Sdk `v17.14.1`<br/>xunit `v2.9.3`<br/>xunit.runner.visualstudio `v3.1.4`<br/>coverlet.collector `v6.0.4`<br/>*+2 more* |
| **Clovent.MasterData.Infrastructure** | `src/Clovent.MasterData.Infrastructure/Clovent.MasterData.Infrastructure.csproj` | `net10.0` | `Library` | No | Clovent.MasterData<br/>Clovent.MasterData.Application<br/>Clovent.Platform | Microsoft.EntityFrameworkCore `v10.0.10`<br/>Microsoft.EntityFrameworkCore.SqlServer `v10.0.10`<br/>Microsoft.EntityFrameworkCore.Design `v10.0.10`<br/>Microsoft.Extensions.Configuration.Abstractions `v10.0.10`<br/>*+2 more* |
| **Clovent.MasterData.Tests** | `src/Clovent.MasterData.Tests/Clovent.MasterData.Tests.csproj` | `net10.0` | `Library` | No | Clovent.MasterData | Microsoft.NET.Test.Sdk `v17.14.1`<br/>xunit `v2.9.3`<br/>xunit.runner.visualstudio `v3.1.4`<br/>coverlet.collector `v6.0.4` |
| **Clovent.MasterData** | `src/Clovent.MasterData/Clovent.MasterData.csproj` | `net10.0` | `Library` | No | Clovent.Domain<br/>Clovent.Identity | *None* |
| **Clovent.Platform.Tests** | `src/Clovent.Platform.Tests/Clovent.Platform.Tests.csproj` | `net10.0` | `Library` | No | Clovent.Platform | Microsoft.NET.Test.Sdk `v17.14.1`<br/>xunit `v2.9.3`<br/>xunit.runner.visualstudio `v3.1.4`<br/>coverlet.collector `v6.0.4` |
| **Clovent.Platform** | `src/Clovent.Platform/Clovent.Platform.csproj` | `net10.0` | `Library` | No | *None* | Microsoft.Extensions.Hosting `v10.0.10`<br/>Microsoft.Extensions.Options.DataAnnotations `v10.0.10` |
| **Clovent.Restaurant.Application.Tests** | `src/Clovent.Restaurant.Application.Tests/Clovent.Restaurant.Application.Tests.csproj` | `net10.0` | `Library` | No | Clovent.Restaurant.Application | Microsoft.NET.Test.Sdk `v17.14.1`<br/>xunit `v2.9.3`<br/>xunit.runner.visualstudio `v3.1.4`<br/>coverlet.collector `v6.0.4` |
| **Clovent.Restaurant.Application** | `src/Clovent.Restaurant.Application/Clovent.Restaurant.Application.csproj` | `net10.0` | `Library` | No | Clovent.Restaurant<br/>Clovent.Catalog.Application<br/>Clovent.Inventory.Application | MediatR `v12.4.1`<br/>Microsoft.Extensions.Configuration.Abstractions `v10.0.10`<br/>Microsoft.Extensions.DependencyInjection.Abstractions `v10.0.10` |
| **Clovent.Restaurant.Infrastructure.Tests** | `src/Clovent.Restaurant.Infrastructure.Tests/Clovent.Restaurant.Infrastructure.Tests.csproj` | `net10.0` | `Library` | No | Clovent.Restaurant.Infrastructure<br/>Clovent.Catalog.Infrastructure<br/>Clovent.Inventory.Infrastructure | Microsoft.NET.Test.Sdk `v17.14.1`<br/>xunit `v2.9.3`<br/>xunit.runner.visualstudio `v3.1.4`<br/>coverlet.collector `v6.0.4`<br/>*+3 more* |
| **Clovent.Restaurant.Infrastructure** | `src/Clovent.Restaurant.Infrastructure/Clovent.Restaurant.Infrastructure.csproj` | `net10.0` | `Library` | No | Clovent.Restaurant<br/>Clovent.Restaurant.Application<br/>Clovent.Platform | Microsoft.EntityFrameworkCore `v10.0.10`<br/>Microsoft.EntityFrameworkCore.SqlServer `v10.0.10`<br/>Microsoft.EntityFrameworkCore.Design `v10.0.10`<br/>Microsoft.Extensions.Configuration.Abstractions `v10.0.10`<br/>*+2 more* |
| **Clovent.Restaurant.Tests** | `src/Clovent.Restaurant.Tests/Clovent.Restaurant.Tests.csproj` | `net10.0` | `Library` | No | Clovent.Restaurant | Microsoft.NET.Test.Sdk `v17.14.1`<br/>xunit `v2.9.3`<br/>xunit.runner.visualstudio `v3.1.4`<br/>coverlet.collector `v6.0.4` |
| **Clovent.Restaurant** | `src/Clovent.Restaurant/Clovent.Restaurant.csproj` | `net10.0` | `Library` | No | Clovent.Domain<br/>Clovent.Identity<br/>Clovent.MasterData<br/>Clovent.Catalog | *None* |

---

## 2. Complete Folder Structure

The following tree documents the complete folder hierarchy across the root workspace and all sub-systems:

```text
D:/Clovent Business Operating System/
├── .claude/                               # IDE / assistant configuration
├── .git/                                  # Git version control metadata (excluded from source analysis)
├── .obsidian/                             # Obsidian knowledgebase configuration & workspace
├── 00 Vision/                             # Architecture & Strategy Vision Documentation
├── 01 Product Strategy/                   # Product Strategy Specifications
├── 02 Business Analysis/                  # Business Analysis & User Journeys
├── 03 SDLC/                               # Software Development Lifecycle Policies
├── 04 UI UX Standards/                    # Design System, Typography, & UI Guidelines
├── 05 Software Architecture/              # Clean Architecture & DDD Blueprint
├── 06 Coding Standards/                   # C# / .NET Conventions & Style Guides
├── 07 Domain Driven Design/               # DDD Tactical Modeling Guidelines
├── 08 Database Design/                    # SQL & Relational Persistence Standards
├── 09 Security/                           # Security, Auth, & Threat Modeling
├── 10 AI Architecture/                    # AI Assistance & Model Integration Architecture
├── 11 Platform Services/                  # Cross-cutting Platform Service Specifications
├── 12 Restaurant POS/                     # Restaurant POS Domain & Workflow Documentation
├── 13 ADR/                                # Architecture Decision Records
├── Assets/                                # Static branding and UI design assets
├── backups/                               # Manual and automated snapshot backup archives
│   ├── config-security-fix-2026-08-17/
│   ├── logging-2026-08-17/
│   └── rootcause-2026-08-18/
├── Diagrams/                              # Architecture Diagrams (PlantUML, Mermaid, Visio)
├── docs/                                  # Implementation guides, testing reports & specs
├── Foundation/                            # System baseline & architectural foundation docs
├── Images/                                # System screenshots, diagrams, and icon assets
├── qa/                                    # Automated and semi-automated UI acceptance test harness
│   └── live_ui/                           # Interactive UI automation scripts & verification captures
├── scratch/                               # Developer sandboxes, test scripts & diagnostics
│   └── DesignerTest/                      # Standalone WinForms Designer test project
├── src/                                   # Primary Production Source Tree
│   ├── Clovent.Domain/                    # Enterprise Core Entities, Value Objects & Domain Events
│   ├── Clovent.Domain.Tests/              # Domain Unit Tests
│   ├── Clovent.Platform/                  # Cross-cutting Hosting, Diagnostics, Pipeline Infrastructure
│   ├── Clovent.Platform.Tests/            # Platform Services Unit Tests
│   ├── Clovent.Authentication/            # Authentication Core Domain
│   ├── Clovent.Authentication.Application/# Authentication Handlers, Commands, DTOs
│   ├── Clovent.Authentication.Infrastructure/ # EF Core Auth DbContext, Repositories, Migrations
│   ├── Clovent.Authentication.Tests/      # Authentication Domain Unit Tests
│   ├── Clovent.Authentication.Application.Tests/ # Auth Application Handler Tests
│   ├── Clovent.Authentication.Infrastructure.Tests/ # Auth SQLite/EF Integration Tests
│   ├── Clovent.Identity/                  # Identity Domain (Users, Roles, Permissions)
│   ├── Clovent.Identity.Application/      # Identity CQRS Handlers, Queries, Commands
│   ├── Clovent.Identity.Infrastructure/   # Identity EF Core Persistence, Migrations, Seed Tasks
│   ├── Clovent.Identity.Tests/            # Identity Domain Unit Tests
│   ├── Clovent.Identity.Application.Tests/# Identity Application Layer Tests
│   ├── Clovent.Identity.Infrastructure.Tests/# Identity DB Persistence Integration Tests
│   ├── Clovent.MasterData/                # Master Data Domain (Taxes, Currencies, Payment Modes)
│   ├── Clovent.MasterData.Application/    # Master Data CQRS Pipeline
│   ├── Clovent.MasterData.Infrastructure/ # Master Data EF Core Implementation
│   ├── Clovent.MasterData.Tests/          # Master Data Unit Tests
│   ├── Clovent.MasterData.Application.Tests/# Master Data Application Tests
│   ├── Clovent.MasterData.Infrastructure.Tests/# Master Data Integration Tests
│   ├── Clovent.Catalog/                   # Catalog Domain (Products, Categories, Modifiers, Prices)
│   ├── Clovent.Catalog.Application/       # Catalog CQRS Commands, Handlers, DTOs
│   ├── Clovent.Catalog.Infrastructure/    # Catalog EF Core DbContext, Repositories, Migrations
│   ├── Clovent.Catalog.Tests/             # Catalog Domain Tests
│   ├── Clovent.Catalog.Application.Tests/ # Catalog Application Handler Tests
│   ├── Clovent.Catalog.Infrastructure.Tests/ # Catalog Persistence Tests
│   ├── Clovent.Inventory/                 # Inventory Domain (Stocks, Warehouses, Batches, Adjustments)
│   ├── Clovent.Inventory.Application/     # Inventory CQRS Handlers & Workflows
│   ├── Clovent.Inventory.Infrastructure/  # Inventory EF Core DbContext & Repositories
│   ├── Clovent.Inventory.Tests/           # Inventory Domain Tests
│   ├── Clovent.Inventory.Application.Tests/# Inventory Application Tests
│   ├── Clovent.Inventory.Infrastructure.Tests/# Inventory Persistence Tests
│   ├── Clovent.Restaurant/                # Restaurant Domain (Orders, Tables, Shifts, Customers, Deals)
│   ├── Clovent.Restaurant.Application/    # Restaurant CQRS Commands, Queries, Order Health, Pulse
│   ├── Clovent.Restaurant.Infrastructure/ # Restaurant EF Core DbContext, Migrations, Repositories
│   ├── Clovent.Restaurant.Tests/          # Restaurant Domain Unit Tests
│   ├── Clovent.Restaurant.Application.Tests/ # Restaurant CQRS Handler Unit Tests
│   ├── Clovent.Restaurant.Infrastructure.Tests/ # Restaurant DB Persistence & Switch Integration Tests
│   ├── Clovent.Desktop/                   # Desktop Presentation Layer (WinForms + DevExpress)
│   │   ├── Controls/                      # Custom POS controls, keypad, receipt viewers
│   │   ├── DependencyInjection/           # DI bootstrapper & module registration
│   │   ├── Forms/                         # WinForms Screens (Auth, Shell, Users, MasterData)
│   │   ├── Login/                         # Desktop Login Orchestration & PIN Authentication
│   │   ├── Restaurant/                    # POS UI Subsystems
│   │   │   ├── Customers/                 # Customer Management & Search WinForms
│   │   │   ├── Orders/                    # RestaurantPosForm, Recall, Split, Transfer, Running Orders
│   │   │   ├── Shifts/                    # Open/Close Shift, Cash Movement Dialogs
│   │   │   ├── SmartPos/                  # Quick Orders, Health Settings, Recommendations Views
│   │   │   ├── Tables/                    # Visual Table Layout & Management Views
│   │   │   └── Shared/                    # Prompts, Manager Auth, Text Dialogs
│   │   └── Seed/                          # Dev/Test Startup Data Initializers
│   ├── Clovent.Desktop.Tests/             # Desktop UI & POS Logic Tests
│   └── Clovent.Desktop.UiQa/              # Live UI Smoke Test Runner Executable
├── Templates/                             # Scaffolding and Architecture Templates
└── Tools/                                 # CLI Tooling and Module Generators
    └── Clovent.CLI/
        ├── Clovent.CLI/                   # Spectre.Console CLI Tool
        ├── Clovent.CBOS.Desktop/          # Prototype Desktop Host
        ├── Clovent.PackageManager/        # Package Management CLI Host
        ├── Clovent.PackageManager.Abstractions/
        ├── Clovent.PackageManager.Core/
        ├── Clovent.PackageManager.Tests/
        ├── src/                           # Shared CLI generator libraries
        │   ├── Clovent.Configuration/
        │   ├── Clovent.Core/
        │   ├── Clovent.Documents/
        │   ├── Clovent.Generator/
        │   ├── Clovent.Modules.Identity/
        │   ├── Clovent.Shared/
        │   └── Clovent.Templates/
        └── tests/                         # CLI Unit Tests
            ├── Clovent.Core.Tests/
            └── Clovent.Generator.Tests/
```

---

## 3. Every Single File (Comprehensive Active File Inventory)

This section catalogues active non-build files in the repository (excluding auto-generated `bin/`, `obj/`, `.vs/`, and `.git/` binaries which are detailed in Section 4).

### 3.1 Solution Root Files

| File Name | Path | Type | Size | Purpose | Used? | Dependencies / References |
|---|---|---|---|---|---|---|
| `.gitignore` | `.gitignore` | `` | 435 B | Git ignore rules | Yes | Root configuration / diagnostics |
| `Clovent Business Operating System.md` | `Clovent Business Operating System.md` | `.md` | 204 B | Acceptance / Audit Report | Documentation | Root configuration / diagnostics |
| `Clovent.BusinessOperatingSystem.slnx` | `Clovent.BusinessOperatingSystem.slnx` | `.slnx` | 3,969 B | Primary Solution File | Yes | Root configuration / diagnostics |
| `PROJECT_STRUCTURE_REPORT.md` | `PROJECT_STRUCTURE_REPORT.md` | `.md` | 179,563 B | Acceptance / Audit Report | Documentation | Root configuration / diagnostics |
| `Restaurant_POS_Live_UI_Acceptance_Report.md` | `Restaurant_POS_Live_UI_Acceptance_Report.md` | `.md` | 6,622 B | Acceptance / Audit Report | Documentation | Root configuration / diagnostics |
| `Restaurant_POS_Menu_Category_Assignment_Report.md` | `Restaurant_POS_Menu_Category_Assignment_Report.md` | `.md` | 4,906 B | Acceptance / Audit Report | Documentation | Root configuration / diagnostics |
| `Restaurant_POS_Menu_Category_Count_Discrepancy_Report.md` | `Restaurant_POS_Menu_Category_Count_Discrepancy_Report.md` | `.md` | 5,051 B | Acceptance / Audit Report | Documentation | Root configuration / diagnostics |
| `Restaurant_POS_Menu_Category_Data_Synchronization_Report.md` | `Restaurant_POS_Menu_Category_Data_Synchronization_Report.md` | `.md` | 7,730 B | Acceptance / Audit Report | Documentation | Root configuration / diagnostics |
| `Restaurant_POS_Menu_Category_Seed_Regression_Report.md` | `Restaurant_POS_Menu_Category_Seed_Regression_Report.md` | `.md` | 6,811 B | Acceptance / Audit Report | Documentation | Root configuration / diagnostics |
| `Restaurant_POS_Record_Payment_Bug_Report.md` | `Restaurant_POS_Record_Payment_Bug_Report.md` | `.md` | 7,617 B | Acceptance / Audit Report | Documentation | Root configuration / diagnostics |
| `Restaurant_POS_Smart_Intelligence_Implementation_Report.md` | `Restaurant_POS_Smart_Intelligence_Implementation_Report.md` | `.md` | 19,344 B | Acceptance / Audit Report | Documentation | Root configuration / diagnostics |
| `Restaurant_POS_Table_Occupancy_Bug_Report.md` | `Restaurant_POS_Table_Occupancy_Bug_Report.md` | `.md` | 3,168 B | Acceptance / Audit Report | Documentation | Root configuration / diagnostics |
| `cart_diagnostics.txt` | `cart_diagnostics.txt` | `.txt` | 182,842 B | Interactive QA / Live UI Testing Script | No | Root configuration / diagnostics |
| `click.ps1` | `click.ps1` | `.ps1` | 573 B | Interactive QA / Live UI Testing Script | No | Root configuration / diagnostics |
| `enum.ps1` | `enum.ps1` | `.ps1` | 3,129 B | Interactive QA / Live UI Testing Script | No | Root configuration / diagnostics |
| `fg.ps1` | `fg.ps1` | `.ps1` | 561 B | Interactive QA / Live UI Testing Script | No | Root configuration / diagnostics |
| `find.ps1` | `find.ps1` | `.ps1` | 1,466 B | Interactive QA / Live UI Testing Script | No | Root configuration / diagnostics |
| `login.ps1` | `login.ps1` | `.ps1` | 1,865 B | Interactive QA / Live UI Testing Script | No | Root configuration / diagnostics |
| `pclick.ps1` | `pclick.ps1` | `.ps1` | 793 B | Interactive QA / Live UI Testing Script | No | Root configuration / diagnostics |
| `qauia_pos.txt` | `qauia_pos.txt` | `.txt` | 8,192 B | Interactive QA / Live UI Testing Script | No | Root configuration / diagnostics |
| `qauia_pos_tree.txt` | `qauia_pos_tree.txt` | `.txt` | 14,313 B | Interactive QA / Live UI Testing Script | No | Root configuration / diagnostics |
| `qauia_tree.txt` | `qauia_tree.txt` | `.txt` | 40,433 B | Interactive QA / Live UI Testing Script | No | Root configuration / diagnostics |
| `qawin32_pos.txt` | `qawin32_pos.txt` | `.txt` | 30,951 B | Interactive QA / Live UI Testing Script | No | Root configuration / diagnostics |
| `shoot.ps1` | `shoot.ps1` | `.ps1` | 619 B | Interactive QA / Live UI Testing Script | No | Root configuration / diagnostics |
| `shot_login.png` | `shot_login.png` | `.png` | 121,292 B | Interactive QA / Live UI Testing Script | No | Root configuration / diagnostics |
| `shot_login2.png` | `shot_login2.png` | `.png` | 121,292 B | Interactive QA / Live UI Testing Script | No | Root configuration / diagnostics |
| `test_screen.png` | `test_screen.png` | `.png` | 35,902 B | Interactive QA / Live UI Testing Script | No | Root configuration / diagnostics |
| `type.ps1` | `type.ps1` | `.ps1` | 135 B | Interactive QA / Live UI Testing Script | No | Root configuration / diagnostics |
| `uia.ps1` | `uia.ps1` | `.ps1` | 1,299 B | Interactive QA / Live UI Testing Script | No | Root configuration / diagnostics |
| `uia_dump.ps1` | `uia_dump.ps1` | `.ps1` | 1,163 B | Interactive QA / Live UI Testing Script | No | Root configuration / diagnostics |
| `uia_set.ps1` | `uia_set.ps1` | `.ps1` | 1,283 B | Interactive QA / Live UI Testing Script | No | Root configuration / diagnostics |

### 3.2 Presentation Layer (`src/Clovent.Desktop`) Core Files

| File Name | Relative Subpath | Type | Size | Purpose | Used? | Notes / References |
|---|---|---|---|---|---|---|
| `IManagerAuthorizationService.cs` | `Authorization/IManagerAuthorizationService.cs` | `.cs` | 3,524 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `ManagerAuthorizationService.cs` | `Authorization/ManagerAuthorizationService.cs` | `.cs` | 6,195 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `BarcodeCreateForm.Designer.cs` | `Catalog/Barcodes/BarcodeCreateForm.Designer.cs` | `.cs` | 2,844 B | WinForms Visual Designer Code | Yes | Core POS / Shell UI |
| `BarcodeCreateForm.cs` | `Catalog/Barcodes/BarcodeCreateForm.cs` | `.cs` | 1,471 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `BarcodeCreateForm.resx` | `Catalog/Barcodes/BarcodeCreateForm.resx` | `.resx` | 5,627 B | Form Resource XML | Yes | Core POS / Shell UI |
| `BarcodeManagementView.Designer.cs` | `Catalog/Barcodes/BarcodeManagementView.Designer.cs` | `.cs` | 3,190 B | WinForms Visual Designer Code | Yes | Core POS / Shell UI |
| `BarcodeManagementView.cs` | `Catalog/Barcodes/BarcodeManagementView.cs` | `.cs` | 3,246 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `BrandEditForm.Designer.cs` | `Catalog/Brands/BrandEditForm.Designer.cs` | `.cs` | 2,055 B | WinForms Visual Designer Code | Yes | Core POS / Shell UI |
| `BrandEditForm.cs` | `Catalog/Brands/BrandEditForm.cs` | `.cs` | 1,514 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `BrandEditForm.resx` | `Catalog/Brands/BrandEditForm.resx` | `.resx` | 5,627 B | Form Resource XML | Yes | Core POS / Shell UI |
| `BrandManagementView.Designer.cs` | `Catalog/Brands/BrandManagementView.Designer.cs` | `.cs` | 1,778 B | WinForms Visual Designer Code | Yes | Core POS / Shell UI |
| `BrandManagementView.cs` | `Catalog/Brands/BrandManagementView.cs` | `.cs` | 2,675 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `ProductCategoryEditForm.Designer.cs` | `Catalog/Categories/ProductCategoryEditForm.Designer.cs` | `.cs` | 5,195 B | WinForms Visual Designer Code | Yes | Core POS / Shell UI |
| `ProductCategoryEditForm.cs` | `Catalog/Categories/ProductCategoryEditForm.cs` | `.cs` | 3,437 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `ProductCategoryEditForm.resx` | `Catalog/Categories/ProductCategoryEditForm.resx` | `.resx` | 5,627 B | Form Resource XML | Yes | Core POS / Shell UI |
| `ProductCategoryManagementView.Designer.cs` | `Catalog/Categories/ProductCategoryManagementView.Designer.cs` | `.cs` | 1,870 B | WinForms Visual Designer Code | Yes | Core POS / Shell UI |
| `ProductCategoryManagementView.cs` | `Catalog/Categories/ProductCategoryManagementView.cs` | `.cs` | 3,831 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `ProductPriceEditForm.Designer.cs` | `Catalog/Prices/ProductPriceEditForm.Designer.cs` | `.cs` | 4,761 B | WinForms Visual Designer Code | Yes | Core POS / Shell UI |
| `ProductPriceEditForm.cs` | `Catalog/Prices/ProductPriceEditForm.cs` | `.cs` | 3,511 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `ProductPriceEditForm.resx` | `Catalog/Prices/ProductPriceEditForm.resx` | `.resx` | 5,627 B | Form Resource XML | Yes | Core POS / Shell UI |
| `ProductPriceManagementView.Designer.cs` | `Catalog/Prices/ProductPriceManagementView.Designer.cs` | `.cs` | 2,896 B | WinForms Visual Designer Code | Yes | Core POS / Shell UI |
| `ProductPriceManagementView.cs` | `Catalog/Prices/ProductPriceManagementView.cs` | `.cs` | 5,216 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `UnitOfMeasureEditForm.Designer.cs` | `Catalog/UnitsOfMeasure/UnitOfMeasureEditForm.Designer.cs` | `.cs` | 3,116 B | WinForms Visual Designer Code | Yes | Core POS / Shell UI |
| `UnitOfMeasureEditForm.cs` | `Catalog/UnitsOfMeasure/UnitOfMeasureEditForm.cs` | `.cs` | 2,459 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `UnitOfMeasureEditForm.resx` | `Catalog/UnitsOfMeasure/UnitOfMeasureEditForm.resx` | `.resx` | 5,627 B | Form Resource XML | Yes | Core POS / Shell UI |
| `UnitOfMeasureManagementView.Designer.cs` | `Catalog/UnitsOfMeasure/UnitOfMeasureManagementView.Designer.cs` | `.cs` | 1,936 B | WinForms Visual Designer Code | Yes | Core POS / Shell UI |
| `UnitOfMeasureManagementView.cs` | `Catalog/UnitsOfMeasure/UnitOfMeasureManagementView.cs` | `.cs` | 2,862 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `ProductVariantEditForm.Designer.cs` | `Catalog/Variants/ProductVariantEditForm.Designer.cs` | `.cs` | 3,905 B | WinForms Visual Designer Code | Yes | Core POS / Shell UI |
| `ProductVariantEditForm.cs` | `Catalog/Variants/ProductVariantEditForm.cs` | `.cs` | 3,206 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `ProductVariantEditForm.resx` | `Catalog/Variants/ProductVariantEditForm.resx` | `.resx` | 5,627 B | Form Resource XML | Yes | Core POS / Shell UI |
| `ProductVariantManagementView.Designer.cs` | `Catalog/Variants/ProductVariantManagementView.Designer.cs` | `.cs` | 2,890 B | WinForms Visual Designer Code | Yes | Core POS / Shell UI |
| `ProductVariantManagementView.cs` | `Catalog/Variants/ProductVariantManagementView.cs` | `.cs` | 4,549 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `Clovent.Desktop.csproj` | `Clovent.Desktop.csproj` | `.csproj` | 4,866 B | Configuration / Project definition | Yes | Core POS / Shell UI |
| `Clovent.Desktop.csproj.user` | `Clovent.Desktop.csproj.user` | `.user` | 6,637 B | Configuration / Project definition | Yes | Core POS / Shell UI |
| `IdentityUserServiceAdapter.cs` | `Composition/IdentityUserServiceAdapter.cs` | `.cs` | 2,756 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `CatalogDashboardCalculations.cs` | `Dashboard/CatalogDashboardCalculations.cs` | `.cs` | 1,915 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `RestaurantDashboardCalculations.cs` | `Dashboard/RestaurantDashboardCalculations.cs` | `.cs` | 1,764 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `DesktopServiceCollectionExtensions.cs` | `DependencyInjection/DesktopServiceCollectionExtensions.cs` | `.cs` | 9,097 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `AppearanceManager.cs` | `Forms/Base/Appearance/AppearanceManager.cs` | `.cs` | 10,531 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `AppearanceRule.cs` | `Forms/Base/Appearance/AppearanceRule.cs` | `.cs` | 1,414 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `AppearanceScopeType.cs` | `Forms/Base/Appearance/AppearanceScopeType.cs` | `.cs` | 1,364 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `AppearanceSettingsStore.cs` | `Forms/Base/Appearance/AppearanceSettingsStore.cs` | `.cs` | 2,464 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `BaseForm.Designer.cs` | `Forms/Base/BaseForm.Designer.cs` | `.cs` | 7,333 B | WinForms Visual Designer Code | Yes | Core POS / Shell UI |
| `BaseForm.cs` | `Forms/Base/BaseForm.cs` | `.cs` | 9,631 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `BaseForm.resx` | `Forms/Base/BaseForm.resx` | `.resx` | 5,627 B | Form Resource XML | Yes | Core POS / Shell UI |
| `CommandPanelLayout.cs` | `Forms/Base/CommandPanelLayout.cs` | `.cs` | 9,881 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `CurrencyDisplay.cs` | `Forms/Base/CurrencyDisplay.cs` | `.cs` | 2,375 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `CurrencyDisplayLoader.cs` | `Forms/Base/CurrencyDisplayLoader.cs` | `.cs` | 2,080 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `DateTimeDisplay.cs` | `Forms/Base/DateTimeDisplay.cs` | `.cs` | 2,327 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `DateTimeDisplayLoader.cs` | `Forms/Base/DateTimeDisplayLoader.cs` | `.cs` | 3,316 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `DesignModeHelper.cs` | `Forms/Base/DesignModeHelper.cs` | `.cs` | 856 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `DesktopDpi.cs` | `Forms/Base/DesktopDpi.cs` | `.cs` | 857 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `DesktopIcons.cs` | `Forms/Base/DesktopIcons.cs` | `.cs` | 2,136 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `DesktopStyle.cs` | `Forms/Base/DesktopStyle.cs` | `.cs` | 2,734 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `GridSpacer.cs` | `Forms/Base/GridSpacer.cs` | `.cs` | 738 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `GuardedAction.cs` | `Forms/Base/GuardedAction.cs` | `.cs` | 3,013 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `LanguageInitializationStartupTask.cs` | `Forms/Base/Localization/LanguageInitializationStartupTask.cs` | `.cs` | 904 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `LanguagePreferenceStore.cs` | `Forms/Base/Localization/LanguagePreferenceStore.cs` | `.cs` | 1,670 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `LocalizationHelper.cs` | `Forms/Base/Localization/LocalizationHelper.cs` | `.cs` | 8,495 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `PosStrings.cs` | `Forms/Base/Localization/PosStrings.cs` | `.cs` | 5,502 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `PosStrings.resx` | `Forms/Base/Localization/PosStrings.resx` | `.resx` | 8,629 B | Form Resource XML | Yes | Core POS / Shell UI |
| `PosStrings.ur.resx` | `Forms/Base/Localization/PosStrings.ur.resx` | `.resx` | 9,311 B | Form Resource XML | Yes | Core POS / Shell UI |
| `PosSettingsStore.cs` | `Forms/Base/PosSettingsStore.cs` | `.cs` | 5,195 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `ScreenOperationGate.cs` | `Forms/Base/ScreenOperationGate.cs` | `.cs` | 1,577 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `SerializedFeatureAuthorizationPolicy.cs` | `Forms/Base/SerializedFeatureAuthorizationPolicy.cs` | `.cs` | 954 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `SerializedMediator.cs` | `Forms/Base/SerializedMediator.cs` | `.cs` | 4,874 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `StatusBadgeStyler.cs` | `Forms/Base/StatusBadgeStyler.cs` | `.cs` | 2,100 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `WindowPlacementStore.cs` | `Forms/Base/WindowPlacementStore.cs` | `.cs` | 4,383 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `ProductEditForm.Designer.cs` | `Forms/Catalog/Products/ProductEditForm.Designer.cs` | `.cs` | 9,335 B | WinForms Visual Designer Code | Yes | Core POS / Shell UI |
| `ProductEditForm.cs` | `Forms/Catalog/Products/ProductEditForm.cs` | `.cs` | 5,985 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `ProductEditForm.resx` | `Forms/Catalog/Products/ProductEditForm.resx` | `.resx` | 5,627 B | Form Resource XML | Yes | Core POS / Shell UI |
| `ProductsForm.Designer.cs` | `Forms/Catalog/Products/ProductsForm.Designer.cs` | `.cs` | 8,648 B | WinForms Visual Designer Code | Yes | Core POS / Shell UI |
| `ProductsForm.cs` | `Forms/Catalog/Products/ProductsForm.cs` | `.cs` | 15,891 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `ProductsForm.resx` | `Forms/Catalog/Products/ProductsForm.resx` | `.resx` | 5,745 B | Form Resource XML | Yes | Core POS / Shell UI |
| `DashboardView.Designer.cs` | `Forms/Dashboard/DashboardView.Designer.cs` | `.cs` | 61,585 B | WinForms Visual Designer Code | Yes | Core POS / Shell UI |
| `DashboardView.cs` | `Forms/Dashboard/DashboardView.cs` | `.cs` | 16,129 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `DashboardView.resx` | `Forms/Dashboard/DashboardView.resx` | `.resx` | 5,748 B | Form Resource XML | Yes | Core POS / Shell UI |
| `LoginForm.Designer.cs` | `Forms/Identity/LoginForm.Designer.cs` | `.cs` | 37,182 B | WinForms Visual Designer Code | Yes | Core POS / Shell UI |
| `LoginForm.cs` | `Forms/Identity/LoginForm.cs` | `.cs` | 26,663 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `LoginForm.resx` | `Forms/Identity/LoginForm.resx` | `.resx` | 5,748 B | Form Resource XML | Yes | Core POS / Shell UI |
| `RoleEditForm.Designer.cs` | `Forms/Identity/Roles/RoleEditForm.Designer.cs` | `.cs` | 3,791 B | WinForms Visual Designer Code | Yes | Core POS / Shell UI |
| `RoleEditForm.cs` | `Forms/Identity/Roles/RoleEditForm.cs` | `.cs` | 3,546 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `RoleEditForm.resx` | `Forms/Identity/Roles/RoleEditForm.resx` | `.resx` | 5,627 B | Form Resource XML | Yes | Core POS / Shell UI |
| `RolesForm.Designer.cs` | `Forms/Identity/Roles/RolesForm.Designer.cs` | `.cs` | 5,711 B | WinForms Visual Designer Code | Yes | Core POS / Shell UI |
| `RolesForm.cs` | `Forms/Identity/Roles/RolesForm.cs` | `.cs` | 7,726 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `RolesForm.resx` | `Forms/Identity/Roles/RolesForm.resx` | `.resx` | 5,627 B | Form Resource XML | Yes | Core POS / Shell UI |
| `UserEditForm.Designer.cs` | `Forms/Identity/Users/UserEditForm.Designer.cs` | `.cs` | 7,796 B | WinForms Visual Designer Code | Yes | Core POS / Shell UI |
| `UserEditForm.cs` | `Forms/Identity/Users/UserEditForm.cs` | `.cs` | 6,835 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `UserEditForm.resx` | `Forms/Identity/Users/UserEditForm.resx` | `.resx` | 5,627 B | Form Resource XML | Yes | Core POS / Shell UI |
| `UsersForm.Designer.cs` | `Forms/Identity/Users/UsersForm.Designer.cs` | `.cs` | 10,253 B | WinForms Visual Designer Code | Yes | Core POS / Shell UI |
| `UsersForm.cs` | `Forms/Identity/Users/UsersForm.cs` | `.cs` | 20,112 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `UsersForm.resx` | `Forms/Identity/Users/UsersForm.resx` | `.resx` | 5,627 B | Form Resource XML | Yes | Core POS / Shell UI |
| `ActivityLogView.Designer.cs` | `Forms/Restaurant/ActivityLog/ActivityLogView.Designer.cs` | `.cs` | 7,914 B | WinForms Visual Designer Code | Yes | Core POS / Shell UI |
| `ActivityLogView.cs` | `Forms/Restaurant/ActivityLog/ActivityLogView.cs` | `.cs` | 6,198 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `ActivityLogView.resx` | `Forms/Restaurant/ActivityLog/ActivityLogView.resx` | `.resx` | 5,817 B | Form Resource XML | Yes | Core POS / Shell UI |
| `AppearanceRuleEditForm.Designer.cs` | `Forms/Restaurant/Appearance/AppearanceRuleEditForm.Designer.cs` | `.cs` | 13,794 B | WinForms Visual Designer Code | Yes | Core POS / Shell UI |
| `AppearanceRuleEditForm.cs` | `Forms/Restaurant/Appearance/AppearanceRuleEditForm.cs` | `.cs` | 8,986 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `AppearanceRuleEditForm.resx` | `Forms/Restaurant/Appearance/AppearanceRuleEditForm.resx` | `.resx` | 5,627 B | Form Resource XML | Yes | Core POS / Shell UI |
| `AppearanceSettingsView.Designer.cs` | `Forms/Restaurant/Appearance/AppearanceSettingsView.Designer.cs` | `.cs` | 3,510 B | WinForms Visual Designer Code | Yes | Core POS / Shell UI |
| `AppearanceSettingsView.cs` | `Forms/Restaurant/Appearance/AppearanceSettingsView.cs` | `.cs` | 6,065 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `CategoryColorDialog.Designer.cs` | `Forms/Restaurant/MenuItems/CategoryColorDialog.Designer.cs` | `.cs` | 3,997 B | WinForms Visual Designer Code | Yes | Core POS / Shell UI |
| `CategoryColorDialog.cs` | `Forms/Restaurant/MenuItems/CategoryColorDialog.cs` | `.cs` | 2,317 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `CategoryColorDialog.resx` | `Forms/Restaurant/MenuItems/CategoryColorDialog.resx` | `.resx` | 5,627 B | Form Resource XML | Yes | Core POS / Shell UI |
| `IMenuItemsChangeNotifier.cs` | `Forms/Restaurant/MenuItems/IMenuItemsChangeNotifier.cs` | `.cs` | 1,350 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `MenuItemEditForm.Designer.cs` | `Forms/Restaurant/MenuItems/MenuItemEditForm.Designer.cs` | `.cs` | 17,243 B | WinForms Visual Designer Code | Yes | Core POS / Shell UI |
| `MenuItemEditForm.cs` | `Forms/Restaurant/MenuItems/MenuItemEditForm.cs` | `.cs` | 17,448 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `MenuItemEditForm.resx` | `Forms/Restaurant/MenuItems/MenuItemEditForm.resx` | `.resx` | 5,627 B | Form Resource XML | Yes | Core POS / Shell UI |
| `MenuItemImageStore.cs` | `Forms/Restaurant/MenuItems/MenuItemImageStore.cs` | `.cs` | 4,871 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `MenuItemsForm.Designer.cs` | `Forms/Restaurant/MenuItems/MenuItemsForm.Designer.cs` | `.cs` | 14,750 B | WinForms Visual Designer Code | Yes | Core POS / Shell UI |
| `MenuItemsForm.cs` | `Forms/Restaurant/MenuItems/MenuItemsForm.cs` | `.cs` | 39,458 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `PaymentMethodEditForm.Designer.cs` | `Forms/Restaurant/Setup/PaymentMethodEditForm.Designer.cs` | `.cs` | 2,066 B | WinForms Visual Designer Code | Yes | Core POS / Shell UI |
| `PaymentMethodEditForm.cs` | `Forms/Restaurant/Setup/PaymentMethodEditForm.cs` | `.cs` | 1,628 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `PaymentMethodEditForm.resx` | `Forms/Restaurant/Setup/PaymentMethodEditForm.resx` | `.resx` | 5,627 B | Form Resource XML | Yes | Core POS / Shell UI |
| `PaymentMethodsView.Designer.cs` | `Forms/Restaurant/Setup/PaymentMethodsView.Designer.cs` | `.cs` | 1,666 B | WinForms Visual Designer Code | Yes | Core POS / Shell UI |
| `PaymentMethodsView.cs` | `Forms/Restaurant/Setup/PaymentMethodsView.cs` | `.cs` | 5,774 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `PaymentMethodsView.resx` | `Forms/Restaurant/Setup/PaymentMethodsView.resx` | `.resx` | 5,817 B | Form Resource XML | Yes | Core POS / Shell UI |
| `RestaurantSetupView.Designer.cs` | `Forms/Restaurant/Setup/RestaurantSetupView.Designer.cs` | `.cs` | 16,322 B | WinForms Visual Designer Code | Yes | Core POS / Shell UI |
| `RestaurantSetupView.cs` | `Forms/Restaurant/Setup/RestaurantSetupView.cs` | `.cs` | 11,402 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `RestaurantSetupView.resx` | `Forms/Restaurant/Setup/RestaurantSetupView.resx` | `.resx` | 5,817 B | Form Resource XML | Yes | Core POS / Shell UI |
| `IWorkspaceHost.cs` | `Forms/Shell/IWorkspaceHost.cs` | `.cs` | 1,118 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `MainForm.Designer.cs` | `Forms/Shell/MainForm.Designer.cs` | `.cs` | 14,085 B | WinForms Visual Designer Code | Yes | Core POS / Shell UI |
| `MainForm.cs` | `Forms/Shell/MainForm.cs` | `.cs` | 20,077 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `MainForm.resx` | `Forms/Shell/MainForm.resx` | `.resx` | 5,627 B | Form Resource XML | Yes | Core POS / Shell UI |
| `PasswordPromptForm.Designer.cs` | `Identity/Users/PasswordPromptForm.Designer.cs` | `.cs` | 2,313 B | WinForms Visual Designer Code | Yes | Core POS / Shell UI |
| `PasswordPromptForm.cs` | `Identity/Users/PasswordPromptForm.cs` | `.cs` | 2,434 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `PasswordPromptForm.resx` | `Identity/Users/PasswordPromptForm.resx` | `.resx` | 5,627 B | Form Resource XML | Yes | Core POS / Shell UI |
| `PinPromptForm.Designer.cs` | `Identity/Users/PinPromptForm.Designer.cs` | `.cs` | 1,775 B | WinForms Visual Designer Code | Yes | Core POS / Shell UI |
| `PinPromptForm.cs` | `Identity/Users/PinPromptForm.cs` | `.cs` | 2,021 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `StockAdjustmentCreateForm.Designer.cs` | `Inventory/Adjustments/StockAdjustmentCreateForm.Designer.cs` | `.cs` | 5,598 B | WinForms Visual Designer Code | Yes | Core POS / Shell UI |
| `StockAdjustmentCreateForm.cs` | `Inventory/Adjustments/StockAdjustmentCreateForm.cs` | `.cs` | 3,018 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `StockAdjustmentCreateForm.resx` | `Inventory/Adjustments/StockAdjustmentCreateForm.resx` | `.resx` | 5,626 B | Form Resource XML | Yes | Core POS / Shell UI |
| `StockAdjustmentManagementView.Designer.cs` | `Inventory/Adjustments/StockAdjustmentManagementView.Designer.cs` | `.cs` | 2,822 B | WinForms Visual Designer Code | Yes | Core POS / Shell UI |
| `StockAdjustmentManagementView.cs` | `Inventory/Adjustments/StockAdjustmentManagementView.cs` | `.cs` | 4,198 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `InventoryTransactionsView.Designer.cs` | `Inventory/Transactions/InventoryTransactionsView.Designer.cs` | `.cs` | 2,922 B | WinForms Visual Designer Code | Yes | Core POS / Shell UI |
| `InventoryTransactionsView.cs` | `Inventory/Transactions/InventoryTransactionsView.cs` | `.cs` | 5,201 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `StockTransferCreateForm.Designer.cs` | `Inventory/Transfers/StockTransferCreateForm.Designer.cs` | `.cs` | 5,521 B | WinForms Visual Designer Code | Yes | Core POS / Shell UI |
| `StockTransferCreateForm.cs` | `Inventory/Transfers/StockTransferCreateForm.cs` | `.cs` | 3,510 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `StockTransferCreateForm.resx` | `Inventory/Transfers/StockTransferCreateForm.resx` | `.resx` | 5,817 B | Form Resource XML | Yes | Core POS / Shell UI |
| `StockTransferManagementView.Designer.cs` | `Inventory/Transfers/StockTransferManagementView.Designer.cs` | `.cs` | 2,078 B | WinForms Visual Designer Code | Yes | Core POS / Shell UI |
| `StockTransferManagementView.cs` | `Inventory/Transfers/StockTransferManagementView.cs` | `.cs` | 5,560 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `QuantityPromptForm.Designer.cs` | `Inventory/WarehouseStocks/QuantityPromptForm.Designer.cs` | `.cs` | 2,164 B | WinForms Visual Designer Code | Yes | Core POS / Shell UI |
| `QuantityPromptForm.cs` | `Inventory/WarehouseStocks/QuantityPromptForm.cs` | `.cs` | 1,585 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `QuantityPromptForm.resx` | `Inventory/WarehouseStocks/QuantityPromptForm.resx` | `.resx` | 5,817 B | Form Resource XML | Yes | Core POS / Shell UI |
| `ReceiveInventoryForm.Designer.cs` | `Inventory/WarehouseStocks/ReceiveInventoryForm.Designer.cs` | `.cs` | 5,473 B | WinForms Visual Designer Code | Yes | Core POS / Shell UI |
| `ReceiveInventoryForm.cs` | `Inventory/WarehouseStocks/ReceiveInventoryForm.cs` | `.cs` | 3,597 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `ReceiveInventoryForm.resx` | `Inventory/WarehouseStocks/ReceiveInventoryForm.resx` | `.resx` | 5,626 B | Form Resource XML | Yes | Core POS / Shell UI |
| `WarehouseStockEditForm.Designer.cs` | `Inventory/WarehouseStocks/WarehouseStockEditForm.Designer.cs` | `.cs` | 5,321 B | WinForms Visual Designer Code | Yes | Core POS / Shell UI |
| `WarehouseStockEditForm.cs` | `Inventory/WarehouseStocks/WarehouseStockEditForm.cs` | `.cs` | 3,503 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `WarehouseStockEditForm.resx` | `Inventory/WarehouseStocks/WarehouseStockEditForm.resx` | `.resx` | 5,626 B | Form Resource XML | Yes | Core POS / Shell UI |
| `WarehouseStockManagementView.Designer.cs` | `Inventory/WarehouseStocks/WarehouseStockManagementView.Designer.cs` | `.cs` | 4,284 B | WinForms Visual Designer Code | Yes | Core POS / Shell UI |
| `WarehouseStockManagementView.cs` | `Inventory/WarehouseStocks/WarehouseStockManagementView.cs` | `.cs` | 8,736 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `ILoginService.cs` | `Login/ILoginService.cs` | `.cs` | 1,867 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `LoginService.cs` | `Login/LoginService.cs` | `.cs` | 9,990 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `BranchEditForm.Designer.cs` | `MasterData/Branches/BranchEditForm.Designer.cs` | `.cs` | 4,720 B | WinForms Visual Designer Code | Yes | Core POS / Shell UI |
| `BranchEditForm.cs` | `MasterData/Branches/BranchEditForm.cs` | `.cs` | 2,892 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `BranchEditForm.resx` | `MasterData/Branches/BranchEditForm.resx` | `.resx` | 5,745 B | Form Resource XML | Yes | Core POS / Shell UI |
| `BranchManagementView.Designer.cs` | `MasterData/Branches/BranchManagementView.Designer.cs` | `.cs` | 2,067 B | WinForms Visual Designer Code | Yes | Core POS / Shell UI |
| `BranchManagementView.cs` | `MasterData/Branches/BranchManagementView.cs` | `.cs` | 3,891 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `ComboBoxBinder.cs` | `MasterData/ComboBoxBinder.cs` | `.cs` | 2,898 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `CompanyEditForm.Designer.cs` | `MasterData/Companies/CompanyEditForm.Designer.cs` | `.cs` | 3,104 B | WinForms Visual Designer Code | Yes | Core POS / Shell UI |
| `CompanyEditForm.cs` | `MasterData/Companies/CompanyEditForm.cs` | `.cs` | 1,753 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `CompanyEditForm.resx` | `MasterData/Companies/CompanyEditForm.resx` | `.resx` | 5,627 B | Form Resource XML | Yes | Core POS / Shell UI |
| `CompanyManagementView.Designer.cs` | `MasterData/Companies/CompanyManagementView.Designer.cs` | `.cs` | 1,821 B | WinForms Visual Designer Code | Yes | Core POS / Shell UI |
| `CompanyManagementView.cs` | `MasterData/Companies/CompanyManagementView.cs` | `.cs` | 3,791 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `CurrencyCreateForm.Designer.cs` | `MasterData/Currencies/CurrencyCreateForm.Designer.cs` | `.cs` | 5,431 B | WinForms Visual Designer Code | Yes | Core POS / Shell UI |
| `CurrencyCreateForm.cs` | `MasterData/Currencies/CurrencyCreateForm.cs` | `.cs` | 1,756 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `CurrencyCreateForm.resx` | `MasterData/Currencies/CurrencyCreateForm.resx` | `.resx` | 5,627 B | Form Resource XML | Yes | Core POS / Shell UI |
| `CurrencyManagementView.Designer.cs` | `MasterData/Currencies/CurrencyManagementView.Designer.cs` | `.cs` | 1,834 B | WinForms Visual Designer Code | Yes | Core POS / Shell UI |
| `CurrencyManagementView.cs` | `MasterData/Currencies/CurrencyManagementView.cs` | `.cs` | 6,788 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `DepartmentEditForm.Designer.cs` | `MasterData/Departments/DepartmentEditForm.Designer.cs` | `.cs` | 2,073 B | WinForms Visual Designer Code | Yes | Core POS / Shell UI |
| `DepartmentEditForm.cs` | `MasterData/Departments/DepartmentEditForm.cs` | `.cs` | 1,478 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `DepartmentEditForm.resx` | `MasterData/Departments/DepartmentEditForm.resx` | `.resx` | 5,627 B | Form Resource XML | Yes | Core POS / Shell UI |
| `DepartmentManagementView.Designer.cs` | `MasterData/Departments/DepartmentManagementView.Designer.cs` | `.cs` | 1,795 B | WinForms Visual Designer Code | Yes | Core POS / Shell UI |
| `DepartmentManagementView.cs` | `MasterData/Departments/DepartmentManagementView.cs` | `.cs` | 3,596 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `EntityPicker.Designer.cs` | `MasterData/EntityPicker.Designer.cs` | `.cs` | 3,411 B | WinForms Visual Designer Code | Yes | Core POS / Shell UI |
| `EntityPicker.cs` | `MasterData/EntityPicker.cs` | `.cs` | 6,154 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `FiscalYearEditForm.Designer.cs` | `MasterData/FiscalYears/FiscalYearEditForm.Designer.cs` | `.cs` | 4,217 B | WinForms Visual Designer Code | Yes | Core POS / Shell UI |
| `FiscalYearEditForm.cs` | `MasterData/FiscalYears/FiscalYearEditForm.cs` | `.cs` | 2,887 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `FiscalYearEditForm.resx` | `MasterData/FiscalYears/FiscalYearEditForm.resx` | `.resx` | 5,627 B | Form Resource XML | Yes | Core POS / Shell UI |
| `FiscalYearManagementView.Designer.cs` | `MasterData/FiscalYears/FiscalYearManagementView.Designer.cs` | `.cs` | 1,829 B | WinForms Visual Designer Code | Yes | Core POS / Shell UI |
| `FiscalYearManagementView.cs` | `MasterData/FiscalYears/FiscalYearManagementView.cs` | `.cs` | 3,906 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `MasterDataColumn.cs` | `MasterData/MasterDataColumn.cs` | `.cs` | 656 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `MasterDataEditFormBase.Designer.cs` | `MasterData/MasterDataEditFormBase.Designer.cs` | `.cs` | 4,824 B | WinForms Visual Designer Code | Yes | Core POS / Shell UI |
| `MasterDataEditFormBase.cs` | `MasterData/MasterDataEditFormBase.cs` | `.cs` | 14,130 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `MasterDataEditFormBase.resx` | `MasterData/MasterDataEditFormBase.resx` | `.resx` | 5,817 B | Form Resource XML | Yes | Core POS / Shell UI |
| `MasterDataFilter.cs` | `MasterData/MasterDataFilter.cs` | `.cs` | 2,286 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `MasterDataListView.cs` | `MasterData/MasterDataListView.cs` | `.cs` | 31,638 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `OrganizationHierarchySelector.Designer.cs` | `MasterData/OrganizationHierarchySelector.Designer.cs` | `.cs` | 4,522 B | WinForms Visual Designer Code | Yes | Core POS / Shell UI |
| `OrganizationHierarchySelector.cs` | `MasterData/OrganizationHierarchySelector.cs` | `.cs` | 7,492 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `OrganizationEditForm.Designer.cs` | `MasterData/Organizations/OrganizationEditForm.Designer.cs` | `.cs` | 3,118 B | WinForms Visual Designer Code | Yes | Core POS / Shell UI |
| `OrganizationEditForm.cs` | `MasterData/Organizations/OrganizationEditForm.cs` | `.cs` | 1,815 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `OrganizationEditForm.resx` | `MasterData/Organizations/OrganizationEditForm.resx` | `.resx` | 5,627 B | Form Resource XML | Yes | Core POS / Shell UI |
| `OrganizationManagementView.Designer.cs` | `MasterData/Organizations/OrganizationManagementView.Designer.cs` | `.cs` | 1,616 B | WinForms Visual Designer Code | Yes | Core POS / Shell UI |
| `OrganizationManagementView.cs` | `MasterData/Organizations/OrganizationManagementView.cs` | `.cs` | 3,287 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `BusinessSettingsManagementView.Designer.cs` | `MasterData/Settings/BusinessSettingsManagementView.Designer.cs` | `.cs` | 6,579 B | WinForms Visual Designer Code | Yes | Core POS / Shell UI |
| `BusinessSettingsManagementView.cs` | `MasterData/Settings/BusinessSettingsManagementView.cs` | `.cs` | 11,928 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `TerminalEditForm.Designer.cs` | `MasterData/Terminals/TerminalEditForm.Designer.cs` | `.cs` | 3,094 B | WinForms Visual Designer Code | Yes | Core POS / Shell UI |
| `TerminalEditForm.cs` | `MasterData/Terminals/TerminalEditForm.cs` | `.cs` | 2,232 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `TerminalEditForm.resx` | `MasterData/Terminals/TerminalEditForm.resx` | `.resx` | 5,627 B | Form Resource XML | Yes | Core POS / Shell UI |
| `TerminalManagementView.Designer.cs` | `MasterData/Terminals/TerminalManagementView.Designer.cs` | `.cs` | 1,828 B | WinForms Visual Designer Code | Yes | Core POS / Shell UI |
| `TerminalManagementView.cs` | `MasterData/Terminals/TerminalManagementView.cs` | `.cs` | 3,530 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `WarehouseEditForm.Designer.cs` | `MasterData/Warehouses/WarehouseEditForm.Designer.cs` | `.cs` | 3,097 B | WinForms Visual Designer Code | Yes | Core POS / Shell UI |
| `WarehouseEditForm.cs` | `MasterData/Warehouses/WarehouseEditForm.cs` | `.cs` | 2,313 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `WarehouseEditForm.resx` | `MasterData/Warehouses/WarehouseEditForm.resx` | `.resx` | 5,627 B | Form Resource XML | Yes | Core POS / Shell UI |
| `WarehouseManagementView.Designer.cs` | `MasterData/Warehouses/WarehouseManagementView.Designer.cs` | `.cs` | 1,839 B | WinForms Visual Designer Code | Yes | Core POS / Shell UI |
| `WarehouseManagementView.cs` | `MasterData/Warehouses/WarehouseManagementView.cs` | `.cs` | 3,553 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `DesktopModuleCatalog.cs` | `Modules/DesktopModuleCatalog.cs` | `.cs` | 881 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `DesktopModuleLoader.cs` | `Modules/DesktopModuleLoader.cs` | `.cs` | 2,019 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `INavigationService.cs` | `Navigation/INavigationService.cs` | `.cs` | 2,050 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `NavigationMenuBuilder.cs` | `Navigation/NavigationMenuBuilder.cs` | `.cs` | 1,198 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `NavigationService.cs` | `Navigation/NavigationService.cs` | `.cs` | 3,133 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `INotificationService.cs` | `Notifications/INotificationService.cs` | `.cs` | 653 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `Notification.cs` | `Notifications/Notification.cs` | `.cs` | 228 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `NotificationService.cs` | `Notifications/NotificationService.cs` | `.cs` | 939 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `NotificationsForm.Designer.cs` | `Notifications/NotificationsForm.Designer.cs` | `.cs` | 1,535 B | WinForms Visual Designer Code | Yes | Core POS / Shell UI |
| `NotificationsForm.cs` | `Notifications/NotificationsForm.cs` | `.cs` | 1,369 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `NotificationsForm.resx` | `Notifications/NotificationsForm.resx` | `.resx` | 5,627 B | Form Resource XML | Yes | Core POS / Shell UI |
| `Program.cs` | `Program.cs` | `.cs` | 17,779 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `launchSettings.json` | `Properties/launchSettings.json` | `.json` | 249 B | Configuration / Project definition | Yes | Core POS / Shell UI |
| `CustomerEditForm.Designer.cs` | `Restaurant/Customers/CustomerEditForm.Designer.cs` | `.cs` | 14,589 B | WinForms Visual Designer Code | Yes | Core POS / Shell UI |
| `CustomerEditForm.cs` | `Restaurant/Customers/CustomerEditForm.cs` | `.cs` | 5,553 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `CustomerEditForm.resx` | `Restaurant/Customers/CustomerEditForm.resx` | `.resx` | 2,794 B | Form Resource XML | Yes | Core POS / Shell UI |
| `CustomerLedgerDialog.Designer.cs` | `Restaurant/Customers/CustomerLedgerDialog.Designer.cs` | `.cs` | 21,323 B | WinForms Visual Designer Code | Yes | Core POS / Shell UI |
| `CustomerLedgerDialog.cs` | `Restaurant/Customers/CustomerLedgerDialog.cs` | `.cs` | 9,934 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `CustomerLedgerDialog.resx` | `Restaurant/Customers/CustomerLedgerDialog.resx` | `.resx` | 2,794 B | Form Resource XML | Yes | Core POS / Shell UI |
| `CustomerPaymentForm.Designer.cs` | `Restaurant/Customers/CustomerPaymentForm.Designer.cs` | `.cs` | 10,600 B | WinForms Visual Designer Code | Yes | Core POS / Shell UI |
| `CustomerPaymentForm.cs` | `Restaurant/Customers/CustomerPaymentForm.cs` | `.cs` | 5,446 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `CustomerPaymentForm.resx` | `Restaurant/Customers/CustomerPaymentForm.resx` | `.resx` | 5,817 B | Form Resource XML | Yes | Core POS / Shell UI |
| `CustomersView.Designer.cs` | `Restaurant/Customers/CustomersView.Designer.cs` | `.cs` | 16,258 B | WinForms Visual Designer Code | Yes | Core POS / Shell UI |
| `CustomersView.cs` | `Restaurant/Customers/CustomersView.cs` | `.cs` | 26,045 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `CustomersView.resx` | `Restaurant/Customers/CustomersView.resx` | `.resx` | 2,794 B | Form Resource XML | Yes | Core POS / Shell UI |
| `DiningAreaEditForm.Designer.cs` | `Restaurant/DiningAreas/DiningAreaEditForm.Designer.cs` | `.cs` | 2,060 B | WinForms Visual Designer Code | Yes | Core POS / Shell UI |
| `DiningAreaEditForm.cs` | `Restaurant/DiningAreas/DiningAreaEditForm.cs` | `.cs` | 1,510 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `DiningAreaEditForm.resx` | `Restaurant/DiningAreas/DiningAreaEditForm.resx` | `.resx` | 5,627 B | Form Resource XML | Yes | Core POS / Shell UI |
| `DiningAreaManagementView.Designer.cs` | `Restaurant/DiningAreas/DiningAreaManagementView.Designer.cs` | `.cs` | 2,637 B | WinForms Visual Designer Code | Yes | Core POS / Shell UI |
| `DiningAreaManagementView.cs` | `Restaurant/DiningAreas/DiningAreaManagementView.cs` | `.cs` | 4,099 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `EndOfDayReportView.Designer.cs` | `Restaurant/EndOfDay/EndOfDayReportView.Designer.cs` | `.cs` | 21,770 B | WinForms Visual Designer Code | Yes | Core POS / Shell UI |
| `EndOfDayReportView.cs` | `Restaurant/EndOfDay/EndOfDayReportView.cs` | `.cs` | 12,636 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `BillSplitDialog.Designer.cs` | `Restaurant/Orders/BillSplitDialog.Designer.cs` | `.cs` | 3,298 B | WinForms Visual Designer Code | Yes | Core POS / Shell UI |
| `BillSplitDialog.cs` | `Restaurant/Orders/BillSplitDialog.cs` | `.cs` | 2,903 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `BillSplitDialog.resx` | `Restaurant/Orders/BillSplitDialog.resx` | `.resx` | 5,627 B | Form Resource XML | Yes | Core POS / Shell UI |
| `CustomerReorderDialogs.cs` | `Restaurant/Orders/CustomerReorderDialogs.cs` | `.cs` | 13,948 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `DiscountDialog.Designer.cs` | `Restaurant/Orders/DiscountDialog.Designer.cs` | `.cs` | 4,503 B | WinForms Visual Designer Code | Yes | Core POS / Shell UI |
| `DiscountDialog.cs` | `Restaurant/Orders/DiscountDialog.cs` | `.cs` | 2,427 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `DiscountDialog.resx` | `Restaurant/Orders/DiscountDialog.resx` | `.resx` | 5,627 B | Form Resource XML | Yes | Core POS / Shell UI |
| `HoldOrdersView.Designer.cs` | `Restaurant/Orders/HoldOrdersView.Designer.cs` | `.cs` | 1,709 B | WinForms Visual Designer Code | Yes | Core POS / Shell UI |
| `HoldOrdersView.cs` | `Restaurant/Orders/HoldOrdersView.cs` | `.cs` | 4,609 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `KitchenTicketViewerView.Designer.cs` | `Restaurant/Orders/KitchenTicketViewerView.Designer.cs` | `.cs` | 2,307 B | WinForms Visual Designer Code | Yes | Core POS / Shell UI |
| `KitchenTicketViewerView.cs` | `Restaurant/Orders/KitchenTicketViewerView.cs` | `.cs` | 5,355 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `MergeTablesDialog.Designer.cs` | `Restaurant/Orders/MergeTablesDialog.Designer.cs` | `.cs` | 3,185 B | WinForms Visual Designer Code | Yes | Core POS / Shell UI |
| `MergeTablesDialog.cs` | `Restaurant/Orders/MergeTablesDialog.cs` | `.cs` | 2,434 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `MergeTablesDialog.resx` | `Restaurant/Orders/MergeTablesDialog.resx` | `.resx` | 5,627 B | Form Resource XML | Yes | Core POS / Shell UI |
| `OrderHistoryRules.cs` | `Restaurant/Orders/OrderHistoryRules.cs` | `.cs` | 1,538 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `OrderHistoryView.Designer.cs` | `Restaurant/Orders/OrderHistoryView.Designer.cs` | `.cs` | 2,117 B | WinForms Visual Designer Code | Yes | Core POS / Shell UI |
| `OrderHistoryView.cs` | `Restaurant/Orders/OrderHistoryView.cs` | `.cs` | 8,553 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `PaymentHistoryDialog.Designer.cs` | `Restaurant/Orders/PaymentHistoryDialog.Designer.cs` | `.cs` | 4,720 B | WinForms Visual Designer Code | Yes | Core POS / Shell UI |
| `PaymentHistoryDialog.cs` | `Restaurant/Orders/PaymentHistoryDialog.cs` | `.cs` | 6,113 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `PaymentHistoryDialog.resx` | `Restaurant/Orders/PaymentHistoryDialog.resx` | `.resx` | 5,817 B | Form Resource XML | Yes | Core POS / Shell UI |
| `PosPaymentMethodPreferenceStore.cs` | `Restaurant/Orders/PosPaymentMethodPreferenceStore.cs` | `.cs` | 1,915 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `PosPaymentRules.cs` | `Restaurant/Orders/PosPaymentRules.cs` | `.cs` | 2,456 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `PriceOverrideDialog.Designer.cs` | `Restaurant/Orders/PriceOverrideDialog.Designer.cs` | `.cs` | 3,909 B | WinForms Visual Designer Code | Yes | Core POS / Shell UI |
| `PriceOverrideDialog.cs` | `Restaurant/Orders/PriceOverrideDialog.cs` | `.cs` | 2,523 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `PriceOverrideDialog.resx` | `Restaurant/Orders/PriceOverrideDialog.resx` | `.resx` | 5,627 B | Form Resource XML | Yes | Core POS / Shell UI |
| `QuickOrderPreviewDialog.cs` | `Restaurant/Orders/QuickOrderPreviewDialog.cs` | `.cs` | 11,136 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `RecallOrderDialog.Designer.cs` | `Restaurant/Orders/RecallOrderDialog.Designer.cs` | `.cs` | 43,437 B | WinForms Visual Designer Code | Yes | Core POS / Shell UI |
| `RecallOrderDialog.cs` | `Restaurant/Orders/RecallOrderDialog.cs` | `.cs` | 32,458 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `ReceiptFormatter.cs` | `Restaurant/Orders/ReceiptFormatter.cs` | `.cs` | 4,035 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `ReceiptPreviewForm.Designer.cs` | `Restaurant/Orders/ReceiptPreviewForm.Designer.cs` | `.cs` | 3,922 B | WinForms Visual Designer Code | Yes | Core POS / Shell UI |
| `ReceiptPreviewForm.cs` | `Restaurant/Orders/ReceiptPreviewForm.cs` | `.cs` | 2,682 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `ReceiptPreviewForm.resx` | `Restaurant/Orders/ReceiptPreviewForm.resx` | `.resx` | 5,745 B | Form Resource XML | Yes | Core POS / Shell UI |
| `ReceiptPrintDocument.cs` | `Restaurant/Orders/ReceiptPrintDocument.cs` | `.cs` | 1,814 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `RestaurantPosDesignDataProvider.cs` | `Restaurant/Orders/RestaurantPosDesignDataProvider.cs` | `.cs` | 4,341 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `RestaurantPosForm.Designer.cs` | `Restaurant/Orders/RestaurantPosForm.Designer.cs` | `.cs` | 107,444 B | WinForms Visual Designer Code | Yes | Core POS / Shell UI |
| `RestaurantPosForm.cs` | `Restaurant/Orders/RestaurantPosForm.cs` | `.cs` | 328,398 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `RestaurantPosForm.resx` | `Restaurant/Orders/RestaurantPosForm.resx` | `.resx` | 6,330 B | Form Resource XML | Yes | Core POS / Shell UI |
| `RestaurantPulseForm.cs` | `Restaurant/Orders/RestaurantPulseForm.cs` | `.cs` | 11,964 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `RunningOrdersView.Designer.cs` | `Restaurant/Orders/RunningOrdersView.Designer.cs` | `.cs` | 2,016 B | WinForms Visual Designer Code | Yes | Core POS / Shell UI |
| `RunningOrdersView.cs` | `Restaurant/Orders/RunningOrdersView.cs` | `.cs` | 5,139 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `ServiceChargeDialog.Designer.cs` | `Restaurant/Orders/ServiceChargeDialog.Designer.cs` | `.cs` | 4,411 B | WinForms Visual Designer Code | Yes | Core POS / Shell UI |
| `ServiceChargeDialog.cs` | `Restaurant/Orders/ServiceChargeDialog.cs` | `.cs` | 2,489 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `ServiceChargeDialog.resx` | `Restaurant/Orders/ServiceChargeDialog.resx` | `.resx` | 5,627 B | Form Resource XML | Yes | Core POS / Shell UI |
| `SmartPosState.cs` | `Restaurant/Orders/SmartPosState.cs` | `.cs` | 4,364 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `SplitPaymentDialog.cs` | `Restaurant/Orders/SplitPaymentDialog.cs` | `.cs` | 10,070 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `TablePickerEdit.cs` | `Restaurant/Orders/TablePickerEdit.cs` | `.cs` | 5,661 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `TableSelectionDineInPolicy.cs` | `Restaurant/Orders/TableSelectionDineInPolicy.cs` | `.cs` | 701 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `TableTransferDialog.Designer.cs` | `Restaurant/Orders/TableTransferDialog.Designer.cs` | `.cs` | 2,034 B | WinForms Visual Designer Code | Yes | Core POS / Shell UI |
| `TableTransferDialog.cs` | `Restaurant/Orders/TableTransferDialog.cs` | `.cs` | 1,733 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `TableTransferDialog.resx` | `Restaurant/Orders/TableTransferDialog.resx` | `.resx` | 5,745 B | Form Resource XML | Yes | Core POS / Shell UI |
| `UniversalSearchDropdown.cs` | `Restaurant/Orders/UniversalSearchDropdown.cs` | `.cs` | 9,340 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `ManagerAuthorizationForm.Designer.cs` | `Restaurant/Shared/ManagerAuthorizationForm.Designer.cs` | `.cs` | 4,591 B | WinForms Visual Designer Code | Yes | Core POS / Shell UI |
| `ManagerAuthorizationForm.cs` | `Restaurant/Shared/ManagerAuthorizationForm.cs` | `.cs` | 2,823 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `SelectionPromptForm.Designer.cs` | `Restaurant/Shared/SelectionPromptForm.Designer.cs` | `.cs` | 2,014 B | WinForms Visual Designer Code | Yes | Core POS / Shell UI |
| `SelectionPromptForm.cs` | `Restaurant/Shared/SelectionPromptForm.cs` | `.cs` | 1,329 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `SelectionPromptForm.resx` | `Restaurant/Shared/SelectionPromptForm.resx` | `.resx` | 5,627 B | Form Resource XML | Yes | Core POS / Shell UI |
| `TextPromptForm.Designer.cs` | `Restaurant/Shared/TextPromptForm.Designer.cs` | `.cs` | 2,060 B | WinForms Visual Designer Code | Yes | Core POS / Shell UI |
| `TextPromptForm.cs` | `Restaurant/Shared/TextPromptForm.cs` | `.cs` | 2,251 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `TextPromptForm.resx` | `Restaurant/Shared/TextPromptForm.resx` | `.resx` | 5,627 B | Form Resource XML | Yes | Core POS / Shell UI |
| `CashMovementDialog.cs` | `Restaurant/Shifts/CashMovementDialog.cs` | `.cs` | 6,694 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `CloseShiftDialog.cs` | `Restaurant/Shifts/CloseShiftDialog.cs` | `.cs` | 13,718 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `OpenShiftDialog.cs` | `Restaurant/Shifts/OpenShiftDialog.cs` | `.cs` | 6,239 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `ShiftDetailDialog.cs` | `Restaurant/Shifts/ShiftDetailDialog.cs` | `.cs` | 8,849 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `ShiftHistoryView.cs` | `Restaurant/Shifts/ShiftHistoryView.cs` | `.cs` | 10,815 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `OrderHealthSettingsForm.cs` | `Restaurant/SmartPos/OrderHealthSettingsForm.cs` | `.cs` | 4,010 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `QuickOrderTemplateEditForm.cs` | `Restaurant/SmartPos/QuickOrderTemplateEditForm.cs` | `.cs` | 14,003 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `QuickOrderTemplatesView.Designer.cs` | `Restaurant/SmartPos/QuickOrderTemplatesView.Designer.cs` | `.cs` | 6,919 B | WinForms Visual Designer Code | Yes | Core POS / Shell UI |
| `QuickOrderTemplatesView.cs` | `Restaurant/SmartPos/QuickOrderTemplatesView.cs` | `.cs` | 11,771 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `RecommendationRuleEditForm.cs` | `Restaurant/SmartPos/RecommendationRuleEditForm.cs` | `.cs` | 9,570 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `RecommendationRulesView.Designer.cs` | `Restaurant/SmartPos/RecommendationRulesView.Designer.cs` | `.cs` | 6,945 B | WinForms Visual Designer Code | Yes | Core POS / Shell UI |
| `RecommendationRulesView.cs` | `Restaurant/SmartPos/RecommendationRulesView.cs` | `.cs` | 13,646 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `SmartPosCatalogOptions.cs` | `Restaurant/SmartPos/SmartPosCatalogOptions.cs` | `.cs` | 3,619 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `TableEditForm.Designer.cs` | `Restaurant/Tables/TableEditForm.Designer.cs` | `.cs` | 4,598 B | WinForms Visual Designer Code | Yes | Core POS / Shell UI |
| `TableEditForm.cs` | `Restaurant/Tables/TableEditForm.cs` | `.cs` | 2,694 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `TableEditForm.resx` | `Restaurant/Tables/TableEditForm.resx` | `.resx` | 5,627 B | Form Resource XML | Yes | Core POS / Shell UI |
| `TableManagementView.Designer.cs` | `Restaurant/Tables/TableManagementView.Designer.cs` | `.cs` | 3,756 B | WinForms Visual Designer Code | Yes | Core POS / Shell UI |
| `TableManagementView.cs` | `Restaurant/Tables/TableManagementView.cs` | `.cs` | 4,168 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `DevelopmentAuthorizationSeedStartupTask.cs` | `Seed/DevelopmentAuthorizationSeedStartupTask.cs` | `.cs` | 9,491 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `DevelopmentCatalogSeedStartupTask.cs` | `Seed/DevelopmentCatalogSeedStartupTask.cs` | `.cs` | 23,834 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `DevelopmentMasterDataSeedStartupTask.cs` | `Seed/DevelopmentMasterDataSeedStartupTask.cs` | `.cs` | 7,375 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `DevelopmentRestaurantSeedStartupTask.cs` | `Seed/DevelopmentRestaurantSeedStartupTask.cs` | `.cs` | 3,877 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `DevelopmentUserSeedStartupTask.cs` | `Seed/DevelopmentUserSeedStartupTask.cs` | `.cs` | 4,271 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `WorldCurrencySeedStartupTask.cs` | `Seed/WorldCurrencySeedStartupTask.cs` | `.cs` | 9,098 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `WorldLanguageSeedStartupTask.cs` | `Seed/WorldLanguageSeedStartupTask.cs` | `.cs` | 1,616 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `WorldTimeZoneSeedStartupTask.cs` | `Seed/WorldTimeZoneSeedStartupTask.cs` | `.cs` | 1,777 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `CurrentSession.cs` | `Sessions/CurrentSession.cs` | `.cs` | 1,174 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `ICurrentSession.cs` | `Sessions/ICurrentSession.cs` | `.cs` | 1,760 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `CsvFile.cs` | `Shared/CsvFile.cs` | `.cs` | 3,319 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `IRecentItemsService.cs` | `Shell/IRecentItemsService.cs` | `.cs` | 1,341 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `RecentItemsService.cs` | `Shell/RecentItemsService.cs` | `.cs` | 1,229 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `ErrorDialogForm.Designer.cs` | `Startup/ErrorDialogForm.Designer.cs` | `.cs` | 4,236 B | WinForms Visual Designer Code | Yes | Core POS / Shell UI |
| `ErrorDialogForm.cs` | `Startup/ErrorDialogForm.cs` | `.cs` | 2,549 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `ErrorDialogForm.resx` | `Startup/ErrorDialogForm.resx` | `.resx` | 5,627 B | Form Resource XML | Yes | Core POS / Shell UI |
| `ErrorDialogService.cs` | `Startup/ErrorDialogService.cs` | `.cs` | 2,550 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `FriendlyErrorText.cs` | `Startup/FriendlyErrorText.cs` | `.cs` | 3,575 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `GlobalExceptionHandler.cs` | `Startup/GlobalExceptionHandler.cs` | `.cs` | 1,931 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `IErrorDialogService.cs` | `Startup/IErrorDialogService.cs` | `.cs` | 549 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `ISplashScreenService.cs` | `Startup/ISplashScreenService.cs` | `.cs` | 547 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `SplashScreenService.cs` | `Startup/SplashScreenService.cs` | `.cs` | 1,390 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `DesktopOptions.cs` | `Theming/DesktopOptions.cs` | `.cs` | 2,711 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `IThemeService.cs` | `Theming/IThemeService.cs` | `.cs` | 864 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `ThemeInitializationStartupTask.cs` | `Theming/ThemeInitializationStartupTask.cs` | `.cs` | 886 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `ThemeService.cs` | `Theming/ThemeService.cs` | `.cs` | 1,787 B | WinForms UI Code-Behind | Yes | Core POS / Shell UI |
| `appsettings.Development.json` | `appsettings.Development.json` | `.json` | 235 B | Configuration / Project definition | Yes | Core POS / Shell UI |
| `appsettings.json` | `appsettings.json` | `.json` | 1,239 B | Configuration / Project definition | Yes | Core POS / Shell UI |

### 3.3 Restaurant Bounded Context (`src/Clovent.Restaurant*`) Core Files

| File Name | Project | Relative Subpath | Type | Size | Purpose | Used? |
|---|---|---|---|---|---|---|
| `RecordActivityCommand.cs` | `Clovent.Restaurant.Application` | `ActivityLogs/Commands/RecordActivityCommand.cs` | `.cs` | 1,385 B | CQRS Command / Query / Handler | Yes |
| `ActivityLogEntryDto.cs` | `Clovent.Restaurant.Application` | `ActivityLogs/Dtos/ActivityLogEntryDto.cs` | `.cs` | 735 B | CQRS Command / Query / Handler | Yes |
| `ListRecentActivityQuery.cs` | `Clovent.Restaurant.Application` | `ActivityLogs/Queries/ListRecentActivityQuery.cs` | `.cs` | 1,000 B | CQRS Command / Query / Handler | Yes |
| `Clovent.Restaurant.Application.csproj` | `Clovent.Restaurant.Application` | `Clovent.Restaurant.Application.csproj` | `.csproj` | 888 B | CQRS Command / Query / Handler | Yes |
| `CustomerReorderDtos.cs` | `Clovent.Restaurant.Application` | `CustomerReorder/Dtos/CustomerReorderDtos.cs` | `.cs` | 628 B | CQRS Command / Query / Handler | Yes |
| `GetCustomerFrequentProductsQuery.cs` | `Clovent.Restaurant.Application` | `CustomerReorder/Queries/GetCustomerFrequentProductsQuery.cs` | `.cs` | 3,142 B | CQRS Command / Query / Handler | Yes |
| `GetCustomerLastOrderQuery.cs` | `Clovent.Restaurant.Application` | `CustomerReorder/Queries/GetCustomerLastOrderQuery.cs` | `.cs` | 6,049 B | CQRS Command / Query / Handler | Yes |
| `CreateCustomerCommand.cs` | `Clovent.Restaurant.Application` | `Customers/Commands/CreateCustomerCommand.cs` | `.cs` | 3,836 B | CQRS Command / Query / Handler | Yes |
| `RecordCustomerPaymentCommand.cs` | `Clovent.Restaurant.Application` | `Customers/Commands/RecordCustomerPaymentCommand.cs` | `.cs` | 3,259 B | CQRS Command / Query / Handler | Yes |
| `SetCustomerStatusCommand.cs` | `Clovent.Restaurant.Application` | `Customers/Commands/SetCustomerStatusCommand.cs` | `.cs` | 1,281 B | CQRS Command / Query / Handler | Yes |
| `SetDefaultCustomerCommand.cs` | `Clovent.Restaurant.Application` | `Customers/Commands/SetDefaultCustomerCommand.cs` | `.cs` | 1,686 B | CQRS Command / Query / Handler | Yes |
| `UpdateCustomerCommand.cs` | `Clovent.Restaurant.Application` | `Customers/Commands/UpdateCustomerCommand.cs` | `.cs` | 2,370 B | CQRS Command / Query / Handler | Yes |
| `CustomerDto.cs` | `Clovent.Restaurant.Application` | `Customers/Dtos/CustomerDto.cs` | `.cs` | 1,369 B | CQRS Command / Query / Handler | Yes |
| `CustomerLedgerEntryDto.cs` | `Clovent.Restaurant.Application` | `Customers/Dtos/CustomerLedgerEntryDto.cs` | `.cs` | 788 B | CQRS Command / Query / Handler | Yes |
| `GetCustomerByCodeQuery.cs` | `Clovent.Restaurant.Application` | `Customers/Queries/GetCustomerByCodeQuery.cs` | `.cs` | 1,079 B | CQRS Command / Query / Handler | Yes |
| `GetCustomerByIdQuery.cs` | `Clovent.Restaurant.Application` | `Customers/Queries/GetCustomerByIdQuery.cs` | `.cs` | 908 B | CQRS Command / Query / Handler | Yes |
| `GetCustomerLedgerQuery.cs` | `Clovent.Restaurant.Application` | `Customers/Queries/GetCustomerLedgerQuery.cs` | `.cs` | 966 B | CQRS Command / Query / Handler | Yes |
| `GetDefaultCustomerQuery.cs` | `Clovent.Restaurant.Application` | `Customers/Queries/GetDefaultCustomerQuery.cs` | `.cs` | 953 B | CQRS Command / Query / Handler | Yes |
| `ListCustomersQuery.cs` | `Clovent.Restaurant.Application` | `Customers/Queries/ListCustomersQuery.cs` | `.cs` | 1,178 B | CQRS Command / Query / Handler | Yes |
| `ApplicationServiceCollectionExtensions.cs` | `Clovent.Restaurant.Application` | `DependencyInjection/ApplicationServiceCollectionExtensions.cs` | `.cs` | 1,191 B | CQRS Command / Query / Handler | Yes |
| `ActivateDiningAreaCommand.cs` | `Clovent.Restaurant.Application` | `DiningAreas/Commands/ActivateDiningAreaCommand.cs` | `.cs` | 955 B | CQRS Command / Query / Handler | Yes |
| `CreateDiningAreaCommand.cs` | `Clovent.Restaurant.Application` | `DiningAreas/Commands/CreateDiningAreaCommand.cs` | `.cs` | 1,004 B | CQRS Command / Query / Handler | Yes |
| `DeactivateDiningAreaCommand.cs` | `Clovent.Restaurant.Application` | `DiningAreas/Commands/DeactivateDiningAreaCommand.cs` | `.cs` | 969 B | CQRS Command / Query / Handler | Yes |
| `RenameDiningAreaCommand.cs` | `Clovent.Restaurant.Application` | `DiningAreas/Commands/RenameDiningAreaCommand.cs` | `.cs` | 1,040 B | CQRS Command / Query / Handler | Yes |
| `DiningAreaDto.cs` | `Clovent.Restaurant.Application` | `DiningAreas/Dtos/DiningAreaDto.cs` | `.cs` | 592 B | CQRS Command / Query / Handler | Yes |
| `GetDiningAreaByIdQuery.cs` | `Clovent.Restaurant.Application` | `DiningAreas/Queries/GetDiningAreaByIdQuery.cs` | `.cs` | 920 B | CQRS Command / Query / Handler | Yes |
| `ListAllDiningAreasQuery.cs` | `Clovent.Restaurant.Application` | `DiningAreas/Queries/ListAllDiningAreasQuery.cs` | `.cs` | 930 B | CQRS Command / Query / Handler | Yes |
| `ListDiningAreasByBranchQuery.cs` | `Clovent.Restaurant.Application` | `DiningAreas/Queries/ListDiningAreasByBranchQuery.cs` | `.cs` | 992 B | CQRS Command / Query / Handler | Yes |
| `ApplyDiscountToOrderCommand.cs` | `Clovent.Restaurant.Application` | `Discounts/Commands/ApplyDiscountToOrderCommand.cs` | `.cs` | 5,024 B | CQRS Command / Query / Handler | Yes |
| `ApplyDiscountToOrderCommand.cs.backup-m4m5-20260817-121628` | `Clovent.Restaurant.Application` | `Discounts/Commands/ApplyDiscountToOrderCommand.cs.backup-m4m5-20260817-121628` | `.backup-m4m5-20260817-121628` | 1,316 B | CQRS Command / Query / Handler | Yes |
| `RemoveDiscountFromOrderCommand.cs` | `Clovent.Restaurant.Application` | `Discounts/Commands/RemoveDiscountFromOrderCommand.cs` | `.cs` | 1,401 B | CQRS Command / Query / Handler | Yes |
| `DiscountDto.cs` | `Clovent.Restaurant.Application` | `Discounts/Dtos/DiscountDto.cs` | `.cs` | 639 B | CQRS Command / Query / Handler | Yes |
| `GetDiscountByIdQuery.cs` | `Clovent.Restaurant.Application` | `Discounts/Queries/GetDiscountByIdQuery.cs` | `.cs` | 889 B | CQRS Command / Query / Handler | Yes |
| `ListDiscountsByOrderQuery.cs` | `Clovent.Restaurant.Application` | `Discounts/Queries/ListDiscountsByOrderQuery.cs` | `.cs` | 960 B | CQRS Command / Query / Handler | Yes |
| `EndOfDayReportDto.cs` | `Clovent.Restaurant.Application` | `EndOfDay/Dtos/EndOfDayReportDto.cs` | `.cs` | 1,515 B | CQRS Command / Query / Handler | Yes |
| `GetEndOfDayReportQuery.cs` | `Clovent.Restaurant.Application` | `EndOfDay/Queries/GetEndOfDayReportQuery.cs` | `.cs` | 7,033 B | CQRS Command / Query / Handler | Yes |
| `IUnitOfWork.cs` | `Clovent.Restaurant.Application` | `IUnitOfWork.cs` | `.cs` | 504 B | CQRS Command / Query / Handler | Yes |
| `CancelKitchenTicketCommand.cs` | `Clovent.Restaurant.Application` | `KitchenTickets/Commands/CancelKitchenTicketCommand.cs` | `.cs` | 1,024 B | CQRS Command / Query / Handler | Yes |
| `MarkKitchenTicketReadyCommand.cs` | `Clovent.Restaurant.Application` | `KitchenTickets/Commands/MarkKitchenTicketReadyCommand.cs` | `.cs` | 1,035 B | CQRS Command / Query / Handler | Yes |
| `SendOrderToKitchenCommand.cs` | `Clovent.Restaurant.Application` | `KitchenTickets/Commands/SendOrderToKitchenCommand.cs` | `.cs` | 1,705 B | CQRS Command / Query / Handler | Yes |
| `ServeKitchenTicketCommand.cs` | `Clovent.Restaurant.Application` | `KitchenTickets/Commands/ServeKitchenTicketCommand.cs` | `.cs` | 1,003 B | CQRS Command / Query / Handler | Yes |
| `StartKitchenTicketCommand.cs` | `Clovent.Restaurant.Application` | `KitchenTickets/Commands/StartKitchenTicketCommand.cs` | `.cs` | 1,012 B | CQRS Command / Query / Handler | Yes |
| `KitchenTicketDto.cs` | `Clovent.Restaurant.Application` | `KitchenTickets/Dtos/KitchenTicketDto.cs` | `.cs` | 919 B | CQRS Command / Query / Handler | Yes |
| `GetKitchenTicketByIdQuery.cs` | `Clovent.Restaurant.Application` | `KitchenTickets/Queries/GetKitchenTicketByIdQuery.cs` | `.cs` | 981 B | CQRS Command / Query / Handler | Yes |
| `ListActiveKitchenTicketsQuery.cs` | `Clovent.Restaurant.Application` | `KitchenTickets/Queries/ListActiveKitchenTicketsQuery.cs` | `.cs` | 984 B | CQRS Command / Query / Handler | Yes |
| `ListKitchenTicketsByOrderQuery.cs` | `Clovent.Restaurant.Application` | `KitchenTickets/Queries/ListKitchenTicketsByOrderQuery.cs` | `.cs` | 1,017 B | CQRS Command / Query / Handler | Yes |
| `NotFoundException.cs` | `Clovent.Restaurant.Application` | `NotFoundException.cs` | `.cs` | 487 B | CQRS Command / Query / Handler | Yes |
| `OrderHealthEvaluator.cs` | `Clovent.Restaurant.Application` | `OrderHealth/OrderHealthEvaluator.cs` | `.cs` | 1,617 B | CQRS Command / Query / Handler | Yes |
| `OrderHealthResult.cs` | `Clovent.Restaurant.Application` | `OrderHealth/OrderHealthResult.cs` | `.cs` | 486 B | CQRS Command / Query / Handler | Yes |
| `OrderHealthStatus.cs` | `Clovent.Restaurant.Application` | `OrderHealth/OrderHealthStatus.cs` | `.cs` | 517 B | CQRS Command / Query / Handler | Yes |
| `OrderHealthThresholds.cs` | `Clovent.Restaurant.Application` | `OrderHealth/OrderHealthThresholds.cs` | `.cs` | 1,375 B | CQRS Command / Query / Handler | Yes |
| `AddOrderLineCommand.cs` | `Clovent.Restaurant.Application` | `OrderLines/Commands/AddOrderLineCommand.cs` | `.cs` | 2,546 B | CQRS Command / Query / Handler | Yes |
| `OverrideOrderLinePriceCommand.cs` | `Clovent.Restaurant.Application` | `OrderLines/Commands/OverrideOrderLinePriceCommand.cs` | `.cs` | 1,444 B | CQRS Command / Query / Handler | Yes |
| `RemoveOrderLineCommand.cs` | `Clovent.Restaurant.Application` | `OrderLines/Commands/RemoveOrderLineCommand.cs` | `.cs` | 1,603 B | CQRS Command / Query / Handler | Yes |
| `SetOrderLineNotesCommand.cs` | `Clovent.Restaurant.Application` | `OrderLines/Commands/SetOrderLineNotesCommand.cs` | `.cs` | 973 B | CQRS Command / Query / Handler | Yes |
| `SetOrderLineQuantityCommand.cs` | `Clovent.Restaurant.Application` | `OrderLines/Commands/SetOrderLineQuantityCommand.cs` | `.cs` | 998 B | CQRS Command / Query / Handler | Yes |
| `UnvoidOrderLineCommand.cs` | `Clovent.Restaurant.Application` | `OrderLines/Commands/UnvoidOrderLineCommand.cs` | `.cs` | 930 B | CQRS Command / Query / Handler | Yes |
| `VoidOrderLineCommand.cs` | `Clovent.Restaurant.Application` | `OrderLines/Commands/VoidOrderLineCommand.cs` | `.cs` | 997 B | CQRS Command / Query / Handler | Yes |
| `OrderLineDto.cs` | `Clovent.Restaurant.Application` | `OrderLines/Dtos/OrderLineDto.cs` | `.cs` | 1,274 B | CQRS Command / Query / Handler | Yes |
| `GetOrderLineByIdQuery.cs` | `Clovent.Restaurant.Application` | `OrderLines/Queries/GetOrderLineByIdQuery.cs` | `.cs` | 902 B | CQRS Command / Query / Handler | Yes |
| `ListOrderLinesByOrderQuery.cs` | `Clovent.Restaurant.Application` | `OrderLines/Queries/ListOrderLinesByOrderQuery.cs` | `.cs` | 973 B | CQRS Command / Query / Handler | Yes |
| `CancelOrderCommand.cs` | `Clovent.Restaurant.Application` | `Orders/Commands/CancelOrderCommand.cs` | `.cs` | 1,415 B | CQRS Command / Query / Handler | Yes |
| `CompleteOrderCommand.cs` | `Clovent.Restaurant.Application` | `Orders/Commands/CompleteOrderCommand.cs` | `.cs` | 10,646 B | CQRS Command / Query / Handler | Yes |
| `CompleteOrderCommand.cs.backup-h2-20260817-113226` | `Clovent.Restaurant.Application` | `Orders/Commands/CompleteOrderCommand.cs.backup-h2-20260817-113226` | `.backup-h2-20260817-113226` | 4,182 B | CQRS Command / Query / Handler | Yes |
| `CompleteOrderCommand.cs.backup-m4m5-20260817-121628` | `Clovent.Restaurant.Application` | `Orders/Commands/CompleteOrderCommand.cs.backup-m4m5-20260817-121628` | `.backup-m4m5-20260817-121628` | 9,704 B | CQRS Command / Query / Handler | Yes |
| `ConfigureOrderNumberSequenceCommand.cs` | `Clovent.Restaurant.Application` | `Orders/Commands/ConfigureOrderNumberSequenceCommand.cs` | `.cs` | 1,327 B | CQRS Command / Query / Handler | Yes |
| `CreateOrderCommand.cs` | `Clovent.Restaurant.Application` | `Orders/Commands/CreateOrderCommand.cs` | `.cs` | 5,017 B | CQRS Command / Query / Handler | Yes |
| `CreateOrderCommand.cs.backup-m3-20260817-115714` | `Clovent.Restaurant.Application` | `Orders/Commands/CreateOrderCommand.cs.backup-m3-20260817-115714` | `.backup-m3-20260817-115714` | 2,089 B | CQRS Command / Query / Handler | Yes |
| `HoldOrderCommand.cs` | `Clovent.Restaurant.Application` | `Orders/Commands/HoldOrderCommand.cs` | `.cs` | 957 B | CQRS Command / Query / Handler | Yes |
| `MergeTablesCommand.cs` | `Clovent.Restaurant.Application` | `Orders/Commands/MergeTablesCommand.cs` | `.cs` | 2,880 B | CQRS Command / Query / Handler | Yes |
| `ReopenOrderCommand.cs` | `Clovent.Restaurant.Application` | `Orders/Commands/ReopenOrderCommand.cs` | `.cs` | 1,295 B | CQRS Command / Query / Handler | Yes |
| `ResumeOrderCommand.cs` | `Clovent.Restaurant.Application` | `Orders/Commands/ResumeOrderCommand.cs` | `.cs` | 853 B | CQRS Command / Query / Handler | Yes |
| `SetOrderCustomerCommand.cs` | `Clovent.Restaurant.Application` | `Orders/Commands/SetOrderCustomerCommand.cs` | `.cs` | 1,497 B | CQRS Command / Query / Handler | Yes |
| `SetOrderCustomerNotesCommand.cs` | `Clovent.Restaurant.Application` | `Orders/Commands/SetOrderCustomerNotesCommand.cs` | `.cs` | 974 B | CQRS Command / Query / Handler | Yes |
| `SetOrderNotesCommand.cs` | `Clovent.Restaurant.Application` | `Orders/Commands/SetOrderNotesCommand.cs` | `.cs` | 903 B | CQRS Command / Query / Handler | Yes |
| `SplitOrderCommand.cs` | `Clovent.Restaurant.Application` | `Orders/Commands/SplitOrderCommand.cs` | `.cs` | 2,418 B | CQRS Command / Query / Handler | Yes |
| `TransferOrderTableCommand.cs` | `Clovent.Restaurant.Application` | `Orders/Commands/TransferOrderTableCommand.cs` | `.cs` | 3,603 B | CQRS Command / Query / Handler | Yes |
| `VoidOrderCommand.cs` | `Clovent.Restaurant.Application` | `Orders/Commands/VoidOrderCommand.cs` | `.cs` | 3,273 B | CQRS Command / Query / Handler | Yes |
| `OrderDto.cs` | `Clovent.Restaurant.Application` | `Orders/Dtos/OrderDto.cs` | `.cs` | 1,412 B | CQRS Command / Query / Handler | Yes |
| `OrderNumberSequenceDto.cs` | `Clovent.Restaurant.Application` | `Orders/Dtos/OrderNumberSequenceDto.cs` | `.cs` | 485 B | CQRS Command / Query / Handler | Yes |
| `OrderTotals.cs` | `Clovent.Restaurant.Application` | `Orders/OrderTotals.cs` | `.cs` | 1,744 B | CQRS Command / Query / Handler | Yes |
| `OrderTotalsCalculator.cs` | `Clovent.Restaurant.Application` | `Orders/OrderTotalsCalculator.cs` | `.cs` | 2,704 B | CQRS Command / Query / Handler | Yes |
| `GetOpenOrHeldOrderByTableQuery.cs` | `Clovent.Restaurant.Application` | `Orders/Queries/GetOpenOrHeldOrderByTableQuery.cs` | `.cs` | 989 B | CQRS Command / Query / Handler | Yes |
| `GetOrderByIdQuery.cs` | `Clovent.Restaurant.Application` | `Orders/Queries/GetOrderByIdQuery.cs` | `.cs` | 827 B | CQRS Command / Query / Handler | Yes |
| `GetOrderNumberSequenceQuery.cs` | `Clovent.Restaurant.Application` | `Orders/Queries/GetOrderNumberSequenceQuery.cs` | `.cs` | 1,174 B | CQRS Command / Query / Handler | Yes |
| `GetOrderSummaryQuery.cs` | `Clovent.Restaurant.Application` | `Orders/Queries/GetOrderSummaryQuery.cs` | `.cs` | 2,139 B | CQRS Command / Query / Handler | Yes |
| `ListAllOrdersQuery.cs` | `Clovent.Restaurant.Application` | `Orders/Queries/ListAllOrdersQuery.cs` | `.cs` | 807 B | CQRS Command / Query / Handler | Yes |
| `ListHeldOrdersQuery.cs` | `Clovent.Restaurant.Application` | `Orders/Queries/ListHeldOrdersQuery.cs` | `.cs` | 872 B | CQRS Command / Query / Handler | Yes |
| `ListOpenOrdersQuery.cs` | `Clovent.Restaurant.Application` | `Orders/Queries/ListOpenOrdersQuery.cs` | `.cs` | 875 B | CQRS Command / Query / Handler | Yes |
| `ActivatePaymentMethodCommand.cs` | `Clovent.Restaurant.Application` | `PaymentMethods/Commands/ActivatePaymentMethodCommand.cs` | `.cs` | 1,018 B | CQRS Command / Query / Handler | Yes |
| `CreatePaymentMethodCommand.cs` | `Clovent.Restaurant.Application` | `PaymentMethods/Commands/CreatePaymentMethodCommand.cs` | `.cs` | 966 B | CQRS Command / Query / Handler | Yes |
| `DeactivatePaymentMethodCommand.cs` | `Clovent.Restaurant.Application` | `PaymentMethods/Commands/DeactivatePaymentMethodCommand.cs` | `.cs` | 1,032 B | CQRS Command / Query / Handler | Yes |
| `RenamePaymentMethodCommand.cs` | `Clovent.Restaurant.Application` | `PaymentMethods/Commands/RenamePaymentMethodCommand.cs` | `.cs` | 1,109 B | CQRS Command / Query / Handler | Yes |
| `PaymentMethodDto.cs` | `Clovent.Restaurant.Application` | `PaymentMethods/Dtos/PaymentMethodDto.cs` | `.cs` | 590 B | CQRS Command / Query / Handler | Yes |
| `GetPaymentMethodByIdQuery.cs` | `Clovent.Restaurant.Application` | `PaymentMethods/Queries/GetPaymentMethodByIdQuery.cs` | `.cs` | 981 B | CQRS Command / Query / Handler | Yes |
| `ListPaymentMethodsQuery.cs` | `Clovent.Restaurant.Application` | `PaymentMethods/Queries/ListPaymentMethodsQuery.cs` | `.cs` | 889 B | CQRS Command / Query / Handler | Yes |
| `RecordPaymentCommand.cs` | `Clovent.Restaurant.Application` | `Payments/Commands/RecordPaymentCommand.cs` | `.cs` | 8,356 B | CQRS Command / Query / Handler | Yes |
| `RecordPaymentCommand.cs.backup-paymentfix-20260817-104258` | `Clovent.Restaurant.Application` | `Payments/Commands/RecordPaymentCommand.cs.backup-paymentfix-20260817-104258` | `.backup-paymentfix-20260817-104258` | 4,063 B | CQRS Command / Query / Handler | Yes |
| `VoidPaymentCommand.cs` | `Clovent.Restaurant.Application` | `Payments/Commands/VoidPaymentCommand.cs` | `.cs` | 2,868 B | CQRS Command / Query / Handler | Yes |
| `PaymentDto.cs` | `Clovent.Restaurant.Application` | `Payments/Dtos/PaymentDto.cs` | `.cs` | 673 B | CQRS Command / Query / Handler | Yes |
| `GetPaymentByIdQuery.cs` | `Clovent.Restaurant.Application` | `Payments/Queries/GetPaymentByIdQuery.cs` | `.cs` | 868 B | CQRS Command / Query / Handler | Yes |
| `ListPaymentsByOrderQuery.cs` | `Clovent.Restaurant.Application` | `Payments/Queries/ListPaymentsByOrderQuery.cs` | `.cs` | 950 B | CQRS Command / Query / Handler | Yes |
| `CreateQuickOrderTemplateCommand.cs` | `Clovent.Restaurant.Application` | `QuickOrderTemplates/Commands/CreateQuickOrderTemplateCommand.cs` | `.cs` | 1,402 B | CQRS Command / Query / Handler | Yes |
| `SetQuickOrderTemplateStatusCommand.cs` | `Clovent.Restaurant.Application` | `QuickOrderTemplates/Commands/SetQuickOrderTemplateStatusCommand.cs` | `.cs` | 1,060 B | CQRS Command / Query / Handler | Yes |
| `UpdateQuickOrderTemplateCommand.cs` | `Clovent.Restaurant.Application` | `QuickOrderTemplates/Commands/UpdateQuickOrderTemplateCommand.cs` | `.cs` | 1,561 B | CQRS Command / Query / Handler | Yes |
| `QuickOrderTemplateDto.cs` | `Clovent.Restaurant.Application` | `QuickOrderTemplates/Dtos/QuickOrderTemplateDto.cs` | `.cs` | 433 B | CQRS Command / Query / Handler | Yes |
| `QuickOrderTemplateItemDto.cs` | `Clovent.Restaurant.Application` | `QuickOrderTemplates/Dtos/QuickOrderTemplateItemDto.cs` | `.cs` | 490 B | CQRS Command / Query / Handler | Yes |
| `ListActiveQuickOrderTemplatesQuery.cs` | `Clovent.Restaurant.Application` | `QuickOrderTemplates/Queries/ListActiveQuickOrderTemplatesQuery.cs` | `.cs` | 1,645 B | CQRS Command / Query / Handler | Yes |
| `ListAllQuickOrderTemplatesQuery.cs` | `Clovent.Restaurant.Application` | `QuickOrderTemplates/Queries/ListAllQuickOrderTemplatesQuery.cs` | `.cs` | 1,508 B | CQRS Command / Query / Handler | Yes |
| `QuickOrderTemplateProjection.cs` | `Clovent.Restaurant.Application` | `QuickOrderTemplates/Queries/QuickOrderTemplateProjection.cs` | `.cs` | 3,312 B | CQRS Command / Query / Handler | Yes |
| `RestaurantPulseDto.cs` | `Clovent.Restaurant.Application` | `RestaurantPulse/Dtos/RestaurantPulseDto.cs` | `.cs` | 1,050 B | CQRS Command / Query / Handler | Yes |
| `RestaurantPulseLowStockItemDto.cs` | `Clovent.Restaurant.Application` | `RestaurantPulse/Dtos/RestaurantPulseLowStockItemDto.cs` | `.cs` | 361 B | CQRS Command / Query / Handler | Yes |
| `GetRestaurantPulseQuery.cs` | `Clovent.Restaurant.Application` | `RestaurantPulse/Queries/GetRestaurantPulseQuery.cs` | `.cs` | 5,131 B | CQRS Command / Query / Handler | Yes |
| `RestaurantPulseCalculator.cs` | `Clovent.Restaurant.Application` | `RestaurantPulse/RestaurantPulseCalculator.cs` | `.cs` | 4,422 B | CQRS Command / Query / Handler | Yes |
| `RestaurantPulseSamples.cs` | `Clovent.Restaurant.Application` | `RestaurantPulse/RestaurantPulseSamples.cs` | `.cs` | 807 B | CQRS Command / Query / Handler | Yes |
| `ApplyServiceChargeToOrderCommand.cs` | `Clovent.Restaurant.Application` | `ServiceCharges/Commands/ApplyServiceChargeToOrderCommand.cs` | `.cs` | 1,414 B | CQRS Command / Query / Handler | Yes |
| `RemoveServiceChargeFromOrderCommand.cs` | `Clovent.Restaurant.Application` | `ServiceCharges/Commands/RemoveServiceChargeFromOrderCommand.cs` | `.cs` | 1,521 B | CQRS Command / Query / Handler | Yes |
| `ServiceChargeDto.cs` | `Clovent.Restaurant.Application` | `ServiceCharges/Dtos/ServiceChargeDto.cs` | `.cs` | 675 B | CQRS Command / Query / Handler | Yes |
| `GetServiceChargeByIdQuery.cs` | `Clovent.Restaurant.Application` | `ServiceCharges/Queries/GetServiceChargeByIdQuery.cs` | `.cs` | 981 B | CQRS Command / Query / Handler | Yes |
| `ListServiceChargesByOrderQuery.cs` | `Clovent.Restaurant.Application` | `ServiceCharges/Queries/ListServiceChargesByOrderQuery.cs` | `.cs` | 1,027 B | CQRS Command / Query / Handler | Yes |
| `CloseShiftCommand.cs` | `Clovent.Restaurant.Application` | `Shifts/Commands/CloseShiftCommand.cs` | `.cs` | 4,544 B | CQRS Command / Query / Handler | Yes |
| `OpenShiftCommand.cs` | `Clovent.Restaurant.Application` | `Shifts/Commands/OpenShiftCommand.cs` | `.cs` | 2,915 B | CQRS Command / Query / Handler | Yes |
| `RecordCashMovementCommand.cs` | `Clovent.Restaurant.Application` | `Shifts/Commands/RecordCashMovementCommand.cs` | `.cs` | 2,120 B | CQRS Command / Query / Handler | Yes |
| `CashMovementDto.cs` | `Clovent.Restaurant.Application` | `Shifts/Dtos/CashMovementDto.cs` | `.cs` | 786 B | CQRS Command / Query / Handler | Yes |
| `ShiftDto.cs` | `Clovent.Restaurant.Application` | `Shifts/Dtos/ShiftDto.cs` | `.cs` | 1,247 B | CQRS Command / Query / Handler | Yes |
| `ShiftSummaryDto.cs` | `Clovent.Restaurant.Application` | `Shifts/Dtos/ShiftSummaryDto.cs` | `.cs` | 572 B | CQRS Command / Query / Handler | Yes |
| `GetActiveShiftQuery.cs` | `Clovent.Restaurant.Application` | `Shifts/Queries/GetActiveShiftQuery.cs` | `.cs` | 1,390 B | CQRS Command / Query / Handler | Yes |
| `GetShiftByIdQuery.cs` | `Clovent.Restaurant.Application` | `Shifts/Queries/GetShiftByIdQuery.cs` | `.cs` | 943 B | CQRS Command / Query / Handler | Yes |
| `GetShiftSummaryQuery.cs` | `Clovent.Restaurant.Application` | `Shifts/Queries/GetShiftSummaryQuery.cs` | `.cs` | 3,616 B | CQRS Command / Query / Handler | Yes |
| `ListShiftsQuery.cs` | `Clovent.Restaurant.Application` | `Shifts/Queries/ListShiftsQuery.cs` | `.cs` | 1,520 B | CQRS Command / Query / Handler | Yes |
| `CreateRecommendationRuleCommand.cs` | `Clovent.Restaurant.Application` | `SmartRecommendations/Commands/CreateRecommendationRuleCommand.cs` | `.cs` | 1,419 B | CQRS Command / Query / Handler | Yes |
| `SetRecommendationRuleStatusCommand.cs` | `Clovent.Restaurant.Application` | `SmartRecommendations/Commands/SetRecommendationRuleStatusCommand.cs` | `.cs` | 1,069 B | CQRS Command / Query / Handler | Yes |
| `UpdateRecommendationRuleCommand.cs` | `Clovent.Restaurant.Application` | `SmartRecommendations/Commands/UpdateRecommendationRuleCommand.cs` | `.cs` | 1,652 B | CQRS Command / Query / Handler | Yes |
| `BasketRecommendationDto.cs` | `Clovent.Restaurant.Application` | `SmartRecommendations/Dtos/BasketRecommendationDto.cs` | `.cs` | 323 B | CQRS Command / Query / Handler | Yes |
| `RecommendationReason.cs` | `Clovent.Restaurant.Application` | `SmartRecommendations/Dtos/RecommendationReason.cs` | `.cs` | 580 B | CQRS Command / Query / Handler | Yes |
| `RecommendationRuleDto.cs` | `Clovent.Restaurant.Application` | `SmartRecommendations/Dtos/RecommendationRuleDto.cs` | `.cs` | 878 B | CQRS Command / Query / Handler | Yes |
| `GetBasketRecommendationsQuery.cs` | `Clovent.Restaurant.Application` | `SmartRecommendations/Queries/GetBasketRecommendationsQuery.cs` | `.cs` | 6,502 B | CQRS Command / Query / Handler | Yes |
| `ListRecommendationRulesQuery.cs` | `Clovent.Restaurant.Application` | `SmartRecommendations/Queries/ListRecommendationRulesQuery.cs` | `.cs` | 1,084 B | CQRS Command / Query / Handler | Yes |
| `ActivateTableCommand.cs` | `Clovent.Restaurant.Application` | `Tables/Commands/ActivateTableCommand.cs` | `.cs` | 862 B | CQRS Command / Query / Handler | Yes |
| `CreateTableCommand.cs` | `Clovent.Restaurant.Application` | `Tables/Commands/CreateTableCommand.cs` | `.cs` | 1,310 B | CQRS Command / Query / Handler | Yes |
| `DeactivateTableCommand.cs` | `Clovent.Restaurant.Application` | `Tables/Commands/DeactivateTableCommand.cs` | `.cs` | 876 B | CQRS Command / Query / Handler | Yes |
| `OccupyTableCommand.cs` | `Clovent.Restaurant.Application` | `Tables/Commands/OccupyTableCommand.cs` | `.cs` | 846 B | CQRS Command / Query / Handler | Yes |
| `ReserveTableCommand.cs` | `Clovent.Restaurant.Application` | `Tables/Commands/ReserveTableCommand.cs` | `.cs` | 877 B | CQRS Command / Query / Handler | Yes |
| `ReturnTableToServiceCommand.cs` | `Clovent.Restaurant.Application` | `Tables/Commands/ReturnTableToServiceCommand.cs` | `.cs` | 931 B | CQRS Command / Query / Handler | Yes |
| `SetTableOutOfServiceCommand.cs` | `Clovent.Restaurant.Application` | `Tables/Commands/SetTableOutOfServiceCommand.cs` | `.cs` | 915 B | CQRS Command / Query / Handler | Yes |
| `UpdateTableCommand.cs` | `Clovent.Restaurant.Application` | `Tables/Commands/UpdateTableCommand.cs` | `.cs` | 1,025 B | CQRS Command / Query / Handler | Yes |
| `VacateTableCommand.cs` | `Clovent.Restaurant.Application` | `Tables/Commands/VacateTableCommand.cs` | `.cs` | 3,274 B | CQRS Command / Query / Handler | Yes |
| `VacateTableCommand.cs.backup-vacateguard-20260817-135631` | `Clovent.Restaurant.Application` | `Tables/Commands/VacateTableCommand.cs.backup-vacateguard-20260817-135631` | `.backup-vacateguard-20260817-135631` | 846 B | CQRS Command / Query / Handler | Yes |
| `TableDto.cs` | `Clovent.Restaurant.Application` | `Tables/Dtos/TableDto.cs` | `.cs` | 774 B | CQRS Command / Query / Handler | Yes |
| `GetTableByIdQuery.cs` | `Clovent.Restaurant.Application` | `Tables/Queries/GetTableByIdQuery.cs` | `.cs` | 826 B | CQRS Command / Query / Handler | Yes |
| `ListAllTablesQuery.cs` | `Clovent.Restaurant.Application` | `Tables/Queries/ListAllTablesQuery.cs` | `.cs` | 860 B | CQRS Command / Query / Handler | Yes |
| `ListTablesByDiningAreaQuery.cs` | `Clovent.Restaurant.Application` | `Tables/Queries/ListTablesByDiningAreaQuery.cs` | `.cs` | 969 B | CQRS Command / Query / Handler | Yes |
| `UniversalPosSearchResultsDto.cs` | `Clovent.Restaurant.Application` | `UniversalPosSearch/Dtos/UniversalPosSearchResultsDto.cs` | `.cs` | 1,329 B | CQRS Command / Query / Handler | Yes |
| `UniversalPosSearchQuery.cs` | `Clovent.Restaurant.Application` | `UniversalPosSearch/Queries/UniversalPosSearchQuery.cs` | `.cs` | 7,432 B | CQRS Command / Query / Handler | Yes |
| `Clovent.Restaurant.Infrastructure.csproj` | `Clovent.Restaurant.Infrastructure` | `Clovent.Restaurant.Infrastructure.csproj` | `.csproj` | 1,299 B | EF Core DbContext / Repository / Migration | Yes |
| `InfrastructureServiceCollectionExtensions.cs` | `Clovent.Restaurant.Infrastructure` | `DependencyInjection/InfrastructureServiceCollectionExtensions.cs` | `.cs` | 1,179 B | EF Core DbContext / Repository / Migration | Yes |
| `PersistenceServiceCollectionExtensions.cs` | `Clovent.Restaurant.Infrastructure` | `DependencyInjection/PersistenceServiceCollectionExtensions.cs` | `.cs` | 4,207 B | EF Core DbContext / Repository / Migration | Yes |
| `20260728041421_InitialCreate.Designer.cs` | `Clovent.Restaurant.Infrastructure` | `Migrations/20260728041421_InitialCreate.Designer.cs` | `.cs` | 12,914 B | EF Core DbContext / Repository / Migration | Yes |
| `20260728041421_InitialCreate.cs` | `Clovent.Restaurant.Infrastructure` | `Migrations/20260728041421_InitialCreate.cs` | `.cs` | 13,806 B | EF Core DbContext / Repository / Migration | Yes |
| `RestaurantDbContextModelSnapshot.cs` | `Clovent.Restaurant.Infrastructure` | `Migrations/RestaurantDbContextModelSnapshot.cs` | `.cs` | 30,789 B | EF Core DbContext / Repository / Migration | Yes |
| `ActivityLogEntryConfiguration.cs` | `Clovent.Restaurant.Infrastructure` | `Persistence/Configurations/ActivityLogEntryConfiguration.cs` | `.cs` | 1,148 B | EF Core DbContext / Repository / Migration | Yes |
| `CashMovementConfiguration.cs` | `Clovent.Restaurant.Infrastructure` | `Persistence/Configurations/CashMovementConfiguration.cs` | `.cs` | 1,386 B | EF Core DbContext / Repository / Migration | Yes |
| `CustomerConfiguration.cs` | `Clovent.Restaurant.Infrastructure` | `Persistence/Configurations/CustomerConfiguration.cs` | `.cs` | 1,887 B | EF Core DbContext / Repository / Migration | Yes |
| `CustomerLedgerEntryConfiguration.cs` | `Clovent.Restaurant.Infrastructure` | `Persistence/Configurations/CustomerLedgerEntryConfiguration.cs` | `.cs` | 1,342 B | EF Core DbContext / Repository / Migration | Yes |
| `DailySalesSequenceConfiguration.cs` | `Clovent.Restaurant.Infrastructure` | `Persistence/Configurations/DailySalesSequenceConfiguration.cs` | `.cs` | 1,078 B | EF Core DbContext / Repository / Migration | Yes |
| `DiningAreaConfiguration.cs` | `Clovent.Restaurant.Infrastructure` | `Persistence/Configurations/DiningAreaConfiguration.cs` | `.cs` | 1,249 B | EF Core DbContext / Repository / Migration | Yes |
| `DiscountConfiguration.cs` | `Clovent.Restaurant.Infrastructure` | `Persistence/Configurations/DiscountConfiguration.cs` | `.cs` | 1,219 B | EF Core DbContext / Repository / Migration | Yes |
| `KitchenTicketConfiguration.cs` | `Clovent.Restaurant.Infrastructure` | `Persistence/Configurations/KitchenTicketConfiguration.cs` | `.cs` | 1,485 B | EF Core DbContext / Repository / Migration | Yes |
| `OrderConfiguration.cs` | `Clovent.Restaurant.Infrastructure` | `Persistence/Configurations/OrderConfiguration.cs` | `.cs` | 2,687 B | EF Core DbContext / Repository / Migration | Yes |
| `OrderLineConfiguration.cs` | `Clovent.Restaurant.Infrastructure` | `Persistence/Configurations/OrderLineConfiguration.cs` | `.cs` | 1,891 B | EF Core DbContext / Repository / Migration | Yes |
| `OrderNumberSequenceConfiguration.cs` | `Clovent.Restaurant.Infrastructure` | `Persistence/Configurations/OrderNumberSequenceConfiguration.cs` | `.cs` | 892 B | EF Core DbContext / Repository / Migration | Yes |
| `PaymentConfiguration.cs` | `Clovent.Restaurant.Infrastructure` | `Persistence/Configurations/PaymentConfiguration.cs` | `.cs` | 1,356 B | EF Core DbContext / Repository / Migration | Yes |
| `PaymentMethodConfiguration.cs` | `Clovent.Restaurant.Infrastructure` | `Persistence/Configurations/PaymentMethodConfiguration.cs` | `.cs` | 1,097 B | EF Core DbContext / Repository / Migration | Yes |
| `QuickOrderTemplateConfiguration.cs` | `Clovent.Restaurant.Infrastructure` | `Persistence/Configurations/QuickOrderTemplateConfiguration.cs` | `.cs` | 1,430 B | EF Core DbContext / Repository / Migration | Yes |
| `QuickOrderTemplateItemConfiguration.cs` | `Clovent.Restaurant.Infrastructure` | `Persistence/Configurations/QuickOrderTemplateItemConfiguration.cs` | `.cs` | 1,228 B | EF Core DbContext / Repository / Migration | Yes |
| `RecommendationRuleConfiguration.cs` | `Clovent.Restaurant.Infrastructure` | `Persistence/Configurations/RecommendationRuleConfiguration.cs` | `.cs` | 1,577 B | EF Core DbContext / Repository / Migration | Yes |
| `ServiceChargeConfiguration.cs` | `Clovent.Restaurant.Infrastructure` | `Persistence/Configurations/ServiceChargeConfiguration.cs` | `.cs` | 1,259 B | EF Core DbContext / Repository / Migration | Yes |
| `ShiftConfiguration.cs` | `Clovent.Restaurant.Infrastructure` | `Persistence/Configurations/ShiftConfiguration.cs` | `.cs` | 2,624 B | EF Core DbContext / Repository / Migration | Yes |
| `TableConfiguration.cs` | `Clovent.Restaurant.Infrastructure` | `Persistence/Configurations/TableConfiguration.cs` | `.cs` | 1,582 B | EF Core DbContext / Repository / Migration | Yes |
| `20260729054113_AddDailySalesSequenceAndDailySalesNumber.Designer.cs` | `Clovent.Restaurant.Infrastructure` | `Persistence/Migrations/20260729054113_AddDailySalesSequenceAndDailySalesNumber.Designer.cs` | `.cs` | 13,843 B | EF Core DbContext / Repository / Migration | Yes |
| `20260729054113_AddDailySalesSequenceAndDailySalesNumber.cs` | `Clovent.Restaurant.Infrastructure` | `Persistence/Migrations/20260729054113_AddDailySalesSequenceAndDailySalesNumber.cs` | `.cs` | 2,044 B | EF Core DbContext / Repository / Migration | Yes |
| `20260804060819_AddOrderNumberSequence.Designer.cs` | `Clovent.Restaurant.Infrastructure` | `Persistence/Migrations/20260804060819_AddOrderNumberSequence.Designer.cs` | `.cs` | 14,444 B | EF Core DbContext / Repository / Migration | Yes |
| `20260804060819_AddOrderNumberSequence.cs` | `Clovent.Restaurant.Infrastructure` | `Persistence/Migrations/20260804060819_AddOrderNumberSequence.cs` | `.cs` | 1,277 B | EF Core DbContext / Repository / Migration | Yes |
| `20260804082352_AddOrderLinePriceOverride.Designer.cs` | `Clovent.Restaurant.Infrastructure` | `Persistence/Migrations/20260804082352_AddOrderLinePriceOverride.Designer.cs` | `.cs` | 15,193 B | EF Core DbContext / Repository / Migration | Yes |
| `20260804082352_AddOrderLinePriceOverride.cs` | `Clovent.Restaurant.Infrastructure` | `Persistence/Migrations/20260804082352_AddOrderLinePriceOverride.cs` | `.cs` | 3,195 B | EF Core DbContext / Repository / Migration | Yes |
| `20260804084644_AddActivityLog.Designer.cs` | `Clovent.Restaurant.Infrastructure` | `Persistence/Migrations/20260804084644_AddActivityLog.Designer.cs` | `.cs` | 16,441 B | EF Core DbContext / Repository / Migration | Yes |
| `20260804084644_AddActivityLog.cs` | `Clovent.Restaurant.Infrastructure` | `Persistence/Migrations/20260804084644_AddActivityLog.cs` | `.cs` | 1,861 B | EF Core DbContext / Repository / Migration | Yes |
| `20260809031725_AddCustomerAndLedger.Designer.cs` | `Clovent.Restaurant.Infrastructure` | `Persistence/Migrations/20260809031725_AddCustomerAndLedger.Designer.cs` | `.cs` | 20,451 B | EF Core DbContext / Repository / Migration | Yes |
| `20260809031725_AddCustomerAndLedger.cs` | `Clovent.Restaurant.Infrastructure` | `Persistence/Migrations/20260809031725_AddCustomerAndLedger.cs` | `.cs` | 4,965 B | EF Core DbContext / Repository / Migration | Yes |
| `20260821110043_AddTableNameToTable.Designer.cs` | `Clovent.Restaurant.Infrastructure` | `Persistence/Migrations/20260821110043_AddTableNameToTable.Designer.cs` | `.cs` | 20,640 B | EF Core DbContext / Repository / Migration | Yes |
| `20260821110043_AddTableNameToTable.cs` | `Clovent.Restaurant.Infrastructure` | `Persistence/Migrations/20260821110043_AddTableNameToTable.cs` | `.cs` | 1,021 B | EF Core DbContext / Repository / Migration | Yes |
| `20260821123746_UpdateMonetaryDecimalPrecision.Designer.cs` | `Clovent.Restaurant.Infrastructure` | `Persistence/Migrations/20260821123746_UpdateMonetaryDecimalPrecision.Designer.cs` | `.cs` | 20,662 B | EF Core DbContext / Repository / Migration | Yes |
| `20260821123746_UpdateMonetaryDecimalPrecision.cs` | `Clovent.Restaurant.Infrastructure` | `Persistence/Migrations/20260821123746_UpdateMonetaryDecimalPrecision.cs` | `.cs` | 3,234 B | EF Core DbContext / Repository / Migration | Yes |
| `20260821123934_AddCustomerFields.Designer.cs` | `Clovent.Restaurant.Infrastructure` | `Persistence/Migrations/20260821123934_AddCustomerFields.Designer.cs` | `.cs` | 21,094 B | EF Core DbContext / Repository / Migration | Yes |
| `20260821123934_AddCustomerFields.cs` | `Clovent.Restaurant.Infrastructure` | `Persistence/Migrations/20260821123934_AddCustomerFields.cs` | `.cs` | 1,744 B | EF Core DbContext / Repository / Migration | Yes |
| `20260912025646_AddShiftManagement.Designer.cs` | `Clovent.Restaurant.Infrastructure` | `Persistence/Migrations/20260912025646_AddShiftManagement.Designer.cs` | `.cs` | 26,196 B | EF Core DbContext / Repository / Migration | Yes |
| `20260912025646_AddShiftManagement.cs` | `Clovent.Restaurant.Infrastructure` | `Persistence/Migrations/20260912025646_AddShiftManagement.cs` | `.cs` | 6,333 B | EF Core DbContext / Repository / Migration | Yes |
| `20260914160000_AddCustomerIsDefault.Designer.cs` | `Clovent.Restaurant.Infrastructure` | `Persistence/Migrations/20260914160000_AddCustomerIsDefault.Designer.cs` | `.cs` | 21,300 B | EF Core DbContext / Repository / Migration | Yes |
| `20260914160000_AddCustomerIsDefault.cs` | `Clovent.Restaurant.Infrastructure` | `Persistence/Migrations/20260914160000_AddCustomerIsDefault.cs` | `.cs` | 879 B | EF Core DbContext / Repository / Migration | Yes |
| `20260915054626_AddSmartPosFeatures.Designer.cs` | `Clovent.Restaurant.Infrastructure` | `Persistence/Migrations/20260915054626_AddSmartPosFeatures.Designer.cs` | `.cs` | 30,910 B | EF Core DbContext / Repository / Migration | Yes |
| `20260915054626_AddSmartPosFeatures.cs` | `Clovent.Restaurant.Infrastructure` | `Persistence/Migrations/20260915054626_AddSmartPosFeatures.cs` | `.cs` | 6,047 B | EF Core DbContext / Repository / Migration | Yes |
| `RestaurantDbContext.cs` | `Clovent.Restaurant.Infrastructure` | `Persistence/RestaurantDbContext.cs` | `.cs` | 3,529 B | EF Core DbContext / Repository / Migration | Yes |
| `RestaurantDbContextFactory.cs` | `Clovent.Restaurant.Infrastructure` | `Persistence/RestaurantDbContextFactory.cs` | `.cs` | 828 B | EF Core DbContext / Repository / Migration | Yes |
| `RestaurantPersistenceInitializer.cs` | `Clovent.Restaurant.Infrastructure` | `Persistence/RestaurantPersistenceInitializer.cs` | `.cs` | 15,759 B | EF Core DbContext / Repository / Migration | Yes |
| `UnitOfWork.cs` | `Clovent.Restaurant.Infrastructure` | `Persistence/UnitOfWork.cs` | `.cs` | 565 B | EF Core DbContext / Repository / Migration | Yes |
| `UnitOfWorkBehavior.cs` | `Clovent.Restaurant.Infrastructure` | `Persistence/UnitOfWorkBehavior.cs` | `.cs` | 767 B | EF Core DbContext / Repository / Migration | Yes |
| `ValueConverters.cs` | `Clovent.Restaurant.Infrastructure` | `Persistence/ValueConverters.cs` | `.cs` | 14,952 B | EF Core DbContext / Repository / Migration | Yes |
| `ActivityLogEntryRepository.cs` | `Clovent.Restaurant.Infrastructure` | `Repositories/ActivityLogEntryRepository.cs` | `.cs` | 926 B | EF Core DbContext / Repository / Migration | Yes |
| `CustomerLedgerEntryRepository.cs` | `Clovent.Restaurant.Infrastructure` | `Repositories/CustomerLedgerEntryRepository.cs` | `.cs` | 1,388 B | EF Core DbContext / Repository / Migration | Yes |
| `CustomerRepository.cs` | `Clovent.Restaurant.Infrastructure` | `Repositories/CustomerRepository.cs` | `.cs` | 5,357 B | EF Core DbContext / Repository / Migration | Yes |
| `DailySalesSequenceRepository.cs` | `Clovent.Restaurant.Infrastructure` | `Repositories/DailySalesSequenceRepository.cs` | `.cs` | 952 B | EF Core DbContext / Repository / Migration | Yes |
| `DiningAreaRepository.cs` | `Clovent.Restaurant.Infrastructure` | `Repositories/DiningAreaRepository.cs` | `.cs` | 1,306 B | EF Core DbContext / Repository / Migration | Yes |
| `DiscountRepository.cs` | `Clovent.Restaurant.Infrastructure` | `Repositories/DiscountRepository.cs` | `.cs` | 1,067 B | EF Core DbContext / Repository / Migration | Yes |
| `KitchenTicketRepository.cs` | `Clovent.Restaurant.Infrastructure` | `Repositories/KitchenTicketRepository.cs` | `.cs` | 1,471 B | EF Core DbContext / Repository / Migration | Yes |
| `OrderLineRepository.cs` | `Clovent.Restaurant.Infrastructure` | `Repositories/OrderLineRepository.cs` | `.cs` | 1,080 B | EF Core DbContext / Repository / Migration | Yes |
| `OrderNumberSequenceRepository.cs` | `Clovent.Restaurant.Infrastructure` | `Repositories/OrderNumberSequenceRepository.cs` | `.cs` | 821 B | EF Core DbContext / Repository / Migration | Yes |
| `OrderRepository.cs` | `Clovent.Restaurant.Infrastructure` | `Repositories/OrderRepository.cs` | `.cs` | 2,004 B | EF Core DbContext / Repository / Migration | Yes |
| `PaymentMethodRepository.cs` | `Clovent.Restaurant.Infrastructure` | `Repositories/PaymentMethodRepository.cs` | `.cs` | 1,043 B | EF Core DbContext / Repository / Migration | Yes |
| `PaymentRepository.cs` | `Clovent.Restaurant.Infrastructure` | `Repositories/PaymentRepository.cs` | `.cs` | 1,338 B | EF Core DbContext / Repository / Migration | Yes |
| `QuickOrderTemplateRepository.cs` | `Clovent.Restaurant.Infrastructure` | `Repositories/QuickOrderTemplateRepository.cs` | `.cs` | 2,066 B | EF Core DbContext / Repository / Migration | Yes |
| `RecommendationRuleRepository.cs` | `Clovent.Restaurant.Infrastructure` | `Repositories/RecommendationRuleRepository.cs` | `.cs` | 1,764 B | EF Core DbContext / Repository / Migration | Yes |
| `ServiceChargeRepository.cs` | `Clovent.Restaurant.Infrastructure` | `Repositories/ServiceChargeRepository.cs` | `.cs` | 1,132 B | EF Core DbContext / Repository / Migration | Yes |
| `ShiftRepository.cs` | `Clovent.Restaurant.Infrastructure` | `Repositories/ShiftRepository.cs` | `.cs` | 3,304 B | EF Core DbContext / Repository / Migration | Yes |
| `TableRepository.cs` | `Clovent.Restaurant.Infrastructure` | `Repositories/TableRepository.cs` | `.cs` | 1,256 B | EF Core DbContext / Repository / Migration | Yes |
| `ActivityLogEntry.cs` | `Clovent.Restaurant` | `ActivityLogs/ActivityLogEntry.cs` | `.cs` | 3,903 B | Domain Entity / Value Object | Yes |
| `ActivityLogEntryId.cs` | `Clovent.Restaurant` | `ActivityLogs/ActivityLogEntryId.cs` | `.cs` | 700 B | Domain Entity / Value Object | Yes |
| `ActivityLogEntryRecorded.cs` | `Clovent.Restaurant` | `ActivityLogs/Events/ActivityLogEntryRecorded.cs` | `.cs` | 305 B | Domain Entity / Value Object | Yes |
| `IActivityLogEntryRepository.cs` | `Clovent.Restaurant` | `ActivityLogs/IActivityLogEntryRepository.cs` | `.cs` | 579 B | Domain Entity / Value Object | Yes |
| `Clovent.Restaurant.csproj` | `Clovent.Restaurant` | `Clovent.Restaurant.csproj` | `.csproj` | 615 B | Domain Entity / Value Object | Yes |
| `Customer.cs` | `Clovent.Restaurant` | `Customers/Customer.cs` | `.cs` | 7,221 B | Domain Entity / Value Object | Yes |
| `CustomerId.cs` | `Clovent.Restaurant` | `Customers/CustomerId.cs` | `.cs` | 656 B | Domain Entity / Value Object | Yes |
| `CustomerLedgerEntry.cs` | `Clovent.Restaurant` | `Customers/CustomerLedgerEntry.cs` | `.cs` | 2,702 B | Domain Entity / Value Object | Yes |
| `CustomerLedgerEntryId.cs` | `Clovent.Restaurant` | `Customers/CustomerLedgerEntryId.cs` | `.cs` | 701 B | Domain Entity / Value Object | Yes |
| `ICustomerLedgerEntryRepository.cs` | `Clovent.Restaurant` | `Customers/ICustomerLedgerEntryRepository.cs` | `.cs` | 805 B | Domain Entity / Value Object | Yes |
| `ICustomerRepository.cs` | `Clovent.Restaurant` | `Customers/ICustomerRepository.cs` | `.cs` | 2,112 B | Domain Entity / Value Object | Yes |
| `DiningArea.cs` | `Clovent.Restaurant` | `DiningAreas/DiningArea.cs` | `.cs` | 3,230 B | Domain Entity / Value Object | Yes |
| `DiningAreaId.cs` | `Clovent.Restaurant` | `DiningAreas/DiningAreaId.cs` | `.cs` | 668 B | Domain Entity / Value Object | Yes |
| `DiningAreaActivated.cs` | `Clovent.Restaurant` | `DiningAreas/Events/DiningAreaActivated.cs` | `.cs` | 267 B | Domain Entity / Value Object | Yes |
| `DiningAreaCreated.cs` | `Clovent.Restaurant` | `DiningAreas/Events/DiningAreaCreated.cs` | `.cs` | 387 B | Domain Entity / Value Object | Yes |
| `DiningAreaDeactivated.cs` | `Clovent.Restaurant` | `DiningAreas/Events/DiningAreaDeactivated.cs` | `.cs` | 267 B | Domain Entity / Value Object | Yes |
| `DiningAreaRenamed.cs` | `Clovent.Restaurant` | `DiningAreas/Events/DiningAreaRenamed.cs` | `.cs` | 335 B | Domain Entity / Value Object | Yes |
| `IDiningAreaRepository.cs` | `Clovent.Restaurant` | `DiningAreas/IDiningAreaRepository.cs` | `.cs` | 1,000 B | Domain Entity / Value Object | Yes |
| `DiningAreaName.cs` | `Clovent.Restaurant` | `DiningAreas/ValueObjects/DiningAreaName.cs` | `.cs` | 1,334 B | Domain Entity / Value Object | Yes |
| `Discount.cs` | `Clovent.Restaurant` | `Discounts/Discount.cs` | `.cs` | 3,250 B | Domain Entity / Value Object | Yes |
| `DiscountId.cs` | `Clovent.Restaurant` | `Discounts/DiscountId.cs` | `.cs` | 656 B | Domain Entity / Value Object | Yes |
| `DiscountType.cs` | `Clovent.Restaurant` | `Discounts/DiscountType.cs` | `.cs` | 325 B | Domain Entity / Value Object | Yes |
| `DiscountCreated.cs` | `Clovent.Restaurant` | `Discounts/Events/DiscountCreated.cs` | `.cs` | 345 B | Domain Entity / Value Object | Yes |
| `IDiscountRepository.cs` | `Clovent.Restaurant` | `Discounts/IDiscountRepository.cs` | `.cs` | 739 B | Domain Entity / Value Object | Yes |
| `KitchenTicketCancelled.cs` | `Clovent.Restaurant` | `KitchenTickets/Events/KitchenTicketCancelled.cs` | `.cs` | 278 B | Domain Entity / Value Object | Yes |
| `KitchenTicketCreated.cs` | `Clovent.Restaurant` | `KitchenTickets/Events/KitchenTicketCreated.cs` | `.cs` | 340 B | Domain Entity / Value Object | Yes |
| `KitchenTicketMarkedReady.cs` | `Clovent.Restaurant` | `KitchenTickets/Events/KitchenTicketMarkedReady.cs` | `.cs` | 292 B | Domain Entity / Value Object | Yes |
| `KitchenTicketServed.cs` | `Clovent.Restaurant` | `KitchenTickets/Events/KitchenTicketServed.cs` | `.cs` | 272 B | Domain Entity / Value Object | Yes |
| `KitchenTicketStarted.cs` | `Clovent.Restaurant` | `KitchenTickets/Events/KitchenTicketStarted.cs` | `.cs` | 285 B | Domain Entity / Value Object | Yes |
| `IKitchenTicketRepository.cs` | `Clovent.Restaurant` | `KitchenTickets/IKitchenTicketRepository.cs` | `.cs` | 1,085 B | Domain Entity / Value Object | Yes |
| `KitchenTicket.cs` | `Clovent.Restaurant` | `KitchenTickets/KitchenTicket.cs` | `.cs` | 5,290 B | Domain Entity / Value Object | Yes |
| `KitchenTicketId.cs` | `Clovent.Restaurant` | `KitchenTickets/KitchenTicketId.cs` | `.cs` | 686 B | Domain Entity / Value Object | Yes |
| `KitchenTicketStatus.cs` | `Clovent.Restaurant` | `KitchenTickets/KitchenTicketStatus.cs` | `.cs` | 575 B | Domain Entity / Value Object | Yes |
| `OrderLineCreated.cs` | `Clovent.Restaurant` | `OrderLines/Events/OrderLineCreated.cs` | `.cs` | 412 B | Domain Entity / Value Object | Yes |
| `OrderLineNotesChanged.cs` | `Clovent.Restaurant` | `OrderLines/Events/OrderLineNotesChanged.cs` | `.cs` | 284 B | Domain Entity / Value Object | Yes |
| `OrderLinePriceOverridden.cs` | `Clovent.Restaurant` | `OrderLines/Events/OrderLinePriceOverridden.cs` | `.cs` | 434 B | Domain Entity / Value Object | Yes |
| `OrderLineQuantityChanged.cs` | `Clovent.Restaurant` | `OrderLines/Events/OrderLineQuantityChanged.cs` | `.cs` | 289 B | Domain Entity / Value Object | Yes |
| `OrderLineTransferredToOrder.cs` | `Clovent.Restaurant` | `OrderLines/Events/OrderLineTransferredToOrder.cs` | `.cs` | 368 B | Domain Entity / Value Object | Yes |
| `OrderLineUnvoided.cs` | `Clovent.Restaurant` | `OrderLines/Events/OrderLineUnvoided.cs` | `.cs` | 263 B | Domain Entity / Value Object | Yes |
| `OrderLineVoided.cs` | `Clovent.Restaurant` | `OrderLines/Events/OrderLineVoided.cs` | `.cs` | 253 B | Domain Entity / Value Object | Yes |
| `IOrderLineRepository.cs` | `Clovent.Restaurant` | `OrderLines/IOrderLineRepository.cs` | `.cs` | 760 B | Domain Entity / Value Object | Yes |
| `OrderLine.cs` | `Clovent.Restaurant` | `OrderLines/OrderLine.cs` | `.cs` | 9,017 B | Domain Entity / Value Object | Yes |
| `OrderLineId.cs` | `Clovent.Restaurant` | `OrderLines/OrderLineId.cs` | `.cs` | 663 B | Domain Entity / Value Object | Yes |
| `OrderCancelled.cs` | `Clovent.Restaurant` | `Orders/Events/OrderCancelled.cs` | `.cs` | 254 B | Domain Entity / Value Object | Yes |
| `OrderCompleted.cs` | `Clovent.Restaurant` | `Orders/Events/OrderCompleted.cs` | `.cs` | 263 B | Domain Entity / Value Object | Yes |
| `OrderCreated.cs` | `Clovent.Restaurant` | `Orders/Events/OrderCreated.cs` | `.cs` | 443 B | Domain Entity / Value Object | Yes |
| `OrderCustomerNotesChanged.cs` | `Clovent.Restaurant` | `Orders/Events/OrderCustomerNotesChanged.cs` | `.cs` | 291 B | Domain Entity / Value Object | Yes |
| `OrderDailySalesNumberAssigned.cs` | `Clovent.Restaurant` | `Orders/Events/OrderDailySalesNumberAssigned.cs` | `.cs` | 298 B | Domain Entity / Value Object | Yes |
| `OrderDiscountApplied.cs` | `Clovent.Restaurant` | `Orders/Events/OrderDiscountApplied.cs` | `.cs` | 340 B | Domain Entity / Value Object | Yes |
| `OrderDiscountRemoved.cs` | `Clovent.Restaurant` | `Orders/Events/OrderDiscountRemoved.cs` | `.cs` | 342 B | Domain Entity / Value Object | Yes |
| `OrderHeld.cs` | `Clovent.Restaurant` | `Orders/Events/OrderHeld.cs` | `.cs` | 229 B | Domain Entity / Value Object | Yes |
| `OrderLineAdded.cs` | `Clovent.Restaurant` | `Orders/Events/OrderLineAdded.cs` | `.cs` | 338 B | Domain Entity / Value Object | Yes |
| `OrderLineRemoved.cs` | `Clovent.Restaurant` | `Orders/Events/OrderLineRemoved.cs` | `.cs` | 344 B | Domain Entity / Value Object | Yes |
| `OrderNotesChanged.cs` | `Clovent.Restaurant` | `Orders/Events/OrderNotesChanged.cs` | `.cs` | 268 B | Domain Entity / Value Object | Yes |
| `OrderPaymentRecorded.cs` | `Clovent.Restaurant` | `Orders/Events/OrderPaymentRecorded.cs` | `.cs` | 341 B | Domain Entity / Value Object | Yes |
| `OrderReopened.cs` | `Clovent.Restaurant` | `Orders/Events/OrderReopened.cs` | `.cs` | 256 B | Domain Entity / Value Object | Yes |
| `OrderResumed.cs` | `Clovent.Restaurant` | `Orders/Events/OrderResumed.cs` | `.cs` | 236 B | Domain Entity / Value Object | Yes |
| `OrderServiceChargeApplied.cs` | `Clovent.Restaurant` | `Orders/Events/OrderServiceChargeApplied.cs` | `.cs` | 370 B | Domain Entity / Value Object | Yes |
| `OrderServiceChargeRemoved.cs` | `Clovent.Restaurant` | `Orders/Events/OrderServiceChargeRemoved.cs` | `.cs` | 372 B | Domain Entity / Value Object | Yes |
| `OrderTableAssigned.cs` | `Clovent.Restaurant` | `Orders/Events/OrderTableAssigned.cs` | `.cs` | 345 B | Domain Entity / Value Object | Yes |
| `OrderVoided.cs` | `Clovent.Restaurant` | `Orders/Events/OrderVoided.cs` | `.cs` | 248 B | Domain Entity / Value Object | Yes |
| `IOrderNumberSequenceRepository.cs` | `Clovent.Restaurant` | `Orders/IOrderNumberSequenceRepository.cs` | `.cs` | 572 B | Domain Entity / Value Object | Yes |
| `IOrderRepository.cs` | `Clovent.Restaurant` | `Orders/IOrderRepository.cs` | `.cs` | 1,408 B | Domain Entity / Value Object | Yes |
| `Order.cs` | `Clovent.Restaurant` | `Orders/Order.cs` | `.cs` | 16,970 B | Domain Entity / Value Object | Yes |
| `OrderId.cs` | `Clovent.Restaurant` | `Orders/OrderId.cs` | `.cs` | 639 B | Domain Entity / Value Object | Yes |
| `OrderNumberSequence.cs` | `Clovent.Restaurant` | `Orders/OrderNumberSequence.cs` | `.cs` | 3,897 B | Domain Entity / Value Object | Yes |
| `OrderNumberSequenceId.cs` | `Clovent.Restaurant` | `Orders/OrderNumberSequenceId.cs` | `.cs` | 710 B | Domain Entity / Value Object | Yes |
| `OrderStatus.cs` | `Clovent.Restaurant` | `Orders/OrderStatus.cs` | `.cs` | 710 B | Domain Entity / Value Object | Yes |
| `OrderType.cs` | `Clovent.Restaurant` | `Orders/OrderType.cs` | `.cs` | 341 B | Domain Entity / Value Object | Yes |
| `OrderNumber.cs` | `Clovent.Restaurant` | `Orders/ValueObjects/OrderNumber.cs` | `.cs` | 1,942 B | Domain Entity / Value Object | Yes |
| `PaymentMethodActivated.cs` | `Clovent.Restaurant` | `PaymentMethods/Events/PaymentMethodActivated.cs` | `.cs` | 282 B | Domain Entity / Value Object | Yes |
| `PaymentMethodCreated.cs` | `Clovent.Restaurant` | `PaymentMethods/Events/PaymentMethodCreated.cs` | `.cs` | 356 B | Domain Entity / Value Object | Yes |
| `PaymentMethodDeactivated.cs` | `Clovent.Restaurant` | `PaymentMethods/Events/PaymentMethodDeactivated.cs` | `.cs` | 282 B | Domain Entity / Value Object | Yes |
| `PaymentMethodRenamed.cs` | `Clovent.Restaurant` | `PaymentMethods/Events/PaymentMethodRenamed.cs` | `.cs` | 356 B | Domain Entity / Value Object | Yes |
| `IPaymentMethodRepository.cs` | `Clovent.Restaurant` | `PaymentMethods/IPaymentMethodRepository.cs` | `.cs` | 720 B | Domain Entity / Value Object | Yes |
| `PaymentMethod.cs` | `Clovent.Restaurant` | `PaymentMethods/PaymentMethod.cs` | `.cs` | 2,885 B | Domain Entity / Value Object | Yes |
| `PaymentMethodId.cs` | `Clovent.Restaurant` | `PaymentMethods/PaymentMethodId.cs` | `.cs` | 686 B | Domain Entity / Value Object | Yes |
| `PaymentMethodName.cs` | `Clovent.Restaurant` | `PaymentMethods/ValueObjects/PaymentMethodName.cs` | `.cs` | 1,360 B | Domain Entity / Value Object | Yes |
| `PaymentCreated.cs` | `Clovent.Restaurant` | `Payments/Events/PaymentCreated.cs` | `.cs` | 389 B | Domain Entity / Value Object | Yes |
| `PaymentVoided.cs` | `Clovent.Restaurant` | `Payments/Events/PaymentVoided.cs` | `.cs` | 242 B | Domain Entity / Value Object | Yes |
| `IPaymentRepository.cs` | `Clovent.Restaurant` | `Payments/IPaymentRepository.cs` | `.cs` | 976 B | Domain Entity / Value Object | Yes |
| `Payment.cs` | `Clovent.Restaurant` | `Payments/Payment.cs` | `.cs` | 3,464 B | Domain Entity / Value Object | Yes |
| `PaymentId.cs` | `Clovent.Restaurant` | `Payments/PaymentId.cs` | `.cs` | 650 B | Domain Entity / Value Object | Yes |
| `IQuickOrderTemplateRepository.cs` | `Clovent.Restaurant` | `QuickOrderTemplates/IQuickOrderTemplateRepository.cs` | `.cs` | 1,209 B | Domain Entity / Value Object | Yes |
| `QuickOrderTemplate.cs` | `Clovent.Restaurant` | `QuickOrderTemplates/QuickOrderTemplate.cs` | `.cs` | 5,811 B | Domain Entity / Value Object | Yes |
| `QuickOrderTemplateId.cs` | `Clovent.Restaurant` | `QuickOrderTemplates/QuickOrderTemplateId.cs` | `.cs` | 716 B | Domain Entity / Value Object | Yes |
| `QuickOrderTemplateItem.cs` | `Clovent.Restaurant` | `QuickOrderTemplates/QuickOrderTemplateItem.cs` | `.cs` | 3,151 B | Domain Entity / Value Object | Yes |
| `QuickOrderTemplateItemId.cs` | `Clovent.Restaurant` | `QuickOrderTemplates/QuickOrderTemplateItemId.cs` | `.cs` | 733 B | Domain Entity / Value Object | Yes |
| `RestaurantDomainException.cs` | `Clovent.Restaurant` | `RestaurantDomainException.cs` | `.cs` | 13,045 B | Domain Entity / Value Object | Yes |
| `RestaurantDomainException.cs.backup-m3-20260817-115714` | `Clovent.Restaurant` | `RestaurantDomainException.cs.backup-m3-20260817-115714` | `.backup-m3-20260817-115714` | 10,110 B | Domain Entity / Value Object | Yes |
| `RestaurantDomainException.cs.backup-m4m5-20260817-121628` | `Clovent.Restaurant` | `RestaurantDomainException.cs.backup-m4m5-20260817-121628` | `.backup-m4m5-20260817-121628` | 10,708 B | Domain Entity / Value Object | Yes |
| `RestaurantDomainException.cs.backup-vacateguard-20260817-135631` | `Clovent.Restaurant` | `RestaurantDomainException.cs.backup-vacateguard-20260817-135631` | `.backup-vacateguard-20260817-135631` | 11,737 B | Domain Entity / Value Object | Yes |
| `DailySalesSequence.cs` | `Clovent.Restaurant` | `Sales/DailySalesSequence.cs` | `.cs` | 1,971 B | Domain Entity / Value Object | Yes |
| `DailySalesSequenceId.cs` | `Clovent.Restaurant` | `Sales/DailySalesSequenceId.cs` | `.cs` | 702 B | Domain Entity / Value Object | Yes |
| `DailySalesSequenceAdvanced.cs` | `Clovent.Restaurant` | `Sales/Events/DailySalesSequenceAdvanced.cs` | `.cs` | 401 B | Domain Entity / Value Object | Yes |
| `IDailySalesSequenceRepository.cs` | `Clovent.Restaurant` | `Sales/IDailySalesSequenceRepository.cs` | `.cs` | 641 B | Domain Entity / Value Object | Yes |
| `ServiceChargeCreated.cs` | `Clovent.Restaurant` | `ServiceCharges/Events/ServiceChargeCreated.cs` | `.cs` | 380 B | Domain Entity / Value Object | Yes |
| `IServiceChargeRepository.cs` | `Clovent.Restaurant` | `ServiceCharges/IServiceChargeRepository.cs` | `.cs` | 797 B | Domain Entity / Value Object | Yes |
| `ServiceCharge.cs` | `Clovent.Restaurant` | `ServiceCharges/ServiceCharge.cs` | `.cs` | 3,274 B | Domain Entity / Value Object | Yes |
| `ServiceChargeId.cs` | `Clovent.Restaurant` | `ServiceCharges/ServiceChargeId.cs` | `.cs` | 686 B | Domain Entity / Value Object | Yes |
| `ServiceChargeType.cs` | `Clovent.Restaurant` | `ServiceCharges/ServiceChargeType.cs` | `.cs` | 345 B | Domain Entity / Value Object | Yes |
| `RestaurantStatus.cs` | `Clovent.Restaurant` | `Shared/RestaurantStatus.cs` | `.cs` | 913 B | Domain Entity / Value Object | Yes |
| `CashMovement.cs` | `Clovent.Restaurant` | `Shifts/CashMovement.cs` | `.cs` | 2,378 B | Domain Entity / Value Object | Yes |
| `CashMovementId.cs` | `Clovent.Restaurant` | `Shifts/CashMovementId.cs` | `.cs` | 670 B | Domain Entity / Value Object | Yes |
| `CashMovementType.cs` | `Clovent.Restaurant` | `Shifts/CashMovementType.cs` | `.cs` | 407 B | Domain Entity / Value Object | Yes |
| `CashMovementRecorded.cs` | `Clovent.Restaurant` | `Shifts/Events/CashMovementRecorded.cs` | `.cs` | 430 B | Domain Entity / Value Object | Yes |
| `ShiftClosed.cs` | `Clovent.Restaurant` | `Shifts/Events/ShiftClosed.cs` | `.cs` | 394 B | Domain Entity / Value Object | Yes |
| `ShiftOpened.cs` | `Clovent.Restaurant` | `Shifts/Events/ShiftOpened.cs` | `.cs` | 560 B | Domain Entity / Value Object | Yes |
| `IShiftRepository.cs` | `Clovent.Restaurant` | `Shifts/IShiftRepository.cs` | `.cs` | 1,794 B | Domain Entity / Value Object | Yes |
| `Shift.cs` | `Clovent.Restaurant` | `Shifts/Shift.cs` | `.cs` | 8,492 B | Domain Entity / Value Object | Yes |
| `ShiftId.cs` | `Clovent.Restaurant` | `Shifts/ShiftId.cs` | `.cs` | 638 B | Domain Entity / Value Object | Yes |
| `ShiftStatus.cs` | `Clovent.Restaurant` | `Shifts/ShiftStatus.cs` | `.cs` | 463 B | Domain Entity / Value Object | Yes |
| `IRecommendationRuleRepository.cs` | `Clovent.Restaurant` | `SmartRecommendations/IRecommendationRuleRepository.cs` | `.cs` | 1,067 B | Domain Entity / Value Object | Yes |
| `RecommendationRule.cs` | `Clovent.Restaurant` | `SmartRecommendations/RecommendationRule.cs` | `.cs` | 7,268 B | Domain Entity / Value Object | Yes |
| `RecommendationRuleId.cs` | `Clovent.Restaurant` | `SmartRecommendations/RecommendationRuleId.cs` | `.cs` | 717 B | Domain Entity / Value Object | Yes |
| `TableActivated.cs` | `Clovent.Restaurant` | `Tables/Events/TableActivated.cs` | `.cs` | 242 B | Domain Entity / Value Object | Yes |
| `TableCapacityChanged.cs` | `Clovent.Restaurant` | `Tables/Events/TableCapacityChanged.cs` | `.cs` | 272 B | Domain Entity / Value Object | Yes |
| `TableCreated.cs` | `Clovent.Restaurant` | `Tables/Events/TableCreated.cs` | `.cs` | 380 B | Domain Entity / Value Object | Yes |
| `TableDeactivated.cs` | `Clovent.Restaurant` | `Tables/Events/TableDeactivated.cs` | `.cs` | 242 B | Domain Entity / Value Object | Yes |
| `TableOccupancyChanged.cs` | `Clovent.Restaurant` | `Tables/Events/TableOccupancyChanged.cs` | `.cs` | 315 B | Domain Entity / Value Object | Yes |
| `ITableRepository.cs` | `Clovent.Restaurant` | `Tables/ITableRepository.cs` | `.cs` | 952 B | Domain Entity / Value Object | Yes |
| `Table.cs` | `Clovent.Restaurant` | `Tables/Table.cs` | `.cs` | 8,239 B | Domain Entity / Value Object | Yes |
| `TableId.cs` | `Clovent.Restaurant` | `Tables/TableId.cs` | `.cs` | 638 B | Domain Entity / Value Object | Yes |
| `TableOccupancyStatus.cs` | `Clovent.Restaurant` | `Tables/TableOccupancyStatus.cs` | `.cs` | 803 B | Domain Entity / Value Object | Yes |
| `TableName.cs` | `Clovent.Restaurant` | `Tables/ValueObjects/TableName.cs` | `.cs` | 1,339 B | Domain Entity / Value Object | Yes |

## 4. Build / Generated Files

### 4.1 Summary of Generated & Build Output Directories

Across the workspace, **11,085 files** are compilation outputs, IDE caches, or version control databases. These files do NOT belong in manual source code reviews and should be cleaned via `dotnet clean` or excluded by `.gitignore`:

- **`bin/Debug/net10.0/` & `bin/Release/`:** Output binaries (`.dll`), debug symbols (`.pdb`), XML documentation (`.xml`), and runtime configuration (`.runtimeconfig.json`).
- **`obj/Debug/net10.0/`:** Incremental compilation intermediate files (`.cache`, `.props`, `.targets`, `.FileListAbsolute.txt`, generated assembly attributes).
- **`.vs/`:** Visual Studio IDE user caches, workspace state, Roslyn compiler caches (`.vsidx`).
- **`.git/`:** Git repository commit graph, objects, and refs.
- **`.tmp.driveupload/` & `.tmp.drivedownload/`:** Cloud sync temporary files.
- **`qa/live_ui/ClickTool.exe`:** Compiled Win32 input simulator tool used for automated mouse clicks.

### 4.2 Standard Source Control Assessment

| File Pattern | Normal Source Control Status | Action Required |
|---|---|---|
| `src/**/bin/**` | **Ignored** (Should never be committed) | Retain in `.gitignore`; cleanable via `dotnet clean` |
| `src/**/obj/**` | **Ignored** (Should never be committed) | Retain in `.gitignore`; safe to delete at any time |
| `Tools/**/bin/**` | **Ignored** | Retain in `.gitignore` |
| `Tools/**/obj/**` | **Ignored** | Retain in `.gitignore` |
| `scratch/**/bin/**` | **Ignored** | Scratch project build artifacts |
| `*.user` (e.g. `Clovent.Desktop.csproj.user`) | **User-specific** | Should be in `.gitignore` |
| `qa/live_ui/ClickTool.exe` | **Binary Executable** | Recommend compiling on demand from `ClickTool.cs` rather than tracking binary |

---

## 5. Duplicate / Similar Files

### 5.1 Snapshot & Timestamped Backup Files

During recent development sprints (specifically between 2026-08-17 and 2026-08-18), snapshot backups were made in-place beside active source files or in dedicated backup folders. These represent clear redundancy:

| Original File | Backup / Duplicate File | Reason for Duplication | Recommendation |
|---|---|---|---|
| `src/Clovent.Desktop/Program.cs` | `src/Clovent.Desktop/Program.cs.backup-orderhistory-20260817-124222` | Order history feature snapshot | Safe to archive / delete |
| `src/Clovent.Desktop/DependencyInjection/DesktopServiceCollectionExtensions.cs` | `...DesktopServiceCollectionExtensions.cs.backup-orderhistory-20260817-124222` | Order history DI snapshot | Safe to archive / delete |
| `src/Clovent.Desktop/Forms/Identity/LoginForm.cs` | `...LoginForm.cs.backup-ui-fixes-20260818` | UI fix snapshot | Safe to archive / delete |
| `src/Clovent.Desktop/Forms/Shell/MainForm.Designer.cs` | `...MainForm.Designer.cs.backup-orderhistory-20260817-124222` | Shell layout snapshot | Safe to archive / delete |
| `src/Clovent.Desktop/Restaurant/Orders/RestaurantPosForm.cs` | `...RestaurantPosForm.cs.backup-castfix-20260817-104258`<br/>`...RestaurantPosForm.cs.backup-cleanup-20260817-153334`<br/>`...RestaurantPosForm.cs.backup-parenting-20260817-121628`<br/>`...RestaurantPosForm.cs.backup-paymentfix-20260817-104258`<br/>`...RestaurantPosForm.cs.backup-readiness-20260818`<br/>`...RestaurantPosForm.cs.backup-ui-fixes-20260818` | Successive debugging iterations of POS form logic | 6 redundant historical copies; active form contains all consolidated fixes |
| `src/Clovent.Desktop/Restaurant/Orders/RestaurantPosForm.Designer.cs` | `...RestaurantPosForm.Designer.cs.backup-20260817-111140`<br/>`...backup-20260817-113226`<br/>`...backup-barcode-20260817-115714`<br/>`...backup-capturefix-20260817-135631`<br/>`...backup-cleanup-20260817-153334`<br/>`...backup-parenting-20260817-121628`<br/>`...backup-pre-sortfix.bak`<br/>`...backup-readiness-20260818`<br/>`...backup-runtime-test.bak`<br/>`...backup-ui-fixes-20260818` | 10 redundant designer snapshots created during layout adjustments | Safe to archive / delete |
| `src/Clovent.Desktop/Seed/DevelopmentAuthorizationSeedStartupTask.cs` | `...DevelopmentAuthorizationSeedStartupTask.cs.backup-orderhistory-20260817-124222` | Seed task snapshot | Safe to archive / delete |
| `src/Clovent.Authentication.Application/.../ApplicationServiceCollectionExtensions.cs` | `...ApplicationServiceCollectionExtensions.cs.backup-m4m5-20260817-121628` | Authentication DI snapshot | Safe to archive / delete |
| `src/Clovent.Restaurant.Infrastructure/Persistence/Migrations/20260912025646_AddShiftManagement.cs` | `.../20260911120000_AddShiftManagement.cs` (Deleted in Git working tree) | Renamed migration timestamp | Replaced by active migration |

### 5.2 Standalone Backups Directory (`backups/`)

- `backups/config-security-fix-2026-08-17/` (3 files: `appsettings.json.bak`, `Clovent.Desktop.csproj.bak`, `Clovent.Desktop.Tests.csproj.bak`)
- `backups/logging-2026-08-17/` (2 files: `appsettings.json.bak`, `Program.cs.bak`)
- `backups/rootcause-2026-08-18/` (4 files: `FileLoggerProvider.cs.bak`, `RestaurantPosForm.cs.bak`, `RestaurantPosForm.Designer.cs.bak`, `RestaurantPosForm.Designer.cs.pre-sortfix.bak`)

### 5.3 Duplicate / Prototype CLI Modules (`Tools/Clovent.CLI/Clovent.CBOS.Desktop`)

In `Tools/Clovent.CLI/Clovent.CBOS.Desktop/`, there are 20 empty stub C# files (0 to 3 bytes each) with identical names to production desktop components:
- `Forms/Dashboard/MainForm.cs`, `Forms/Dashboard/MainForm.Designer.cs`
- `Forms/Login/LoginForm.cs`, `Forms/Login/LoginForm.Designer.cs`
- `Infrastructure/ApplicationBootstrapper.cs`, `ApplicationHost.cs`, `DependencyInjection.cs`, etc.
- **Analysis:** These are leftover scaffold templates that were never implemented and duplicate class naming in `src/Clovent.Desktop`.

---

## 6. Possibly Unused Files (Static Reference & Usage Analysis)

| File / Component | Location | Confidence Level | Reason / Usage Analysis | References Found | Runtime Verification Required? |
|---|---|---|---|---|---|
| 19 in-place `.backup-*` files | `src/Clovent.Desktop/**` | **HIGH** | Static backup snapshots with timestamped suffixes | 0 code references | No — confirmed historical snapshots |
| 9 `.bak` backup files | `backups/**` | **HIGH** | Out-of-tree backup folder from August 2026 | 0 code references | No — confirmed historical snapshots |
| `cart_diagnostics.txt` | Root directory | **HIGH** | 182 KB diagnostic dump from 2026-08-17 | 0 code references | No — obsolete debug log |
| 10 root `.ps1` QA scripts (`click.ps1`, `enum.ps1`, `fg.ps1`, `find.ps1`, `login.ps1`, `pclick.ps1`, `shoot.ps1`, `type.ps1`, `uia.ps1`, `uia_set.ps1`) | Root directory | **MEDIUM** | Temporary automation scripts from manual test sessions | Consumed by root QA only | Not used by build or CI |
| Root PNG screenshots (`shot_login.png`, `shot_login2.png`, `test_screen.png`) | Root directory | **HIGH** | Ad-hoc screenshots created during QA testing | 0 code references | No — test artifacts |
| 20 stub C# files in `Tools/Clovent.CLI/Clovent.CBOS.Desktop` | `Tools/Clovent.CLI/Clovent.CBOS.Desktop/**` | **HIGH** | 0-byte or 3-byte empty class stubs | 0 consumers | No — uncompleted prototype |
| 7 stub C# services in `Tools/Clovent.CLI/Clovent.CLI` | `Tools/Clovent.CLI/Clovent.CLI/Services/**` | **HIGH** | 3-byte empty file stubs | 0 references | No — scaffolding remnants |
| `scratch/DesignerTest/` (5 files) | `scratch/DesignerTest/**` | **HIGH** | Standalone test project created to inspect WinForms Designer behavior | Not in any solution | No — scratch test |
| `Clovent.Desktop.UiQa` Project | `src/Clovent.Desktop.UiQa/**` | **MEDIUM** | Console test runner for UI automation; not included in `Clovent.BusinessOperatingSystem.slnx` | Self-contained executable | Intentionally separate QA runner |
| SQLite QA scripts (`qa/v3_inv1.sql`, `qa/v3_sql1.sql`..`sql4.sql`) | `qa/` | **LOW** | Direct SQL inspection queries used during acceptance tests | Used by QA scripts | Retain for test reproduction |
| `UniversalSearchDropdown.cs` | `src/Clovent.Desktop/Restaurant/Orders/` | **LOW** | Recently added search component for POS | Bound dynamically | **Yes — Requires runtime verification** |
| `CustomerReorderDialogs.cs` | `src/Clovent.Desktop/Restaurant/Orders/` | **LOW** | Smart POS Customer Reorder feature | Triggered via UI events | **Yes — Requires runtime verification** |
| `RestaurantPulseForm.cs` | `src/Clovent.Desktop/Restaurant/Orders/` | **LOW** | POS real-time intelligence analytics dashboard | Opened via POS button | **Yes — Requires runtime verification** |

---

## 7. Core Application Files by Architectural Category

### Core Startup & Presentation Host
- `src/Clovent.Desktop/Program.cs` (Application entry point, Host builder, DI container configuration)
- `src/Clovent.Desktop/DependencyInjection/DesktopServiceCollectionExtensions.cs` (Service registrations)
- `src/Clovent.Desktop/Forms/Shell/MainForm.cs` & `.Designer.cs` (Main MDI/Ribbon Shell window)
- `src/Clovent.Desktop/Forms/Shell/MainFormNavigationAdapter.cs` (View switcher)

### Authentication & Authorization
- `src/Clovent.Desktop/Login/LoginService.cs` (Desktop authentication session management)
- `src/Clovent.Desktop/Forms/Identity/LoginForm.cs` & `.Designer.cs` (User login form)
- `src/Clovent.Desktop/Identity/Users/PinPromptForm.cs` & `.Designer.cs` (Quick POS PIN authentication)
- `src/Clovent.Authentication.Application/Credentials/Commands/SetPinCommand.cs` & Handlers
- `src/Clovent.Authentication/Credentials/PinCredential.cs` (PIN domain model)
- `src/Clovent.Desktop/Restaurant/Shared/ManagerAuthorizationForm.cs` (Privileged action override)

### Restaurant POS Core Subsystem
- `src/Clovent.Desktop/Restaurant/Orders/RestaurantPosForm.cs` & `.Designer.cs` (The central POS cashier/waiter workstation UI)
- `src/Clovent.Desktop/Restaurant/Orders/SmartPosState.cs` (State machine for POS active order, cart, modes)
- `src/Clovent.Desktop/Restaurant/Orders/TableSelectionDineInPolicy.cs` (Auto table assignment & dining rules)
- `src/Clovent.Desktop/Restaurant/Orders/RecallOrderDialog.cs` & `.Designer.cs` (Recall held/open orders)
- `src/Clovent.Desktop/Restaurant/Orders/SplitPaymentDialog.cs` & `.Designer.cs` (Multi-tender order split)
- `src/Clovent.Desktop/Restaurant/Orders/TableTransferDialog.cs` & `.Designer.cs` (Transfer orders between tables)
- `src/Clovent.Desktop/Restaurant/Orders/RunningOrdersView.cs` & `.Designer.cs` (Live kitchen/order board)
- `src/Clovent.Desktop/Restaurant/Orders/ReceiptFormatter.cs` & `ReceiptPrintDocument.cs` (Thermal printing engine)

### Orders Domain & Application
- `src/Clovent.Restaurant/Orders/Order.cs` (Aggregate root: lines, taxes, service charges, payments, status)
- `src/Clovent.Restaurant/Orders/OrderItem.cs` (Order line items and modifiers)
- `src/Clovent.Restaurant/Orders/OrderPayment.cs` (Tender, change, tips, payment method)
- `src/Clovent.Restaurant.Application/Orders/Commands/CreateOrderCommand.cs` & Handler
- `src/Clovent.Restaurant.Application/Orders/Commands/AddOrderItemCommand.cs` & Handler
- `src/Clovent.Restaurant.Application/Orders/Commands/RecordOrderPaymentCommand.cs` & Handler
- `src/Clovent.Restaurant.Application/Orders/Commands/HoldOrderCommand.cs` & Handler
- `src/Clovent.Restaurant.Application/Orders/Commands/RecallOrderCommand.cs` & Handler
- `src/Clovent.Restaurant.Application/Orders/Commands/CancelOrderCommand.cs` & Handler
- `src/Clovent.Restaurant.Application/Orders/Commands/TransferOrderTableCommand.cs` & Handler

### Customers & Loyalty
- `src/Clovent.Restaurant/Customers/Customer.cs` (Customer aggregate: balances, credits, orders)
- `src/Clovent.Restaurant.Application/Customers/Commands/CreateCustomerCommand.cs`
- `src/Clovent.Restaurant.Application/Customers/Commands/SetDefaultCustomerCommand.cs`
- `src/Clovent.Desktop/Restaurant/Customers/CustomersView.cs` & `CustomerEditForm.cs` (Customer directory & edit)

### Tables & Dining Areas
- `src/Clovent.Restaurant/Tables/RestaurantTable.cs` (Table aggregate: status, capacity, section)
- `src/Clovent.Restaurant.Application/Tables/Commands/UpdateTableStatusCommand.cs`
- `src/Clovent.Desktop/Restaurant/Tables/TableManagementView.cs` & `TableEditForm.cs` (Table layout designer)

### Menu & Catalog
- `src/Clovent.Catalog/Products/Product.cs` (Catalog products, variants, barcode, SKU)
- `src/Clovent.Catalog/Categories/Category.cs` (Menu item taxonomy and hierarchical display)
- `src/Clovent.Catalog.Application/Products/Queries/GetProductsQuery.cs`

### Smart POS & Intelligence
- `src/Clovent.Desktop/Restaurant/SmartPos/QuickOrderTemplatesView.cs` (One-touch combo templates)
- `src/Clovent.Desktop/Restaurant/SmartPos/RecommendationRulesView.cs` (Smart cross-sell rules)
- `src/Clovent.Desktop/Restaurant/SmartPos/OrderHealthSettingsForm.cs` (Prep-time thresholds & alerts)
- `src/Clovent.Restaurant.Application/SmartRecommendations/Commands/` & `Queries/`
- `src/Clovent.Restaurant.Application/OrderHealth/Queries/GetOrderHealthStatusQuery.cs`

### Shifts & Cash Management
- `src/Clovent.Restaurant/Shifts/Shift.cs` (Shift aggregate: opening cash, cash drops, closing counts)
- `src/Clovent.Desktop/Restaurant/Shifts/OpenShiftDialog.cs` & `CloseShiftDialog.cs`
- `src/Clovent.Desktop/Restaurant/Shifts/CashMovementDialog.cs` (Paid-in / Paid-out)
- `src/Clovent.Desktop/Restaurant/Shifts/ShiftHistoryView.cs` (Z-Reports and audit trails)

### Database & Persistence
- `src/Clovent.Restaurant.Infrastructure/Persistence/RestaurantDbContext.cs` (EF Core mapping for Restaurant)
- `src/Clovent.Restaurant.Infrastructure/Persistence/RestaurantPersistenceInitializer.cs` (DB Migrator/Seeder)
- `src/Clovent.Catalog.Infrastructure/Persistence/CatalogDbContext.cs`
- `src/Clovent.Inventory.Infrastructure/Persistence/InventoryDbContext.cs`
- `src/Clovent.Identity.Infrastructure/Persistence/IdentityDbContext.cs`
- `src/Clovent.Authentication.Infrastructure/Persistence/AuthenticationDbContext.cs`

---

## 8. Restaurant POS Specific Audit

The Restaurant POS is the flagship workload in this application. The table below maps all POS operational actions to their corresponding UI forms, Application Handlers, Domain Aggregates, and Persistence configurations:

| POS Action / Feature | Presentation (UI Form) | Application Command / Query | Domain Model / Logic | Database Table / Entity |
|---|---|---|---|---|
| **POS Main Screen** | `RestaurantPosForm.cs` | `GetMenuCategoriesQuery`, `GetProductsQuery` | `Order`, `OrderItem`, `SmartPosState` | N/A (Composite UI Host) |
| **Dine-In Mode** | `RestaurantPosForm.cs` | `CreateOrderCommand(DiningType.DineIn)` | `TableSelectionDineInPolicy` | `RestaurantOrders (DiningType=1)` |
| **Take Away Mode** | `RestaurantPosForm.cs` | `CreateOrderCommand(DiningType.TakeAway)` | `Order.CreateTakeAway()` | `RestaurantOrders (DiningType=2)` |
| **Table Assignment** | `TablePickerEdit.cs` | `AssignOrderTableCommand` | `RestaurantTable.Occupy()` | `RestaurantTables`, `RestaurantOrders` |
| **Table Transfer** | `TableTransferDialog.cs` | `TransferOrderTableCommand` | `Order.TransferTable()`, `Table.Vacate()` | `RestaurantOrders.TableId` |
| **Order Creation** | `RestaurantPosForm.cs` | `CreateOrderCommand` | `Order.Create()` | `RestaurantOrders` |
| **Menu Category Nav** | `RestaurantPosForm.cs` | `GetCategoriesQuery` | `Category` | `CatalogCategories` |
| **Order Item Addition** | `RestaurantPosForm.cs` | `AddOrderItemCommand` | `Order.AddItem()`, `OrderItem` | `RestaurantOrderItems` |
| **Quick Orders** | `QuickOrderTemplatesView.cs` | `ApplyQuickOrderTemplateCommand` | `QuickOrderTemplate` | `RestaurantQuickOrderTemplates` |
| **Recommendations** | `RestaurantPosForm.cs` | `GetRecommendationsForCartQuery` | `RecommendationRule` | `RestaurantRecommendationRules` |
| **Discounts** | `RestaurantPosForm.cs` | `ApplyDiscountCommand` | `Order.ApplyDiscount()` | `RestaurantOrders.DiscountAmount` |
| **Service Charges** | `ServiceChargeDialog.cs` | `ApplyServiceChargeCommand` | `Order.ApplyServiceCharge()` | `RestaurantOrders.ServiceChargeAmount` |
| **Hold Order** | `RestaurantPosForm.cs` | `HoldOrderCommand` | `Order.Hold()`, `OrderStatus.Held` | `RestaurantOrders.Status = 2` |
| **Recall Order** | `RecallOrderDialog.cs` | `RecallOrderCommand`, `GetHeldOrdersQuery` | `Order.Recall()` | `RestaurantOrders` |
| **Cancel Order** | `RestaurantPosForm.cs` | `CancelOrderCommand` | `Order.Cancel()`, `OrderStatus.Cancelled` | `RestaurantOrders.Status = 5` |
| **Record Payment** | `RestaurantPosForm.cs` | `RecordOrderPaymentCommand` | `Order.AddPayment()`, `OrderPayment` | `RestaurantOrderPayments` |
| **Split Payment** | `SplitPaymentDialog.cs` | `RecordOrderPaymentCommand` (Multiple) | Multi-tender allocation logic | `RestaurantOrderPayments` |
| **Order Completion** | `RestaurantPosForm.cs` | `CompleteOrderCommand` | `Order.Complete()`, `Table.Vacate()` | `RestaurantOrders.Status = 3` |
| **Receipt Printing** | `ReceiptPreviewForm.cs` | `GetReceiptDetailsQuery` | `ReceiptFormatter`, `ReceiptPrintDocument` | Hardware / Spooler |
| **Shift Cash Ops** | `CashMovementDialog.cs` | `RecordCashMovementCommand` | `Shift.AddMovement()` | `RestaurantCashMovements` |
| **Customer Balance** | `CustomerEditForm.cs` | `GetCustomerBalanceQuery` | `Customer.UpdateBalance()` | `RestaurantCustomers.Balance` |

---

## 9. Test Projects and Test Files

The solution contains **18 automated test projects** ensuring end-to-end verification across Domain, Application, Infrastructure, and Presentation layers:

### 9.1 Test Projects Inventory

| Project Name | Path | Target Framework | Output | Test Count (Approx) | Focus Area |
|---|---|---|---|---|---|
| **Clovent.PackageManager.Tests** | `Tools/Clovent.CLI/Clovent.PackageManager.Tests/Clovent.PackageManager.Tests.csproj` | `net10.0` | `Library` | ~10-50 tests | Domain Logic & Aggregates |
| **Clovent.Core.Tests** | `Tools/Clovent.CLI/tests/Clovent.Core.Tests/Clovent.Core.Tests.csproj` | `net10.0` | `Library` | ~10-50 tests | Domain Logic & Aggregates |
| **Clovent.Generator.Tests** | `Tools/Clovent.CLI/tests/Clovent.Generator.Tests/Clovent.Generator.Tests.csproj` | `net10.0` | `Library` | ~10-50 tests | Domain Logic & Aggregates |
| **Clovent.Authentication.Application.Tests** | `src/Clovent.Authentication.Application.Tests/Clovent.Authentication.Application.Tests.csproj` | `net10.0` | `Library` | ~10-50 tests | CQRS Commands & Queries |
| **Clovent.Authentication.Infrastructure.Tests** | `src/Clovent.Authentication.Infrastructure.Tests/Clovent.Authentication.Infrastructure.Tests.csproj` | `net10.0` | `Library` | ~10-50 tests | EF Core SQLite In-Memory & Migrations |
| **Clovent.Authentication.Tests** | `src/Clovent.Authentication.Tests/Clovent.Authentication.Tests.csproj` | `net10.0` | `Library` | ~10-50 tests | Domain Logic & Aggregates |
| **Clovent.Catalog.Application.Tests** | `src/Clovent.Catalog.Application.Tests/Clovent.Catalog.Application.Tests.csproj` | `net10.0` | `Library` | ~10-50 tests | CQRS Commands & Queries |
| **Clovent.Catalog.Infrastructure.Tests** | `src/Clovent.Catalog.Infrastructure.Tests/Clovent.Catalog.Infrastructure.Tests.csproj` | `net10.0` | `Library` | ~10-50 tests | EF Core SQLite In-Memory & Migrations |
| **Clovent.Catalog.Tests** | `src/Clovent.Catalog.Tests/Clovent.Catalog.Tests.csproj` | `net10.0` | `Library` | ~10-50 tests | Domain Logic & Aggregates |
| **Clovent.Desktop.Tests** | `src/Clovent.Desktop.Tests/Clovent.Desktop.Tests.csproj` | `net10.0-windows` | `Library` | ~10-50 tests | Desktop WinForms & POS Workflow Acceptance |
| **Clovent.Desktop.UiQa** | `src/Clovent.Desktop.UiQa/Clovent.Desktop.UiQa.csproj` | `net10.0-windows` | `Exe` | ~10-50 tests | Desktop WinForms & POS Workflow Acceptance |
| **Clovent.Domain.Tests** | `src/Clovent.Domain.Tests/Clovent.Domain.Tests.csproj` | `net10.0` | `Library` | ~10-50 tests | Domain Logic & Aggregates |
| **Clovent.Identity.Application.Tests** | `src/Clovent.Identity.Application.Tests/Clovent.Identity.Application.Tests.csproj` | `net10.0` | `Library` | ~10-50 tests | CQRS Commands & Queries |
| **Clovent.Identity.Infrastructure.Tests** | `src/Clovent.Identity.Infrastructure.Tests/Clovent.Identity.Infrastructure.Tests.csproj` | `net10.0` | `Library` | ~10-50 tests | EF Core SQLite In-Memory & Migrations |
| **Clovent.Identity.Tests** | `src/Clovent.Identity.Tests/Clovent.Identity.Tests.csproj` | `net10.0` | `Library` | ~10-50 tests | Domain Logic & Aggregates |
| **Clovent.Inventory.Application.Tests** | `src/Clovent.Inventory.Application.Tests/Clovent.Inventory.Application.Tests.csproj` | `net10.0` | `Library` | ~10-50 tests | CQRS Commands & Queries |
| **Clovent.Inventory.Infrastructure.Tests** | `src/Clovent.Inventory.Infrastructure.Tests/Clovent.Inventory.Infrastructure.Tests.csproj` | `net10.0` | `Library` | ~10-50 tests | EF Core SQLite In-Memory & Migrations |
| **Clovent.Inventory.Tests** | `src/Clovent.Inventory.Tests/Clovent.Inventory.Tests.csproj` | `net10.0` | `Library` | ~10-50 tests | Domain Logic & Aggregates |
| **Clovent.MasterData.Application.Tests** | `src/Clovent.MasterData.Application.Tests/Clovent.MasterData.Application.Tests.csproj` | `net10.0` | `Library` | ~10-50 tests | CQRS Commands & Queries |
| **Clovent.MasterData.Infrastructure.Tests** | `src/Clovent.MasterData.Infrastructure.Tests/Clovent.MasterData.Infrastructure.Tests.csproj` | `net10.0` | `Library` | ~10-50 tests | EF Core SQLite In-Memory & Migrations |
| **Clovent.MasterData.Tests** | `src/Clovent.MasterData.Tests/Clovent.MasterData.Tests.csproj` | `net10.0` | `Library` | ~10-50 tests | Domain Logic & Aggregates |
| **Clovent.Platform.Tests** | `src/Clovent.Platform.Tests/Clovent.Platform.Tests.csproj` | `net10.0` | `Library` | ~10-50 tests | Domain Logic & Aggregates |
| **Clovent.Restaurant.Application.Tests** | `src/Clovent.Restaurant.Application.Tests/Clovent.Restaurant.Application.Tests.csproj` | `net10.0` | `Library` | ~10-50 tests | CQRS Commands & Queries |
| **Clovent.Restaurant.Infrastructure.Tests** | `src/Clovent.Restaurant.Infrastructure.Tests/Clovent.Restaurant.Infrastructure.Tests.csproj` | `net10.0` | `Library` | ~10-50 tests | EF Core SQLite In-Memory & Migrations |
| **Clovent.Restaurant.Tests** | `src/Clovent.Restaurant.Tests/Clovent.Restaurant.Tests.csproj` | `net10.0` | `Library` | ~10-50 tests | Domain Logic & Aggregates |

### 9.2 Specialized QA & Live Acceptance Assets (`qa/`)

In addition to unit test projects, the repository includes a live UI verification suite:
- **`qa/live_ui/`:** Contains 15 PowerShell automation scripts (`flow.ps1`, `drv.ps1`, `login2.ps1`, `btns.ps1`) driving the live POS application via UI Automation (UIA) and Windows messaging.
- **Acceptance Screenshots:** 20+ PNG reference screenshots capturing verified UI states (PIN authentication, Dining Type selection, Cart calculation, Split tender).
- **`qa/live_ui/ClickTool.cs`:** Native C# helper for precise mouse simulation during automated acceptance runs.

---

## 10. Database, SQL, Migrations & Seed Data

The Clovent system utilizes **Entity Framework Core 10** with dual provider support: Microsoft SQL Server for enterprise production and SQLite for local development and integration testing.

### 10.1 EF Core Migrations Inventory

| Bounded Context | Migration File | Date / Target Feature |
|---|---|---|
| **Restaurant** | `20260912025646_AddShiftManagement.cs` | Shift tracking, cash movements, till audits |
| **Restaurant** | `20260914160000_AddCustomerIsDefault.cs` | Walk-in default customer flag |
| **Restaurant** | `20260915054626_AddSmartPosFeatures.cs` | Quick Order templates, Recommendation rules, Order health |
| **Catalog** | Initial Catalog Migrations | Product catalog, Categories, Modifiers |
| **Identity** | Initial Identity Migrations | Users, Roles, Permissions, Refresh Tokens |
| **Authentication** | Initial Auth Migrations | Credential stores, PIN hashes |
| **Inventory** | Initial Inventory Migrations | Stock levels, Warehouses, Stock adjustments |
| **MasterData** | Initial MasterData Migrations | Currencies, Tax rates, Payment modes |

### 10.2 Standalone SQL Scripts (`qa/`)

| SQL File | Size | Purpose |
|---|---|---|
| `qa/v3_sql1.sql` | 420 B | Table occupancy and status verification query |
| `qa/v3_sql2.sql` | 318 B | Customer default flag sanity query |
| `qa/v3_sql3.sql` | 776 B | Active orders and order line item diagnostic query |
| `qa/v3_sql4.sql` | 231 B | Payment method distribution check |
| `qa/v3_inv1.sql` | 315 B | Inventory level verification query |
| `qa/v3_inv2.sql` | 1,060 B | Stock decrement verification query |

### 10.3 Seed Data Tasks
- `src/Clovent.Desktop/Seed/DevelopmentAuthorizationSeedStartupTask.cs`: Seeds initial SuperAdmin, Cashier, and Manager roles and users.
- `src/Clovent.Restaurant.Infrastructure/Persistence/RestaurantPersistenceInitializer.cs`: Seeds sample tables, dining sections, and payment modes.

---

## 11. Configuration Files

Configuration is managed via Microsoft Extensions Configuration (JSON providers and environment variables):

| Configuration File | Relative Path | Purpose | Sensitive Data Redaction |
|---|---|---|---|
| `appsettings.json` | `src/Clovent.Desktop/appsettings.json` | Production desktop configuration, database connection strings, logging levels | `Server=...;Password=[SECRET REDACTED]` |
| `appsettings.Development.json` | `src/Clovent.Desktop/appsettings.Development.json` | Development overrides (Detailed errors, debug logging) | None |
| `launchSettings.json` | `src/Clovent.Desktop/Properties/launchSettings.json` | Visual Studio runtime launch profiles and environment variables | None |
| `appsettings.json` | `Tools/Clovent.CLI/src/Clovent.Modules.Identity/appsettings.json` | Identity scaffolding database target | `[SECRET REDACTED]` |
| `.editorconfig` | Root and subdirectories (101 files across obj/debug) | Roslyn compiler and code formatting rules | None |
| `.gitignore` | Root `.gitignore` | Git repository ignore patterns | None |

---

## 12. Project Dependency Map

The solution strictly complies with Onion / Clean Architecture dependency rules where dependencies flow inward toward Domain:

```text
Clovent.Desktop (WinForms UI Host)
  │
  ├──► Clovent.Platform (Cross-cutting Hosting & Logging)
  │
  ├──► Clovent.Authentication.Infrastructure ──► Clovent.Authentication.Application ──► Clovent.Authentication (Domain)
  │                                                                                          │
  ├──► Clovent.Identity.Infrastructure       ──► Clovent.Identity.Application       ──► Clovent.Identity (Domain)
  │                                                                                          │
  ├──► Clovent.MasterData.Infrastructure     ──► Clovent.MasterData.Application     ──► Clovent.MasterData (Domain)
  │                                                                                          │
  ├──► Clovent.Catalog.Infrastructure        ──► Clovent.Catalog.Application        ──► Clovent.Catalog (Domain)
  │                                                                                          │
  ├──► Clovent.Inventory.Infrastructure      ──► Clovent.Inventory.Application      ──► Clovent.Inventory (Domain)
  │                                                                                          │
  └──► Clovent.Restaurant.Infrastructure     ──► Clovent.Restaurant.Application     ──► Clovent.Restaurant (Domain)
                                                                                             │
                                                                                             ▼
                                                                                       Clovent.Domain (Core Enterprise Kernel)
```

### 12.1 Dependency Findings
- **Circular Dependencies:** None detected. The DAG (Directed Acyclic Graph) is completely acyclic.
- **Suspicious References:** `Tools/Clovent.CLI/Clovent.CBOS.Desktop` contains duplicate project references to `src/Clovent.Core` but has empty class implementations.
- **Standalone Unconsumed Projects:** `scratch/DesignerTest/DesignerTest.csproj` is an isolated test project not referenced by any consumer.

---

## 13. NuGet Package Reference Directory

| Package ID | Typical Version | Purpose | Consuming Projects |
|---|---|---|---|
| `Microsoft.EntityFrameworkCore` | `10.0.10` | ORM Engine | Infrastructure projects, Tools |
| `Microsoft.EntityFrameworkCore.SqlServer` | `10.0.10` | SQL Server Database Provider | Infrastructure projects |
| `Microsoft.EntityFrameworkCore.Sqlite` | `10.0.10` | SQLite In-Memory Provider | Infrastructure Test projects |
| `Microsoft.EntityFrameworkCore.Design` | `10.0.10` | EF Core Migrations Tooling | Infrastructure projects |
| `MediatR` | `12.4.1` | In-process CQRS Command/Query Dispatching | Application projects |
| `DevExpress.Win` | `26.1.4-pre-26179` | WinForms UI Component Suite (Grids, Ribbon, POS UI) | `Clovent.Desktop` |
| `DevExpress.Reporting.Core` | `26.1.4-pre-26179` | Report and Receipt Printing Engine | `Clovent.Desktop` |
| `DevExpress.Images` | `26.1.4-pre-26179` | Vector and raster iconography for DevExpress UI | `Clovent.Desktop` |
| `Microsoft.Extensions.Hosting` | `10.0.10` | Generic Host, DI Lifecycle, Logging | `Clovent.Desktop`, `Clovent.Platform` |
| `BCrypt.Net-Next` | `4.2.0` | Password and PIN cryptographic hashing | `Clovent.Authentication`, `Clovent.Identity` |
| `Spectre.Console` / `Spectre.Console.Cli` | `0.57.2` | CLI formatting and command parsing | `Tools/Clovent.CLI` |
| `xunit` / `xunit.runner.visualstudio` | `2.9.3` / `3.1.4` | Automated Unit & Integration Testing | All Test projects |
| `coverlet.collector` | `6.0.4` | Code Coverage Collection | All Test projects |

---

## 14. Comprehensive File & Folder Counts

### 14.1 Global Metric Summary

| Category | Metric Value | Notes |
|---|---|---|
| **Total Solution Projects (.csproj)** | **59** | 43 in main slnx, 15 in tools/scratch |
| **Total Folders (Recursive)** | **3,232** | 907 source/doc folders, 2,325 build/git folders |
| **Total Files (Recursive)** | **13,622** | Complete workspace file inventory |
| **Total C# Source Files (`.cs`)** | **2,064** | 1,758 active source files; 306 in generated/obj folders |
| **Total Designer Files (`.Designer.cs`)** | **110** | WinForms and Migration designers |
| **Total Form Resource Files (`.resx`)** | **69** | WinForms UI resources |
| **Total Configuration Files** | **16** | `.json`, `.config`, `.user` (excluding build obj files) |
| **Total SQL Script Files (`.sql`)** | **6** | Diagnostic scripts in `qa/` |
| **Total Automated Test Source Files** | **318** | Test classes across 18 test projects |
| **Total JSON Files** | **417** | 12 active configs; 405 in `bin/obj/.vs/` |
| **Total XML / Manifest Files** | **565** | 59 csproj + 506 doc/build XMLs |
| **Total Build / Generated Files** | **11,085** | Compiled DLLs, PDBs, caches, obj files |
| **Total Images / Screenshots** | **304** | PNG/JPEG assets across `Images/`, `qa/`, and `docs/` |
| **Total Documentation Files (`.md`)** | **83** | Architecture, ADRs, user guides, test reports |
| **Total PowerShell Automation Scripts (`.ps1`)** | **104** | QA, UI automation, build helpers |

### 14.2 File Breakdown by Project (in `src/`)

| Project Name | Total Files | C# Files | Designer Files | Resx Files | Config / Other |
|---|---|---|---|---|---|
| **Clovent.Desktop** | 364 | 279 | 92 | 59 | 26 |
| **Clovent.Desktop.Tests** | 52 | 51 | 0 | 0 | 1 |
| **Clovent.Desktop.UiQa** | 2 | 1 | 0 | 0 | 1 |
| **Clovent.Restaurant** | 125 | 121 | 0 | 0 | 4 |
| **Clovent.Restaurant.Application** | 154 | 147 | 0 | 0 | 7 |
| **Clovent.Restaurant.Infrastructure** | 70 | 69 | 12 | 0 | 1 |
| **Clovent.Restaurant.Tests** | 16 | 15 | 0 | 0 | 1 |
| **Clovent.Restaurant.Application.Tests** | 55 | 47 | 0 | 0 | 8 |
| **Clovent.Restaurant.Infrastructure.Tests** | 17 | 15 | 0 | 0 | 2 |
| **Clovent.Catalog** | 78 | 77 | 0 | 0 | 1 |
| **Clovent.Catalog.Application** | 76 | 75 | 0 | 0 | 1 |
| **Clovent.Catalog.Infrastructure** | 32 | 31 | 3 | 0 | 1 |
| **Clovent.Catalog.Tests** | 10 | 9 | 0 | 0 | 1 |
| **Clovent.Catalog.Application.Tests** | 18 | 17 | 0 | 0 | 1 |
| **Clovent.Catalog.Infrastructure.Tests** | 11 | 10 | 0 | 0 | 1 |
| **Clovent.Inventory** | 33 | 31 | 0 | 0 | 2 |
| **Clovent.Inventory.Application** | 37 | 35 | 0 | 0 | 2 |
| **Clovent.Inventory.Infrastructure** | 21 | 19 | 1 | 0 | 2 |
| **Clovent.Inventory.Tests** | 5 | 4 | 0 | 0 | 1 |
| **Clovent.Inventory.Application.Tests** | 10 | 8 | 0 | 0 | 2 |
| **Clovent.Inventory.Infrastructure.Tests** | 7 | 6 | 0 | 0 | 1 |
| **Clovent.Identity** | 68 | 67 | 0 | 0 | 1 |
| **Clovent.Identity.Application** | 60 | 59 | 0 | 0 | 1 |
| **Clovent.Identity.Infrastructure** | 29 | 28 | 3 | 0 | 1 |
| **Clovent.Identity.Tests** | 20 | 19 | 0 | 0 | 1 |
| **Clovent.Identity.Application.Tests** | 18 | 17 | 0 | 0 | 1 |
| **Clovent.Identity.Infrastructure.Tests** | 10 | 9 | 0 | 0 | 1 |
| **Clovent.MasterData** | 56 | 55 | 0 | 0 | 1 |
| **Clovent.MasterData.Application** | 54 | 53 | 0 | 0 | 1 |
| **Clovent.MasterData.Infrastructure** | 28 | 27 | 1 | 0 | 1 |
| **Clovent.MasterData.Tests** | 10 | 9 | 0 | 0 | 1 |
| **Clovent.MasterData.Application.Tests** | 17 | 16 | 0 | 0 | 1 |
| **Clovent.MasterData.Infrastructure.Tests** | 11 | 10 | 0 | 0 | 1 |
| **Clovent.Authentication** | 42 | 41 | 0 | 0 | 1 |
| **Clovent.Authentication.Application** | 28 | 26 | 0 | 0 | 2 |
| **Clovent.Authentication.Infrastructure** | 23 | 22 | 1 | 0 | 1 |
| **Clovent.Authentication.Tests** | 15 | 14 | 0 | 0 | 1 |
| **Clovent.Authentication.Application.Tests** | 25 | 24 | 0 | 0 | 1 |
| **Clovent.Authentication.Infrastructure.Tests** | 12 | 11 | 0 | 0 | 1 |
| **Clovent.Platform** | 24 | 23 | 0 | 0 | 1 |
| **Clovent.Platform.Tests** | 9 | 7 | 0 | 0 | 2 |
| **Clovent.Domain** | 6 | 5 | 0 | 0 | 1 |
| **Clovent.Domain.Tests** | 7 | 6 | 0 | 0 | 1 |

---

## 15. Cleanup Candidates — DO NOT DELETE

> **NOTICE:** This is an audit list for developer review only. No files have been deleted, moved, or altered.

### Category A: Very Likely Unnecessary (Redundant Backups & Logs)
- **19 in-place `.backup-*` files** in `src/Clovent.Desktop/**` (e.g. `RestaurantPosForm.cs.backup-castfix-20260817-104258`, `RestaurantPosForm.Designer.cs.backup-ui-fixes-20260818`).
- **9 `.bak` snapshot files** in `backups/` directory.
- **`cart_diagnostics.txt`** (182 KB diagnostic dump from 2026-08-17 in root).
- **3 root screenshot captures:** `shot_login.png`, `shot_login2.png`, `test_screen.png`.

### Category B: Possibly Unnecessary (Orphaned Stubs & Ad-Hoc Scripts)
- **20 empty C# stubs in `Tools/Clovent.CLI/Clovent.CBOS.Desktop/`** (0 bytes / 3 bytes each; abandoned prototype).
- **7 empty C# services in `Tools/Clovent.CLI/Clovent.CLI/Services/`** (3 bytes each).
- **10 root-level ad-hoc test scripts:** `click.ps1`, `enum.ps1`, `fg.ps1`, `find.ps1`, `login.ps1`, `pclick.ps1`, `shoot.ps1`, `type.ps1`, `uia.ps1`, `uia_set.ps1` (Can be relocated to `qa/` or archived).

### Category C: Requires Investigation (Sandbox Projects & Staging Dumps)
- **`scratch/DesignerTest/`**: Standalone WinForms test project (5 files + bin/obj). Safe to archive once developer confirms Designer investigation is finished.
- **`src/Clovent.Desktop.UiQa/`**: Standalone UI QA project not in `.slnx`. Check whether to add it to solution or retain as external tool.
- **`.tmp.driveupload/` & `.tmp.drivedownload/`**: Cloud storage synchronizer temporary folders.

### Category D: Definitely Required (Core Business & Architecture Assets)
- All 43 projects under `src/` referenced in `Clovent.BusinessOperatingSystem.slnx`.
- All Domain aggregates, CQRS Handlers, EF Core Migrations, and Repositories.
- All production WinForms and UserControls in `Clovent.Desktop`.
- `Clovent.BusinessOperatingSystem.slnx`, `appsettings.json`, `.gitignore`.
- Documentation files (`00 Vision/` through `13 ADR/`, `Foundation/`).

### Category E: Generated / Build Output (Managed via Clean/Ignore)
- All `bin/` and `obj/` compilation directories (10,278 files).
- `.vs/` Visual Studio cache.
- `qa/live_ui/ClickTool.exe` (Compiled test binary).

### Category F: Test / QA Related (Valid Testing Assets)
- All 18 automated test projects (`src/*.Tests`).
- Acceptance scripts in `qa/live_ui/` and reference baseline screenshots in `qa/`.
- Diagnostic SQL queries in `qa/*.sql`.

---

## 16. Final Recommendation: Recommended Cleanup Review Order

When you are ready to review and clean the repository, execute your cleanup in the following prioritized sequence:

1. **Step 1: In-Place Source Backups (`.backup-*`)**
   - Review and delete the 19 in-place timestamped backup files located inside `src/Clovent.Desktop/`.
   - *Benefit:* Restores clean directory readability without any risk to production code.

2. **Step 2: External Backup Snapshot Directories (`backups/`)**
   - Move or delete the 3 historical backup directories from August 2026 (`config-security-fix-2026-08-17`, `logging-2026-08-17`, `rootcause-2026-08-18`).

3. **Step 3: Root Workspace Ad-Hoc Scripts & Screenshots**
   - Move the 10 temporary root PowerShell scripts (`click.ps1`, `login.ps1`, etc.) and test screenshots (`shot_login.png`) into `qa/ad_hoc/` or delete them.
   - Remove obsolete diagnostic logs (`cart_diagnostics.txt`, `qauia_pos.txt`).

4. **Step 4: CLI Scaffolding Empty Stubs (`Tools/Clovent.CLI`)**
   - Review the 27 empty/3-byte stub files in `Clovent.CBOS.Desktop` and `Clovent.CLI`. Either implement the classes or remove the dead stubs.

5. **Step 5: Scratch Sandbox (`scratch/DesignerTest`)**
   - Remove the `DesignerTest` prototype folder once WinForms Designer tests are verified.

6. **Step 6: UI QA Project Solution Integration (`src/Clovent.Desktop.UiQa`)**
   - Decide whether `Clovent.Desktop.UiQa` should be formally registered in `Clovent.BusinessOperatingSystem.slnx` under `/src/qa/` or kept standalone.

7. **Step 7: Build Artifact Cleansing**
   - Run `dotnet clean` across the solution and remove any stale `.tmp.driveupload` folders to eliminate build caches.

---

### Audit Completion & Integrity Confirmation
- **Audit Status:** Complete.
- **Modifications:** ZERO source files were modified, moved, renamed, deleted, or refactored.
- **Report Location:** `D:\Clovent Business Operating System\PROJECT_STRUCTURE_REPORT.md`
