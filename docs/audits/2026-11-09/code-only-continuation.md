# Code-only Designer continuation — 2026-11-09

This is the current continuation on audit/structure-designer-20261109 from 804fcf8. No Visual Studio control, document reload, Save All, commissioning, SQL connection or VM operation was performed. No new rendering screenshots were captured. Compilation and hidden-control tests do not establish Visual Studio Designer compatibility.

## Changes and evidence

- BranchEditForm owns its serialized two-column, six-row table. All six labels and six editors have explicit attachments and unit row/column spans. Both constructors adopt that table through the existing base layout field and dispose the unused inherited panel. Inherited OK/Cancel buttons, value loading and validation remain intact.
- BranchManagementView serializes the actual OrganizationHierarchySelector and concrete BranchListView, which reuses the production generic grid/toolbar. Its service-free constructor creates real controls; its runtime constructor binds scoped services and existing delegates afterward. Organization/company filtering, branch authorization, refresh and activate/deactivate routing remain in the runtime path. Controls are disposed before their scope; resolution failure disposes the temporary scope.
- CustomerEditForm retains its owned table and base sizing/localization helpers. Regression checks exercise parent relationships, helper effects, values, single inherited OK event wiring and repeated disposal.
- Tax Summary and Refund Audit retain runtime event wiring and explicit grid definitions. Saved Absolute-width layout and non-growing flow panels caused a reproduced narrow-width containment failure; percent width and growing top-docked flow panels correct it in hidden layout checks.
- FirstRunWizardForm uses serialized native tables for all eight pages, header, sidebar and footer instead of manual pixel positioning. Hidden tests check native composition at two sizes; actual clipping, rendering and high-DPI behavior still require inspection. Commissioning was not executed.
- Eight saved Designer files were structurally reviewed: no unattached declared controls, repeated attachments or duplicate named subscriptions were identified. This static review is not a CodeDom serialization round trip. DashboardView and ManagerAuthorizationForm saved edits remain outside the repair commit.

## Preservation and limitations

Evidence is in D:\CBOS\Artifacts\designer-code-only-20261109: disk-before.patch, untracked-before, disk-before-hashes.json, recovery comparisons, tests, builds, resource-review.json and protected-hashes-final.json. Frozen installer, commissioned backup and original backup 06 hashes match their recorded values.

Read-only DTE enumeration exported the accessible document inventory without saving or closing anything. The older Visual Studio instance was not exposed through the accessible Running Object Table. AutoRecover copies were preserved separately and compared with disk. **AutoRecover copies do not establish a complete export of live unsaved buffers.** No live buffer was discarded or overwritten. Microsoft documents AutoRecover as recovery storage: https://learn.microsoft.com/en-us/visualstudio/ide/autorecover-environment-options-dialog-box?view=visualstudio

Five untracked resource files were inspected individually and contain no data or metadata entries: FirstRunWizardForm.resx, DatabaseConnectionDialog.resx, RefundAuditReportView.resx, TaxSummaryReportView.resx and ManagerAuthorizationForm.resx. They are preserved locally and in the initial snapshot, excluded from the commit as incidental empty serialization artifacts. The already tracked CustomerEditForm.resx is retained. Unrelated saved DashboardView/ManagerAuthorizationForm changes are preserved in the worktree and unrelated-saved-designers.patch.

## Executed validation

- 54 focused and related tests: PASS, zero failures/skips (verified-final.trx).
- Full solution Debug: PASS, zero warnings/errors (debug-final.log).
- Full solution Release: PASS, zero warnings/errors (release-final.log).
- Protected hashes: all three match.
- Initial failing regression runs are retained alongside the final logs. Tests use hidden controls and fake services; they do not qualify database workflows or rendered UI. Existing tests that show the wizard were excluded.

Current inventory contains 142 types: four unchanged historical rendering PASS and 138 NOT VERIFIED. Every changed surface is NOT VERIFIED for current rendering. Designer edit/save/close/reopen and runtime high-DPI qualification remain NOT VERIFIED. Historical observations are retained in separate columns; older screenshots are not evidence for the changed source.

## Manual Visual Studio checklist

Preserve/export your own unrelated live buffers before reloading disk changes. Avoid Save All in the stale instance. For each surface below, capture normal/narrow screenshots and mark PASS/FAIL only after inspecting it.

| Surface | Expected controls and resizing |
|---|---|
| BranchEditForm | Six label/editor pairs: Name, Street, City, State, Postal Code, Country; one owned two-column/six-row table; inherited OK/Cancel. Check approximately 540x520 and 900x600: no missing editors, overlap or clipped buttons. |
| BranchManagementView | Real Organization and Company selectors; actual search and New/Edit/Activate/Deactivate/Refresh toolbar; grid columns Name, City, Country, Status, Created. Check 1000x600 and narrower: selector above grid, usable toolbar and scrollbars. No runtime data/service execution is needed to inspect composition. |
| CustomerEditForm | All 13 editors, four-column layout, section headings spanning four columns, Notes spanning three, inherited OK/Cancel. Check 760x540 and 1000x700, labels and credit/opening fields, no orphan controls. |
| TaxSummaryReportView | Nine filter controls including Generate, six KPI controls, eight grid columns. Check 800x600 and 1280x720: wrapped filters remain within the surface and grid fills remaining space. |
| RefundAuditReportView | Nine filter controls including Generate, six KPI controls, ten grid columns. Check the same sizes, filter/button containment and grid resize. |
| FirstRunWizardForm | Inspect all eight pages through Document Outline/design-time visibility, header/sidebar, Back/Next/Finish/Cancel and status labels. Check 1020x720 and 960x640: long descriptions wrap, groups follow descriptions, footer buttons remain readable. Restore original page visibility afterward. Do not execute Test Connection, Apply Migrations, Finish or commissioning. |

For each affected form/control, perform a controlled edit/save/close/reopen trial using a temporary root AccessibleDescription marker. Compare the generated diff before and after; restore the marker and repeat. Check that controls/parents, rows/spans, resource references and single Load/Click subscriptions survive. Do not accept unexplained generated changes. Record rendering, resizing and round-trip outcomes separately. Designer DPI-unaware rendering does not qualify runtime PerMonitorV2 behavior.