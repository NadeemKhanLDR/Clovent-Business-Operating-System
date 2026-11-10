# Transaction failure and recovery pass — 2026-11-10

## Root cause analysis

Verified starting checkout: D:\CBOS\Worktrees\cbos-structure-designer-audit, branch audit/structure-designer-20261109, HEAD 8c6fbb07e96254f9ae01cfdd8c2552d2b3e4b74e. This already includes the previous transaction fixes; they were preserved.

Another workstream began changing transaction tests, handlers and refund rounding during this pass. To follow the repository's isolation rule, final work was moved to D:\CBOS\Worktrees\cbos-transaction-recovery-audit, branch audit/transaction-recovery-20261110, based on the same HEAD. The original checkout was left intact, including its uncommitted changes and this pass's earlier working copies. No merge was performed. Use the isolated checkout for the commit and manual checklist below; the shared checkout may contain newer independent work.

| Confirmed defect | Reproduction | Focused fix |
|---|---|---|
| Failed/cancelled commands leave a staged payment and modified order tracked | Three failing relational tests: handler exception, cancellation before save, database insert rejection | Restaurant UnitOfWork clears failed tracked financial work on save failure; its MediatR behavior also clears handler-staged work on command failure. This cleanup is not itself database rollback. |
| Naively clearing everything breaks outbox retry updates | Broad SQL regression exposed a lost original concurrency token after nested inventory failure | Preserve unchanged worker-owned OutboxMessage entries; detach financial entries and unsaved/modified outbox work. Existing worker-collision assertions remain intact. |
| Refund receipt errors escape through the financial-failure catch after commit | Injected receipt presenter throws; the pre-fix hidden-dialog regression fails | Separate presentation error handling, retain ResultRefund, report committed status and reprint instructions, return OK, reject resubmission of the confirmed result. |
| Online payment retry generates a new key; an uncertain online result offers an offline sale | Source inspection plus lost-acknowledgement component tests | Retain the immutable payment request/key through uncertain results and refresh failures. Refresh authoritative state; reject changed details while unresolved. Remove the post-send offline replacement offer. |
| Payment saved but later completion/logging/receipt work fails | Injected completion/receipt follow-up error | Handle acknowledged-payment follow-up separately, refresh best effort, explain that payment is saved and completion/reprint may be needed. |
| A tracked Open order survives completion by another scope | Failing real-handler regression against a fully paid order; old screen can edit it | Reload unchanged tracked orders before ID-based use. Preserve actual in-flight modifications. Completed order editing then rejects. |
| Payment command accepts a closed selected shift | Failing cross-context stale-shift regression | Validate the selected shift in the command and reload unchanged tracked shifts. A typed domain rejection explains that no payment was saved. Same-key committed-payment recovery still happens first. |
| A tracked payment retains its old non-void flag after another scope voids it | Failing real-pipeline regression rejects a valid replacement payment using stale balance | Read payment lists/idempotency results without tracking; reload unchanged single-payment entities before mutation. Original voided rows remain in audit history. |
| Refund response can be lost after commit | Hidden-dialog regression injects commit into fake history then loses acknowledgement | Requery refund history, update remaining quantities, and accept only a saved record matching key, order, reason, settlement/reference, shift/terminal, cashier and line payload. Otherwise keep the result explicitly uncertain. |
| Four lifecycle tests cannot construct the payment handler | Full infrastructure run fails missing allocation repository registration | Register the real CustomerPaymentAllocationRepository in that existing test fixture. No assertion was removed or weakened. |

## Architectural and business decisions

Payment persistence and order completion remain separate commands. A saved payment can legitimately leave a paid Open order when completion fails; recovery completes that existing order instead of collecting again. A receipt failure must not reverse or repeat a payment/refund.

Refund financial posting and inventory delivery use the existing durable outbox. Inventory can be pending, retrying or quarantined after the refund commits. Do not fabricate inventory or submit a second financial transaction to repair an inventory/printing error. Existing SQL consistency tests cover insufficient stock, missing correlation, out-of-order delivery, duplicate delivery and competing workers.

Known Restaurant payment domain rejection is distinguished from uncertain acknowledgement. The former permits a corrected attempt after refresh; the latter retains its original payload/key. A retained key is local to this open screen; this pass does not add a new persisted attempt contract.

No schema migration, package upgrade, financial rounding change, persisted payload change or framework change was introduced. Financial history is retained; refunds remain compensating documents and voided payment rows are not deleted.

## Implementation and files

