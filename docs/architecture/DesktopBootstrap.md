---
title: Desktop Bootstrap Reference
type: Architecture
status: Awaiting Solution Architect review
created: 2026-07-27
updated: Desktop Shell & Document Architecture rebuild (2026-08-01) - see note below
applies_to: src/Clovent.Desktop
---

> **Superseded in part (2026-08-01).** `ShellForm` (Section 4, Section 8) has been renamed/replaced by `Forms/Shell/MainForm.cs`, and the single-panel `IWorkspaceHost.SetContent` workspace described throughout Section 4 and Section 8 has been replaced by a non-MDI `DocumentManager`/`TabbedView` multi-document host. `DashboardView`/`LoginForm` moved under `Forms/Dashboard`/`Forms/Identity`; `UserListView`/`RoleEditorView`/`ProductManagementView` were replaced by `Forms/Identity/Users/UsersForm`, `Forms/Identity/Roles/RolesForm`, and `Forms/Catalog/Products/ProductsForm` respectively, each now a genuine `BaseForm`-derived Designer form. **See `DesktopShellArchitecture.md` for the current shell/document/`BaseForm` architecture, folder structure, conversion recipe, and remaining-work backlog.** Everything below this note is historical Milestone/Phase narrative, kept intact for context; treat any mention of `ShellForm`, a single swappable workspace panel, or "no `.Designer.cs` split-file forms" as describing a since-superseded state, not current behavior.

# Desktop Bootstrap Reference

Describes `src/Clovent.Desktop` after Milestone 7 (host shell), Milestone 8 (Login UI), and Milestone 11 (Desktop Shell): the first UI-hosting project in the solution, and the first to use DevExpress WinForms.

## Milestone 11 addendum: Desktop Shell

