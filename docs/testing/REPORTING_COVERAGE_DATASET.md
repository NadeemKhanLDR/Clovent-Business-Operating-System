# Reporting Coverage Dataset & Reconciliation Documentation

## Overview & Integrity Guarantee

This document defines the verified dataset provisioned for operational reporting testing in the development environment of the **Clovent Business Operating System** (`Clovent_Restaurant`, `Clovent_Inventory`, `Clovent_Catalog`, `Clovent_MasterData`).

> [!IMPORTANT]
> **Zero Direct SQL Transactional Mutation Guarantee**:
> The final dataset was generated **without direct transactional database repair** (no manual `UPDATE` or `INSERT` statements against `Restaurant.Orders`, `Restaurant.OrderLines`, `Restaurant.Payments`, `Restaurant.CustomerLedgerEntries`, `Restaurant.Customers`, `Inventory.InventoryTransactions`, or `Inventory.WarehouseStocks`).
> Every transaction was created strictly via **legitimate Application Workflows** (`RecordCustomerPaymentCommand`, `RecordBulkCustomerPaymentsCommand`, `CloseShiftCommand`, `OpenShiftCommand`) or **Development Seed using Domain/Application Services** (`Order.Create`, `OrderLine.Create`, `Payment.Create`, `WarehouseStock.Receive`, `WarehouseStock.Issue`, `InventoryTransaction.Create`, `KitchenTicket.Create`, `SuggestionEvent.Create`).

---

## 1. Scenario Workflow & Origin Classification

