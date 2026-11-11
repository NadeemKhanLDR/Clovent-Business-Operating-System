# Transaction integration review — 2026-11-11

## Verified history and preserved work

New integration-review branch: audit/transaction-integration-review-20261111, checkout D:\CBOS\Worktrees\cbos-transaction-integration-review.

| Object | Full SHA | Parent |
|---|---|---|
| Recovery implementation 46b4256 | 46b42568aff410d695ee72977609313c3ec5da67 | 8c6fbb07e96254f9ae01cfdd8c2552d2b3e4b74e |
| Recovery report 11063d7 | 11063d7708a5ce429c476a590a03712634a4ef52 | 46b42568aff410d695ee72977609313c3ec5da67 |
| Other committed transaction audit 7aad813 | 7aad81392171f327d8152f69e864c038c90dc5d5 | 8c6fbb07e96254f9ae01cfdd8c2552d2b3e4b74e |

Common ancestor is 8c6fbb07e96254f9ae01cfdd8c2552d2b3e4b74e. Recovery's checkout was clean initially. The other checkout already contained unrelated Designer/screenshot changes, untracked resources and earlier recovery working copies; they were not used as an integration source. Both original branch tips and checkout status lists remain unchanged.

Additional hardening from the follow-up pass is now integrated and verified directly on `audit/transaction-integration-review-20261111`: ordered hashing in all five Restaurant `ValueComparer`s (`ValueConverters.cs` + `ValueComparerContractTests.cs`), hardened single-use `PipelineLockLease` + sequential `ExecuteNestedRequestAsync` with canonical hierarchy enforcement (`TransactionLockManager.cs` + `TransactionSerializationBehavior.cs`), direct-handler/pipeline parity across all 10 commands (`RecordPaymentCommand`, `VoidPaymentCommand`, `VoidOrderCommand`, `ProcessRefundCommand`, `RecordBulkCustomerPaymentsCommand`), unchanged tracked-entity reload guards in `ShiftRepository` and `TableRepository`, and Desktop layout/provisioner test fixes (`FirstRunWizardForm.Designer.cs`, `FirstRunWizardLayoutTests.cs`). The original branches, `separate-inflight-layer.patch` artifact, and unrelated Designer edits remain untouched.

Final integration and hardening live exclusively on `audit/transaction-integration-review-20261111` (merge commit `b366d0d0e4073f55480551607cb69fe8c1dea35d` -> `4d85c61` -> current HEAD) while preserving both original branches and worktrees unchanged.

## Findings and conflict decisions

| Finding | Combined behavior / resolution | Evidence |
|---|---|---|
| Branches contain complementary fixes | Keep recovery's failed-state cleanup, authoritative reads and truthful receipt/retry outcomes; keep 7aad813's commit-spanning process locks, refund rounding/validation, outbox fail-closed guards and completed-payment UI restrictions | Both histories retained; no original branch rewritten |
| Shift rejection conflicts | Retain typed PaymentRequiresOpenShift so the UI distinguishes a known rejection from uncertain acknowledgement. Keep the other branch's serialization. Update the exact concurrency-test exception and assert zero saved payments | Combined infrastructure suite |
| Refund dialog conflicts | Retain protected receipt presentation and lost-response recovery together with the other branch's gross-residual calculation and accumulated gross data | Combined Desktop/domain tests |
| Read-only payment snapshots break order void persistence | Real-pipeline regression reproduced a Voided order whose payment remained active. Re-fetch each payment as tracked before mutating it; preserve its audit row | void-merge-before fails; final regression passes |
| Recovery omitted gross-history data needed by new rounding logic | Regression reproduced AlreadyReversedGross = 0 instead of persisted 500. Refresh gross history alongside quantity, discount and tax | refund-gross-merge-before fails; final regression passes |
| Process-local locks do not protect other terminals | Two OS processes reproduced two 500 payments against a 500 order; a same-key request also failed acknowledgement recovery. Add database-scoped, canonical Shift/Customer/Order SQL session locks held through handler and commit | cross-process-before fails twice; final four-process scenarios pass |
| A new coordinator loses a pending partial-payment key | Reproduced a duplicate partial payment after recreating the coordinator. Persist the immutable attempt before sending, reload the same key, and clear only after acknowledged persistence and refresh | restart-before fails; durable-journal tests pass |
| Old SQL evidence did not prove cleanup | Three fixture teardowns suppressed deletion errors. Add strict target checks, actual provider/database identity checks, asserted deletion and a parameterized DB_ID absence check | All 31 final SQL identities pair with verified cleanup |

Three paths overlap between the two committed branches: RefundOrderDialog.cs, RecordPaymentCommand.cs and OrderLifecycleIntegrationTests.cs. The allocation-repository fixture registration is identical and retained once. Two textual conflicts were resolved by behavior at their conflicting hunks, not by choosing entire files.

