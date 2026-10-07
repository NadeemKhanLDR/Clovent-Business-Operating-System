# CBOS 1.2.2 Release-Integrity Closure & Packaging Report

## 1. Executive Summary
- **Release Version:** 1.2.2 (consistently set across Desktop, Provisioner, and Inno Setup).
- **Test Isolation Status:** Fully resolved. 0 files created, 0 files deleted, 0 files modified on host workstation.
- **Regression Test Status:** 1,824 passed, 0 failed, 7 skipped across all bounded contexts.
- **Build Status:** Debug (0 warnings, 0 errors), Release (0 warnings, 0 errors).
- **ReleaseGuard Status:** 0 violations across release directory, extracted `{app}`, and full installer.
- **Deployment Status:** Certified and ready for Windows Sandbox retesting.

---

## 2. Root Cause Analysis & Test Isolation Fixes

### Defects Identified
1. **`pos_settings.json` Overwrite:** `RestaurantSetupViewTests.cs` called `view.SaveSettingsAsync()` without setting testing path overrides, writing to `%LOCALAPPDATA%\Clovent\pos_settings.json`.
2. **`trial.state` Mutation:** `TrialStateManager.RecordCommercialLicenseInstalled()` unconditionally persisted state to disk even when `HasCommercialLicenseEverBeenInstalled` was already true. Because DPAPI uses a random salt on every encryption operation, redundant saves mutated the file hash.
3. **`license_guard.dat` Drift Write:** `LicenseTamperGuard.VerifyAndUpdateClock()` updated clock drift timestamps during automated test runs without checking for test execution environments.

### Resolutions Implemented
- **`PosSettingsStore`:** Added `SetTestingOverrides(string baseDirectory)` and `ResetTestingOverrides()`. Updated all test fixtures with temporary directories and deterministic `IDisposable` cleanup.
- **`TrialStateManager`:** Made `RecordCommercialLicenseInstalled()` strictly idempotent: early-returns when `state.HasCommercialLicenseEverBeenInstalled` is already set.
- **`LicenseTamperGuard`:** Added `SetTestingOverrides(string? customGuardFilePath)` and automatic test runner detection (`IsTestEnvironment()`) to suppress writes to real workstation state during tests.
- **Test Fixture Hardening:** Updated `LicenseServiceTests`, `TrialStateManagerTests`, `SecurityAndLicensingHardeningTests`, and `WorkstationStartupVerificationTests` to run against isolated temporary folders.

---

## 3. Workstation State Isolation Manifest Evidence

Monitored paths: 37 total (23 existing workstation files + 14 non-existing candidate paths).
Comparison of pre-test and post-test manifests:
- **Files Created:** 0
- **Files Deleted:** 0
- **Files Modified:** 0
- **Workstation Files Changed:** **NO** (100% Identical)

### Key Workstation Hashes (Pre-Test vs Post-Test):
- `%LOCALAPPDATA%\Clovent\Clovent.BusinessOperatingSystem\database.config.json`
  - SHA-256: `da2b54d35fbe5c056fb4ec0e0456911296582dad7fb361d8efd429524b3c0cdc` (Unchanged)
- `%LOCALAPPDATA%\Clovent\Clovent.BusinessOperatingSystem\commissioning.json`
  - SHA-256: `fe8237283209d68565fd73631434bc3b6e97bc4c5e26bf1216586c1958b30a22` (Unchanged)
- `%LOCALAPPDATA%\Clovent\BusinessOperatingSystem\Config\terminal.json`
  - SHA-256: `1e755a0de312f79026836893b70113e4941f22853f25e8c6c35475b8532d01f3` (Unchanged)
- `%LOCALAPPDATA%\Clovent\pos_settings.json`
  - SHA-256: `92c810f8a0ac9033dbb57477bcd8f6950e1fd2ed6c4d0f6b8a05c48593be2934` (Unchanged)
- `%LOCALAPPDATA%\Clovent\company_display_settings.json`
  - SHA-256: `66bebdfddd7cb18fcf22d333f10fe25a1818acd8aee2244354a80211e118ded3` (Unchanged)
