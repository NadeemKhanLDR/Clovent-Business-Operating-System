using Clovent.Restaurant.ActivityLogs;

namespace Clovent.Restaurant.Application.ActivityLogs.Dtos;

/// <summary>Read-model shape for an <see cref="ActivityLogEntry"/>, safe to cross a process boundary.</summary>
public sealed record ActivityLogEntryDto(
    Guid ActivityLogEntryId,
    string Action,
    string? Details,
    string PerformedBy,
    string MachineName,
    DateTimeOffset OccurredAtUtc)
{
    /// <summary>Projects a domain <see cref="ActivityLogEntry"/> into its DTO.</summary>
    public static ActivityLogEntryDto FromDomain(ActivityLogEntry entry) => new(
        entry.Id.Value,
        entry.Action,
        entry.Details,
        entry.PerformedBy,
        entry.MachineName,
        entry.OccurredAtUtc);
}
