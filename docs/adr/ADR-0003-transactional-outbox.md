# ADR-0003: Transactional Outbox for Asynchronous Integration

| Attribute | Details |
| :--- | :--- |
| **Status** | **ACCEPTED** |
| **Date** | 2026-10-05 (Milestone 15) |
| **Deciders** | Architecture Working Group |
| **Area** | Asynchronous Messaging & Reliability |

---

## 1. Context & Problem Statement

Completing an order in Restaurant POS requires updating central tables, dispatching invoices to QuickBooks, printing thermal customer receipts, updating warehouse inventory ledgers, and recording analytics.

Executing these integrations synchronously within the cashier's checkout thread creates critical failure modes:
1. If the receipt printer is out of paper or disconnected, the cashier's checkout is blocked.
2. If the QuickBooks service times out or errors, rolling back the database checkout leads to severe inconsistencies if payment was already physically tendered or swiped.
3. If the database transaction commits but the network fails immediately afterward, the external system never receives the event (dual-write problem).

---

## 2. Decision

CBOS adopts the **Transactional Outbox Pattern**:
1. Integration messages (`OutboxMessage`) are written to the database table `[Restaurant].[OutboxMessages]` within the exact same database transaction that finalizes the business aggregate (`Order.Complete()`).
2. An asynchronous background worker (`OutboxProcessor`) polls for pending messages and dispatches them to concrete `IOutboxMessageHandler` implementations.
3. All external dispatch calls are guarded by circuit breakers (`ICircuitBreakerRegistry`).
4. Transient errors trigger exponential backoff retries; exhausted retries transition to `DeadLetter` status for administrative inspection.

---

## 3. Consequences

### Positive
- **Instant UI Response:** Cashiers experience immediate transaction settlement without waiting for slow printers or remote APIs.
- **Zero Data Loss:** Dual-write inconsistencies are eliminated. If the business transaction commits, the outbox message is guaranteed to be saved.
- **Fault Tolerance:** Temporary network or printer outages do not stall front-of-house sales.

### Negative / Trade-offs
- Eventual consistency requires operators to understand that remote sync may complete a few seconds after checkout.
- Requires maintenance and monitoring of outbox table size and dead-letter queues.
