# Structure, lifecycle and WinForms audit — 2026-11-09

**Current code-only continuation:** [repairs, validation and manual checklist](code-only-continuation.md), [142-type coverage table](designer-coverage.csv). 54 focused tests pass; Debug/Release builds have zero warnings/errors. Current coverage is four unchanged historical rendering PASS and 138 NOT VERIFIED. All changed screens and all edit/save/reopen checks remain NOT VERIFIED. [Earlier rendering results](designer-verification.md) are historical and do not qualify the current source.

This audit changes source on an isolated branch. It does **not** requalify or modify frozen candidate `9ccaa17`, its installer, backups or historical acceptance results. No VM/Sandbox operation, operational SQL access, host feature/network/security change, packaging, publishing, push or merge was performed.

## Baseline and scope

- Intended integration source: `D:\CBOS\Worktrees\cbos-unified-candidate`, branch `integration/cbos-1.3.0-unified-candidate`, **6ca6f9c5807437666955f69a03b64d453456498f**, clean at inspection. The frozen candidate is **9ccaa17ebd35edd58a3daa280df64d5ed16464ce**, not current integration HEAD.
- Primary repository: `D:\CBOS\Repository`, branch `fix/refund-allocation-and-ui-acceptance`, **805c6ce1d3b9de5d1da483ef982b4585b48f5c8a**. Not used as the source revision and not edited.
- Audit checkout: `D:\CBOS\Worktrees\cbos-structure-designer-audit`, branch `audit/structure-designer-20261109`, created from the integration SHA. Existing dirty work in `cbos-integration` and `cbos-recovery-drill` was preserved, not copied into the audit.
- Full baseline worktree/status snapshot, exact commands/results, build logs, TRX and protected hashes: `D:\CBOS\Artifacts\structure-designer-audit-20261109`. Final SHA/status is recorded there after commits; it cannot be embedded self-referentially in its own commit.

Read `AGENTS.md` and all six `.agents/rules` files. No nested AGENTS files found. This audit does not change financial rules, persisted contracts, schemas, package/framework versions, public API signatures, authorization or licensing behavior.

## Audit checklist and coverage

| Area | Work performed | Status / limit |
|---|---|---|
| Baseline/isolation | Full SHA, branches, all registered worktree statuses; isolated branch; protected input hashes | Complete |
| Projects/packages | 68 project files, all 42 root-solution entries, frameworks, packages, references; graph has no missing project paths/cycles | STATIC SOURCE CONFIRMED; auxiliary CLI/tool solutions not all built |
| Layers/DI | All six context persistence registrations; Desktop composition and scoped screens; Platform bootstrap; singleton outbox | Scoped DbContexts/schema history confirmed in registrations; resource defects below fixed; no runtime SQL validation |
| Async/disposal/events | Gate/mediator wrappers, screen scopes, reporting disposal, host startup, shell subscriptions, branch-sync/QuickBooks timers and callbacks | Targeted regression coverage; remaining cancellation/lifecycle concerns below |
| Forms/controls | [141-type inventory](ui-inventory.csv): 106 have `.Designer.cs`, 57 same-stem `.resx`, 72 explicitly code-only | Heuristic source inventory, manually reviewed representative findings; resource-free/code-only types are not automatically defects |
| Designer grammar/resources | Namespace/partial pairing, constructors, event bindings, initialization/disposal, UI project metadata/resource inventory; sampled layouts and DPI | 59 `.resx` files parse, no missing ResXFileRef targets; no namespace mismatches flagged; no blanket serialization/DPI guarantee |
| Actual Visual Studio | Opened isolated solution in Visual Studio Enterprise 2026 18.8.2; requested BranchEditForm Designer | **FAIL — environment**, missing 10.0.13 host runtime, followed by named-pipe timeout; no rendered pass |
| Documentation | Current index, setup/testing/structure guidance, deployment/support cross references and historical attribution | Five broken support paths fixed; path-existence scan, not remote URL or anchor certification |
| Verification | Debug/Release builds, focused regression and affected/shared suites | 838 passed, 0 failed, 0 skipped in final selected runs; exclusions explicit below |
| Preservation | Frozen installer/commissioned backup/backup 06 hashes before/after; other checkout states | No protected mutation; no operational launch |

