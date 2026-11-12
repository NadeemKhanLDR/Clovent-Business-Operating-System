# Reporting / transaction integration review

Historical checkpoint. Use the current
[company/transaction review](../2026-11-12/company-transaction-review.md) for the combined
revision, actual results and active deployment/recovery checklist.

The user approved company-owned customers/ledgers with explicit legacy mapping and
proportional aging with largest fractional remainders. Both formerly failing assertions
now pass. Current implementation, results and the remaining deployment prerequisite are
recorded in [approved ownership and aging](company-ownership/README.md). The earlier
results below remain historical evidence for commit 92a3984, not the current result.
Live UI, physical printing and operational migration remain **NOT VERIFIED**.

## Revision provenance and coordination

- New isolated branch: `audit/reporting-integration-review-20261111`.
- Actual latest transaction integration at discovery:
  **27217fddf9e7adecc15e2cb6502c85f6a47b4bfc**, not 40f4b1c.
- Reported reporting baseline:
  **40f4b1c16f6655d853da6ba7a96fc749f9bfc61c**. Verified ancestor of both reporting
  commit 7444d23 and transaction integration 27217fd.
- Original reporting commit:
  **7444d2375eeb00c486392613c180bf528d03ce15**.
- Applied cleanly as **99f0659ae8d6c372cdd63c67abbe412e0eb03ab7**, on 27217fd.
- Focused integration corrections:
  **312d666841c184b2f7eba716794f6cfa1718eca4**.
- Final review/gate commit: the commit containing this document. Resolve with
  `git log -1 --format=%H audit/reporting-integration-review-20261111`.

Worktree: C:/Users/NexGen/.codex/visualizations/2026/11/04/01a1a636-2871-7d12-87ed-1a1ba60aa9bf/cbos-reporting-integration-review

Inspection found no overlap between the reporting diff and 27217fd's lock/continuity
changes; the cherry-pick had no conflicts. The original integration and reporting
worktrees were clean. Agent-tree and app-thread inventories exposed no separate active
transaction agent to address. No other agent was stopped or another worktree edited.
Both original branches remain unchanged. Later unfinished work was not imported.
No transaction lock/recovery implementation, schema, project file or shared test
infrastructure was changed by this integration.

The only completion-handler change is sale-time payment-label lookup for its immutable
receipt snapshot. Payment persistence/recovery, inventory posting, transaction boundaries,
outbox behavior and locking code were not rewritten.

## Meanings and expected-versus-actual checks

| Field / screen | Meaning | Mixed discounted sale | Split 300 sale |
|---|---|---:|---:|
| Expanded GrossSales / Item Sales | Shelf-price subtotal before discounts; shelf prices can include inclusive tax | 250 | 300 |
| Expanded Discounts | Discount amounts on completed sales | 25 | 0 |
| Expanded TotalBillSales / Bill Total | Gross payable including tax and fees, before refunds | 243 | 300 |
| Expanded NetSales | Legacy compatibility field for Bill Total, not excluding-tax net sales | 243 | 300 |
| New BillSalesExcludingTax | Bill payable less charged tax, including fees, before refunds | 225 | 300 |
| Tax Summary SalesExcludingTax | Item sales after discounts and excluding tax, before refund adjustment | 225 | 300 |
| Tax Summary NetSales | Sales excluding tax less refunded bases | 225 without refunds | 300 without refunds |
| Expanded Tax | Original tax charged, before refund reversals | 18 | 0 |
| Expanded TotalCollected | External order tender; excludes on-account and already-collected advance settlements | 243 | 100 |
| Expanded Cash/CardCollected | External cash / bank-card order tender | 243 / 0 | 40 / 60 |
| OnAccountCreated | Credit sale, not cash collected | 0 | 200 |

Thus **243 is correct for bill payable, not the mixed sale's excluding-tax sales (225),
taxable-only base (180), exempt base (45), or all sales metrics.**
Existing Desktop captions for legacy NetSales already say Bill Total. The DTO now
documents those semantics and exposes a distinct excluding-tax bill property; tests
assert the difference. Tax Summary's net-after-refund definition was not changed.
Sales Summary's existing collection-based bill totals and shift/day tender totals were
not silently redefined as item sales. Fees and inclusive shelf pricing remain explicit.

### Completion dates

Matching frozen receipt completion timestamps attribute completed sales. A later rider
update does not move the sale; later-dated refunds stay on their own refund dates.
Four executed legacy cases (missing JSON, malformed JSON, default timestamp, wrong order)
verify that the pre-existing UpdatedAtUtc fallback remains explicit. No CreatedAtUtc
substitution, inferred historical timestamp or financial backfill was introduced.
Those historical orders still cannot be reliably re-attributed without authoritative
completion evidence.

UTC and configured business dates remain different contracts: a Jan 10 19:30 UTC sale
in UTC+5 is Jan 11 in Expanded but Jan 10 in UTC Tax Summary. The two-day custom range
counts it once. Unifying date contracts and cross-midnight shift/refund attribution
requires a separate decision; no host timezone or accounting calendar was changed.

### Classification and ledger movement corrections

