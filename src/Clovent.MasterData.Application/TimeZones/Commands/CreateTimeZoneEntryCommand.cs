using Clovent.MasterData.Application.TimeZones.Dtos;
using Clovent.MasterData.TimeZones;
using MediatR;

namespace Clovent.MasterData.Application.TimeZones.Commands;

/// <summary>Creates a new time zone catalog entry.</summary>
public sealed record CreateTimeZoneEntryCommand(string IanaId, string DisplayName, int UtcOffsetMinutes) : IRequest<TimeZoneEntryDto>;

/// <summary>Handles <see cref="CreateTimeZoneEntryCommand"/>.</summary>
public sealed class CreateTimeZoneEntryCommandHandler(ITimeZoneRepository timeZoneRepository)
    : IRequestHandler<CreateTimeZoneEntryCommand, TimeZoneEntryDto>
{
    /// <inheritdoc/>
    public async Task<TimeZoneEntryDto> Handle(CreateTimeZoneEntryCommand request, CancellationToken cancellationToken)
    {
        var entry = TimeZoneEntry.Create(IanaId.Create(request.IanaId), request.DisplayName, request.UtcOffsetMinutes);

        await timeZoneRepository.AddAsync(entry, cancellationToken);

        return TimeZoneEntryDto.FromDomain(entry);
    }
}
