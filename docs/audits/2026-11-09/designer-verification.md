> Historical checkpoint. See [current code-only status and manual checks](code-only-continuation.md) and the coverage CSV for current dispositions.

# Designer continuation — verified results

Continuation on `audit/structure-designer-20261109`, starting at `804fcf8aeffd104e96f106c502b79a14f06ed5d5`. Changes remain in this audit worktree for review. This report supersedes the earlier blocked Designer disposition and resume notes, without rewriting their historical execution results.

## Result and scope

CustomerEditForm, TaxSummaryReportView and RefundAuditReportView were actually reopened in Visual Studio after their source fixes. All three now render their intended baseline controls. CustomerEditForm also passed one actual Designer resize: the four columns, Notes editor and inherited buttons remained visible; only that inspection resize was undone.

The complete [coverage table](designer-coverage.csv) covers all 141 inventoried UI types: **10 PASS, 2 FAIL, 129 NOT VERIFIED**. Thirteen screens were attempted; the Brand capture remains NOT VERIFIED for full fidelity, and 128 other screens were not inspected. PASS means the explicitly recorded baseline Designer rendering scope, not application-wide Designer compatibility, a save/round-trip guarantee, multi-monitor DPI qualification, runtime acceptance or a Batch 2 pass.

| Screen | Result | Actual evidence and limits |
|---|---|---|
| CustomerEditForm | PASS | [Final four-column rendering](D:/CBOS/Artifacts/designer-diagnosis-20261109/customer-owned-final-rendered.png), [actual resize](D:/CBOS/Artifacts/designer-diagnosis-20261109/customer-owned-resized.png). Notes, credit settings and inherited buttons visible. |
| Tax Summary | PASS | [Fresh-instance rendering](D:/CBOS/Artifacts/designer-diagnosis-20261109/tax-final-fresh-designer.png): filters, Generate button, six KPIs and eight columns. |
| Refund Audit | PASS | [Fresh-instance rendering](D:/CBOS/Artifacts/designer-diagnosis-20261109/refund-final-fresh-designer.png): filters, Generate button, six KPIs and ten columns. |
| BranchEditForm | PASS | [Prior actual runtime-remediation retry](D:/CBOS/Artifacts/designer-diagnosis-20261109/branch-rendered.png): six fields and inherited buttons. |
| MasterDataEditFormBase | PASS | [Prior rendered base](D:/CBOS/Artifacts/designer-diagnosis-20261109/base-rendered.png): content and button strip. |
| BranchManagementView | FAIL | [Designer-command attempt](D:/CBOS/Artifacts/designer-diagnosis-20261109/branch-management-designer-attempt.png) remains code view. Explicit code-only designation, DI-only constructor and procedural/generic layout do not provide an editable Designer. Runtime view was not launched. |
| BrandEditForm / Catalog | NOT VERIFIED | [Partial rendering](D:/CBOS/Artifacts/designer-diagnosis-20261109/catalog-brand-rendered.png); complete label/layout fidelity not established. |
| RoleEditForm / Identity | PASS | [Full inherited form](D:/CBOS/Artifacts/designer-diagnosis-20261109/identity-role-full.png): Name, permissions and buttons. |
| WarehouseStockEditForm / Inventory | PASS | [Rendered form](D:/CBOS/Artifacts/designer-diagnosis-20261109/inventory-stock-rendered.png): stock fields, variant editor and buttons. Resize/DPI not inspected. |
| DatabaseConnectionDialog / Configuration | PASS | [Rendered dialog](D:/CBOS/Artifacts/designer-diagnosis-20261109/config-database-rendered.png). No Test Connection or Save action executed. |
| FirstRunWizardForm / Commissioning | FAIL | [Captured layout](D:/CBOS/Artifacts/designer-diagnosis-20261109/commissioning-wizard-rendered.png) loads but shows clipping in labels/body/button captions. Other pages/workflow not executed. |
| DashboardView | PASS | [Rendered sample cards](D:/CBOS/Artifacts/designer-diagnosis-20261109/dashboard-designer.png). Full viewport/runtime workflow not verified. |
| ManagerAuthorizationForm / Shared | PASS | [Designer-only rendering](D:/CBOS/Artifacts/designer-diagnosis-20261109/shared-manager-designer.png). No authentication or authorization action executed. |

The reports' Generate button was clipped at an earlier, narrower Designer viewport and visible at the final expanded viewport. Isolated constructor/layout checks pass at widths 800, 1000 and 1280, but a complete interactive narrow-width and DPI matrix is still NOT VERIFIED. No runtime sizing change was introduced to conceal that distinction.

## Causes established before remediation

