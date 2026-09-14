# Restaurant POS Bug Report: Record Payment Failure

**Status:** Resolved & Verified  
**Date:** 2026-09-14  
**Impact:** Critical (Blocked payment processing in Restaurant POS)  
**Affects:** Restaurant POS Payment Recording, Shift Management, Dine-In & Take-Away Workflows  

---

## 1. Problem Description
When attempting to record a payment in the Restaurant POS (e.g., creating a Dine-In order for Table T-01, adding items totaling 630.00, tendering 630.00 cash, and clicking "Record Payment"), the operation failed with the following dialog:

```
Action Failed
Unable to record this payment.

Reason:
Something went wrong and this action could not be completed.
Try again. If this keeps happening, click "Show Details" below and share that information with support.

Please try again.
```

Furthermore, the dialog was presented using standard `XtraMessageBox.Show(...)`, meaning the "Show Details" button mentioned in the message was absent.

---

## 2. Root Cause Analysis

### A. Primary Failure: Database Schema Desynchronization (`Invalid object name 'Restaurant.Shifts'`)
- Inspection of the application diagnostic log (`%LOCALAPPDATA%\Clovent\Logs\clovent-2026-09-14.log`) revealed:
  ```
  Microsoft.EntityFrameworkCore.Query: An exception occurred while iterating over the results of a query for context type 'Clovent.Restaurant.Infrastructure.Persistence.RestaurantDbContext'.
  Microsoft.Data.SqlClient.SqlException (0x80131904): Invalid object name 'Restaurant.Shifts'.
     at Microsoft.EntityFrameworkCore.Storage.RelationalCommand.ExecuteReaderAsync(...)
  Action failed: record this payment
  ```
- Before recording a payment, `RestaurantPosForm.RecordPaymentAsync` called `EnsureShiftActiveOrPromptAsync()`, which executes `GetActiveShiftQuery(CashierId: cashierId)` against the database.
- EF Core migration `20260912025646_AddShiftManagement` had previously been marked as applied in `__EFMigrationsHistory` in the local SQL Server database, but the actual tables `[Restaurant].[Shifts]` and `[Restaurant].[CashMovements]` were never created.
- Because the migration was marked in `__EFMigrationsHistory`, `dbContext.Database.MigrateAsync()` skipped it, leaving the database without the required tables.

### B. POS Control Flow Bug: Unchecked Return Value
- `EnsureShiftActiveOrPromptAsync()` returns a `bool` indicating whether an active shift exists or was successfully opened.
- In `RestaurantPosForm.cs`, both `RecordPaymentAsync()` and `SplitPaymentAsync()` previously called `await EnsureShiftActiveOrPromptAsync();` without checking the return value. If the cashier cancelled the open shift prompt, the application still attempted to dispatch `RecordPaymentCommand`.

### C. Error Dialog UX Gap: Missing "Show Details"
- `GuardedAction.RunAsync` previously called `XtraMessageBox.Show()`, which does not provide expandable exception details or clipboard copying, contradicting the prompt advice to *"click 'Show Details' below"*.

---

## 3. Implemented Fixes

1. **Idempotent Persistence Initializer (`RestaurantPersistenceInitializer.cs`)**:
   - Added DDL checks inside `InitializeAsync()` to verify and idempotently create:
     - Table `[Restaurant].[Shifts]` and its indexes (`IX_Shifts_ShiftNumber`, `IX_Shifts_CashierId`, `IX_Shifts_Status`, `IX_Shifts_TerminalId`).
     - Table `[Restaurant].[CashMovements]` and foreign key `[FK_CashMovements_Shifts_ShiftId]`.
     - Column `ShiftId` and index `IX_Payments_ShiftId` on `[Restaurant].[Payments]`.
   - Guaranteed that any environment or database with desynchronized migration history automatically repairs the required schema upon startup.

2. **Shift Check Guards (`RestaurantPosForm.cs`)**:
   - Updated `RecordPaymentAsync()`:
     ```csharp
     if (!await EnsureShiftActiveOrPromptAsync())
     {
         return;
     }
     ```
   - Updated `SplitPaymentAsync()`:
     ```csharp
     if (!await EnsureShiftActiveOrPromptAsync())
     {
         return;
     }
     ```
   - Restored and protected the `RecordButton_Click` event handler with `_isRecordingPayment` re-entry locking to prevent duplicate submission while in-flight.

