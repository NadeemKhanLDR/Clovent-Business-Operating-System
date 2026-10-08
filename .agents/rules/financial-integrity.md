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
3. **Audit Immutability & Receipt Snapshots:** Completed financial facts (amounts, line items, unit prices, discounts, taxes, service charges, tendered amounts, change, ledger postings, Day Close figures) and receipt snapshots (`ReceiptSnapshotJson`) must reflect the exact business reality at completion and are permanently immutable.
4. **Metadata Updates vs. Financial Facts:** Explicitly allowed operational metadata updates (such as updating delivery driver notes, customer delivery instructions, or dispatch tags) are strictly distinguished from financial facts.
5. **No Financial Corrections via Metadata:** Financial corrections or retrofits must never be permitted through metadata exceptions or backdoors. Any financial adjustment requires an explicit, auditable compensating transaction.

---

## 3. Compensating Transactions & Draft Cancellation

- **Compensating Transactions Rule:** Compensating transactions correct posted financial effects. Financial adjustments, order cancellations of completed transactions, and corrections must occur exclusively through explicit **compensating transactions** (credit notes, ledger reversal entries, or formal return records).
- **Unpaid Draft Cancellation:** Legitimate unpaid/unposted draft cancellation remains possible under domain rules (e.g. discarding an unposted draft check before any payment is recorded).
- **Prohibition of Pseudo-Refunds:** Modifying an existing order, applying negative line items, or setting `OrderStatus.Voided` on completed orders as an ad-hoc refund workaround is strictly prohibited by policy in pilot operations.
- **Policy Requirement vs. Source Behavior (TASK-04):** In the CBOS 1.2.2 baseline, `Order.Void()` in source code allows voiding orders regardless of whether `Status == OrderStatus.Completed`. The prohibition of completed-order voids and BalanceEpsilon elimination is a **policy requirement** that must be enforced via domain validation and UI locking in CBOS 1.2.3 under **TASK-04: BalanceEpsilon Elimination + Completed Void Restriction**.
- **Formal Refund Domain:** In CBOS 1.2.2 / 1.2.3, the formal Refund domain is **NOT YET IMPLEMENTED**. Completed-order voids and refunds are prohibited during the attended single-terminal pilot until the formal compensating refund architecture is introduced in CBOS 1.3.0.

---

## 4. Centralized Financial Rounding Policy

### A. Strict Prohibition of Ad-Hoc Rounding
- Feature code, UI layers, report generators, and MediatR command handlers must **never** perform ad-hoc rounding using arbitrary midpoint strategies (e.g. sporadic calls to `Math.Round(...)`).
- All financial rounding across lines, taxes, discounts, service charges, and bill totals must route exclusively through the centrally approved `MoneyRoundingPolicy`.

### B. Permanent Rounding Policy Rule
All financial rounding must use the single centrally approved `MoneyRoundingPolicy`. Feature code may not perform ad-hoc rounding. The active midpoint strategy is part of the versioned financial specification and must be covered by invariant tests.

### C. Selected Pilot Midpoint Direction & TASK-03 Calculation Contract
> [!IMPORTANT]
> **Selected Pilot Midpoint Direction & Pre-TASK-03 Scope:**
> - **Selected Pilot Direction:** `MidpointRounding.AwayFromZero` is recorded as the **selected pilot midpoint direction**, pending completion of TASK-03's explicit calculation contract.
> - **Contract Decisions to be Resolved by TASK-03:** TASK-03 (financial rounding contract) must formally complete and verify:
>   1. **Rounding Boundaries:** Define exact points in the calculation pipeline where intermediate rounding occurs versus where full decimal precision is preserved.
>   2. **Tax & Discount Ordering:** Establish the exact execution sequence between line-item discounts, line-level exclusive/inclusive taxes, order-level discounts, and service charges.
>   3. **Allocations & Residual Cents:** Define deterministic distribution algorithms for apportioning order-level discounts or inclusive taxes across individual line items, including exact residual-cent remainder handling (e.g. largest-remainder method).
>   4. **Mathematical Consistency Across Operational Paths:** Ensure consistency across online sales, Continuity/replay, receipts, payments, Day Close, and all sale paths (dine-in, takeaway, delivery, split billing, table transfer, combo pricing).
> - **Zero Code Mutation in Governance Task:** No financial calculation code or rounding implementation is modified in this documentation/governance task. Implementation belongs strictly to TASK-03 in CBOS 1.2.3.
> - **Schema Preservation:** Do not claim new currency or higher-precision storage support beyond the existing schema.

