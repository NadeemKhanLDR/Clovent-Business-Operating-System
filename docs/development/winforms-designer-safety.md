# WinForms Visual Studio Designer Safety & CodeDom Guidelines

| Attribute | Details |
| :--- | :--- |
| **Area** | WinForms UI Architecture & IDE Tooling Safety |
| **Audience** | Frontend Developers, Desktop Engineers, AI Coding Agents |
| **Last Reviewed Version** | CBOS 1.2.2 |
| **Status** | **CANONICAL STANDARD** |
| **Classification** | **PUBLIC-SAFE** |

---

## 1. Visual Studio Designer Architecture

The Visual Studio Windows Forms Designer is not a live application runtime; it is a **CodeDom Parser & Design-Time Host**. When a developer opens a Form or UserControl in the IDE, the Designer:
1. Parses the C# code inside `InitializeComponent()` into a CodeDom syntax tree.
2. Creates an unmanaged design-time instance of the container using its **parameterless constructor**.
3. Re-serializes the visual properties back into C# code when saved.

If modern C# language constructs, dependency injection, database queries, or unhandled exceptions are present during this sequence, the Visual Studio Designer crashes with white-screen CodeDom parser errors or permanently corrupts layout coordinates.

---

## 2. Cardinal Rules for Forms & UserControls

### 2.1 The Parameterless Constructor Rule
Every `Form` and `UserControl` must provide a parameterless constructor for Visual Studio Designer instantiation:
```csharp
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
public OrderDetailsForm()
{
    InitializeComponent();
}

// Runtime Dependency Injection Constructor:
public OrderDetailsForm(IMediator mediator, ILogger<OrderDetailsForm> logger) : this()
{
    _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    _logger = logger ?? throw new ArgumentNullException(nameof(logger));
}
```

### 2.2 DesignModeHelper Guard in `Form.Load`
Runtime initialization (data loading, MediatR calls) must reside in the `Load` event or an asynchronous initialization method, protected by:
```csharp
private async void OrderDetailsForm_Load(object? sender, EventArgs e)
{
    if (Clovent.Desktop.Forms.Base.DesignModeHelper.IsInDesignMode) return;

    await LoadOrderDataAsync();
}
```

---

## 3. What Must NEVER Be in `InitializeComponent()`

`InitializeComponent()` in `*.Designer.cs` is strictly reserved for:
- Control instantiations (`new DevExpress.XtraGrid.GridControl()`)
- Property assignments (`Text`, `Size`, `Dock`, `Anchor`, `TabIndex`)
- Visual container hierarchies (`Controls.Add()`)
- Event handler wireups (`Click += ...`)

### ❌ STRICTLY FORBIDDEN IN `InitializeComponent()`:
- Service resolution (`Program.Services.GetRequiredService<...>()`)
- MediatR queries or commands (`_mediator.Send(...)`)
- Database access or Entity Framework calls
- Asynchronous tasks (`Task.Run`, `await`)
- LINQ queries or dynamic collections
- User permission or authorization checks
- Environment variable reads or file system I/O

---

## 4. CodeDom Syntax Constraints in `*.Designer.cs`

Visual Studio CodeDom parsers do not support modern C# syntax. Using modern features in `*.Designer.cs` causes instant deserialization failure:

| Modern C# Syntax (FORBIDDEN in Designer) | Required Classic Syntax in `*.Designer.cs` |
| :--- | :--- |
| ❌ `var btn = new SimpleButton();` | ✅ `DevExpress.XtraEditors.SimpleButton btn = new DevExpress.XtraEditors.SimpleButton();` |
| ❌ Target-typed `new()`: `Size = new(100, 30);` | ✅ Explicit constructor: `Size = new System.Drawing.Size(100, 30);` |
| ❌ Object Initializers: `new Button { Text = "OK" };` | ✅ Sequential assignments: `btn.Text = "OK";` |
| ❌ Lambdas in events: `btn.Click += (s, e) => Close();` | ✅ Named event handlers: `btn.Click += new System.EventHandler(this.Btn_Click);` |

---

## 5. Pure Code-Built Views (`DesignerCategory("Code")`)

For complex responsive views built entirely in code using layout helpers (such as `RestaurantPosForm`), instruct Visual Studio not to attempt visual CodeDom design by decorating the class:

```csharp
[System.ComponentModel.DesignerCategory("Code")]
public partial class RestaurantPosForm : DevExpress.XtraEditors.XtraForm
{
    // Composed purely in code with deterministic DPI scaling
}
```

---

## 6. High-DPI Scaling & Preventing Double-Scaling

1. **Disable WinForms `AutoScaleMode`:** Set `AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;` in forms to prevent WinForms from multiplying coordinates on open/save.
2. **Native DevExpress Vector Scaling:** DevExpress vector skins scale glyphs, borders, and controls automatically based on screen DPI.
3. **Runtime Coordinate Scaling:** For manual pixel offsets, use `DesktopDpi.Scale(int logicalPixels, Control reference)`.

---

## 7. Designer Execution Claim Rule

> [!WARNING]
> **VERIFICATION CLAIM RESTRICTION:**
> Never report `VISUAL STUDIO DESIGNER UI EXECUTED` unless the form was interactively opened and verified inside the Visual Studio Designer host. Structural compilation, automated layout tests, and reflection tests do **not** constitute Designer execution.

---

## 8. Cross References
- [UI & High-DPI Guidelines](ui-high-dpi.md)
- [Coding Guidelines](coding-guidelines.md)
- [ADR-003: Designer-Safe WinForms](../architecture/adr/ADR-003-Designer-Safe-WinForms.md)
- [ADR-007: Designer CodeDom Constraints](../architecture/adr/ADR-007-Designer-CodeDom-Constraints.md)
