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
4. **Workstation Isolation:** Tests leave zero residual state in host `%ProgramData%`, `%LocalAppData%`, the Windows registry, or developer machine SQL Server instances.
5. **No Hardcoded Secrets:** Zero embedded credentials, development passwords (`Admin123!`), API tokens, or private keys.
6. **Documentation Synchronization:** Relevant architectural, design, or user-facing documentation in `docs/` is updated to reflect actual code behavior.
7. **Structured Report:** Work is documented using the standard Final Report structure defined in [AGENTS.md](../../AGENTS.md).

---

## 2. Specialized DoD Matrices by Change Category

### 2.1 Financial & Accounting Changes
*Scope:* Orders, order lines, payments, discounts, service charges, customer ledgers, cash movements, day close.
- [ ] Uses C# `decimal` exclusively for all monetary arithmetic; zero usage of `float` or `double`.
- [ ] Routes all rounding operations through the centralized `MoneyRoundingPolicy`; zero ad-hoc calls to `Math.Round(...)`.
- [ ] Completed financial records remain strictly immutable; zero in-place updates or hard `DELETE` queries.
- [ ] Cancellations or adjustments produce compensating transactions (ledger reversals, credit notes).
- [ ] Enforces client-side `IdempotencyKey` on all payment mutations.
- [ ] Reconciles all standard financial invariants (Bill Total equation, Shift Drawer equation, Customer Ledger equation).
- [ ] Includes parameterized xUnit invariant tests asserting penny-exact calculations.

### 2.2 Security & Authentication Changes
*Scope:* Authentication, session handling, user credentials, roles, permissions, licensing, commissioning.
- [ ] All authorization checks fail closed on null, missing context, or evaluation errors.
- [ ] Zero authorization decisions based on literal usernames (`admin`, `administrator`).
- [ ] Application-layer authorization is enforced independently of UI control visibility or enabled state.
- [ ] Managerial elevation challenges are action-specific and fail closed if elevation services are unreachable.
- [ ] Interactive login mechanisms enforce bounded brute-force protection (rate limiting, progressive delay, or lockout).
- [ ] Cardholder data (PAN, CVV, track data) is neither collected nor stored.
- [ ] Cryptographic private keys are never stored in the repository or shipped in distribution packages.

### 2.3 Database & Persistence Changes
*Scope:* EF Core entity configurations, migrations, repositories, connection strings, schema definitions.
- [ ] Exactly one physical SQL Server database (`Clovent_BusinessOperatingSystem`) targeted; zero secondary physical databases.
- [ ] Uses designated bounded-context schema (`[Authentication]`, `[Identity]`, `[MasterData]`, `[Catalog]`, `[Inventory]`, `[Restaurant]`).
- [ ] Uses schema-scoped migration history table `[<Schema>].[__EFMigrationsHistory]`.
- [ ] Explicit decimal precision declared on all numerical columns (e.g. `HasPrecision(18, 2)`).
- [ ] Destructive schema changes (column drops, table drops) are explicitly reviewed and accompanied by a data migration plan.
- [ ] Validated against real SQL Server before release qualification.

### 2.4 User Interface & WinForms Changes
*Scope:* `Clovent.Desktop`, Forms, UserControls, Dialogs, DevExpress controls.
- [ ] Dual constructors provided: parameterized for runtime DI, parameterless decorated with `[EditorBrowsable(Never)]` for Designer.
- [ ] Design-time guard present in `Load` event handler: `if (DesignModeHelper.IsInDesignMode) return;`.
- [ ] `InitializeComponent()` contains layout and control instantiation only; zero database calls, DI resolutions, or async tasks.
- [ ] Runtime scaling handles 200%–250% High-DPI (`DeviceDpi` ~240) using `DesktopDpi.Scale(...)`.
- [ ] Filter bars use structured `TableLayoutPanel` with single rows; labels vertically centered (`Anchor = Left`).
- [ ] Truthful execution status claimed: `LIVE UI EXECUTED` only if tested interactively; otherwise `LIVE UI NOT EXECUTED`.

### 2.5 External Integration Changes
*Scope:* Outbox handlers, QuickBooks synchronization, hardware drivers, printers.
- [ ] Integration calls implement timeout, retry, and circuit breaker protection.
- [ ] All external mutations are driven idempotently via the Transactional Outbox.
- [ ] Status explicitly documented: `PRODUCTION EXTERNAL INTEGRATION NOT VALIDATED` if tested against simulated gateways.
- [ ] External service outages do not crash or block local POS transaction capture.

### 2.6 Packaging & Release Changes
*Scope:* Release publishing, Inno Setup scripts, ReleaseGuard verification.
- [ ] Built in `Release` configuration and published as self-contained `win-x64` executable.
- [ ] All excluded files stripped: `*.cs`, `*.csproj`, `*.pdb`, `appsettings.Development.json`, `*development*.lic`, private keys.
- [ ] Release package passes `ScanReleasePackage.ps1` with exit code 0 (`PASS`).
- [ ] Binaries and installer signed with Authenticode and timestamped prior to qualification.
- [ ] Exact accepted installer verified via fresh installation in clean Windows Sandbox.
- [ ] Accompanied by generated SHA-256 integrity checksum manifest.
