SET NOCOUNT ON;
-- 1) naive duplicate check on the number alone (across all days)
SELECT DailySalesNumber, COUNT(*) AS Cnt
FROM [Restaurant].Orders
WHERE DailySalesNumber IS NOT NULL
GROUP BY DailySalesNumber
HAVING COUNT(*) > 1
ORDER BY DailySalesNumber;

-- 2) the meaningful check: duplicates within same warehouse + same completion day
SELECT WarehouseId, CONVERT(date, UpdatedAtUtc) AS Day, DailySalesNumber, COUNT(*) AS Cnt
FROM [Restaurant].Orders
WHERE DailySalesNumber IS NOT NULL
GROUP BY WarehouseId, CONVERT(date, UpdatedAtUtc), DailySalesNumber
HAVING COUNT(*) > 1;

-- 3) the specific orders from the observation
SELECT OrderNumber, Status, DailySalesNumber, WarehouseId, CreatedAtUtc, UpdatedAtUtc
FROM [Restaurant].Orders
WHERE OrderNumber IN ('ORD-93','ORD-85','ORD-48','ORD-52','ORD-82')
ORDER BY UpdatedAtUtc;

-- 4) all completed orders with numbers, by day
SELECT CONVERT(date, UpdatedAtUtc) AS Day, OrderNumber, DailySalesNumber, Status
FROM [Restaurant].Orders
WHERE DailySalesNumber IS NOT NULL
ORDER BY UpdatedAtUtc, DailySalesNumber;
