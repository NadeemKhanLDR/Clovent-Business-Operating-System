# ADR-0005: Encrypted Local Operational Cache with Freshness TTL

| Attribute | Details |
| :--- | :--- |
| **Status** | **ACCEPTED** |
| **Date** | 2026-10-06 (Baseline 1.2.0+) |
| **Deciders** | Architecture Working Group |
| **Area** | Local Data & Security |

---

## 1. Context & Problem Statement

To support Emergency Continuity Mode (ADR-0004), POS workstations must be capable of rendering menu hierarchies, barcode scans, prices, and tax rates when the primary database is disconnected.

However, storing catalog and pricing data locally on disk presents security and operational integrity risks:
1. If plaintext files are stored, malicious actors or competitors could tamper with prices (e.g. modifying prices to 0.01) or steal sensitive pricing structures.
2. If stale caches remain valid indefinitely, cashiers might continue selling discontinued items or honoring outdated prices for weeks after an outage began.

---

## 2. Decision

CBOS establishes a **Protected Local Operational Cache (`ProtectedOperationalCacheStore`)**:
1. **Scope of Cached Entities:** Categories, products, variants, barcodes, pricing tiers, tax profiles, and dining areas/tables.
2. **Exclusions:** Customer account balances, historical orders, and user administrator records are explicitly excluded.
3. **Cryptographic Protection:** The cached payload is encrypted using Windows DPAPI (`DataProtectionScope.LocalMachine`) and signed with HMAC-SHA256 to ensure confidentiality and tamper evidence.
4. **Freshness Policy (`CacheFreshnessPolicy`):** Caches enforce a strict Time-to-Live (TTL). If the cache is older than the configured freshness threshold (or if the system clock has been tampered with), offline sales are blocked.
5. **Continuous Refresh:** The cache is refreshed in the background during normal connected operations.

---

## 3. Consequences

### Positive
- POS workstations can boot or operate offline without risking pricing tampering or out-of-band manipulation.
- High-speed in-memory indexing of cached items provides sub-millisecond barcode lookups.

### Negative / Trade-offs
- An extended network outage exceeding the TTL will eventually cause the cache to expire, disabling Continuity Mode until reconnected.
- Consumes minor local disk storage per terminal workstation.