---

## 5. Currency Precision Governance

### A. CBOS 1.2.x Pilot Scope
- **Transactional Currency Scope:** CBOS 1.2.x strictly supports **2-decimal currencies** for settled transaction records, cash drawer movements, and customer ledger balances (e.g., PKR, USD, EUR, GBP).
- High-precision (3-decimal or 4-decimal) settlement currencies are out of scope for the 1.2.x pilot baseline. Do not claim new currency or higher-precision storage support beyond the existing schema.

### B. Permitted Higher-Precision Scope
- The 2-decimal transactional currency rule applies to final transaction records, ledgers, and payable tender at explicitly defined contract boundaries.
- **Permitted Higher-Precision Areas:** The system explicitly permits higher precision for:
  - Intermediate mathematical calculations prior to defined rounding boundaries.
  - Unit prices (e.g. fractional cost per unit or weight: `decimal(18,4)`).
  - Exchange rates, tax percentage rates, and discount percentage rates.
  - Inventory quantities, recipe ingredient proportions, and unit-of-measure conversion factors (`decimal(18,4)`).
- Monetary values are rounded to 2 decimals only at explicitly defined contract boundaries established by `MoneyRoundingPolicy`.

### C. Permanent Currency Precision Rule
Transactional currency precision must match the approved persistence schema and centralized Money policy. A broader precision change requires migration, compatibility testing, and financial regression validation.

---

## 6. Financial Arithmetic & Type Safety

- **Decimal Types Only:** All monetary figures, prices, quantities, discounts, surcharges, and tax calculations must use C# `decimal` (`decimal(18,2)` or `decimal(18,4)` for intermediate unit pricing).
- **Floating-Point Types Strictly Prohibited:** `float` and `double` are forbidden in all financial domains, models, DTOs, and calculations. Binary floating-point representation errors will corrupt reconciliations.

---

## 7. Payment Idempotency & Identity Governance (TASK-05)

- **Payment Attempt Identity Rule:**
  - Generate `PaymentAttemptId` once per intentional payment attempt.
  - Reuse it across retries, retransmissions, and duplicate UI submissions.
  - A genuinely separate intentional payment receives a new ID.
  - Reusing an ID with conflicting payment details must be rejected.
  - Do not prescribe a new schema for this documentation task.
- **Persistence Boundary Enforcement:** Handlers and database configurations must enforce idempotency at the persistence boundary to prevent duplicate payment postings.
- In-flight payment mutations must fail closed on concurrency collisions.
- Payment idempotency and credit-limit approval gating are governed under **TASK-05**.

---

## 8. Customer Accounts & Ledger Integrity

- **Approved Current Behavior:** Customer credit transactions post to `[Restaurant].[CustomerLedgerEntries]` as a single-entry subledger recording debits, credits, and running balance (`OutstandingBalance`, where positive is receivable and negative is advance/credit balance).
- **Separately Reviewed Financial Contract Required:** Unsupported mandates for dual entries in `CustomerLedgerEntries`, automatic advance creation/consumption, and specific new fields are removed. The approved current behavior is preserved; any introduction of dual entries or automated advance lifecycle semantics requires a separately reviewed financial contract before implementation.
- **Credit Limit Approval Gating (TASK-05):** Credit sales exceeding a customer's `CreditLimit` require explicit, action-specific manager elevation that fails closed.

