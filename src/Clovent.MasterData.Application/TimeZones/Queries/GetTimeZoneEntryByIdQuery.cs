using Clovent.MasterData.Application.TimeZones.Dtos;
using Clovent.MasterData.TimeZones;
using MediatR;

namespace Clovent.MasterData.Application.TimeZones.Queries;

/// <summary>Retrieves a single time zone entry by identity.</summary>
public sealed record GetTimeZoneEntryByIdQuery(Guid TimeZoneEntryId) : IRequest<TimeZoneEntryDto>;

/// <summary>Handles <see cref="GetTimeZoneEntryByIdQuery"/>.</summary>
public sealed class GetTimeZoneEntryByIdQueryHandler(ITimeZoneRepository timeZoneRepository)
    : IRequestHandler<GetTimeZoneEntryByIdQuery, TimeZoneEntryDto>
{
    /// <inheritdoc/>
    public async Task<TimeZoneEntryDto> Handle(GetTimeZoneEntryByIdQuery request, CancellationToken cancellationToken)
    {
        var entry = await timeZoneRepository.GetByIdAsync(new TimeZoneEntryId(request.TimeZoneEntryId), cancellationToken)
            ?? throw new NotFoundException(nameof(TimeZoneEntry), request.TimeZoneEntryId);

        return TimeZoneEntryDto.FromDomain(entry);
    }
}
