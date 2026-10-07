# CBOS Order Lifecycle & Workflow State Machine

| Attribute | Details |
| :--- | :--- |
| **Area** | Restaurant Operations & Sales Workflow |
| **Audience** | Backend Engineers, QA Engineers, Support Technicians |
| **Last Reviewed Version** | CBOS 1.2.2 |
| **Status** | **FACTUAL BASELINE** |
| **Classification** | **PUBLIC-SAFE** |

---

## 1. Order Status State Machine

The lifecycle of a customer order in the `Restaurant` bounded context is modeled by the `Order` aggregate root (`src/Clovent.Restaurant/Orders/Order.cs`) and governed by the `OrderStatus` enum (`src/Clovent.Restaurant/Orders/OrderStatus.cs`):

```mermaid
stateDiagram-v2
    [*] --> Open: Order.Create(OrderType, Terminal, Branch)
    
    state Open {
        [*] --> Building
        Building --> Building: AddLine / RemoveLine / PriceOverride
        Building --> Building: ApplyDiscount / ApplyServiceCharge
        Building --> Building: SendToKitchen (KitchenTicket Generated)
        Building --> Building: RecordPayment (Partial / Split Tender)
    }

    Open --> Held: Order.Hold(Reason)
    Held --> Open: Order.Resume() / Recall
    
    Open --> Cancelled: Order.Cancel() (No payments recorded)
    Open --> Voided: Order.Void(ManagerPin, Reason)
    Held --> Voided: Order.Void(ManagerPin, Reason)
    
    Open --> Completed: All payments settled (Balance <= 0.005m)
    
    note right of Completed
        Completed is an immutable financial state.
        Voiding or refunding completed orders
        is PROHIBITED in the 1.2.3 pilot.
        Formal Refund aggregate scheduled for 1.3.0.
    end note
    
    Completed --> [*]
    Voided --> [*]
    Cancelled --> [*]
```

---

## 2. Order States Reference

### 2.1 `Open`
- **Definition:** The active working state. Lines, quantities, modifiers, discounts, and payments may be continuously added, adjusted, or removed.
- **Allowed Operations:**
  - `AddLine(ProductVariantId, Quantity, UnitPrice)`
  - `UpdateLineQuantity(OrderLineId, NewQuantity)`
  - `OverrideLinePrice(OrderLineId, NewPrice, ManagerPin)`
  - `VoidLine(OrderLineId, Reason)`
  - `ApplyDiscount(DiscountType, Amount)`
  - `ApplyServiceCharge(Percentage)`
  - `AssignTable(TableId)`
  - `AssignCustomer(CustomerId)`
  - `SendToKitchen()` -> Generates kitchen tickets for unprinted lines.
  - `RecordPayment(PaymentMethodId, Amount)` -> Adds tender records.

### 2.2 `Held`
- **Definition:** Temporarily suspended order. Occurs when a dining party changes tables, steps away from the counter to review their order, or a drive-thru vehicle is pulled forward.
- **Allowed Operations:**
  - `Order.Resume()` returns the order to `Open` state for modification or settlement.
  - `Order.Void()` with managerial justification.

### 2.3 `Completed`
- **Definition:** Fully paid and finalized check.
- **Completion Trigger:** Automatically achieved when `PaidAmount + OnAccountAmount >= TotalAmount` within a half-cent tolerance (`BalanceEpsilon = 0.005m`).
- **Post-Completion Invariants:** Strictly immutable. No new order lines or payment adjustments can be added.
- **Outbox Side Effects:** Triggers generation of `QuickBooksSync`, `ReceiptPrint`, and `InventoryPosting` messages in the Transactional Outbox.

### 2.4 `Voided`
- **Definition:** Invalidated order before completion. Can occur from `Open` or `Held`.
- **Pilot Governance Rule (1.2.3):** Voiding already `Completed` orders is strictly prohibited during the attended single-terminal pilot to protect ledger and reporting integrity.
- **Audit Rules:** Requires mandatory manager authorization and free-text justification reason. Historical records remain in the database for financial audit inspection; rows are never physically deleted.

### 2.5 `Cancelled`
- **Definition:** Abandoned order discarded before any payment was recorded (e.g. customer walked away before paying at the counter).
- **Rule:** Permitted only if zero payments have been posted to the check.

---

## 3. Critical Notice on Returns & Refunds

> [!WARNING]
> **REFUND DOMAIN NOT YET IMPLEMENTED**
>
> In CBOS 1.2.2 / 1.2.3, a dedicated **Refund / Return Aggregate** is **NOT** implemented in the domain layer.
> - Customer return slips, credit vouchers, partial item returns against completed orders, and refund tender transactions are not currently modeled in the codebase.
> - **Pilot Rule (CBOS 1.2.3):** Voiding or pseudo-refunding completed orders is prohibited during the attended single-terminal pilot to prevent corrupting completed financial ledgers and day-close figures.
> - **Roadmap Status:** A formal compensating Refund & Return domain aggregate (with inventory restock credit, credit vouchers, and reversing ledger entries) is scheduled for **CBOS 1.3.0**. Do not present refund features as available in current production deployments.

---

## 4. Kitchen Ticket Dispatching

When an order in the `Open` state has new prepared items added, the cashier executes `Send to Kitchen`:
1. Collects unprinted order lines belonging to kitchen-routed categories.
2. Creates an immutable `KitchenTicket` record.
3. Formats course tickets (Appetizer, Main, Dessert) with line-level chef notes.
4. Kitchen tickets are dispatched to the kitchen display / ticket printer via the Transactional Outbox.

---

## 5. Key Classes & Source Traceability

- **Aggregate Root:** `src/Clovent.Restaurant/Orders/Order.cs`
- **Status Enum:** `src/Clovent.Restaurant/Orders/OrderStatus.cs`
- **Line Entity:** `src/Clovent.Restaurant/OrderLines/OrderLine.cs`
- **Domain Events:** `src/Clovent.Restaurant/Orders/Events/` (`OrderCreated`, `OrderLineAdded`, `OrderPaymentRecorded`, `OrderCompleted`, `OrderVoided`, `OrderCancelled`)
- **Payment Rules:** `src/Clovent.Desktop/Restaurant/Orders/PosPaymentRules.cs`

---

## 6. Cross References
- [Restaurant POS Architecture](pos-architecture.md)
- [Payments & Settlement Architecture](payments.md)
- [Shifts & Cash Management](shifts-and-cash-management.md)
- [Transactional Outbox Architecture](../architecture/transactional-outbox.md)
- [Known Limitations](../known-limitations.md)
- [PDR-0003: Completed Financial Immutability](../product/pdr/PDR-0003-completed-financial-transaction-immutability.md)
