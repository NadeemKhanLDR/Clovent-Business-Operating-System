# Smart Combo Builder

Implemented 2026-09-21. Manager approval is required; no external AI service, generated confidence score or automatic publication is involved.

## Architecture discovered

- `Restaurant.Orders.Order` has `Open`, `Held`, `Completed`, `Voided`, `Cancelled`; it owns WarehouseId and serialized child ID collections. `UpdatedAtUtc` is the existing completion-time proxy, as used by Restaurant Pulse; there is no separate immutable completed timestamp.
- `OrderLine` has ProductVariantId, quantity, snapshotted unit price/tax, IsVoided and OrderId. Removed lines must also be excluded using the parent OrderLineIds. Product/portion names and current selling/cost prices are resolved from Catalog, not historical name snapshots.
- `QuickOrderTemplate` / items are the canonical published deal aggregate. `CreateQuickOrderTemplateCommandHandler` stages templates; Restaurant `UnitOfWorkBehavior` saves changes. `ListActiveQuickOrderTemplatesQuery` expands templates for the POS strip. The existing `QuickOrderPreviewDialog` and `HandleQuickOrderAddAsync` use normal AddOrderLine / price override flows.
- `RecommendationRule` and `SuggestionEvent` remain the separate existing real-time upsell rules and offer/accept/dismiss analytics. Customer history, Active Orders, Hold/Recall and table lifecycle were not redesigned.
- Restaurant Pulse currently retrieves completed orders and resolves lines per order. Running a separate 30-day analysis inside that panel would add avoidable work. No Pulse metric was added; this is a documented secondary integration opportunity.
- Back Office navigation is registered in Program and MainForm.Designer; screens use DevExpress. Currency formatting uses CurrencyDisplay. Smart Combo currency is resolved from the authenticated user's company -> organization BusinessSettings, avoiding the older global-first-organization fallback.
- Permission codes follow `menu.*` and `feature.*`. Users have one CompanyId and an optional BranchId. No unscoped organization-wide sales analysis is provided.

## Components and data flow

`SmartComboBuilderView` -> AnalyzeSmartCombosQuery -> SmartComboService -> ISmartComboStore / SmartComboStore SQL projection -> SmartComboCalculator -> preview -> ConvertSmartComboCommand or DismissSmartComboCommand.

Historical orders are read-only. Each UI operation gets a fresh DI scope; a busy gate disables controls while async work runs. Mining runs on a background task. Failed conversion scopes are disposed rather than reused with unsaved tracked changes.

Analytics results are transient. `Restaurant.ComboDecisions` stores warehouse + sorted variant signature (composite primary key), user, timestamp, reason, dismissal expiry, resulting template ID and optimistic concurrency token. A signature is sorted distinct GUIDs in N format separated by `|`; pair/triple ordering cannot change identity. UI rows expose names only.

## Eligibility and scope

SQL limits to one authorized warehouse, `Completed`, and a half-open UTC date range `[now - days, now)`. Voided, cancelled, open and held orders are excluded. QA/TEST/[QA] note prefixes are excluded. Lines must be nonvoided, positive price and quantity, and still present in their parent's OrderLineIds. Orders with no valid sale lines are excluded.

Catalog variants and products must be active with a positive currently effective selling price in the configured currency. Missing/deleted identities and QA / TEST product-name prefixes are excluded. Catalog has no separate sellable/non-sale-category flag, so active product + active variant + valid selling price is the current sellability boundary. No invented category classification is used.

Development sample orders use `DEV-SMARTCOMBO:` notes and are excluded by default. Only the Development configuration explicitly enables them, and the screen identifies this mode. Unknown/unmarked historical test data cannot be reliably distinguished from genuine sales.

Company assignment is mandatory. Warehouses are restricted to active branches in that company and, if assigned, the user's single branch. The first permitted location is selected because ICurrentSession has no current warehouse field. There is no All Locations option or cross-company fallback. Access is rechecked inside every manager handler.

## Algorithm and mathematical meaning

Each eligible order becomes a distinct set of VariantIds; ordering multiple units or separate lines does not inflate frequency. Candidates contain exactly two or three distinct variants, with one unit of each in a suggested deal. Different portions remain different identities.

- Frequency F(S): number of baskets containing every item in S.
- Support: F(S) / N, where N is eligible baskets in the mining population.
- Pair A -> B: attach rate = F(A,B) / F(A).
- Triple (A,B) -> C: attach rate = F(A,B,C) / F(A,B).
- Lift for either direction: attach rate / (F(consequent) / N).
- Every possible singleton consequent is evaluated. Of qualifying directions, display the highest lift, then highest attach rate, then stable variant identity. The preview explicitly names the direction. This is a selected association rule, not a probability of future purchase or proof of causality.
- Results sort by frequency descending, then lift descending, then signature. No arbitrary High/Medium/Low strength classification is shown.

Baskets exceeding MaximumBasketSize are excluded from both counting and N, and reported. The entire analysis fails clearly if the SQL order limit or 250,000 distinct counted sets is exceeded; it never silently analyzes a truncated sample. CPU cancellation is supported. SQL projects only IDs/parent membership and uses existing Status and OrderId indexes. No speculative analytics index was added; measure production plans before adding a warehouse/status/date covering index.

