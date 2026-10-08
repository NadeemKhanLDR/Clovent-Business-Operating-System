# PDR-0003: Completed Financial Transaction Immutability

| Attribute | Details |
| :--- | :--- |
| **Status** | **ACCEPTED** |
| **Date** | 2026-10-07 |
| **Scope** | Financial Integrity, POS, Accounting, Auditability |
| **Decision Owner** | Product Leadership & Financial Governance |

---

## Context
Retail POS systems frequently face requests to "edit an order" or "fix an error" after a check has been settled and the receipt handed to the customer. Performing in-place SQL updates or hard deletes on completed orders creates financial drift, audit vulnerabilities, and tax non-compliance.

## Decision
All completed financial transactions in CBOS are strictly **immutable**:
1. Completed orders, payments, discounts, service charges, customer ledger entries, and business day close records must never be modified or deleted in-place.
2. Corrections, mistakes, or returns must be handled exclusively via explicit **compensating transactions** (reversal entries, credit notes, or formal returns). Compensating transactions correct posted financial effects; legitimate unpaid/unposted draft cancellation remains possible under domain rules.
3. The receipt snapshot captured at completion (`ReceiptSnapshotJson`) and completed financial facts remain permanently immutable.
4. **Metadata Updates vs. Financial Facts:** Explicitly allowed operational metadata updates (such as updating delivery driver notes, customer delivery instructions, or dispatch tags) are strictly distinguished from financial facts. Financial corrections or amount changes must never be permitted through metadata exceptions or backdoors.
5. **Policy Requirement vs. Source Behavior (TASK-04):** In the CBOS 1.2.2 baseline source code, `Order.Void()` does not currently block orders in `OrderStatus.Completed`. The complete prohibition of completed-order voids and BalanceEpsilon elimination is an **accepted policy requirement** that must be enforced via domain validation and UI locking in CBOS 1.2.3 under **TASK-04: BalanceEpsilon Elimination + Completed Void Restriction**. The formal compensating Refund domain aggregate will be delivered in CBOS 1.3.0.

## Consequences
- **Positive:** Unimpeachable financial audit trail; zero silent revenue distortion; protects historical transaction integrity.
- **Trade-off:** Operational errors cannot be quietly erased; correcting mistakes requires explicit compensating entries.
