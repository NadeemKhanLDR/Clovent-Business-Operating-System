SET NOCOUNT ON;
SELECT OrderNumber, Status, OrderType, UpdatedAtUtc FROM Restaurant.Orders WHERE OrderNumber IN ('ORD-92');
SELECT Action, COUNT(*) AS Cnt FROM Restaurant.ActivityLogs WHERE Details LIKE '%ORD-92%' GROUP BY Action;
