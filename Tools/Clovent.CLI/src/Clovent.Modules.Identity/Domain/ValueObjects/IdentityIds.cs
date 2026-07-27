namespace Clovent.Modules.Identity.Domain.ValueObjects;

public readonly record struct UserId(Guid Value)
{
    public static UserId New() => new(Guid.NewGuid());
    public static UserId Empty => new(Guid.Empty);
}

public readonly record struct RoleId(Guid Value)
{
    public static RoleId New() => new(Guid.NewGuid());
    public static RoleId Empty => new(Guid.Empty);
}

public readonly record struct PermissionId(Guid Value)
{
    public static PermissionId New() => new(Guid.NewGuid());
    public static PermissionId Empty => new(Guid.Empty);
}

public readonly record struct CompanyId(Guid Value)
{
    public static CompanyId New() => new(Guid.NewGuid());
    public static CompanyId Empty => new(Guid.Empty);
}

public readonly record struct BranchId(Guid Value)
{
    public static BranchId New() => new(Guid.NewGuid());
    public static BranchId Empty => new(Guid.Empty);
}
