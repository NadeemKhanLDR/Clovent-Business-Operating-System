# Financial Integrity & Accounting Rules

**Scope:** Global (`src/Clovent.Restaurant/**`, `src/**/Orders/**`, `src/**/Payments/**`, financial calculations, ledgers, day close, reports)  
**Authoritative Reference:** [AGENTS.md](../../AGENTS.md)

---

## 1. Prime Directive: Financial Correctness Before Performance

Financial correctness, audit durability, and mathematical reconciliation outrank system throughput, cashier convenience, and execution speed without exception. Under no circumstances may an engineer or agent bypass financial controls, introduce unverified rounding heuristics, or mute reconciliation errors.

---

## 2. Immutable Completed Financial History

Once a financial transaction or order has been completed and durable settlement recorded:
1. **No In-Place Mutation:** Records in `[Restaurant].[Orders]`, `[Restaurant].[OrderLines]`, `[Restaurant].[Payments]`, `[Restaurant].[CustomerLedgerEntries]`, and `[Restaurant].[BusinessDayCloses]` must **never** be updated or overwritten to correct historical errors.
2. **No Physical Deletion:** Hard SQL `DELETE` operations on financial and transaction tables are strictly forbidden.
3. **Audit Immutability:** Historical receipt snapshots (`ReceiptSnapshotJson`) and transaction records must reflect the exact business reality at the moment of completion.

---

## 3. Compensating Transactions (No Pseudo-Refunds)

- Financial adjustments, order cancellations, and transaction corrections must occur exclusively through explicit **compensating transactions** (credit notes, ledger reversal entries, or formal return records).
- **Prohibition of Pseudo-Refunds:** Modifying an existing order, applying negative line items, or setting `OrderStatus.Voided` on completed orders as an ad-hoc refund workaround is strictly prohibited in pilot operations.
- **Formal Refund Domain:** In CBOS 1.2.2 / 1.2.3, the formal Refund domain is **NOT YET IMPLEMENTED**. Completed-order voids and refunds are prohibited during the attended single-terminal pilot until the formal compensating refund architecture is introduced in CBOS 1.3.0.

---

## 4. Centralized Financial Rounding Policy

### A. Strict Prohibition of Ad-Hoc Rounding
- Feature code, UI layers, report generators, and MediatR command handlers must **never** perform ad-hoc rounding using arbitrary midpoint strategies (e.g. sporadic calls to `Math.Round(...)`).
- All financial rounding across lines, taxes, discounts, service charges, and bill totals must route exclusively through the centrally approved `MoneyRoundingPolicy`.

### B. Permanent Rounding Policy Rule
All financial rounding must use the single centrally approved `MoneyRoundingPolicy`. Feature code may not perform ad-hoc rounding. The active midpoint strategy is part of the versioned financial specification and must be covered by invariant tests.

### C. Unresolved Specification Decision (Pre-TASK-03 Requirement)
> [!IMPORTANT]
> **Open Policy Decision:** A policy conflict exists between commercial standard rounding (`MidpointRounding.AwayFromZero` / Half-Up) and banking/statistical rounding (`MidpointRounding.ToEven` / Banker's Rounding).
> - **Policy Status:** UNRESOLVED DECISION.
> - **Requirement:** The Founder / CTO / Financial Stakeholder must formally approve either `AwayFromZero` or `ToEven` as the enterprise standard prior to the implementation of TASK-03 in CBOS 1.2.3.
> - Feature code must NOT speculate or prematurely hardcode either strategy prior to this formal policy determination.

---

## 5. Currency Precision Governance

### A. CBOS 1.2.x Pilot Scope
- **Transactional Currency Support:** CBOS 1.2.x strictly supports **2-decimal currencies only** (e.g., PKR, USD, EUR, GBP).
- High-precision (3-decimal or 4-decimal) currencies (e.g., KWD, BHD, OMR) are out of scope for the 1.2.x pilot baseline.

### B. Permanent Currency Precision Rule
Transactional currency precision must match the approved persistence schema and centralized Money policy. A broader precision change requires migration, compatibility testing, and financial regression validation.

---

## 6. Financial Arithmetic & Type Safety

- **Decimal Types Only:** All monetary figures, prices, quantities, discounts, surcharges, and tax calculations must use C# `decimal` (`decimal(18,2)` or `decimal(18,4)` for intermediate unit pricing).
- **Floating-Point Types Strictly Prohibited:** `float` and `double` are forbidden in all financial domains, models, DTOs, and calculations. Binary floating-point representation errors will corrupt reconciliations.

---

## 7. Payment Idempotency & Concurrency Safety

- Every payment submission and tender operation must carry a unique client-generated `IdempotencyKey`.
- Handlers and database configurations must enforce idempotency at the persistence boundary to prevent duplicate payment postings caused by rapid cashier double-clicks or network retransmissions.
- In-flight payment mutations must fail closed on concurrency collisions.

---

## 8. Customer Accounts & Ledger Integrity

- **Double-Entry Principle:** Customer credit sales ("On Account") and credit repayments must post dual ledger entries to `[Restaurant].[CustomerLedgerEntries]`.
- **Customer Advance Balances:** When customer payments exceed outstanding receivables, the excess must be recorded as an advance credit balance (`AdvanceBalance`). Subsequent orders must consume advances first before extending credit.
- **Credit Limit Gating:** Credit sales exceeding a customer's `CreditLimit` require explicit, action-specific manager elevation that fails closed.

---

## 9. Reconciliation Invariants

All reporting, shifts, and cash drawer views must satisfy mathematical reconciliation invariants:
1. **Bill Reconciliation Invariant:**
   $$\text{Subtotal} - \text{Discount} + \text{ServiceCharge} + \text{ExclusiveTax} = \text{GrandTotal}$$
   $$\text{PaidTotal} + \text{OnAccountTotal} = \text{GrandTotal}$$
2. **Shift Drawer Reconciliation Invariant:**
   $$\text{StartingFloat} + \text{CashIn} + \text{CashSales} + \text{CashCollections} - \text{CashOut} = \text{ExpectedCash}$$
   $$\text{CountedCash} - \text{ExpectedCash} = \text{Variance}$$
   - For open/active shifts where no count has occurred, `CountedCash` and `Variance` must remain `null` / `"N/A"`. Fake zero or negative variances are prohibited.
3. **No Sum of Averages or Unit Prices:** Report footers must never sum unit prices, percentages, or average ticket values.
