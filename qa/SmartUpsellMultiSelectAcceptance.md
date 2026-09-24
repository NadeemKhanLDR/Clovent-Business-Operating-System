# Smart Upsell multi-select implementation and acceptance report

Date: 2026-09-18. **STATUS: NOT READY** — implementation and focused automated checks pass; mandatory live acceptance remains incomplete.

1. **Existing architecture found.** The workspace already contained an unfinished `SuggestedAddOnsDialog` and POS integration. Back Office `RecommendationRules` feed `GetBasketRecommendationsQuery`; Catalog bulk queries resolve product/variant names and active selling prices. The POS uses MediatR add/quantity commands, a semaphore for cart mutation, order summary refresh and fire-and-forget `SuggestionEvents`. Existing cart docking corrections were retained.

2. **Files modified in this task.**
   - `src/Clovent.Desktop/Restaurant/SmartPos/SuggestedAddOnsDialog.cs`
   - `src/Clovent.Desktop/Restaurant/Orders/RestaurantPosForm.cs`
   - `src/Clovent.Restaurant.Application/SmartRecommendations/Queries/GetBasketRecommendationsQuery.cs`
   - `src/Clovent.Desktop.Tests/Restaurant/SmartPos/SuggestedAddOnsDialogTests.cs`
   - `src/Clovent.Desktop.Tests/Restaurant/SmartPos/SuggestedAddOnsPipelineTests.cs` (new)
   - `src/Clovent.Desktop.Tests/LiveAcceptanceQaRun.cs`
   - `src/Clovent.Restaurant.Application.Tests/SmartRecommendations/GetBasketRecommendationsQueryHandlerTests.cs`
   - `12 Restaurant POS/12.01 Restaurant POS Overview.md`
   - `docs/architecture/RestaurantPOSArchitecture.md`
   - `scratch/AddOnsReadOnlyCheck/Program.cs` and its project file (new diagnostic)
   - This report, QA logs and three dialog rendering images.
   The workspace had substantial unrelated changes before this task; these were not reverted.

3. **Dialog.** DevExpress XtraForm, GridControl/GridView, sizable 780x460 client area, 640x380 minimum, DPI scaling, 42px scaled touch rows, alternating rows, readable grid fonts and pinned footer. Opening uses cached recommendations and performs no per-row lookup.

4. **Columns.** Real checkbox, Item, Portion, right-aligned Price. No product, variant or rule IDs are displayed. Columns are explicitly visible and auto-fit the grid width.

5. **Checkbox behavior.** Row clicks and Space toggle selection. Multiple rows can be checked without Ctrl. Selected-count text updates immediately. Empty selection disables Add Selected.

6. **Select All.** Selects or clears available rows, with a reentrancy guard so reflecting partial selection does not accidentally clear other rows. Confirmed additions cannot be reselected after a partial failure.

7. **Add Selected.** The dialog stays modal while the POS processes the batch. Add, Cancel, selection controls and X are blocked while processing. The whole batch holds the existing order semaphore and calls the same extracted `AddProductToCurrentOrderCoreAsync` used by ordinary menu adds. Cart/totals refresh precedes recommendation refresh and successful dialog close. Confirmed writes are tracked before refresh, preventing duplicate replay after a later error. This follows existing per-item persistence; it is not an all-or-nothing SQL transaction. Failure leaves the dialog open with a partial-completion message.

8. **Duplicates.** Existing nonvoided same-variant lines without notes receive SetOrderLineQuantityCommand. Voided/noted lines do not merge. Focused tests verify this exact shared method and quantity behavior.

9. **Variants.** The selected ProductVariantId passes unchanged into normal cart commands. Names come from Catalog. Half Plate/Full Plate display as Half/Full; Standard, Regular, identical and empty portions collapse to `-`. Unit prices use CurrencyDisplay formatting.

10. **SuggestionEvents.** Offered tracking remains once per variant/order in the existing model. Each successful add command supplies its actual line ID and unit price for one Accepted event with quantity 1. Failed and unchecked items produce none. Duplicate variant inputs are deduplicated. Two focused batch tests verify event attribution after success and the absence of false/duplicate acceptance when a subsequent write or refresh fails. Analytics retains the existing asynchronous, best-effort persistence behavior; live SQL acceptance-event verification was not completed.

11. **Back Office integration.** No hardcoded relationships or prices were added. The POS now requests all applicable results rather than imposing the previous ten-result cap. A query bug was fixed: a nonmatching earlier rule no longer suppresses a later matching rule for the same variant. The development rules were sufficient, so no dummy rule was created. The final diagnostic builds query services without initializing the desktop or running startup tasks.

