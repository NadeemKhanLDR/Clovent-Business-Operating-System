# ADR-007 — Designer CodeDom Constraints and Code-Built Views

## Status

Accepted

## Context

[ADR-003](ADR-003-Designer-Safe-WinForms.md) addressed one class of Visual Studio WinForms
Designer failure: the Designer *instantiates* form classes, so constructors that resolve DI
services or hit the database crash it. That analysis was correct but incomplete.

Manual testing in Visual Studio surfaced a second, unrelated class of failure:

- `CustomerLedgerDialog.Designer.cs` — *"The designer cannot process the code at line 335"*
- `AppearanceRuleEditForm.Designer.cs` — *"The designer cannot process the code at line 81"*
- `CustomerPaymentForm` — opened, but rendered only one control while the Properties window
  listed many

The cause is not instantiation. Before running anything, the Designer **parses**
`InitializeComponent()` with a CodeDom parser, which supports only a restricted subset of C#.
It cannot represent:

| Construct | Example |
|---|---|
| `var` | `var col = view.Columns.AddVisible(...)` |
| Target-typed `new()` | `private readonly GridControl _grid = new();` |
| Object/collection initializers | `new TableLayoutPanel { Dock = DockStyle.Fill }` |
| Generic method invocation | `Enum.GetNames<AppearanceScopeType>()` |
| Lambdas / anonymous handlers | `btn.Click += (_, _) => DoThing();` |
| Loops | `foreach (var b in buttons) { ... }` |
| Helper methods called from `InitializeComponent` | `BuildLayout();` |

On encountering one, the Designer aborts parsing and reports the line number. Anything not yet
parsed is simply absent from the surface — which is exactly the "only the Cancel button renders"
symptom, not a layout bug.

The line numbers corroborate this: `AppearanceRuleEditForm` reported line 81, and line 82 was
`_scopeTypeCombo.Properties.Items.AddRange(Enum.GetNames<AppearanceScopeType>());`.

Auditing all 86 `*.Designer.cs` files under `src/Clovent.Desktop` showed these constructs are
not isolated defects. A substantial subset of these files are **hand-written layout code that
happens to carry the `.Designer.cs` suffix**, using shared layout helpers
(`CommandPanelLayout.Build(...)`, `BuildStatCard(...)`), object initializers, and lambda
handlers throughout. `PaymentHistoryDialog.Designer.cs` documents this in its own header
comment. These files were never Designer-generated and cannot round-trip through the Designer.

## Decision

We split the Desktop views into two explicitly-labelled categories.

### 1. Designer-editable views

Files genuinely shaped like Designer output stay Designer-editable, and their
CodeDom-hostile constructs were removed:

- `var` replaced with explicit types (`DevExpress.XtraGrid.Columns.GridColumn colDate = ...`)
- Field initializers (`= new() { ... }`) moved into `InitializeComponent()` as plain
  construction plus property assignments
- Generic method calls moved out of `InitializeComponent()` into code-behind initializers

Rules for these files, in addition to those in ADR-003:

- No `var`, no target-typed `new()`, no object or collection initializers
- No generic method invocations
- No lambdas — event handlers wire to named methods (`_btn.Click += Btn_Click;`)
- No loops, no LINQ, no helper methods called from `InitializeComponent()`
- Every control is constructed in `InitializeComponent()` and added to a parent there

### 2. Code-built views

Views whose layout is genuinely composed in code are marked:

```csharp
[System.ComponentModel.DesignerCategory("Code")]
public sealed partial class EndOfDayReportView : XtraUserControl
```

Visual Studio then opens them in the code editor instead of attempting a Designer load, so the
parser is never invoked and no error appears. This is a declaration of intent, not a
workaround: these views are maintained as code and their shared layout helpers are a deliberate
abstraction worth keeping.

Views marked code-built:

`EndOfDayReportView`, `PaymentHistoryDialog`, `MainForm`, `RestaurantSetupView`,
`AppearanceSettingsView`, `BusinessSettingsManagementView`, `EntityPicker`,
`OrganizationHierarchySelector`, `ReceiptPreviewForm`

## Consequences

### Benefits

- The two reported parse failures are addressed at their root cause.
- Code-built views no longer present a broken Designer surface that invites "fixing".
- Shared layout helpers survive; no large rewrite of working screens was undertaken.
- The category a view belongs to is now explicit in source rather than assumed.

### Trade-offs

- Code-built views are not visually editable. This was already true in practice; the attribute
  makes it honest.
- Designer-editable files are more verbose (explicit types, expanded initializers).
- The split must be maintained: a new hand-coded view needs the attribute, and a
  Designer-editable file must not acquire modern C# syntax. See
  [DesktopUILayout.md](../DesktopUILayout.md).

## Verification Status

- **Source review:** PASS — all 86 `*.Designer.cs` files audited for the constructs above.
- **Build:** PASS — Release build, 0 errors, 0 warnings.
- **Automated tests:** PASS.
- **Visual Studio Designer:** **NOT VERIFIED.** No Designer instance was opened as part of this
  work. The fixes target the documented CodeDom constraints and the observed error lines, but
  confirmation requires opening each form in Visual Studio. See
  [RestaurantPOSManualQA.md](../../testing/RestaurantPOSManualQA.md) for the list to check.

## Revision — 2026-08-13 (QA defect remediation)

The Verification Status above was **overstated and is superseded.**

An independent QA pass (`D:\FCCReports\CBOS_QA_Report_2026-08-13_131207.md`) opened every
Designer-editable form in a real Visual Studio 18 Enterprise instance. Two findings matter here:

1. **The source audit was incomplete.** `RestaurantPosForm.Designer.cs` was missed. It broke three
   of this ADR's own rules — `nameof()` (lines 460, 461, 471–473), object initializers
   (471–473), and 29 helper-method calls inside `InitializeComponent()` — and it was the **only**
   form in the solution that actually failed to load: *"The designer cannot process the code at
   line 460."* Reproduced in a fresh VS process. "Source review: PASS — all 86 files audited" was
   therefore wrong.

2. **A flagged construct does not predict a Designer failure.** Of 86 `*.Designer.cs` files, 37
   contain at least one flagged construct; only one failed to load. `ActivityLogView` uses
   `nameof()` and opens fine. The rules in this ADR remain the right convention to code to, but
   pattern presence must not be reported as a defect, and pattern absence must not be reported as
   verification.

`RestaurantPosForm.Designer.cs` was brought into compliance in the remediation pass: literals
instead of `nameof()`, declared fields plus property assignments instead of object initializers,
and every helper call written out inline. `StyleCategoryButton` moved to `RestaurantPosForm.cs`
because the category buttons built at runtime still need it; the other four helpers had no runtime
caller and were deleted.

### Verification Status (2026-08-13, superseding the section above)

- **Source review:** PASS for the constructs this ADR names, now including `RestaurantPosForm`.
- **Build:** PASS — Release build, 0 errors, 0 warnings.
- **Automated tests:** PASS — 1113 total / 1113 passed / 0 failed / 0 skipped.
- **Visual Studio Designer:** **PENDING CLAUDE QA.** A clean compile is not evidence that a form
  loads in the Designer, and is not offered as any. ~30 flagged management views were never opened
  individually and remain **NOT VERIFIED**.
