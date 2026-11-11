# Transaction integration review — 2026-11-11

## Verified history and preserved work

New integration-review branch: audit/transaction-integration-review-20261111, checkout D:\CBOS\Worktrees\cbos-transaction-integration-review.

| Object | Full SHA | Parent |
|---|---|---|
| Recovery implementation 46b4256 | 46b42568aff410d695ee72977609313c3ec5da67 | 8c6fbb07e96254f9ae01cfdd8c2552d2b3e4b74e |
| Recovery report 11063d7 | 11063d7708a5ce429c476a590a03712634a4ef52 | 46b42568aff410d695ee72977609313c3ec5da67 |
| Other committed transaction audit 7aad813 | 7aad81392171f327d8152f69e864c038c90dc5d5 | 8c6fbb07e96254f9ae01cfdd8c2552d2b3e4b74e |

Common ancestor is 8c6fbb07e96254f9ae01cfdd8c2552d2b3e4b74e. Recovery's checkout was clean initially. The other checkout already contained unrelated Designer/screenshot changes, untracked resources and earlier recovery working copies; they were not used as an integration source. Both original branch tips and checkout status lists remain unchanged.

Additional hardening is now integrated and verified directly on `audit/transaction-integration-review-20261111`: ordered hashing in all five Restaurant `ValueComparer`s (`Clovent.Restaurant.Infrastructure/Persistence/ValueConverters.cs` + `ValueComparerContractTests.cs`) and all four sibling Identity `ValueComparer`s (`Clovent.Identity.Infrastructure/Persistence/ValueConverters.cs` -> `RoleIdsComparer`, `PermissionIdsComparer`, `CompanyIdsComparer`, `BranchIdsComparer` + `Clovent.Identity.Infrastructure.Tests/Persistence/ValueComparerContractTests.cs`), hardened single-use `PipelineLockLease` + sequential `ExecuteNestedRequestAsync` with canonical hierarchy enforcement (`TransactionLockManager.cs` + `TransactionSerializationBehavior.cs`), direct-handler/pipeline parity, unchanged tracked-entity reload guards in `ShiftRepository` and `TableRepository`, Continuity replay + pre-settlement order/line/discount/table-merge/business-day lock participation (`TransactionLockScope.BusinessDay`, `TransactionLockScope.Continuity`, `EmergencyJournalReplayer.cs`, `FinancialLockGapRegressionTests.cs`), least-privileged LocalDB `sp_getapplock`/`sp_releaseapplock` verification (`SqlTransactionLockTests.LeastPrivilegedDatabaseUser_ExecutesAppLockContentionAndFinancialDml_WithoutOwnerOrControlPermissions`), and Desktop layout/typography fixes (`FirstRunWizardForm.Designer.cs`, `FirstRunWizardLayoutTests.cs`, `PrinterSettingsForm.Designer.cs`, `PrinterSettingsFormTests.cs`). The original branches, `separate-inflight-layer.patch` artifact, and unrelated Designer edits remain untouched.

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

