---
title: Dashboard Reference
type: Architecture
status: Awaiting Solution Architect review
created: 2026-07-27
updated: Milestone 15
applies_to: src/Clovent.Desktop
---

# Dashboard Reference

Milestone 12 builds the Dashboard shell `Program.cs` navigates to immediately after a successful sign-in - the default view inside the Milestone 11 Shell. "No business modules" per the brief; every number and list shown is real data from what Milestones 7-11 already built, not fabricated business metrics.

---

## 1. `DashboardView` - a `XtraUserControl`, not a `Form`

Unlike every other UI type built so far (`LoginForm`, `ShellForm`, `ErrorDialogForm`, `NotificationsForm`), `DashboardView` is a `DevExpress.XtraEditors.XtraUserControl` - it's hosted *inside* `ShellForm`'s workspace via `INavigationService`/`IWorkspaceHost`, exactly the pattern those two were built for since Milestone 7, not a standalone window. This also matches `05 Software Architecture`'s stated convention for the "UI Layer" (`XtraUserControl`, `ViewModels`) more literally than any Milestone 7-11 form did, since none of those needed to be embeddable.

**Real data, no business module needed to demonstrate it:**

| Section | Data source |
|---|---|
| Stat cards (Active Sessions, Logins (7 days), Notifications) | `ISessionRepository.GetActiveByUserIdAsync`, `ILoginAttemptRepository.GetRecentByUserIdAsync`, `INotificationService.Notifications` - all for `ICurrentSession.UserId` |
| Recent Activity list | The same recent `LoginAttempt`s, formatted |
| Notifications list | `INotificationService.Notifications` (Milestone 11), shown inline in addition to the Ribbon's popup |
| Company/Branch selectors | `IRecentItemsService.RecentCompanies`/`RecentBranches` (Milestone 11) |
| Quick actions | "Refresh" (re-runs the load), "View All Notifications" (opens Milestone 11's `NotificationsForm`) |

Nothing here queries Role/Permission/Organization/Company/Branch - consistent with every prior milestone's documented scope boundary.

---

## 2. A third instance of the Scoped-dependency-from-a-long-lived-object pattern

`ISessionRepository`/`ILoginAttemptRepository` are Scoped. `DashboardView` is resolved via `INavigationService`'s factory (`Func<Control>`, called once per navigation) and then lives for as long as it's the workspace's content - potentially much longer than a single async operation, unlike `LoginService`'s one-call scope or `ShellForm.RefreshNavigationAsync`'s per-refresh scope.

**This is why `DashboardView` owns its scope for its own lifetime rather than creating a short-lived one per query**: its constructor calls `scopeFactory.CreateScope()` once and keeps it, resolving both repositories from that one scope; `Dispose(bool disposing)` disposes the scope when the control itself is disposed (navigating away, or the Shell closing). A per-query scope (the `LoginService`/`NavigationMenuBuilder` pattern) would work equally correctly here but would mean re-resolving/reconnecting a `DbContext` on every `Refresh` click - fine either way at this scale, but tying the scope to the control's own lifetime is the more natural fit for a view that stays alive and might reload its data repeatedly.

---

## 3. Empty and loading states

**Loading**: a `ProgressBarControl` (same control Milestone 8's Login form uses for its own loading indicator) shown for the duration of `LoadAsync`, with the Refresh button disabled meanwhile - identical reasoning to `LoginForm.SetLoading`.

**Empty state, per section, not a single global one**: the Recent Activity and Notifications lists each show a plain placeholder string (`"No recent activity."`/`"No notifications yet."`) when empty rather than an empty `ListBoxControl`; the Company/Branch selectors show a single disabled placeholder entry (`"No recent companies"`/`"No recent branches"`) rather than an empty, clickable dropdown. Since nothing seeds `IRecentItemsService` yet (flagged in `DesktopBootstrap.md`'s Milestone 11 addendum), the selectors render in their empty state by default in this milestone - a real, honest demonstration of "empty state," not a placeholder screenshot.

---

## 4. Open questions for Solution Architect review

1. **Dashboard registers directly in `Program.cs`, not through `DesktopModuleCatalog`.** Consistent with treating the Dashboard as part of the Shell itself rather than a business module - confirm this categorization holds once real business modules (Restaurant POS, Inventory, etc., per `01.04 Product Modules.md`) start registering their own navigation entries alongside it.
2. **`DashboardView`'s per-instance scope lifetime** (Section 2) is a third variant of the Scoped-dependency pattern already used twice differently (`LoginService`'s per-call scope, `ShellForm.RefreshNavigationAsync`'s per-refresh scope). **Needs a decision**: should the solution settle on one canonical pattern for "a long-lived UI element needing Scoped services" once a second view like this exists, rather than each view choosing independently?
3. **Recent Activity has no pagination or "view more" affordance** - it always shows the last 7 days, capped by whatever `GetRecentByUserIdAsync` returns. Acceptable for a dashboard summary; flagged in case a future requirement wants a full activity log view.

---

## 5. Milestone 13 addendum: Current Organization/Company/Branch/Fiscal Year/User

A new "Business Context" row of five stat cards sits above the existing Active Sessions/Logins/Notifications row: Current Organization, Current Company, Current Branch, Current Fiscal Year, Current User. All five now query real data - `ListOrganizationsQuery`/`ListCompaniesByOrganizationQuery`/`ListBranchesByCompanyQuery`/`GetBusinessSettingsByOrganizationQuery`/`GetFiscalYearByIdQuery` (`Clovent.Identity.Application`/`Clovent.MasterData.Application`, both new in Milestone 13) via the same per-instance `IServiceScope` `DashboardView` already held for `ISessionRepository`/`ILoginAttemptRepository` (Section 2's pattern, now also resolving `MediatR.IMediator` from it).

**"Current" still means "the first one," since no tenant-switcher UI exists.** This is the same honest simplification as the Company/Branch selectors' `IRecentItemsService`-backed combos (Section 1's table) - not a fabricated multi-tenant context concept. See `DesktopAdministration.md` Section 4 and its open question #2 for the flagged follow-up (does a real "current context" selector eventually need to exist, and where would it be persisted).

This finally gives Row 2's open question #2 (Section 4 above, "should the solution settle on one canonical Scoped-dependency pattern") its second and third data points within the same class: `DashboardView` now resolves *two* different Scoped surfaces (Authentication's repositories, Identity/MasterData's `IMediator`) from the one scope it already owned - no new pattern was needed, reinforcing that the per-instance-scope approach generalizes cleanly to more dependencies of the same kind.

---

## 6. Milestone 14 addendum: Total Products, Low Stock, Out of Stock, Inventory Value, Recent Stock Movements

A second stat-cards row (Total Products, Low Stock, Out of Stock, Inventory Value) sits below Milestone 13's Business Context row, and a third list column (Recent Stock Movements) joins Recent Activity/Notifications in the content row - all backed by real `Clovent.Catalog.Application`/`Clovent.Inventory.Application` queries resolved from the same per-instance `IServiceScope` `DashboardView` already held (Section 2's pattern generalizing to a third bounded context's worth of dependencies, the same reinforcement Section 5 already noted for a second).

**Low Stock/Out of Stock/Inventory Value are pure calculations, extracted for testability** - `CatalogDashboardCalculations` (`Clovent.Desktop.Dashboard`) holds `CountLowStock`/`CountOutOfStock`/`CalculateInventoryValue` as static functions with no DevExpress or MediatR dependency, the identical "extract the logic, keep the view a thin wrapper" discipline `DesktopAdministration.md` Section 1 established for `MasterDataFilter`. "Low Stock" is defined as quantity-on-hand at or below a configured minimum but still above zero (a minimum of `0` means "no policy set," deliberately excluded rather than miscounted as perpetually low); "Out of Stock" is quantity-on-hand at or below zero.

**Inventory Value resolves each distinct variant's cost price with a separate query, not a batched one** - there is no flat "list every current price" query surface in `Clovent.Catalog.Application` (only `ListProductPricesByVariantQuery`, scoped per variant), so the widget iterates every distinct `ProductVariantId` across all warehouse balances and looks up its active Cost-type price individually, defaulting to `0` for a variant with no cost price recorded yet rather than throwing. Fine at this milestone's demo scale; flagged in `WarehouseManagement.md`'s own open questions as a performance concern once a catalog holds meaningfully more variants with stock.

---

## 7. Milestone 15 addendum: Today's Sales, Open Tables, Running Orders, Kitchen Queue, Top Selling Items

A third stat-cards row (Today's Sales, Open Tables, Running Orders, Kitchen Queue) sits below Milestone 14's Catalog/Inventory row, and a fourth list column (Top Selling Items) joins the content row - all backed by real `Clovent.Restaurant.Application` queries resolved from the same per-instance `IServiceScope` `DashboardView` already held (Section 2's pattern generalizing to a fourth bounded context's worth of dependencies).

**Open Tables counts tables whose `OccupancyStatus` is `Occupied`** - not every table on the floor plan, and not "tables with an open order" measured a different way; `RestaurantDashboardCalculations.CountOccupiedTables` is the pure, tested function backing it. Running Orders and Kitchen Queue are plain counts of `ListOpenOrdersQuery`/`ListActiveKitchenTicketsQuery` results, needing no extraction into a pure function since there is no filtering logic beyond what those queries already return.

**Today's Sales and Top Selling Items both walk every order completed today individually** - the identical "no batched read model exists yet" limitation Section 6 already documents for Inventory Value, now a third instance: for each order `RestaurantDashboardCalculations.FilterCompletedOn` selects, the widget calls `GetOrderSummaryQuery` (summing `PaidTotal` for Today's Sales) and `ListOrderLinesByOrderQuery` (accumulating every active line for `RestaurantDashboardCalculations.TopSellingItems` to rank by quantity sold, resolving each ranked variant's display name via `GetProductVariantByIdQuery`). Fine at demo scale; the same performance flag already raised twice below applies a third time.
