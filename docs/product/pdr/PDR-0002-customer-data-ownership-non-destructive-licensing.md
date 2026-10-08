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
1. Customer business data is the sole property of the customer. Expiry must not destroy data or remove authorized historical access. CBOS will never delete, truncate, encrypt, or corrupt customer data upon license expiration.
2. When a license expires, the system transitions to a read-only administrative state.
3. Users retain full, unhindered access to view historical sales, generate financial reports, inspect customer receivables, view inventory history, and execute database backups.
4. **Authentication & RBAC Remain Enforced:** Authentication and role-based access control (RBAC) remain strictly enforced at all times. Unauthenticated access is rejected, and user role permissions continue to govern historical data visibility and administrative actions.
5. Only the creation of *new* commercial transactions (new sales orders, payments, stock movements) is gated until license renewal.
6. **Accepted Policy vs. Source Behavior:** This record defines the accepted enterprise policy. In the CBOS 1.2.2 baseline source code (`Program.cs`), an expired license displays an informational warning dialog alerting the operator to read-only mode and proceeds to normal authentication without destroying data. An expiry warning alone does not establish read-only transaction gating; this is recorded as a known gap without implementing or assigning new release scope.

## Consequences
- **Positive:** Ethical software governance; complete customer data sovereignty; protects against software-driven data deletion or encryption upon license expiration.
- **Trade-off:** Requires robust application-level feature gating (`CanCreateTransactions()`) rather than binary application shutdown.
