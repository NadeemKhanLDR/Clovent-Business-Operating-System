using System.Globalization;

namespace Clovent.Platform.Execution;

/// <summary>
/// Default, immutable implementation of <see cref="IExecutionContext"/>.
/// Use `with` expressions to derive a new context from an existing one
/// (e.g. a new RequestId per operation while keeping the same UserId/
/// TenantId).
/// </summary>
public sealed record PlatformExecutionContext : IExecutionContext
{
    public Guid? UserId { get; init; }

    public Guid? TenantId { get; init; }

    public Guid? OrganizationId { get; init; }

    public Guid? CompanyId { get; init; }

    public Guid? BranchId { get; init; }

    public string? Language { get; init; }

    public string? Currency { get; init; }

    public TimeZoneInfo? TimeZone { get; init; }

    public CultureInfo? Culture { get; init; }

    public Guid CorrelationId { get; init; } = Guid.NewGuid();

    public Guid RequestId { get; init; } = Guid.NewGuid();

    public DateTimeOffset ExecutionTimestamp { get; init; } = DateTimeOffset.UtcNow;

    public static PlatformExecutionContext Empty { get; } = new();
}