The project/reference/package inventory is [project-inventory.md](project-inventory.md) and [projects.json](projects.json). Root projects target `net10.0` except Desktop and Desktop.Tests (`net10.0-windows`). Evaluated C# language version is **14.0** with SDK **10.0.401**; old “C# 13” tooling statements were corrected to SDK default. DevExpress packages remain pinned at `26.1.4-pre-26179`; EF Core and most Microsoft.Extensions packages at `10.0.10`. Test SDK/runner version differences exist in auxiliary tools but were not upgraded without a demonstrated defect.

## Confirmed findings fixed

| Finding / root cause | Practical impact | Fix and direct evidence |
|---|---|---|
| Platform bootstrap disposed its startup scope but leaked the root host when initialization threw/cancelled | Owned singleton resources remained alive after failed startup | [ApplicationBootstrapper](../../../src/Clovent.Platform/Bootstrap/ApplicationBootstrapper.cs): dispose failed host, async disposal when supported; original success ownership unchanged. Three new regression cases failed before fix, pass afterward |
| TaxSummaryReportView and RefundAuditReportView design constructors set `_scope` null, but disposal dereferenced it | Design instance cleanup threw NullReferenceException | Null-safe scope disposal in both Designer partials. Two STA reflection-construction/disposal tests failed before, pass afterward; repeated disposal covered |
| OutboxProcessor subscribed an anonymous handler to a longer-lived connectivity probe and never removed it | Probe retained disposed processor; later notifications could reach disposed signal resources | Named handler, retained probe and unsubscribe during disposal; synthetic subscriber-count test failed before, passes after. SQLite outbox regressions also pass |
| BranchEditForm.InitializeComponent used loops/implicit iteration unsupported by repository CodeDom rules | Source was not in the supported serializable Designer subset | Expand the existing six rows, six labels and six editors into explicit equivalent assignments; preserve sizes, docking, control identity and order. Grammar guard and existing edit-form layout regressions pass. This is static/test verification, not proof of Designer rendering |
| Current docs/badges treated old version/test totals as current verification; reference diagram pointed outward; setup called tracked development config ignored | Misleading readiness/dependency guidance and risk of committing secrets | Current-version/index clarification, corrected dependency direction and config status, actual test-evidence instructions, missing Designer runtime prerequisite; historical candidate reports untouched |
| Five cross references in nested customer support runbook used wrong relative depth | Support navigation failed | Correct relative paths to operations, PDR, database and troubleshooting documents |

Failure-before logs/TRX: `report-lifecycle-before` (2 failures), `bootstrap-before` (3), `outbox-lifecycle-before` (1). The file named `branch-grammar-before` completed after the source edit and **is not a red-before proof**; baseline loop presence is source-confirmed. No passing counts are inferred from compilation.

## Architecture and implementation observations

- Domain projects reference shared/domain contexts rather than Desktop/Infrastructure. Infrastructure references its Domain/Application layers and Platform. Desktop references infrastructure at composition, which is expected. MasterData.Application and Restaurant.Application also reference Platform; Platform currently combines abstractions with EF/host infrastructure. This is an existing coupling, not a dependency cycle, and was documented rather than rewritten.
- Persistence registration uses scoped contexts and schema-specific migration history for Authentication, Identity, MasterData, Catalog, Inventory and Restaurant. SerializedMediator and feature-policy wrappers share a per-screen gate where wired. These registrations do not prove all UI calls are concurrency-safe.
- Existing code-only views deliberately compose controls through shared helpers. Missing parameterless constructors or `.resx` files in those types are not automatically Designer defects. Converting them to editable CodeDom layout would be a separate, behavior-preserving authoring change; hiding controls or adding placeholders is not an acceptable repair.
- Historical `.backup-*` source copies and scaffold files remain tracked but are not normal `.cs` compilation inputs. They were not deleted as “duplicates”; historical evidence preservation takes precedence over cosmetic cleanup.
- Program startup, configuration precedence, file logging and error handling were inspected. No application entry point was executed, because that could contact or modify host operational configuration. Synthetic startup tests and constructor tests are separate evidence.

