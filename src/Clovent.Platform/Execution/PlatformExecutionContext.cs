using System.Globalization;

namespace Clovent.Platform.Execution;

/// <summary>
/// Default, immutable implementation of <see cref="IExecutionContext"/>.
/// Use <see langword="with"/> expressions to derive a new context from an
/// existing one (e.g. a new <see cref="RequestId"/> per operation while
/// keeping the same <see cref="UserId"/>/<see cref="TenantId"/>).
/// </summary>
public sealed record PlatformExecutionContext : IExecutionContext
{
    /// <inheritdoc />
    public Guid? UserId { get; init; }

    /// <inheritdoc />
    public Guid? TenantId { get; init; }

    /// <inheritdoc />
    public Guid? OrganizationId { get; init; }

    /// <inheritdoc />
    public Guid? CompanyId { get; init; }

    /// <inheritdoc />
    public Guid? BranchId { get; init; }

    /// <inheritdoc />
    public string? Language { get; init; }

    /// <inheritdoc />
    public string? Currency { get; init; }

    /// <inheritdoc />
    public TimeZoneInfo? TimeZone { get; init; }

    /// <inheritdoc />
    public CultureInfo? Culture { get; init; }

    /// <inheritdoc />
    public Guid CorrelationId { get; init; } = Guid.NewGuid();

    /// <inheritdoc />
    public Guid RequestId { get; init; } = Guid.NewGuid();

    /// <inheritdoc />
    public DateTimeOffset ExecutionTimestamp { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// A context with every field at its default (all identifiers/values
    /// <see langword="null"/>, fresh <see cref="CorrelationId"/>/<see cref="RequestId"/>,
    /// current timestamp). Useful as a base for <see langword="with"/>
    /// expressions, or as a placeholder in tests.
    /// </summary>
    public static PlatformExecutionContext Empty { get; } = new();
}
