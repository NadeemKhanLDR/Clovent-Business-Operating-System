# PDR-0004: Transactional Currency Precision (2 Decimals in CBOS 1.2.x)

| Attribute | Details |
| :--- | :--- |
| **Status** | **ACCEPTED** |
| **Date** | 2026-10-07 |
| **Scope** | MasterData, Catalog, Financial Storage, Currencies |
| **Decision Owner** | Product Leadership & Financial Governance |

---

## Context
Currencies across global markets possess varying decimal subdivisions: standard 2-decimal currencies (USD, EUR, GBP, PKR, INR), zero-decimal currencies (JPY, KRW), and 3-decimal currencies (KWD, BHD, OMR). Accommodating arbitrary decimal precision across transaction tables, tender strips, UI controls, receipt formatters, and tax engines requires complex schema and UI adaptations.

## Decision
1. **CBOS 1.2.x Pilot Boundary:** Transactional currency processing in CBOS 1.2.x is strictly bounded to **2-decimal currencies** for settled transaction records, cash drawer movements, and customer ledger balances (e.g., PKR, USD, EUR, GBP).
2. **Permitted Higher-Precision Scope:** The 2-decimal transactional currency rule applies to final transaction records, ledgers, and payable tender at explicitly defined contract boundaries. The system explicitly permits higher precision for:
   - Intermediate mathematical calculations prior to defined rounding boundaries.
   - Unit prices (e.g. fractional cost per unit or weight: `decimal(18,4)`).
   - Exchange rates, tax percentage rates, and discount percentage rates.
   - Inventory quantities, recipe ingredient proportions, and unit-of-measure conversion factors (`decimal(18,4)`).
3. **Explicit Rounding Boundaries:** Monetary values must be rounded at explicitly defined contract boundaries established by `MoneyRoundingPolicy`.
4. **Selected Pilot Midpoint Direction & TASK-03 Contract:** `MidpointRounding.AwayFromZero` is recorded as the **selected pilot midpoint direction**, pending completion of **TASK-03: Financial Rounding Contract**. TASK-03 must formally complete: (a) exact rounding boundaries in the calculation flow, (b) tax and discount application ordering, (c) apportionment and allocation algorithms with residual cent handling across line items, and (d) mathematical consistency across online sales, Continuity/replay, receipts, payments, Day Close, and all sale paths (dine-in, takeaway, delivery, split bills, combo pricing).
5. High-precision (3-decimal or 4-decimal) settlement currencies are explicitly out of scope for the 1.2.x pilot baseline. Do not claim new currency or higher-precision storage support beyond the existing schema.

## Consequences
- **Positive:** Clear, predictable pilot boundaries; avoids premature generalization; ensures intermediate precision is preserved while guaranteeing deterministic 2-decimal reconciliation at financial boundaries.
- **Trade-off:** CBOS 1.2.x cannot be deployed in jurisdictions requiring 3-decimal transactional accounting without subsequent architectural expansion.
