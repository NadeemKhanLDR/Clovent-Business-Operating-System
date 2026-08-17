# Restaurant POS Change History

This document records significant changes made to the Restaurant POS and related login/session modules in the Clovent Business Operating System (CBOS), documented chronologically.

---

## 2026-08-14 — WinForms Designer Compatibility & Layout Hardening (D28 / D29 / D30)

### Context

Following the independent live QA run of the remediation pass, we addressed three remaining WinForms layout and designer defects (D28, D29, and D30) to achieve complete Visual Studio Designer compatibility and layout alignment.

### Fixed

- **D28 (HIGH) — WinForms Designer fails to load multiple views and edit forms.**
  - Marked the 24 code-built management views using generic `MasterDataListView<T>` with `[System.ComponentModel.DesignerCategory("Code")]` to declare them code-only.
  - Added `[System.ComponentModel.DesignerCategory("Code")]` to `MasterDataListView<T>` itself and provided a safe parameterless constructor.
  - Refactored the double-click lambda handler inside `MenuItemsForm.Designer.cs` into a named event handler `GridView_DoubleClick` in `MenuItemsForm.cs`.
  - Refactored 35+ edit forms inheriting from `MasterDataEditFormBase` to move `AddField(...)` calls out of `InitializeComponent()` into a separate `InitializeFields()` method in the code-behind, resolving CodeDom parsing errors.
- **D29 (MEDIUM) — TextPromptForm button and input field overlap.**
  - Corrected docking order by changing `_contentPanel.BringToFront();` to `_contentPanel.SendToBack();` in `MasterDataEditFormBase_Load` to ensure `Dock.Fill` content panel occupies remaining space without overlapping bottom buttons.
  - Increased `TextPromptForm` client size to `420, 170` and maximum size to `460, 220`.
  - Specified `fixedHeight: 90` for the reason input field inside `TextPromptForm` to keep it bounded.
- **D30 (LOW) — Manager Authorization Dialog title truncation and extra spacing.**
  - Added parameterless constructors to `TextPromptForm` and `ManagerAuthorizationForm` to satisfy visual designer loading.
  - Updated `MasterDataEditFormBase_Load` to calculate vertical docked chrome heights using `c.GetPreferredSize(...)` instead of stale design-time heights, preventing vertical layout truncation.
  - Enforced a minimum form width based on title bar text measured via `TextRenderer` to prevent caption truncation.

### Testing

- **Compilation Build:** PASS — 0 errors / 0 warnings (`-c Release --no-incremental`).
- **Automated Tests:** PASS — **1128 total / 1128 passed / 0 failed / 0 skipped** (9 new layout and validation tests added in `Clovent.Desktop.Tests`).
  - `DesignerSafetyTests` (+5) — Verified parameterless reflection instantiation.
  - `TextPromptFormTests` (+2) — Verified button positions and no overlap under multiple label lengths.
  - `ManagerAuthorizationFormTests` (+2) — Verified auto-sizing and title bounds.
- **Runtime UI:** PENDING CLAUDE QA / USER SIGN-OFF.
- **Visual Studio Designer:** PENDING CLAUDE QA / USER SIGN-OFF.

### Database

- **No schema change.** No migrations added.

# NOT READY FOR FINAL UI SIGN-OFF

---

## 2026-08-13 (even later) — Dialog Sizing & Text Clipping Hardening

### Context

Following the independent live QA run of the remediation pass, we addressed two newly identified layout defects (D26 and D27) to ensure managers can fully see exception authorization details and prompts are compact and professional.

### Fixed

- **D26 (MEDIUM) — Manager Authorization Dialog detail text clipped.**
  Configured `_detailLabel` to use `LabelAutoSizeMode.Vertical` and removed its hardcoded height constraint in `ManagerAuthorizationForm.Designer.cs`. Added `Dock = DockStyle.Fill` for the username and password fields, and initialized a compact starting `ClientSize` of `420, 160` in the constructor. This ensures the form naturally scales up vertically during `Load` to accommodate multi-line messages without clipping or overlapping controls.