## Architectural decisions and implementation

SQL coordination is injected through ITransactionLockProvider into the committed TransactionSerializationBehavior. SQL Server uses bounded session-owned application locks in canonical Shift -> Customer -> Order order. The provider holds them across UnitOfWork SaveChanges, normalizes cancellation during lock acquisition, releases in reverse order, and retires a connection if lock-release cleanup fails without converting a known commit into a reported financial failure. SQLite retains the existing in-process behavior. Customer discovery is rechecked under locks; repeated changes fail closed rather than entering an unlocked handler.

Four independent-process cases use actual command handlers, MediatR behaviors, EF Core and disposable SQL Server databases:
1. Same order, different full-payment keys: one 500 payment; the other attempt rejects.
2. Same key: both responses identify the one committed payment.
3. Different orders, same customer and 500 credit limit: one 500 payment/ledger entry; second credit sale rejects.
4. Shift close paused at SaveChanges versus another process's payment: shift closes; zero payments enter the closed shift.

The new local journal is version 1, DPAPI CurrentUser protected, scoped by terminal/cashier/order, atomically written and flushed before submission, and coordinated by an exclusive file lock. It contains order/method/amount/shift/key/owner identity, never manager approval or credentials. Unknown/corrupt data and storage failure prevent submission and preserve recovery evidence. A current invocation must supply its own approval. A changed unresolved payload is blocked. The default volatile helper remains for design/component construction; the runtime ordinary RecordPayment path explicitly uses the protected journal.

No SQL schema migration, package upgrade, framework target change or persisted outbox-payload change was introduced. Payment list reads remain authoritative snapshots; mutation paths must deliberately load tracked entities. Completed financial records remain immutable and refunds remain compensating documents.

## Files modified / created

New/hardened implementation files are `ITransactionLockProvider.cs`, `SqlTransactionLockProvider.cs`, `ProtectedPaymentAttemptJournal.cs`, `TransactionLockManager.cs` (single-use mutable-disposal `PipelineLockLease`, zero ambient `AsyncLocal` bypass in `ExecuteWithLocksAsync`, explicit sequential `ExecuteNestedRequestAsync` with canonical `Shift < Customer < Order` violation checks), `TransactionSerializationBehavior.cs` (2-attempt pre-lock/in-lock entity/target recheck loops for `ExecuteForOrderWithCustomerAsync`, `ExecuteForVoidOrderAsync`, and `ExecuteForRefundAsync`), `ValueConverters.cs` (ordered `GetHashCode` across all 5 `HashSet` comparers), and `ShiftRepository.cs` / `TableRepository.cs` (`EntityState.Unchanged` reload guards). `RecordPaymentCommandHandler` retries customer changes on both direct-call attempts and fails closed on repeated target changes matching `TransactionSerializationBehavior`; `VoidPaymentCommand`, `VoidOrderCommand`, and `ProcessRefundCommand` route fallback locking consistently through `ExecuteForRequestAsync`; `RecordBulkCustomerPaymentsCommandHandler` wraps sequential `singleHandler.Handle` calls in `ExecuteNestedRequestAsync`. `FirstRunWizardForm.Designer.cs` adds `Padding(16, 10, 16, 6)` to `prerequisiteLayout` and `detectionLayout` so Step 1 cards preserve edge/header margins without weakening `FirstRunWizardLayoutTests`.

New validation files include `CrossProcessPaymentTests`, `SqlTransactionLockTests`, `SqlTestTargetGuardTests`, `PaymentAttemptJournalTests`, `TransactionLockManagerTests`, `ValueComparerContractTests`, and the bounded `Tools/Clovent.Transaction.TestWorker` executable. SQL fixtures now use `SqlTestDatabaseGuard`. Existing suites gain void/gross-history, `AsyncLocal` parallel-child and post-exit escape, sequential/canonical nested lock, cancellation/rollback cleanup, stale preloaded-entity reload, and direct-handler parity regressions. No unrelated `DashboardView` or `ManagerAuthorizationForm` Designer edits are included. The committed `PaymentHistoryDialog` Designer restrictions from `7aad813` are included intentionally; rendering remains unverified.

## SQL target and cleanup verification

Evidence logged via `CBOS_SQL_TEST_EVIDENCE` during the final `Clovent.Restaurant.Infrastructure.Tests` run:

