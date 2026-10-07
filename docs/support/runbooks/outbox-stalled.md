# Support Runbook: Outbox Stalled or Backlog Growing

| Attribute | Details |
| :--- | :--- |
| **Area** | Support Runbook / Asynchronous Outbox |
| **Audience** | Level 2–3 Support Technicians, Systems Administrators |
| **Last Reviewed Version** | CBOS 1.2.2 |
| **Status** | **PROCEDURAL RUNBOOK** |
| **Classification** | **PUBLIC-SAFE** |

---

## 1. Problem Description
The **Operations Health Center** displays an orange or red alert:
- Outbox queue depth > 100 messages
- Circuit breaker state: `Open`
- `DeadLetter` message count > 0

Cashiers can ring up orders, but receipts are not printing or external integrations (QuickBooks) are delayed.

---

## 2. Step-by-Step Diagnostic & Resolution Procedure

### Step 1: Inspect Outbox Table via Diagnostic Query
Connect to SQL Server via SSMS or `sqlcmd`:

```sql
USE [Clovent_BusinessOperatingSystem];

SELECT 
    Status, 
    MessageType, 
    COUNT(*) AS TotalCount,
    MIN(CreatedAtUtc) AS OldestMessageUtc
FROM [Restaurant].[OutboxMessages]
GROUP BY Status, MessageType;
```

Identify which `MessageType` is stalled:
- `ReceiptPrint` -> Physical printer issue (proceed to Step 2).
- `QuickBooksSync` -> Accounting gateway issue (proceed to Step 3).

### Step 2: Resolving ReceiptPrint Outbox Backlog
1. Check the physical thermal printer connected to the terminal:
   - Is paper loaded?
   - Is the printer cover closed firmly?
   - Is the USB/LAN cable firmly attached?
2. Open Windows **Print Management** -> **Printers**.
3. Right-click the printer and check if there are jammed print jobs. Clear stuck jobs.
4. Once the printer prints a Windows test page, the CBOS `ReceiptPrintOutboxHandler` circuit breaker will transition to `HalfOpen`, probe the printer, and rapidly drain the backlog.

### Step 3: Resolving QuickBooksSync Outbox Backlog
1. Inspect the error message of dead-letter records:
   ```sql
   SELECT TOP 5 Id, Payload, ErrorMessage, RetryCount 
   FROM [Restaurant].[OutboxMessages] 
   WHERE Status = 4 -- DeadLetter
   ORDER BY CreatedAtUtc DESC;
   ```
2. If `DefaultQuickBooksGateway` simulated outage is enabled, disable the test flag in Back Office -> Settings -> QuickBooks.
3. Once the gateway is reachable, reset dead-letter messages to `Pending` so the background worker retries them:
   ```sql
   UPDATE [Restaurant].[OutboxMessages]
   SET Status = 0, RetryCount = 0, NextRetryUtc = NULL
   WHERE Status = 4;
   ```

---

## 3. What Support Must NEVER Do
- **NEVER** run `DELETE FROM [Restaurant].[OutboxMessages]`. This destroys unsynced accounting records and unprinted customer receipts permanently.