## Remaining defects, risks and decisions

1. **Designer prerequisite (blocking live inspection):** `DesignToolsServer.exe` requested `Microsoft.NETCore.App 10.0.13 (x64)`; installed shared runtimes include 10.0.12, not 10.0.13. The actual dialog is `designer-runtime-blocker.png`. After declining download, the Designer error page said **“Timed out while connecting to named pipe.”**, recorded in `designer-load-error.txt`. A self-contained restore/build does not satisfy the shared Designer-host runtime. No runtime installation or downgrade of the pin was attempted. Installing the matching developer runtime is a separate workstation-owner decision.
2. **Procedural Designer files remain:** `MenuItemEditForm.Designer.cs` field/object initializers, `ActivityLogView.Designer.cs:72` local object initializer, and Products/Users/Roles/MenuItems `InitializeComponent` calls to `CommandPanelLayout` are outside the documented serializable subset. Tax/refund report Designer methods call `BuildLayout`/`WireEvents` (e.g. TaxSummaryReportView.Designer.cs:72). Their lifecycle crash is fixed; full CodeDom conversion/rendering remains open. They were not relabeled code-only merely to conceal a failed Designer gate.
3. **Cancellation/drain consistency:** `WarehouseStockManagementView.cs:55` disposes its scope directly, whereas ProductPriceManagementView has explicit cancellation/draining. Closing a screen during a pending operation needs controlled runtime/isolated-provider testing before copying a complex lifetime pattern across screens. Existing ScreenOperationGate disposal while waiters exist also merits a dedicated stress test. No claim that these paths were runtime-safe is made.
4. **Monitor observability/concurrency:** BranchSyncStatusControl.LoadMetricsAsync and QuickBooksSyncLogControl.LoadDataAsync use `async void` refresh and broad background catches. Timer/event overlap and stale update ordering remain review concerns; no exceptions were swallowed or workflows removed by this audit. A follow-up should add cancellable Task-returning refresh paths and structured logging with regression tests.
5. **Test isolation debt:** `LayoutTreeDiagnosticTests` writes absolute `D:\Clovent Business Operating System\scratch` paths; some SQL suites create LocalDB targets directly. Unrestricted Desktop/full-solution tests were intentionally not executed under this task's operational boundaries. These fixtures need temporary paths/explicit opt-in before a protected-workstation blanket run.
6. **Rendering/layout:** clipping, resizing, DPI transitions, tab order, DevExpress editor serialization and shared inherited layouts remain live checks, not source-certified results. No financial/security/schema/deployment change was proposed as a workaround.

## Screen-level live coverage

Every inventoried screen has PASS/FAIL/NOT VERIFIED in `ui-inventory.csv`. Zero rendered PASS claims.

| Representative area / screen | Static review | Actual Designer result |
|---|---|---|
| Shared chrome: BaseForm, MasterDataEditFormBase, MasterDataListView | Constructors, inherited layout, localization, busy overlay/disposal | NOT VERIFIED — shared runtime prerequisite |
| Master data: BranchEditForm | Partial/resource pairing, loop fix, field layout | **FAIL (environment)** — Designer entry attempted; host missing runtime, no surface |
| Identity: LoginForm, UsersForm, RolesForm | DI guard, control construction, layout helpers | NOT VERIFIED |
| Catalog: ProductsForm, ProductPriceManagementView, EntityPicker | Composition, gate/scope ownership, disposal and helpers | NOT VERIFIED |
| Inventory: WarehouseStockManagementView, transactions/transfers | Scope/event/lookup ownership and layouts | NOT VERIFIED |
| Restaurant: MenuItemEditForm, RestaurantPosForm, template/rule controls | Constructors, control/resources, initialization and command layout | NOT VERIFIED |
| Reports: TaxSummaryReportView, RefundAuditReportView | Disposal defect reproduced/fixed; filter/DPI tests | NOT VERIFIED — constructor/disposal tests are not Designer rendering |
| Setup/printing: FirstRunWizardForm, PrinterSettingsForm | Static initialization, event/DPI patterns; no provisioning/printing executed | NOT VERIFIED |
| Operations: BranchSyncStatusControl, QuickBooksSyncLogControl | Timers, async callbacks, subscriptions/disposal | NOT VERIFIED |