- **D27 (LOW) — Void Order Reason Dialog had 80% empty space.**
  Refactored `TextPromptForm` to be a compact, non-resizable `FixedDialog` (starting client size `420, 150`, maximum size `460, 190`), docked the input memo edit to fill the layout width, and renamed the OK button to "Confirm". Validation logic remains mandatory, rejecting blank or whitespace reasons.

### Testing

- **Compilation Build:** PASS — 0 errors / 0 warnings (`-c Release --no-incremental`).
- **Automated Tests:** PASS — **1119 total / 1119 passed / 0 failed / 0 skipped** (6 new layout and validation tests added in `Clovent.Desktop.Tests`).
  - `ManagerAuthorizationFormTests` (+3) — Verified short/long messages, dynamic vertical growth, and bounds safety for username/password and button controls.
  - `TextPromptFormTests` (+3) — Verified that empty input is correctly rejected, valid reasons are accepted, and button text/properties are correctly initialized for compact FixedDialog layouts.
- **Runtime UI:** PENDING CLAUDE QA.
- **Visual Studio Designer:** PENDING CLAUDE QA (both changed files verified as containing no designer-hostile constructs like lambdas, loops, or local functions inside `InitializeComponent`).

### Database

- **No schema change.** No migrations added. QA balances and John Smith profile remain untouched.

# NOT READY FOR FINAL UI SIGN-OFF

---

## 2026-08-13 (later) — QA Defect Remediation

### Context

An independent QA pass (`D:\FCCReports\CBOS_QA_Report_2026-08-13_131207.md`) re-tested every prior
finding from scratch against a real interactive desktop and a real Visual Studio 18 Enterprise
Designer. It reproduced every previously reported CRITICAL and HIGH defect, withdrew one
(`D21`) as not reproducible, and found four new ones — including `D23`, a financial-data
corruption path. That report is the authoritative baseline for this pass; none of its findings were
reinterpreted or downgraded. Full detail of the work is in
`docs/architecture/RestaurantPOSArchitecture.md` Section 21.

### Fixed

- **D1 (CRITICAL) — Customer Ledger grid, Print Preview and exports contained no data.**
  `CustomerLedgerDialog.Designer.cs` called `BeginInit()` on `_ledgerGrid`/`_ledgerGridView` without
  the matching `EndInit()`, so the DevExpress grid never finished initialising and created no view.
  One root cause for the blank grid and for both headers-only exports.
- **D2 (CRITICAL) — `RestaurantPosForm` could not be opened in the Visual Studio Designer.**
  Removed the three construct classes ADR-007 forbids from `InitializeComponent()`: `nameof(...)`,
  object initializers, and 29 helper-method calls. Visual behaviour preserved.
- **D23 (CRITICAL, financial integrity) — activate/deactivate silently overwrote
  `OutstandingBalance` with a stale cached value.** Fixed in the persistence layer: command-side
  reads re-read the row, list reads are `AsNoTracking`, `UpdateAsync` no longer forces a full-row
  update, and a new `ICustomerRepository.UpdateStatusAsync` writes the status column alone.
- **D7 (CRITICAL, security) — the credit-limit override required no manager authentication.**
  Added `IManagerAuthorizationService`, reusing Identity/Authentication rather than adding a second
  credential system. Also applied to **D25** (Void Order), documented as a deliberate scope
  decision.
- **D22 (HIGH) — `DbContext` concurrency exception reachable from the POS payment path.**
  `CustomersView` now routes its scope's mediator and feature policy through a shared
  `ScreenOperationGate`, and its two fire-and-forget `_ = SomeAsync()` chains are awaited through
  `GuardedAction` so a fault can no longer resurface later as an unobserved task exception.
- **D24 (MEDIUM) — the error dialog opened behind the POS and could not be dismissed.**
  `ErrorDialogService` now marshals to the UI thread and owns the dialog to the active window;
  previously it called `ShowDialog()` with no owner from the finalizer thread.