New/hardened implementation files include `ITransactionLockProvider.cs`, `SqlTransactionLockProvider.cs` (`Shared`/`Exclusive` `sp_getapplock` mode support on the same `CBOS:financial:*` resource), `ProtectedPaymentAttemptJournal.cs`, `TransactionLockManager.cs` (`BusinessDay < Shift < Customer < Table < Order < Continuity` with non-evicting `AsyncLockGate` `Shared`/`Exclusive` coordination, single-use mutable-disposal `PipelineLockLease`, zero ambient `AsyncLocal` bypass in `ExecuteWithLocksAsync`, explicit sequential `ExecuteNestedRequestAsync`), `TransactionSerializationBehavior.cs` (shared 2-attempt recheck across payment/void/refund/line/table-merge/business-day-close writers, Continuity idempotency-key overlap with `RecordPaymentCommand`, `Table` lock coordination across `CreateOrder`/`SplitOrder`/`MergeTables`/`TransferOrderTable`/`CancelOrder`/`ReopenOrder`/`CompleteOrder`/`VoidOrder`), `EmergencyJournalReplayer.cs` & `ApplicationServiceCollectionExtensions.cs` (production DI non-optional `ITransactionLockProvider` + `IOutboxRepository`, `BusinessDay` Shared + `Continuity` locks through `SaveChangesAsync`), `Clovent.Restaurant.Infrastructure/Persistence/ValueConverters.cs` (5 comparers), `Clovent.Identity.Infrastructure/Persistence/ValueConverters.cs` (4 comparers), `ShiftRepository.cs` / `TableRepository.cs`, handler direct-call locking parity (`RecordPaymentCommand`, `VoidPaymentCommand`, `VoidOrderCommand`, `ProcessRefundCommand`, `RecordBulkCustomerPaymentsCommand`, `CancelOrderCommand`, `ApplyDiscountToOrderCommand`, `CloseBusinessDayCommand`, `OpenShiftCommand`), `FirstRunWizardForm.Designer.cs`, and `PrinterSettingsForm.Designer.cs` (`AutoScaleMode = AutoScaleMode.None` per `BaseForm.Designer.cs` PerMonitorV2 rules + widened label-to-editor spacing for production `Segoe UI 10pt`).

New validation files include `CrossProcessPaymentTests`, `SqlTransactionLockTests` (`Shared` vs `Exclusive` LocalDB test + constrained user `cbos_lock_tester`), `SqlTestTargetGuardTests`, `PaymentAttemptJournalTests`, `TransactionLockManagerTests`, `FinancialLockGapRegressionTests` (Continuity vs `RecordPayment`, `CloseBusinessDay` vs `OpenShift` & late financial postings + concurrent `Shared` payments, `SplitOrder` vs moved `OrderLine` recheck, empty-target `MergeTables` vs `CreateOrder`), Restaurant and Identity `ValueComparerContractTests`, `ProductionCompositionGuaranteeTests` (`IEmergencyJournalReplayer` fail-closed DI & null-provider guards), and the bounded `Tools/Clovent.Transaction.TestWorker` executable. No unrelated `DashboardView` or `ManagerAuthorizationForm` Designer edits are included. The committed `PaymentHistoryDialog` Designer restrictions from `7aad813` are included intentionally; rendering remains unverified.

## SQL target and cleanup verification

Evidence logged via `CBOS_SQL_TEST_EVIDENCE` during the final `Clovent.Restaurant.Infrastructure.Tests` run:

- Data source is checked before creation: `(localdb)\MSSQLLocalDB` only (`BypassSandbox: true` required because Windows Sandbox blocks host LocalDB registry/named-pipe access, explaining why earlier sandboxed runs reported SQL unavailable).
- Database names must match allowed synthetic prefixes plus a 32-hex GUID; operational names and remote/default server targets reject before opening.
- Windows integrated authentication only; no credentials are recorded.
- Provider is Microsoft.EntityFrameworkCore.SqlServer. SELECT DB_NAME() confirms the actual catalog.
- Reported server version is LocalDB `17.00.4025` (`Microsoft.EntityFrameworkCore.SqlServer`). No SQL Server 2022 / version 16 compatibility or acceptance is claimed or inferred from LocalDB v17.
- Cleanup calls EnsureDeleted and then checks DB_ID(@database) is NULL using the same test identity against master. Cleanup errors are no longer suppressed.
- 33 created records, 33 unique databases, 33 matching cleanup-verified records (`leaked: 0`). Each has one creation and one verified deletion.
- Least-privileged database-user test (`EXECUTE AS USER = 'cbos_lock_tester' WITHOUT LOGIN`, granted only `SELECT, INSERT, UPDATE, DELETE ON SCHEMA::[Restaurant]` with `IS_ROLEMEMBER('db_owner') = 0`, `IS_SRVROLEMEMBER('sysadmin') = 0`, and `HAS_PERMS_BY_NAME(NULL, 'DATABASE', 'CONTROL') = 0`) executed real `SqlTransactionLockProvider` (`sys.sp_getapplock` / `sys.sp_releaseapplock`) contention, cancellation, and EF `SaveChangesAsync` cleanly on a disposable `CBOS_Recovery_*` LocalDB catalog, followed by `REVERT`, pool clear, and verified DB drop. Zero server-level permissions or non-test catalogs were modified. Actual SQL Server login connectivity and SQL Server 2022/v16 qualification remain NOT VERIFIED.

