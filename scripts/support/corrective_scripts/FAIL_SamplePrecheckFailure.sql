-- ==============================================================================
-- SCRIPT_ID: FAIL_SamplePrecheckFailure
-- DESCRIPTION: Negative test script that fails during precheck to verify zero mutation occurs
-- TARGET_SCHEMA: Restaurant
-- TARGET_DATABASE_PATTERN: (?i)^CBOS_Support_Diag_.*_Disposable$
-- AUTHOR: CBOS Customer Support Engineering
-- REVIEW_STATUS: Negative test control (Unapproved)
-- ==============================================================================

-- PRECHECK:
SELECT 0 AS PrecheckResult;

-- PREVIEW_DIAGNOSIS:
SELECT [Id], [Code], [Notes] FROM [Restaurant].[Customers] WHERE [Code] = 'CREDIT-DEMO';

-- MUTATION:
UPDATE [Restaurant].[Customers]
SET [Notes] = 'MUTATED_BY_PRECHECK_FAILURE'
WHERE [Code] = 'CREDIT-DEMO';

-- POSTCHECK_RECONCILIATION:
SELECT 1 AS PostcheckResult;