- **D4 (HIGH) — `CustomersView` opened empty.** The `Load` handler existed but
  `InitializeComponent()` never subscribed it. Wired, with a duplicate-load guard.
- **D5 (HIGH) — Refresh did not re-query.** Refresh re-sends the query and rebinds; the stale-read
  half of the problem is fixed in the repository (see D23).
- **D3 / D6 / D14 (HIGH / HIGH / LOW) — POS layout.** Totals split across two rows so Service Charge
  and GRAND TOTAL are no longer truncated; `MinimumSize` lowered to 1200×700 so the outer window can
  actually fit a 1280-wide display; the Exact button keeps its smaller font and gains a minimum
  width.
- **D9 (MEDIUM) — customer payment methods were hardcoded and mismatched.** Now loaded from the
  configured payment methods, the same source the POS tender strip uses.
- **D11 (MEDIUM) — "Outstanding outstanding" in the audit log.** Corrected, and the message
  composition extracted so the wording is covered by a test.
- **D20 (LOW) — Amount Tendered showed "72.5".** Formats to two decimal places.

### Testing

- **Compilation Build:** PASS — 0 errors / 0 warnings (`-c Release --no-incremental`).
- **Automated Tests:** PASS — **1113 total / 1113 passed / 0 failed / 0 skipped** across 21
  assemblies (up from 1082; **31 tests added**).
  - `CustomerRepositoryTests` (Restaurant.Infrastructure.Tests, 6) — D23 and D5 against a real
    relational engine, including the exact QA repro: DB 111.11, screen cache 999.99, deactivate,
    DB still 111.11 with only `IsActive` changed.
  - `CustomerHandlerTests` (+2) — D23 at the handler level, asserting the status path never
    round-trips the whole aggregate.
  - `ManagerAuthorizationServiceTests` (Desktop.Tests, 10) — D7/D25, including that valid
    credentials without the permission are refused and that failures count towards lockout.
  - `CustomersViewLoadAndRefreshTests` (Desktop.Tests, 7) — D4 through the real `Load` event, D5
    re-query and filter retention, and D22 as a concurrency assertion (max observed concurrency 1).
  - `CustomerPaymentTests` (Desktop.Tests, 6) — D9 payment-method loading and D11 wording.
- **Runtime UI:** PENDING CLAUDE QA.
- **Visual Studio Designer:** PENDING CLAUDE QA.
- **Native 100% DPI:** NOT VERIFIED.

Automated tests are not UI evidence and are not offered as any. No test in this solution drives a
WinForms surface, a Designer load, or a rendered layout.

### Database

**No schema change.** No migration was added, altered, or removed. No QA data was created or
deleted by this pass.

### Superseded

This entry supersedes the "PASS — LIVE RUNTIME" claims for the Customer Ledger and its exports, the
"no clipping" and "renders in full" claims for the POS totals and Exact button, the "Visual Studio
Designer compatibility: PASS" claim, and the Designer known-limitation note naming
`CustomersView`/`CustomerLedgerDialog`/`CustomerPaymentForm` — all recorded in earlier entries below
and all disproved by the QA report. See `RestaurantPOSArchitecture.md` Section 21.2 for the
point-by-point corrections.

# NOT READY FOR FINAL UI SIGN-OFF

---


## 2026-08-13 — Designer CodeDom Audit & POS Payment Interaction

### Context
Manual testing in Visual Studio Designer produced failures the previous pass had not predicted:
`CustomerLedgerDialog.Designer.cs` reported *"cannot process the code at line 335"*,
`AppearanceRuleEditForm.Designer.cs` reported the same at line 81, and `CustomerPaymentForm`
opened but rendered only one control despite the Properties window listing many.

This invalidates the 2026-08-12 claim below that the Customer Designer files were "fully parsed
and compatible with the Visual Studio WinForms Designer". That claim was based on source review
only and was not correct.

