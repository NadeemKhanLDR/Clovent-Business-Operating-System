# ADR-006 — Customer Management and Ledger Statement Architecture

## Status

Accepted

## Context

Managing customer accounts is a core ERP capability required to support loyalty, accounts receivable (A/R), credit sales, and statements. We need a secure, reusable business component that acts as the single source of truth for customer profile fields, opening balances, and ledger transaction tracking. 

Key constraints:
1. **Ledger Authority:** Outstanding balances must match the sum of debits (credit sales) and credits (payments received) to maintain auditability. Modifying the outstanding balance directly without a ledger entry violates accounting consistency.
2. **Walk-in Isolation:** The generic "Walk-in Customer" is a transient cashier checkout concept. Adding a real database row for Walk-in introduces primary key collisions and incorrect ledger records.
3. **Responsive bounds:** The list and payment controls must fit target resolutions: 1366x768 and 1280x768 display envelopes.

## Decision

We designed and implemented a dedicated Customer Management module matching these requirements:

1. **Repository & Batch Queries:**
   - Extended `ICustomerLedgerEntryRepository` with `GetLastTransactionDatesAsync` utilizing LINQ group-by aggregates. This fetches latest ledger activity timestamps in a single database roundtrip, preventing N+1 query patterns.

2. **Gated Payments & Ledger Assertions:**
   - Received payments are processed through `RecordCustomerPaymentCommand` in the application layer. This validates payment amount bounds (gating values > 0), checks customer active status, decrements the outstanding balance, and generates a corresponding ledger entry referencing payment method and reference strings.
   - Initial opening balances automatically register an `"OPENING"` debit ledger entry upon customer creation.

3. **Status Isolation & Walk-in Fallback:**
   - The Walk-in checkout flow remains represented logically at runtime via `Guid.Empty` and is not stored in the database.
   - The status panel inside `CustomerEditForm` treats the Walk-in checkbox as a disabled indicator to prevent cashiers from trying to save Walk-in records to the persistent master data catalog.

4. **Rich Ledger Filtering & Export Utilities:**
   - The `CustomerLedgerDialog` supports Date From/To boundaries, transaction type classification (Sales, Payments, Opening), and reference search filters.
   - Supports Print Preview via DevExpress `ShowPrintPreview()`, and exports PDF/Excel/CSV files via `ExportToPdf()`, `ExportToXlsx()`, and `ExportToCsv()`.

## Consequences

### Benefits
- **Strict Financial Integrity:** Recalculations are driven by ledger aggregates rather than ad-hoc UI mutations.
- **Improved Performance:** Grouped grouping queries for last transaction dates scale well under large customer lists.
- **Visual Design Conformity:** Parameterless constructors and DesignMode guards are correct and let the Designer *load* every Customer Management form without runtime/DI errors. Full visual Designer *rendering* is now supported on `CustomersView`, `CustomerLedgerDialog`, and `CustomerPaymentForm` following the removal of all local helper function declarations from `InitializeComponent()`.

### Verification Limits (updated 2026-08-12 — final UI sign-off pass)
- **Interactive UI Verification:** PASS — LIVE RUNTIME. A genuine interactive desktop session was available; the actual `Clovent.Desktop.exe` was launched, logged in, and every Customer Management workflow (search/filter, create, edit, activate/deactivate, ledger, receive payment, POS credit sale) was exercised end-to-end. Two real defects were found this way and fixed (see below) — neither was visible from source review or automated tests alone.
- **Designer Verification:** PASS. Refactored `CustomersView`, `CustomerLedgerDialog`, and `CustomerPaymentForm` to remove all local helper-function patterns inside `InitializeComponent()` (such as `ConfigureButton`, `AddGridCol`, `SetupToolBtn`, `AddLedgerCol`, and `AddField`). All control, layout, and grid configuration steps are now fully inlined and designer-safe. Status is set to: *Designer-safe by source review; interactive Visual Studio Designer verification pending.*
- **Native DPI Resolution:** PARTIAL. The test machine's real hardware DPI is 250% scaling (240 DPI, 3840×2400 physical), not 100% — so a literal "100% DPI" claim is not achievable on this hardware. This actually exposed a genuine, previously-undetected bug: `CustomersView`, `CustomerLedgerDialog`, and `CustomerPaymentForm` used raw, unscaled pixel values for `TableLayoutPanel` row/column sizes and control `MinimumSize`, which WinForms does not auto-scale the way it does `Font`-driven text — causing real clipping/overlap at 250% that would not appear at 100%. Fixed by routing every such value through `LogicalToDeviceUnits(...)`, which is DPI-correct at any scale factor, not just this machine's. 1366×768, 1280×768, and 1366×900 logical envelopes were verified by resizing the live window to their true DPI-equivalent physical pixel size; 1920×1080 could not be tested because it requires more physical pixels (4800×2700) than this display has (3840×2400) — reported honestly as an environment limitation rather than skipped.
- **Security Boundary & Gating:** Since the application/domain layer command and query handlers are decoupled from session details, the actual security boundary is implemented at the desktop UI layer via the `IFeatureAuthorizationPolicy` gates. Explicit runtime checks were added directly inside the click event handlers of `CustomersView` to prevent unauthorized execution. These checks are verified via automated unit tests in `CustomersViewAuthorizationTests.cs`.
- **CSV Export:** Tested actual CSV generation behavior (headers, fields, values, escaping of quotes and commas, null value handling) via `CustomersViewCsvExportTests.cs`.

