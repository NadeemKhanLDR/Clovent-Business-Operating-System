# CBOS Domain Data Model Reference

| Attribute | Details |
| :--- | :--- |
| **Area** | Relational Data Model & Aggregate Specifications |
| **Audience** | Backend Engineers, Database Architects, Data Analysts |
| **Last Reviewed Version** | CBOS 1.2.2 |
| **Status** | **SOURCE-VERIFIED** |
| **Classification** | **PUBLIC-SAFE** |

---

## 1. Domain Entity Modeling Principles

In accordance with Domain-Driven Design (DDD) principles:
1. Every domain aggregate root possesses a strongly-typed identifier (e.g. `OrderId`, `ShiftId`, `ProductVariantId`).
2. Entities manage their own state transitions through domain methods (`Order.AddLine()`, `Shift.Close()`), raising immutable Domain Events.
3. Relationships within a bounded context are configured using EF Core Fluent API with explicit backing fields.
4. Relationships across bounded contexts are strictly logical identifiers (e.g., `Order.TerminalId`). There are zero physical foreign keys across SQL schemas.

---

## 2. Core Entities by Bounded Context

### 2.1 Identity Context (`[Identity]` Schema)

| Entity / Aggregate | Primary Key | Key Attributes | Relationships & Rules |
| :--- | :--- | :--- | :--- |
| **`Organization`** | `OrganizationId` (Guid) | `Code`, `Name`, `Status`, `CreatedAtUtc` | Root of multi-company hierarchy. Owns one or more `Company` entities. |
| **`Company`** | `CompanyId` (Guid) | `OrganizationId`, `Code`, `LegalName`, `TaxId`, `BaseCurrencyCode`, `Status` | Legal business entity. Owns one or more physical `Branch` records. |
| **`Branch`** | `BranchId` (Guid) | `CompanyId`, `Code`, `Name`, `Address`, `Status` | Operational location (outlet/store). Backs POS workstations and dining areas. |
| **`User`** | `UserId` (Guid) | `Username`, `PasswordHash`, `Salt`, `PinCodeHash`, `Email`, `BranchId`, `Status` | Operator/employee record. Can log in via username/password or cashier PIN. |
| **`Role`** | `RoleId` (Guid) | `Name`, `Description`, `IsSystemRole` | System roles include `Administrator`, `Manager`, `Cashier`, `Supervisor`. |
| **`UserRole`** | Composite (`UserId`, `RoleId`) | `AssignedAtUtc` | Many-to-many relationship linking users to roles. |
| **`Permission`** | `PermissionId` (Guid) | `Code`, `Category`, `Description` | 214 granular permission entries governing UI visibility and command execution. |

---

### 2.2 MasterData Context (`[MasterData]` Schema)

| Entity / Aggregate | Primary Key | Key Attributes | Relationships & Rules |
| :--- | :--- | :--- | :--- |
| **`Terminal`** | `TerminalId` (Guid) | `BranchId`, `Code`, `Name`, `DefaultWarehouseId`, `Status` | Physical register hardware definition. Bound during First-Run Commissioning. |
| **`Warehouse`** | `WarehouseId` (Guid) | `BranchId`, `Code`, `Name`, `Status` | Physical stock storage location backing terminal inventory deductions. |
| **`Currency`** | `CurrencyId` (Guid) | `Code` (ISO 4217), `Symbol`, `Name`, `DecimalPlaces`, `IsBaseCurrency` | Defines monetary display formatting and calculation rules across the system. |
| **`UnitOfMeasure`** | `UnitOfMeasureId` (Guid) | `Code`, `Name`, `Precision` | Measurement standards (e.g., `KG`, `PCS`, `LTR`, `BOX`). |
| **`NumberSequence`** | `NumberSequenceId` (Guid) | `Context`, `Prefix`, `NextNumber`, `FormatPattern` | Deterministic number sequence generator for human-readable identifiers. |

---

### 2.3 Catalog Context (`[Catalog]` Schema)

| Entity / Aggregate | Primary Key | Key Attributes | Relationships & Rules |
| :--- | :--- | :--- | :--- |
| **`Category`** | `CategoryId` (Guid) | `ParentCategoryId`, `Code`, `Name`, `DisplayOrder`, `Status` | Hierarchical category tree organizing menu items on POS left rail. |
| **`Product`** | `ProductId` (Guid) | `CategoryId`, `BrandId`, `Code`, `Name`, `Description`, `Status` | Conceptual product identity. Owns one or more sellable `ProductVariant` items. |
| **`ProductVariant`** | `ProductVariantId` (Guid) | `ProductId`, `Sku`, `VariantName`, `Status` | Concrete sellable item (e.g., "Latte - Large"). |
| **`Barcode`** | `BarcodeId` (Guid) | `ProductVariantId`, `BarcodeValue`, `IsPrimary` | Barcodes mapped to variants for high-speed scanner entry. |
| **`PricingTier`** | `PricingTierId` (Guid) | `Code`, `Name`, `IsDefault` | Customer pricing categories (e.g. Retail, Wholesale, VIP, Staff). |
| **`ProductPrice`** | `ProductPriceId` (Guid) | `ProductVariantId`, `PricingTierId`, `Price`, `TaxRate` | Price matrix determining active price based on tier and variant. |

---

### 2.4 Inventory Context (`[Inventory]` Schema)

