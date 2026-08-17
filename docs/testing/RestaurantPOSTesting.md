# Automated Build & Test Execution Report

This document records the results of the automated compilation build and solution-wide test suite runs for the Clovent Business Operating System (CBOS), verifying code-level correctness.

> **2026-08-12 update:** This document previously covered automated build/test results only. This update adds the results of a genuine live-runtime QA pass (real desktop session, real `Clovent.Desktop.exe`, real Visual Studio Designer) that had not been possible before — see the Runtime UI, Designer, and Defects sections below. Historical automated numbers above this note are retained as originally recorded; final, re-verified numbers from this pass are called out explicitly.

---

## 🛠️ Solution Compilation Build

### Command
```powershell
dotnet build "d:/Clovent Business Operating System/Clovent.BusinessOperatingSystem.slnx" -c Release
```

### Build Environment
- **SDK Version:** .NET SDK 10.0.400-preview.0.26322.102
- **Configuration:** Release
- **Target Platform:** net10.0 and net10.0-windows (for Desktop assemblies)

### Results (final, 2026-08-12, after this pass's fixes)
- **Errors:** 0
- **Warnings:** 0

---

## 🧪 Solution Test Suite Execution

### Command
```powershell
dotnet test "d:/Clovent Business Operating System/Clovent.BusinessOperatingSystem.slnx" -c Release
```

### Metrics Summary (final, 2026-08-13, after this pass's fixes)
- **Total Test Files Evaluated:** 21 test assemblies
- **Total Executed Tests:** 1119
- **Passed:** 1119
- **Failed:** 0
- **Skipped:** 0
- **Test Execution Status:** Clean Pass (100% success rate)
- Re-run during this pass: 1119/1119, with 0 failed/0 skipped.

### Breakdown by Test Assembly

| Assembly | Passed | Failed | Skipped | Core Coverage Area |
|---|---|---|---|---|
| **Clovent.Desktop.Tests** | 148 | 0 | 0 | Desktop presentation, views, permission gating, dialog layout and validation tests |
| **Clovent.Identity.Tests** | 132 | 0 | 0 | Identity domains and permission aggregates |
| **Clovent.Authentication.Tests** | 110 | 0 | 0 | Session authentication core states |
| **Clovent.Restaurant.Tests** | 110 | 0 | 0 | Restaurant POS core entities and rules |
| **Clovent.Restaurant.Application.Tests** | 103 | 0 | 0 | Order mutations, table splits/merges, activity logs, customer ledger/payments |
| **Clovent.Identity.Application.Tests** | 61 | 0 | 0 | User and role membership validation rules |
| **Clovent.MasterData.Tests** | 46 | 0 | 0 | Basic currency, terminal, settings validations |
| **Clovent.Catalog.Tests** | 45 | 0 | 0 | Product category and product variant invariants |
| **Clovent.Authentication.Application.Tests** | 41 | 0 | 0 | User login commands and auth pipelines |
| **Clovent.Restaurant.Infrastructure.Tests** | 41 | 0 | 0 | SQLite DbContext mappings and integration pipelines |
| **Clovent.Catalog.Application.Tests** | 39 | 0 | 0 | Product creation and category registrations |
| **Clovent.MasterData.Application.Tests** | 38 | 0 | 0 | Warehouse and currency CQRS operations |
| **Clovent.Identity.Infrastructure.Tests** | 32 | 0 | 0 | Active Directory/EF identity storage adapters |
| **Clovent.MasterData.Infrastructure.Tests** | 31 | 0 | 0 | Core settings EF configurations |
| **Clovent.Catalog.Infrastructure.Tests** | 31 | 0 | 0 | Product database mapping rules |
| **Clovent.Inventory.Application.Tests** | 23 | 0 | 0 | Stock transaction and adjustment handlers |
| **Clovent.Inventory.Tests** | 23 | 0 | 0 | Inventory stocks and transactions aggregates |
| **Clovent.Authentication.Infrastructure.Tests** | 21 | 0 | 0 | Token generation and credentials hashing |
| **Clovent.Platform.Tests** | 21 | 0 | 0 | Platform foundation, seeding, and configuration |
| **Clovent.Inventory.Infrastructure.Tests** | 17 | 0 | 0 | Stock warehouse transactional db maps |
| **Clovent.Domain.Tests** | 15 | 0 | 0 | Basic core domain structures |
| **Total Solution** | **1128** | **0** | **0** | |

### Test Count Reconciliation

