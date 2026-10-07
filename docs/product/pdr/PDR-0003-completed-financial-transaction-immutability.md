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
2. Corrections, mistakes, or returns must be handled exclusively via explicit **compensating transactions** (reversal entries, credit notes, or formal returns).
3. The receipt snapshot captured at completion (`ReceiptSnapshotJson`) remains the immutable record of what was presented to the customer.
4. In CBOS 1.2.2 / 1.2.3, the formal Refund domain is NOT yet implemented. Completed-order voids and pseudo-refunds are prohibited during the attended single-terminal pilot; formal compensating refund workflows will be delivered in CBOS 1.3.0.

## Consequences
- **Positive:** Unimpeachable financial audit trail; tax authority compliance; zero silent revenue distortion.
- **Trade-off:** Operational errors cannot be quietly erased; correcting mistakes requires explicit compensating entries.
