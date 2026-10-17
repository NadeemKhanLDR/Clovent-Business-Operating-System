-- ==============================================================================
-- SCRIPT_ID: FIX_20261017_02_CompensatingLedgerAdjustment
-- DESCRIPTION: Post approved compensating credit entry for billing dispute resolution under PDR-0003
-- TARGET_SCHEMA: Restaurant
-- TARGET_DATABASE_PATTERN: (?i)^CBOS_Support_Diag_.*_Disposable$|(?i)^CBOS_Client_.*_Disposable$|(?i)^Clovent_BusinessOperatingSystem$
-- AUTHOR: CBOS Customer Support Engineering
-- REVIEW_STATUS: Approved for isolated & live execution
-- POLICY_COMPLIANCE: Enforces PDR-0003 by posting an additive compensating ledger entry without rewriting historical transactions.
-- TENANCY_GOVERNANCE: Requires verified target database pattern AND explicit pinned customer ID $(TARGET_CUSTOMER_ID) to mitigate un-scoped Customer schema.
-- ==============================================================================

-- PRECHECK:
DECLARE @TargetCustomerId UNIQUEIDENTIFIER = '$(TARGET_CUSTOMER_ID)';
SELECT CASE 
    WHEN c.[Id] = @TargetCustomerId
     AND c.[Code] = 'CREDIT-DEMO'
     AND c.[OutstandingBalance] = 1044.00 
     AND (ISNULL(SUM(l.[Debit]), 0) - ISNULL(SUM(l.[Credit]), 0)) = 1044.00 
    THEN 1 
    ELSE 0 
END AS PrecheckResult
FROM [Restaurant].[Customers] c
LEFT JOIN [Restaurant].[CustomerLedgerEntries] l ON c.[Id] = l.[CustomerId]
WHERE c.[Id] = @TargetCustomerId AND c.[Code] = 'CREDIT-DEMO'
GROUP BY c.[Id], c.[Code], c.[OutstandingBalance];

-- PREVIEW_DIAGNOSIS:
DECLARE @TargetCustomerId UNIQUEIDENTIFIER = '$(TARGET_CUSTOMER_ID)';
SELECT [Id], [Code], [Name], [OutstandingBalance], [OpeningBalance], [CreditLimit]
FROM [Restaurant].[Customers]
WHERE [Id] = @TargetCustomerId AND [Code] = 'CREDIT-DEMO';

-- MUTATION:
DECLARE @TargetCustomerId UNIQUEIDENTIFIER = '$(TARGET_CUSTOMER_ID)';
DECLARE @CurrentBalance DECIMAL(18,2);
DECLARE @AdjustmentAmount DECIMAL(18,2) = 100.00; -- Approved concession/credit compensation
DECLARE @NewBalance DECIMAL(18,2);
DECLARE @NewEntryId UNIQUEIDENTIFIER = NEWID();

SELECT @CurrentBalance = [OutstandingBalance]
FROM [Restaurant].[Customers]
WHERE [Id] = @TargetCustomerId AND [Code] = 'CREDIT-DEMO';

IF @CurrentBalance IS NOT NULL
BEGIN
    SET @NewBalance = @CurrentBalance - @AdjustmentAmount;

    -- 1. Insert additive compensating transaction pinned strictly to explicit CustomerId
    INSERT INTO [Restaurant].[CustomerLedgerEntries]
        ([Id], [CustomerId], [Date], [Reference], [Description], [Debit], [Credit], [RunningBalance], [ShiftId], [PaymentMethod])
    VALUES
        (@NewEntryId, @TargetCustomerId, SYSUTCDATETIME(), 'COMP-DISPUTE-20261017', 'Approved support compensation credit note for dispute resolution (PDR-0003 compliant)', 0.00, @AdjustmentAmount, @NewBalance, NULL, 'Support Credit Memo');

    -- 2. Update customer outstanding balance strictly by explicit CustomerId
    UPDATE [Restaurant].[Customers]
    SET [OutstandingBalance] = @NewBalance,
        [UpdatedAtUtc] = SYSUTCDATETIME()
    WHERE [Id] = @TargetCustomerId AND [Code] = 'CREDIT-DEMO';
END;

-- POSTCHECK_RECONCILIATION:
DECLARE @TargetCustomerId UNIQUEIDENTIFIER = '$(TARGET_CUSTOMER_ID)';
SELECT CASE
    WHEN c.[Id] = @TargetCustomerId
     AND c.[OutstandingBalance] = 944.00
     AND (ISNULL(SUM(l.[Debit]), 0) - ISNULL(SUM(l.[Credit]), 0)) = 944.00
     AND EXISTS (
         SELECT 1 
         FROM [Restaurant].[CustomerLedgerEntries] e 
         WHERE e.[CustomerId] = @TargetCustomerId 
           AND e.[Credit] = 100.00 
           AND e.[RunningBalance] = 944.00
     )
    THEN 'PASS: Customer balance adjusted from 1,044.00 to 944.00 PKR and fully reconciled with ledger'
    ELSE 'FAIL: Ledger reconciliation mismatch detected'
END AS ReconciliationResult
FROM [Restaurant].[Customers] c
LEFT JOIN [Restaurant].[CustomerLedgerEntries] l ON c.[Id] = l.[CustomerId]
WHERE c.[Id] = @TargetCustomerId AND c.[Code] = 'CREDIT-DEMO'
GROUP BY c.[Id], c.[OutstandingBalance];
