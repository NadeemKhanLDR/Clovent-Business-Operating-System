# Transaction reporting and reconciliation audit — 11 November 2026

## Revision and isolation

Baseline: **40f4b1c16f6655d853da6ba7a96fc749f9bfc61c**, integration branch
`audit/transaction-integration-review-20261111`. Older supplied checkpoint
`b366d0d0e4073f55480551607cb69fe8c1dea35d` is an ancestor, not the starting revision.
Reporting branch: **audit/transaction-reporting-20261111**.
The final reporting commit is the commit containing this document; resolve it with
`git log -1 --format=%H audit/transaction-reporting-20261111`.

Separate worktree: C:/Users/NexGen/.codex/visualizations/2026/11/04/01a1a636-2871-7d12-87ed-1a1ba60aa9bf/cbos-reporting-audit

D: lacked room for a checkout, so this worktree and all outputs were placed on C:.
No changes were imported from the integration worktree's later uncommitted recovery,
locking, continuity or startup edits. Original branches, unrelated Designer edits,
candidate and backups were not edited. No project files, shared test infrastructure,
command handlers, locks, recovery journals or shared payment classifier were changed.
There was no merge, push, package creation, UI control or VM operation.

## Confirmed discrepancies

Before values below were reproduced with failing assertions, not inferred from screenshots.

| Defect / source | Independently expected | Before | Fixed result |
|---|---|---|---|
| Expanded KPI and order-type totals omitted exclusive tax, and type totals omitted other bill charges | Mixed sale bill 243 | KPI 225 while bill row 243 | KPI and type totals sum authoritative bill rows: 243 |
| Expanded detail constructor mapped rate to Tax and tax amount to ServiceCharge | Tax 18; service charge 0 | Tax 10; service charge 18 | Tax 18; service charge 0; order-level charges remain at order level |
| Expanded date predicate OR-ed creation and completion dates | Created Jan 9, completed Jan 10: one Jan 10 sale | Same sale included on Jan 9 and Jan 10 | Included only on completion business date |
| Sales Summary, Expanded and Tax Summary used mutable UpdatedAtUtc | Sale stays on frozen Jan 10 completion date after Jan 11 rider update | Jan 10 sale disappeared | Frozen receipt completion timestamp drives attribution and bill timestamp |
| Shift summary used narrower card-name matching than other reports | MasterCard collection 60 | Card sales 0; misclassified as other | Existing shared classifier used: card 60 |
| Expanded treated void ledger credits as fresh customer money | Advance settlement reversal: new collection 0; new advance receipt 0 | Both incorrectly 100 | Both 0; restored liability remains represented in ledger movement |
| As-of receivables fetched all-time last transaction dates | Jan 10 as-of: last entry Jan 9 | Last date Jan 11, although balance excluded that entry | Both balance and last date use entries through as-of |
| Receipt called a sum containing exempt bases “Net Taxable Sales” | Mixed sale calculation base 225; genuinely taxable base 180 | Receipt described 225 as taxable sales | Label “Item calculation base”; no amounts or historical snapshots changed |

The completion-date helper accepts only a matching-order, nondefault frozen receipt
timestamp. Legacy absent/malformed/unmatched snapshots retain the previous UpdatedAtUtc
fallback; that fallback cannot reconstruct lost completion history.
No financial rows or receipt snapshots are rewritten.

Void-credit recognition follows the existing producer's VOID- reference plus known
Void Advance Settlement / Void Credit Sale / Void Order Reversal descriptions.
Restored credit is not an external cash/card collection. Unknown legacy credit kinds
are not silently reclassified; a typed ledger-entry contract would need a separate change.
The advance-receipts KPI now counts external advance receipts separately from liability
restoration by credit notes/reversals.

## Synthetic reconciliation matrix

Fixtures set persisted facts explicitly. Literal expected values are calculated separately
from production calculators. Component tests use in-memory repositories; SQL tests described
below reload facts from SQL repositories. These are harness assistance, not commissioning,
command rollback qualification or live financial records.

