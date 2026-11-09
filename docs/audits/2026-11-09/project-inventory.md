# Project inventory

Static XML inventory; root-solution membership is explicit. References are compile edges, not proof of runtime ownership. Package versions and all references are preserved in [projects.json](projects.json).

| Project | Framework | Root solution | Direct references |
|---|---|---|---|
| scratch/AddOnsReadOnlyCheck/AddOnsReadOnlyCheck.csproj | net10.0-windows | no | Clovent.Desktop |
| scratch/DesignerTest/DesignerTest.csproj | net10.0-windows | no | Clovent.Desktop |
| scratch/SmartComboCheck/SmartComboCheck.csproj | net10.0-windows | no | Clovent.Desktop |
| scratch/SmartComboDiagnostic/SmartComboDiagnostic.csproj | net10.0-windows | no | Clovent.Platform, Clovent.Desktop, Clovent.Restaurant.Application, Clovent.Restaurant.Infrastructure |
| src/Clovent.Authentication/Clovent.Authentication.csproj | net10.0 | yes | Clovent.Domain, Clovent.Identity |
| src/Clovent.Authentication.Application/Clovent.Authentication.Application.csproj | net10.0 | yes | Clovent.Authentication, Clovent.Identity |
| src/Clovent.Authentication.Application.Tests/Clovent.Authentication.Application.Tests.csproj | net10.0 | yes | Clovent.Authentication.Application |
| src/Clovent.Authentication.Infrastructure/Clovent.Authentication.Infrastructure.csproj | net10.0 | yes | Clovent.Authentication, Clovent.Authentication.Application, Clovent.Identity, Clovent.Platform |
| src/Clovent.Authentication.Infrastructure.Tests/Clovent.Authentication.Infrastructure.Tests.csproj | net10.0 | yes | Clovent.Authentication.Infrastructure |
| src/Clovent.Authentication.Tests/Clovent.Authentication.Tests.csproj | net10.0 | yes | Clovent.Authentication |
| src/Clovent.Catalog/Clovent.Catalog.csproj | net10.0 | yes | Clovent.Domain, Clovent.MasterData |
| src/Clovent.Catalog.Application/Clovent.Catalog.Application.csproj | net10.0 | yes | Clovent.Catalog |
| src/Clovent.Catalog.Application.Tests/Clovent.Catalog.Application.Tests.csproj | net10.0 | yes | Clovent.Catalog.Application |
| src/Clovent.Catalog.Infrastructure/Clovent.Catalog.Infrastructure.csproj | net10.0 | yes | Clovent.Catalog, Clovent.Catalog.Application, Clovent.Platform |
| src/Clovent.Catalog.Infrastructure.Tests/Clovent.Catalog.Infrastructure.Tests.csproj | net10.0 | yes | Clovent.Catalog.Infrastructure |
| src/Clovent.Catalog.Tests/Clovent.Catalog.Tests.csproj | net10.0 | yes | Clovent.Catalog |
| src/Clovent.Desktop/Clovent.Desktop.csproj | net10.0-windows | yes | Clovent.Platform, Clovent.Authentication.Application, Clovent.Authentication.Infrastructure, Clovent.Identity, Clovent.Identity.Application, Clovent.Identity.Infrastructure, Clovent.MasterData, Clovent.MasterData.Application, Clovent.MasterData.Infrastructure, Clovent.Catalog, Clovent.Catalog.Application, Clovent.Catalog.Infrastructure, Clovent.Inventory, Clovent.Inventory.Application, Clovent.Inventory.Infrastructure, Clovent.Restaurant, Clovent.Restaurant.Application, Clovent.Restaurant.Infrastructure |
| src/Clovent.Desktop.Tests/Clovent.Desktop.Tests.csproj | net10.0-windows | yes | Clovent.Desktop |
| src/Clovent.Desktop.UiQa/Clovent.Desktop.UiQa.csproj | net10.0-windows | no | Clovent.Desktop |
| src/Clovent.Domain/Clovent.Domain.csproj | net10.0 | yes |  |
| src/Clovent.Domain.Tests/Clovent.Domain.Tests.csproj | net10.0 | yes | Clovent.Domain |
| src/Clovent.Identity/Clovent.Identity.csproj | net10.0 | yes | Clovent.Domain |
| src/Clovent.Identity.Application/Clovent.Identity.Application.csproj | net10.0 | yes | Clovent.Identity |
| src/Clovent.Identity.Application.Tests/Clovent.Identity.Application.Tests.csproj | net10.0 | yes | Clovent.Identity.Application |
| src/Clovent.Identity.Infrastructure/Clovent.Identity.Infrastructure.csproj | net10.0 | yes | Clovent.Identity, Clovent.Identity.Application, Clovent.Platform |
| src/Clovent.Identity.Infrastructure.Tests/Clovent.Identity.Infrastructure.Tests.csproj | net10.0 | yes | Clovent.Identity.Infrastructure |
| src/Clovent.Identity.Tests/Clovent.Identity.Tests.csproj | net10.0 | yes | Clovent.Identity |
| src/Clovent.Inventory/Clovent.Inventory.csproj | net10.0 | yes | Clovent.Domain, Clovent.MasterData, Clovent.Catalog |
| src/Clovent.Inventory.Application/Clovent.Inventory.Application.csproj | net10.0 | yes | Clovent.Inventory |
| src/Clovent.Inventory.Application.Tests/Clovent.Inventory.Application.Tests.csproj | net10.0 | yes | Clovent.Inventory.Application |
| src/Clovent.Inventory.Infrastructure/Clovent.Inventory.Infrastructure.csproj | net10.0 | yes | Clovent.Inventory, Clovent.Inventory.Application, Clovent.Platform |
| src/Clovent.Inventory.Infrastructure.Tests/Clovent.Inventory.Infrastructure.Tests.csproj | net10.0 | yes | Clovent.Inventory.Infrastructure |
| src/Clovent.Inventory.Tests/Clovent.Inventory.Tests.csproj | net10.0 | yes | Clovent.Inventory |
| src/Clovent.MasterData/Clovent.MasterData.csproj | net10.0 | yes | Clovent.Domain, Clovent.Identity |
| src/Clovent.MasterData.Application/Clovent.MasterData.Application.csproj | net10.0 | yes | Clovent.Domain, Clovent.Identity, Clovent.MasterData, Clovent.Platform |
| src/Clovent.MasterData.Application.Tests/Clovent.MasterData.Application.Tests.csproj | net10.0 | yes | Clovent.Domain, Clovent.Identity, Clovent.MasterData, Clovent.MasterData.Application, Clovent.Platform |
| src/Clovent.MasterData.Infrastructure/Clovent.MasterData.Infrastructure.csproj | net10.0 | yes | Clovent.MasterData, Clovent.MasterData.Application, Clovent.Platform |
| src/Clovent.MasterData.Infrastructure.Tests/Clovent.MasterData.Infrastructure.Tests.csproj | net10.0 | yes | Clovent.MasterData.Infrastructure |
| src/Clovent.MasterData.Tests/Clovent.MasterData.Tests.csproj | net10.0 | yes | Clovent.MasterData |
| src/Clovent.Platform/Clovent.Platform.csproj | net10.0 | yes |  |
| src/Clovent.Platform.Tests/Clovent.Platform.Tests.csproj | net10.0 | yes | Clovent.Platform |
| src/Clovent.Restaurant/Clovent.Restaurant.csproj | net10.0 | yes | Clovent.Domain, Clovent.Identity, Clovent.MasterData, Clovent.Catalog |
| src/Clovent.Restaurant.Application/Clovent.Restaurant.Application.csproj | net10.0 | yes | Clovent.Restaurant, Clovent.Platform, Clovent.Catalog.Application, Clovent.Inventory.Application, Clovent.Identity.Application |
| src/Clovent.Restaurant.Application.Tests/Clovent.Restaurant.Application.Tests.csproj | net10.0 | yes | Clovent.Restaurant.Application |
| src/Clovent.Restaurant.Infrastructure/Clovent.Restaurant.Infrastructure.csproj | net10.0 | yes | Clovent.Restaurant, Clovent.Restaurant.Application, Clovent.Platform |
| src/Clovent.Restaurant.Infrastructure.Tests/Clovent.Restaurant.Infrastructure.Tests.csproj | net10.0 | yes | Clovent.Restaurant.Infrastructure, Clovent.Catalog.Infrastructure, Clovent.Inventory.Infrastructure |
| src/Clovent.Restaurant.Tests/Clovent.Restaurant.Tests.csproj | net10.0 | yes | Clovent.Restaurant |
| Tools/AcceptanceViewRunner/AcceptanceViewRunner.csproj | net10.0-windows | no |  |
| Tools/Clovent.CLI/Clovent.CBOS.Desktop/Clovent.CBOS.Desktop.csproj | net10.0-windows | no |  |
| Tools/Clovent.CLI/Clovent.CLI/Clovent.CLI.csproj | net10.0 | no | Clovent.Core, Clovent.Generator, Clovent.Documents, Clovent.Configuration, Clovent.Shared |
| Tools/Clovent.CLI/Clovent.PackageManager/Clovent.PackageManager.csproj | net10.0 | no | Clovent.PackageManager.Core, Clovent.PackageManager.Abstractions |
| Tools/Clovent.CLI/Clovent.PackageManager.Abstractions/Clovent.PackageManager.Abstractions.csproj | net10.0 | no |  |
| Tools/Clovent.CLI/Clovent.PackageManager.Core/Clovent.PackageManager.Core.csproj | net10.0 | no | Clovent.PackageManager.Abstractions |
| Tools/Clovent.CLI/Clovent.PackageManager.Tests/Clovent.PackageManager.Tests.csproj | net10.0 | no |  |
| Tools/Clovent.CLI/src/Clovent.Configuration/Clovent.Configuration.csproj | net10.0 | no |  |
| Tools/Clovent.CLI/src/Clovent.Core/Clovent.Core.csproj | net10.0 | no |  |
| Tools/Clovent.CLI/src/Clovent.Documents/Clovent.Documents.csproj | net10.0 | no | Clovent.Core |
| Tools/Clovent.CLI/src/Clovent.Generator/Clovent.Generator.csproj | net10.0 | no | Clovent.Core, Clovent.Documents, Clovent.Templates, Clovent.Configuration, Clovent.Shared |
| Tools/Clovent.CLI/src/Clovent.Modules.Identity/Clovent.Modules.Identity.csproj | net10.0 | no | Clovent.Core, Clovent.Shared |
| Tools/Clovent.CLI/src/Clovent.Shared/Clovent.Shared.csproj | net10.0 | no |  |
| Tools/Clovent.CLI/src/Clovent.Templates/Clovent.Templates.csproj | net10.0 | no | Clovent.Core |
| Tools/Clovent.CLI/tests/Clovent.Core.Tests/Clovent.Core.Tests.csproj | net10.0 | no | Clovent.Core |
| Tools/Clovent.CLI/tests/Clovent.Generator.Tests/Clovent.Generator.Tests.csproj | net10.0 | no | Clovent.Generator, Clovent.Core, Clovent.Templates, Clovent.Documents |
| Tools/Clovent.Installer.Provisioner/Clovent.Installer.Provisioner.csproj | net10.0-windows | no | Clovent.Platform, Clovent.Authentication.Infrastructure, Clovent.Identity.Infrastructure, Clovent.MasterData.Infrastructure, Clovent.Catalog.Infrastructure, Clovent.Inventory.Infrastructure, Clovent.Restaurant.Infrastructure |
| Tools/InnoSetup/Examples/MyDll/C#/MyDll.csproj | legacy/unspecified | no |  |
| Tools/LicenseIssuer/Clovent.LicenseIssuer.csproj | net10.0 | no |  |
| Tools/ScreenCaptureUtil/ScreenCaptureUtil.csproj | net10.0-windows | no |  |
| Tools/SupportFixtureRunner/SupportFixtureRunner.csproj | net10.0-windows | no | Clovent.Desktop |