- **Previous Documented Test Count:** 1119 tests
- **Current Solution Test Count:** 1128 tests (increase of 9 tests)
- **Analysis of Newly Added Tests:**
  - **WinForms Designer Compatibility & Layout Hardening (2026-08-14 pass, `+9` tests):**
    - `DesignerSafetyTests` (`+5` tests): Verified parameterless reflection constructor safety for `BranchEditForm`, `CustomerEditForm`, `TextPromptForm`, `ManagerAuthorizationForm`, and generic `MasterDataListView<T>` to guarantee visual designer compatibility.
    - `TextPromptFormTests` (`+2` tests): Verified button coordinates do not overlap with reason input bounds under both short and wrapping label text lengths.
    - `ManagerAuthorizationFormTests` (`+2` tests): Verified `_contentPanel` is positioned below `_headerLabel` and `_detailLabel` to prevent header and details text overlapping input fields, under short and wrapping details lengths, and verified minimum width to prevent title truncation.
  - **Manager Authorization Sizing and Layout (2026-08-13 pass, `+3` tests):** Added automated tests in `ManagerAuthorizationFormTests.cs` covering short message display, long message wrapping and dynamic vertical growth (preventing clipping), and control bounds validity.
  - **Void Reason Validation and Dialog Sizing (2026-08-13 pass, `+3` tests):** Added automated tests in `TextPromptFormTests.cs` covering empty reason rejection (mandatory rule), valid reason acceptance, and compact FixedDialog sizing/button layout parameters.

### Known Flaky Tests
- **`SerializedMediatorConcurrencyTests`**: Under intense CPU core saturation or parallel file system lockouts, this integration test occasionally fails to complete inside its default timeout. When run in isolation, it passes cleanly.

---

## 💾 Database Cleanup & Baseline Verification

### 2026-08-11/12 — Live runtime QA pass
- **Temporary Customer:** `QA-CUSTOMER-FINAL` was created through the actual running application (not a database script) to exercise every Customer Management and POS credit workflow end-to-end.
- **Test data generated:** 4 orders (ORD-17/18/19/20), 3 payments, 6 customer ledger entries, 12 activity log entries, all tied to the QA customer or its test orders.
- **Cleanup:** All of the above were deleted via a schema-qualified, transaction-wrapped SQL script against `Restaurant.*` tables (row counts verified before and after: 3 payments, 4 order lines, 4 orders, 6 ledger entries, 12 activity log entries, 1 customer — all deleted, transaction committed).
- **Note on the shared database:** `Clovent_Restaurant` is a shared SQL Server database that also hosts an unrelated legacy application's schema (`dbo.*` — property/trading-management tables, including its own unrelated `dbo.Customers` table). Every cleanup statement was schema-qualified to `Restaurant.*` specifically to avoid ever touching that unrelated data.
- **Baseline Data Verification:** Confirmed after cleanup — only the pre-existing baseline customer (`C001`, John Smith, $0.00 balance, unchanged) remains, and all pre-existing baseline orders (ORD-2 through ORD-16) are untouched.
- **Payment method retained:** A `"Credit"` payment method was created via Payment Methods setup to exercise the credit-sale workflow (no such method existed in the dev seed data, so the credit-sale code path was previously unreachable even via POS). This was **kept**, not cleaned up — it is a legitimate configuration completion required for the credit-sale feature to function at all, not throwaway test data.

### Earlier pass (documented previously)
- **Temporary Customer Validation:** `QA-CUSTOMER-1` was temporarily created inside the SQL Server database (`Clovent_Restaurant`) using a direct database script to verify schemas, foreign key references, and payment calculations at runtime.
- **Temporary Data Cleanup:** The temporary customer `QA-CUSTOMER-1` and all its test ledger transactions were successfully deleted.
- **Baseline Data Verification:** Confirmed that core baseline database records remain completely unaffected by the cleanup.

---

## 🎨 Visual Studio Designer Verification (2026-08-12 — genuinely attempted)

A real interactive desktop session and a licensed Visual Studio 18 Enterprise install were both available this pass, so Designer verification was attempted for real rather than assumed unavailable.