---

## 9. Reconciliation Invariants & Term Definitions

All reporting, shifts, and cash drawer views must define reconciliation terms without overlap and satisfy mathematical reconciliation invariants:

### A. Non-Overlapping Reconciliation Terms
- **Cash Tendered vs. Change vs. Net Cash Applied:** `Cash Tendered` is gross cash presented by the customer. `Change` is currency returned to the customer. `Net Cash Applied` is $\text{Cash Tendered} - \text{Change}$.
- **Applied Payments:** Sum of applied amounts across tender methods (Net Cash Applied + Card + etc.) covering the bill.
- **On Account Classification:** Represents credit extension at settlement time, increasing customer accounts receivable. It is NOT cash or tender received in the drawer.
- **Customer Collections:** Cash received from customers as repayments against outstanding credit account balances. This is a drawer cash inflow distinct from point-of-sale order cash sales.
- **Existing Advance Usage:** Consuming pre-existing customer credit/advance balances. It represents neither new cash tendered into the drawer nor new credit extended ("On Account"). Do not introduce new advance-payment functionality.
- **Contract Scope:** Any unresolved terminology or reconciliation mappings are marked as **TASK-03: Financial Rounding Contract** and **TASK-06: Business Day Close Aggregation** contract work. In TASK-06, Day Close aggregates real Tax and Discount totals; Refund = 0 because refunds are explicitly disabled for the pilot, not because missing data is concealed (never use zero to conceal unsupported or missing financial data).

### B. Mathematical Reconciliation Invariants
1. **Bill Settlement Invariant:**
   $$\text{Subtotal} - \text{Discount} + \text{ServiceCharge} + \text{ExclusiveTax} = \text{GrandTotal}$$
   $$\text{Applied Payments} + \text{On Account Applied} = \text{GrandTotal}$$
   *(Terms must not double-count tendered cash and applied settlement).*

2. **Shift Drawer Reconciliation Invariant:**
   $$\text{StartingFloat} + \text{CashIn} + \text{NetCashSales} + \text{CashCollections} - \text{CashOut} = \text{ExpectedCash}$$
   $$\text{CountedCash} - \text{ExpectedCash} = \text{Variance}$$
   where:
   - $\text{NetCashSales} = \sum (\text{Cash Tendered} - \text{Change})$ for cash-settled orders in the shift (including the cash portion of every supported split tender, including Cash plus On Account, without double-counting customer collections).
   - $\text{CashCollections} = \text{Cash received from customer debt repayments}$.
   - For open/active shifts where no count has occurred, `CountedCash` and `Variance` must remain `null` / `"N/A"`. Fake zero or negative variances are prohibited.

3. **No Sum of Averages or Unit Prices:** Report footers must never sum unit prices, percentages, or average ticket values.

### C. Cost & Margin Truthfulness
- **Prepared Items:** If recipe/BOM cost is unavailable, cost must be `null` and displayed as `"N/A"`. Gross Profit must display `"Known GP: N/A"` or `"Known GP: {sum}"`. Never fake profitability by silently replacing unknown costs with `0.00`.
- **Purchased/Resale Items:** Use actual known purchase cost.
- **Service Items:** Legitimate zero direct cost (100% GP).

---

## 10. Inventory Workflow & Stock Integrity

- **Workflow Integrity:** Warehouse stock changes occur strictly via formal domain workflows: `Receive`, `Issue`, `Adjustment`, `Transfer`, `Reserve`, `Release`.
- **No Direct Mutation:** Never directly update stock quantities via raw SQL or ad-hoc DB updates to make reports match.
- **Stock On Hand Display:** Must clearly resolve both SKU (`NAAN-STD`) and Product identity (`${ProductName} - ${VariantName}`).
- **Precision Distinction:** Database storage precision (`decimal(18,4)` or `decimal(18,6)`) and UI display precision are strictly separated. Format all display quantities using `QuantityDisplay.Format()`.
