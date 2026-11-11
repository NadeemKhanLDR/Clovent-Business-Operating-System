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
        THROW 51005, 'Financial facts changed during ownership mapping; all assignments rolled back.', 1;
    IF EXISTS (SELECT 1 FROM Restaurant.Customers WHERE CompanyId IS NULL)
       OR EXISTS (SELECT 1 FROM Restaurant.CustomerLedgerEntries WHERE CompanyId IS NULL)
        THROW 51006, 'Ownership mapping did not cover every account and ledger row.', 1;
    DROP TABLE #CustomerFacts;
    DROP TABLE #LedgerFacts;
    COMMIT TRANSACTION;
    SET TRANSACTION ISOLATION LEVEL READ COMMITTED;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    SET TRANSACTION ISOLATION LEVEL READ COMMITTED;
    THROW;
END CATCH;