### Root Cause
The failures are a *parser* problem, not the instantiation problem [ADR-003](../architecture/adr/ADR-003-Designer-Safe-WinForms.md)
addressed. The Designer parses `InitializeComponent()` with a CodeDom parser that cannot
represent `var`, target-typed `new()`, object initializers, generic method invocations, lambdas,
loops, or helper-method calls. On hitting one it aborts parsing and reports the line — leaving
every control after that point absent from the surface. That is the "only the Cancel button
renders" symptom, which is a parse truncation rather than a layout fault.

The reported line numbers confirm it: `AppearanceRuleEditForm` failed at line 81, and line 82
was `_scopeTypeCombo.Properties.Items.AddRange(Enum.GetNames<AppearanceScopeType>());`.

### Audited
- All 86 `*.Designer.cs` files under `src/Clovent.Desktop` scanned for the constructs above.
- Finding: a substantial subset are hand-written layout files that merely carry the
  `.Designer.cs` suffix, built on shared helpers (`CommandPanelLayout.Build(...)`,
  `BuildStatCard(...)`) and lambda handlers. These were never Designer-generated.

### Fixed — Designer-editable forms
- `CustomerLedgerDialog.Designer.cs` — moved three `readonly ... = new() { ... }` field
  initializers into `InitializeComponent()` with explicit construction, added the missing
  `GridControl`/`GridView` `BeginInit`/`EndInit` pair, and replaced six `var` column
  declarations with explicit `GridColumn` types.
- `CustomersView.Designer.cs` — replaced eight `var` column declarations with explicit types.
- `AppearanceRuleEditForm.Designer.cs` — moved the generic `Enum.GetNames<T>()` call out of
  `InitializeComponent()` into the existing `InitializeFields()` code-behind initializer.
- `CustomerPaymentForm.Designer.cs`, `CustomerEditForm.Designer.cs` — audited, no hostile
  constructs found; left unchanged.

### Changed — Code-built views
Nine views whose layout is genuinely composed in code are now marked
`[System.ComponentModel.DesignerCategory("Code")]`, so Visual Studio opens them in the code
editor rather than attempting a Designer load: `EndOfDayReportView`, `PaymentHistoryDialog`,
`MainForm`, `RestaurantSetupView`, `AppearanceSettingsView`, `BusinessSettingsManagementView`,
`EntityPicker`, `OrganizationHierarchySelector`, `ReceiptPreviewForm`. No runtime or layout
change. See [ADR-007](../architecture/adr/ADR-007-Designer-CodeDom-Constraints.md).

### Changed — POS payment method selection
Selected and unselected method buttons previously both rendered with a full saturated fill,
differing only in border colour, which made the selection hard to read. Selection is now carried
by fill inversion (solid colour + white text when selected, white fill + coloured text and
border when not), a leading `✓` glyph, and bold weight — three independent signals, so it does
not depend on hue alone. The border is now permanently `Simple` in both states, so a selection
reads as persistent rather than as a transient pressed flash. Unavailable methods render in a
flat grey. Centralised in `UpdateMethodButtonSelection`, driven by `PosPaymentRules.ResolveButtonState`.

### Added
- `PosPaymentRules` (`src/Clovent.Desktop/Restaurant/Orders/PosPaymentRules.cs`) — payment-method
  button state and auto-completion decisions, shared by the form and its tests rather than
  re-implemented in each.
- `PosPaymentRulesTests` — 18 cases covering selected/unselected/unavailable button state,
  single-selection invariant, auto-completion at zero balance from `Open` and `Held`, no
  completion while a balance remains, no completion when the payment was not recorded, no
  completion from a non-open status, no double completion, and the half-cent settle tolerance.

### Verified (no change required)
Automatic completion on full payment was **already implemented** at
`RestaurantPosForm.RecordPaymentAsync`. It records the payment, awaits `RefreshOrderAsync` to
re-read the server balance, then calls the same `CompleteAsync()` the manual Complete button
uses — so there is a single completion workflow, not two. Duplicate completion is guarded by the
order-status check at the call site and a server-side balance re-check inside `CompleteAsync()`.
The inline condition was replaced with a call to `PosPaymentRules.ShouldAutoComplete` so the
behaviour is now covered by tests; the logic is unchanged.

