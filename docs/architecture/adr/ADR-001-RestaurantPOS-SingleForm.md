# ADR-001 — Restaurant POS Single Form Architecture

## Status

Accepted

## Context

Initially, the Restaurant POS layout was split into `RestaurantPosView` (the main panel) and a separate `PaymentPanel` (the payment controls strip). This nested layout made coordinate alignment, docking, and state synchronization complex. When using `TableLayoutPanel` and `FlowLayoutPanel`, nested panels did not reliably communicate their preferred sizes to their parent containers, causing layout bugs such as the payment controls collapsing or being clipped on low-resolution displays (e.g., 1366x768). Additionally, keeping order/cart state synchronized across multiple custom controls required complex event boilerplate or shared state objects, increasing the risk of data inconsistency.

## Decision

We decided to consolidate the entire POS visual hierarchy and logic into a single class: `RestaurantPosForm` (and its designer partial `RestaurantPosForm.Designer.cs`). All controls, including the header, product discovery area, cart grid, totals panel, and the bottom payment tender strip, are declared directly within this form. State changes (like cart additions, customer changes, or payment records) directly trigger a full refresh of the form's in-memory `OrderDto` and its controls, removing partial UI state tracking.

## Consequences

### Benefits
- **Simplified Layout Management:** Standardized docking rules (e.g. `DockStyle.Left`, `DockStyle.Bottom`, `DockStyle.Fill`) inside a single form prevent nested layout engines from collapsing or miscalculating dimensions.
- **Unified State Management:** The entire screen runs around a single `OrderDto? _currentOrder` field. refreshes are transactional—no complex events or data bindings are required between nested controls.
- **Easy Hand-off and Navigation:** When a cashier logs out or logs in, the program can directly reload the form via `LoadAsync` or close it and spin up the Back Office shell, avoiding state memory leaks and duplicate windows.
- **Visual Studio Designer Safety:** Having all controls defined in one place makes it easier to preview and modify the layout in the Visual Studio Designer without breaking custom container linkages.

### Trade-offs
- **File Size:** `RestaurantPosForm.cs` and its designer file are relatively large (over 1,800 lines of code-behind). However, the clarity of having a single source of truth for the screen outweighs the complexity of maintaining multiple files.

## Alternatives Considered

- **Custom User Control Split:** Keeping `PaymentPanel` as a separate `UserControl` and custom event handlers to update the cart. This was rejected because the WinForms `TableLayoutPanel` AutoSize engine fails to query the nested `UserControl`'s preferred height accurately under different DPI configurations, leading to persistent layout collapsing.