The original 26 cases are represented by InventorySaleRefundConsistencyIntegrationTests (14), OutboxClaimConcurrencyIntegrationTests (7), RefundOutboxConcurrencyIntegrationTests (1), SqlPaymentRecoveryTests (3) and SqlRestartRecoveryTests (1). They were rerun with the new guards; the additional cases are four independent-process races (`CrossProcessPaymentTests`) and three SQL lock tests (`SqlTransactionLockTests`, including constrained-user impersonation and `Shared`/`Exclusive` verification).

Historical cleanup for the earlier run cannot be independently reconstructed from its old logs, which omitted database identities and suppressed some teardown errors. This report claims checked cleanup for the new run, not retroactive proof about the old run. No operational database contents were queried.

## Final executed verification

All counts below were executed after the final code edit on this branch:

| Execution | PASS | FAIL | SKIP |
|---|---:|---:|---:|
| Full `Clovent.Restaurant.Infrastructure.Tests` (33 LocalDB + SQLite + EF ValueComparer + pipeline) | 166 | 0 | 0 |
| Full `Clovent.Restaurant.Application.Tests` | 491 | 0 | 0 |
| Full `Clovent.Restaurant.Tests` | 244 | 0 | 0 |
| Full `Clovent.Identity.Infrastructure.Tests` (verified at `40f4b1c`; unchanged since) | 34 | 0 | 0 |
| Full `Clovent.Domain.Tests` (verified at `40f4b1c`; unchanged since) | 15 | 0 | 0 |
| Full `Clovent.Inventory.Tests` (verified at `40f4b1c`; unchanged since) | 23 | 0 | 0 |
| Full `Clovent.Inventory.Application.Tests` (verified at `40f4b1c`; unchanged since) | 30 | 0 | 0 |
| Applicable `Clovent.Desktop.Tests` (after building `Tools\Clovent.Installer.Provisioner`; excluding live-SQL `RuntimeVerificationTests` & `DevelopmentRestaurantReportingSeedLiveSqlServerExecutionTests`) | 1011 | 0 | 7 |
| **Total verified** | **2014** | **0** | **7** |

Desktop failure & skip accounting:
- **0 Failures**: `PrinterSettingsFormTests.CompactDimensions_FitsWithinMdiTabWithoutExcessiveHeight` and `CompactDimensions_UnderProductionGlobalTypography_RemainsCompactWithoutOverlaps` pass (`9/9` in `PrinterSettingsFormTests`) inside the shared `testhost` process after changing `PrinterSettingsForm.Designer.cs` to `AutoScaleMode = AutoScaleMode.None` (matching `BaseForm.Designer.cs`) and widening label-to-editor horizontal spacing (`grpInstalled`, `grpProfileDetails`) for `DesktopStyle.ApplyGlobalTypography()` (`Segoe UI 10pt`). Compact-height assertions validate 100%-DPI unscaled form bounds only and **do not qualify high-DPI visual rendering at 150% or 200% scaling**. `xunit.runner.json` serializes assembly/collection execution (`parallelizeAssembly: false`, `parallelizeTestCollections: false`) around static WinForms/DevExpress typography settings.
- **7 Explicit Skips**: `QuickOrderDealLiveAcceptanceTests.Execute_Live_Quick_Order_Deal_Acceptance_Suite`, `RecallOrderDialogScreenshotTests` (2), `LayoutTreeDiagnosticTests` (2), `LiveAcceptanceQaRun.Run_Full_POS_Live_Acceptance_Suite`, `CategorySelectionLiveScreenshotsTest.Generate_Live_Category_Selection_Screenshots`.
- **2 Explicit Exclusions**: `RuntimeVerificationTests` and `DevelopmentRestaurantReportingSeedLiveSqlServerExecutionTests` (require isolated live SQL target via `CBOS_TEST_SQL_CONNECTION`).