### Verification Status
- Source review: **PASS**
- Build (Release): **PASS** — 0 errors, 0 warnings
- Automated tests: **PASS** — 1067 passed, 0 failed, 0 skipped
- Runtime UI: **NOT VERIFIED**
- Visual Studio Designer: **NOT VERIFIED** — no Designer instance was opened during this work
- Native DPI: **NOT VERIFIED**

### Not Done
POS layout compaction (compact payment area, compact cart/item table, compact totals row,
1366x768 / 1280x768 verification) was **not** attempted in this pass and remains outstanding.

---

## 2026-08-12 — Customer Management Designer Refactoring & Gating Hardening

### Refactored
- **Visual Studio Designer compatibility refactoring**: Completely refactored `CustomersView.Designer.cs`, `CustomerLedgerDialog.Designer.cs`, and `CustomerPaymentForm.Designer.cs` to remove all local helper-function patterns inside `InitializeComponent()`. Inlined properties, columns, buttons, and layout setups, making them fully parsed and compatible with the Visual Studio WinForms Designer.
- **DPI Scaling preservation**: Preserved logical-to-device unit scaling (`LogicalToDeviceUnits(...)`) for layout bounds, ensuring correct high-DPI scaling across resolutions without designer incompatibility.

### Hardened
- **Defensive Permission Gating**: Added explicit runtime checks for permissions (`customers.payment`, `customers.viewledger`, and `customers.activate`/`customers.deactivate`) inside the click event handlers of `CustomersView`. Even if button enabled states are somehow bypassed in the UI, unauthorized operations will be rejected immediately, preventing any unauthorized balance/ledger updates.

### Added
- **Automated Authorization Tests**: Created `CustomersViewAuthorizationTests.cs` to test the UI button states and verify permission validation boundaries under simulated Cashier session roles.
- **Automated CSV Export Tests**: Created `CustomersViewCsvExportTests.cs` to verify the actual CSV generation logic, columns, formatting, comma/quote escaping, and null/empty value mapping of the Customers grid.

---

## 2026-08-12 — Customer Management Final UI Sign-Off Pass

### Context
A genuine interactive desktop session and a licensed Visual Studio Enterprise install were both available for this pass, unlike prior passes, which let real runtime and Designer verification be attempted for the first time rather than assumed unavailable. Four genuine defects were found this way and fixed; none were visible from source review, unit tests, or database verification alone.

### Fixed
- **Customer Management was unreachable from the running application.** `CustomersView` was fully implemented, registered with `INavigationService`, and had its `menu.customers`/`feature.customers.*` permissions seeded — but the Back Office ribbon's data-driven navigation table (`MainForm.Designer.cs`'s `NavigationItems`) had no row for it, so no ribbon button ever existed to open it. Added the missing row.
- **`CustomerLedgerDialog` threw an unhandled exception on open.** `ApplyFilters()` built a `DateTimeOffset` from the Date From/To picker values via `new DateTimeOffset(date, TimeSpan.Zero)`; DevExpress's `DateEdit` returns `DateTime.Kind = Local` (and defaults to today's date), and .NET's `DateTimeOffset` constructor rejects a `Local`-kind `DateTime` paired with an offset that doesn't match the machine's real UTC offset. Fixed by normalizing to `DateTimeKind.Unspecified` before constructing the offset.
- **`CustomerLedgerDialog` rendered as a near-empty window even after the crash fix.** `InitializeComponent()` called `SuspendLayout()` but never called the matching `ResumeLayout()`, so its `TableLayoutPanel` layout was never resolved against the real window size. Added the missing `ResumeLayout(false)` call.
- **`CustomersView`, `CustomerLedgerDialog`, and `CustomerPaymentForm` clipped and overlapped at real high-DPI scaling.** All three used raw, unscaled pixel values for `TableLayoutPanel` row/column sizes and control `MinimumSize`/`Size` — values WinForms does not auto-scale for DPI the way it does `Font`-driven text. This is invisible at 100% DPI but causes real button-text clipping and row overlap at higher scale factors (verified at this pass's test machine's actual 250% hardware scaling). Fixed by routing every such value through `LogicalToDeviceUnits(...)`.

