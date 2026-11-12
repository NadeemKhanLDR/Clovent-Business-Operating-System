-- Prepared deployment step; never executed against an operational database by this audit.
-- Caller must set CBOS.Ownership.ExpectedDatabase on THIS connection after target review.
-- Populate Restaurant.CustomerCompanyOwnershipMap only from the reviewed explicit mapping.
SET NOCOUNT ON;
SET XACT_ABORT ON;
DECLARE @expected sysname = CONVERT(sysname, SESSION_CONTEXT(N'CBOS.Ownership.ExpectedDatabase'));
IF @expected IS NULL OR @expected <> DB_NAME()
    THROW 51000, 'Ownership mapping target database has not been explicitly verified.', 1;
IF @@TRANCOUNT <> 0
    THROW 51007, 'Ownership mapping requires a dedicated connection without an active transaction.', 1;
IF OBJECT_ID(N'Identity.Companies', N'U') IS NULL
    THROW 51001, 'Company registry is required for explicit ownership mapping.', 1;
BEGIN TRY
    SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;
    BEGIN TRANSACTION;
    -- Hold account/mapping locks through validation and assignment.
    DECLARE @count bigint;
    SELECT @count = COUNT_BIG(*) FROM Restaurant.Customers WITH (UPDLOCK, HOLDLOCK);
    SELECT @count = COUNT_BIG(*) FROM Restaurant.CustomerCompanyOwnershipMap WITH (UPDLOCK, HOLDLOCK);
    IF EXISTS (
        SELECT 1 FROM Restaurant.CustomerCompanyOwnershipMap m
        LEFT JOIN Restaurant.Customers c ON c.Id = m.CustomerId
        LEFT JOIN [Identity].Companies co ON co.Id = m.CompanyId
        WHERE c.Id IS NULL OR co.Id IS NULL OR m.CompanyId = '00000000-0000-0000-0000-000000000000'
           OR LEN(LTRIM(RTRIM(m.ApprovedBy))) = 0 OR LEN(LTRIM(RTRIM(m.EvidenceReference))) = 0
           OR (c.CompanyId IS NOT NULL AND c.CompanyId <> m.CompanyId))
        THROW 51002, 'Explicit mapping is invalid, unapproved, or attempts to reassign an owner.', 1;
    IF EXISTS (
        SELECT 1 FROM Restaurant.Customers c
        LEFT JOIN Restaurant.CustomerCompanyOwnershipMap m ON m.CustomerId = c.Id
        WHERE c.CompanyId IS NULL AND m.CustomerId IS NULL)
        THROW 51003, 'Explicit mapping is incomplete; no ownership was inferred.', 1;
    IF EXISTS (
        SELECT 1 FROM Restaurant.CustomerLedgerEntries e
        LEFT JOIN Restaurant.Customers c ON c.Id = e.CustomerId
        LEFT JOIN Restaurant.CustomerCompanyOwnershipMap m ON m.CustomerId = c.Id
        WHERE c.Id IS NULL OR (e.CompanyId IS NOT NULL AND e.CompanyId <> COALESCE(c.CompanyId, m.CompanyId)))
        THROW 51004, 'Ledger ownership is orphaned or conflicts with the mapped customer.', 1;

    -- Ownership is explicit, but it may not contradict persisted transaction ownership.
    SELECT c.Id, COALESCE(c.CompanyId, m.CompanyId) CompanyId INTO #Owners
      FROM Restaurant.Customers c LEFT JOIN Restaurant.CustomerCompanyOwnershipMap m ON m.CustomerId=c.Id;
    IF EXISTS (SELECT 1 FROM Restaurant.Orders WHERE CustomerId IS NOT NULL)
       OR EXISTS (SELECT 1 FROM Restaurant.Refunds WHERE CustomerId IS NOT NULL)
    BEGIN
        IF OBJECT_ID(N'MasterData.Warehouses',N'U') IS NULL OR OBJECT_ID(N'Identity.Branches',N'U') IS NULL
            THROW 51009, 'Persisted warehouse/branch hierarchy is required to validate customer transaction references.', 1;
        EXEC sys.sp_executesql N'
          IF EXISTS (
            SELECT 1 FROM Restaurant.Orders o
            LEFT JOIN #Owners c ON c.Id=o.CustomerId
            LEFT JOIN MasterData.Warehouses w ON w.Id=o.WarehouseId
            LEFT JOIN [Identity].Branches b ON b.Id=w.BranchId
            WHERE o.CustomerId IS NOT NULL AND (c.Id IS NULL OR b.Id IS NULL OR b.CompanyId<>c.CompanyId))
            THROW 51009, ''Order references are missing, ambiguous across companies, or contradict explicit ownership.'', 1;
          IF EXISTS (
            SELECT 1 FROM Restaurant.Refunds r
            LEFT JOIN #Owners c ON c.Id=r.CustomerId
            LEFT JOIN MasterData.Warehouses w ON w.Id=r.WarehouseId
            LEFT JOIN [Identity].Branches b ON b.Id=w.BranchId
            LEFT JOIN Restaurant.Orders o ON o.Id=r.OrderId
            WHERE r.CustomerId IS NOT NULL AND (c.Id IS NULL OR b.Id IS NULL OR b.CompanyId<>c.CompanyId
                OR o.Id IS NULL OR o.CustomerId IS NULL OR o.CustomerId<>r.CustomerId))
            THROW 51009, ''Refund references contradict customer or company ownership.'', 1;';
    END;
    IF EXISTS (SELECT 1 FROM Restaurant.CustomerLedgerEntries WHERE ShiftId IS NOT NULL)
    BEGIN
        IF OBJECT_ID(N'Identity.Branches',N'U') IS NULL
            THROW 51010, 'Persisted branch hierarchy is required for ledger shifts.', 1;
        EXEC sys.sp_executesql N'
          IF EXISTS (
            SELECT 1 FROM Restaurant.CustomerLedgerEntries e
            JOIN #Owners c ON c.Id=e.CustomerId
            LEFT JOIN Restaurant.Shifts s ON s.Id=e.ShiftId
            LEFT JOIN [Identity].Branches b ON b.Id=s.BranchId
            WHERE e.ShiftId IS NOT NULL AND (s.Id IS NULL OR b.Id IS NULL OR b.CompanyId<>c.CompanyId))
            THROW 51010, ''Ledger shift belongs to a different or unknown company.'', 1;';
    END;
    IF EXISTS (
        SELECT 1 FROM Restaurant.CustomerPaymentAllocations a
        LEFT JOIN #Owners c ON c.Id=a.CustomerId
        LEFT JOIN Restaurant.Orders o ON o.Id=a.OrderId
        LEFT JOIN Restaurant.CustomerLedgerEntries e ON e.Id=a.CustomerLedgerEntryId
        WHERE c.Id IS NULL OR o.Id IS NULL OR o.CustomerId IS NULL OR o.CustomerId<>a.CustomerId
           OR (a.CustomerLedgerEntryId IS NOT NULL AND (e.Id IS NULL OR e.CustomerId<>a.CustomerId)))
        THROW 51011, 'Allocation has missing or cross-customer order/ledger references.', 1;

    SELECT * INTO #AllocationFacts FROM Restaurant.CustomerPaymentAllocations;
    SELECT * INTO #AuditFacts FROM Restaurant.ActivityLogEntries;
    SELECT * INTO #OrderFacts FROM Restaurant.Orders;
    SELECT * INTO #PaymentFacts FROM Restaurant.Payments;
    SELECT * INTO #RefundFacts FROM Restaurant.Refunds;
    SELECT Id, OpeningBalance, OutstandingBalance, CreatedAtUtc, UpdatedAtUtc
      INTO #CustomerFacts FROM Restaurant.Customers;
    SELECT Id, CustomerId, [Date], Reference, Description, Debit, Credit, RunningBalance, ShiftId, PaymentMethod
      INTO #LedgerFacts FROM Restaurant.CustomerLedgerEntries;

    UPDATE c SET CompanyId = m.CompanyId
      FROM Restaurant.Customers c JOIN Restaurant.CustomerCompanyOwnershipMap m ON m.CustomerId = c.Id
      WHERE c.CompanyId IS NULL;
    UPDATE e SET CompanyId = c.CompanyId
      FROM Restaurant.CustomerLedgerEntries e JOIN Restaurant.Customers c ON c.Id = e.CustomerId
      WHERE e.CompanyId IS NULL;

    IF EXISTS (SELECT * FROM #CustomerFacts EXCEPT
        SELECT Id, OpeningBalance, OutstandingBalance, CreatedAtUtc, UpdatedAtUtc FROM Restaurant.Customers)
       OR EXISTS (SELECT Id, OpeningBalance, OutstandingBalance, CreatedAtUtc, UpdatedAtUtc FROM Restaurant.Customers
                  EXCEPT SELECT * FROM #CustomerFacts)
       OR EXISTS (SELECT * FROM #LedgerFacts EXCEPT
        SELECT Id, CustomerId, [Date], Reference, Description, Debit, Credit, RunningBalance, ShiftId, PaymentMethod FROM Restaurant.CustomerLedgerEntries)
       OR EXISTS (SELECT Id, CustomerId, [Date], Reference, Description, Debit, Credit, RunningBalance, ShiftId, PaymentMethod FROM Restaurant.CustomerLedgerEntries
                  EXCEPT SELECT * FROM #LedgerFacts)
       OR EXISTS (SELECT * FROM #AllocationFacts EXCEPT SELECT * FROM Restaurant.CustomerPaymentAllocations)
       OR EXISTS (SELECT * FROM Restaurant.CustomerPaymentAllocations EXCEPT SELECT * FROM #AllocationFacts)
       OR EXISTS (SELECT * FROM #AuditFacts EXCEPT SELECT * FROM Restaurant.ActivityLogEntries)
       OR EXISTS (SELECT * FROM Restaurant.ActivityLogEntries EXCEPT SELECT * FROM #AuditFacts)
       OR EXISTS (SELECT * FROM #OrderFacts EXCEPT SELECT * FROM Restaurant.Orders)
       OR EXISTS (SELECT * FROM Restaurant.Orders EXCEPT SELECT * FROM #OrderFacts)
       OR EXISTS (SELECT * FROM #PaymentFacts EXCEPT SELECT * FROM Restaurant.Payments)
       OR EXISTS (SELECT * FROM Restaurant.Payments EXCEPT SELECT * FROM #PaymentFacts)
       OR EXISTS (SELECT * FROM #RefundFacts EXCEPT SELECT * FROM Restaurant.Refunds)
       OR EXISTS (SELECT * FROM Restaurant.Refunds EXCEPT SELECT * FROM #RefundFacts)
        THROW 51005, 'Financial facts changed during ownership mapping; all assignments rolled back.', 1;
    IF EXISTS (SELECT 1 FROM Restaurant.Customers WHERE CompanyId IS NULL)
       OR EXISTS (SELECT 1 FROM Restaurant.CustomerLedgerEntries WHERE CompanyId IS NULL)
        THROW 51006, 'Ownership mapping did not cover every account and ledger row.', 1;
    DROP TABLE #CustomerFacts;
    DROP TABLE #LedgerFacts;
    DROP TABLE #AllocationFacts;
    DROP TABLE #Owners;
    DROP TABLE #AuditFacts;
    DROP TABLE #OrderFacts;
    DROP TABLE #PaymentFacts;
    DROP TABLE #RefundFacts;
    COMMIT TRANSACTION;
    SET TRANSACTION ISOLATION LEVEL READ COMMITTED;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    SET TRANSACTION ISOLATION LEVEL READ COMMITTED;
    THROW;
END CATCH;