## Settings

`SmartCombos` in appsettings.json binds to validated SmartComboOptions. Defaults: 30 days, minimum 5 orders, frequency 3, support 0.01, attach rate 0.10, lift 1.0, maximum 20 suggestions, pairs + triples, discount 0.05, minimum margin 0.20, dismissal 30 days, maximum 50,000 orders, maximum basket 40. Period is editable from 1–366 days in Back Office. Other settings are configuration-based; a persistent Back Office settings editor is a future extension.

## Pricing and cost

Normal price is the sum of current per-unit selling prices. Suggested price starts at 95% (configurable), rounded away from zero at currency precision. No psychological rounding is applied. Each component receives a proportional price override rounded down to minor units; the final component receives the exact remaining amount. Quantities are one.

Cost is the latest active effective Catalog Cost price in the same currency. It is an estimate, not recipe COGS, batch valuation or accounting profit. If any component lacks cost, total cost/profit/margin are unavailable, never zero-filled.

With complete cost, minimum price is rounded UP to protect minimum margin. For tax-inclusive products the conservative floor uses the lowest component net-revenue factor `1 / (1 + taxRate/100)`; exclusive-tax prices use factor 1. Conversion checks exact allocated net revenue, `margin = (net revenue - cost) / net revenue`, again. If the protected price exceeds normal price the candidate is omitted. Preview profit/margin strips inclusive tax; exclusive tax is still added by the existing POS tax pipeline. The displayed deal price is the existing template price basis and may not equal a tax-exclusive product's final payable total.

Manager-edited name is 1–100 characters. Price must be positive, no greater than normal price, match currency precision and meet the minimum margin when cost exists. Current catalog, evidence and duplicate/decision state are reanalyzed before creation; stale opportunities require reanalysis.

## Approval, conversion, duplicate prevention and auditing

Create Deal invokes the existing CreateQuickOrderTemplateCommandHandler directly to stage its normal aggregate, avoiding an inner mediator SaveChanges commit. Template, decision and ActivityLogEntry are committed together by the outer Restaurant UoW. Duplicate manager conversions collide on the warehouse/signature primary key or concurrency token, rolling back that transaction.

A new nullable WarehouseId on QuickOrderTemplate scopes smart deals to the analyzed warehouse. Legacy null-scoped templates retain global behavior. The existing POS query receives the current order/selected warehouse, and Add Deal rejects a scoped template after a location change. Expanding Quick Orders reloads available templates; reopening POS also loads them. The existing POS preview is reused without modification.

Active templates with exactly the same variant set and one-unit quantities suppress suggestions. Different quantities (e.g. a four-person family deal) are different compositions. Converted decisions suppress that combination permanently even if its template is later deactivated; no automatic republishing occurs. Existing manual template CRUD retains scope.

Dismissal accepts an optional 250-character reason. It suppresses the exact warehouse/signature for 30 days (configurable), then fresh rolling-period evidence can qualify it again. There is no permanent dismissal blacklist or undocumented frequency-change heuristic.

Permissions: `menu.smartcombos`; `feature.smartcombos.analyze`, `.create`, `.dismiss`; creation also requires `feature.quickordertemplates.create`. The existing development seed grants these only through the Administrator role; production administrators must assign them. No cashier bypass.

Audits record analysis, dismissal, conversion, actor ID, time, warehouse/signature/template references, original/current name and price. Audit writes are required and transactional for decisions; failure is surfaced rather than swallowed. Components are not editable in the opportunity preview.

## Database changes

- `20260921071737_AddSmartComboDecisions`: ComboDecisions + nullable QuickOrderTemplates.WarehouseId. No historical order changes and no parallel published-deal table.
- `20260921072627_PreserveQuickOrderCurrencyPrecision`: TemplateUnitPrice decimal(18,2) -> decimal(18,4), matching other existing monetary columns and supporting 0–4 currency decimals. Down-migration loses fractional precision and needs operator review.
- Snapshot includes the existing table-code uniqueness model already enforced by RestaurantPersistenceInitializer; this feature does not create or claim to fix that index.

## Development demonstration and cleanup

See `scratch/SmartComboCheck/Program.cs`, price/order manifests and `docs/testing/SmartComboBuilderImplementationReport.md`. The guarded explicit seeder is restricted to local `.` / Clovent demo databases and demo company. It does not run on application startup. Five completed take-away orders were created through CreateOrder, AddOrderLine, RecordPayment and CompleteOrder, using existing products and payment method, with no new tables/customers and no automatically published deal. Price records use the Catalog creation handler. Costs were not invented.

Run the desktop Development launch profile to include these samples. Set IncludeDevelopmentSamples=false to immediately exclude them from Smart Combo analysis. To reverse the demonstration, identify only manifest orders in Sales History and use the existing Void action; inspect related inventory movements before any reversal because existing cross-module stock reversal behavior is outside this feature. Deactivate only manifest price IDs in Catalog Prices to revert the development currency setup. Do not delete or rewrite historical orders using SQL.

