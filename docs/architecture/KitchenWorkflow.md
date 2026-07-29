---
title: Kitchen Workflow Reference
type: Architecture
status: Awaiting Solution Architect review
created: 2026-07-28
updated: Milestone 15
applies_to: src/Clovent.Restaurant, src/Clovent.Restaurant.Application, src/Clovent.Desktop
---

# Kitchen Workflow Reference

This document covers `KitchenTicket`'s lifecycle and the Send to Kitchen flow - the piece of `RestaurantPOSArchitecture.md`'s aggregate list that has no Kitchen Display System behind it (explicitly out of scope this milestone), only a ticket record and a viewer screen.

---

## 1. A ticket is a snapshot, not a live view of the order

**`SendOrderToKitchenCommand` copies the order's currently-active (non-voided) `OrderLineId`s onto a new `KitchenTicket` at send-time** - `KitchenTicket.OrderLineIds` is a fixed list from that moment, not a live query against the order's current lines. Lines added to the order *after* a ticket is sent need a second `SendOrderToKitchenCommand` call, producing a second ticket; they are never retroactively added to an already-sent ticket. This mirrors a real kitchen workflow: a ticket already in the kitchen queue is a physical (or KDS-equivalent) artifact the cooking staff is acting on, not something the POS can silently rewrite out from under them.

**There is no limit on how many tickets one order can accumulate** - a table that orders an appetizer, then a main course twenty minutes later, then dessert, sends three separate tickets against the same `OrderId`. `ListKitchenTicketsByOrderQuery` retrieves all of them; `ListActiveKitchenTicketsQuery` (the Kitchen Ticket Viewer's data source) retrieves only those not yet `Served` or `Cancelled`, across every order.

---

## 2. Status vocabulary

`KitchenTicketStatus`: `New`, `InProgress`, `Ready`, `Served`, `Cancelled`.

| From | Command | To | Timestamp recorded |
|---|---|---|---|
| *(none)* | `SendOrderToKitchenCommand` | `New` | `CreatedAtUtc` |
| `New` | `StartKitchenTicketCommand` | `InProgress` | `StartedAtUtc` |
| `InProgress` | `MarkKitchenTicketReadyCommand` | `Ready` | `ReadyAtUtc` |
| `Ready` | `ServeKitchenTicketCommand` | `Served` | `ServedAtUtc` |
| `New`/`InProgress` | `CancelKitchenTicketCommand` | `Cancelled` | *(none - domain records no cancellation timestamp)* |

Every transition is one-way and strictly sequential - there is no "send back to New" or "unserve." A ticket started in error is Cancelled, not reverted, the same "no undo, a mistake is corrected by a new record" discipline every other propose-then-commit workflow in this solution follows (`InventoryArchitecture.md` Section 3).

**`Serve` requires `Ready` and `Cancel` requires `New`/`InProgress`** - a ticket cannot be served before it's marked ready, and cannot be cancelled once serving has already happened (a served ticket represents food that has already left the kitchen; "cancelling" it at that point would be a Void on the order's payment side, not a kitchen-ticket state change).

---

## 3. Desktop: Kitchen Ticket Viewer

`KitchenTicketViewerView` is a `MasterDataListView<KitchenTicketDto>`-based read-and-act screen (`RestaurantPOSArchitecture.md` Section 9), listing every active ticket with its parent order's `OrderNumber` (resolved per ticket via `GetOrderByIdQuery`, since `KitchenTicketDto` carries only `OrderId`), line count, status, and the three lifecycle timestamps. Four extra actions - Start, Mark Ready, Serve, Cancel - each gated both by `MasterDataListAction<TDto>.IsEnabledFor` (matching Section 2's table: Start only enabled while `New`, Mark Ready only while `InProgress`, Serve only while `Ready`, Cancel only while `New`/`InProgress`) and by `feature.kitchentickets.{operation}` permission, the same double gate (domain-state-allows AND user-is-permitted) every list-view action in this solution uses.

**The POS screen's own "Send to Kitchen" button is the only way a ticket comes into existence** - the Kitchen Ticket Viewer has no "New" action of its own, since a kitchen ticket without a source order and set of lines is meaningless. This mirrors `WarehouseManagement.md` Section 5's "no button wired to a command that doesn't exist" discipline: there genuinely is no `CreateKitchenTicketCommand` independent of `SendOrderToKitchenCommand`.
