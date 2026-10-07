# PDR-0002: Customer Data Ownership & Non-Destructive Licensing

| Attribute | Details |
| :--- | :--- |
| **Status** | **ACCEPTED** |
| **Date** | 2026-10-07 |
| **Scope** | Licensing, Customer Trust, Data Governance, Legal |
| **Decision Owner** | Product Leadership & Executive Governance |

---

## Context
Commercial software licensing mechanisms can enforce entitlement boundaries upon license expiry. Aggressive mechanisms that delete local data, encrypt databases, or block database backups destroy customer trust and create severe legal liabilities.

## Decision
CBOS adopts an unconditional **Non-Destructive Licensing Policy**:
1. Customer business data is the sole property of the customer. CBOS will never delete, truncate, encrypt, or corrupt customer data upon license expiration.
2. When a license expires, the system transitions to a read-only administrative state.
3. Users retain full, unhindered access to view historical sales, generate financial reports, inspect customer receivables, view inventory history, and execute database backups.
4. Only the creation of *new* commercial transactions (new sales orders, payments, stock movements) is gated until license renewal.

## Consequences
- **Positive:** Ethical software governance; complete customer data sovereignty; zero risk of catastrophic business data loss due to billing or license renewal delays.
- **Trade-off:** Requires robust application-level feature gating (`CanCreateTransactions()`) rather than binary application shutdown.
