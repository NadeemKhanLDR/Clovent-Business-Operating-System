# Clovent Business Operating System (CBOS) — Release Checklist

> **Standard Header**
> - **Document ID:** REL-CHK-001
> - **Category:** Release Engineering
> - **Target Audience:** Release Engineers, QA Leads, Product Owners, Security Auditors
> - **Status:** VALIDATED
> - **Last Updated:** 2026-10-07

---

## 1. Overview & Release Verification Standard

This checklist is the authoritative gate for qualifying, packaging, and signing off on any production distribution of the Clovent Business Operating System (CBOS). Every stage must be strictly satisfied and signed off before customer delivery.

In accordance with CBOS quality standards:
- **`LIVE UI NOT EXECUTED`** and **`VISUAL STUDIO DESIGNER UI NOT EXECUTED`** must be maintained unless interactive verification was explicitly executed.
- No release may proceed with failing automated unit, integration, or ReleaseGuard scans.

---

## 2. Pre-Release Engineering Gate

- [ ] **Repository Isolation Verified**
  - [ ] Main branch (`origin/main`) is clean with no uncommitted working tree modifications.
  - [ ] Git commit hash recorded: `________________________________________`
  - [ ] Branch status verified: `git status --porcelain` returns empty.
- [ ] **Architecture & Schema Freeze**
  - [ ] Database migrations reviewed across all schemas (`Authentication`, `Identity`, `MasterData`, `Catalog`, `Inventory`, `Restaurant`).
  - [ ] No migrations contain direct DDL altering runtime permissions or dropping critical tables without upgrade path.
  - [ ] Bounded context boundaries intact; zero cross-context direct DbContext references.
- [ ] **Dependency & Vulnerability Audit**
  - [ ] .NET 10 SDK (v10.0.100+) baseline verified.
  - [ ] NuGet package references locked:
    - EF Core: `10.0.10`
    - MediatR: `12.4.1`
    - DevExpress WinForms: `26.1.4-pre-26179`
    - xUnit: `2.9.3`
  - [ ] `dotnet list package --vulnerable` returns zero critical or high vulnerabilities.

---

## 3. Build & Compilation Verification Gate

- [ ] **Clean Solution Build (Debug)**
  - [ ] Executed: `dotnet build Clovent.BusinessOperatingSystem.slnx -c Debug`
  - [ ] Exit Code: `0`
  - [ ] Zero compile errors; zero compile warnings treated as errors.