No representative claims have been imported from older candidate acceptance evidence. Reopen affected/representative Designers after the missing runtime is resolved, starting with BranchEditForm and the two report views and expanding shared-base/helper coverage. Actual attempt in this session was preserved; a post-fix surface pass was impossible with the same missing prerequisite.

## Verification and exact evidence

All commands below ran in the isolated audit checkout. Full argument arrays/exit codes are in `*-result.json`; TRX in the external evidence `tests` directory.

| Execution | Exit | Result / log |
|---|---:|---|
| `dotnet build Clovent.BusinessOperatingSystem.slnx -c Debug --nologo -v:minimal` (baseline) | 0 | 0 warnings/errors; baseline-debug.log |
| `dotnet build Clovent.BusinessOperatingSystem.slnx -c Debug --no-restore --nologo -v:minimal` | 0 | 0 warnings/errors; final-debug.log |
| `dotnet build Clovent.BusinessOperatingSystem.slnx -c Release --no-restore --nologo -v:minimal` | 0 | 0 warnings/errors; final-release.log |
| Desktop focused filter, `-c Debug --no-build --no-restore` | 0 | 31 passed; desktop-focused.log / TRX |
| Platform.Tests, `-c Debug --no-build --no-restore` | 0 | 125 passed; platform-regression.log / TRX |
| Restaurant.Infrastructure.Tests filter `OutboxProcessorLifecycleTests\|OutboxAtomicityIntegrationTests` | 0 | 10 passed; outbox-regression.log / TRX (synthetic/SQLite, not SQL Server) |
| Domain.Tests, `-c Debug --no-build --no-restore` | 0 | 15 passed; Clovent.Domain.Tests.log / TRX |
| Restaurant.Tests, same options | 0 | 202 passed; Clovent.Restaurant.Tests.log / TRX |
| Restaurant.Application.Tests, same options | 0 | 455 passed; Clovent.Restaurant.Application.Tests.log / TRX (fakes/synthetic benchmarks) |

Desktop filter: `FullyQualifiedName~ReportDesignerLifecycleTests|FullyQualifiedName~DesignerSafetyTests|FullyQualifiedName~EditFormLayoutStructureTests|FullyQualifiedName~TaxSummaryReportViewTests|FullyQualifiedName~SerializedMediatorConcurrencyTests|FullyQualifiedName~MasterDataListViewConcurrencyTests|FullyQualifiedName~EntityPickerConcurrencyTests|FullyQualifiedName~BusinessFormattersTests`. Each run used `--logger trx;LogFileName=<run>.trx --results-directory D:\CBOS\Artifacts\structure-designer-audit-20261109\tests` (logger argument quoted in shell).

`dotnet --info` in restricted execution initially failed while querying Service Control Manager (Access Denied); supported host execution was used for builds/tests. This was not a compilation defect or a request to change privileges. No persistent membership/feature change was made.

**Evidence vocabulary:** AUTOMATED TEST VALIDATED for the selected 838 final test cases; STATIC SOURCE CONFIRMED for inventories/reviews. **LIVE UI NOT EXECUTED** for the CBOS application. **VISUAL STUDIO DESIGNER UI NOT EXECUTED** as a completed render/edit validation: Visual Studio/Designer entry was actually attempted but blocked as recorded. **REAL SQL SERVER NOT VALIDATED**; clean-machine acceptance and external integrations not executed.

## Final disposition

Four focused defect classes fixed with regression evidence and documentation corrected. No broad restructuring or upgrades. The audit does not assert that every identified legacy Designer/async concern is fixed. Remaining live and isolation gates are explicit. Changes may be committed on this audit branch; do not merge, push, package or publish without further instruction.
