SET NOCOUNT ON;
SELECT u.UserName, u.Id FROM [Identity].Users u;
SELECT r.Name, r.Id, LEN(r.PermissionIds) AS PermLen, LEFT(r.PermissionIds, 200) AS PermHead FROM [Identity].Roles r;
SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA='Identity' AND TABLE_NAME='Users' AND COLUMN_NAME LIKE '%Role%';