- [ ] **Clean Solution Build (Release)**
  - [ ] Executed: `dotnet build Clovent.BusinessOperatingSystem.slnx -c Release`
  - [ ] Exit Code: `0`
  - [ ] Release binaries produced in `bin\Release\net10.0-windows\` and `bin\Release\net10.0\`.
- [ ] **Assembly Metadata & Version Alignment**
  - [ ] AssemblyVersion matches target release (e.g. `1.2.2.0`).
  - [ ] FileVersion matches target release.
  - [ ] InformationalVersion includes commit SHA.

---

## 4. Automated Test & QA Suite Gate

- [ ] **Unit & Application Tests**
  - [ ] `Clovent.Authentication.Tests`: Passed (100%)
  - [ ] `Clovent.Identity.Tests` & `Identity.Application.Tests`: Passed (100%)
  - [ ] `Clovent.MasterData.Tests` & `MasterData.Application.Tests`: Passed (100%)
  - [ ] `Clovent.Catalog.Tests` & `Catalog.Application.Tests`: Passed (100%)
  - [ ] `Clovent.Inventory.Tests` & `Inventory.Application.Tests`: Passed (100%)
  - [ ] `Clovent.Restaurant.Tests` & `Restaurant.Application.Tests`: Passed (100%)
- [ ] **Persistence & Migration Tests**
  - [ ] All `*.Infrastructure.Tests` pass against isolated SQLite in-memory / SQL LocalDB.
  - [ ] Migration history schema isolation validated across all 6 contexts.
- [ ] **Desktop Client Tests**
  - [ ] `Clovent.Desktop.Tests`: Passed (100%)
  - [ ] High-DPI layout math and formatter unit tests passed.
  - [ ] Designer safety compliance tests passed.

---

## 5. Client Packaging & Self-Contained Publishing Gate

- [ ] **Publish Self-Contained Win-x64 Client**
  - [ ] Executed:
    ```powershell
    dotnet publish src\Clovent.Desktop\Clovent.Desktop.csproj -c Release -r win-x64 --self-contained true -o artifacts\release\Clovent.BusinessOperatingSystem-win-x64
    ```
  - [ ] Target directory created: `artifacts\release\Clovent.BusinessOperatingSystem-win-x64`
  - [ ] Client executable present: `Clovent.Desktop.exe`
  - [ ] DevExpress runtime libraries present (`DevExpress.*.dll`).
  - [ ] Target framework runtime present (`coreclr.dll`, `System.Private.CoreLib.dll`).

---

## 6. Automated Security & ReleaseGuard Scan Gate

- [ ] **Execute ReleaseGuard Security Scan**
  - [ ] Executed:
    ```powershell
    powershell -ExecutionPolicy Bypass -File tools\ReleaseGuard\ScanReleasePackage.ps1 -ReleaseDir artifacts\release\Clovent.BusinessOperatingSystem-win-x64
    ```
  - [ ] Scan Status: **CLEAN (0 violations)**
- [ ] **Forbidden Asset Exclusion Verification**
  - [ ] Zero source files (`*.cs`, `*.csproj`, `*.sln`, `*.slnx`) present.
  - [ ] Zero debug symbols (`*.pdb`) present.
  - [ ] Zero development configurations (`appsettings.Development.json`) present.
  - [ ] Zero private keys (`*.key`, `*.pem`, `*.pfx`, `*.snk`) present.
  - [ ] Zero license files (`clovent.lic`, `*development*.lic`) shipped in bundle.
  - [ ] Zero default or hardcoded credentials (`Admin123!`, etc.) in configuration.
  - [ ] Zero backup files (`*.bak`, `*.old`) present.

---

## 7. Windows Sandbox Clean-Machine Acceptance Gate

- [ ] **Sandbox Environment Preparation**
  - [ ] Windows Sandbox launched on clean Windows 11 / Windows 10 x64 host.
  - [ ] Staging folder mapped read-only into Sandbox.
- [ ] **Prerequisites Verification**
  - [ ] Confirm no developer tools, .NET SDK, or Visual Studio installed on target.
  - [ ] SQL Server Express (or remote SQL Server instance) configured with mixed-mode auth.
- [ ] **Database Provisioning Execution**
  - [ ] Provisioner executed against target database `Clovent_BusinessOperatingSystem`.
  - [ ] Runtime role `cbos_app` verified with restricted permissions (`db_datareader`, `db_datawriter`, `GRANT EXECUTE`).
  - [ ] Schema-scoped migration tables confirmed:
    - `[Authentication].[__EFMigrationsHistory]`
    - `[Identity].[__EFMigrationsHistory]`
    - `[MasterData].[__EFMigrationsHistory]`
    - `[Catalog].[__EFMigrationsHistory]`
    - `[Inventory].[__EFMigrationsHistory]`
    - `[Restaurant].[__EFMigrationsHistory]`
- [ ] **First-Run Commissioning Wizard Execution**
  - [ ] Application starts without runtime crash.
  - [ ] Terminal resolution executes; unassigned terminal triggers Commissioning Wizard.
  - [ ] Organization, Company, and Branch selected.
  - [ ] Initial Administrator credentials established via single-use endpoint.
  - [ ] Commissioning completed; terminal state persisted to DPAPI-protected local store.
- [ ] **Core Functional Workflow Sanity**
  - [ ] Login screen authenticates configured user.
  - [ ] Shift opened with opening cash float.
  - [ ] Order created, item added from catalog, cash tender processed.
  - [ ] Receipt generated to printer queue without crash.
  - [ ] Shift closed with counted cash; reconciliation variance calculated accurately.
- [ ] **Continuity & Offline Fault Injection Test**
  - [ ] SQL Server service stopped (`net stop MSSQLSERVER` or firewall drop).
  - [ ] Terminal transitions to Continuity Mode within threshold.
  - [ ] Operational Cache serves catalog items; cash orders accepted into HMAC-signed local journal.
  - [ ] Restricted operations (credit card, refunds, master data edits) cleanly disabled.
  - [ ] SQL Server restored; local journal successfully replayed and cleared.

---

## 8. Distribution Manifest & Integrity Checksums

- [ ] **Generate SHA-256 Checksums**
  - [ ] Executed:
    ```powershell
    Get-FileHash -Algorithm SHA256 (Get-ChildItem -Path artifacts\release\Clovent.BusinessOperatingSystem-win-x64 -File) | Out-File -FilePath artifacts\release\SHA256SUMS.txt -Encoding utf8
    ```
  - [ ] Installer package hash generated:
    ```powershell
    Get-FileHash -Algorithm SHA256 artifacts\release\CloventSetup-*.exe
    ```
- [ ] **Digital Signing (When Production Code Signing Certificate Configured)**
  - [ ] `signtool.exe sign /fd SHA256 /tr http://timestamp.digicert.com /td SHA256 ...`
  - [ ] Digital signature verified: `signtool.exe verify /pa CloventSetup-*.exe`

---

## 9. Formal Sign-Off Table

| Gate | Role / Title | Sign-Off Name | Date | Status |
| :--- | :--- | :--- | :--- | :--- |
| **Pre-Release Engineering** | Lead Architect | ____________________ | ____-____-____ | [ ] APPROVED |
| **Build & Test Verification** | QA Lead | ____________________ | ____-____-____ | [ ] APPROVED |
| **Security & ReleaseGuard** | Security Officer | ____________________ | ____-____-____ | [ ] APPROVED |
| **Clean Sandbox Acceptance** | Acceptance Tester | ____________________ | ____-____-____ | [ ] APPROVED |
| **Release Authorization** | Product Owner | ____________________ | ____-____-____ | [ ] APPROVED |

---

## 10. Post-Release Distribution Archive

Once all sign-offs are complete:
1. Store distribution binaries and `SHA256SUMS.txt` in the enterprise artifact repository.
2. Archive the signed release checklist with the release audit bundle.
3. Tag the repository release commit strictly following SemVer (e.g., `v1.2.2`).
4. Update `CHANGELOG.md` to reflect official release date and customer notes.
