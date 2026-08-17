# ADR-005 — Customer Selection and Credit-Sales Workflow

## Status

Accepted

## Context

Cashiers need to assign specific customers to orders to support tab billing, loyalty tracking, and credit sales. While cash and card transactions can be processed under a generic "Walk-in" customer, credit-sales (paying later on account) require a registered customer record with verified credit constraints to prevent financial loss.

## Decision

We implemented a secure, validated customer selection and credit gating workflow within `RestaurantPosForm`:

1. **Customer Selection Interface:**
   - A DevExpress `SearchLookUpEdit` control (`_customerLookup`) allows search-as-you-type selection of registered customers.
   - The dropdown grid displays the customer's name, code, and current **Outstanding Balance** (dynamically resolved from the `CustomerLedgerEntry` table).
   - A "New Customer" shortcut button resides adjacent to the lookup control to allow cashiers to register customers quickly.

2. **Credit-Sale Guard Rules:**
   - **No Walk-in Credit:** If the cashier selects "Credit" as the payment method, the UI checks the active customer. If the customer is the default "Walk-in" record, the system blocks the transaction and prompts the cashier to select a registered customer.
   - **Cashier Authorization Check:** Processing a credit sale requires the active cashier session to possess the `"pos.creditsale"` feature permission.
   - **Credit Limit Gating:** Before recording a credit payment, the system evaluates the order totals against the customer's configured credit limit and outstanding balance. If the new transaction exceeds the limit:
     - The transaction is blocked until a manager provides override credentials.
     - The override credentials must satisfy the `"pos.exceedcreditlimit"` authorization policy.
     - Once approved, the override is logged as a security activity entry in the database.

## Consequences

### Benefits
- **Financial Security:** Eliminates accidental credit extension to Walk-in customers and restricts credit limit breaches to authorized managers.
- **Improved Cashier UX:** Live outstanding balance visibility directly within the search dropdown helps cashiers make quick decisions during checkout.
- **Audit Trails:** Every limit breach override is permanently recorded in the `ActivityLogEntry` table for back-office auditing.

### Trade-offs & Gaps
- **Load-time Ledger Queries:** Determining the current outstanding balance for each customer requires querying ledger aggregates, contributing to the load-time performance debt logged in `TechnicalDebt.md`.
