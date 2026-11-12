-- READ ONLY. Run after schema expansion, on an explicitly approved target/connection.
SET NOCOUNT ON;
DECLARE @expected sysname = CONVERT(sysname, SESSION_CONTEXT(N'CBOS.Ownership.ExpectedDatabase'));
IF @expected IS NULL OR DB_NAME()<>@expected
    THROW 51000, 'Preflight target has not been explicitly verified.', 1;
IF COL_LENGTH(N'Restaurant.Customers',N'CompanyId') IS NULL
   OR COL_LENGTH(N'Restaurant.CustomerLedgerEntries',N'CompanyId') IS NULL
    THROW 51014, 'Ownership expansion is not installed.', 1;
SELECT DB_NAME() AS DatabaseName, CONVERT(nvarchar(128),SERVERPROPERTY('ProductVersion')) AS EngineVersion;
SELECT COUNT_BIG(*) AccountCount, SUM(CASE WHEN CompanyId IS NULL THEN CONVERT(bigint,1) ELSE 0 END) UnmappedAccounts,
       SUM(OpeningBalance) OpeningBalanceControl, SUM(OutstandingBalance) OutstandingBalanceControl
  FROM Restaurant.Customers;
SELECT COUNT_BIG(*) LedgerCount, SUM(Debit) DebitControl, SUM(Credit) CreditControl,
       SUM(CASE WHEN CompanyId IS NULL THEN CONVERT(bigint,1) ELSE 0 END) UnmappedLedger
  FROM Restaurant.CustomerLedgerEntries;
SELECT COUNT_BIG(*) AllocationCount, SUM(Amount) AllocationControl FROM Restaurant.CustomerPaymentAllocations;
SELECT COUNT_BIG(*) AuditCount FROM Restaurant.ActivityLogEntries;
SELECT c.Id CustomerId, c.Code, c.CompanyId ExistingOwner, m.CompanyId ExplicitMappedOwner,
       c.OpeningBalance, c.OutstandingBalance
  FROM Restaurant.Customers c LEFT JOIN Restaurant.CustomerCompanyOwnershipMap m ON m.CustomerId=c.Id
  ORDER BY c.Id;
SELECT name TriggerName, is_disabled IsDisabled FROM sys.triggers
  WHERE parent_id IN (OBJECT_ID(N'Restaurant.Customers'),OBJECT_ID(N'Restaurant.CustomerLedgerEntries'));
IF OBJECT_ID(N'Restaurant.__EFMigrationsHistory',N'U') IS NOT NULL
    EXEC(N'SELECT MigrationId FROM Restaurant.__EFMigrationsHistory WHERE MigrationId IN
      (''20261111220000_AddCustomerCompanyOwnership'',''20261112010000_PreventCompanyOwnershipReassignment'')');
ELSE
    SELECT N'Migration history absent: schema operations alone do not qualify deployment.' AS MigrationHistoryWarning;
