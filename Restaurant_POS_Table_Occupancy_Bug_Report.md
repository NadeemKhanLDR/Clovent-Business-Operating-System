# Restaurant POS Table Occupancy State Bug Report

## Bug Overview
When a Dine-In order was cancelled in the Restaurant POS, the associated table (e.g. `T-03`) remained marked as `Occupied` (`T-03 (Occupied)`) in the table selection dropdown UI.

## Reproduction Steps
1. Open Restaurant POS.
2. Select Dine-In.
3. Select table `T-03`.
4. Add a menu item to the cart.
5. Verify `T-03` becomes `Occupied`.
6. Click **Cancel Order** and confirm cancellation reason.
7. Open table selector dropdown.
8. **Observed Behavior (Before Fix):** Table selector still displayed `T-03 (Occupied)` even though the current order was cancelled and cleared.

## Technical Root Cause
1. **UI Table State Stale Cache**: In `RestaurantPosForm.cs`, `CancelOrderAsync()` (and `VoidOrderAsync()`) set `_currentOrder = null` and `_tablePicker.SelectId(null)` followed by `RefreshOrderAsync()`. Because `_currentOrder` was null, `RefreshOrderAsync()` returned early without calling `ReloadTablesAsync()`. `CancelOrderAsync()` itself never called `ReloadTablesAsync()`. As a result, `_tablePicker` retained the table items array cached when the form was loaded (`"T-03 (Occupied)"`).
2. **Multi-Order Vacate Safety**: `CancelOrderCommandHandler`, `CompleteOrderCommandHandler`, and `VoidOrderCommandHandler` called `table.Vacate()` unconditionally without checking if another active (`Open` or `Held`) order was also assigned to the same table.

## Fix Implemented
1. **Domain / Application Layer**:
   - `CancelOrderCommandHandler`: Query `orderRepository.GetOpenOrHeldByTableIdAsync(tableId)` before calling `table.Vacate()`. Vacates table only if no other active orders remain.
   - `CompleteOrderCommandHandler`: Query `orderRepository.GetOpenOrHeldByTableIdAsync(tableId)` before calling `table.Vacate()`.
   - `VoidOrderCommandHandler`: Query `orderRepository.GetOpenOrHeldByTableIdAsync(tableId)` before calling `table.Vacate()`.
2. **Desktop UI Layer**:
   - `RestaurantPosForm.cs`: Added `await ReloadTablesAsync();` inside `CancelOrderAsync()` and `VoidOrderAsync()` immediately after order state transition.
3. **Automated Unit Tests**:
   - Added unit tests in `OrderHandlerTests.cs` verifying single-order cancellation vacates table, multi-order cancellation preserves occupancy when active order remains, voiding multi-order preserves occupancy, and take-away orders do not occupy tables.

## Database & UI Behavior
- **Database:** `Table.OccupancyStatus` transitions from `Occupied` to `Available` upon order cancellation (or remains `Occupied` if another active order exists) and is persisted via EF Core `UnitOfWorkBehavior`.
- **UI:** `TablePickerEdit` reloads table items immediately from database upon cancellation and displays `T-03 (Available)` in the dropdown list and `Table: None` on the header.

## Automated Test Results
- `Clovent.Restaurant.Application.Tests`: 155 Passed, 0 Failed.
- `Clovent.Restaurant.Infrastructure.Tests`: 46 Passed, 0 Failed.
- `Clovent.Restaurant.Tests`: 128 Passed, 0 Failed.
- `Clovent.Desktop.Tests`: 248 Passed, 0 Failed.

## Live UI Verification Status
**PASS — AUTOMATED TEST SUITE (Live UI Acceptance: PENDING MANUAL ACCEPTANCE)**