- `%LOCALAPPDATA%\Clovent\Clovent.BusinessOperatingSystem\clovent.lic`
  - SHA-256: `6e44abdd743f95169b6d9ad0cdd1061704a4fe425ca883dbe13d3cb702fe5e98` (Unchanged)
- `%LOCALAPPDATA%\Clovent\Clovent.BusinessOperatingSystem\trial.state`
  - SHA-256: `27aa3c643337fb438c5959d95511781a5e5e0bec0f5424ed8b6f8d4ebf67a0bb` (Unchanged)
- `%LOCALAPPDATA%\Clovent\Clovent.BusinessOperatingSystem\license_guard.dat`
  - SHA-256: `a1963f4e6e00179b9ea08bcacca21b1373bdb1a170699d00cf5b11cc34cbee8d` (Unchanged)

---

## 4. Workstation Startup Verification Tests
Executed `WorkstationStartupVerificationTests`:
- Database Resolution: `Clovent_BusinessOperatingSystem` (Not `TestDb`).
- Schema Status: `Compatible` (`DatabaseSchemaCompatibilityValidator`).
- Commissioning Marker: Valid with HMAC-SHA256 signature intact.
- License Status: Authorized (`Commercial - Enterprise`).
- Terminal Configuration: Valid (`POS-TERM-01`).
- POS Settings: Valid (`BranchId`, `TerminalId`).
- Company Display Settings: Valid (`dd-MMM-yyyy`, `12 Hour`, precision 2).
- **Result:** 7 passed, 0 failed.

---

## 5. Full Solution Regression Results
Executed: `dotnet test Clovent.BusinessOperatingSystem.slnx -c Debug`
- **Total Passed:** 1,824
- **Total Failed:** 0
- **Total Skipped:** 7 (Live visual automation and screenshot fixtures)
- **Total Tests:** 1,831

### Project Breakdown:
- `Clovent.Authentication.Application.Tests`: 55 passed, 0 failed
- `Clovent.Authentication.Infrastructure.Tests`: 21 passed, 0 failed
- `Clovent.Identity.Application.Tests`: 61 passed, 0 failed
- `Clovent.Identity.Infrastructure.Tests`: 32 passed, 0 failed
- `Clovent.MasterData.Infrastructure.Tests`: 31 passed, 0 failed
- `Clovent.Catalog.Tests`: 45 passed, 0 failed
- `Clovent.Catalog.Application.Tests`: 39 passed, 0 failed
- `Clovent.Catalog.Infrastructure.Tests`: 31 passed, 0 failed
- `Clovent.Inventory.Tests`: 23 passed, 0 failed
- `Clovent.Inventory.Application.Tests`: 23 passed, 0 failed
- `Clovent.Inventory.Infrastructure.Tests`: 17 passed, 0 failed
- `Clovent.Restaurant.Tests`: 177 passed, 0 failed
- `Clovent.Restaurant.Application.Tests`: 395 passed, 0 failed
- `Clovent.Restaurant.Infrastructure.Tests`: 86 passed, 0 failed
- `Clovent.Desktop.Tests`: 788 passed, 0 failed, 7 skipped

---

## 6. Build Verification
- **Debug:** `dotnet build Clovent.BusinessOperatingSystem.slnx -c Debug` -> 0 Warning(s), 0 Error(s).
- **Release:** `dotnet build Clovent.BusinessOperatingSystem.slnx -c Release` -> 0 Warning(s), 0 Error(s).

---

## 7. Version Consistency Verification
- `src/Clovent.Desktop/Clovent.Desktop.csproj`: `<Version>1.2.2</Version>`, `<AssemblyVersion>1.2.2.0</AssemblyVersion>`
- `Tools/Clovent.Installer.Provisioner/Clovent.Installer.Provisioner.csproj`: `<Version>1.2.2</Version>`, `<AssemblyVersion>1.2.2.0</AssemblyVersion>`
- `installer/Clovent.BusinessOperatingSystem.iss`: `#define MyAppVersion "1.2.2"`, `OutputBaseFilename=Clovent.BusinessOperatingSystem-1.2.2-Setup`

---

## 8. Release Packaging & Hashes