- Data source is checked before creation: `(localdb)\MSSQLLocalDB` only (`BypassSandbox: true` required because Windows Sandbox blocks host LocalDB registry/named-pipe access, explaining why earlier sandboxed runs reported SQL unavailable).
- Database names must match allowed synthetic prefixes plus a 32-hex GUID; operational names and remote/default server targets reject before opening.
- Windows integrated authentication only; no credentials are recorded.
- Provider is Microsoft.EntityFrameworkCore.SqlServer. SELECT DB_NAME() confirms the actual catalog.
- Reported server version is LocalDB `17.00.4025` (`Microsoft.EntityFrameworkCore.SqlServer`). No SQL Server 2022 / version 16 compatibility or acceptance is claimed or inferred from LocalDB v17.
- Cleanup calls EnsureDeleted and then checks DB_ID(@database) is NULL using the same test identity against master. Cleanup errors are no longer suppressed.
- 31 created records, 31 unique databases, 31 matching cleanup-verified records (`leaked: 0`). Each has exactly one creation and one verified deletion.

The original 26 cases are represented by InventorySaleRefundConsistencyIntegrationTests (14), OutboxClaimConcurrencyIntegrationTests (7), RefundOutboxConcurrencyIntegrationTests (1), SqlPaymentRecoveryTests (3) and SqlRestartRecoveryTests (1). They were rerun with the new guards; the additional cases are four independent-process races (`CrossProcessPaymentTests`) and one SQL cancellation/release case (`SqlTransactionLockTests`).

Historical cleanup for the earlier run cannot be independently reconstructed from its old logs, which omitted database identities and suppressed some teardown errors. This report claims checked cleanup for the new run, not retroactive proof about the old run. No operational database contents were queried.

## Final executed verification

All counts below were executed after the final code edit on this branch:

| Execution | PASS | FAIL | SKIP |
|---|---:|---:|---:|
| Full `Clovent.Restaurant.Infrastructure.Tests` (31 LocalDB + SQLite + EF ValueComparer + pipeline) | 164 | 0 | 0 |
| Full `Clovent.Restaurant.Application.Tests` | 484 | 0 | 0 |
| Full `Clovent.Restaurant.Tests` | 244 | 0 | 0 |
| Full `Clovent.Domain.Tests` | 15 | 0 | 0 |
| Full `Clovent.Inventory.Tests` | 23 | 0 | 0 |
| Full `Clovent.Inventory.Application.Tests` | 30 | 0 | 0 |
| Applicable `Clovent.Desktop.Tests` (after building `Tools\Clovent.Installer.Provisioner`; excluding live-SQL `RuntimeVerificationTests` & `DevelopmentRestaurantReportingSeedLiveSqlServerExecutionTests`) | 1007 | 1 | 7 |
| **Total executed** | **1967** | **1** | **7** |

Desktop failure & skip accounting:
- **1 Failure**: `PrinterSettingsFormTests.CompactDimensions_FitsWithinMdiTabWithoutExcessiveHeight` (`height 570 should be <= 450px`) when executed in the shared `testhost` process after `GlobalTypographyTests` mutates static `DevExpress.XtraEditors.WindowsFormsSettings.DefaultFont` (`Segoe UI >= 10F`, scaling `AutoScaleMode.Font`). Running `PrinterSettingsFormTests` isolated passes `8/8`.
- **7 Explicit Skips**: `QuickOrderDealLiveAcceptanceTests.Execute_Live_Quick_Order_Deal_Acceptance_Suite`, `RecallOrderDialogScreenshotTests` (2), `LayoutTreeDiagnosticTests` (2), `LiveAcceptanceQaRun.Run_Full_POS_Live_Acceptance_Suite`, `CategorySelectionLiveScreenshotsTest.Generate_Live_Category_Selection_Screenshots`.
- **2 Explicit Exclusions**: `RuntimeVerificationTests` and `DevelopmentRestaurantReportingSeedLiveSqlServerExecutionTests` (require isolated live SQL target via `CBOS_TEST_SQL_CONNECTION`).
- **Resolved earlier failures**: `ProvisionerCli_InvalidArguments_FailsBeforeSideEffectsWithExitCode64` (`8/8` pass after building `Tools\Clovent.Installer.Provisioner`), `FirstRunWizardLayoutTests` (`24/24` pass after adding `Padding(16, 10, 16, 6)` to `prerequisiteLayout`/`detectionLayout` and translating label coordinates relative to `GroupControl`), and `LostRefundAcknowledgement_RefreshesHistoryAndReturnsOriginalCommittedRefund` (passes both in `RefundOrderDialogTests` and full Desktop suite).