| Dataset (quantities and prices) | Expected | Actual after fixes |
|---|---|---|
| Taxable 2 × 100, exclusive 10%, allocated discount 20; exempt 1 × 50, discount 5 | Gross 250; discounts 25; excluding tax 225 = 180 taxable + 45 exempt; tax 18; payable/cash 243 | All match; bill rows, KPI and order-type total each 243 |
| Exclusive partial return of 1 of those 2 taxable units | Gross reversed 100; discount 10; tax 9; payable refund 99; retained sales excluding tax 90 and tax 9 | All match Tax Summary and Refund Audit |
| Inclusive 1 × 10.05 at 10%; two returns of 0.5 units each | Original base 9.14, tax 0.91; refunds 5.03 + 5.02 = 10.05 and tax 0.46 + 0.45 = 0.91; remaining net sales/tax 0 | All match; original frozen receipt unchanged; both credit notes retain stored residuals |
| Exempt 1 × 300; voided cash 20 then valid cash 40, MasterCard 60, on-account 200 | Active sale 300; external collections 100 = cash 40 + card 60; receivable 200; shift expected cash 40 with opening 0 | All match Expanded, Shift, business-day summary and receivables |
| Prior-day customer advance 300; sale 1 × 100; advance settlement 100 voided and replaced with cash 100 | Liability 300 → 200 → 300; current cash sale 100; new advance receipts/customer collections 0; net customer position -300 | All match; no invented second collection |
| Jan 10 inclusive sale 2 × 55 at 10%; Jan 11 refund 1 × 55 | Jan 10 gross 110, excluding tax 100, tax 10, refunds 0; Jan 11 refund 55, tax reversal 5, net tax -5 and net sales -50; original sale stays Jan 10 | All match; Jan 11 Expanded contains refund 55 and no sale row |
| Jan 9 customer debit 100; Jan 11 payment 60; statement as of Jan 10 | Balance 100 and last transaction Jan 9 | Both match; future payment excluded |
| Own warehouse sale 100; foreign warehouse sale 900; own cancelled and voided drafts | Own completed report count 1 and total 100 | Match; foreign/status rows excluded |

Cash figures use persisted Payment.Amount (net collection), not the customer's tendered
note amount. Cash tender/change entry through the real UI and command pipeline is outside
this reporting qualification. These report fixtures do not prove stock posting, command
idempotency, database rollback, locking or crash recovery.

## Executed verification

Evidence directory (logs, TRX and disposable-database records):
C:/Users/NexGen/.codex/visualizations/2026/11/04/01a1a636-2871-7d12-87ed-1a1ba60aa9bf/transaction-reporting-evidence-20261111

| Execution | Actual result |
|---|---|
| Initial focused reports, before fixes (`reports-before.trx`) | 9 total: 6 FAIL, 3 PASS |
| Operational-update date regression, before date fix (`completion-date-before.trx`) | 1 FAIL |
| Receipt calculation-base label, before formatter fix (`receipt-label-before.trx`) | 2 FAIL |
| Complete Restaurant.Application.Tests (`application-final.trx`) | 499 PASS, 0 FAIL, 0 skipped; includes 12 new focused report tests |
| Receipt formatter + new reconciliation formatter tests (`formatters-verified.trx`) | 6 PASS, 0 FAIL, 0 skipped; 4 new cases |
| New SQL report tests (`sql-final.trx`) | 2 PASS, 0 FAIL, 0 skipped |
| Full solution Debug (`build-debug-final.log`) | PASS; 0 warnings, 0 errors |
| Full solution Release (`build-release-final.log`) | PASS; 0 warnings, 0 errors |

Build artifacts: C:/Users/NexGen/AppData/Local/Temp/CBOS-reporting-audit-20261111.
No package upgrade or host configuration change was performed.

SQL qualification: Microsoft.EntityFrameworkCore.SqlServer, server
`(localdb)\MSSQLLocalDB`, actual engine version **17.00.4025**.
Connection guard requires Windows integrated authentication, no SQL username/password,
and an allowlisted GUID database name. Each test verifies DB_NAME() equals its generated
target before report execution; finally deletes that database and verifies DB_ID is NULL.
`sql-final-databases.jsonl` records two created/cleanup-verified pairs.
Only generated CBOS_Recovery_<32 hex> databases were used; no application configuration
connection string or production/customer database was opened.

SQL cases persist/reload the mixed discounted exclusive and inclusive sales, exercise
Tax Summary and Expanded with real financial repositories, compare row/type/KPI totals,
check detail tax/service-charge mapping, and check a foreign warehouse yields zero.
The catalogue mediator only omits optional catalogue labels. Other scenarios in the matrix
are component-tested, not newly qualified on SQL. Earlier SQL recovery/locking results are
not counted as results of this reporting branch.

## Remaining decisions and limits