| Entity / Aggregate | Primary Key | Key Attributes | Relationships & Rules |
| :--- | :--- | :--- | :--- |
| **`WarehouseStock`** | Composite (`WarehouseId`, `VariantId`) | `QuantityOnHand`, `QuantityReserved`, `ReorderLevel` | Materialized stock balance. Modified exclusively through formal transactions. |
| **`InventoryTransaction`** | `InventoryTransactionId` (Guid) | `WarehouseId`, `VariantId`, `TransactionType`, `Quantity`, `UnitCost`, `Reference` | Immutable inventory ledger entry. Types: `Receipt`, `Issue`, `Transfer`, `Adjustment`, `Reserve`, `Release`. |
| **`InventoryAdjustment`** | `InventoryAdjustmentId` (Guid) | `WarehouseId`, `Reason`, `Status`, `CreatedAtUtc` | Formal inventory reconciliation audit adjusting stock-on-hand. |

---

### 2.5 Restaurant Context (`[Restaurant]` Schema)

| Entity / Aggregate | Primary Key | Key Attributes | Relationships & Rules |
| :--- | :--- | :--- | :--- |
| **`DiningArea`** | `DiningAreaId` (Guid) | `BranchId`, `Code`, `Name`, `DisplayOrder` | Physical dining section (e.g., Main Hall, Terrace, VIP Lounge). |
| **`Table`** | `TableId` (Guid) | `DiningAreaId`, `TableNumber`, `Capacity`, `Status` | Dining table. Status: `Available`, `Occupied`, `Reserved`, `Billed`. |
| **`Order`** | `OrderId` (Guid) | `OrderNumber`, `OrderType`, `Status`, `TableId`, `TerminalId`, `BranchId`, `Subtotal`, `DiscountAmount`, `TaxAmount`, `TotalAmount`, `PaidAmount` | Primary transaction aggregate. Status: `Open`, `Held`, `Completed`, `Voided`, `Cancelled`. Owns `OrderLine` items. |
| **`OrderLine`** | `OrderLineId` (Guid) | `OrderId`, `VariantId`, `ItemName`, `Quantity`, `UnitPrice`, `LineTotal`, `IsVoided` | Individual item entry in order. Tracks quantity modifications and price overrides. |
| **`Payment`** | `PaymentId` (Guid) | `OrderId`, `PaymentMethodId`, `Amount`, `IsVoided`, `ShiftId`, `IdempotencyKey` | Tendered amount. Supports split tender. Voiding marks `IsVoided = true`. |
| **`PaymentMethod`** | `PaymentMethodId` (Guid) | `Name`, `Status` | Available payment tender types (e.g., Cash, Card, Mobile Wallet, On Account). |
| **`Shift`** | `ShiftId` (Guid) | `ShiftNumber`, `TerminalId`, `CashierId`, `Status`, `StartingCash`, `ExpectedCash`, `CountedCash`, `CashVariance`, `OpenedAtUtc`, `ClosedAtUtc` | Cashier drawer session. Owns mid-shift `CashMovement` entries. |
| **`CashMovement`** | `CashMovementId` (Guid) | `ShiftId`, `Type` (`CashIn` / `CashOut`), `Amount`, `Reason`, `UserId` | Mid-shift drawer adjustments. |
| **`Customer`** | `CustomerId` (Guid) | `Code`, `Name`, `MobileNumber`, `CreditLimit`, `OutstandingBalance` | Customer account managing credit sales and receivables. Tracks net `OutstandingBalance` (receivable when positive; advance credit balance when negative via computed `AdvanceBalance`). |
| **`CustomerLedgerEntry`** | `CustomerLedgerEntryId` (Guid) | `CustomerId`, `Date`, `Reference`, `Description`, `Debit`, `Credit`, `RunningBalance`, `ShiftId`, `PaymentMethod` | Single-entry subledger for customer accounts receivable. Preserves approved current behavior; dual entries and automated advance creation/consumption require a separately reviewed financial contract. |
| **`CustomerPaymentAllocation`**| `AllocationId` (Guid) | `PaymentId`, `CustomerId`, `AmountAllocated` | Links customer payments to outstanding receivables. |
| **`OutboxMessage`** | `OutboxMessageId` (Guid) | `MessageType`, `Payload`, `Status`, `RetryCount`, `NextRetryUtc`, `CreatedAtUtc` | Transactional outbox table for asynchronous integration dispatch (TASK-07 automatic startup). |

---

## 3. Financial Invariants & Calculation Rules

### Customer Bill Reconciliation:
$$\text{Total Amount} = \sum(\text{OrderLine.LineTotal}) - \text{DiscountAmount} + \text{ServiceCharge} + \text{TaxAmount}$$
$$\text{Applied Payments} + \text{On Account Applied} = \text{Total Amount}$$
*(Applied Payments represents net settled tender across payment methods, e.g. Net Cash Applied + Card, with $\text{Net Cash Applied} = \text{Cash Tendered} - \text{Change}$. Terms must not double-count).*

### Shift Cash Drawer Reconciliation:
$$\text{Expected Cash} = \text{StartingCash} + \text{Cash In} + \text{Net Cash Sales} + \text{Cash Collections} - \text{Cash Out}$$
$$\text{Cash Variance} = \text{CountedCash} - \text{Expected Cash}$$
*(where $\text{Net Cash Sales} = \sum(\text{Cash Tendered} - \text{Change})$ from cash-settled orders; On Account credit sales, non-cash tenders, and existing advance usage are strictly excluded from drawer cash. For open shifts, `CountedCash` and `CashVariance` are strictly `null` and displayed as `N/A`. Unresolved reconciliation mappings are marked as TASK-03/TASK-06 contract work).*

---

## 4. Cross References
- [Database Architecture](database-architecture.md)
- [Migrations Guide](migrations.md)
- [Order Lifecycle Documentation](../pos/order-lifecycle.md)
- [Shifts & Cash Management](../pos/shifts-and-cash-management.md)
