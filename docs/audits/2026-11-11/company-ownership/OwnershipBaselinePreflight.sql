-- READ ONLY, before expansion. Repeat controls with writers stopped in the approved window.
SET NOCOUNT ON;
DECLARE @expected sysname = CONVERT(sysname,SESSION_CONTEXT(N'CBOS.Ownership.ExpectedDatabase'));
IF @expected IS NULL OR @expected<>DB_NAME()
    THROW 51000, 'Baseline preflight target has not been explicitly verified.', 1;
SELECT DB_NAME() DatabaseName, CONVERT(nvarchar(128),SERVERPROPERTY('ProductVersion')) EngineVersion;
SELECT COL_LENGTH(N'Restaurant.Customers',N'CompanyId') CustomerOwnershipColumn,
       COL_LENGTH(N'Restaurant.CustomerLedgerEntries',N'CompanyId') LedgerOwnershipColumn;
SELECT Id CustomerId, Code, OpeningBalance, OutstandingBalance, CreatedAtUtc, UpdatedAtUtc
  FROM Restaurant.Customers ORDER BY Id;
SELECT COUNT_BIG(*) LedgerRows, SUM(Debit) DebitControl, SUM(Credit) CreditControl FROM Restaurant.CustomerLedgerEntries;
SELECT COUNT_BIG(*) AllocationRows, SUM(Amount) AllocationControl FROM Restaurant.CustomerPaymentAllocations;
SELECT COUNT_BIG(*) OrderRows FROM Restaurant.Orders;
SELECT COUNT_BIG(*) PaymentRows, SUM(Amount) PaymentControl FROM Restaurant.Payments;
SELECT COUNT_BIG(*) RefundRows, SUM(GrandTotalRefunded) RefundControl FROM Restaurant.Refunds;
SELECT COUNT_BIG(*) AuditRows FROM Restaurant.ActivityLogEntries;
IF OBJECT_ID(N'Restaurant.__EFMigrationsHistory',N'U') IS NOT NULL
    EXEC(N'SELECT MigrationId,ProductVersion FROM Restaurant.__EFMigrationsHistory ORDER BY MigrationId');
ELSE
    SELECT N'No Restaurant migration history: baseline must be reviewed before deployment.' MigrationHistoryWarning;