### Rebuilt Deliverables:
1. **Application Release ZIP:**
   - Path: `artifacts\release\Clovent.BusinessOperatingSystem-1.2.2-win-x64.zip`
   - Size: 106,260,173 bytes (101.34 MB)
   - SHA-256: `FBA4AB399B1ADAD0411F8CD6D98222F100F86B8958F0E11C5D67933D2849AA90`

2. **Single-File Database Provisioner:**
   - Path: `Tools\Clovent.Installer.Provisioner\bin\publish\Clovent.Installer.Provisioner.exe`
   - Size: 44,632,790 bytes (42.57 MB)
   - SHA-256: `287A98DFA31E7B0101C10AC02272C21B276CFB034D763B01941F973130DADFA6`
   - Test execution (valid SQL): exit code 0 (`Compatible`).
   - Test execution (invalid SQL): exit code 1 (`SQL Server connection failed`).

3. **Production Installer Setup:**
   - Path: `artifacts\installer\Clovent.BusinessOperatingSystem-1.2.2-Setup.exe`
   - Size: 115,604,288 bytes (110.25 MB)
   - SHA-256: `AA90C2EC0CB8D14D9A47649E0E62D1419681438E274ED9F43F4EAF048D5A3E6F`

### Installer Extraction & Integrity Verification:
- `{app}` payload file count: 473 files (matches published package).
- `{tmp}\Clovent.Installer.Provisioner.exe`: Extracted SHA-256 matches published provisioner exactly (`287A98DFA31E7B0101C10AC02272C21B276CFB034D763B01941F973130DADFA6`).
- Standalone execution of extracted provisioner: exit code 0 (`Compatible`).

---

## 9. ReleaseGuard Automated Security Scans
Executed `tools\ReleaseGuard\ScanReleasePackage.ps1` across all target directories:
1. `artifacts\release\Clovent.BusinessOperatingSystem-1.2.2-win-x64`: 473 files inspected -> **0 violations (PASS)**.
2. Extracted `{app}` directory: 473 files inspected -> **0 violations (PASS)**.
3. Extracted installer root: 475 files inspected -> **0 violations (PASS)**.
No private keys, development licenses, source code, PDBs, backup files, or plaintext passwords detected.

---

## 10. Prior Releases Integrity Confirmation
Previous release packages remain untouched with original hashes and timestamps:
- `Clovent.BusinessOperatingSystem-1.2.0-win-x64.zip` (106,257,404 bytes, LastWriteTime: 10/06/2026 15:04:15, SHA-256: `6655D5CFBD036E85D6AE96332F83239AED44E80280771D322AE6743D67A02A57`)
- `Clovent.BusinessOperatingSystem-1.2.0-Setup.exe` (115,588,463 bytes, LastWriteTime: 10/06/2026 15:03:02, SHA-256: `AA39B017C88AFCE6F93E79B87497F01A3DAC56C6B8BC18687DDDF516FEF66366`)
- `Clovent.BusinessOperatingSystem-1.2.1-win-x64.zip` (106,259,387 bytes, LastWriteTime: 10/06/2026 16:37:01, SHA-256: `37B11AA913664F27421C939398EE6DA8C90C7291E51ACB06BBF999215DDBD40F`)
- `Clovent.BusinessOperatingSystem-1.2.1-Setup.exe` (115,599,990 bytes, LastWriteTime: 10/06/2026 16:39:21, SHA-256: `89EB25B3420D9D303ACF9D6A5B0089FF1BD9D495EB0E7C98E9A631F56032DE51`)

---

## 11. Compliance & Execution Claims
- **AUTHENTICODE SIGNING:** PENDING (Production certificate signing occurs during CI/CD release stage).
- **LIVE UI NOT EXECUTED** (Headless build and verification; no interactive display manipulated).
- **VISUAL STUDIO DESIGNER UI NOT EXECUTED** (Designer surface was not interactively loaded).
- **CLEAN CLIENT MACHINE TEST NOT EXECUTED BY ANTIGRAVITY** (Windows Sandbox testing delegated to verification operator).

---

## 12. Final Status Statement
**READY FOR WINDOWS SANDBOX RETEST**