| Reference | Classification | Workflow Used | Expected Result | Where User Can Verify |
| :--- | :--- | :--- | :--- | :--- |
| `ORD-RPT-S2-SRV` | **APPLICATION WORKFLOW** | POS Sale → Service Order Workflow | Food Heating x2 @ Rs. 30 = Rs. 60 Cash, 100% margin, zero stock impact | Sales Summary → Items / Item Types / Profitability |
| `GRN-2026-09-001` | **DEVELOPMENT SEED (DOMAIN)** | Inventory Goods Receipt Workflow (`naanStock.Receive(100)`) | +100 Naan @ Rs. 20 cost received into warehouse | Reports → Inventory Movements / Stock On Hand |
| `ORD-RPT-S2-001` | **DEVELOPMENT SEED (DOMAIN)** | POS Dine-In Order Workflow | Biryani x1, Naan x2 = Rs. 500 paid via Mobile Wallet | Sales Summary → Orders / Payments |
| `ORD-RPT-S2-002` | **DEVELOPMENT SEED (DOMAIN)** | POS TakeAway Order Workflow | Biryani x1, Naan x4 = Rs. 550 paid via Credit Card | Sales Summary → Orders / Payments |
| `ORD-RPT-S2-003` | **DEVELOPMENT SEED (DOMAIN)** | POS Split Tender Workflow | Biryani x1, Naan x2 = Rs. 500 (Rs. 250 Cash + Rs. 250 Wallet) | Sales Summary → Payments / Cash Summary |
| `ORD-RPT-S2-004` | **DEVELOPMENT SEED (DOMAIN)** | POS On Account Credit Workflow | Biryani x2, Naan x4 = Rs. 1,000 On Account (Customer B) | Sales Summary → Customers / Receivables |
| `ORD-RPT-S2-005` | **DEVELOPMENT SEED (DOMAIN)** | POS On Account Credit Workflow | Biryani x4, Naan x8 = Rs. 2,000 On Account (Customer C, 8 Naan issued) | Sales Summary → Orders / Customers |
| `PAY-RPT-S2-C01` | **APPLICATION WORKFLOW** | `RecordCustomerPaymentCommand` | Rs. 800 Cash collection applied to `ORD-RPT-S2-005`, A/R reduces to 1,200 | Reports → Customer Receivables (Customer C) |
| `PAY-RPT-S2-C02` | **APPLICATION WORKFLOW** | `RecordCustomerPaymentCommand` | Rs. 500 Cash collection applied to `ORD-RPT-S2-005`, A/R reduces to 700 | Reports → Customer Receivables (Customer C) |
| `ORD-RPT-S2-006` | **DEVELOPMENT SEED (DOMAIN)** | POS On Account Credit Workflow | Biryani x1, Naan x1 = Rs. 475 On Account (Customer D) | Sales Summary → Customers / Receivables |
| `PAY-RPT-S2-D01` | **APPLICATION WORKFLOW** | `RecordCustomerPaymentCommand` | Rs. 700 Cash paid: 475 settles A/R to 0, 225 becomes Customer Advance | Reports → Customer Receivables (Customer D) |
| `ORD-RPT-S2-006B`| **DEVELOPMENT SEED (DOMAIN)** | Advance Application POS Sale | Biryani x1 = Rs. 450 (Rs. 225 Advance + Rs. 225 Cash); Advance becomes 0 | Sales Summary → Payments / Customer Receivables |
| `ORD-RPT-S2-007` | **DEVELOPMENT SEED (DOMAIN)** | Delivery Order Workflow | Biryani x1, Naan x2 + Delivery Fee Rs. 150 = Rs. 650 Mobile Wallet | Sales Summary → Order Types / Orders |
| `RCV-BATCH-S2-001`| **APPLICATION WORKFLOW** | `RecordBulkCustomerPaymentsCommand`| Bulk batch: Corp (300 Cash), B (500 Cash), C (200 Wallet) | Reports → Customer Receivables / Shift History |
| `ORD-RPT-S2-HELD`| **DEVELOPMENT SEED (DOMAIN)** | POS Hold Order Workflow | 1x Biryani Rs. 450, Status = `Held`, 0 payments | POS Screen → Hold Orders / Recall |
| `ORD-RPT-S2-RUN` | **DEVELOPMENT SEED (DOMAIN)** | POS Open Order Workflow | 2x Biryani + 4x Naan = Rs. 1,000, Status = `Open`, 0 payments | POS Screen → Running Orders |
| Kitchen Ticket | **DEVELOPMENT SEED (DOMAIN)** | Kitchen Display Workflow | Attached to `ORD-RPT-S2-RUN`, Status = `New` | Kitchen Ticket Viewer |
| Upsell Events | **DEVELOPMENT SEED (DOMAIN)** | Smart Recommendation Workflow | Naan Offered & Accepted; Karahi Offered & Dismissed | Reports → Upsell Performance |
| Shift #1003 | **APPLICATION WORKFLOW** | `CloseShiftCommand` | Expected Rs. 21,175 = Counted Rs. 21,175, Variance = Rs. 0.00 | Reports → Shift History / Sales Summary Cash |
| Shift #1004 | **APPLICATION WORKFLOW** | `OpenShiftCommand` | Starting float Rs. 4,000, Status = `Open`, active for testing | POS Register Screen |

---

## 2. Purchased / Resale Stock Reconciliation (Naan - SKU: NAAN-STD)

- **Cost Price**: Rs. 20.00
- **Selling Price**: Rs. 25.00
- **Gross Margin**: 20.00% (Rs. 5.00 profit / Rs. 25.00 sale)

### Chronological Inventory Movement Ledger

| Step | Reference | Transaction Type | Qty In | Qty Out | Running Qty | Transaction Notes |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| **1** | `Seed` | `Receipt` | 100.00 | - | 100.00 | Initial Naan stock for restaurant operations |
| **2** | `ORD-RPT-002` .. `011` | `Issue` (8 orders) | - | 30.00 | 70.00 | Phase 1 restaurant sales (2+4+2+2+4+12+2+2) |
| **3** | `GRN-2026-09-001` | `Receipt` | 100.00 | - | 170.00 | Direct Vendor Delivery (100 Naan @ Rs.20 cost) |
| **4** | `ORD-RPT-S2-005` | `Issue` | - | 8.00 | 162.00 | Sale for ORD-RPT-S2-005 (Phase 2 seed) |
| **5** | `ORD-RPT-S2-003` | `Issue` | - | 2.00 | 160.00 | Sale for ORD-RPT-S2-003 (Phase 2 seed) |
| **6** | `ORD-RPT-S2-001` | `Issue` | - | 2.00 | 158.00 | Sale for ORD-RPT-S2-001 (Phase 2 seed) |
| **7** | `ORD-RPT-S2-004` | `Issue` | - | 4.00 | 154.00 | Sale for ORD-RPT-S2-004 (Phase 2 seed) |
| **8** | `ORD-RPT-S2-006` | `Issue` | - | 1.00 | 153.00 | Sale for ORD-RPT-S2-006 (Phase 2 seed) |
| **9** | `ORD-RPT-S2-002` | `Issue` | - | 4.00 | 149.00 | Sale for ORD-RPT-S2-002 (Phase 2 seed) |
| **10**| `ORD-RPT-S2-007` | `Issue` | - | 2.00 | 147.00 | Sale for ORD-RPT-S2-007 (Phase 2 seed) |

