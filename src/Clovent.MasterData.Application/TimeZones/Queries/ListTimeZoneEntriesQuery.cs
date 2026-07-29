using Clovent.MasterData.Application.TimeZones.Dtos;
using Clovent.MasterData.TimeZones;
using MediatR;

namespace Clovent.MasterData.Application.TimeZones.Queries;

/// <summary>Retrieves every time zone entry in the catalog.</summary>
public sealed record ListTimeZoneEntriesQuery : IRequest<IReadOnlyCollection<TimeZoneEntryDto>>;

/// <summary>Handles <see cref="ListTimeZoneEntriesQuery"/>.</summary>
public sealed class ListTimeZoneEntriesQueryHandler(ITimeZoneRepository timeZoneRepository)
    : IRequestHandler<ListTimeZoneEntriesQuery, IReadOnlyCollection<TimeZoneEntryDto>>
{
    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<TimeZoneEntryDto>> Handle(ListTimeZoneEntriesQuery request, CancellationToken cancellationToken)
    {
        var entries = await timeZoneRepository.GetAllAsync(cancellationToken);
        return [.. entries.Select(TimeZoneEntryDto.FromDomain)];
    }
}
