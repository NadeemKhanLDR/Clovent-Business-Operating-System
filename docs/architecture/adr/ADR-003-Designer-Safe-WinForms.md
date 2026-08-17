# ADR-003 — Designer-Safe WinForms Architecture

## Status

Accepted — **extended by [ADR-007](ADR-007-Designer-CodeDom-Constraints.md)**

> **Incomplete on its own.** This ADR covers Designer failures caused by *instantiation*
> (constructors resolving DI services, touching the database, or reading auth state). Manual
> Visual Studio testing on 2026-08-13 surfaced a second, independent class of failure caused by
> *parsing*: the Designer reads `InitializeComponent()` with a CodeDom parser that rejects
> `var`, target-typed `new()`, object initializers, generic method calls, lambdas, and loops.
> Following this ADR alone does not make a form Designer-compatible. See ADR-007.

## Context

Visual Studio's WinForms Designer instantiates form and control classes directly to render them at design time. It executes the parameterless constructor and parses the `InitializeComponent()` method. If this code touches the database, resolves dependencies from a dependency injection container (which isn't running in the Designer), performs asynchronous operations, or accesses authentication state, the Designer crashes (often showing `NullReferenceException` or `LoaderExceptions`) and blocks the developer from using visual layout tools.

## Decision

We enforced a strict split between design-time layout and runtime initialization across all WinForms classes, particularly `LoginForm` and `RestaurantPosForm`:

1. **Dual Constructors:**
   - A public parameterized constructor is used at runtime, resolving dependencies from the DI container (e.g., `IMediator`, `ILoginService`, `IThemeService`).
   - A parameterless constructor decorated with `[EditorBrowsable(EditorBrowsableState.Never)]` is maintained exclusively for Visual Studio Designer use. It passes `null!` to the parameterized constructor.
2. **Design-Time Guarding:**
   - Any runtime initialization, data loading, or event subscriptions that require dependencies must be deferred to the form's `Load` event handler.
   - The first line of the `Load` handler (and any other initialization methods) must check `Clovent.Desktop.Forms.Base.DesignModeHelper.IsInDesignMode` and return immediately if true:
     ```csharp
     if (DesignModeHelper.IsInDesignMode)
     {
         InitializeDesignTime();
         return;
     }
     ```
3. **Predefined Design-Time Data:**
   - A dedicated static class `RestaurantPosDesignDataProvider` supplies mock warehouses, tables, menu categories, product variants, and sample cart data to populate UI controls during design-time layout, avoiding empty/collapsed grid panels in the Designer.
4. **Clean Code Generation and Property Declarations:**
   - Designer files must restrict control properties to static types or plain fields (e.g. `private static readonly Color PosAccentColor = Color.FromArgb(...)`) instead of inline expressions inside `InitializeComponent()`, which the Designer's parser cannot reliably round-trip.
5. **No DbContext/Async Database Calls in UI Construction:**
   - Database queries are restricted to MediatR queries dispatched asynchronously during runtime `LoadAsync` or in response to events, never during control instantiation.

## Consequences

### Benefits
- **Zero Designer Crashes:** Developers can open `RestaurantPosForm` and `LoginForm` in Visual Studio without design-time compilation or container errors.
- **Visual Mockups:** Pre-seeded design-time data allows the layout to look exactly like the running application within the Visual Studio Designer.
- **Maintainable Code-behind:** Code-behind is strictly limited to user interaction, while DI scopes manage the business service lifetimes.

### Trade-offs
- **Boilerplate Constructor:** The requirement to maintain a parameterless constructor that forwards nulls increases boilerplate code.
- **Designer Logging:** Exceptions occurring during design-time instantiation are caught and logged to `d:\Clovent Business Operating System\designer_exception.txt` to help troubleshoot designer-only issues.

## Verification Status
- **Designer compatibility code-reviewed but not runtime-verified in Visual Studio Designer.** The files were inspected to confirm compliance with all designer constraints (including dual constructors, null forwarding, and DesignMode guards), but they were not run or verified within the actual Visual Studio Designer interface.
