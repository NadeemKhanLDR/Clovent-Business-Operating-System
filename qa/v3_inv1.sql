SET NOCOUNT ON;
-- Orders table columns
SELECT COLUMN_NAME, DATA_TYPE FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA='Restaurant' AND TABLE_NAME='Orders' ORDER BY ORDINAL_POSITION;
-- Sequence table contents
SELECT WarehouseId, Date, LastNumber FROM [Restaurant].DailySalesSequences ORDER BY Date, WarehouseId;
