---
title: Desktop Bootstrap Reference
type: Architecture
status: Awaiting Solution Architect review
created: 2026-07-27
updated: Milestone 11
applies_to: src/Clovent.Desktop
---

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
