# Changelog

All notable changes to the Clovent Business Operating System (CBOS) project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [1.2.2] - 2026-10-07

### Status
- **Engineering Status:** `FROZEN INTERNAL ACCEPTANCE BASELINE ONLY` (Not certified for customer pilot or commercial GA)
- **Release Guard:** `PASS` (0 violations)
- **Automated Tests:** **1,824 passed**, 0 failed, 7 skipped
- **Build Quality:** Debug clean (0 warnings / 0 errors), Release clean (0 warnings / 0 errors)

### Documentation & Governance
- **Canonical Engineering Roadmap Synchronization:** Synchronized `docs/roadmap/engineering-roadmap.md` with the authoritative roadmap across CBOS 1.2.2 (frozen internal baseline), 1.2.3 (pilot hardening), 1.3.0 (commercial GA foundation), 1.3.1+ (resilience expansion), and 1.4.0+ (multi-terminal failover / multi-branch GA).
- **Permanent Engineering Rules:** Established operational domain rules in `.agents/rules/` including `financial-integrity.md` (decimal math, immutable completed transactions, centralized rounding policy, currency precision), `database.md` (migration safety, real SQL Server validation triggers), `security.md` (fail-closed authorization, brute-force defense), `testing.md` (workstation test isolation, truthful runtime evidence vocabulary), and `release.md` (source freeze, exact accepted artifact rule).
- **Definition of Done & Readiness Levels:** Formally documented `docs/development/definition-of-done.md` and `docs/development/readiness-levels.md` establishing strict evidence standards for software progression.
- **Product Decision Records (PDRs):** Established `docs/product/pdr/` with foundational product policies: PDR-0001 (Cash-Only Continuity Mode), PDR-0002 (Customer Data Ownership & Non-Destructive Licensing), PDR-0003 (Completed Financial Immutability), and PDR-0004 (2-Decimal Currency Scope).
- **Maturity Claim Reconciliation:** Corrected and factualized operational claims across refunds (marked explicitly as NOT YET IMPLEMENTED), QuickBooks (marked as ARCHITECTURE IMPLEMENTED / SIMULATED GATEWAY), card payments (manual tender classification only), database backup (pre-upgrade manual backup only, no automated maintenance service), observability (plain-text logger, lost stack traces documented), and authentication/authorization (PIN brute-force gap and admin username bypass recorded as 1.2.3 hardening items).
- **Production Code Isolation:** Confirmed zero production application code, migrations, or project files modified during documentation and governance integration.

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