- **Date contracts differ, reproduced:** at 2026-01-10 19:30 UTC in an explicit UTC+5
  business zone, Expanded assigns the sale to Jan 11, while Tax Summary's explicit UTC
  contract assigns it to Jan 10. The custom two-day filter counts it once. Refund Audit
  and Sales Summary also use UTC calendars; business-day totals use configured business
  dates and selected shifts. No timezone, schema, host setting or accounting policy was
  changed. Decide a common report-date contract before unifying these screens.
- **Cross-midnight shifts need qualification:** business-day totals select shifts by
  opening business date and combine day-dated/shift-associated refunds with ID deduplication.
  Their operating-day totals need not equal a UTC calendar report. Cross-day shift/refund
  attribution is not qualified by the new SQL cases.
- **Customer scope is global:** Expanded's order/refund facts are warehouse-scoped, while
  customer receivable/advance movement reads customer-wide ledgers. Customer statements
  have a customer-wide/as-of contract. Do not describe these AR KPIs as warehouse-local.
  Warehouse allocation and authorization changes require an explicit owner/policy decision.
  No company/branch authorization penetration test was performed.
- **Aging penny allocation remains unqualified:** existing proportional aging rounds buckets
  independently. Residual pennies can require an approved allocation/tie policy; this pass
  does not change that policy or claim exhaustive aging reconciliation.
- **Owner handoff — frozen payment labels:** CompleteOrderCommand constructs snapshot payments
  with literal method label “Payment”. A formatter cannot recover the genuine cash/card/
  account name from those frozen objects. Proposed owner fix: resolve and persist the method
  label at snapshot creation, with historical compatibility; do not fabricate old labels.
  The formatter tests here pass explicit frozen labels and therefore do not qualify that producer.
- Legacy no-snapshot sale dates retain fallback limitations. Not every join/catalogue combination,
  refund settlement category, refund charge allocation, discount-after-tax configuration or
  historical malformed snapshot was exercised. Read consistency during a concurrent report
  remains outside this pinned report-query audit.
- **Live UI, Designer rendering, resizing, physical printing and manual report checks:
  NOT VERIFIED.** Compilation and pure string formatting are separate evidence.

## Manual report checklist (dedicated nonproduction data only)

1. Warehouse A: complete taxable 2 × 100 with 20 line/order-allocated discount at 10%
   exclusive plus exempt 1 × 50 with 5 discount. Record cash 243. Compare Sales Summary,
   Expanded, Tax Summary and reprinted receipt: one completed bill, gross 250, discount 25,
   excluding tax 225, taxable base 180, exempt 45, tax 18, payable 243. Detail tax 18,
   service charge 0. Item calculation base 225 is not a claim of taxable-only sales.
2. Complete 1 × 10.05 inclusive at 10%, then refund 0.5 units twice. Verify separate
   credit notes use the persisted 5.03/5.02 residuals and 0.46/0.45 reversed tax
   (if the approved refund calculator assigns the opposite ordering, inspect its persisted
   facts rather than demanding this fixture's order). Combined refunds 10.05, quantity 1,
   tax 0.91; net retained sale/tax zero. Original receipt stays 10.05 and 0.91.
3. Complete exempt 1 × 300 using voided cash 20, then valid cash 40 + MasterCard 60 +
   on-account 200. If entering tendered cash 60, expect change 20 and net cash 40.
   Expect one bill; three active payment rows plus one audit-retained void; external
   collections 100, receivable 200; zero-opening drawer expected cash 40.
4. Receive advance 300 on the prior date. For a 1 × 100 sale, settle 100 from advance,
   void that settlement while the order is editable, then pay cash 100 and complete.
   Ledger liability must read 300 → 200 → 300; final advance 300, current cash 100,
   new current-period advance/customer collection KPIs zero. Inspect reversal history.
5. Jan 10: complete inclusive 2 × 55; Jan 11: refund 1 × 55. On Jan 10 expect sale
   110/base 100/tax 10 and no refund; on Jan 11 expect refund 55/tax reversal 5 and no
   duplicate sale. Across both dates expect retained quantity 1, retained payable 55,
   net sales excluding tax 50 and tax 5. Confirm the chosen UTC/business-date contract.
6. Repeat with Warehouse B and a different customer, cancelled/voided drafts, a custom
   range straddling midnight and an as-of statement before a later payment. Verify
   warehouse sale isolation, no duplicate bills, customer ledger balances, and the
   global/customer scope labels. Assign/update a rider after completion and re-run the
   original sale-date reports: the sale and its receipt completion date must remain fixed.

No whole-application reporting or live-printing acceptance is claimed.
