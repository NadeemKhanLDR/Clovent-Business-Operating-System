# CBOS Build & Compilation Guide

| Attribute | Details |
| :--- | :--- |
| **Area** | Build Engineering & Compilation Pipelines |
| **Audience** | Software Developers, Build Engineers, CI/CD Maintainers |
| **Last Reviewed Version** | CBOS 1.2.2 |
| **Status** | **PROCEDURAL STANDARD** |
| **Classification** | **PUBLIC-SAFE** |

---

## 1. Canonical Build Commands Reference

All commands listed below have been verified against the CBOS repository and solution file (`Clovent.BusinessOperatingSystem.slnx`). Execute these commands from the root directory of the repository:

### 1.1 Dependency Restoration
```powershell
dotnet restore Clovent.BusinessOperatingSystem.slnx
```

### 1.2 Debug Build (Local Development & Incremental Testing)
```powershell
dotnet build Clovent.BusinessOperatingSystem.slnx -c Debug
```
- **Policy:** Zero warnings and zero errors tolerated (`TreatWarningsAsErrors = true`).

### 1.3 Release Build (Optimization & Pre-Publish Verification)
```powershell
dotnet build Clovent.BusinessOperatingSystem.slnx -c Release
```
- **Policy:** Optimized compiler IL emission, dead code elimination, 0 warnings, 0 errors.

### 1.4 Automated Test Suite Execution
```powershell
# Run complete test suite across all 42 projects in Release configuration:
dotnet test Clovent.BusinessOperatingSystem.slnx -c Release --no-build

# Run targeted Desktop tests:
dotnet test src\Clovent.Desktop.Tests\Clovent.Desktop.Tests.csproj -c Release

# Run startup verification & workstation test isolation tests:
dotnet test src\Clovent.Desktop.Tests\Clovent.Desktop.Tests.csproj --filter "FullyQualifiedName~WorkstationStartupVerificationTests"
```

### 1.5 Publishing Self-Contained Desktop Client (`win-x64`)
Publishes a self-contained executable package requiring zero pre-installed .NET runtimes on target workstations:

```powershell
dotnet publish src\Clovent.Desktop\Clovent.Desktop.csproj `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=false `
    -o artifacts\release\Clovent.BusinessOperatingSystem-win-x64
```

### 1.6 Running Automated Security & Hygiene Scan (ReleaseGuard)
```powershell
powershell -ExecutionPolicy Bypass -File tools\ReleaseGuard\ScanReleasePackage.ps1 `
    -ReleaseDir artifacts\release\Clovent.BusinessOperatingSystem-win-x64
```

---

## 2. Compilation Flags & Project Properties

- **`<ApplicationHighDpiMode>PerMonitorV2</ApplicationHighDpiMode>`**: Configures Windows PerMonitorV2 High-DPI mode on `Clovent.Desktop`.
- **`<ForceDesignerDPIUnaware>true</ForceDesignerDPIUnaware>`**: Deliberate host setting preventing repeated Visual Studio Designer coordinate multiplication.
- **`<Nullable>enable</Nullable>`**: Enforces C# 13 nullable reference types across all projects.
- **`<ImplicitUsings>enable</ImplicitUsings>`**: Enables global usings for standard .NET namespaces.

---

## 3. Cross References
- [Environment Setup](environment-setup.md)
- [Project Structure](project-structure.md)
- [Testing Strategy](../testing/testing-strategy.md)
- [Release Process](../release/release-process.md)
- [ReleaseGuard Documentation](../release/releaseguard.md)