| Reproduction | Expected | Before integration correction | After |
|---|---:|---:|---:|
| Canonical classifier: Discarded / Cardboard / Gift Card Voucher | Not bank card | All bank card | All excluded |
| Canonical supported MasterCard | Bank card | Bank card | Bank card |
| Advance settlement reversed: row external AdvanceReceived | 0 | 100 | 0 |
| Same reversal: non-cash advance credit | 100 | No distinct field | 100 |
| Void credit sale: opening receivable 100, reversal 100 | Closing 0; collection 0; non-cash credit 100 | No distinct credit field | Reconciles with distinct field |
| Registered Cash completion snapshot | Cash | Payment | Cash |

Canonical card matching now uses exact, trimmed, case-insensitive established aliases:
Card, Credit Card, Debit Card, MasterCard, Visa Card. Report and shift queries call this
same classifier. No broad substring remains there. Custom card names outside this
list now remain other tender; review configured aliases before adoption. A typed tender
kind / configuration mapping is a separate design decision.

Receivables movement now separates external AdvanceReceived from NonCashAdvanceCredits
(including restored advance and credit-note liability). NonCashReceivableCredits records
credits reducing AR. The grid captions distinguish ledger credits from collections.
Tests reconcile both sides without treating restored liability as incoming money.
For the synthetic advance reversal: opening 300 + external 0 + restored 100 - used 100
= closing 300. For the AR reversal: opening 100 - non-cash credit 100 = closing 0.

Completion now freezes resolved method names for new snapshots through the injected
payment-method repository. Cash, MasterCard, On Account and Customer Advance are tested.
Existing receipt JSON is never relabelled. Legacy manually composed handlers without
the optional repository retain their historical Payment fallback; a missing method
with a repository is explicitly unknown. Standard DI registers that repository.
No snapshot schema change or historical data migration was performed.

Receipt label “Item calculation base” describes the calculator's sum of all item bases,
including exempt lines. It does not certify that 225 is taxable-only sales; the mixed
sale taxable-only base remains 180. Existing invoice references, tax codes, classifications,
rates, charged/reversed tax, payable amounts and credit-note references are retained.
Fiscal/legal certification and actual printed output were not performed.

## Historical gate reproductions (resolved by the approved follow-up)

| Enabled assertion | Actual reproduction | Decision / work needed |
|---|---|---|
| AgingPennyBuckets_MustSumToOutstandingPenny | Two 0.25 unallocated credit sales aged 2 and 10 days, payment 0.49: receivable 0.01; bucket sum 0.00 | Approve penny residual allocation/tie order while retaining proportional aging, or approve a different debt allocation policy |
| CompanyAReceivables_MustNotExposeCustomerOnlyAssociatedWithCompanyBOrder | Real registered mediator pipeline, separate companies/branches/warehouses: Company A is denied B's order, but receivables returns B-associated customer's name/contact/balance 200 | Customer/opening-balance/ledger ownership and explicit migration mapping required; this is a correctness/security blocker, not a cosmetic global KPI |

The scope test first asserts ScopeMismatchException for B's order as a positive access
denial control. It then executes the real receivables mediator request under A's scope
and detects the customer row. Existing Customer has no persisted CompanyId/BranchId,
CustomerRepository.GetAllAsync is unfiltered, and statement queries do not establish
organizational ownership. Arbitrary customer IDs also reach the ledger query without
ownership enforcement in its source; that additional path is source-confirmed, not
separately SQL penetration-tested here.

Do not infer ownership from whichever company has an order: opening balances, advances,
unallocated payments and intentionally shared customers lack such evidence. Required
separate migration design: select company/branch/organization ownership, map legacy
customers and opening balances explicitly, enforce ownership on customer/ledger/report
reads and posting, and add negative scope tests with real persisted ownership.
No schema migration or arbitrary historical owner assignment was applied.

Changing Math.Round to the approved midpoint strategy alone would not solve aging:
two 0.005 shares can round to 0.02 instead of the required 0.01. An approved residual
distribution rule is needed. No expected values were changed to fit output. These two
test failures are deliberately retained as active review gates.

## Actual combined verification

Evidence: C:/Users/NexGen/.codex/visualizations/2026/11/04/01a1a636-2871-7d12-87ed-1a1ba60aa9bf/transaction-reporting-integration-evidence-20261111

Artifacts: C:/Users/NexGen/AppData/Local/Temp/CBOS-reporting-integration-20261111

| Execution on combined source | Actual result |
|---|---|
| Complete Restaurant.Application.Tests, application-complete.trx | **512 PASS, 2 FAIL, 0 skipped; 514 total** |
| Complete Restaurant domain tests, combined-domain.trx | **247 PASS, 0 FAIL, 0 skipped** |
| Formatter/reconciliation + production composition, formatters-final.trx | **12 PASS, 0 FAIL, 0 skipped** |
| Persistence/serialization/report/recovery/outbox/cross-process cases, infrastructure-final.trx | **31 PASS, 0 FAIL, 0 skipped** |
| Debug solution build | PASS, 0 warnings/errors |
| Release solution build | PASS, 0 warnings/errors |

