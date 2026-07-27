using System.Globalization;

namespace Clovent.Platform.Execution;

/// <summary>
/// The ambient execution context for the current unit of work (a request,
/// a UI command, a background job). Holds only identifiers and values -
/// never full domain entities - since User/Organization/Company/Branch
/// are not implemented by Platform Foundation. Read via
/// <see cref="IExecutionContextAccessor.Current"/>; set only via
/// <see cref="ExecutionContextScope"/>.
/// </summary>
public interface IExecutionContext
{
    /// <summary>Id of the user the current operation is executing on behalf of, or <see langword="null"/> if there is none (e.g. an unauthenticated or system-initiated operation).</summary>
    Guid? UserId { get; }

    /// <summary>Id of the tenant the current operation is scoped to, or <see langword="null"/> in a single-tenant deployment or before a tenant has been resolved.</summary>
    Guid? TenantId { get; }

    /// <summary>Id of the organization the current operation is scoped to, or <see langword="null"/> if not applicable/not yet resolved.</summary>
    Guid? OrganizationId { get; }

    /// <summary>Id of the company (within the organization) the current operation is scoped to, or <see langword="null"/> if not applicable/not yet resolved.</summary>
    Guid? CompanyId { get; }

    /// <summary>Id of the branch (within the company) the current operation is scoped to, or <see langword="null"/> if not applicable/not yet resolved.</summary>
    Guid? BranchId { get; }

    /// <summary>The user's preferred display/UI language (e.g. "en"), independent of <see cref="Culture"/> - a user may want an English UI while working in a different regional locale.</summary>
    string? Language { get; }

    /// <summary>The ISO 4217 currency code (e.g. "USD") monetary values should be presented/calculated in for this operation.</summary>
    string? Currency { get; }

    /// <summary>The time zone dates/times should be converted to when presented for this operation. All persisted timestamps remain UTC regardless of this value.</summary>
    TimeZoneInfo? TimeZone { get; }

    /// <summary>The culture (number/date formatting, etc.) to use for this operation.</summary>
    CultureInfo? Culture { get; }

    /// <summary>
    /// Identifier shared by every operation that is part of the same
    /// logical transaction/workflow, even across multiple requests or
    /// process boundaries - intended for distributed tracing and
    /// cross-service log correlation.
    /// </summary>
    Guid CorrelationId { get; }

    /// <summary>Identifier unique to this single request/command/job execution, distinct from <see cref="CorrelationId"/> which may span several.</summary>
    Guid RequestId { get; }

    /// <summary>UTC timestamp this execution context was created, suitable for measuring operation duration or as an audit timestamp.</summary>
    DateTimeOffset ExecutionTimestamp { get; }
}
