# CBOS Restaurant POS Architecture

| Attribute | Details |
| :--- | :--- |
| **Area** | Point of Sale & Front-of-House Workflows |
| **Audience** | UI Engineers, Domain Developers, QA Engineers, Support |
| **Last Reviewed Version** | CBOS 1.2.2 |
| **Status** | **IMPLEMENTED** |
| **Classification** | **PUBLIC-SAFE** |

---

## 1. Overview & 3-Column Layout Concept

The front-of-house point-of-sale register is the primary revenue-generating touchpoint in CBOS. In accordance with [ADR-001 (Restaurant POS Single Form Architecture)](../architecture/adr/ADR-001-RestaurantPOS-SingleForm.md), all cashier workflows are unified within a single, highly-optimized container: `RestaurantPosForm` (`src/Clovent.Desktop/Restaurant/Orders/RestaurantPosForm.cs`).

The interface is structured in a responsive **3-Column Layout** optimized for speed, visual clarity, and high-throughput cashier operations:

```
+---------------------------------------------------------------------------------------------------+
| TOP BAR: Operator Profile | Branch & Terminal Badges | Active Shift # | Rush Mode | Diagnostics   |
+--------------------------+------------------------------------------+-----------------------------+
| LEFT COLUMN (20% Width)  | CENTER COLUMN (48% Width)                | RIGHT COLUMN (32% Width)    |
| - Categories Tree / Rail | - Search / Universal Barcode Input       | - Active Order Number & Mode|
| - Dining Mode Selector   | - Quick Orders Preset Toolbar            | - Dining Table / Customer   |
|   (Dine-In, Take Away,   | - Flow Product Variant Grid              | - Interactive Cart Grid     |
|    Delivery)             |   (Tiles with Image, Sku, Price)         |   (Qty, Modifiers, Price)   |
| - Table Layout Switcher  | - Smart Combo & Recommendations Rail     | - Subtotals, Discounts, Tax |
| - Active Orders Rail     |                                          | - BOTTOM TENDER STRIP       |
|   (Wait Time Health)     |                                          |   (Cash, Card, Account)     |
+--------------------------+------------------------------------------+-----------------------------+
```

### 1.1 Left Column: Navigation & Operational Context
- **Dining Mode Selector:** Instant toggling between `Dine-In` (table assignment), `Take Away` (counter pickup), and `Delivery` (driver dispatch).
- **Categories Selector:** Fast filtering of menu items by hierarchical categories.
- **Active Orders Rail:** Collapsible sidebar showing open tables and pending tickets with visual wait-time health indicators:
  - Green: < 10 minutes wait
  - Orange: 10–20 minutes wait
  - Red: > 20 minutes wait

### 1.2 Center Column: Catalog Discovery & Rapid Entry
- **Universal Scanner & Search Input:** Immediate autofocus on barcode scan; debounced search filtering by item name, SKU, or shortcut code.
- **Quick Orders Toolbar:** Template-driven one-click preset buttons for top-volume items.
- **Product Variant Tiles:** Touch-friendly buttons displaying item name, variant (e.g. Regular/Large), price, and stock status.
- **Smart Combo & Recommendation Engine:** Rule-based suggestions offering upsell combinations and complement items based on active cart contents.

### 1.3 Right Column: Active Cart & Tender Strip
- **Order Header:** Displays order number, dining type, assigned table, and customer account.
- **Interactive Cart Grid:** Real-time quantity adjustment (`+` / `-`), line notes, line item discounts, and line voiding.
- **Summary Footers:** Immediate display of Subtotal, Order Discount, Service Charge, Tax, and Net Total.
- **Bottom Payment Tender Strip ([ADR-002](../architecture/adr/ADR-002-Payment-Tender-Strip.md)):** One-click payment buttons (`Cash`, `Card`, `On Account`, `Split`) with quick cash preset buttons for fast drawer turnaround.

---

## 2. Advanced POS Capabilities

### 2.1 Rush Mode
During peak meal periods, cashiers can toggle **Rush Mode**. This compresses tile margins, increases grid row density, and hides secondary diagnostic panels, maximizing screen real estate for rapid checkout.

### 2.2 Cart Crash / Session Recovery
To protect against accidental application closure or sudden hardware power loss, the active cart state is persisted to a local session file on every line mutation. Upon restarting CBOS, if an uncommitted cart exists for the terminal, the cashier is offered immediate session recovery.

### 2.3 Table Layout & Guest Management
In Dine-In mode, cashiers can switch to the interactive **Table Floor Plan View**, visually inspecting table availability, seated guest counts, elapsed dining times, and unpaid bill amounts across dining areas.

---

## 3. Key Classes & Source Traceability

- **Main POS Form:** `src/Clovent.Desktop/Restaurant/Orders/RestaurantPosForm.cs`
- **Tender Strip Rules:** `src/Clovent.Desktop/Restaurant/Orders/PosPaymentRules.cs`
- **Cart Presentation:** `src/Clovent.Desktop/Restaurant/Orders/OrderCartPresenter.cs`
- **Smart Combos:** `src/Clovent.Desktop/Restaurant/SmartPos/SmartComboBuilderForm.cs`
- **Table Management:** `src/Clovent.Desktop/Restaurant/Tables/TableManagementView.cs`
- **POS Preferences:** `src/Clovent.Desktop/Forms/Base/PosSettingsStore.cs`

---

## 4. Cross References
- [Order Lifecycle Documentation](order-lifecycle.md)
- [Payments & Settlement Architecture](payments.md)
- [Shifts & Cash Management](shifts-and-cash-management.md)
- [ADR-001: POS Single Form Layout](../architecture/adr/ADR-001-RestaurantPOS-SingleForm.md)
- [ADR-002: Integrated Payment Tender Strip](../architecture/adr/ADR-002-Payment-Tender-Strip.md)