### Summary & Database Match
- **Total Receipts**: 200.00 units (100.00 baseline + 100.00 `GRN-2026-09-001`)
- **Total Sales Issues**: 53.00 units (30.00 Phase 1 + 23.00 Phase 2)
- **Calculated Closing Stock**: 200.00 - 53.00 = **147.00 units**
- **WarehouseStocks.QuantityOnHand**: **147.0000 units**
- **MATCH**: **YES** (Zero discrepancy, purely driven by `Receive` and `Issue` methods)

---

## 3. Sales Summary KPIs Semantics & Exact Reconciliations

### KPI Financials
- **Gross Item Sales**: Rs. 16,175.00
- **Discounts**: Rs. 0.00
- **Net Item Sales**: Rs. 16,175.00
- **Delivery / Service Fees**: Rs. 350.00 (`ORD-RPT-S2-007` Rs. 150 + baseline delivery orders Rs. 200)
- **Total Bill Sales**: **Rs. 16,525.00** (`16,175.00 + 350.00`)
- **Completed Orders Count**: 22 orders
- **Average Order / Bill Value**: **Rs. 751.14** (`16,525.00 / 22 = 751.136...`)

### Orders / Bills Tab Column Terminology
- Column header: **`Line Items`** (formerly "Items").
- Represents line record count per bill (e.g. 1, 2, 3), distinctly separating line item count from physical product units sold.

### Order Types Tab Quantity Aggregation
- Column header: **`Qty Sold`** (formerly "Items Sold").
- Aggregates actual physical units sold (`SUM(OrderLine.Quantity)`), reconciling 1:1 with the **Items** tab.
  - **DineIn**: 34 units sold
  - **TakeAway**: 21 units sold
  - **Delivery**: 11 units sold
  - **Total Order Types Qty Sold**: 34 + 21 + 11 = **66 units**
  - **Items Tab Total Qty Sold**: **66 units**
  - **Reconciliation Match**: **YES (Exact 1:1)**

---

## 4. Customer Sales & Receivables Movement Reconciliations

### Customer Sales Semantics
Every customer row strictly satisfies:
$$\text{ItemSales} - \text{Discount} + \text{Fees} = \text{BillTotal} = \text{Paid} + \text{OnAccount}$$

- **Item Sales across all customers**: Rs. 16,175.00
- **Fees across all customers**: Rs. 350.00
- **Total Bill Sales across all customers**: **Rs. 16,525.00**
- **Paid across all customers**: Rs. 10,520.00
- **On Account across all customers**: Rs. 6,005.00
- **Bill Total Reconciliation**: $10,520.00 + 6,005.00 = \mathbf{16,525.00}$ (`MATCH = YES`)

### Receivables Movement Accounting Identities
The Receivables Movement enforces strict mathematical integrity:
1. **Closing A/R Identity**:
   $$\text{ClosingAR} = \text{OpeningAR} + \text{NewOnAccount} - \text{CollectionsApplied} - \text{AdvanceApplied}$$
2. **Closing Advance Identity**:
   $$\text{ClosingAdvance} = \text{OpeningAdvance} + \text{AdvanceReceived} - \text{AdvanceUsed}$$

### Customer Breakdown

