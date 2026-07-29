---
title: Order Lifecycle Reference
type: Architecture
status: Awaiting Solution Architect review
created: 2026-07-28
updated: Milestone 15
applies_to: src/Clovent.Restaurant, src/Clovent.Restaurant.Application
---

# Order Lifecycle Reference

This document covers `Order`'s status machine and every Application-layer command that drives it - the detail `RestaurantPOSArchitecture.md` Sections 2, 4, and 5 summarize. See that document for `OrderTotalsCalculator` and the Table Merge/Split/Transfer/inventory-deduction reasoning; this one focuses on the transitions themselves.

---

## 1. Status vocabulary

`OrderStatus`: `Open`, `Held`, `Completed`, `Voided`, `Cancelled`. `OrderType`: `DineIn` (requires a `TableId`), `TakeAway` (must not have one) - `Order.Create` rejects the mismatched combination in either direction (`TakeAwayOrderMustNotHaveTable`/a `DineIn` order created without a table).

| From | Command | To | Notes |
|---|---|---|---|
| *(none)* | `CreateOrderCommand` | `Open` | Occupies the table if `DineIn` |
| `Open` | `HoldOrderCommand` | `Held` | |
| `Held` | `ResumeOrderCommand` | `Open` | Two-way with Hold, unlike every other transition below |
| `Open`/`Held`/`Completed` | `VoidOrderCommand` | `Voided` | Managerial override, allowed at any point including after completion; vacates the table |
| `Open`/`Held` | `CancelOrderCommand` | `Cancelled` | One-way; **blocked if any payment has been recorded** (`Order.Cancel` throws) - a cancellation with money already collected against it must be Voided instead, since Cancel implies "nothing happened" |
| `Voided`/`Cancelled` | `ReopenOrderCommand` | `Open` | Two-way, deliberately breaking the "no undo" pattern every other closing transition in this solution follows - see `RestaurantPOSArchitecture.md` Section 5. Re-seats the table if not already occupied |
| `Open` | `CompleteOrderCommand` | `Completed` | Requires `Balance <= 0.005m` (`OrderNotFullyPaid` otherwise); issues stock for every active line, vacates the table |

**Void has no "already paid" guard; Cancel does.** This is the intentional distinction between the two closing-without-completing transitions: Cancel means "this never should have existed" (blocked once money is involved, since that would silently discard a real payment), Void means "this needs to be reversed" (always allowed, including after `Completed`, for the managerial-correction case a real POS needs).

---

## 2. Notes, table assignment, and per-line operations don't change status at all

`SetOrderNotesCommand`/`SetOrderCustomerNotesCommand` (internal vs customer-facing notes - two separate fields, two separate commands, matching `Item Notes`/`Order Notes`/`Customer Notes` being three distinct capabilities in the milestone brief) and `TransferOrderTableCommand` (Section 3 below) never touch `OrderStatus`.

**`OrderLine` has its own, much smaller lifecycle**: `SetQuantity`, `SetNotes`, `Void`/`Unvoid` (two-way - a mistakenly-voided line can be restored, unlike `Order`'s mostly-one-way transitions, since voiding a line is a correction tool, not a closing action), and `Remove` (detaches the line from the order and voids it, keeping the record for audit history rather than deleting it - `RemoveOrderLineCommandHandler`'s doc comment states this explicitly).

---

## 3. Table Transfer, Merge, and Split - worked examples

**Transfer** (`TransferOrderTableCommand`): a two-top wants to move to a four-top. `Order.AssignTable(newTableId)` reassigns the order; the new table is occupied, the old one vacated (only if it differs from the new one). No line, discount, or payment data changes - purely a `TableId` swap plus the two tables' occupancy.

**Merge** (`MergeTablesCommand`): two adjacent tables' parties combine into one. The source table's open/held order is found (`GetOpenOrHeldByTableIdAsync` - in practice zero or one); the target table's order is found or created if none exists yet. Every line moves from source to target (`sourceOrder.RemoveOrderLine` + `line.TransferToOrder(targetOrder.Id)` + `targetOrder.AddOrderLine`), the now-empty source order is Cancelled (`"Merged into table {targetTableId}"` as its required reason), and the source table is vacated. **Discounts, service charges, and payments already recorded against the source order do not move** - they stay attached to the (now cancelled) source order's history. This is a known scope limitation, not an oversight: merging money already collected against two different orders into one running balance was judged out of scope for this milestone's core POS flow.

**Split** (`SplitOrderCommand`): a party splits into separate checks by table. A new order is created at the target table (always `DineIn`, inheriting the source order's `WarehouseId`); the caller-selected `OrderLineId`s move onto it the same way Merge moves lines, one at a time, with `OrderLineNotOnOrder` thrown if a caller passes a line that doesn't actually belong to the source order. The source order keeps every line not selected. Like Merge, existing discounts/service charges/payments on the source order stay with the source order - a genuine "split the bill in two" experience for a fresh table's worth of items, not a mid-order money reallocation tool.

**Split Bill (paying) is unrelated to Split (moving lines) and needs no command of its own** - see `RestaurantPOSArchitecture.md` Section 4. Multiple `RecordPaymentCommand` calls against one order, in any combination of amounts and payment methods, is what "split the bill three ways" means at the Payment level; `SplitOrderCommand` is only for physically dividing one table's order into two tables' orders.

---

## 4. Completion: the one transition that touches another bounded context

`CompleteOrderCommandHandler` is the only handler in `Clovent.Restaurant.Application` that calls into `Clovent.Inventory.Application` (see `RestaurantPOSArchitecture.md` Section 6 for the full reasoning and the "no stock record → skip" rule). The sequence, in order:

1. Load the order and recompute `OrderTotals` from its current lines/discounts/service charges/payments.
2. Throw `RestaurantDomainException.OrderNotFullyPaid` if `Balance > 0.005m`.
3. For each active (non-voided) line, look up `WarehouseStock` by `(order.WarehouseId, line.ProductVariantId)`; if found, issue exactly `line.Quantity` against it with a note referencing the order number.
4. Call `order.Complete()` (the domain method itself performs no balance check - step 2 is the only gate).
5. If the order is `DineIn`, vacate its table.

**Nothing here is transactional across the two bounded contexts** - if step 3 partially completes and then fails (e.g. a transient database error mid-loop), some lines' stock will have been issued and others not, with the order not yet marked `Completed`. This is the same honest limitation every cross-context, non-distributed-transaction integration in this solution carries; no saga or compensating-transaction mechanism exists yet anywhere in the codebase, and Milestone 15 does not introduce one.
