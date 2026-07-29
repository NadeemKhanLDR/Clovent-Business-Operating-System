using Clovent.MasterData.TimeZones;

namespace Clovent.MasterData.Application.TimeZones.Dtos;

/// <summary>Read-model shape for a <see cref="TimeZoneEntry"/>, safe to cross a process boundary.</summary>
public sealed record TimeZoneEntryDto(
    Guid TimeZoneEntryId,
    string IanaId,
    string DisplayName,
    int UtcOffsetMinutes,
    string Status,
    DateTimeOffset CreatedAtUtc)
{
    /// <summary>Projects a domain <see cref="TimeZoneEntry"/> into its DTO.</summary>
    public static TimeZoneEntryDto FromDomain(TimeZoneEntry entry) => new(
        entry.Id.Value,
        entry.IanaId.Value,
        entry.DisplayName,
        entry.UtcOffsetMinutes,
        entry.Status.ToString(),
        entry.CreatedAtUtc);
}