Full-solution `Debug` (`--no-incremental`) and `Release` (`--no-incremental -p:UseArtifactsOutput=true -p:ArtifactsPath=%TEMP%\cbos-rel-artifacts` due to 0.49 GB free space on `D:\`) builds of `Clovent.BusinessOperatingSystem.slnx` succeeded with `0 Warning(s), 0 Error(s)`. Temporary Release artifacts on `C:\` were removed after verification.

LIVE UI NOT EXECUTED. VISUAL STUDIO DESIGNER UI NOT EXECUTED. Live operational SQL (`CBOS_TEST_SQL_CONNECTION`), SQL Server 2022/v16 qualification, and physical printing remain NOT VERIFIED. No Sandbox, Hyper-V, provisioning, production-data access, main merge, push or packaging.

## Remaining limitations and decisions

**Cross-terminal races:** The four described independent-process cases are REAL LOCALDB (`17.00.4025`) VALIDATED, and request-bound `PipelineLockLease` + `ExecuteNestedRequestAsync` hardening closes inherited `AsyncLocal` parallel-child and post-exit escapes inside the process while preserving canonical lock ordering (`Shift < Customer < Order`). The protocol requires all participating financial clients to use the same database lock convention; older/noncooperating clients, ad-hoc SQL, and sibling `ValueConverters` outside Restaurant (noted in `Clovent.Identity.Infrastructure\Persistence\ValueConverters.cs`) remain out of scope of this branch. Deployment must validate application-lock use under the intended least-privileged runtime identity and target SQL Server engine version in an approved nonproduction environment; this pass used Windows-integrated disposable LocalDB databases and changed no permissions.

**Restart identity:** Ordinary single-payment identity survives recreation of the coordinator/store on the same terminal, cashier and Windows profile, with real protected files. A literal desktop process restart was not performed. This is not certification of split-tender batch recovery, customer collection workflows, another workstation/profile, database restore/recommissioning or machine loss.

**Separate business decisions:** Define explicit operator reconciliation/abandonment of an unresolved attempt when details or shift change; current behavior blocks rather than discarding it. Define split-tender batch/leg identity and partial-batch cancellation semantics before claiming whole-checkout restart recovery. Define retention and portable recovery requirements for journal files. Existing unresolved pre-upgrade attempts require history reconciliation; no old key can be reconstructed automatically.

No schema migration is required for the implemented locks or local journal. A server-wide attempt registry with origin ownership, request fingerprint, status and retention would require a separately reviewed schema/business contract if cross-workstation recovery, legacy-client enforcement or durable multi-leg workflow guarantees are required. No such migration was applied.

## Corrected manual checklist — all NOT VERIFIED

Use a synthetic isolated database, one SKU RECOVERY-100, unit price 100.00, tax 0%, no discounts/service charges, opening stock 10 and shift cash float 0. Exclude the setup opening-stock transaction from operational movement counts.

| Step | Exact action | Expected result |
|---|---|---|
| A | Sell 2 units at 100.00; tender 250.00 cash; return 50.00 change; complete | Sale 200.00, one active payment 200.00, stock 10 -> 8, one Issue of quantity 2 |
| Return A | Refund 1 of those units, Restock, cash payout 100.00, reason RECOVERY-RETURN | One refund 100.00, stock 8 -> 9, one Receipt of quantity 1; original completed sale/payment/receipt remain 200.00 |
| B | Sell 1 unit at 100.00. Record an erroneous 40.00 payment **without receiving cash**, void it, then collect 100.00 and complete | Two B payment rows: voided 40.00 retained, active 100.00; stock 9 -> 8, one Issue of quantity 1 |
| Reconcile | Include completed A+B and Return A | Quantity sold 3, quantity restocked 1, net deduction 2; stock 10 - 2 + 1 - 1 = 8. Gross sales 300.00, refunds 100.00, net sales/net cash 200.00, tax 0.00, customer ledger 0.00. Two completed orders, three payment rows (two active/one voided), one refund, three operational stock movements |
| Printer unavailable | Exercise completion/refund preview and reprint on the existing documents | Clearly committed status or follow-up instruction; no extra payment/refund/movement. Physical paper delivery is not exactly-once proof |
| Old screen | After completing B or closing its shift, submit from the older screen | Reject/refresh, unchanged financial counts and stock. If creating an extra draft to test shift closure, exclude that draft from the completed-order count |
| Restart recovery — separate dataset | Order total 100.00, one 40.00 partial payment; inject lost acknowledgement in a test-only harness, then recreate/restart under the same cashier/profile and retry unchanged | Same attempt key, one payment 40.00, balance 60.00; no inventory Issue until completion. Do not add this dataset to the A+B reconciliation |
| Two test instances | Race different full-payment keys against one 500.00 order | One payment 500.00; other attempt rejects. Reusing the same key returns that same payment ID |

Capture IDs, attempt keys where exposed, balances, stock quantities, row counts, report exports and screenshots. Money totals alone are not evidence of stock movement. No commissioning or production fault injection is part of this checklist.