### Fixed (introduced and fixed within this same pass)
- The first attempt at the DPI-scaling fix above used a local helper function (`Scaled(...)`) declared inside `InitializeComponent()`. Opening `CustomersView` in the actual Visual Studio WinForms Designer showed this breaks the Designer's strict code-gen parser ("The designer cannot process the code..."). Fixed by inlining `LogicalToDeviceUnits(...)` calls directly instead of routing through a local function.

### Known Limitation (pre-existing, not introduced by this pass, not fixed)
- After the above fix, the Designer still cannot fully render `CustomersView`, `CustomerLedgerDialog`, or `CustomerPaymentForm`: all three use a pre-existing, deliberate pattern of local helper functions declared inside `InitializeComponent()` for data-driven, repeated control construction (e.g. `ConfigureButton`, `AddGridCol`, `AddField`, `SetupToolBtn`), and the Designer's parser rejects local function declarations wherever they appear in designer-generated code. This predates this pass, is a working architectural choice (avoids copy-pasted per-field/per-button code), and unwinding it into flat Designer-safe code would be a multi-file rewrite — left as-is per standing "don't rewrite working architecture" guidance, and reported honestly rather than silently left as "unavailable."

### Testing
- **Compilation Build (final):** 0 Errors / 0 Warnings (Clean Release compile).
- **Automated Tests (final):** 1061 Total / 1061 Passed / 0 Failed / 0 Skipped across 21 assemblies.
- **Runtime Verification:** PASS — LIVE RUNTIME. Real desktop session (3840×2400 physical, 250% DPI scaling — not the 100% assumed in earlier passes' documentation). Every Customer Management workflow exercised end-to-end against the actual running `Clovent.Desktop.exe`: search/filter/sort, create, edit, activate/deactivate, ledger (filters, print preview, PDF/Excel export), receive payment (validation, recording, ledger/balance verification), and POS integration (customer search/select, walk-in blocked from credit, credit sale within limit, credit limit exceeded blocked/overridden).
- **Responsive UI:** 1366×768, 1280×768, and 1366×900 verified live at their true DPI-equivalent physical size on the test display; no clipping/overlap. 1920×1080 and native 100% DPI hardware NOT VERIFIED — ENVIRONMENT LIMITATION (this display cannot physically render either).
- **Visual Studio Designer:** genuinely attempted (VS 18 Enterprise, full 42-project solution). See Known Limitation above.

### Database
- **QA Data:** `QA-CUSTOMER-FINAL` created through the running application (not a script) to exercise every workflow live; generated 4 orders, 3 payments, 6 ledger entries, and 12 activity log entries in the process.
- **Cleanup:** All of the above deleted via a schema-qualified (`Restaurant.*`), transaction-wrapped SQL script; row counts verified before and after. `Clovent_Restaurant` is a shared database also hosting an unrelated legacy application's schema (`dbo.*`, including its own unrelated `dbo.Customers` table) — every statement was schema-qualified specifically to avoid ever touching it.
- **Baseline data:** Verified unchanged after cleanup (`C001`/John Smith and all pre-existing orders untouched).
- **Retained (not cleanup, a real configuration gap filled):** A `"Credit"` payment method was created via Payment Methods setup — none existed in the dev seed data, so the credit-sale feature was previously unreachable via POS even though the application code fully supports it. Kept rather than removed.

### Documentation
- Updated `docs/testing/RestaurantPOSTesting.md`, `docs/testing/RestaurantPOSManualQA.md`, and `docs/architecture/adr/ADR-006-Customer-Management-and-Ledger.md` to reflect genuine live-runtime and Designer verification results in place of the prior "environment limitation" assumptions.

---

## 2026-08-11 — Customer Management & Ledger Statement Modules

### Changed
- Expanded `CustomersView` to a professional ERP-style customer list manager (visual status filter, text search, custom grid Columns, and bottom summary metrics).
- Created `CustomerPaymentForm` (Receive Customer Payment dialog) for taking customer payment transactions (amount, payment method, reference, notes).
- Extended `CustomerLedgerDialog` to show debits, credits, running balances, date filters, type filters, and print/export (PDF/Excel) action controls.
- Optimized queries inside `ListCustomersQueryHandler` by resolving last transaction dates in a single database round-trip via `GetLastTransactionDatesAsync`.
- Extended `RecordCustomerPaymentCommand` to collect payment details (payment method name, references, notes) and map them onto ledger entries.
- Registered the new `"customers.payment"` permission in the seed task.

### Architecture
- Gated ledger balances by transactional authority. Balance changes are registered through ledger entries rather than direct mutations.
- Isolated Walk-in Customer selections as transient logical configurations in the POS.
- Documented details in `ADR-006`.

### Testing
- **Compilation Build:** 0 Errors / 0 Warnings (Clean Release compile).
- **Automated Tests:** 1062 Total / 1062 Passed / 0 Failed / 0 Skipped. Added `CustomerHandlerTests` (8 tests) and 9 credit payment/override/void/security tests in `PaymentHandlerTests` under `Clovent.Restaurant.Application.Tests`.
  - Added automated security and audit tests for unauthorized manager overrides (throwing validation exception and preventing database changes), authorized manager overrides (verifying exact one-time ledger/balance updates and proper activity audit logging), and denied manager overrides (verifying cancellation, transaction rollback, and log safety).

### Runtime Verification
- **Interactive UI Verification:** NOT VERIFIED — ENVIRONMENT LIMITATION (Headless execution environment; no active desktop display session is available to render forms).
- **DPI targets (1366x768 & 1280x768):** NOT VERIFIED — ENVIRONMENT LIMITATION (No native 100% DPI touchscreen hardware available).
- **Visual Studio Designer compatibility:** NOT VERIFIED — ENVIRONMENT LIMITATION (Visual Studio Designer unavailable in execution environment).

### Database
- **QA Data:** `QA-CUSTOMER-1` (CustomerId `88888888-4444-4444-4444-123456789abc`) was temporarily created to test schema constraints, payment balance queries, and ledger logging.
- **Cleanup:** Completed successfully (the record and all linked transactions were deleted after the run).
- **Baseline data:** Verified unchanged.

### Documentation
- Updated `docs/README.md`, `docs/architecture/RestaurantPOSArchitecture.md`, `docs/testing/RestaurantPOSTesting.md`, and `docs/testing/RestaurantPOSManualQA.md`.
- Created `docs/architecture/adr/ADR-006-Customer-Management-and-Ledger.md`.

### Known Limitations
- The WinForms Designer canvas rendering and native 100% DPI hardware rendering are unverified due to headless server sandbox constraints.

## 2026-08-10 — Final Documentation Audit, Data Cleanup & Baseline Verification
- **Change:** Completed final documentation synchronization and database cleanup.
- **Reason:**
  - Deactivated six temporary layout verification products (`QA-TEST-1` through `QA-TEST-6`) in the catalog database.
  - Verified compilation build runs cleanly with 0 Errors and 0 Warnings.
  - Reconciled final automated test count of 1045 passed tests.
  - Created ADRs for responsive POS layout design (`ADR-004`) and customer credit workflow (`ADR-005`).
  - Added explicit disclaimers for native 100% DPI displays and interactive Visual Studio Designer testing limitations.
- **Files Affected:**
  - Database values in `Catalog.Products` and `Catalog.ProductVariants` (deactivated, no code changes).
  - Documentation index, ADR directory, QA manuals, and testing reports.
- **Verification:**
  - Direct SQL database query validation of deactivated rows and core item persistence.
  - Final Release builds and full test executions.
  - Status: Completed (0 Warnings, 1045/1045 Passing Tests).

## 2026-08-10 — Unified POS Form, Searchable Customer & Keypad Completion
- **Change:** Consolidated POS UI into a single `RestaurantPosForm` with integrated numeric keypad and a searchable customer selector.
- **Reason:** 
  - Having a split `RestaurantPosView` + `PaymentPanel` layout caused layout collapses inside nested containers.
  - The payment numeric keypad needed to handle preset digit replacement logic (e.g. typing a digit over the prefilled balance replacements it rather than appending).
  - Customer selection was restricted to non-typeable combos; cashiers needed search lookup capability.
  - Successive cashiers logging in/out leaked DI scopes and window handles over time.
- **Files Affected:**
  - `src/Clovent.Desktop/Restaurant/Orders/RestaurantPosForm.cs`
  - `src/Clovent.Desktop/Restaurant/Orders/RestaurantPosForm.Designer.cs`
  - `src/Clovent.Desktop/Forms/Identity/LoginForm.cs`
- **Verification:**
  - Runtime verification with PowerShell screenshot-taking and synthetic mouse/keyboard inputs.
  - Automated unit tests (`ProductTileWrappingTests`) verifying flow panel tile calculations.
- **Status:** Completed

---

## 2026-08-09 — Layout Root-Cause, Demo Readiness & POS Polish Pass
- **Change:** Adjusted the layout engine heights, added demo usability guards, and polished visual styling.
- **Reason:**
  - The bottom payment panel was given a dynamic height that collapsed on low-resolution displays; set to absolute 180px.
  - `LoginForm` inputs were clipped on higher DPIs; added content-driven minimum client sizing.
  - Unpaid orders thrown raw database exceptions when completed; added UI-level guard and friendly dialog prompts.
  - Cash/Card split and prices formatted with standard currency rules across POS and Sales Summary screens.
- **Files Affected:**
  - `src/Clovent.Desktop/Forms/Identity/LoginForm.Designer.cs`
  - `src/Clovent.Desktop/Forms/Restaurant/MenuItems/MenuItemsForm.cs`
  - `src/Clovent.Desktop/Forms/Base/CurrencyDisplay.cs`
  - `src/Clovent.Desktop/Forms/Base/StatusBadgeStyler.cs`
- **Verification:**
  - Local compilation and full solution test execution.
- **Status:** Completed

---

## 2026-08-08 — Menu Items Layer & Sales Summary Refinement
- **Change:** Introduced the Menu Items management screen (`MenuItemsForm`) and redesigned the closing report into KPI stat cards.
- **Reason:**
  - Restaurant owners needed a simplified menu and category upkeep tool without being exposed to Catalog's complex Multi-Variant and Price List screens.
  - Needed optional photo uploads per menu item without modifying the shared Catalog database schema.
  - The End-of-Day report needed to visually represent cashier totals and top selling items as modern cards.
- **Files Affected:**
  - `src/Clovent.Desktop/Forms/Restaurant/MenuItems/MenuItemsForm.cs`
  - `src/Clovent.Desktop/Restaurant/EndOfDay/EndOfDayReportView.cs`
- **Verification:**
  - Automated tests running in `Clovent.Restaurant.Application.Tests` and `Clovent.Desktop.Tests`.
- **Status:** Completed

---

## 2026-07-28 — Initial Restaurant POS Core Implementation
- **Change:** Introduced the initial `Clovent.Restaurant` bounded context, databases, tables, and CRUD screens.
- **Reason:**
  - Established initial domain aggregates for order taking, kitchen ticket routing, and split payment recording.
- **Files Affected:**
  - `src/Clovent.Restaurant` (Domain, Application, Infrastructure)
- **Verification:**
  - 206 automated xUnit tests passing across Domain, Application, and Repository infrastructure.
- **Status:** Completed
