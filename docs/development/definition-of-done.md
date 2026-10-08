# CBOS Definition of Done (DoD)

| Attribute | Details |
| :--- | :--- |
| **Area** | Engineering Governance & Quality Assurance |
| **Audience** | Developers, QA Engineers, Architects, Release Leads |
| **Last Reviewed Version** | CBOS 1.2.2 |
| **Status** | **PERMANENT STANDARD** |
| **Classification** | **PUBLIC-SAFE** |

---

## 1. Universal Engineering DoD (All Changes)

Before any commit or pull request is eligible for review, it must satisfy:
1. **Source Safety:** Modifies only permitted files within the designated bounded context or layer. Zero unintended edits to unrelated files.
2. **Build Cleanliness:** Builds with zero errors and zero new compiler warnings in both `Debug` and `Release` configurations via `dotnet build Clovent.BusinessOperatingSystem.slnx`.
3. **Automated Testing:** All targeted unit and integration tests execute and pass cleanly. Regression suites for affected contexts pass.
4. **Workstation & Test Isolation:** SQLite is an available fast-test provider, not mandatory for all tests. SQL Server tests execute against explicitly isolated disposable databases/environments. Tests leave zero residual state in host operational databases, `%ProgramData%`, `%LocalAppData%`, the Windows registry, or developer machine operational SQL instances.
5. **No Hardcoded Secrets:** Zero embedded credentials, development passwords (`Admin123!`), API tokens, or private keys.
6. **Documentation Synchronization:** Relevant architectural, design, or user-facing documentation in `docs/` is updated to reflect actual code behavior.
7. **Structured Report:** Work is documented using the standard Final Report structure defined in [AGENTS.md](../../AGENTS.md).

---

## 2. Specialized DoD Matrices by Change Category

### 2.1 Financial & Accounting Changes
*Scope:* Orders, order lines, payments, discounts, service charges, customer ledgers, cash movements, day close.
- [ ] Uses C# `decimal` exclusively for all monetary arithmetic; zero usage of `float` or `double`.
- [ ] Routes all rounding operations through the centralized `MoneyRoundingPolicy` (`AwayFromZero` selected pilot midpoint direction) at explicitly defined contract boundaries; zero ad-hoc calls to `Math.Round(...)`.
- [ ] Preserves intermediate decimal precision for rates, quantities, and unit pricing prior to defined rounding boundaries. Schema preserves existing storage (`decimal(18,2)` / `decimal(18,4)`).
- [ ] Completed financial facts and receipt snapshots remain strictly immutable; zero in-place updates or hard `DELETE` queries. Distinguish from operational metadata updates; zero financial corrections via metadata exceptions.
- [ ] Cancellations or adjustments produce compensating transactions (ledger reversals, credit notes). Compensating transactions correct posted financial effects; legitimate unpaid/unposted draft cancellation remains possible under domain rules.
- [ ] Enforces exact payment identity rule under TASK-05 (Payment Idempotency + Credit-Limit Approval): generate `PaymentAttemptId` once per intentional payment attempt, reuse across retries/retransmissions/duplicate UI submissions, new ID for separate payment, reject reuse with conflicting details (without prescribing new schema). Credit-limit approval cannot be forged with a bare Boolean.
- [ ] Customer ledger changes preserve approved single-entry subledger behavior; dual entries and automated advance creation/consumption require a separately reviewed financial contract. Do not introduce new advance-payment functionality.
- [ ] Reconciles non-overlapping financial terms without double-counting (applied settlement vs cash tendered/change, On Account classification, customer collections, and advance usage). Net cash sales include the cash portion of every supported split tender, including cash plus On Account, without double-counting collections. Mark unresolved mappings as TASK-03 (Financial Rounding Contract) / TASK-06 (Business Day Close Aggregation) contract work. TASK-06 aggregates real Tax and Discount totals, with Refund = 0 because refunds are explicitly disabled for the pilot.
- [ ] Includes parameterized xUnit invariant tests asserting penny-exact calculations.