`ShellForm` changed base type from `XtraForm` to `DevExpress.XtraBars.Ribbon.RibbonForm` - the standard DevExpress form for hosting a `RibbonControl` (`Form.Ribbon`) and a `RibbonStatusBar` (`Form.StatusBar`) natively. Every API used here (`RibbonPage`/`RibbonPageGroup`/`BarButtonItem`/`BarEditItem`/`BarSubItem`/`AccordionControl`) was verified against the installed DevExpress 26.1 assemblies via reflection probes before being used, the same discipline as Milestone 8's `LoginForm` - notably, `BarEditItem`'s selected value is read/set through its own `EditValue` property, not through the `RepositoryItemComboBox` assigned to `.Edit` (that repository item has no `SelectedItem`/`SelectedIndex` of its own - those belong to a live editor control, and a `BarEditItem`'s repository item is a template, not an editor instance).

**Navigation is permission-aware via a testable, UI-free class.** `NavigationMenuBuilder` (`Clovent.Desktop.Navigation`) filters `INavigationService.RegisteredKeys` through `IMenuAuthorizationPolicy.CanViewMenuItemAsync` (Milestone 10) - this is the actual "permission-aware navigation" logic, kept entirely separate from `ShellForm`'s `AccordionControl` construction so it can be unit tested without a Windows Forms message loop. `ShellForm` itself just turns whatever keys the builder returns into `AccordionControlElement`s.

**A second Scoped-dependency-from-a-Singleton problem, solved the same way as `LoginService`'s.** `IMenuAuthorizationPolicy`/`IAuthorizationService` are Scoped (they ultimately need Scoped repositories). `ShellForm` is a Singleton (one Shell window per app run). `NavigationMenuBuilder` is therefore also registered Scoped - so `ShellForm` cannot hold one as a constructor-injected field. It instead takes `IServiceScopeFactory` and creates a fresh scope inside `RefreshNavigationAsync()` each time navigation is (re)built, resolving `NavigationMenuBuilder` from that scope - the identical pattern `LoginService` already established in Milestone 9 for the same underlying reason.

**`ICurrentSession` gained `DisplayName`.** The profile menu needs something to show; `LoginService.LoginAsync` already has the resolved `User` in scope at the point it calls `SignIn`, so passing `user.DisplayName.Value` through cost nothing extra to plumb.

**Notifications** (`INotificationService`/`NotificationService`, Desktop-owned, in-memory) and **recent companies/branches** (`IRecentItemsService`/`RecentItemsService`, also Desktop-owned, in-memory, capped at 5, de-duplicated) are both genuinely new, testable services - not stubs. What's *not* real: nothing populates recent companies/branches with actual data, because no Organization/Company/Branch persistence exists anywhere in this solution (consistently out of scope since `IdentityDomain.md`) - the Ribbon shows the mechanism (a `BarSubItem` per category, rebuilt from `IRecentItemsService`) with nothing in it yet, the same "seam built, not yet populated" pattern as `DesktopModuleCatalog` since Milestone 7.

**Theme switching** reuses `IThemeService` (Milestone 7) via a Ribbon `BarEditItem`, exactly like the Login form's theme selector (Milestone 8) but now available after sign-in too. **Language switching** is decorative only, identical to Login's, for the identical reason (no localization infrastructure exists).

**Module registration** did not change: `DesktopModuleCatalog`/`DesktopModuleLoader` (Milestone 7) are unchanged - this milestone is what finally gives them a UI to register *navigation* into (via `INavigationService.Register`), but no module registers anything yet (Milestone 12, "Dashboard," is expected to be the first).

### Open question added by this milestone

4. **No permission/role seed data exists** (same gap `Authorization.md` Section 4, item 3 already flagged) - `NavigationMenuBuilder` is wired correctly but will show nothing for the Milestone 9 seed admin user until either seed permissions or a real role-assignment flow exists. Confirm this is acceptable to carry into Milestone 12 rather than blocking on it here.

## Milestone 8 addendum: Login UI

`Login/LoginForm.cs` is now the app's entry screen (`Program.cs` runs it via `Application.Run(loginForm)` instead of `ShellForm` - the Shell is still built and registered, just not yet the first thing shown). It contains **no credential-checking logic**: the Log In button's click handler calls `ILoginService.LoginAsync(request)` only, exactly as the milestone brief requires ("Button should call an interface only").

**`ILoginService`** (`Login/ILoginService.cs`) is a UI-owned seam, the same Dependency Inversion pattern as `Clovent.Authentication.Application.IIdentityUserService`/`IUnitOfWork`: the layer that needs the capability (the Login form) defines the interface. `LoginRequest` carries `Username`, optional `Password`, optional `Pin` (at least one of the two is required - the form supports signing in with either), and `RememberMe`. `LoginResult` is a simple succeeded/error-message pair with `Success()`/`Failure(message)` factories.

**`PlaceholderLoginService`** is this milestone's registered implementation - it always returns `LoginResult.Failure("Sign-in is not available yet.")` rather than throwing. This was a deliberate choice over throwing `NotImplementedException`: a thrown exception would route through Milestone 7's `GlobalExceptionHandler`/`ErrorDialogForm` (the *unexpected*-error path), when "no auth logic yet" is an *expected* state this milestone should demonstrate gracefully through the form's own failure-message UI (loading indicator → disabled inputs → re-enabled inputs → red failure message), not through a crash dialog. Milestone 9 replaces only the DI registration (`services.TryAddSingleton<ILoginService, ...>()`); `LoginForm` does not change.

**Layout**: `TableLayoutPanel` (two percentage-sized columns: 38% branding, 62% form) rather than DevExpress's `LayoutControl` for the outer split - a stock `TableLayoutPanel` re-flows proportionally on resize with zero custom code, which is what "responsive layout" actually needs at the top level. Within the form column, `DevExpress.XtraLayout.LayoutControl` lays out the actual fields (username/password/PIN/remember-me/language/theme/button) - it auto-aligns labels-to-editors and is the idiomatic DevExpress control for exactly this. **Company logo**: no logo asset exists anywhere in this repository (`Assets/`, `Diagrams/` are both empty) - the branding panel shows a styled "CBOS" wordmark placeholder rather than fabricating an image asset; swapping in a real logo later is a one-line change (`PictureEdit` in place of the wordmark `LabelControl`).

**Theme selector is functionally wired** (not just decorative): selecting a skin calls `IThemeService.ApplySkin` (built in Milestone 7) immediately. **Language selector is decorative only** - three static entries, no localization infrastructure exists yet in this repository to wire it to.

**Keyboard shortcuts**: `AcceptButton` is the Log In button (Enter submits from any field); a hidden `CancelButton` closes the form on Escape; every `LayoutControl` field label carries an `&` mnemonic (`&Username`, `&Password`, `P&IN`, `&Language`, `&Theme`) for Alt-key access. **Accessibility**: every input has an explicit `AccessibleName`; tab order follows the `LayoutControl`'s declared item order; color/contrast is left to the active DevExpress skin rather than hardcoded, so switching themes doesn't fight custom colors (only the branding panel and failure text set explicit colors, both deliberately - brand identity and a semantically red error color respectively).

**Loading indicator** is a `DevExpress.XtraEditors.ProgressBarControl`, shown/hidden around the `LoginAsync` call, with every input and the button disabled while visible and the button's own text switching to "Logging in...". A true marquee/indeterminate animation was investigated (`RepositoryItemProgressBar.ProgressKind`/`.ProgressViewStyle` were checked via reflection against the installed DevExpress 26.1 assemblies) but neither controls marquee animation - that lives in a separate `DevExpress.XtraWaitIndicator` assembly not referenced by this project. The visible/hidden + disabled-inputs state change is a genuine loading indicator on its own; a future milestone can add the animated variant if `DevExpress.Win.WaitIndicator`-equivalent package is added.

### Open question added by this milestone

4. **`PlaceholderLoginService`'s always-fail behavior vs. throwing.** Ratify or override the reasoning above - some teams prefer every not-yet-implemented seam to throw loudly (fail fast, impossible to miss in a demo) over failing quietly through the normal UX path.

---

## 1. Why this environment's DevExpress evaluation license matters

This machine has DevExpress 26.1 installed with local NuGet feeds (`nuget source list` shows `DevExpress 26.1 Local`) and an active license file, but it is a **trial/evaluation license** (compiler emits `DX1000` for every `DevExpress.Win*` control used). `Clovent.Desktop.csproj` suppresses `DX1000` explicitly, with a comment explaining why and noting the suppression should be removed once a production license is registered - silencing it without a paper trail would hide a real "is this build production-legal" signal from whoever builds this next.

`DevExpress.Win` (one package reference) was sufficient for everything this milestone and every later Desktop milestone needs - its own `nuspec` already depends on `DevExpress.Win.Grid`, `DevExpress.Win.Navigation`, `DevExpress.Win.Printing`, `DevExpress.Win.TreeList`, `DevExpress.Win.VerticalGrid`, `DevExpress.Data`, `DevExpress.Data.Desktop`, `DevExpress.Utils`, and `DevExpress.Drawing`.

---

## 2. Folder structure

```
src/Clovent.Desktop/                    (WinExe, net10.0-windows, UseWindowsForms)
  Clovent.Desktop.csproj
  appsettings.json                      - Platform + Desktop + ConnectionStrings sections
  Program.cs                            - [STAThread] entry point, the boot sequence
  Theming/
    DesktopOptions.cs                   - validated options (DefaultSkin, DefaultLanguage)
    IThemeService.cs, ThemeService.cs    - runtime DevExpress skin switching
    ThemeInitializationStartupTask.cs   - IStartupTask (Clovent.Platform), applies the default skin once
  Startup/
    ISplashScreenService.cs, SplashScreenService.cs
    IErrorDialogService.cs, ErrorDialogService.cs, ErrorDialogForm.cs
    GlobalExceptionHandler.cs           - wires ThreadException/UnhandledException/UnobservedTaskException
  Shell/
    IWorkspaceHost.cs, ShellForm.cs      - main window; bare in this milestone (Ribbon etc. is Milestone 11)
  Navigation/
    INavigationService.cs, NavigationService.cs
  Modules/
    DesktopModuleCatalog.cs             - the ordered IModule type list to load (empty for now)
    DesktopModuleLoader.cs              - reflectively calls Clovent.Platform.Modules.AddModule<T>() per catalog entry
  DependencyInjection/
    DesktopServiceCollectionExtensions.cs - AddDesktopHost(services, configuration)

src/Clovent.Desktop.Tests/              (net10.0-windows, UseWindowsForms - Control-typed test doubles need it)
  Navigation/NavigationServiceTests.cs
  Modules/DesktopModuleLoaderTests.cs
```

---

## 3. Reused vs. new

Everything in Section 2 is new to this milestone. What it reuses without modification from `Clovent.Platform` (Milestones 1-3): `ApplicationBootstrapper` (`.Create().WithLogging().WithPlatform()`, then `.BuildAndInitializeAsync()`), `IStartupTask`, `IModule`/`ModuleRegistry`/`AddModule<TModule>()`, `OptionsRegistrationExtensions.AddValidatedOptions<T>()`. No changes were needed to any Platform Foundation file - the desktop host is exactly the kind of consumer that infrastructure was built for.

**`DesktopModuleLoader`** is the one piece of reflection in this milestone. `AddModule<TModule>()` is intentionally generic (a compile-time type parameter, so a module's own `RegisterServices` is called with full static typing) - but `DesktopModuleCatalog.ModuleTypes` is a runtime `IReadOnlyList<Type>` (so appending a module later is a one-line list edit, not a new call site in `Program.cs`). Bridging the two needs exactly one `MethodInfo.MakeGenericMethod` call; `DesktopModuleLoader` is that bridge, validated (throws `ArgumentException` with a clear message) before the reflection call rather than surfacing a raw `TargetInvocationException`.

**`DesktopModuleCatalog.ModuleTypes` is empty.** No business module implements `IModule` yet - Authentication's Application/Infrastructure layers don't need to (they're registered directly via `services.AddDesktopHost(...)`-adjacent calls once Milestone 9 needs them). This is not an oversight; it's the seam Milestone 9 ("Authentication Integration") is expected to use once an `AuthenticationModule : IModule` exists.

---

## 4. Splash screen and Shell: minimal by design

**Splash screen** uses DevExpress's built-in default wait form (`SplashScreenManager.ShowDefaultWaitForm`/`CloseDefaultWaitForm`) rather than a custom `SplashScreen`-derived form - it needs no designer-authored layout and no runtime dependency beyond DevExpress itself, while still giving the description-text updates ("Initializing persistence...", "Loading shell...") the boot sequence in `Program.cs` reports.

**`ShellForm`** is deliberately bare: a caption strip, a swappable workspace panel (`IWorkspaceHost.SetContent`), and a status label stub. Milestone 11 ("Desktop Shell") is where the Ribbon, navigation panes, notifications, and profile menu belong - building them now, before a Login/Dashboard exists to navigate between, would be building UI with nothing real to demonstrate it against.

**No `.Designer.cs` split-file forms anywhere.** Every form in this milestone (and, by the same reasoning, every later Desktop milestone) is built with a single code-only constructor rather than the classic Visual-Studio-designer-generated partial-class pattern. This environment has no interactive Windows Forms designer surface to author or visually verify a `.Designer.cs` against - hand-writing one is pure risk (subtly wrong `.ResumeLayout()`/`TableLayoutPanel` column-span serialization is a classic source of forms that compile but render wrong) with no way to catch the mistake short of a human opening the form later. A plain constructor is exactly as functional and is something a build can actually verify.

---

## 5. Exception handling

`GlobalExceptionHandler.Initialize(logger, errorDialogService)` wires all three WinForms unhandled-exception surfaces to one path: `Application.ThreadException` (UI thread), `AppDomain.CurrentDomain.UnhandledException` (any other thread), `TaskScheduler.UnobservedTaskException` (a faulted `Task` nobody awaited - explicitly marked `Observed()` after handling, so the process doesn't still terminate on some .NET versions' finalizer-thread behavior for truly unobserved task exceptions). Each path logs via the host's `ILoggerFactory` and shows `ErrorDialogForm` (message + collapsible full-exception details + copy-to-clipboard) through `IErrorDialogService`, so no unhandled exception is either silently lost or shown as a raw, unstyled framework crash dialog.

Startup-time failures (configuration validation, persistence initialization) happen *before* `GlobalExceptionHandler.Initialize` runs - `Program.cs` wraps the whole boot sequence in a `try`/`catch` that closes the splash screen and shows a plain `MessageBox` in that case, since the DI container (and therefore `IErrorDialogService`) may not have finished building.

---

## 6. Open questions for Solution Architect review

1. **`ThemeService.AvailableSkins`** enumerates every skin DevExpress ships (`SkinManager.Default.Skins`), not a curated subset. **Needs a decision**: should the product restrict the theme selector (Milestone 8/11) to a shortlist matching `04 UI UX Standards` once that document has real content (currently empty placeholders), rather than exposing every DevExpress-bundled skin?
2. **`DesktopModuleCatalog` is a static, compile-time list**, not configuration-driven. **Needs a decision**: should which modules load be a build-time decision (current) or a deployment-time one (e.g. a module list in `appsettings.json`, resolved by assembly-qualified type name)? The current design is deliberately the simpler of the two until a second real module exists to motivate the more flexible one.
3. **No `.sln` file was created**, consistent with every prior milestone in this repository (all 12 projects so far build independently via `dotnet build <csproj>`). **Needs a decision** on whether a solution file becomes worthwhile once a human is expected to open this in Visual Studio for real desktop UI work, where a `.sln`'s multi-project debugging/designer integration is more valuable than it was for library-only milestones.

---

## 7. What is deliberately *not* here

Per the milestone brief: no Login page (Milestone 8), no authentication wiring (Milestone 9), no authorization (Milestone 10), no Ribbon/navigation panes/notifications/profile menu (Milestone 11), no Dashboard (Milestone 12).

---

## 8. Desktop UI Rebuild Phase 1: professional Ribbon shell, `.cs`/`.Designer.cs` separation, POS rebuild

The Milestone 11 Shell (Section "Milestone 11 addendum" above) was functional but not commercially presentable: a single flat `AccordionControl` sidebar listing all ~30 navigation keys in one undifferentiated list, and every form built as a single code-only constructor with no `.Designer.cs` split (Section 6's original reasoning: no interactive WinForms designer surface in this environment to author or verify one against). Phase 1 of the Desktop UI rebuild replaces both, for `ShellForm`, `DashboardView`, `LoginForm`, and `RestaurantPosView` (POS rebuild covered in `RestaurantPOSArchitecture.md` Section 12). No Domain/Application/Infrastructure/Authorization code changed anywhere in this phase - every screen still calls the exact same commands/queries/policies it did before.

### 8.1 Ribbon architecture: pages, groups, commands

`ShellForm`'s `RibbonControl` is organized by business area, built almost entirely from one data table - `ShellForm.Designer.cs`'s `NavigationItems` array (`(Key, Page, Group, Caption)` per navigation key) - rather than one hand-wired `BarButtonItem` field per screen:

| Page | Groups | Notes |
|---|---|---|
| **Home** | Navigation (Dashboard); Session (profile menu - current user, Change Password, Sign Out); Recent (Recent Companies/Branches submenus); Appearance (Theme, Language); Notifications | Never permission-gated as a whole page - always visible to any signed-in user. "Recent" stands in for a "current organization/company/branch" context group (the Phase-1-brief's suggested vocabulary) - no tenant-switcher UI resolves an actual "current" org/company/branch anywhere in this codebase yet (`DesktopAdministration.md` Section 4/open question #2), so this group deliberately keeps its existing, honest "recent selections" framing rather than fabricating a "current context" display with nothing real behind it. |
| **Administration** | Security (Users, Roles); Organization (Organizations, Companies, Branches, Departments); Operations Setup (Warehouses, Terminals); Financial Setup (Fiscal Years, Currencies); Configuration (Business Settings) | |
| **Catalog** | Product Setup (Categories, Brands, Units of Measure); Products (Products, Product Variants); Identification (Barcodes); Pricing (Prices) | |
| **Inventory** | Stock (Warehouse Stocks, Inventory Transactions); Operations (Stock Adjustments, Stock Transfers) | |
| **Restaurant** | POS (Restaurant POS); Dining (Dining Areas, Tables); Orders (Running Orders, Held Orders); Kitchen (Kitchen Tickets); Closing (End of Day) | |

**No Reports page.** The Phase 1 brief explicitly forbids fake/non-functional buttons where reporting infrastructure isn't implemented; End of Day (the one real report screen) already has a natural home under Restaurant → Closing, so a near-empty "Reports" page was not created.

**Building the Ribbon from data, not thirty hand-written fields.** `ShellForm.Designer.cs`'s `BuildBusinessAreaPages()` iterates `NavigationItems` once: the first row naming a given page/group creates that `RibbonPage`/`RibbonPageGroup`, every row creates one `BarButtonItem` (`Tag` = navigation key), added to `_navigationButtonsByKey`. This is the same data-driven discipline the old `MenuLabels` dictionary already used in this class, extended to own the Ribbon's actual structure, not just display labels - the alternative (thirty near-identical `private BarButtonItem xButtonItem` fields, each with its own one-line `ItemClick` handler) would multiply the surface area for a typo without adding any traceability a shared handler doesn't already give (Section 8.3).

### 8.2 Navigation and authorization: unchanged mechanism, new display

`NavigationMenuBuilder`/`IMenuAuthorizationPolicy` (Milestone 11's mechanism, unchanged) still decide *which* keys a signed-in user may see. What changed is only how `ShellForm` displays the result: `RefreshNavigationAsync` (in `ShellForm.cs`) sets each navigation `BarButtonItem.Visibility` to `Always`/`Never` per the visible-keys set, then collapses each `RibbonPageGroup` once none of its buttons are visible, then collapses each business-area `RibbonPage` (Administration/Catalog/Inventory/Restaurant - never Home) once none of its groups are visible. A user with no Catalog permissions therefore never sees an empty "Catalog" tab, the same "prefer hiding unauthorized commands" policy Milestone 11 already established for the accordion.

**Re-navigating to the already-open screen is now a deliberate no-op** (`NavigationButtonItem_ItemClick`) - every workspace view is Transient (a fresh instance per `NavigateTo`), so without this guard, clicking "Restaurant POS" while already on it would silently replace the current view (and any order in progress) with a blank one. This is new behavior Milestone 11's accordion never needed to guard against, since nothing about the old navigation habitually invited already-there clicks the way a persistent Ribbon tab does.

### 8.3 `.cs`/`.Designer.cs` separation: what belongs where, and why this reverses Section 6's original call

Section 6 above explains why no form in this project used a `.Designer.cs` split before Phase 1: no interactive WinForms designer surface exists in this environment to author or visually verify one against, and a hand-written Designer partial that compiles but renders wrong (a classic risk with `ResumeLayout`/column-span serialization) had no way to be caught short of a human opening the form. That risk is unchanged by Phase 1 - but the Phase 1 brief explicitly requires the split for `ShellForm`, `DashboardView`, `LoginForm`, and `RestaurantPosView` regardless, trading that risk for standard WinForms maintainability (a developer with a real designer surface can now open `*.Designer.cs` and see the control tree without reading behavior code). Every split form still builds and its existing tests (`Clovent.Desktop.Tests`) still pass - see Section 10 below - but, per Section 6's own caution, only a human with an interactive designer can confirm a `.Designer.cs` partial *renders* correctly, not just compiles; this phase's own verification could only be build/test-level (Section 11).

The convention applied to all four forms:

- **`*.Designer.cs`**: every field declaration for a control, `InitializeComponent()` (or, for `ShellForm`, an equivalently-named build method taking its one external dependency), every `Build*` layout-construction method, and every event *subscription* (`control.Click += ThisForm_ControlName_Event;`) - visual structure and wiring only, never business logic, never a MediatR/EF call.
- **`*.cs`**: the constructor (DI dependencies, calls `InitializeComponent()`), every named handler's *implementation*, data loading, validation, and navigation/service calls.

One deliberate deviation from the Phase-1-brief's own worked example (`private BarButtonItem usersButtonItem; ... this.usersButtonItem.ItemClick += this.UsersButtonItem_ItemClick;`, one field/handler pair per command): `ShellForm`'s thirty navigation buttons share **one** handler, `NavigationButtonItem_ItemClick`, keyed by `BarItem.Tag` (Section 8.1) - the same Control → Event → Handler → Service traceability the worked example asks for (one hop, not thirty near-identical ones), consistent with how the pre-rebuild `AccordionControl` already routed every element through one `ElementClick` handler keyed by `Tag`.

### 8.4 UI file inventory (screens touched by Phase 1)

| Screen | Main File | Designer File | Purpose |
|---|---|---|---|
| Login | `Login/LoginForm.cs` | `Login/LoginForm.Designer.cs` | Authentication entry screen |
| Main Shell | `Shell/ShellForm.cs` | `Shell/ShellForm.Designer.cs` | Ribbon navigation, workspace host, status bar |
| Dashboard | `Dashboard/DashboardView.cs` | `Dashboard/DashboardView.Designer.cs` | Operational dashboard (KPIs, business context, activity) |
| Restaurant POS | `Restaurant/Orders/RestaurantPosView.cs` | `Restaurant/Orders/RestaurantPosView.Designer.cs` | Restaurant selling screen (see `RestaurantPOSArchitecture.md` Section 12) |

Every other Desktop screen (Users, Roles, the nine Milestone 13 management views, the eleven Milestone 14 views, the remaining nine Milestone 15 Restaurant screens/dialogs) is unchanged by Phase 1 - still a single code-only constructor, per this document's Section 28 scope boundary ("do not proceed into complete redesigns of Payment/Receipt/Dining Areas/Tables/Running Orders/Kitchen/End-of-Day unless a minimal modification is necessary").

### 8.5 Event handler inventory (representative, not exhaustive)

| Screen | Control | Event | Handler | Action |
|---|---|---|---|---|
| Shell | any navigation `BarButtonItem` (`Tag` = key) | `ItemClick` | `NavigationButtonItem_ItemClick` | `INavigationService.NavigateTo(key)`, no-op if already current |
| Shell | Sign Out `BarButtonItem` | `ItemClick` | `SignOutItem_ItemClick` → `SignOutAsync` | `ICurrentSession.SignOut()`, re-shows `LoginForm` |
| Shell | Change Password `BarButtonItem` | `ItemClick` | `ChangePasswordItem_ItemClick` → `ChangePasswordAsync` | `IMediator.Send(ChangePasswordCommand)` |
| Shell | Theme `BarEditItem` | `EditValueChanged` | `ThemeEditItem_EditValueChanged` | `IThemeService.ApplySkin(skinName)` |
| Dashboard | Refresh button | `Click` | `RefreshButton_Click` → `LoadAsync` | Re-queries every KPI/context widget |
| Login | Log In button | `Click` | `LoginButton_Click` | `ILoginService.LoginAsync(request)` only - no credential logic in the handler |
| POS | any product tile (`Tag` = variant id) | `Click` | `ProductTile_Click` | `IMediator.Send(AddOrderLineCommand)` |
| POS | New Dine-In button | `Click` | `NewDineInButton_Click` → `NewDineInAsync` | `IMediator.Send(CreateOrderCommand)` |
| POS | Pay button | `Click` | `PayButton_Click` → `PayAsync` | Opens `PaymentForm` |
| POS | Hold button | `Click` | `HoldButton_Click` | `IMediator.Send(HoldOrderCommand)` |

### 8.6 Verified: builds clean, tests pass

- `Clovent.Desktop`: 0 build warnings, 0 errors (`dotnet build src/Clovent.Desktop/Clovent.Desktop.csproj`).
- `Clovent.Desktop.Tests`: 79 tests, all passing - no test referenced `ShellForm`/`DashboardView`/`LoginForm`/`RestaurantPosView` directly (they all need a DI container/message loop), so the split/rebuild needed no test changes; `NavigationMenuBuilderTests`/`MasterDataFilterTests` and every other pure-logic suite are unaffected since the authorization/filter mechanisms themselves did not change (Section 8.2).
- **Native WinForms UI visual verification was not available** in the environment this phase was implemented in (no interactive display/designer surface to run and inspect the compiled application against) - see Section 11 below and this phase's own final report for the full caveat. Everything above this line is build/test-verified only.

### 8.7 Visual Studio Designer Compatibility

Having a `.cs`/`.Designer.cs`/`.resx` split (Section 8.3) is necessary but not sufficient for Visual Studio's WinForms Designer to actually host a form - the Designer instantiates the type being designed itself, and it does that through reflection, calling a **public parameterless constructor**. `ShellForm`/`RestaurantPosView`/`LoginForm`/`DashboardView` all only had DI-facing constructors (`LoginForm(ILoginService, IThemeService)`, `DashboardView(IServiceScopeFactory, ...)`, etc.) - with no parameterless constructor to call, the Designer could not construct an instance at all, which is why opening `LoginForm.cs [Design]` showed an essentially empty surface even though the exact same control tree renders correctly at runtime. This was fixed for **`LoginForm` and `DashboardView` only** (`ShellForm`/`RestaurantPosView` still lack Designer support - not yet in scope).

**The fix has two parts, and both matter:**

1. **A `public` parameterless constructor**, added purely for the Designer to call. It is never used at runtime: the built-in Microsoft.Extensions.DependencyInjection container (`services.TryAddTransient<LoginForm>()`/`<DashboardView>()`) always resolves the constructor whose parameters can *all* be satisfied by registered services, and always prefers the one with the most such parameters when more than one qualifies - since every DI dependency these two types need (`ILoginService`, `IThemeService`, `IServiceScopeFactory`, `ICurrentSession`, `INotificationService`, `IRecentItemsService`) is registered, the parameterless overload is strictly less resolvable and is therefore never chosen outside the Designer. This is not a service-locator workaround - nothing resolves a service from an ambient container; the parameterless constructor simply doesn't need any services at all, by construction (see point 2).

2. **Every service-dependent statement moved out of the constructor and into `Load`, guarded by `DesignMode`.** `DashboardView`'s constructor used to eagerly create a DI scope and resolve `ISessionRepository`/`ILoginAttemptRepository`/`IMediator` inline; `LoginForm`'s constructor used to call `PopulateLanguages()`/`PopulateThemes()` (the latter reading `IThemeService.AvailableSkins`) inline. Both now happen only inside their `*_Load` handler, and both handlers `return` immediately when `DesignMode` is `true` - so the Designer-constructed instance never touches a database, MediatR, or any injected service, exactly as required. The parameterless constructor can therefore pass `null!` for dependencies it will never use (`LoginForm() : this(null!, null!)`) or simply not resolve them at all (`DashboardView`'s `_scopeFactory` stays `null`, checked lazily in `EnsureServicesResolved()`), with no risk of a null-reference exception at design time.

**Why `DesignMode` in `Load`, not the constructor.** `Control.DesignMode` is well known to be unreliable when read from a constructor (the Designer hasn't sited the component yet at that point) but is reliable by the time `Load` fires - which is also, independently, already where both forms deferred DevExpress-specific and data-loading work for unrelated reasons before this fix (`LoginForm`'s `UseSystemPasswordChar` workaround; `DashboardView`'s original `Load += async (_, _) => await LoadAsync()`). No new timing was introduced - the guard was added to logic that was already `Load`-deferred, plus the two pieces (`DashboardView`'s scope creation, `LoginForm`'s `Populate*` calls) that were not yet deferred were moved there specifically for this fix.

**The general rule for every Desktop screen going forward** (not just these two):

- Static controls, layout, and event-subscription wiring belong in `*.Designer.cs`'s `InitializeComponent()` and never depend on an injected service - the Designer must be able to run this method with zero DI dependencies satisfied.
- Behavior - including *any* service-dependent initialization, however small - belongs in `*.cs`, deferred to `Load` (or a handler `Load` calls) rather than the constructor, and that deferred path must check `DesignMode` and return early before touching a service, repository, `IMediator`, or database.
- A type meant to be Designer-hosted needs a `public` parameterless constructor. It is safe to add one alongside a DI-facing constructor without weakening runtime DI, provided every field it can't populate is only ever read from behind a `DesignMode` guard (or an event handler the Designer never raises, like a `Click` handler).
- Dynamic/computed content (real KPI numbers, query results, populated combo items) is *not* required at design time - a static, unpopulated control tree ("Active Sessions: 0", an empty list box) is the correct and expected Designer view; only the control tree's existence and structure needs to survive design time, not its data.

### 8.8 Restaurant Desktop UI Rebuild Roadmap

- **Phase 1 (this phase): Application Shell + Ribbon + Dashboard + Restaurant POS.** Done - see Sections 8.1-8.6 above and `RestaurantPOSArchitecture.md` Section 12.
- **Phase 2: Payment.** Not started.
- **Phase 3: Receipt.** Not started.
- **Phase 4: Dining Areas & Tables.** Not started.
- **Phase 5: Running / Held Orders.** Not started.
- **Phase 6: Kitchen.** Not started.
- **Phase 7: End-of-Day.** Not started.
