-- ==============================================================================
-- SCRIPT_ID: FIX_20261017_01_QuarantineStuckOutbox
-- DESCRIPTION: Quarantine stuck outbox message causing background processor retry loops
-- TARGET_SCHEMA: Restaurant
-- TARGET_DATABASE_PATTERN: (?i)^CBOS_Support_.*_(?:Isolated|Disposable)$|(?i)^Clovent_BusinessOperatingSystem$
-- AUTHOR: CBOS Customer Support Engineering
-- REVIEW_STATUS: Approved for isolated & live execution
-- ==============================================================================

-- PRECHECK:
SELECT COUNT(1) AS StuckCount 
FROM [Restaurant].[OutboxMessages]
WHERE [Status] IN ('RetryScheduled', 'Processing')
  AND [AttemptCount] >= 5;

-- PREVIEW_DIAGNOSIS:
SELECT [Id], [MessageType], [Status], [AttemptCount], [LastError], [CreatedAtUtc], [LastAttemptAtUtc]
FROM [Restaurant].[OutboxMessages]
WHERE [Status] IN ('RetryScheduled', 'Processing')
  AND [AttemptCount] >= 5;

-- MUTATION:
UPDATE [Restaurant].[OutboxMessages]
SET [Status] = 'DeadLetter',
    [LastError] = 'Quarantined by support script FIX_20261017_01: exceeded max attempts and held for engineer inspection'
WHERE [Status] IN ('RetryScheduled', 'Processing')
  AND [AttemptCount] >= 5;

-- POSTCHECK_RECONCILIATION:
SELECT CASE 
    WHEN COUNT(1) = 0 THEN 'PASS: No stuck messages remaining in active processing state'
    ELSE 'FAIL: Stuck messages still exist in active status'
END AS Result
FROM [Restaurant].[OutboxMessages]
WHERE [Status] IN ('RetryScheduled', 'Processing')
  AND [AttemptCount] >= 5;