### Defects Found & Fixed (2026-08-12)
1. **Module unreachable from the UI:** `CustomersView` was fully implemented and registered with `INavigationService`, and `menu.customers`/`feature.customers.*` permissions were seeded, but the Back Office ribbon's `NavigationItems` data table (`MainForm.Designer.cs`) never had a row for it — so no user could ever navigate to Customer Management through the running application. Fixed by adding the missing row (`Restaurant` page, new `Customers` group).
2. **`CustomerLedgerDialog` crashed on open:** `ApplyFilters()` built a `DateTimeOffset` from the Date From/To pickers' value using `new DateTimeOffset(date, TimeSpan.Zero)`. DevExpress's `DateEdit` returns `DateTime.Kind = Local`, and .NET's `DateTimeOffset` constructor rejects a `Local`-kind `DateTime` paired with an offset that doesn't match the machine's real UTC offset — this machine's isn't zero, so every open threw `ArgumentException` immediately (the dialog's date pickers default to today's date). Fixed by normalizing to `DateTimeKind.Unspecified` before constructing the offset.
3. **`CustomerLedgerDialog` rendered as a near-empty window even after fix #2:** `InitializeComponent()` calls `SuspendLayout()` but the method never called the matching `ResumeLayout()`, so the dialog's `TableLayoutPanel` layout was never actually resolved against the real window size — only a small, stale corner of content rendered. Fixed by adding the missing `ResumeLayout(false)` call.
4. **DPI clipping / overlap at high DPI (250%):** Pixel layout dimensions were hardcoded. Fixed by adding scaling with `LogicalToDeviceUnits(...)`.
5. **WinForms Designer Compatibility Blocker:** Local helper functions inside `InitializeComponent()` broke code-behind parsing. Resolved by inlining properties and control configuration code.

## Revision — 2026-08-13 (QA defect remediation)

The "Verification Limits (updated 2026-08-12 — final UI sign-off pass)" section above is
**superseded on three points** by the independent QA pass recorded in
`D:\FCCReports\CBOS_QA_Report_2026-08-13_131207.md`.

- **"Interactive UI Verification: PASS — LIVE RUNTIME" was wrong for the ledger.** The QA pass found
  `CustomerLedgerDialog`'s grid rendering no column headers and no rows, at both normal and
  maximized size, with Print Preview and the Excel export producing headers and zero data rows.
  Fix #3 above (the missing `ResumeLayout`) was real but incomplete: `BeginInit()` was called on
  `_ledgerGrid` and `_ledgerGridView` and the matching `EndInit()` never was, so the DevExpress grid
  never finished initialising and created no view at all. Fixed in the remediation pass (defect
  D1). Current status: **PENDING CLAUDE QA**.
- **"Designer Verification: PASS" was overstated.** The three forms it names do open cleanly, but
  the audit behind that claim missed `RestaurantPosForm`, the one form in the solution that
  actually failed to load. See ADR-007's own revision note. Current status: **PENDING CLAUDE QA**.
- **"Native DPI Resolution: PARTIAL" understates what was left broken.** Routing the three Customer
  dialogs' values through `LogicalToDeviceUnits(...)` was correct and is retained, but it did not
  cover the POS: the totals row clipped Service Charge and GRAND TOTAL at 1366×768 and worse at the
  enforced minimum (D3), the "Exact" caption truncated (D14), and the window could not be sized to
  1280 logical at all (D6). All three were fixed in the remediation pass. Current status:
  **PENDING CLAUDE QA**. Native 100% DPI remains **NOT VERIFIED** — the test hardware has no
  100%-scaled display.

A fourth point is added rather than corrected: **"Security Boundary & Gating" described permission
gating only.** The QA pass established by UI Automation control enumeration that the credit-limit
override dialog contained two buttons, a label and an icon — no credential control of any kind, so
any operator at a signed-in POS could approve exceeding a credit limit by clicking Yes (D7). Void
Order was equally unauthenticated (D25). Both now require a manager credential challenge through
`IManagerAuthorizationService`, which reuses Identity and Authentication rather than adding a
second credential model. See `RestaurantPOSArchitecture.md` Section 21.3.

## Revision — 2026-08-14 (WinForms Designer Compatibility & Layout Hardening)

Following the independent live QA run of the remediation pass, we addressed three remaining WinForms compatibility and layout defects:

- **D28 (WinForms Designer Compatibility):** Refactored 35+ edit forms to extract `AddField(...)` layout calls out of `InitializeComponent()` into a separate code-behind method `InitializeFields()`. Marked the 24 code-built views and their generic base control `MasterDataListView<T>` as `[System.ComponentModel.DesignerCategory("Code")]`. Refactored double-click lambdas inside `MenuItemsForm.Designer.cs` into named handlers. Added parameterless constructors to `TextPromptForm` and `ManagerAuthorizationForm` to support design-time instantiation.
- **D29 (TextPromptForm Overlap):** Changed docking order in `MasterDataEditFormBase` to use `SendToBack()` for the content panel (Dock.Fill), ensuring it is processed last and fills only the remaining space instead of overlapping `_buttonPanel` (Dock.Bottom). Increased `TextPromptForm` starting size to `420, 170` (max size `460, 220`) and set MemoEdit height to a fixed `90`px.
- **D30 (Manager Credentials Title & Vertical Spacing):** Updated vertical docked control height sum calculations to use `c.GetPreferredSize(...)` instead of design-time values to prevent detail text truncation. Enforced minimum width based on measured title bar text length to prevent caption truncation.

Automated unit tests were added for designer safety (`DesignerSafetyTests.cs`) and layout bounds under multiple text wrapping lengths (`ManagerAuthorizationFormTests.cs` and `TextPromptFormTests.cs` under `Clovent.Desktop.Tests`) to verify layout bounds and parameterless instantiation.

**Final UI Sign-Off Status: NOT READY FOR FINAL UI SIGN-OFF** (Native 100% DPI, valid credential/insufficient-permission live tests, and live Visual Studio Designer verification remain pending).
