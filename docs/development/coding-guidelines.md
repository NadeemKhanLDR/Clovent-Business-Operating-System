# CBOS Engineering & Coding Guidelines

| Attribute | Details |
| :--- | :--- |
| **Area** | Software Engineering Standards & Code Quality |
| **Audience** | All Software Engineers, AI Coding Agents, Reviewers |
| **Last Reviewed Version** | CBOS 1.2.2 |
| **Status** | **CANONICAL STANDARD** |
| **Classification** | **PUBLIC-SAFE** |

---

## 1. Clean Architecture & Bounded Context Boundaries

All code contributions must strictly adhere to Clean Architecture boundaries:
1. **Domain Layer:** Zero dependencies on external packages, EF Core, or presentation libraries. Entities protect invariants through internal encapsulation.
2. **Zero Cross-Context Domain References:** An aggregate in `Restaurant` cannot reference an entity in `Inventory`. Cross-context references are logical IDs (`Guid` or value objects).
3. **Application Layer:** Use MediatR Commands (`IRequest<Result>`) for state mutations and Queries (`IRequest<Result<T>>`) for reads.
4. **No Arbitrary Classes in Root Folders:** Place files in strictly categorized feature folders:
   - Commands & Handlers -> `src/Clovent.<Context>.Application/<Feature>/Commands/`
   - Queries & DTOs -> `src/Clovent.<Context>.Application/<Feature>/Queries/`
   - Entities & Value Objects -> `src/Clovent.<Context>/<Feature>/`
   - EF Configurations -> `src/Clovent.<Context>.Infrastructure/Persistence/Configurations/`
   - Forms & UserControls -> `src/Clovent.Desktop/Forms/<Context>/` or `src/Clovent.Desktop/<Context>/`

---

## 2. Asynchronous Programming & Cancellation

1. **Always Propagate `CancellationToken`:** Every async method must accept `CancellationToken cancellationToken = default` and pass it to downstream EF Core or I/O calls.
2. **Use `ConfigureAwait(false)` in Non-UI Libraries:** All async operations in Application and Infrastructure layers must append `.ConfigureAwait(false)` to prevent thread deadlocks.
3. **Never Use `async void`:** Permitted **only** on top-level WinForms UI event handlers (e.g. `button_Click(object? sender, EventArgs e)`). Wrap the method body in a `try/catch` block.

---

## 3. Financial Calculations & Monetary Precision

1. **Strictly `decimal` Type:** Never use `float` or `double` for currency, prices, taxes, or discounts.
2. **Half-Cent Slack Tolerance (`BalanceEpsilon`):** When evaluating whether a bill balance is settled, compare against `PosPaymentRules.BalanceEpsilon` (`0.005m`):
   ```csharp
   public static bool IsSettled(decimal balance) => balance <= 0.005m;
   ```
3. **Truth in Costing & Profit Margins:**
   - If recipe/BOM cost is unknown for a prepared menu item, cost must be `null` and displayed as `"N/A"`.
   - Never fake gross profit by replacing unknown costs with `0.00`.
   - Display `"Known GP: N/A"` or `"Known GP: {sum}"`.

---

## 4. Entity Framework Core Persistence Guidelines

1. **Fluent API Exclusively:** Relational schema configurations belong exclusively in `IEntityTypeConfiguration<T>` classes. Do not use data annotations (`[Table]`, `[Column]`) on domain entities.
2. **Field Backing for Navigations:** Private backing fields (e.g. `private readonly List<OrderLine> _lines = new();`) must be bound using `.UsePropertyAccessMode(PropertyAccessMode.Field)`.
3. **No Tracking for Read Queries:** Always append `.AsNoTracking()` to EF Core queries in query handlers.

---

## 5. WinForms, DevExpress & UI Standards

1. **Visual Studio Designer Safety:** Forms must include parameterless constructors marked `[EditorBrowsable(EditorBrowsableState.Never)]` and guard `Form.Load` with `if (DesignModeHelper.IsInDesignMode) return;`.
2. **Never Put Business Logic in `InitializeComponent()`:** Zero DI resolutions, zero MediatR dispatches, zero database queries.
3. **Use Central Formatters:** Never use hardcoded strings like `"yyyy-MM-dd"` or `"C"`. Use `BusinessDateFormatter`, `BusinessTimeFormatter`, `CurrencyDisplay`, and `QuantityDisplay`.
4. **High-DPI Alignment:** Scale logical pixel constants using `DesktopDpi.Scale(int pixels, Control reference)`.

---

## 6. Logging & Error Handling

1. **Structured Logging:** Use `ILogger<T>` with message templates:
   ```csharp
   logger.LogInformation("Order {OrderNumber} completed successfully.", order.Number);
   ```
2. **Sanitize Sensitive Data:** Never log passwords, PIN hashes, cardholder data, or RSA private keys.
3. **Clean Global Exception Handling:** In UI threads, unhandled exceptions must pass through `GlobalExceptionHandler` to present user-friendly error dialogs without crashing the desktop shell.

---

## 7. Cross References
- [WinForms Designer Safety](winforms-designer-safety.md)
- [UI & High-DPI Guidelines](ui-high-dpi.md)
- [Testing Strategy](../testing/testing-strategy.md)
- [Project Structure](project-structure.md)
