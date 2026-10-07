# CBOS Test Execution Guide

| Attribute | Details |
| :--- | :--- |
| **Area** | Test Execution & Automation Tooling |
| **Audience** | Software Developers, QA Engineers, CI/CD Pipeline Maintainers |
| **Last Reviewed Version** | CBOS 1.2.2 |
| **Status** | **PROCEDURAL STANDARD** |
| **Classification** | **PUBLIC-SAFE** |

---

## 1. Complete Solution Test Suite Execution

To execute all 1,824+ automated unit, integration, and UI tests across all 42 projects in the CBOS solution:

```powershell
dotnet test Clovent.BusinessOperatingSystem.slnx -c Release --verbosity normal
```

- **Execution Time:** ~30–45 seconds on modern multi-core workstations.
- **Expected Outcome:** `Total tests: 1831. Passed: 1824. Failed: 0. Skipped: 7.`

---

## 2. Targeted Test Execution by Bounded Context

For rapid feedback during targeted feature development, execute specific project test suites:

### 2.1 Domain Primitives & Shared Core
```powershell
dotnet test src\Clovent.Domain.Tests\Clovent.Domain.Tests.csproj -c Debug
```

### 2.2 Restaurant POS Domain & Application Tests
```powershell
dotnet test src\Clovent.Restaurant.Tests\Clovent.Restaurant.Tests.csproj -c Debug
dotnet test src\Clovent.Restaurant.Application.Tests\Clovent.Restaurant.Application.Tests.csproj -c Debug
```

### 2.3 Identity & Authorization Tests
```powershell
dotnet test src\Clovent.Identity.Tests\Clovent.Identity.Tests.csproj -c Debug
dotnet test src\Clovent.Identity.Application.Tests\Clovent.Identity.Application.Tests.csproj -c Debug
```

### 2.4 Catalog & Inventory Tests
```powershell
dotnet test src\Clovent.Catalog.Tests\Clovent.Catalog.Tests.csproj -c Debug
dotnet test src\Clovent.Inventory.Tests\Clovent.Inventory.Tests.csproj -c Debug
```

### 2.5 Outbox Atomicity & Persistence Integration Tests
```powershell
dotnet test src\Clovent.Restaurant.Infrastructure.Tests\Clovent.Restaurant.Infrastructure.Tests.csproj `
    --filter "FullyQualifiedName~OutboxAtomicityIntegrationTests"
```

---

## 3. Workstation Test Isolation & Startup Verification

To verify that running tests does not leak files into host workstation configuration directories:

```powershell
dotnet test src\Clovent.Desktop.Tests\Clovent.Desktop.Tests.csproj `
    --filter "FullyQualifiedName~WorkstationStartupVerificationTests" `
    -c Release
```

- **Monitored Scope:** 37 configuration files across `%LOCALAPPDATA%\Clovent` and `%ProgramData%\Clovent`.
- **Pass Criteria:** 100% pre/post SHA-256 hash parity (0 files created, 0 deleted, 0 modified).

---

## 4. Automated ReleaseGuard Hygiene Scan

Scan a published distribution package for forbidden debug symbols, source files, or unencrypted secrets:

```powershell
powershell -ExecutionPolicy Bypass -File tools\ReleaseGuard\ScanReleasePackage.ps1 `
    -ReleaseDir artifacts\release\Clovent.BusinessOperatingSystem-win-x64
```

- **Pass Criteria:** `Exit Code: 0 (PASS - 0 security violations detected)`.

---

## 5. Code Coverage Collection

To generate cross-platform code coverage metrics:

```powershell
dotnet test Clovent.BusinessOperatingSystem.slnx -c Release `
    --collect:"XPlat Code Coverage" `
    --results-directory ./TestResults
```

---

## 6. Cross References
- [Testing Strategy](testing-strategy.md)
- [Windows Sandbox Acceptance](windows-sandbox-acceptance.md)
- [ReleaseGuard Documentation](../release/releaseguard.md)
- [Build Guide](../development/build.md)
