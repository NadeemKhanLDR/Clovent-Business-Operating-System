using Clovent.MasterData.Application.TimeZones.Dtos;
using Clovent.MasterData.TimeZones;
using MediatR;

namespace Clovent.MasterData.Application.TimeZones.Commands;

/// <summary>Activates a time zone entry.</summary>
public sealed record ActivateTimeZoneEntryCommand(Guid TimeZoneEntryId) : IRequest<TimeZoneEntryDto>;

/// <summary>Handles <see cref="ActivateTimeZoneEntryCommand"/>.</summary>
public sealed class ActivateTimeZoneEntryCommandHandler(ITimeZoneRepository timeZoneRepository)
    : IRequestHandler<ActivateTimeZoneEntryCommand, TimeZoneEntryDto>
{
    /// <inheritdoc/>
    public async Task<TimeZoneEntryDto> Handle(ActivateTimeZoneEntryCommand request, CancellationToken cancellationToken)
    {
        var entry = await timeZoneRepository.GetByIdAsync(new TimeZoneEntryId(request.TimeZoneEntryId), cancellationToken)
            ?? throw new NotFoundException(nameof(TimeZoneEntry), request.TimeZoneEntryId);

        entry.Activate();

        return TimeZoneEntryDto.FromDomain(entry);
    }
}
