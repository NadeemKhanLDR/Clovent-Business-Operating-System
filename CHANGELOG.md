# Changelog

All notable changes to the Clovent Business Operating System (CBOS) project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [1.2.2] - 2026-10-07

### Status
- **Engineering Status:** `READY FOR WINDOWS SANDBOX RETEST`
- **Release Guard:** `PASS` (0 violations)
- **Automated Tests:** **1,824 passed**, 0 failed, 7 skipped
- **Build Quality:** Debug clean (0 warnings / 0 errors), Release clean (0 warnings / 0 errors)

### Added
- **Comprehensive Enterprise Documentation Suite:** Complete documentation overhaul in `docs/` covering System Architecture, Bounded Contexts, Database Architecture & EF Core Migrations, POS & Payment Workflows, Continuity Mode & Operational Cache, Security & Licensing, Deployment & Installation, Operations & Health, Testing & Sandbox Acceptance, Release Engineering & ReleaseGuard, and Support Incident Runbooks.

### Fixed
- **Automated Test Filesystem Isolation:** Fully eliminated workstation state leakage during automated test execution. Monitored 37 workstation configuration paths across `%LOCALAPPDATA%\Clovent` and `%ProgramData%\Clovent` with 100% pre/post hash parity (0 created, 0 deleted, 0 modified).
- **POS Settings Store Isolation:** Refactored `PosSettingsStore` with thread-safe testing directory overrides (`SetTestingOverrides` and `ResetTestingOverrides`). Updated `RestaurantSetupViewTests` and `PosSettingsStoreTests` to execute in disposable temporary directories with deterministic cleanup.
- **Company Display Settings Isolation:** Isolated `CompanyDisplaySettings` persistence during unit testing to prevent modifying local company formats or timezone settings.
- **Commissioning & Database State Isolation:** Hardened `CommissioningStateService`, `CommissioningProvisioningCoordinator`, and `InitialMasterDataProvisioningService` testing overrides, guaranteeing zero modifications to production commissioning markers or database configurations during test execution.
- **Trial State Manager Idempotency:** Made `TrialStateManager.RecordCommercialLicenseInstalled()` strictly idempotent, eliminating redundant DPAPI re-encryption and hash drift of `trial.state` when commercial licenses are already active.
- **License Tamper Guard Isolation:** Implemented test-environment detection (`IsTestEnvironment()`) and custom testing paths in `LicenseTamperGuard`, preventing clock drift updates from touching host workstation `license_guard.dat` files during automated test runs.
- **Licensing & Security Test Fixtures:** Isolated `LicenseServiceTests`, `TrialStateManagerTests`, and `SecurityAndLicensingHardeningTests` to run against isolated temporary sandboxes with strict deterministic disposal.
- **Workstation Startup State Protection:** Verified that host workstation runtime resolution (`Clovent_BusinessOperatingSystem`, license validity, terminal mappings, and schema compatibility) remains completely unaffected by test suite execution.

### Preserved
- **Global Typography & High-DPI Support (v1.2.1):** Preserved proportional font scaling, unified `DesktopStyle` constants, ribbon and grid typography, and button/dialog layout boundaries across standard workstation scaling factors.
- **Local Operational Cache & Enhanced Continuity (v1.2.0):** Maintained DPAPI-protected local operational caching, HMAC integrity validation, emergency cash journal replay, and collision-safe local receipt numbers.

---

## [1.2.1] - 2026-10-06

### Status
- **Engineering Status:** Certified Release Package

### Changed
- **Global Desktop Typography:** Unified typography scale across Ribbon navigation, Quick Orders, Smart Combo builders, GridViews, and modal dialogs.
- **High-DPI Layout Alignment:** Eliminated button clipping and label truncation on high-DPI displays.

---

## [1.2.0] - 2026-10-06

### Status
- **Engineering Status:** Certified Release Package

### Added
- **Local Operational Cache:** Encrypted offline catalog and pricing cache (`ProtectedOperationalCacheStore`) for resilient Continuity Mode order taking.
- **Emergency Cash Journal:** Append-only local transaction journal with HMAC integrity signatures.
- **Exactly-Once Replay Engine:** Replay coordinator validating signatures and reconciling emergency sales into SQL Server upon reconnection.
- **Operations Health Center:** Workstation diagnostic screen monitoring database connectivity, outbox queue depth, and cache status.
