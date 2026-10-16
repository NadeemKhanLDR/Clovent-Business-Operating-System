namespace Clovent.Platform.Execution;

/// <summary>
/// Marker interface indicating that a request requires valid Organization-level execution scope.
/// </summary>
public interface IRequireOrganizationScope
{
}

/// <summary>
/// Marker interface indicating that a request requires valid Company-level execution scope
/// (including its parent Organization).
/// </summary>
public interface IRequireCompanyScope : IRequireOrganizationScope
{
}

/// <summary>
/// Marker interface indicating that a request requires valid Branch-level execution scope
/// (including its parent Company and Organization).
/// </summary>
public interface IRequireBranchScope : IRequireCompanyScope
{
}

/// <summary>
/// Indicates that a request targets an explicit branch, which must match the caller's active branch scope.
/// </summary>
public interface ITargetBranchScoped : IRequireBranchScope
{
    /// <summary>The explicit branch target ID requested by the operation.</summary>
    Guid TargetBranchId { get; }
}

/// <summary>
/// Indicates that a request targets an explicit company, which must match the caller's active company scope.
/// </summary>
public interface ITargetCompanyScoped : IRequireCompanyScope
{
    /// <summary>The explicit company target ID requested by the operation.</summary>
    Guid TargetCompanyId { get; }
}

/// <summary>
/// Indicates that a request targets an explicit organization, which must match the caller's active organization scope.
/// </summary>
public interface ITargetOrganizationScoped : IRequireOrganizationScope
{
    /// <summary>The explicit organization target ID requested by the operation.</summary>
    Guid TargetOrganizationId { get; }
}

/// <summary>
/// Marker interface for unauthenticated or public requests that do not require tenancy execution scope
/// (e.g. credential verification, database connection health probing).
/// </summary>
public interface IAllowAnonymousScope
{
}