## UI Layout System and Responsive Architecture (Rebuilt & Hardened 2026-09-21)

Following manual operator testing where collapsed container rows squashed controls across both Smart Combo Builder and Quick Order Template Edit Dialog, the root cause was identified and completely eliminated:
- **Root Cause:** In WinForms, DevExpress `PanelControl` and `GroupControl` placed with `Dock = DockStyle.Fill` inside `AutoSize` rows of a parent `TableLayoutPanel` do not aggregate their children's preferred heights, collapsing their preferred size to ~24px (or caption height) and violently clipping all child controls. Additionally, giving child editors `Dock = Fill` inside `AutoSize` columns created ambiguous sizing loops.
- **Smart Combo Builder Filter Panel:**
  - Rebuilt with a direct 3-row × 4-column `TableLayoutPanel` (`Dock = Top, AutoSize = true, AutoSizeMode = GrowAndShrink`) inside the filter card.
  - **Row 0 (AutoSize):** Separate label controls for "Analysis Period" (Col 0) and "Location / Warehouse" (Col 1).
  - **Row 1 (AutoSize):** LookUpEdit Period (`180x36`, Col 0), LookUpEdit Warehouse (`300x36`, Col 1), SimpleButton Analyze Sales (`160x36`, Col 2), Spacer (Col 3, 100%).
  - **Row 2 (AutoSize):** Status message and progress bar span all 4 columns.
  - Controls use `Dock = DockStyle.None` with explicit dimensions and margins, guaranteeing zero overlap or vertical compression.
- **Quick Order Template Edit Dialog:**
  - Removed all `GroupControl` wrappers.
  - Rebuilt into 5 clean root rows inside `mainTable`:
    - **Row 0 (AutoSize):** Template Details panel (TableLayoutPanel with Name, Description, Display Order, and Active CheckEdit).
    - **Row 1 (AutoSize):** Line Item Builder panel (TableLayoutPanel with Product 55%, Variant 35%, Qty 10%; Price grid with Deal Price 150px and Catalog Price 150px; Action strip with Deal Price reset, Line Total preview, and Add Item button).
    - **Row 2 (AutoSize):** Dedicated Grid Toolbar (`FlowLayoutPanel`) hosting `[ Edit Selected ]` (120x34) and `[ Remove Selected ]` (135x34) with equal heights and unified selection-state disabling.
    - **Row 3 (Percent 100%):** Items Grid panel—the sole filling region, receiving ~204px+ of vertical height with `ColumnAutoWidth = true`. Sized proportionally across full width (Product 28%, Variant 22%, Qty 10%, Deal Price 13%, Catalog Price 13%, Line Total 14%) to eliminate truncation and empty right margin.
    - **Row 4 (AutoSize):** Footer panel with item count, large Deal Total, Cancel, and Save Changes buttons.
  - Dialog dimensions: `960 x 660` target, `840 x 540` MinimumSize, `CenterParent`, resizable, DesktopDpi-scaled.
- **Smart Combo Opportunity Dialog Rebuild:**
  - Sizable DPI-aware dialog (~760x650 scaled, min 680x580, CenterParent).
  - Replaced plain-text unaligned memo preview with a structured read-only DevExpress `GridControl`/`GridView` (`ColumnAutoWidth = true`) showing Product, Variant / Portion, Qty, and Price, plus right-aligned `Normal Total`.
  - Structured 6-metric Sales Evidence card (Bought Together, Eligible Orders, Support, Attach Rate, Lift, Analysis Period).
  - Clear Deal Settings with live two-way Deal Price / Discount calculation and unverified cost notice.
  - Full-width, unclipped Dismiss Reason dropdown.
- **Visual Acceptance Status:** READY FOR USER MANUAL RETEST (LIVE UI = NOT EXECUTED). Automated tests validate complete layout geometry and runtime bounds.

## Quick Order Template Integration & Deal Price Semantics

When a manager converts a Smart Combo opportunity, `ConvertSmartComboCommand` allocates the suggested deal price across components and stages a `QuickOrderTemplate` with explicit `TemplateUnitPrice` values.
- When opening an existing template in `QuickOrderTemplateEditForm`, the saved `TemplateUnitPrice` values are strictly preserved. The editor never silently replaces deal prices with current catalog prices.
- When adding new items to a template, the active selling price is automatically fetched from the canonical Catalog read model and remains fully editable for manager-defined deal component pricing.

## Validation and limitations

Automated calculator, handler, SQLite persistence/concurrency, historical query, and desktop presentation/state tests cover core behavior. SQLite historical query tests use a test-only UTC-ticks converter because SQLite does not support datetimeoffset comparison. The real development analysis exercises the unmodified SQL Server projection. Full suite results and the separate live UI status are in the implementation report.

Manual visual/live acceptance remains required. Restaurant Pulse has no new card. There is no revenue forecast, no learned preferred quantities, no complete recipe COGS, no immutable completed timestamp, and no separate sellability flag. DailySalesSequence and all previously documented technical debt remain unresolved by this work. Future service/query extensions can introduce segments, dayparts, weekdays, seasons, profit weighting and alternative scoring without replacing published Quick Orders or decision identity.
