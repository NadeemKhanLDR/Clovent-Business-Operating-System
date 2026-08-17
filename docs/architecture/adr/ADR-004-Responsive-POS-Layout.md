# ADR-004 — Responsive POS Layout Design

## Status

Accepted

## Context

The CBOS Restaurant POS has a primary usability requirement to support low-resolution cashier displays:
- **Primary Target:** 1366×768 at 100% DPI
- **Secondary Target:** 1280×768 at 100% DPI

Previous iterations used nested panels and `AutoSize` flow layouts without strict vertical constraint bounds. Under low resolutions or high DPI scaling, the bottom payment controls collapsed entirely, and the right-hand cart actions (e.g., Void, Reopen) clipped below the screen edge with no way for a cashier to access them.

## Decision

We restructured `RestaurantPosForm` with explicit, bounded constraints and layout proportions:

1. **Root Layout Partitioning:**
   - The main workspace is managed by a master `TableLayoutPanel` (`_leftColumnLayout`) containing a 100% width, two-row grid:
     - Row 1: `SizeType.Percent, 100F` (hosts the Left Category, Center Product, and Right Cart panels).
     - Row 2: `SizeType.Absolute, 180F` (hosts the bottom payment tender strip `pnlPayment` with a constant height of 180px to prevent AutoSize collapsing).

2. **Three-Pane Column Proportions:**
   - Row 1 is partitioned horizontally via `_leftColumnLayout` columns:
     - **Left Column (Category Rail):** `Percent, 20F`. Contains vertically stacked category buttons in an autoscrolling panel.
     - **Center Column (Product Tile Wall):** `Percent, 48F`. Contains the search box, barcode input, and `_productTilesFlow` layout. Product variant tiles (168px wide) automatically wrap into 3 or 4 columns depending on the remaining horizontal space.
     - **Right Column (Current Bill / Cart):** `Percent, 32F`. Hosts the cart order grid (`dgvOrderLines`), the `pnlTotals` panel (fixed 250px height), and the lifecycle buttons.

3. **Scrolling Boundaries:**
   - Both the Center Product Tile panel and the Right Cart Totals panel are configured with `AutoScroll = true` and `AutoScrollMinSize` guidelines to prevent clipping. When buttons wrap (e.g. at 1280px widths), the container spawns vertical scrollbars instead of hiding the controls off-screen.

## Consequences

### Benefits
- **No Layout Collapse:** Fixing the bottom panel height at 180px ensures all cashier payment controls (Numeric Keypad, Quick Cash, Record Payment) are permanently visible.
- **Dynamic Columns:** Product tiles automatically reflow to maximize available width, adapting to layout resizes.
- **Accessible Cart Actions:** Auto-scrolling on the Totals panel ensures that lifecycle actions (Hold, Resume, Kitchen, Complete, Void, Reopen) can always be scrolled into view if they wrap.

### Trade-offs & Verification Limits
- **DPI-Equivalent Sizing:** Logical 1366×768 and 1280×768 layouts were exercised through DPI-equivalent runtime testing in the available 250% DPI environment. Native 100% DPI hardware verification remains pending.
- **Fixed Widths:** Proportions (20%/48%/32%) are hardcoded in the designer layout; changing them requires manually adjusting columns in `RestaurantPosForm.Designer.cs`.