Production: PaymentAttemptRecovery; RestaurantPosForm payment path; RefundOrderDialog result/presentation/recovery path; RecordPaymentCommand selected-shift validation; RestaurantDomainException; Restaurant UnitOfWork/UnitOfWorkBehavior; OrderRepository, PaymentRepository and ShiftRepository freshness.

Tests: PaymentAttemptRecoveryTests; RefundOrderDialogTests; FailedCommandRecoveryTests; SqlPaymentRecoveryTests; SqlRestartRecoveryTests; one real-repository registration in OrderLifecycleIntegrationTests.

The commit contains no Designer.cs or resx edits. Existing Designer changes, screenshots, untracked resources, disk patches and recovery copies were preserved. AutoRecover copies are not a complete export of live unsaved buffers. No Visual Studio buffer was saved, reloaded, discarded or controlled.

## Testing verification

Final runs from the isolated checkout:

| Execution | Result |
|---|---|
| Full Restaurant.Infrastructure.Tests | 142 PASS, 0 FAIL, 0 SKIP |
| Full Restaurant.Application.Tests | 471 PASS, 0 FAIL, 0 SKIP |
| Selected Desktop refund/retry/payment-rules/outbox-startup tests | 33 PASS, 0 FAIL, 0 SKIP |
| Selected Restaurant domain order/payment/refund/shift tests | 124 PASS, 0 FAIL, 0 SKIP |
| Total | 770 PASS, 0 FAIL, 0 SKIP |

The infrastructure run includes 26 executed SQL Server cases from InventorySaleRefundConsistencyIntegrationTests, OutboxClaimConcurrencyIntegrationTests, RefundOutboxConcurrencyIntegrationTests, SqlPaymentRecoveryTests and SqlRestartRecoveryTests. Each uses GUID-named disposable LocalDB databases. No operational connection string or production database was used.

New SQL proof uses the real payment handler, MediatR commit behavior, EF Core and SQL Server:
- A temporary CHECK constraint rejects payment persistence. Fresh-context verification finds zero payments, zero ledger/allocation rows and unchanged customer balance. Removing the injected fault and retrying yields one payment/ledger entry and balance 100.
- Bounded execution barriers inject cancellation immediately before SaveChanges or after its successful committed save, before acknowledgement. Fresh contexts see respectively zero or one payment. Same-key retry recovers exactly one payment; a different full-payment key is rejected after settlement.
- New provider/worker instances recover pending, due-retry and stale Processing receipt work. A second new worker does not dispatch completed work again. Failed and DeadLetter records retain their diagnostic state for explicit operator action. This recreates providers/workers; it does not kill or restart the desktop process.
- Existing SQL tests exercise refund/outbox collisions and inventory movement deduplication through the real registered infrastructure.

SQLite tests separately exercise real handlers, repositories, the MediatR behavior and relational persistence. Application handler fakes and Desktop fake mediators are component evidence, not proof of database rollback. Receipt presenters are injected; no physical printer or external payment processor was exercised.

Evidence: D:\CBOS\Artifacts\transaction-recovery-20261110. Final logs/TRX are infrastructure-final, application-final, desktop-final and domain-final. Failed reproductions remain in recovery-before, receipt-before, stale-paid-before, closed-shift-before and stale-void-before. The earlier outbox cleanup regression and four fixture failures remain in outbox-regressions and infrastructure-isolated. They were fixed rather than filtered out of the final infrastructure run.

LIVE UI NOT EXECUTED. VISUAL STUDIO DESIGNER UI NOT EXECUTED. Manual checks below are NOT VERIFIED.

## Build verification

Full solution Debug and Release builds pass, zero warnings and zero errors (debug-final.log, release-final.log). No publish, packaging, merge or push.

## Coverage and remaining concerns