### 2.2 Security & Authentication Changes
*Scope:* Authentication, session handling, user credentials, roles, permissions, licensing, commissioning.
- [ ] All authorization checks fail closed on null, missing context, or evaluation errors.
- [ ] Zero authorization decisions based on literal usernames (`admin`, `administrator`).
- [ ] Application-layer authorization is enforced independently of UI control visibility or enabled state.
- [ ] Managerial elevation challenges are action-specific and fail closed if elevation services are unreachable.
- [ ] Interactive login mechanisms enforce bounded brute-force protection (rate limiting, progressive delay, or lockout).
- [ ] License expiry preserves data ownership and authorized historical access; authentication and RBAC remain strictly enforced.
- [ ] Cardholder data (PAN, CVV, track data) is neither collected nor stored.
- [ ] Cryptographic private keys are never stored in the repository or shipped in distribution packages.

### 2.3 Database & Persistence Changes
*Scope:* EF Core entity configurations, migrations, repositories, connection strings, schema definitions.
- [ ] Exactly one physical SQL Server database (`Clovent_BusinessOperatingSystem`) targeted; zero secondary physical databases.
- [ ] Uses designated bounded-context schema (`[Authentication]`, `[Identity]`, `[MasterData]`, `[Catalog]`, `[Inventory]`, `[Restaurant]`).
- [ ] Uses schema-scoped migration history table `[<Schema>].[__EFMigrationsHistory]`.
- [ ] Explicit decimal precision declared on all numerical columns (e.g. `HasPrecision(18, 2)`).
- [ ] Preserves zero database migrations for CBOS 1.2.3. Concurrency tokens belong to CBOS 1.3.0.
- [ ] Destructive schema changes (column drops, table drops) are explicitly reviewed and accompanied by a data migration plan.
- [ ] Validated against explicitly isolated real SQL Server before release qualification.

### 2.4 User Interface & WinForms Changes
*Scope:* `Clovent.Desktop`, Forms, UserControls, Dialogs, DevExpress controls.
- [ ] Dual constructors provided: parameterized for runtime DI, parameterless decorated with `[EditorBrowsable(Never)]` for Designer.
- [ ] Design-time guard present in `Load` event handler: `if (DesignModeHelper.IsInDesignMode) return;`.
- [ ] `InitializeComponent()` contains layout and control instantiation only; zero database calls, DI resolutions, or async tasks.
- [ ] Runtime scaling handles 200%–250% High-DPI (`DeviceDpi` ~240) using `DesktopDpi.Scale(...)`.
- [ ] Filter bars use structured `TableLayoutPanel` with single rows; labels vertically centered (`Anchor = Left`).
- [ ] Truthful execution status claimed: `LIVE UI EXECUTED` only if tested interactively on display; otherwise `LIVE UI NOT EXECUTED` (which does not imply headless validation occurred).

### 2.5 External Integration Changes
*Scope:* Outbox handlers, QuickBooks synchronization, hardware drivers, printers.
- [ ] Integration calls implement timeout, retry, and circuit breaker protection.
- [ ] All external mutations are driven idempotently via the Transactional Outbox (governed under **TASK-07: Outbox Processor Startup**).
- [ ] Status explicitly documented: `PRODUCTION EXTERNAL INTEGRATION NOT VALIDATED` if tested against simulated gateways.
- [ ] External service outages do not crash or block local POS transaction capture.

### 2.6 Packaging & Release Changes
*Scope:* Release publishing, Inno Setup scripts, ReleaseGuard verification.
- [ ] Built in `Release` configuration and published as self-contained `win-x64` executable.
- [ ] All excluded files stripped: `*.cs`, `*.csproj`, `*.pdb`, `appsettings.Development.json`, `*development*.lic`, private keys.
- [ ] Release package passes `ScanReleasePackage.ps1` with exit code 0 (`PASS`).
- [ ] Authenticode signing is bounded to approved first-party deliverables (explicitly including `Clovent.Installer.Provisioner.exe`, `Clovent.Desktop.exe`, approved first-party DLLs `Clovent.*.dll`, and final installer) while preserving third-party binaries and signatures (governed under **TASK-10: Authenticode / Release Verification Pipeline**).
- [ ] Exact accepted installer verified via fresh installation in clean Windows Sandbox. (Note: CBOS 1.2.2 clean-machine acceptance is marked as PENDING unless actual runtime acceptance evidence is supplied).
- [ ] Accompanied by generated SHA-256 integrity checksum manifest.