12. **Live scenario / actual products.** The actual Desktop executable was launched with its existing development POS entry point. Computer Use app approval timed out before a POS screenshot or interaction could be obtained. The required fresh Dine-In sequence was therefore not performed. Separately, the real database-backed recommendation query was exercised: Chicken Biryani / Standard returned Salad / Standard (configured), Leechi (configured), and Chicken Haleem / Half Plate (popular today). After supplying all three IDs as basket contents, all three were excluded and Garlic Nan became eligible through the Haleem rule. This was a query check, not a cart mutation or live acceptance test.

13. **Before subtotal.** Catalog price for one Chicken Biryani / Standard: 450.00. No live starting-cart subtotal was measured.

14. **Available items/prices.** The actual query returned Salad 30.00, Leechi 50.00 and Chicken Haleem / Half 260.00. These were not checked/added through the live POS. The rendering fixtures instead use Leechi, Haleem, Garlic Nan and Fresh Salad; they must not be confused with database acceptance evidence.

15. **Expected vs actual totals.** The actual query's three choices imply 450 + 30 + 50 + 260 = **790.00** subtotal. Those four products have zero configured tax; with no discount, service charge or payments, payable/balance would also be 790.00. Actual POS subtotal, tax, discount, service charge, payable and balance remain unverified. The user's alternate Leechi/Haleem/Garlic Nan example would have subtotal 810.00, but the current catalog's 15% exclusive Garlic Nan tax would make payable 817.50 under the same assumptions. No totals were overwritten or calculated inside the dialog.

16. **Hold/Recall/restart.** Not live verified for this new batch. Existing persistence commands are reused; that is not a substitute for the required fresh Dine-In Hold/Recall/restart test.

17. **1024x768.** Not live verified. Main cart docking order was preserved; dialog rendering alone does not prove POS layout at this size.

18. **1366x768.** Not live verified for the completed workflow.

19. **Maximized.** Not live verified for the completed workflow.

20. **Build.** Exact requested command `dotnet build src/Clovent.Desktop/Clovent.Desktop.csproj -c Debug --no-incremental`: final result **0 errors, 63 warnings**. Initial sandbox attempt could not read NuGet.Config; the authorized retry succeeded. A later focused-test build encountered the QA application's DLL lock; the specific application instance launched by this task was stopped and the rerun succeeded. See `addons_build_final.log`.

21. **Tests.**

   | Run | Passed | Failed | Skipped | Completion |
   |---|---:|---:|---:|---|
   | Restaurant Application, requested command | 264 | 0 | 0 | Complete |
   | Focused SuggestedAddOns tests, final | 25 | 0 | 0 | Complete |
   | Full Desktop, bounded diagnostic | 374 | 0 completed failures | 1 | Aborted; live acceptance test remained running |

   The initial Desktop run hit an existing CS0103 in LiveAcceptanceQaRun (`suggestionOrderIdField` out of scope). That compile error and obsolete quick-add test selectors were corrected. The subsequent full run hung; the bounded rerun stopped after 90 seconds of inactivity with `LiveAcceptanceQaRun.Run_Full_POS_Live_Acceptance_Suite` active. The console's partial “Passed!” line does **not** mean the full run passed. The bounded run preceded two additional focused event tests; do not sum overlapping suite counts. Its skipped test was `CategorySelectionLiveScreenshotsTest.Generate_Live_Category_Selection_Screenshots`. Logs and TRX files preserve the evidence.

22. **Documentation.** Both required architecture/overview documents now describe the workflow, checkbox selection, Select All, Add Selected, cancel/busy behavior, merge/variant handling, event attribution, totals refresh and responsive layout. They also document partial-failure semantics.

23. **Remaining issues / evidence.** Mandatory real Dine-In screenshots A/B/C/D, three-item add, actual calculated totals, SQL Accepted-event verification, Hold/Recall, restart, and three-resolution checks remain outstanding because desktop interaction was blocked. The existing broad live acceptance test also hangs and its post-abort database cleanup was not verified. Existing dependency diagnostics include NU1903 warnings for transitive System.Security.Cryptography.Xml 8.0.3 in the diagnostic build, and EF precision warnings for the existing SuggestionEvent decimal fields; these were not changed as part of this UI task. The tracked POS file also had an unrelated pre-existing trailing-whitespace diff at line 761. Rendered fixture images are `addons_01_dialog_unchecked.png`, `addons_02_three_checked.png`, and `addons_03_select_all.png`; they are **automated dialog renders, not live POS acceptance screenshots**. Read-only evidence is in `addons_development_rules.txt`, `addons_development_prices.txt`, and `addons_engine_readonly.log`.

24. **STATUS: NOT READY.** The multi-select implementation is available and its focused tests pass. Full acceptance cannot be declared until the required live checks and hanging full-suite test are resolved. No sell-ready claim is made.
