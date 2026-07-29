using Clovent.MasterData.Application.TimeZones.Dtos;
using Clovent.MasterData.TimeZones;
using MediatR;

namespace Clovent.MasterData.Application.TimeZones.Commands;

/// <summary>Deactivates a time zone entry.</summary>
public sealed record DeactivateTimeZoneEntryCommand(Guid TimeZoneEntryId) : IRequest<TimeZoneEntryDto>;

/// <summary>Handles <see cref="DeactivateTimeZoneEntryCommand"/>.</summary>
public sealed class DeactivateTimeZoneEntryCommandHandler(ITimeZoneRepository timeZoneRepository)
    : IRequestHandler<DeactivateTimeZoneEntryCommand, TimeZoneEntryDto>
{
    /// <inheritdoc/>
    public async Task<TimeZoneEntryDto> Handle(DeactivateTimeZoneEntryCommand request, CancellationToken cancellationToken)
    {
        var entry = await timeZoneRepository.GetByIdAsync(new TimeZoneEntryId(request.TimeZoneEntryId), cancellationToken)
            ?? throw new NotFoundException(nameof(TimeZoneEntry), request.TimeZoneEntryId);

        entry.Deactivate();

        return TimeZoneEntryDto.FromDomain(entry);
    }
}
