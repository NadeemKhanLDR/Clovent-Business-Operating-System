# Clovent Business Operating System - Role & Permission Matrix

This document defines the production authorization model and role-permission matrix across core store roles in Clovent Business Operating System (CBOS).

---

## 1. Role Definitions

| Role | Target Persona | Scope of Authority |
|---|---|---|
| **Cashier** | Frontline POS Terminal Operator | Order entry, tender processing, bill printing, standard shifts. Restricted from voids, refunds, discounts above policy, and inventory/admin settings. |
| **Supervisor** | Floor Lead / Head Cashier | All Cashier permissions plus authorization overrides (discounts, price overrides, voiding active orders, manager approval for customer credit exceptions). |
| **Manager** | Store / Branch General Manager | Full operational oversight of branch: inventory receiving, adjustments, shift balancing, financial closing, local user management, customer credit approvals. |
| **Administrator** | System / Franchise Administrator | Full system authority across all companies and branches: database settings, license management, global business settings, audit logging, system commissioning. |

---

## 2. Core Operation Permission Matrix

| Operation Category | Specific Operation | Cashier | Supervisor | Manager | Administrator | Implementation Status |
|---|---|:---:|:---:|:---:|:---:|:---:|
| **POS Sales** | Create & Tender Standard Cash/Card Order | **Allowed** | **Allowed** | **Allowed** | **Allowed** | Implemented (`pos.order.create`, `pos.order.tender`) |
| **Discounts** | Apply Standard Line Discount (within policy) | **Allowed** | **Allowed** | **Allowed** | **Allowed** | Implemented (`pos.discount.apply`) |
| **Discounts** | Manager Discount Override (> Maximum % limit) | Denied | **Allowed** | **Allowed** | **Allowed** | Implemented (`pos.discount.override` via Manager Authorization) |
| **Pricing** | Custom Price Override | Denied | **Allowed** | **Allowed** | **Allowed** | Implemented (`pos.price.override` via Manager Authorization) |
| **Order Modification** | Void Order / Line Item after Sending to Kitchen | Denied | **Allowed** | **Allowed** | **Allowed** | Implemented (`pos.order.void` via Manager Authorization) |
| **Returns & Refunds** | Process Customer Return / Refund Payment | Denied | **Allowed** | **Allowed** | **Allowed** | Implemented (`pos.order.refund` via Manager Authorization) |
| **Customer Credit** | Tender to On-Account (within credit limit) | **Allowed** | **Allowed** | **Allowed** | **Allowed** | Implemented (`pos.credit.tender`) |
| **Customer Credit** | Credit Limit Override (overdue/over-limit account) | Denied | Denied | **Allowed** | **Allowed** | Implemented (`pos.credit.override` via Manager Authorization) |
| **Receivables** | Record Customer Collection Payment | **Allowed** | **Allowed** | **Allowed** | **Allowed** | Implemented (`customers.receivables.collect`) |
| **Receivables** | Bulk Settlement / Bad Debt Write-off | Denied | Denied | **Allowed** | **Allowed** | Implemented (`customers.receivables.settle`) |
| **Cash Control** | Cash In / Cash Out (Petty Cash / Paid Out) | Denied | **Allowed** | **Allowed** | **Allowed** | Implemented (`restaurant.cashmovements.create`) |
| **Shift Management** | Open / Close Cash Drawer Shift & Count | **Allowed** | **Allowed** | **Allowed** | **Allowed** | Implemented (`restaurant.shifts.manage`) |
| **Inventory** | Receive Purchase Order / Inward Goods | Denied | Denied | **Allowed** | **Allowed** | Implemented (`inventory.transactions.receive`) |
| **Inventory** | Issue Goods / Write-Off Wastage | Denied | Denied | **Allowed** | **Allowed** | Implemented (`inventory.transactions.issue`) |
| **Inventory** | Stock Adjustment (Quantity Correction) | Denied | Denied | **Allowed** | **Allowed** | Implemented (`inventory.adjustments.create`) |
| **Inventory** | Inter-Branch / Inter-Warehouse Stock Transfer | Denied | Denied | **Allowed** | **Allowed** | Implemented (`inventory.transfers.create`) |
| **System Settings** | Database Connection Configuration | Denied | Denied | Denied | **Allowed** | Implemented (`AdministrativePrivilegeChecker` + Windows UAC) |
| **System Settings** | Software License Import & Renewal | Denied | Denied | Denied | **Allowed** | Implemented (`AdministrativePrivilegeChecker` + Windows UAC) |
| **Administration** | User Creation, Role Assignment, Password Reset | Denied | Denied | Branch Only | **Allowed** | Implemented (`feature.users.create`, `feature.roles.assign`) |
| **Administration** | Business & Regional Settings Configuration | Denied | Denied | Denied | **Allowed** | Implemented (`feature.businesssettings.edit`) |

---

## 3. Enforcement Mechanisms

1. **Menu & View Filtering:** Navigation menus dynamically filter visible modules and navigation tiles based on granted permissions.
2. **Elevated Action Interception:** Actions requiring Supervisor or Manager privileges (e.g., void, refund, discount override) invoke `ManagerAuthorizationForm` modal prompting for a valid Manager/Supervisor PIN or credentials before execution.
3. **OS-Level Separation:** Sensitive machine-level operations (Database credentials, License import) require Windows Administrator UAC elevation before login, and application Administrator permissions post-login.
