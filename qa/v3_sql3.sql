SET NOCOUNT ON;
SELECT u.UserName, u.RoleIds FROM [Identity].Users u WHERE u.UserName = 'admin';
-- permission ids for pos.create / pos.hold / pos.resume
SELECT p.Code, p.Id FROM [Identity].Permissions p WHERE p.Code IN ('feature.pos.create','feature.pos.hold','feature.pos.resume','feature.pos.cancel','feature.pos.pay','feature.pos.additem');
-- does Administrator role contain those ids?
SELECT p.Code,
       CASE WHEN CHARINDEX('"'+CAST(p.Id AS NVARCHAR(40))+'"', r.PermissionIds) > 0 THEN 'YES' ELSE 'NO' END AS InAdminRole
FROM [Identity].Permissions p
CROSS JOIN [Identity].Roles r
WHERE r.Name = 'Administrator' AND p.Code IN ('feature.pos.create','feature.pos.hold','feature.pos.resume','feature.pos.cancel','feature.pos.pay','feature.pos.additem')
ORDER BY p.Code;
