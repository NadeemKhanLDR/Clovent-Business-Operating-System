-- ==============================================================================
-- SCRIPT_ID: REJECT_SampleIllegalFinancialRewrite
-- DESCRIPTION: Prohibited script attempting direct update of completed orders
-- TARGET_SCHEMA: Restaurant
-- AUTHOR: Testing
-- EXPECTED_OUTCOME: REJECTED by Invoke-CbosCorrectiveScript under PDR-0003
-- ==============================================================================

-- MUTATION:
-- This statement violates PDR-0003 (Completed Financial Transaction Immutability)
UPDATE [Restaurant].[Orders]
SET [GrandTotal] = 0.00
WHERE [Status] = 'Completed';