Full-solution `Debug` (`--no-incremental`) and `Release` (`--no-incremental -p:UseArtifactsOutput=true -p:ArtifactsPath=%TEMP%\cbos-rel-artifacts`, 14.39s, due to ~0.49 GB free space on `D:\`) builds of `Clovent.BusinessOperatingSystem.slnx` succeeded after the final code edit with `0 Warning(s), 0 Error(s)`. Temporary Release artifacts on `C:\` were removed after verification.

LIVE UI NOT EXECUTED. VISUAL STUDIO DESIGNER UI NOT EXECUTED. Actual SQL Server runtime-login connectivity, live operational SQL (`CBOS_TEST_SQL_CONNECTION`), SQL Server 2022/v16 qualification, high-DPI rendering (150%/200%), and physical printing remain NOT VERIFIED. No Sandbox, Hyper-V, provisioning, production-data access, main merge, push or packaging.

## Remaining limitations and decisions

**Financial writer lock inventory & boundaries:**
- **Participating in `TransactionSerializationBehavior` (`ITransactionLockProvider` + `TransactionLockManager`, canonical order `BusinessDay < Shift < Customer < Table < Order < Continuity`, with `Shared`/`Exclusive` coordination on `BusinessDay`)**: `RecordPaymentCommand` (plus `Continuity` lock for `emergency:` idempotency keys), `VoidPaymentCommand`, `VoidOrderCommand`, `CompleteOrderCommand`, `SetOrderCustomerCommand`, `ProcessRefundCommand`, `CloseShiftCommand`, `RecordCashMovementCommand`, `RecordCustomerPaymentCommand`, `RecordBulkCustomerPaymentsCommand`, `CreateOrderCommand` (when `TableId` is set), pre-settlement order/line/discount/service-charge/table-merge mutators (`CancelOrderCommand`, `HoldOrderCommand`, `ResumeOrderCommand`, `ReopenOrderCommand`, `TransferOrderTableCommand`, `UpdateDeliveryDetailsCommand`, `AddOrderLineCommand`, `RemoveOrderLineCommand`, `ApplyDiscountToOrderCommand`, `RemoveDiscountFromOrderCommand`, `ApplyServiceChargeToOrderCommand`, `RemoveServiceChargeFromOrderCommand`, `SplitOrderCommand`, `SetOrderLineQuantityCommand`, `OverrideOrderLinePriceCommand`, `VoidOrderLineCommand`, `UnvoidOrderLineCommand` with 2-attempt moved-line recheck, `MergeTablesCommand`), and branch lifecycle (`CloseBusinessDayCommand`, `OpenShiftCommand`). `EmergencyJournalReplayer` participates directly via `TransactionLockManager` + required `ITransactionLockProvider` (`BusinessDay` Shared + `Continuity`).
- **Outside `CBOS:financial:*` lock protocol**: `OutboxProcessor` / `InventoryPostingOutboxHandler` use optimistic concurrency claim tokens on `OutboxMessage` plus idempotent inventory reference checks (`ListInventoryTransactionsByReferenceQuery`), and development seed/sync-conflict ingestion paths remain separate. Cross-terminal coordination applies only to clients participating in `CBOS:financial:*`; legacy/non-cooperating clients or ad-hoc SQL remain uncoordinated at the application lock layer. Database-user impersonation (`EXECUTE AS USER = 'cbos_lock_tester' WITHOUT LOGIN`) on disposable LocalDB (`17.00.4025`) verified `sp_getapplock`/`sp_releaseapplock` and minimal schema DML without `db_owner`/`CONTROL`/`sysadmin`; actual runtime-login connectivity and SQL Server 2022/v16 qualification remain unverified and must be validated in an approved nonproduction SQL Server 2022 environment before deployment.

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
| PrinterSettingsForm scaling (100%, 150%, 200% DPI) | Open `PrinterSettingsForm` (MDI document tab and standalone resize) under Windows display scaling at 100%, 150%, and 200%; inspect all labels/editors (`grpInstalled`, `grpProfileDetails`), checkboxes, and action buttons | Readable `Segoe UI 10pt` text, zero label truncation or overlapping controls, bottom action bar (`lblStatus`, `btnClose`) remains visible without clipping when resized down to `MinimumSize` |
| Restart recovery — separate dataset | Order total 100.00, one 40.00 partial payment; inject lost acknowledgement in a test-only harness, then recreate/restart under the same cashier/profile and retry unchanged | Same attempt key, one payment 40.00, balance 60.00; no inventory Issue until completion. Do not add this dataset to the A+B reconciliation |
| Two test instances | Race different full-payment keys against one 500.00 order | One payment 500.00; other attempt rejects. Reusing the same key returns that same payment ID |

Capture IDs, attempt keys where exposed, balances, stock quantities, row counts, report exports and screenshots. Money totals alone are not evidence of stock movement. No commissioning or production fault injection is part of this checklist.

## Combined transaction + reporting integration (`audit/final-transaction-reporting-integration-20261111`)

Integrated reporting commit `7444d2375eeb00c486392613c180bf528d03ce15` (from `audit/transaction-reporting-20261111`, based on `40f4b1c16f6655d853da6ba7a96fc749f9bfc61c`) onto latest transaction hardening `27217fddf9e7adecc15e2cb6502c85f6a47b4bfc` (`audit/transaction-integration-review-20261111`) in a dedicated review branch `audit/final-transaction-reporting-integration-20261111` (cherry-pick `049c1e3` plus reconciliation commit). All source branch tips (`27217fd`, `7444d23`, `99f0659`, `11063d7`, `7aad813`, `main`) and unrelated work remain untouched.

### Reconciliation fixes applied on this branch

1. **`CloseShiftCommand` canonical payment-method classification (`CloseShiftCommand.cs`)**: Replaced legacy hardcoded `"Cash"` / `"Card"` / `"Credit Card"` / `"Debit Card"` string comparisons with `PaymentMethodClassifier.IsCash` and `PaymentMethodClassifier.IsCard` so shift closure reconciles cash/card aliases (e.g., `"MasterCard"`, `"Visa"`) identically to `GetShiftSummaryQuery` and `GetBusinessDaySummaryQuery`. Regression added in `ShiftHandlerTests.CloseShift_UsesCanonicalCardAndCashClassificationMatchingShiftSummaryQuery`.
2. **Offline continuity receipt `TotalTaxableBase` (`EmergencyJournalReplayer.cs`)**: Changed `ReceiptSnapshot.TotalTaxableBase` from `tx.OrderSnapshot.Subtotal` to `MoneyRoundingPolicy.RoundMoney(snapshotItems.Sum(i => i.TaxableBase))` so offline/replayed receipts match `CompleteOrderCommandHandler` and `ReceiptFormatter.FormatFromSnapshot` (`Item calculation base`) on inclusive-tax and discounted orders. Regression added in `TransactionReportingAuditTests.ContinuityReplayTodayForYesterdaysSale_AttributesToFrozenOfflineDateAndTrueCalculationBase`.
3. **Expanded sales summary `NetSales` vs `TotalBillSales` (`GetExpandedSalesSummaryQuery.cs`)**: Restored `SalesSummaryKpiDto.NetSales` to `grossSales - totalDiscounts + totalDeliveryFees + totalServiceCharges` (exclusive of exclusive tax) while retaining `totalBillSales = orderRows.Sum(o => o.Total)` on `SalesSummaryKpiDto.TotalBillSales` and `AverageOrderValue`. Fixed `EndToEndPosWorkflowAcceptanceTests.CategoryInheritedTaxProfile_OrderLineAndExpandedSalesReport_PreservesInheritedAndExemptSnapshots` (`NetSales` 990.00 vs 1134.00) and added `Assert.Equal(225m, expanded.Kpis.NetSales)` to `TransactionReportingAuditTests.MixedDiscountedSale_BillKpiMatchesRowsAndTaxSummary`.

### Fresh post-edit verification (separate from earlier branch runs)

All runs below executed sequentially after the final code edit on `audit/final-transaction-reporting-integration-20261111`:

| Check | PASS | FAIL | SKIP | Notes |
|---|---:|---:|---:|---|
| `Clovent.Domain.Tests` | 15 | 0 | 0 | Full project |
| `Clovent.Identity.Infrastructure.Tests` | 34 | 0 | 0 | Full project (includes 4 Identity `ValueComparer` suites) |
| `Clovent.Inventory.Tests` | 23 | 0 | 0 | Full project |
| `Clovent.Inventory.Application.Tests` | 30 | 0 | 0 | Full project |
| `Clovent.Restaurant.Tests` | 244 | 0 | 0 | Full project |
| `Clovent.Restaurant.Application.Tests` | 505 | 0 | 0 | Full project (13 `TransactionReportingAuditTests`, `ShiftHandlerTests`, lock/continuity tests) |
| `Clovent.Restaurant.Infrastructure.Tests` | 168 | 0 | 0 | Full project; 35 disposable LocalDB `17.00.4025` DBs (`created=35`, `cleanup-verified=35`, `mismatch=0`) |
| `Clovent.Desktop.Tests` (filtered) | 1015 | 0 | 7 | Excludes live-SQL `RuntimeVerificationTests` & `DevelopmentRestaurantReportingSeedLiveSqlServerExecutionTests` |
| **Total tests verified** | **2034** | **0** | **7** | **Zero failures** |

Full-solution `Debug` (`--no-restore`) and `Release` (`-p:UseArtifactsOutput=true -p:ArtifactsPath=%TEMP%\cbos-rel-artifacts`) builds of `Clovent.BusinessOperatingSystem.slnx` succeeded with `0 Warning(s), 0 Error(s)` (temp Release artifacts deleted afterward; a pre-existing October `CBOS_RefundConcurrency_ff95cae36d8844f09587ecbe6c693ea1` LocalDB database from `2026-10-14` predates this session and was left untouched).

### Consolidated manual acceptance checklist — ALL NOT VERIFIED IN LIVE UI

Run only against an isolated synthetic test database (never operational/production DBs). Record Order IDs, Payment IDs, attempt keys, Shift/BusinessDay IDs, stock balances, report exports, and screenshots.

1. **Cash/change, split tenders, and customer balances**
   - Record single cash tender with overpayment change (`tender > order total`): verify `Payment.Amount` and shift expected cash equal net order collection, receipt shows cash tendered and change returned, and shift drawer summary excludes returned change from net cash sales.
   - Record split tender across cash + card alias (e.g., `MasterCard` / `Visa`) + `OnAccount`: verify card alias lands in **Card** (never `Other`) across both `GetShiftSummaryQuery`, `CloseShiftCommand`, `GetBusinessDaySummaryQuery`, and `ExpandedSalesSummary`; verify customer receivable increases strictly by the `OnAccount` amount.
   - Collect a customer advance / credit settlement and test voiding an active advance settlement payment (and whole-order void): verify customer balance, ledger entries, allocations, payment active/voided status, and `ExpandedSalesSummary` (`CustomerPaymentsCollected` / `AdvancesReceived` stay unchanged while liability restores).
   - Attempt a stale-screen submission or concurrent duplicate payment from two test instances against the same order/customer credit limit: verify one succeeds cleanly and the second rejects cleanly without double charge.
2. **Partial refunds, rounding, and inventory reconciliation**
   - Test an inclusive-tax fractional item split across repeated partial returns (`0.5` + `0.5` of a `10.05` item at 10% tax): verify cumulative refunded net, tax, and gross residuals equal original order totals with penny-exact final remainder (`5.03 + 5.02 = 10.05`, tax `0.46 + 0.45 = 0.91`), original completed sale/receipt stays immutable, and credit notes preserve stored residuals.
   - Test restock vs non-restock partial/full refunds: verify on-hand stock increases only on restock returns, net inventory delta matches sold minus restocked quantities, and `Tax Summary`, `Refund Audit`, and `ExpandedSalesSummary` agree.
3. **Restart/retry recovery**
   - Inject lost acknowledgement after recording a partial payment (e.g., `40.00` on `100.00`), restart the desktop app under the same terminal/cashier/Windows profile, and retry unchanged: verify protected DPAPI attempt journal reuses the same attempt key and reconciles the single `40.00` payment (`60.00` balance due, zero duplicate payment, zero premature inventory Issue).
   - Change amount/method/shift while an unresolved attempt exists: verify fail-closed block/reconciliation prompt rather than silently generating a duplicate attempt.
   - Trigger offline continuity capture + emergency journal replay on a later day: verify replayed receipt uses frozen offline completion date and item calculation base.
4. **Sales, tax, and refund reports across dates**
   - Create an order on Day 1, complete it on Day 2, modify operational/delivery notes on Day 3, and refund partially on Day 3: verify Day 1 excludes it, Day 2 attributes the completed sale and frozen receipt timestamp (even after Day 3 operational edits), Day 3 includes only the refund in `Refund Audit` and net tax/sales adjustment, and `NetSales` (ex-exclusive-tax) vs `TotalBillSales` (including exclusive tax) match KPI tiles and row totals.
   - Verify customer statement/receivables "as-of Day 2" excludes Day 3 customer payments for both closing balance and `LastTransactionDate`.
5. **Shift and business-day closure**
   - Open shift, post mixed cash/card-alias payments and a cash refund/movement, and close shift: verify `CloseShiftCommand` expected cash/card/other figures match `GetShiftSummaryQuery` exactly and zero subsequent payments can post to the closed shift.
   - Race business-day closure against shift opening / late payment / continuity replay: verify business-day closure waits for in-flight postings or rejects new postings cleanly once closing/closed.
6. **`PrinterSettingsForm` DPI & layout inspection (100%, 150%, 200% scaling) + preview/reprint**
   - Open `PrinterSettingsForm` (MDI document tab and standalone resize) under Windows display scaling at **100%**, **150%**, and **200%**: verify readable `Segoe UI 10pt` typography, zero label truncation or overlapping controls in `grpInstalled` / `grpProfileDetails`, and that `lblStatus` / `btnClose` remain visible and unclipped down to `MinimumSize`.
   - Exercise completion/refund receipt preview & unavailable-printer handling: verify committed status and no duplicate financial or stock mutations on print failure.

**Explicitly NOT VERIFIED**: Live interactive WinForms UI, Visual Studio WinForms Designer, high-DPI rendering at 150%/200%, physical printing, runtime SQL Server login connectivity (only `EXECUTE AS USER ... WITHOUT LOGIN` on LocalDB `17.00.4025`), and SQL Server 2022/v16 compatibility (LocalDB tested is v17 `17.00.4025`).