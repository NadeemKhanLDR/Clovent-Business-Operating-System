# PDR-0001: Continuity Mode Cash-Only Operational Policy

| Attribute | Details |
| :--- | :--- |
| **Status** | **ACCEPTED** |
| **Date** | 2026-10-07 |
| **Scope** | Resilience, Restaurant POS, Financial Risk |
| **Decision Owner** | Product Leadership & Financial Governance |

---

## Context
When primary Microsoft SQL Server database connectivity is lost, POS terminals transition into emergency Continuity Mode to allow continued front-of-house trading against cached menu items. Offline operations lack immediate access to real-time customer account balances, credit limits, or centralized duplicate sequence checks.

## Decision
During Continuity Mode, CBOS strictly enforces a **Cash-Only** transaction policy:
1. Only cash tenders against pre-cached catalog items are permitted.
2. Customer credit sales ("On Account"), credit limit overrides, account balance lookups, customer advances, and administrative setup modifications are strictly blocked.
3. Refunds and returns are strictly prohibited in Continuity Mode.
4. All cash sales are cryptographically recorded in the local journal (`continuity_journal.dat`) and reconciled to the primary SQL Server database upon reconnection.
5. **Terminal-Local & Machine-Bound DPAPI Scope:** Continuity Mode is strictly terminal-local, secured by machine-bound Windows DPAPI and HMAC signatures. Distributed peer-to-peer LAN continuity synchronization and headquarters replication are **unapproved exploratory concepts** and are not part of accepted roadmap scope.

## Consequences
- **Positive:** Eliminates the risk of bad debt from unverified credit accounts; ensures offline transactions are recorded strictly as immediate cash payments rather than credit extensions; protects financial integrity during network partitions; preserves machine-bound cryptographic isolation.
- **Trade-off:** Cashiers cannot extend credit to corporate or regular customers while offline. Offline transactions cannot be synchronized peer-to-peer across registers without direct SQL Server connectivity.
