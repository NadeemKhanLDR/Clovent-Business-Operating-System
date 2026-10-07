# ADR-0004: Emergency Continuity Mode & Local Cash Journal

| Attribute | Details |
| :--- | :--- |
| **Status** | **ACCEPTED** |
| **Date** | 2026-10-06 (Baseline 1.2.0+) |
| **Deciders** | Architecture & Operations Working Group |
| **Area** | High Availability & Resilience |

---

## 1. Context & Problem Statement

In retail and hospitality, database availability can be disrupted by severed local area network (LAN) cables, database server hardware restarts, or Windows OS updates on the back-office PC.

Traditional desktop POS systems display a modal error dialog ("Cannot connect to database") and halt entirely. In a busy restaurant or retail store, turning away customers or halting checkout during dinner rush causes immediate revenue loss and severe reputational damage.

---

## 2. Decision

CBOS implements **Emergency Continuity Mode**:
1. When SQL Server connectivity fails, the POS workstation automatically transitions to Continuity Mode rather than crashing.
2. The workstation utilizes the locally cached product catalog (`ProtectedOperationalCacheStore`) to look up items, categories, and prices.
3. Operations in Continuity Mode are governed by a strict **Cash-Only Policy**:
   - Only cash sales against cached items are permitted.
   - Credit-account sales, price alterations, voids, and administrative configurations are blocked.
4. Offline sales are written to an encrypted, append-only local journal (`ProtectedContinuityJournalStore`) using Windows DPAPI and signed with HMAC-SHA256.
5. Transactions are assigned collision-safe local receipt numbers (`LocalReceiptNumber`).
6. Upon reconnection, an automated replayer (`ContinuityCoordinator`) verifies cryptographic signatures and posts pending orders into SQL Server with exactly-once execution semantics.

---

## 3. Consequences

### Positive
- **Always-On Selling:** Cashiers continue taking orders and issuing receipts even when the central database is entirely unreachable.
- **Financial Safety:** The cash-only restriction prevents uncollectible accounts receivable or credit limit overruns while offline.
- **Tamper Resistance:** Local journals cannot be forged or modified out-of-band without invalidating HMAC signatures.

### Negative / Trade-offs
- Cashiers cannot perform customer on-account sales or view historical bills during an outage.
- Central inventory ledger adjustments are delayed until reconnection.