| Requested scenario | Actual evidence and limit |
|---|---|
| Payment saved; completion/receipt fails | SQL commit/ack recovery plus tested UI coordinator/follow-up logic. Actual receipt rendering/printing and cashier dialogs remain manual. |
| Refund saved; inventory/receipt fails | Existing SQL inventory/outbox consistency cases; injected receipt and lost-response dialog tests. Actual printer unavailable scenarios are manual. |
| Cancellation/timeout before/after persistence | Deterministic SQL payment barriers and component timeouts. Not a live network cable-disconnect test; refund cancellation at every individual await is not exhaustively covered. |
| Restart with pending/failed outbox work | SQL fresh provider/worker recreation. Failed/DeadLetter require explicit operator retry; completed work is not requeued. A literal process crash/restart remains manual. |
| Same/different keys and repeated clicks | Same-key SQL retries, unique constraints, duplicate inventory delivery and component identity tests. A different key denotes another intent; uniqueness alone does not serialize competing partial payments across terminals/processes. |
| Old screens after completion/shift close | Sequential cross-context stale-order, stale-void and closed-shift regressions. A close occurring between validation and commit is a concurrency boundary not fixed by this check. |
| Failed scope reuse/disposal | Failed tracked work is discarded and subsequent real commands persist only their own changes; actual scopes/contexts are disposed in SQL tests. This is not certification of every service or every bounded context after arbitrary failure. |
| Reports, voids, refunds and tender totals | Existing application reconciliation/tender/day-close tests pass; payment list refresh now avoids stale void flags. The combined SQL-backed UI checklist below remains manual. |

Important limits:
- The independently edited transaction-serialization/rounding work is not imported or committed here. Cross-process/multi-terminal payment/customer-balance races and shift-close-versus-commit races need that separate work and SQL concurrency qualification. Do not treat this branch as concurrency acceptance.
- Single-screen retained payment identity is not durable across application restart. After an uncertain result and restart, inspect authoritative order/payment/refund history before collecting money. Distinct keys cannot identify duplicate human intent.
- Physical receipt delivery is not exactly-once proof: a printer can accept a job before acknowledgement is lost. Reprint only the existing document.
- No claim of all-or-nothing cross-context inventory delivery or rollback of external card capture is made. Durable outbox retries and correlation provide recoverable asynchronous effects.
- No live UI, Designer, commissioning, VM, Sandbox, host configuration, production SQL or external-service access occurred.
- Frozen candidate installer, commissioned backup and original backup 06 SHA-256 values still match; protected-hashes-final.json records all three.

## Manual checklist — isolated synthetic database only

Use the isolated checkout above. Start a new synthetic business day/shift with cash float 0, one tracked SKU RECOVERY-100, stock 10 units, price 100.00, tax 0%, no discounts/service charges and no customer credit. Exclude its setup opening-stock movement when counting operational movements. Do not use production data.

1. Order A: quantity 2, total 200.00. Tender cash 250.00: applied payment 200.00, change 50.00, balance 0, exactly one payment. With printer unavailable, complete it. Expected: completed order persists, stock eventually 8, one Issue of 2 units. Repeated click/reprint must not add a payment or stock movement. If completion fails, the payment remains saved and the Open order is completed after refresh without collecting again.
2. Refund A: return 1 unit, Restock, reason RECOVERY-RETURN, cash payout 100.00. Interrupt printing. Expected: one refund/credit note, original payment remains 200.00, original completed receipt/lines remain unchanged, stock eventually 9, one Receipt of 1. Reopen history and reprint the existing refund. No second payout/refund/movement.
3. Order B: quantity 1, total 100.00. Record cash 40.00 while Open, void that payment using the authorised workflow, then pay 100.00 and complete. Expected: two B payment rows, original 40.00 row retained as voided, one active 100.00 payment, stock eventually 8, one additional Issue of 1.
4. Reconcile A+B: gross completed sales 300.00; cash refunds 100.00; net sales/net cash tender 200.00; taxes 0.00; customer ledger balance 0.00. Two completed orders, three payment rows total (two active, one voided), one refund, three operational stock movements, final stock 8. Tendered 250.00 is not revenue; the 50.00 change is not another payment.
5. Keep an older order/shift screen open, complete the order or close its shift elsewhere, then submit from the old screen. Expected rejection and refresh; counts, net cash 200.00 and stock 8 remain unchanged. This manual sequential check does not qualify simultaneous terminals.
6. In a controlled test-only fault harness, interrupt the response at the persistence barrier. Reconcile history before retry. Same key/payload returns the original payment/refund; changed payload conflicts; a distinct full-payment key after settlement is rejected. Never intentionally cut production connectivity.
7. Stop/restart the test application with due receipt/outbox work pending. Confirm existing IDs are recovered, completed records are not replayed, Failed/DeadLetter items remain visible for an explicit retry decision, and all financial counts/balances above stay unchanged.

Capture exact order/refund/payment IDs, attempt keys where exposed, screenshots and report exports. Do not mark these checks PASS until actually performed.

## Final status

Focused recovery defects are reproduced and fixed with passing automated tests and builds. SQL Server rollback and retry proof is limited to the described isolated cases. Live UI, physical printing, literal crash/restart and remaining concurrency boundaries are not qualified.