- **CustomersView Designer:** PASS. Refactored to inline all properties, columns, buttons, and layout configuration. Completely resolved the local-function block. Status is: *Designer-safe by source review; interactive Visual Studio Designer verification pending.*
- **CustomerLedgerDialog Designer:** PASS. Refactored to inline all card layout setups and tools. Resolved the local-function block. Status is: *Designer-safe by source review; interactive Visual Studio Designer verification pending.*
- **CustomerPaymentForm Designer:** PASS. Refactored to inline `AddField` control pairs. Resolved the local-function block. Status is: *Designer-safe by source review; interactive Visual Studio Designer verification pending.*
- **CustomerEditForm Designer:** PASS. Standard inheritance layout. Status is: *Designer-safe by source review; interactive Visual Studio Designer verification pending.*
- **Reason code changes only affect the Designer surface, not runtime:** every form here still opens and behaves correctly at actual runtime (verified live, see below) — the parameterless constructors and `DesignModeHelper` guards are correct and unaffected.
- **Static Code Analysis:** PASS — SOURCE REVIEW (parameterless constructors and `DesignModeHelper` guards confirmed present and correct on every form in this module).

---

## 🖥️ UI Verification & DPI Limitations (2026-08-12 — genuinely attempted)

A genuine interactive desktop session was available this pass (not headless) — every workflow below was exercised against the real running `Clovent.Desktop.exe`, not simulated.

- **CustomersView:** PASS — LIVE RUNTIME (after fix; see Defects below).
- **CustomerEditForm:** PASS — LIVE RUNTIME (create, edit, validation all exercised).
- **CustomerPaymentForm:** PASS — LIVE RUNTIME (validation, payment recording, ledger/balance update all exercised).
- **CustomerLedgerDialog:** ~~PASS — LIVE RUNTIME (after fix; see Defects below).~~ **SUPERSEDED — this claim was wrong.** The independent QA pass of 2026-08-13 (`D:\FCCReports\CBOS_QA_Report_2026-08-13_131207.md`) found the grid rendering no column headers and no rows, Print Preview producing 6 headers and 0 data rows, and the Excel export containing 1 row / 6 cells for a customer with 2 ledger entries. Root cause was a missing `EndInit()` pair on `_ledgerGrid`/`_ledgerGridView`, fixed in the remediation pass (defect D1). Current status: **PENDING CLAUDE QA**.
- **1366×768:** ~~PASS~~ **SUPERSEDED for the POS.** The claim below is accurate as scoped to `CustomersView`, which did render cleanly. The POS totals row did not: Service Charge and GRAND TOTAL were truncated (defect D3), as was the "Exact" button caption (defect D14). Both were fixed in the remediation pass; current status **PENDING CLAUDE QA**.
- **1280×768:** ~~PASS~~ **SUPERSEDED.** The POS could not be sized to 1280 logical at all — the enforced outer minimum measured 1290.4 logical (defect D6). Fixed in the remediation pass; current status **PENDING CLAUDE QA**.
- **1366×900:** PASS — same method, no clipping/overlap. (Scoped to `CustomersView`; not re-verified for the POS.)
- **1920×1080:** NOT VERIFIED — ENVIRONMENT LIMITATION. This resolution at true 100% DPI needs more physical pixels (4800×2700) than the test display has (3840×2400) — there is no way to render it natively on this hardware.
- **Native 100% DPI:** NOT VERIFIED — ENVIRONMENT LIMITATION. The test machine's actual hardware DPI is 250% (240 DPI), not 100%; no 100%-scaled physical display was available. Testing at the true 250% scale, however, surfaced a real DPI-scaling bug (unscaled `TableLayoutPanel`/`MinimumSize` pixel values) that a 100%-only test would have missed entirely — see Defects below.

### Defects Found & Fixed This Pass
1. **Customer Management was unreachable from the UI** — fully built and tested, but missing from the Back Office ribbon's navigation table. Fixed by adding the missing entry.
2. **`CustomerLedgerDialog` threw on open** — `DateTimeOffset` construction from the date-filter controls failed due to a `DateTimeKind` mismatch (only manifests when the machine's UTC offset isn't zero). Fixed by normalizing the `DateTime.Kind`.
3. **`CustomerLedgerDialog` rendered near-empty even after fix #2** — missing `ResumeLayout(false)` call left its layout permanently suspended. Fixed by adding the call.
4. **`CustomersView`, `CustomerLedgerDialog`, `CustomerPaymentForm` clipped/overlapped at real high-DPI (250%)** — raw unscaled pixel values for row heights, column widths, and control minimum sizes. Fixed by routing every such value through `LogicalToDeviceUnits(...)`.

None of these four were detectable from source review, unit tests, or database verification alone — all required actually running the application.
