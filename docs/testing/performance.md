# CBOS Performance Testing & Benchmark Methodology

| Attribute | Details |
| :--- | :--- |
| **Area** | Performance Benchmarking & Latency Engineering |
| **Audience** | Performance Engineers, Architects, QA Developers |
| **Last Reviewed Version** | CBOS 1.2.2 |
| **Status** | **BENCHMARK METHODOLOGY** |
| **Classification** | **PUBLIC-SAFE** |

---

## 1. Benchmarking Methodology: Synthetic vs. End-to-End Latency

> [!CAUTION]
> **SYNTHETIC BENCHMARKS ARE NOT CASHIER LATENCY:**
> In-memory algorithmic benchmarks (such as indexing 10,000 items in a memory dictionary in 2ms) do **not** reflect real cashier turnaround times. End-to-end cashier latency includes Windows Forms UI message dispatch, GDI text measurement, control repainting, and thermal printer hardware spooling. Performance reports must explicitly differentiate synthetic throughput from end-to-end user experience.

---

## 2. Key Latency Budgets & Target Operations

CBOS establishes strict response time budgets for mission-critical front-of-house operations:

| Operational Touchpoint | Target P50 | Target P95 | Target P99 | Measurement Boundary |
| :--- | :---: | :---: | :---: | :--- |
| **Barcode Scan to Cart Item Insertion** | < 15ms | < 35ms | < 50ms | Scanner HID keystroke event to UI grid row update. |
| **Category Switch & Tile Grid Flow** | < 25ms | < 50ms | < 80ms | Click category button to full visual redraw of 50 product tiles. |
| **Cash Settlement to Local Receipt Spool**| < 40ms | < 80ms | < 120ms | Click Exact Cash to database commit and spooler job handoff. |
| **Continuity Journal Append** | < 5ms | < 10ms | < 20ms | Disk append of DPAPI encrypted, HMAC-signed emergency sale. |
| **Local Operational Cache Index Build** | < 50ms | < 100ms | < 200ms | Ingesting 2,500 catalog items from decrypted JSON into memory index. |

---

## 3. Statistical Profiling Methodology

Performance profiling in CBOS follows strict percentiles:
- **P50 (Median):** Typical user experience under unconstrained hardware.
- **P95:** Performance during high-volume rush hours with active background outbox worker polling.
- **P99 (Worst Case):** Tail latency during simultaneous kitchen ticket dispatch, disk cache flushing, and network retries.

---

## 4. Current Profiling Evidence (CBOS 1.2.2)

1. **Operational Cache Indexing Performance:**
   - Evaluated via `OperationalCacheIndexingPerformanceTests.cs`.
   - Verified that querying 1,000 cached variants by barcode averages **under 0.2ms per lookup** in memory.
2. **Outbox Atomicity Overhead:**
   - Evaluated via `OutboxAtomicityIntegrationTests.cs`.
   - Verified that inserting an outbox message inside the primary `Order.Complete()` transaction adds **less than 4ms** of relational overhead to SQL Server commits.

---

## 5. Cross References
- [Testing Strategy](testing-strategy.md)
- [Restaurant POS Architecture](../pos/pos-architecture.md)
- [Operational Cache](../resilience/operational-cache.md)
