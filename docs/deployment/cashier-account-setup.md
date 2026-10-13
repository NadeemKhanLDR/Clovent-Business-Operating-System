# CBOS Cashier Account Setup & Permissions Guide

| Attribute | Details |
| :--- | :--- |
| **Area** | Identity & Access Management / Frontline Staff Onboarding |
| **Audience** | Shop Owners, Store Managers, System Administrators |
| **Target Baseline** | CBOS 1.2.2 Baseline / CBOS 1.3.0 Candidate |
| **Status** | **SOURCE-VERIFIED STANDARD** |
| **Classification** | **PUBLIC-SAFE** |

---

## 1. Overview & Security Architecture

In Clovent Business Operating System (CBOS), initial system commissioning provisions **only** the root `Administrator` account. Frontline staff must never operate POS registers using Administrator accounts.

Store administrators provision dedicated frontline cashier accounts through the Back-Office Identity subsystem. Each cashier account receives:
1. A unique login handle (`UserName`) and display name.
2. Branch assignment binding the cashier to their store location.
3. The least-privilege `Cashier` role.
4. Standard password credentials and an optional 4-digit quick sign-in PIN for fast POS terminal access.

---

## 2. Navigating to User Administration

The user management interface is verified in source code at:
`src/Clovent.Desktop/Forms/Identity/Users/UsersForm.cs`

### UI Navigation Route:
1. Log in to the CBOS Back Office using Administrator or Manager credentials.
2. In the top ribbon bar, select the **Users** ribbon tab (`NavigationPage.Users`).
3. In the **Security** ribbon group, click the **Users** button (Icon: `bo_user.svg`).
4. The **User Administration** screen opens, presenting the user grid and security action toolbar.

```
Ribbon: [ Users Tab ]
            │
            └── Group: [ Security ]
                          │
                          └── Action: [ Users ]  ──> Opens UsersForm
```

> [!NOTE]
> **Permission Gate:** Access to this screen requires the `menu.users` permission and `users.view` feature permission. Users lacking this permission will not see the navigation tile.

---

## 3. Step-by-Step: Creating a Cashier Account

### Step 1: Open the Create User Dialog
In the `UsersForm` top action toolbar, click the **Add User** button (`BtnAdd`).
This launches the `UserEditForm` dialog.

### Step 2: Populate User Identity Fields
- **Email:** Enter the cashier's email address (e.g. `cashier1@store.local`).
  *(Note: Email serves as a unique account identifier and cannot be renamed once created).*
- **Username:** Enter the cashier's login handle (e.g. `cashier01` or `john.c`).
  *(Note: Username is immutable once saved; choose a standardized naming convention).*
- **Display Name:** Enter the cashier's full name (e.g. `John Cashier`). This name prints on customer receipts and appears in shift reconciliation audit logs.

### Step 3: Assign Company & Branch
- **Company Dropdown:** Select the registered legal company name.
- **Branch Dropdown:** Select the specific physical store branch (e.g. `Downtown Branch`) where the cashier is authorized to open shifts and ring sales.

### Step 4: Assign the Cashier Role
In the **Roles** checked list box:
- Check the box next to **Cashier**.
- Ensure administrative roles (**Administrator**, **SystemManager**) remain **unchecked**.

### Step 5: Save the Account Record
Click **Save** (or **OK**). The dialog dispatches:
- `CreateUserCommand` (creates base identity record).
- `AssignUserCompanyCommand` & `AssignUserBranchCommand` (binds enterprise hierarchy).
- `AssignUserToRoleCommand` (grants cashier authorization context).

The new user appears in the user grid in an **Active** state.

---

## 4. Configuring Cashier Authentication (Password & POS PIN)

Cashiers can authenticate using either standard username/password or a fast 4-digit numeric PIN on the POS login screen (`LoginForm`).

### 4.1 Setting the Account Password
1. In the `UsersForm` grid, click to highlight the cashier user row.
2. In the action toolbar, click **Reset Password** (`BtnResetPassword`).
3. The `PasswordPromptForm` dialog appears.
4. Enter and confirm a secure temporary password satisfying password policy:
   - Minimum 8 characters.
   - At least one uppercase letter, one lowercase letter, one number, and one symbol.
5. Click **OK** to save. Inform the cashier to update their password upon first login.

