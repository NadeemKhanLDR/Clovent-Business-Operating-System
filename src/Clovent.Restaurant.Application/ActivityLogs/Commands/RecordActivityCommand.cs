using Clovent.Restaurant.ActivityLogs;
using Clovent.Restaurant.Application.ActivityLogs.Dtos;
using MediatR;

namespace Clovent.Restaurant.Application.ActivityLogs.Commands;

/// <summary>
/// Records one Restaurant POS activity log entry - New Order, Remove Line,
/// Price Override, Discount, Payment, Refund, Print, Setup Changes,
/// Login/Logout. <paramref name="PerformedBy"/>/<paramref name="MachineName"/>
/// are supplied by the caller (the Desktop layer's <c>ICurrentSession</c>/
/// <see cref="Environment.MachineName"/>) - this aggregate has no notion of
/// "current user" or "current machine".
/// </summary>
public sealed record RecordActivityCommand(string Action, string? Details, string PerformedBy, string MachineName) : IRequest<ActivityLogEntryDto>;

/// <summary>Handles <see cref="RecordActivityCommand"/>.</summary>
public sealed class RecordActivityCommandHandler(IActivityLogEntryRepository repository) : IRequestHandler<RecordActivityCommand, ActivityLogEntryDto>
{
    /// <inheritdoc/>
    public async Task<ActivityLogEntryDto> Handle(RecordActivityCommand request, CancellationToken cancellationToken)
    {
        var entry = ActivityLogEntry.Record(request.Action, request.Details, request.PerformedBy, request.MachineName);

        await repository.AddAsync(entry, cancellationToken);

        return ActivityLogEntryDto.FromDomain(entry);
    }
}
