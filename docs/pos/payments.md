# CBOS Payments & Settlement Architecture

| Attribute | Details |
| :--- | :--- |
| **Area** | Financial Settlement & Tender Processing |
| **Audience** | POS Developers, Payment Engineers, Compliance Auditors, Support |
| **Last Reviewed Version** | CBOS 1.2.2 |
| **Status** | **FACTUAL BASELINE** |
| **Classification** | **PUBLIC-SAFE** |

---

## 1. Settlement Architectural Overview

The payment subsystem in CBOS processes monetary tenders tendered against an active `Order`. Governed by the `Payment` aggregate (`src/Clovent.Restaurant/Payments/Payment.cs`) and `PosPaymentRules` (`src/Clovent.Desktop/Restaurant/Orders/PosPaymentRules.cs`), payments in CBOS are strictly additive, immutable financial records:

```mermaid
flowchart TD
    subgraph TenderOptions["Supported Tender Methods"]
        Cash["Cash (Immediate Drawer Float Addition)"]
        Card["Card (Manual External Terminal Tender Classification)"]
        Account["On Account (Customer Credit Ledger / Advance Credit)"]
        Other["Custom Payment Method (Mobile Wallet / Voucher Classification)"]
    end

    subgraph Validation["Settlement Engine (PosPaymentRules.cs)"]
        CheckBalance["Evaluate Remaining Balance = Total - Sum(Active Payments)"]
        Idempotency["Client-Side Idempotency Key Validation"]
        ShiftLink["Active Shift Association (ShiftId)"]
    end

    subgraph Outcome["Settlement Outcomes"]
        Split["Partial Payment / Split Tender (Balance > 0.005m)"]
        Settled["Order Completed & Receipt Printed (Balance <= 0.005m)"]
        Advance["Excess Tender -> Recorded as Customer Advance Credit"]
    end

    TenderOptions --> Validation
    Validation --> CheckBalance
    CheckBalance -- Balance > 0.005 --> Split
    CheckBalance -- Balance <= 0.005 --> Settled
    CheckBalance -- Overpayment on Account --> Advance
```

---

## 2. Supported Tender Methods

### 2.1 Cash
- **Workflow:** Cashier enters amount tendered or clicks quick-cash denominations (`Exact`, `500`, `1000`, `5000`).
- **Drawer Linkage:** Tendered cash is automatically linked to the active `ShiftId`.
- **Change Calculation:** If tendered cash exceeds the bill total, change is calculated and displayed on screen; the payment record is saved for the exact bill total (or the overage is recorded as customer advance if account-linked).

### 2.2 Card (Debit / Credit) — Manual Tender Classification Only
> [!IMPORTANT]
> **NO DIRECT CARD PROCESSOR INTEGRATION:**
> - CBOS does **not** directly integrate with bank card acquirers, payment processors, or EMV card readers.
> - **Workflow:** The cashier charges the card on a standalone, external merchant payment terminal (EFTPOS/cellular card reader). Upon receiving a successful printed authorization slip, the cashier selects `Card` in CBOS and optionally records the card brand and external terminal approval code.
> - **Compliance Note:** CBOS is **not** PCI-DSS certified and does **not** collect, process, or store cardholder Primary Account Numbers (PAN), CVVs, or track data.

### 2.3 On Account (Customer Credit Sales)
- **Workflow:** Allows trusted corporate or repeat customers to purchase on credit.
- **Credit Limit Verification:** Validates that the order amount does not breach the customer's `CreditLimit`.
- **Ledger Posting:** Creates a debit entry in `[Restaurant].[CustomerLedgerEntries]` increasing the customer's accounts receivable balance.
- **Advance Balance Consumption:** If the customer holds pre-paid advance credits (`AdvanceBalance > 0`), CBOS consumes advance credits first before extending new credit.

### 2.4 Split Tender
Split billing in CBOS is an inherent property of the relational data model. An `Order` accumulates multiple `Payment` records (e.g. $20 Cash + $30 Card) until the remaining unpaid balance is at or below half a cent (`BalanceEpsilon = 0.005m`).

---

## 3. Payment Idempotency & Immutability

To prevent duplicate charges caused by double-clicking payment buttons or network retransmissions during high cashier velocity:
1. Every payment submission generates a client-side idempotency key (`IdempotencyKey`).
2. Handlers and database configurations enforce unique constraints on active payments by idempotency key.
3. If an identical command is retried, the existing payment record is returned without creating duplicate financial debits.
4. Payments are **immutable**: historical records are never deleted. Adjustments occur exclusively via compensating entries.

---

## 4. Key Classes & Source Traceability

- **Payment Aggregate:** `src/Clovent.Restaurant/Payments/Payment.cs`
- **Payment Method Aggregate:** `src/Clovent.Restaurant/PaymentMethods/PaymentMethod.cs`
- **Payment Presentation Rules:** `src/Clovent.Desktop/Restaurant/Orders/PosPaymentRules.cs`
- **Customer Ledger:** `src/Clovent.Restaurant/Customers/CustomerLedgerEntry.cs`
- **Customer Payment Allocations:** `src/Clovent.Restaurant/Customers/CustomerPaymentAllocation.cs`

---

## 5. Cross References
- [Order Lifecycle Documentation](order-lifecycle.md)
- [Shifts & Cash Management](shifts-and-cash-management.md)
- [Financial Integrity Rules](../../.agents/rules/financial-integrity.md)
- [PDR-0003: Completed Financial Immutability](../product/pdr/PDR-0003-completed-financial-transaction-immutability.md)
- [PDR-0004: Transactional Currency Precision](../product/pdr/PDR-0004-transactional-currency-precision.md)