### 4.2 Setting the POS Quick Sign-In PIN
1. In the `UsersForm` grid, click to highlight the cashier user row.
2. In the action toolbar, click **Set PIN** (`BtnSetPin`).
3. The `PinPromptForm` dialog appears.
4. Enter a **4-digit numeric PIN** (e.g. `1234`) and confirm the PIN.
5. Click **OK**.
6. The PIN is securely hashed and stored. The cashier can now sign in at the register by selecting the **PIN** tab on the login screen, entering their username/handle, and tapping their PIN on the on-screen numeric keypad.

### 4.3 Revoking a PIN or Unlocking a Locked Cashier
- **Revoke PIN:** Highlight the cashier row and click **Clear PIN** (`BtnClearPin`). The PIN is erased, and the user must sign in using their password.
- **Unlock Account:** If a cashier exceeds maximum failed authentication attempts and triggers account lockout, highlight their row and click **Unlock User** (`BtnUnlock`) to restore active access.
- **Deactivate Cashier:** If frontline staff leaves employment, highlight their row and click **Deactivate** (`BtnDeactivate`). Deactivated accounts are blocked from logging in or opening shifts, but historical transactional records linked to their user ID remain completely intact.

---

## 5. Permission & Access Matrix

CBOS enforces strict Role-Based Access Control (RBAC). The table below details operational rights confirmed in source code:

| Capability / Workflow | Administrator | Store Manager | Cashier | Notes |
| :--- | :---: | :---: | :---: | :--- |
| **Open Cashier Shift & Starting Float** | Allowed | Allowed | Allowed | Scoped to assigned branch. |
| **Ring Sales & Add Order Items** | Allowed | Allowed | Allowed | Standard POS functionality. |
| **Accept Cash & External Card Tenders** | Allowed | Allowed | Allowed | Standard POS functionality. |
| **Split Bill Across Tenders** | Allowed | Allowed | Allowed | Opens `BillSplitDialog`. |
| **Settle "On Account" (Customer Credit)** | Allowed | Allowed | Allowed | If within customer credit limit. |
| **Override Customer Credit Limit** | Allowed | Allowed | **Blocked** | Requires Manager PIN elevation. |
| **Apply Percentage / Fixed Discount** | Allowed | Allowed | **Restricted** | Manager elevation required for unapproved discounts. |
| **Reprint Last Completed Receipt** | Allowed | Allowed | Allowed | Scoped to active shift's last check. |
| **Process Line-Item Refund / Return** | Allowed | Allowed | **Restricted** | Candidate feature; requires Manager approval dialog. |
| **Mid-Shift Cash In / Cash Out** | Allowed | Allowed | Allowed | Audit reason recorded. |
| **Close Shift & Enter Blind Cash Count** | Allowed | Allowed | Allowed | Required at end of cashier duty. |
| **Execute Business Day Close (End of Day)**| Allowed | Allowed | **Blocked** | Manager / Admin privilege only. |
| **User Administration & Password Reset** | Allowed | **Blocked** | **Blocked** | Reserved for `Administrator`. |
| **Hardware & Printer Settings** | Allowed | **Blocked** | **Blocked** | Reserved for `Administrator`. |

---

## 6. Workstation & Terminal Binding

Cashiers operate registers within physical stores. Workstation identity is bound to terminals as follows:

1. **Terminal Identity Storage:**
   The active workstation terminal code is persisted locally in:
   `%LOCALAPPDATA%\Clovent\pos_settings.json`
2. **Environment Variable Override:**
   In managed deployments, workstations can be assigned an immutable terminal code via the system environment variable:
   `CBOS_TERMINAL_ID=REG-01`
3. **Shift Binding:**
   When a cashier opens a shift, the shift record binds the `UserId`, `BranchId`, and `TerminalId`. A cashier can have only one active open shift at a time.

---

## 7. Operational Status & Acceptance Boundaries

- **Source Code Verification:** All UI forms (`UsersForm`, `UserEditForm`, `PasswordPromptForm`, `PinPromptForm`) and MediatR command handlers are verified in committed source code.
- **Pending Acceptance Verification:** Execution of CBOS Desktop under non-elevated standard Windows user accounts (`Users` group) without triggering UAC elevation prompts remains subject to final qualification sign-off by the active acceptance stream.