| Customer | Opening A/R | New On-Account | Collections Applied | Advance Applied | Closing A/R | Opening Adv | Adv Received | Adv Used | Closing Adv |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| **Waris Ali** | 0.00 | 1,360.00 | 0.00 | 70.00 | **1,290.00** | 70.00 | 0.00 | 70.00 | **0.00** |
| **Sana Textile** | 0.00 | 475.00 | 475.00 | 0.00 | **0.00** | 0.00 | 225.00 | 225.00 | **0.00** |
| **Corporate Lunch** | 130.00 | 0.00 | 130.00 | 0.00 | **0.00** | 0.00 | 170.00 | 0.00 | **170.00** |
| **Hassan Brothers** | 0.00 | 2,000.00 | 1,500.00 | 0.00 | **500.00** | 0.00 | 0.00 | 0.00 | **0.00** |
| **Malik Faisal** | 0.00 | 1,000.00 | 500.00 | 0.00 | **500.00** | 0.00 | 0.00 | 0.00 | **0.00** |
| **John Smith** | 0.00 | 1,170.00 | 0.00 | 0.00 | **1,170.00** | 0.00 | 0.00 | 0.00 | **0.00** |
| **FOOTER TOTALS** | **130.00** | **6,005.00** | **2,605.00** | **70.00** | **3,460.00** | **70.00** | **395.00** | **295.00** | **170.00** |

#### Footer Identity Verification:
- **A/R**: $130.00 + 6,005.00 - 2,605.00 - 70.00 = \mathbf{3,460.00}$ (`MATCH = YES`)
- **Advance**: $70.00 + 395.00 - 295.00 = \mathbf{170.00}$ (`MATCH = YES`)

---

## 5. Shift Cash Drawer Balancing & Reconciliation

### Shift #1003 Drawer Formula
$$\text{OpeningFloat} + \text{CashSales} + \text{CashCollections} + \text{CashIn} - \text{CashOut} = \text{ExpectedCash}$$

| Component | Value | Description |
| :--- | :--- | :--- |
| **Starting / Opening Float** | Rs. 4,000.00 | Cash placed in drawer at shift start |
| **Cash Sales** | Rs. 13,875.00 | Direct cash collected on finalized orders |
| **Cash Collections** | Rs. 2,800.00 | Customer debt payments collected in cash during shift |
| **Cash In** | Rs. 1,000.00 | Petty cash drawer replenishment |
| **Cash Out** | Rs. 500.00 | Kitchen supplies / operational expense |
| **Expected Cash** | **Rs. 21,175.00** | $4,000 + 13,875 + 2,800 + 1,000 - 500 = 21,175$ |
| **Counted Cash** | **Rs. 21,175.00** | Physical cash counted by cashier at shift close |
| **Cash Variance** | **Rs. 0.00** | Difference between Counted and Expected Cash |
| **Status** | **Closed** | Reconciled with zero variance |

### Active Register Shift for Testing (Shift #1004)
- **Shift Number**: `1004`
- **Status**: `Open`
- **Starting Float**: Rs. 4,000.00
- **Active and Ready for Subsequent Manual POS Testing**

---

## 6. High-DPI & User Interface Layout Upgrades

1. **`ShiftDetailDialog` Redesign**:
   - `infoGrid` expanded to 6 rows with separate cells:
     - `Expected Cash` (Rs. 21,175.00)
     - `Counted Cash` (Rs. 21,175.00)
     - `Cash Variance` (Rs. 0.00)
     - `Cash Collections` (Rs. 2,800.00)
   - Eliminates compound text strings ("Expected: Rs. 21,175.00 (Variance: Rs. 0.00)") that caused truncation at 250% high-DPI scaling.
2. **Sales Summary Cash Summary Grid Redesign**:
   - Replaced redundant tender breakdown duplicate with true cash drawer reconciliation by shift.
   - Dedicated columns: `Shift #`, `Cashier`, `Opening Float`, `Cash Sales`, `Collections`, `Cash In`, `Cash Out`, `Expected Cash`, `Counted Cash`, `Variance`, `Status`.
3. **Payments Tab Share %**:
   - Order tenders are evaluated against Total Bill Sales (Rs. 16,525.00).
   - Customer debt collections are evaluated against Total Collections (Rs. 3,000.00).