3. **Enhanced Diagnostics in GuardedAction (`GuardedAction.cs` & `ErrorDialogForm.cs`)**:
   - Updated `ErrorDialogForm` to accept an optional `customMessage` parameter.
   - Updated `GuardedAction.RunAsync` to display `ErrorDialogForm` modally, making the "Show Details" button and stack trace diagnostics available whenever an unhandled exception occurs.

---

## 4. Verification & Validation

### A. Real Database End-to-End Integration Test
- Added test `RestaurantPos_DineIn_RecordPayment_ExactCash_RealDatabase_CompletesSuccessfully` in `Clovent.Desktop.Tests/RuntimeVerificationTests.cs`.
- Executed against the actual local SQL Server database `Clovent_Restaurant`:
  1. Creates Dine-In order for Table T-01.
  2. Adds *Aloo Gobi - Full* and *Aloo Gobi - Half*.
  3. Validates shift resolution via `GetActiveShiftQuery` (or creates active shift).
  4. Dispatches `RecordPaymentCommand` with exact total and `ShiftId`.
  5. Validates balance equals 0.00 and order transitions to `Completed`.
  6. Queries SQL Server directly to verify `Order` status is `Completed` and `Payment` has valid `ShiftId`.
  - **Result:** PASSED in 6 seconds.

### B. Automated Regression Suite (PAYMENT-RECORD-01 to 12)
Added unit test suite in `Clovent.Restaurant.Application.Tests/Payments/PaymentHandlerTests.cs`:
- `PAYMENT_RECORD_01_ExactCashPayment_RecordsPaymentSuccessfully`: PASSED
- `PAYMENT_RECORD_02_PaymentWithShiftId_AssociatesShiftWithPayment`: PASSED
- `PAYMENT_RECORD_03_PartialPayment_ReducesBalanceSuccessfully`: PASSED
- `PAYMENT_RECORD_04_OverPayment_ThrowsInvalidOperationException`: PASSED
- `PAYMENT_RECORD_05_SplitPaymentAcrossTwoMethods_Succeeds`: PASSED
- `PAYMENT_RECORD_06_CreditPaymentWithoutCustomer_ThrowsInvalidOperationException`: PASSED
- `PAYMENT_RECORD_07_CreditPaymentExceedingLimitWithoutApproval_ThrowsInvalidOperationException`: PASSED
- `PAYMENT_RECORD_08_CreditPaymentExceedingLimitWithApproval_Succeeds`: PASSED
- `PAYMENT_RECORD_09_NonExistentOrder_ThrowsNotFoundException`: PASSED
- `PAYMENT_RECORD_10_NonExistentPaymentMethod_ThrowsNotFoundException`: PASSED
- `PAYMENT_RECORD_11_InactiveCustomerOnCreditSale_ThrowsInvalidOperationException`: PASSED
- `PAYMENT_RECORD_12_ZeroBalanceOrderPayment_ThrowsInvalidOperationException`: PASSED

### C. Full Project Builds
- `Clovent.Desktop.csproj` - PASSED (0 Errors)
- `Clovent.Restaurant.Application.Tests.csproj` - PASSED (184 Passed, 0 Failed)
- `Clovent.Restaurant.Infrastructure.Tests.csproj` - PASSED (48 Passed, 0 Failed)
- `Clovent.Desktop.Tests.csproj` (PosPaymentRulesTests) - PASSED (18 Passed, 0 Failed)

---

## 5. Manual QA Verification Procedure

To manually test the fix in the running application:
1. Launch the Clovent application and log in as an administrator / cashier.
2. Open **Restaurant POS**.
3. Select or start a **Dine-In** order.
4. Select Table **T-01**.
5. Add items to cart:
   - *Aloo Gobi - Full* (380.00)
   - *Aloo Gobi - Half* (250.00)
   - Total Payable displays **630.00**, Balance Due displays **630.00**.
6. On the tender strip:
   - Select **Cash** as payment method.
   - Click **Exact** (tender field populates with 630.00).
7. Click **Record Payment**.
8. **Expected Outcome:**
   - If no shift was active, the "Open Shift" dialog appears cleanly; upon entering opening float and confirming, the shift opens and payment proceeds.
   - Payment records without error.
   - Balance reaches 0.00.
   - Order automatically completes and prints/resets cart as configured.
