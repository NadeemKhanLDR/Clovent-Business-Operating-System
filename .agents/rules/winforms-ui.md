# WinForms & DevExpress Engineering Standards

**Scope:** `src/Clovent.Desktop/**`  
**Authoritative Reference:** [AGENTS.md](../../AGENTS.md)

---

## 1. Visual Studio Designer Safety

All Forms and UserControls in `Clovent.Desktop` must remain strictly Visual Studio Designer-compatible:
1. **Dual Constructors:**
   - Parameterized constructor for runtime execution resolving dependencies via dependency injection (`IMediator`, `ICurrentSession`, `ILogger`, etc.).
   - Parameterless constructor decorated with `[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]` forwarding `null!` to the parameterized constructor for Designer instantiation.
2. **Design-Time Guarding:**
   - In form/control `Load` event handlers and initialization pipelines, check design mode immediately:
     ```csharp
     if (Clovent.Desktop.Forms.Base.DesignModeHelper.IsInDesignMode)
     {
         return;
     }
     ```
3. **InitializeComponent Purity:**
   - `InitializeComponent()` must contain layout construction and control instantiation **only**.
   - **Strictly Prohibited in `InitializeComponent()`:**
     - Database queries or DbContext usage
     - MediatR commands or queries
     - Dependency injection container resolution
     - Asynchronous operations (`async`/`await`)
     - Business logic, LINQ queries, or complex data processing
     - Runtime security or authorization evaluations

---

## 2. Designer CodeDom Constraints vs Code-Built Views

Visual Studio parses `InitializeComponent()` using a CodeDom parser that does **not** support modern C# syntax.

### For Designer-Editable Views (`*.Designer.cs`):
- **No `var`:** Always use explicit types (`DevExpress.XtraGrid.Columns.GridColumn col = ...`).
- **No target-typed `new()`:** Always use `new DevExpress.XtraEditors.TextEdit()`.
- **No object or collection initializers:** Set properties on individual lines following instantiation.
- **No generic method calls:** Move generic operations to code-behind.
- **No lambdas or anonymous delegates:** Always wire event handlers to named private methods (`btn.Click += Btn_Click;`).
- **No loops or helper method calls:** Control creation and container addition must be written inline.

### For Code-Built Views:
Views composed programmatically using shared layout helpers (`CommandPanelLayout.Build()`, etc.) must be explicitly declared as code-only to prevent Visual Studio from attempting CodeDom parsing:
```csharp
[System.ComponentModel.DesignerCategory("Code")]
public sealed partial class CustomReportView : DevExpress.XtraEditors.XtraUserControl
```

---

## 3. High-DPI UI Standard (1920×1080 @ 200%–250% Scaling / 240 DPI)

- **Target Baseline:** 1920×1080 resolution, Windows display scaling 200%–250% (`DeviceDpi` ~240).
- **Project Configuration:**
  - `<ApplicationHighDpiMode>PerMonitorV2</ApplicationHighDpiMode>` is configured in `Clovent.Desktop.csproj`.
  - `<ForceDesignerDPIUnaware>true</ForceDesignerDPIUnaware>` in `Clovent.Desktop.csproj` ensures Visual Studio Designer stays at 96 DPI baseline and prevents coordinate multiplication on save.
  - **Do NOT reintroduce WinForms `AutoScaleMode` or `AutoScaleDimensions`:** Combining classic Font AutoScale with PerMonitorV2 causes coordinate corruption. DevExpress controls scale cleanly using skin font metrics.
- **DPI Scaling In Code:**
  - Use `Clovent.Desktop.Forms.Base.DesktopDpi.Scale(int logicalPixels, Control reference)` for all fixed pixel constants that must track font size.
  - Never apply arbitrary `Location.Y` coordinate patches.
- **Filter Bars & Toolbars:**
  - Use structured single-row `TableLayoutPanel` controls (`SizeType.AutoSize` or explicit logical sizes) instead of wrapping `FlowLayoutPanel`.
  - Set `Anchor = AnchorStyles.Left` on labels with zero vertical margins to center vertically against companion editors.
  - Standard sequence: `Location` -> `Period` -> `From` -> `To` -> Action Buttons (`Generate`, `Preview`, `Print`, `Export PDF`, `Export Excel`).
- **Shared Constants (`DesktopStyle.cs`):**
  - Toolbar control height: `DesktopStyle.ToolbarControlHeight` (30px).
  - Control gap: `DesktopStyle.ControlGap` (8px).
  - Button widths: `ButtonWidthSmall` (80px), `ButtonWidthMedium` (100px), `ButtonWidthLarge` (130px).
  - Search box width: `DesktopStyle.SearchBoxWidth` (220px).
  - Card height: `DesktopStyle.CardHeight` (110px).
- **EntityPicker (`src/Clovent.Desktop/MasterData/EntityPicker.cs`):**
  - Standard dropdown for Warehouse, Location, and Branch filters.
  - Logical width must be 210–260px (`DesktopDpi.Scale(...)` at runtime) to display long names (e.g. `"Kitchen Backup Warehouse"`) without truncation or ellipsis.
- **GridView Layout Hygiene:**
  - Set `RowHeight = DesktopDpi.Scale(28, this)` and `ColumnPanelRowHeight = DesktopDpi.Scale(32, this)`.
  - Set `MinWidth` based on measured caption text width plus glyph padding to avoid header ellipsis.
  - Disable form-level `AutoScroll` and use `ColumnAutoWidth = true` to avoid duplicate horizontal scrollbars.

---

## 4. Back Office Standards

1. **Reports Are Read-Only:** Report views provide analytics, audit trails, and financial reconciliation. Operational edits (stock receipts, price changes) belong in operational modules.
2. **Navigation Hygiene:** Provide exactly one canonical ribbon/navigation entry point per feature.
3. **Execution Claims:** Never report `LIVE UI EXECUTED` unless the application was interactively run on a display. Never report `VISUAL STUDIO DESIGNER UI EXECUTED` unless Visual Studio Designer was opened.
