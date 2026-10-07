# CBOS Enterprise Glossary & Terminology

| Attribute | Details |
| :--- | :--- |
| **Area** | General Architecture & Domain Dictionary |
| **Audience** | All Developers, Architects, Support, Operations, QA |
| **Last Reviewed Version** | CBOS 1.2.2 |
| **Status** | **CANONICAL REFERENCE** |
| **Classification** | **PUBLIC-SAFE** |

---

## 1. Domain & Architecture Terms

### Accounts Receivable (A/R)
The balance of money owed to a business by customers for goods delivered or services rendered on credit. In CBOS, managed via the `Customer` aggregate and `CustomerLedgerEntry` tracking sales, allocations, payments, and advance credit balances.

### Aggregate Root
A cluster of associated domain entities and value objects treated as a single unit for data changes and consistency boundaries (e.g., `Order`, `Shift`, `WarehouseStock`, `User`). Outside objects hold references only to the aggregate root's identifier.

### Bounded Context
A central pattern in Domain-Driven Design (DDD) defining explicit boundaries within which a domain model applies. CBOS contains six bounded contexts: Authentication, Identity, MasterData, Catalog, Inventory, and Restaurant.

### Branch
A physical or legal retail/restaurant location belonging to a Company. A Branch owns dining areas, registers (Terminals), and default inventory storage locations (Warehouses).

### Cash Movement
A mid-shift cash adjustment recorded in a cashier till:
- **Cash In:** Infusion of cash into the drawer (e.g. float top-up, change replenishment).
- **Cash Out:** Extraction of cash from the drawer (e.g. mid-day manager drop, petty cash expense).

### CBOS
**Clovent Business Operating System** — the modular, high-reliability enterprise business operating system built for retail and hospitality operations.

### Commissioning
The first-run workstation provisioning process that initializes SQL database schemas, creates initial company and branch records, configures terminal identity, provisions the first administrator, and activates software licensing.

### Continuity Mode
An emergency operating state entered automatically by POS workstations when primary SQL Server database connectivity is lost. Allows cash-only order entry using local cached catalog data and logs transactions to a local DPAPI/HMAC-protected emergency journal.

### Cost of Goods Sold (COGS)
The direct costs attributable to the production or acquisition of goods sold. In CBOS, calculated using purchased stock valuation or recipe bills of materials (BOM). If cost is unknown for prepared items, CBOS displays `N/A` rather than fabricating zero cost.

### Data Protection API (DPAPI)
A Windows cryptographic API providing symmetric encryption/decryption using keys derived from machine or user credentials. CBOS uses DPAPI to protect database passwords, local operational caches, license guard states, and offline emergency sales journals.

### EntityPicker
A standard WinForms search-as-you-type dropdown component (`src/Clovent.Desktop/MasterData/EntityPicker.cs`) used across back-office views for warehouse, branch, and company selection.

### HMAC (Hash-Based Message Authentication Code)
A cryptographic mechanism using SHA-256 and a secret key to verify data integrity and authenticity. CBOS uses HMAC to detect out-of-band tampering with local operational caches and continuity journals.

### Idempotency Key
A unique identifier attached to a command or payment transaction ensuring that repeated execution produces the exact same outcome without duplicate side effects or duplicate billing.

### Known Gross Profit (Known GP)
The truthful reporting of gross profit margins based strictly on items with verified purchase or recipe costs. Items with unknown recipe costs display `Known GP: N/A` to prevent false profitability metrics.

### Manager Authorization
A security elevation mechanism in Restaurant POS requiring supervisor or manager PIN/password entry before sensitive operations (e.g. voiding lines, applying custom order discounts, or voiding completed checks) are executed.

### Operational Cache
A local, encrypted JSON store (`ProtectedOperationalCacheStore`) holding menu items, variant prices, tax rates, and dining areas on the POS workstation to enable emergency Continuity Mode sales when the central database is offline.

### Order
The primary sales transaction aggregate root in `Clovent.Restaurant`. Tracks dining mode (Dine-In, Take Away, Delivery), lines, service charges, discounts, settlements, and workflow statuses (`Open`, `Held`, `Completed`, `Voided`, `Cancelled`).

### PerMonitorV2
Windows Forms high-DPI awareness mode where each top-level window queries DPI dynamically based on the display it resides on, preventing blurry bitmap scaling across mixed-DPI multi-monitor environments.

### Quick Orders
A streamlined template-driven order entry mechanism in Restaurant POS for fast-service operations where predefined frequent items are batched into single-click tickets.

### Recovery Point Objective (RPO)
The maximum acceptable age of files or transactions that must be recovered from backup storage for normal operations to resume if a disaster occurs. In CBOS standalone, target RPO is 1 hour; with Continuity Mode, local sales journal RPO is near zero.

### Recovery Time Objective (RTO)
The maximum acceptable length of time that application services can be offline following a disaster. CBOS target RTO is under 15 minutes for terminal swap or database restore.

### ReleaseGuard
A specialized security scanner script (`tools/ReleaseGuard/ScanReleasePackage.ps1`) executed prior to publishing releases to verify zero presence of source files, debug symbols, development settings, or unencrypted secrets in release packages.

### RSA-2048
An asymmetric cryptographic algorithm using a 2048-bit key pair. In CBOS, the vendor signs software license files using a private key kept offline, and the desktop client verifies signatures using an embedded public key.

### Shift
A till session aggregate root tracking cash drawer operations for a specific cashier and terminal, including starting float, cash movements, expected cash, counted cash, and variance.

### Terminal
A physical POS computer or register workstation registered in Master Data, belonging to a Branch and associated with a default inventory Warehouse.

### Transactional Outbox
An enterprise architectural pattern where secondary events (QuickBooks sync, receipt printing, inventory ledger updates) are persisted in the same relational database transaction as the primary aggregate change, and dispatched asynchronously by background workers with retry and circuit breaker protection.

---

## 2. Cross References
- [System Architecture](architecture/system-architecture.md)
- [Bounded Contexts](architecture/bounded-contexts.md)
- [Known Limitations](known-limitations.md)
- [Documentation Coverage](documentation-coverage.md)
