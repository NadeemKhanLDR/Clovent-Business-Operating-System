# CBOS Frontline Cashier Operations Guide

| Attribute | Details |
| :--- | :--- |
| **Area** | Point of Sale (POS) / Daily Cashier Operations |
| **Audience** | Frontline Cashiers, Head Cashiers, Store Managers |
| **Target Baseline** | CBOS 1.2.2 Baseline / CBOS 1.3.0 Candidate |
| **Status** | **SOURCE-VERIFIED STANDARD** |
| **Classification** | **PUBLIC-SAFE** |

---

## 1. Overview & POS Terminal Workspace

The Clovent Business Operating System (CBOS) Point-of-Sale interface (`src/Clovent.Desktop/Restaurant/Orders/RestaurantPosForm.cs`) is optimized for rapid counter and table service operations.

### Standard POS Screen Layout:
```
┌──────────────────────────────────────────────────────────────────────────────────┐
│ [Branch: Downtown]  [Terminal: REG-01]  [Shift: #104 - Open]  [Operations ▼] [Exit]│
├─────────────────────────────────────────────────┬────────────────────────────────┤
│ CATEGORY SELECTOR BAR                           │ CURRENT ORDER CHECK            │
│ [Starters] [Burgers] [Beverages] [Desserts]     │ Order #2041       Dine-In (T-4)│
├─────────────────────────────────────────────────┤ ────────────────────────────── │
│ PRODUCT SELECTION TILES                         │ 1x Classic Burger       12.00  │
│                                                 │ 1x Extra Cheese          1.50  │
│  [ Classic Burger ]   [ Double Cheeseburger ]   │ 2x Mineral Water         4.00  │
│  [ Crispy Chicken ]   [ Spicy Wings ]           │ ────────────────────────────── │
│  [ French Fries ]     [ Onion Rings ]           │ Subtotal:               17.50  │
│  [ Mineral Water ]    [ Soft Drink ]            │ Tax (PK-PRA-16%):        2.80  │
│                                                 │ Discount:                0.00  │
│                                                 │ Total Due:              20.30  │
├─────────────────────────────────────────────────┴────────────────────────────────┤
│ ACTION BAR: [Kitchen] [Hold] [Recall] [Discount] [Void Item]  │  [ PAY / SETTLE ]│
└─────────────────────────────────────────────────────────────────┴────────────────┘
```

The teal **`Operations ▼`** dropdown button in the top toolbar houses management workflows:
- `Print Last Receipt`
- `Cash Movement` (Cash In / Cash Out)
- `Refund / Return` *(Candidate feature)*
- `Shift Close`
- `End of Day` (Manager elevation required)

---

## 2. Shift Opening & Starting Float

A cashier cannot ring sales without an active, open shift.

### Opening Procedure:
1. Launch CBOS Desktop and sign in using your Username/Password or 4-digit numeric PIN.
2. If no shift is open for your user on this terminal, the **Open Shift** dialog appears automatically (`EnsureShiftAndOpenPosAsync`).
3. Count the physical opening cash (drawer seed money) in the cash drawer.
4. Enter the **Starting Cash Float** amount using the on-screen numeric keypad.
5. Click **Open Shift**. The system generates a new immutable `ShiftId`, records the timestamp, and transitions the POS status banner to **Open**.

---

## 3. Ringing Orders & Modifiers

1. **Select Order Type:** Choose **Dine-In** (prompts for Table number), **Takeaway**, or **Delivery**.
2. **Add Products:** Tap category tabs at the top, then tap product tiles. Each tap increments the item quantity.
3. **Adjust Quantities & Prices:** Highlight an item on the right-hand bill panel to adjust quantity, apply line notes (e.g. *"No onions"*), or select product modifiers (e.g. *"Extra Cheese"*).
4. **Send to Kitchen:** For prepared food, click **Kitchen** (`Send to Kitchen`). This dispatches the prep ticket to the kitchen printer or display via the Transactional Outbox while keeping the check open for further additions.
5. **Hold & Recall:** Click **Hold** to park the check while serving another customer. Click **Recall** to view and resume open checks.

---

## 4. Applying Discounts (`DiscountDialog.cs`)

Discounts can be applied at order level with mandatory audit tracking:

1. On the active order screen, click the **Discount** action button.
2. The `DiscountDialog` appears.
3. Select the discount mode:
   - **Percentage Discount (%):** Enter percentage (e.g. `10` for 10% off).
   - **Fixed Amount Discount:** Enter exact currency deduction (e.g. `5.00`).
4. Select a mandatory **Discount Reason** from the dropdown (e.g. *Employee Meal*, *Promotional Coupon*, *Manager Goodwill*).
5. Click **Apply Discount**.
6. The bill recalculates: the discount reduces taxable subtotal, and tax snapshots update accordingly.

---

## 5. Payment Settlement & Split Tenders

When the customer is ready to pay, click the prominent **`PAY / SETTLE`** button.

### 5.1 Cash Settlement
1. Select **Cash** as the payment method.
2. Enter the amount handed by the customer in **Cash Tendered** (or tap fast cash buttons like `20`, `50`, `100`).
3. The dialog displays the exact **Change Due**.
4. Click **Complete Payment**:
   - The cash drawer kicks open via the receipt printer RJ11 hook.
   - The customer receipt prints automatically.
   - The order status transitions to `Completed`.

