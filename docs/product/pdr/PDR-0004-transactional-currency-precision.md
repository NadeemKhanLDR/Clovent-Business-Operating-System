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
1. **CBOS 1.2.x Pilot Boundary:** Transactional currency processing in CBOS 1.2.x is strictly bounded to **2-decimal currencies**.
2. High-precision 3-decimal and 4-decimal currencies are explicitly out of scope for the 1.2.x pilot baseline.
3. Database storage for monetary values is standardized at `decimal(18,2)` for financial totals and balances, with intermediate pricing supported at up to `decimal(18,4)`.
4. Any future expansion to variable high-precision currency transactions requires a formal database migration, compatibility testing, and financial regression suite validation before adoption.

## Consequences
- **Positive:** Clear, predictable pilot boundaries; avoids premature generalization; simplifies rounding verification and tender strip layouts.
- **Trade-off:** CBOS 1.2.x cannot be deployed in jurisdictions requiring 3-decimal transactional accounting without subsequent architectural expansion.
