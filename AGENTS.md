# Clovent Business Operating System (CBOS) — Agent Instructions

This document is the **single authoritative source of truth** for all AI coding agents working on the Clovent Business Operating System repository. All coding agents (Antigravity, Gemini, subagents, and peers) must read and adhere to these instructions before planning or modifying code.

Specialized domain rule files in `.agents/rules/` provide deep implementation details and must be consulted for targeted work:
- WinForms & High-DPI UI: [winforms-ui.md](file:///.agents/rules/winforms-ui.md)
- Database & Persistence: [database.md](file:///.agents/rules/database.md)
- Security & Licensing: [security.md](file:///.agents/rules/security.md)
- Testing & Quality: [testing.md](file:///.agents/rules/testing.md)
- Packaging & Release: [release.md](file:///.agents/rules/release.md)

---

## 1. Project Identity & Technology Stack

- **Product Name:** Clovent Business Operating System (CBOS)
- **Solution File:** `Clovent.BusinessOperatingSystem.slnx`
- **Primary Language:** C# (C# 13 / .NET 10)
- **Target Frameworks:**
  - `net10.0-windows` for Desktop application & Desktop tests (`src/Clovent.Desktop`, `src/Clovent.Desktop.Tests`)
  - `net10.0` for Domain, Application, Infrastructure, Platform, and CLI tools
- **Desktop UI Framework:** Windows Forms with **DevExpress 26.1.4-pre-26179** (`DevExpress.Win`, `DevExpress.Reporting.Core`, `DevExpress.Images`)
- **Database Engine:** Microsoft SQL Server
- **ORM / Persistence:** Entity Framework Core **10.0.10** (`Microsoft.EntityFrameworkCore.SqlServer`, `Microsoft.EntityFrameworkCore.Design`)
- **Mediator / CQRS:** MediatR **12.4.1**
- **Hosting & Dependency Injection:** `Microsoft.Extensions.Hosting` 10.0.10, `Microsoft.Extensions.DependencyInjection`
- **Testing Stack:** xUnit **2.9.3**, `Microsoft.NET.Test.Sdk` 17.14.1, `coverlet.collector` 6.0.4, SQLite in-memory (`Microsoft.EntityFrameworkCore.Sqlite` 10.0.10) for isolated unit/integration tests

---

## 2. Solution Architecture & Bounded Contexts

CBOS follows strict Domain-Driven Design (DDD) with Clean Architecture across bounded contexts. Each context contains:
- `Clovent.<Context>`: Domain layer (aggregates, entities, value objects, domain events, repository interfaces). Zero dependencies on outer layers.
- `Clovent.<Context>.Application`: Application layer (MediatR commands, queries, handlers, DTOs, domain service interfaces). References Domain.
- `Clovent.<Context>.Infrastructure`: Infrastructure & persistence (EF Core DbContext, entity configurations, repository implementations, migration snapshots). References Domain and Application.
- `Clovent.<Context>.Tests`, `*.Application.Tests`, `*.Infrastructure.Tests`: Unit and integration test suites.

### Discovered Bounded Contexts:
1. **Authentication:** User login attempts, session tokens, authentication audit events.
2. **Identity:** Users, roles, permissions (214 granular permissions), organization hierarchy (Organizations, Companies, Branches).
3. **MasterData:** Warehouses, terminals, currencies, units of measure, number sequences.
4. **Catalog:** Product categories, product groups, brands, products, product variants, barcodes, pricing tiers.
5. **Inventory:** Warehouse stocks, inventory transactions (Receipt, Issue, Transfer, Adjustment, Reserve, Release).
6. **Restaurant:** Dining areas, tables, orders, order lines, kitchen tickets, bill settlements, discounts, service charges, customer accounts, customer ledger, shifts, cash drawer movements, smart recommendation engine.
7. **Desktop (`src/Clovent.Desktop`):** WinForms presentation shell, ribbon navigation, views, dialogs, commissioning wizard, licensing UI, local formatters, background startup tasks. References all Application and Infrastructure layers.
8. **Platform (`src/Clovent.Platform`):** Shared cross-cutting abstractions, environment settings, base interfaces.

### Code Placement Rule (Strict):
Never dump new classes into arbitrary or root folders.
- Place Commands, Queries, and DTOs inside `src/Clovent.<Context>.Application/<Feature>/`.
- Place Entities, Enums, and Value Objects inside `src/Clovent.<Context>/<Feature>/`.
- Place EF Configurations and Repositories inside `src/Clovent.<Context>.Infrastructure/`.
- Place Forms, UserControls, and Dialogs inside `src/Clovent.Desktop/<Context>/<Feature>/` or `src/Clovent.Desktop/Forms/<Context>/`.
- Place Tests inside the corresponding test project under matching feature namespaces.

---

## 3. Database Architecture & Persistence

- **Physical Database:** Exactly **one** physical SQL Server database: `Clovent_BusinessOperatingSystem`.
  - Canonical connection string key: `ConnectionStrings:Default` in `appsettings.json`.
  - Do NOT recreate separate physical databases. Do NOT merge bounded contexts into one giant DbContext.
- **Schema Isolation:** Bounded contexts own dedicated SQL schemas:
  - `[Authentication]`, `[Identity]`, `[MasterData]`, `[Catalog]`, `[Inventory]`, `[Restaurant]`
- **Isolated Migrations History:** Each context manages migrations in its own schema-scoped table:
  - `[<Schema>].[__EFMigrationsHistory]` (e.g. `[Restaurant].[__EFMigrationsHistory]`)
- **DesignTime DbContext Factories:** All `*DbContextFactory` classes in Infrastructure projects target `Clovent_BusinessOperatingSystem` with their respective schema-scoped migration table.
- **Runtime Security & Least Privilege:**
  - Runtime application login: `cbos_app`. Must receive **only** `db_datareader`, `db_datawriter`, and `GRANT EXECUTE`.
  - Forbidden runtime roles: `sa`, `sysadmin`, `db_owner`, `db_ddladmin`. Schema alterations are restricted to DBA/maintenance accounts.
  - Runtime credentials are encrypted using Windows DPAPI (`DataProtectionScope.LocalMachine` / `CurrentUser`) in `database.config.json`. Never store plaintext SQL passwords.

---

## 4. WinForms & DevExpress Engineering Standards

- **Visual Studio Designer Safety:**
  - Forms and UserControls must provide a parameterless constructor decorated with `[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]` for Visual Studio Designer instantiation.
  - Keep standard `.cs` / `.Designer.cs` / `.resx` structure.
  - `InitializeComponent()` must contain layout and control instantiation **only**.
  - **Never put in `InitializeComponent()`:** database queries, MediatR calls, DI service resolution, async logic, dynamic data loading, LINQ queries, or authorization checks.
  - In `Load` event handlers, guard design-time execution using:
    ```csharp
    if (Clovent.Desktop.Forms.Base.DesignModeHelper.IsInDesignMode) return;
    ```
- **Designer CodeDom Constraints:**
  - In `*.Designer.cs` files intended to be visually edited, do NOT use modern C# constructs that break VS CodeDom parsers: no `var`, no target-typed `new()`, no object/collection initializers, no lambdas, no generic invocations, no helper method calls.
  - Views composed purely in code with shared layout helpers must be explicitly marked:
    ```csharp
    [System.ComponentModel.DesignerCategory("Code")]
    ```
- **Designer Execution Claim Rule:** Never claim `VISUAL STUDIO DESIGNER UI EXECUTED` unless Visual Studio Designer was interactively opened. Structural compile or reflection tests do not constitute Designer execution.

---

## 5. High-DPI UI Standard (1920×1080 @ ~250% Scaling / 240 DPI)

- **Target Workstation Baseline:** 1920×1080 resolution at 200%–250% Windows scaling (`DeviceDpi` ~240).
- **DPI Modes:**
  - Runtime: `<ApplicationHighDpiMode>PerMonitorV2</ApplicationHighDpiMode>` in `Clovent.Desktop.csproj`.
  - Designer: `<ForceDesignerDPIUnaware>true</ForceDesignerDPIUnaware>` in `Clovent.Desktop.csproj` (deliberate designer-host setting to prevent repeated open/save coordinate multiplication).
  - WinForms `AutoScaleMode` is deliberately omitted/disabled in forms to prevent coordinate recomputation and permanent corruption. DevExpress controls handle per-monitor DPI rendering natively.
- **Runtime DPI Scaling:**
  - Use `Clovent.Desktop.Forms.Base.DesktopDpi.Scale(int logicalPixels, Control reference)` or `DesktopDpi.Scale(int logicalPixels, int dpi)` for fixed pixel constants that must track skin fonts.
  - Never apply arbitrary `Location.Y` pixel hacks.
- **Filter Bars & Toolbars:**
  - Use `TableLayoutPanel` with single rows (`SizeType.AutoSize` or explicit logical sizes) instead of wrapping `FlowLayoutPanel`.
  - Labels must have `Anchor = AnchorStyles.Left` to center vertically against companion editors.
  - Standard sequence: `Location` -> `Period` -> `From` -> `To` -> Action buttons.
- **Shared Sizing Constants (`DesktopStyle.cs`):**
  - Standard toolbar control height: `DesktopStyle.ToolbarControlHeight` (30px baseline).
  - Standard gap: `DesktopStyle.ControlGap` (8px).
  - Standard button widths: `ButtonWidthSmall` (80px), `ButtonWidthMedium` (100px), `ButtonWidthLarge` (130px).
  - Search box width: `DesktopStyle.SearchBoxWidth` (220px).
- **EntityPicker (`src/Clovent.Desktop/MasterData/EntityPicker.cs`):**
  - Standard dropdown for Warehouse, Location, and Branch filters across back-office views.
  - Set logical width to 210–260px (`DesktopDpi.Scale(...)` at runtime) to ensure full business names (e.g. `"Kitchen Backup Warehouse"`) display without truncation or ellipsis.
- **GridView Integrity:**
  - Wrap interactive column adjustments in `_gridView.BeginUpdate()` / `_gridView.EndUpdate()`.
  - Compute column `MinWidth` using `TextRenderer.MeasureText(...)` plus glyph padding to prevent header clipping.
  - Eliminate unnecessary horizontal scrollbars: set `ColumnAutoWidth = true` and disable form-level `AutoScroll` where grids fill the workspace.

---

## 6. Back Office UI Standards

- **EntityPicker Usage:** Always use `EntityPicker` for Warehouse, Location, and Branch selection.
- **Toolbar Buttons:** Uniform heights, consistent typography (`Segoe UI 9pt Bold`), uniform padding `(8, 4, 8, 4)` and margins `(2, 0, 2, 0)`.
- **Reports:** Strictly read-only views for analytical, historical, and financial inspection.
- **Operational Actions:** Inventory adjustments, goods receipts, and menu configurations belong in operational modules (Inventory, Manager, Restaurant POS), not hidden inside report views.
- **Navigation:** Maintain one canonical primary ribbon/navigation location per feature.

---

## 7. Financial & Reporting Semantics

- **Customer Bill Reconciliation:**
  $$\text{Item Sales} - \text{Discount} + \text{Fees} + \text{Tax} = \text{Bill Total}$$
  $$\text{Paid} + \text{On Account} = \text{Bill Total}$$
- **Customer A/R & Advances:**
  - When payments exceed on-account balances, the excess is recorded as a customer advance credit balance.
  - Subsequent orders consume advance balances first before requiring additional tender.
- **Shift Drawer Reconciliation:**
  $$\text{Starting Float} + \text{Cash In} + \text{Cash Sales} + \text{Cash Collections} - \text{Cash Out} = \text{Expected Cash}$$
  $$\text{Counted Cash} - \text{Expected Cash} = \text{Variance}$$
  - For active/open shifts, cashier drawer counts have not occurred: `CountedCash` and `Variance` must be `null` and displayed as `"N/A"`. Never display fake negative variances for open shifts.
- **Cost & Margin Truthfulness:**
  - **Prepared Items:** If recipe/BOM cost is unavailable, cost must be `null` and displayed as `"N/A"`. Gross Profit must display `"Known GP: N/A"` or `"Known GP: {sum}"`. Never fake profitability by silently replacing unknown costs with `0.00`.
  - **Purchased/Resale Items:** Use actual known purchase cost.
  - **Service Items:** Legitimate zero direct cost (100% GP).
- **Summary Footers:** Never sum unit prices, average order values, or percentages across rows.

---

## 8. Inventory Rules

- **Workflow Integrity:** Warehouse stock changes **only** via formal domain workflows: `Receive`, `Issue`, `Adjustment`, `Transfer`, `Reserve`, `Release`.
- **No Direct Mutation:** Never directly update stock quantities via raw SQL or ad-hoc DB updates to make reports match.
- **Stock On Hand Display:** Must clearly resolve both SKU (`NAAN-STD`) and Product identity (`${ProductName} - ${VariantName}`).
- **Precision Distinction:** Database storage precision (`decimal(18,4)` or `decimal(18,6)`) and UI display precision are strictly separated. Format all display quantities using `QuantityDisplay.Format()`.

---

## 9. Date, Time & Display Formatting

Always use central formatters located in `src/Clovent.Desktop/Forms/Base/`:
- **Date Formatting:** `BusinessDateFormatter.Format(date)` (respects company format, e.g. `dd-MMM-yyyy`, and business timezone).
- **Time Formatting:** `BusinessTimeFormatter.Format(time)` (respects 12-hour vs 24-hour setting).
- **Date & Time Combined:** `BusinessDateTimeFormatter.Format(dateTimeOffset)`.
- **Quantities:** `QuantityDisplay.Format(quantity)` (respects company precision, default 2 decimals, e.g. `145.00`; never prefixes currency symbols).
- **Currency:** `CurrencyDisplay.Format(amount)` (respects symbol, e.g. `Rs. 850.00`, and precision).
- Never scatter hardcoded format strings (`"yyyy-MM-dd"`, `"C"`, `0.00`) across screens.

---

## 10. Security & Licensing Rules

- **Zero Shipped Credentials:** Never commit or ship `Admin123!`, development passwords, test credentials, or hidden backdoors. Default admin creation endpoint must disable itself once an administrator exists.
- **Protected Storage & ACLs:** SQL configurations and licenses reside in `%ProgramData%\Clovent\BusinessOperatingSystem\` with strict ACLs (Administrators: Full Control; Users: Read-Only).
- **Sensitive Operations:** Pre-login configuration changes require Windows UAC elevation; post-login changes require the `Administrator` application role (`AdministrativePrivilegeChecker`).
- **Cryptographic Licensing:**
  - Algorithm: Asymmetric RSA-2048 with SHA-256 (`clovent-2026-v2`).
  - Public verification key embedded in client assembly; vendor private key kept strictly outside repository (`%USERPROFILE%\.clovent\keys\`).
  - Generic releases ship **without** an active license (`clovent.lic` excluded).
  - Pre-copy validation: candidate licenses must be verified before replacing active licenses.
  - Non-destructive policy: Expired licenses permit read-only data access (reports, history, backups) while blocking new financial transactions. Customer data is never deleted, encrypted, or held hostage.

---

## 11. Build & Release Engineering

### Canonical Build Commands:
- Debug Build: `dotnet build Clovent.BusinessOperatingSystem.slnx -c Debug`
- Release Build: `dotnet build Clovent.BusinessOperatingSystem.slnx -c Release`
- Publish Client:
  ```powershell
  dotnet publish src\Clovent.Desktop\Clovent.Desktop.csproj -c Release -r win-x64 --self-contained true -o artifacts\release\Clovent.BusinessOperatingSystem-win-x64
  ```
- Automated Security Scan:
  ```powershell
  powershell -ExecutionPolicy Bypass -File tools\ReleaseGuard\ScanReleasePackage.ps1 -ReleaseDir artifacts\release\Clovent.BusinessOperatingSystem-win-x64
  ```

### Release Exclusions:
Production releases must strictly exclude: `*.cs`, `*.csproj`, `*.sln`, `*.slnx`, `*.pdb`, `appsettings.Development.json`, `*development*.lic`, `clovent.lic`, private keys, and `*.bak` files. Client machines require no Visual Studio or .NET SDK.

---

## 12. Testing & Verification Standards

### Standard Implementation Workflow:
1. Investigate existing behavior and inspect code.
2. Identify root cause.
3. Implement minimal, targeted fix adhering to architectural rules.
4. Build solution in Debug configuration.
5. Execute targeted tests for affected module.
6. Execute regression tests for related bounded contexts.
7. Update relevant authoritative documentation.
8. Deliver comprehensive Final Report.

### Test Execution Commands:
- Targeted Tests: `dotnet test <test-project> --filter "FullyQualifiedName~<Feature>"`
- Example: `dotnet test src\Clovent.Desktop.Tests\Clovent.Desktop.Tests.csproj --filter "FullyQualifiedName~BusinessFormatters"`

### Verification Claims (Strict):
- Use `LIVE UI EXECUTED` **only** if the actual Windows application UI was interactively launched and exercised on a physical/virtual display.
- Otherwise, state `LIVE UI NOT EXECUTED`. Automated tests, headless runners, DrawToBitmap, and unit tests are NOT live UI execution.
- Use `VISUAL STUDIO DESIGNER UI EXECUTED` **only** if the form was opened inside Visual Studio Designer.

---

## 13. Multi-Agent Coordination

- Large tasks with independent workstreams may use parallel subagents (UI, Domain/Application, Database, Testing, Security).
- One lead agent must reconcile overlapping changes before the final build.
- Subagents must not introduce conflicting architectural patterns or duplicate classes.

---

## 14. Documentation Standards

- Always update existing authoritative documents in `docs/` or `docs/architecture/` when behavior or architecture changes.
- Do not create transient, single-fix markdown files.
- Documentation must reflect actual code behavior.

---

## 15. Final Report Standard

Every non-trivial task response must conclude with a structured technical report containing:
1. **Root Cause Analysis**
2. **Key Architectural & Business Decisions**
3. **Implementation Details**
4. **Files Modified / Created**
5. **Testing Verification:**
   - Targeted Tests run & results
   - Regression Tests run & results
   - Live UI Status: `LIVE UI EXECUTED` or `LIVE UI NOT EXECUTED`
   - Designer Status: `VISUAL STUDIO DESIGNER UI EXECUTED` or `NOT EXECUTED`
6. **Build Verification:** Debug and Release build status
7. **Risks, Edge Cases & Remaining Concerns**
8. **Final Status Statement**
