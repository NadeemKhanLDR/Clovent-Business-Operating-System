-- ==============================================================================
-- SCRIPT_ID: FAIL_SamplePostcheckFailure
-- DESCRIPTION: Negative test script that fails during postcheck to verify transaction rollback leaves zero mutation
-- TARGET_SCHEMA: Restaurant
-- TARGET_DATABASE_PATTERN: (?i)^CBOS_Support_Diag_.*_Disposable$
-- AUTHOR: CBOS Customer Support Engineering
-- REVIEW_STATUS: Negative test control (Unapproved)
-- ==============================================================================

-- PRECHECK:
SELECT 1 AS PrecheckResult;

-- PREVIEW_DIAGNOSIS:
SELECT [Id], [Code], [Notes] FROM [Restaurant].[Customers] WHERE [Code] = 'CREDIT-DEMO';

-- MUTATION:
UPDATE [Restaurant].[Customers]
SET [Notes] = 'MUTATED_BEFORE_POSTCHECK_FAILURE'
WHERE [Code] = 'CREDIT-DEMO';

-- POSTCHECK_RECONCILIATION:
SELECT 'FAIL: Simulated postcheck reconciliation discrepancy detected' AS PostcheckResult;
