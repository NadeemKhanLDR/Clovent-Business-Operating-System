-- ==============================================================================
-- SCRIPT_ID: FIX_20261017_02_CompensatingLedgerAdjustment
-- DESCRIPTION: Post approved compensating credit entry for billing dispute resolution under PDR-0003
-- TARGET_SCHEMA: Restaurant
-- AUTHOR: CBOS Customer Support Engineering
-- REVIEW_STATUS: Approved for isolated & live execution
-- POLICY_COMPLIANCE: Enforces PDR-0003 by posting an additive compensating ledger entry without rewriting historical transactions.
-- ==============================================================================

-- PRECHECK:
SELECT COUNT(1) AS EligibleCustomerCount
FROM [Restaurant].[Customers]
WHERE [Code] = 'CREDIT-DEMO'
  AND [OutstandingBalance] > 0;

-- PREVIEW_DIAGNOSIS:
SELECT [Id], [Code], [Name], [OutstandingBalance], [OpeningBalance], [CreditLimit]
FROM [Restaurant].[Customers]
WHERE [Code] = 'CREDIT-DEMO';

-- MUTATION:
DECLARE @CustomerId UNIQUEIDENTIFIER;
DECLARE @CurrentBalance DECIMAL(18,2);
DECLARE @AdjustmentAmount DECIMAL(18,2) = 100.00; -- Approved concession/credit compensation
DECLARE @NewBalance DECIMAL(18,2);
DECLARE @NewEntryId UNIQUEIDENTIFIER = NEWID();

SELECT @CustomerId = [Id], @CurrentBalance = [OutstandingBalance]
FROM [Restaurant].[Customers]
WHERE [Code] = 'CREDIT-DEMO';

IF @CustomerId IS NOT NULL
BEGIN
    SET @NewBalance = @CurrentBalance - @AdjustmentAmount;

    -- 1. Insert additive compensating transaction into CustomerLedgerEntries
    INSERT INTO [Restaurant].[CustomerLedgerEntries]
        ([Id], [CustomerId], [Date], [Reference], [Description], [Debit], [Credit], [RunningBalance], [ShiftId], [PaymentMethod])
    VALUES
        (@NewEntryId, @CustomerId, SYSUTCDATETIME(), 'COMP-DISPUTE-20261017', 'Approved support compensation credit note for dispute resolution (PDR-0003 compliant)', 0.00, @AdjustmentAmount, @NewBalance, NULL, 'Support Credit Memo');

    -- 2. Update customer outstanding balance to match reconciled ledger
    UPDATE [Restaurant].[Customers]
    SET [OutstandingBalance] = @NewBalance,
        [UpdatedAtUtc] = SYSUTCDATETIME()
    WHERE [Id] = @CustomerId;
END;

-- POSTCHECK_RECONCILIATION:
SELECT CASE
    WHEN ABS(c.[OutstandingBalance] - (ISNULL(SUM(l.[Debit]), 0) - ISNULL(SUM(l.[Credit]), 0))) < 0.001
    THEN 'PASS: Customer outstanding balance perfectly reconciles with sum of all ledger debits and credits'
    ELSE 'FAIL: Ledger mismatch detected'
END AS ReconciliationResult
FROM [Restaurant].[Customers] c
LEFT JOIN [Restaurant].[CustomerLedgerEntries] l ON c.[Id] = l.[CustomerId]
WHERE c.[Code] = 'CREDIT-DEMO'
GROUP BY c.[OutstandingBalance];