### 5.2 External Card Settlement
1. Select **Card** as the payment method.
2. Present the payable amount to the customer on the standalone bank card terminal.
3. Once the bank card machine authorizes the transaction, enter the **Authorization Code / Reference** into CBOS.
4. Click **Complete Payment**.
*(Note: CBOS does not process or store sensitive card numbers or CVVs).*

### 5.3 Split Tender Settlement (`BillSplitDialog.cs`)
If the customer wants to pay using multiple payment methods (e.g., partial cash and partial card):
1. In the settlement dialog, click **Split Payment**.
2. The `BillSplitDialog` opens, showing the unpaid balance.
3. Enter the first payment line (e.g. Cash: `10.00`) and click **Add Tender Line**.
4. The remaining balance updates automatically.
5. Enter the second payment line (e.g. Card: `10.30`) to cover the remaining balance.
6. Once the remaining balance reaches exactly `0.00`, click **Complete Order**.

### 5.4 Customer Credit ("On Account")
For approved account customers:
1. Select **On Account** and select the customer record.
2. The system checks the customer's current balance against their assigned credit limit.
3. If within the limit, click **Complete Payment**; a debit entry posts to the customer's accounts receivable subledger.
4. If the credit limit is exceeded, the system halts and prompts for **Manager Elevation PIN**. If approved by a manager, the override is logged.

---

## 6. Receipt Reprints

1. Click the teal **`Operations ▼`** button in the top toolbar.
2. Click **`Print Last Receipt`**.
3. The thermal receipt printer reprints the last finalized check.

> [!IMPORTANT]
> **Scoping Boundary:** In accordance with source code invariants, `Print Last Receipt` is strictly scoped to the **last completed order of the active shift**. To view or reprint orders completed earlier in the day or on prior shifts, an authorized supervisor must access Back Office -> **Order History** (`OrderHistoryView.cs`).

---

## 7. Mid-Shift Cash Movements (Cash In / Cash Out)

To ensure the physical cash drawer reconciles with the system ledger:

1. Click **`Operations ▼`** -> **`Cash Movement`**.
2. Choose movement type:
   - **Cash In (Float Top-up):** Adding extra change or cash seed to the drawer.
   - **Cash Out (Safe Drop / Petty Cash):** Skimming large bills into the manager's safe or paying an authorized cash vendor expense.
3. Enter the exact **Amount**.
4. Enter a mandatory **Reason / Description**.
5. Click **Record Movement**. A cash movement voucher prints, and the movement updates the shift's expected cash calculation.

---

## 8. Itemized Refunds & Returns (`RefundOrderDialog.cs`)

> [!WARNING]
> **CANDIDATE CAPABILITY UNDER ACTIVE ACCEPTANCE:**
> The Compensating Refund Ledger and `RefundOrderDialog` are candidate implementations in CBOS 1.3.0. Operational certification in Windows Sandbox is currently in progress by the acceptance stream.

### Refund Procedure:
1. Click **`Operations ▼`** -> **`Refund / Return`**.
2. The `RefundOrderDialog` opens.
3. Locate the original completed order by entering the receipt number or scanning the receipt barcode.
4. Select the specific item line(s) and quantity being returned.
5. Select the item **Disposition**:
   - **Restock to Inventory:** Item is unopened and returnable to stock balance.
   - **Waste / Damaged:** Item is spoiled or discarded; recorded as shrinkage without inventory restock.
6. The system calculates the refundable amount and enforces financial ceilings:
   - External Card refunds cannot exceed the amount originally paid by card on the order.
   - Cash refunds require sufficient cash in the drawer.
7. Enter a mandatory **Refund Reason**.
8. Click **Process Refund**. Manager authorization PIN is required.
9. An immutable compensating refund record (`Refund` / `RefundLine`) is committed to the database, reversing ledger balances without deleting historical records.

---

## 9. Shift Close & Blind Cash Reconciliation (`ShiftCloseDialog.cs`)

At the conclusion of a cashier's working hours, the shift must be reconciled and closed.

### Step-by-Step Shift Closure:
1. Ensure all open checks for this terminal are settled or transferred.
2. Click **`Operations ▼`** -> **`Close Shift`**.
3. The `ShiftCloseDialog` opens.
4. **Perform Blind Cash Count:**
   - Remove cash drawer insert.
   - Physically count all currency bills and coins.
   - Enter the total into **Counted Cash**.
   *(Note: The system intentionally hides the theoretical expected cash until the physical count is entered to enforce blind reconciliation).*
5. **Review Variance:**
   The system calculates expected cash:
   $$\text{Expected Cash} = \text{Starting Float} + \text{Net Cash Sales} + \text{Cash In} + \text{Collections} - \text{Cash Out}$$
   $$\text{Variance} = \text{Counted Cash} - \text{Expected Cash}$$
6. **Variance Justification:**
   - If $\text{Variance} = 0.00$, the shift balances perfectly.
   - If $\text{Variance} \neq 0.00$ (Cash Short or Cash Over), enter a mandatory **Variance Explanation Reason**.
7. Click **Confirm Shift Close**.
8. A comprehensive Shift Close Summary Z-Report prints, detailing tender breakdowns, sales counts, discounts, and variances. The shift is locked and cannot be reopened.

---

## 10. Business Day Close (End of Day)

At the end of daily trading across all shifts:
1. Store Manager logs in and clicks **`Operations ▼`** -> **`End of Day`**.
2. The `EndOfDayCloseDialog` consolidates all closed shifts for the business date, verifies that zero unclosed shifts remain, and captures branch daily totals.
3. The business day is closed and frozen in the financial ledger.