Of the 31 infrastructure cases, **21 execute on SQL LocalDB** and 10 serialization
pipeline cases use test-owned SQLite files. SQL identity:
Microsoft.EntityFrameworkCore.SqlServer, (localdb)\MSSQLLocalDB, **17.00.4025**.
Guard-validated GUID databases only; Windows integrated authentication, no credentials
logged. The SQL guard verifies actual DB_NAME and provider, then finally deletes each
generated database and verifies DB_ID is NULL. sql-final-databases.jsonl records
**21 created / 21 cleanup-verified**, with identical name sets.
No operational connection target was opened.

SQL case breakdown: 2 reporting, 3 payment recovery (including commit acknowledgements),
3 session-lock/least-privilege/shared-exclusive cases, 4 independent-process payment
cases, 7 outbox-claim cases, 1 refund/outbox concurrency, 1 restart recovery.
Earlier branch test results are not substituted for these runs.
The organizational disclosure and aging reproductions are component/pipeline evidence,
not newly executed SQL ownership-migration qualification.

Before-fix evidence is retained for registered generic payment labels (1 failed case),
false card matches (3 failed cases), and advance-row labelling (1 failed case).
Earlier fixture setup/compile errors are also preserved in logs and are not counted
as successful reproductions. Scope reproduction in application-complete.trx reaches
the disclosure assertion, not setup failure.

## One consolidated manual checklist

Use dedicated synthetic nonproduction data. These checks are **NOT EXECUTED**.

1. **Mixed sale:** taxable 2 × 100 at 10% exclusive, discount 20; exempt 1 × 50,
   discount 5. Pay cash 243. Expect one bill, item subtotal 250, discounts 25,
   excluding-tax sales 225, taxable base 180, exempt base 45, tax 18, bill payable 243.
   Expanded legacy NetSales must be displayed as Bill Total. Receipt calculation base
   225 must not be described as taxable-only sales. Refund one taxable unit with discount
   reversal 10/tax reversal 9: payout 99; original bill remains 243; retained payable
   144, excluding-tax net sales 135, tax 9.
2. **Inclusive partial returns:** sell 1 × 10.05 at 10% inclusive; return 0.5 twice.
   Check persisted residuals (synthetic fixture 5.03/5.02 with tax 0.46/0.45), combined
   refund 10.05, tax 0.91, total returned quantity 1. Original receipt remains 10.05;
   each credit note uses its own stored amounts. Net retained base/tax must be zero.
3. **Split tender / void:** exempt 1 × 300. Void a cash payment 20, then settle cash 40,
   MasterCard 60, on-account 200. If tendering 60 cash, expect change 20 and net cash 40.
   One bill; three active payments plus retained void history; external collection 100,
   cash/card 40/60, customer receivable 200, zero-opening drawer expected cash 40.
   “Discarded” and “Gift Card Voucher” must not become bank-card collections.
4. **Advance / reversal:** prior-day advance 300; editable sale 1 × 100, advance settlement
   100, reverse it, replace with cash 100, then complete. Liability 300 → 200 → 300;
   new cash 100, new external advance receipts 0, non-cash advance credit 100,
   advance used 100, closing advance 300. A separate reversed credit sale of 100
   must reduce AR by non-cash ledger credit, with zero incoming collection.
   Inspect new ledger-credit columns at normal/narrow widths; rendering is unverified.
5. **Dates / history:** Jan 10 sell inclusive 2 × 55; Jan 11 return 1 × 55.
   Jan 10 sale/base/tax 110/100/10; Jan 11 refund/tax reversal 55/5 and no duplicate
   sale; combined retained quantity 1, payable 55, excluding-tax net 50, tax 5.
   Update rider after completion and reprint: frozen completion attribution and amounts
   must stay fixed. Check midnight/custom ranges using the declared UTC/business-date
   contract, and identify legacy missing-date orders rather than inventing their date.
6. **New receipt labels and retry:** complete separate Cash/MasterCard/On Account/Advance
   payments; receipt must retain the genuine sale-time label. Reprint an older generic
   Payment receipt unchanged. Retry after uncertain completion/printing failure: confirm
   one committed payment/order/stock effect and truthful printing outcome using recovery
   screens; physical printing and live recovery interaction remain unverified.
7. **As-of and security gate:** debit 100 Jan 9, pay 60 Jan 11; Jan 10 statement balance
   100 and last transaction Jan 9. In a two-company dataset, Company A must not see B's
   customer/contact/balance 200 or ledger through statements while being denied B's order.
   **Now passes automatically with explicit company ownership; operational migration/mapping
   and this manual check remain unverified.** Unmapped legacy accounts must remain quarantined.
8. **Aging gate:** credit sales 0.25 aged 2 and 10 days; unallocated payment 0.49.
   Receivable and sum of aging buckets must both be 0.01 under the approved residual
   policy. **Now passes automatically: the older 8–15-day bucket gets 0.01, the 1–7-day
   bucket 0.00, total 0.01.** Manual rendering remains unverified.

No main merge, push, package, production-data access, Visual Studio control, Sandbox,
Hyper-V operation or release acceptance occurred.