1. **Runtime launch:** the project, actual shadow-cache server runtimeconfig and fresh full-build runtimeconfig requested x64 Microsoft.NETCore.App and Microsoft.WindowsDesktop.App 10.0.13. Native host tracing rejected installed 10.0.12. Tests requested 10.0.0 and selected 10.0.12 instead, explaining the earlier passing constructor tests. The approved installation succeeded with exit code 0 and no restart; both x64 10.0.13 runtimes were verified. This continuation did not reinstall them or change targets/roll-forward.
2. **Customer parser:** the actual error names `CustomerEditForm.Designer.cs`, original line 96, at the loop adding 13 row styles. The [readable expanded stack](D:/CBOS/Artifacts/designer-diagnosis-20261109/customer-loop-error-full.txt) reaches Roslyn CodeDom `Reader.AddStatement`; the [original HTML](D:/CBOS/Artifacts/designer-diagnosis-20261109/customer-loop-error-original.html) preserves the raw report. This reproduces the ForStatement error class; the old unattributed warning alone does not prove which file it concerned.
3. **Customer inheritance:** removing the loop exposed a second real problem: the inherited two-column table rendered instead of the intended four-column layout, clipping lower controls. Microsoft documents that an inherited TableLayoutPanel is locked and does not support visual inheritance in the Designer. A derived-owned table fixed the observed rendering while retaining the same runtime panel contract for base helpers. [Microsoft guidance](https://learn.microsoft.com/en-us/dotnet/desktop/winforms/controls/best-practices-for-the-tablelayoutpanel-control).
4. **Report serialization:** both reports actually opened blank. Their base type is XtraUserControl, with parameterless constructors and Load guards. Runtime constructors built a complete tree, but controls were instantiated in field initializers and their layout/grid setup lived behind private helper calls outside the Designer's serialized initialization. The guard did not cause the blank surface. Inlining serialized construction/layout/grid definitions changed the actual Designer result from blank to populated while preserving constructor layout tests.

Earlier runtime/SDK provenance and compatibility links remain in [the diagnostic record](designer-blocker-diagnosis.md). Microsoft [Windows runtime installation guidance](https://learn.microsoft.com/en-us/dotnet/core/install/windows) and [SDK/Visual Studio compatibility guidance](https://learn.microsoft.com/en-us/dotnet/core/porting/versioning-sdk-msbuild-vs) were used; no SDK/IDE upgrade or roll-forward workaround was performed.

## Implementation and behavior preservation

- `CustomerEditForm.Designer.cs`: thirteen explicit row-style additions and explicit row coordinates replace the loop/counter. A local `_customerLayout` owns the same controls, styles, spans and geometry, with the same four-column arrangement.
- `CustomerEditForm.cs`: both constructors adopt that populated table as the existing protected `_contentPanel`, preserve its runtime name and dispose the now-empty inherited panel. Existing base sizing/localization hooks therefore operate on the populated panel. Inherited buttons, customer values, validation, credit behavior and events remain in place. The shared base and other derived forms were not changed.
- Both report Designer files: explicit field construction, flow layout properties, KPI assignments and grid-column definitions are serialized in InitializeComponent. Grid field names, captions, ordering, widths and formats were retained.
- Both report code-behind files: existing event behavior is still wired once in both constructor paths after layout creation. Appearance subscriptions remain paired with disposal. Load guards, queries, permissions and financial calculations are unchanged.
- Regression files cover unsupported initialization, local layout ownership/base-helper alias, populated report hierarchy, column counts/read-only behavior, custom-date event wiring, three widths and repeated disposal. These tests do not replace actual Designer evidence.

## Executed validation

All logs/TRX are under `D:\CBOS\Artifacts\designer-diagnosis-20261109`.

| Validation | Result | Evidence |
|---|---|---|
| Initial serialization regressions | 3 failed as expected before fixes | `serialization-before-resume.log` / `.trx` |
| Customer owned-layout regression | 1 failed as expected before local ownership fix | `customer-owned-before.log` / `.trx` |
| Final focused Desktop suite | 29 passed, 0 failed/skipped | `designer-owned-final.log` / `.trx` |
| Related isolated Desktop regressions | 18 passed, 0 failed/skipped | `designer-related-regression.log` / `.trx` |
| Final full Debug solution build | 0 warnings, 0 errors | `owned-debug-final.log` |
| Final full Release solution build | 0 warnings, 0 errors | `owned-release-final.log` |

The 47 final test cases comprise the recorded focused serialization/customer/report classes plus related edit-form layout, serialized mediator, master-data list, entity picker and formatting regressions. No operational SQL tests or live application workflow was run. Earlier 838-test audit results are historical, not a newly repeated execution.

## Preserved state and remaining gaps

Only the two expressly approved stale generated report buffers were discarded. All other existing Visual Studio buffers were preserved. Fresh instance PID 26304 / window 6820416 was used to read verified source from disk; the earlier audit instance PID 31412 / window 1639508 still has older unsaved generated buffers. **Do not Save All in that earlier instance onto these fixes.** No broad close/discard was performed. A window save prompt encountered during workspace adjustment was cancelled.

Designer-generated empty `.resx` files for the two reports, commissioning wizard, database configuration dialog and shared manager dialog remain available as untracked generated artifacts; no existing resource file was deleted. Generated Designer resources and commissioning/dashboard sample state are **harness assistance**, not production data, commissioning acceptance or a production-ready claim.

The frozen candidate installer, commissioned backup and original backup 06 all match recorded SHA-256 values in `protected-hashes-final.json`. No merge, push, packaging, production SQL access, VM operation, host CBOS configuration change, privilege change or security/network change occurred.

Remaining work is explicit in the coverage CSV: BranchManagementView requires a separate designable composition path if editable Designer support is required; changing its category alone would expose unsupported initialization and service construction. The commissioning wizard's captured clipping needs focused follow-up. Brand fidelity and the other 128 screens remain unverified. Full edit/save round trips, interactive DPI changes, all-screen resizing and runtime UI acceptance have not been established. For these remaining issues, **no supported repair established by this research** is the appropriate statement; no claim that a repair cannot exist is made.
