using System.Globalization;

namespace Clovent.Platform.Execution;

/// <summary>
/// The ambient execution context for the current unit of work (a request,
/// a UI command, a background job). Holds only identifiers and values -
/// never full domain entities - since User/Organization/Company/Branch
/// are not implemented by Platform Foundation.
/// </summary>
public interface IExecutionContext
{
    Guid? UserId { get; }

    Guid? TenantId { get; }

    Guid? OrganizationId { get; }

    Guid? CompanyId { get; }

    Guid? BranchId { get; }

    string? Language { get; }

    string? Currency { get; }

    TimeZoneInfo? TimeZone { get; }

    CultureInfo? Culture { get; }

    Guid CorrelationId { get; }

    Guid RequestId { get; }

    DateTimeOffset ExecutionTimestamp { get; }
}
