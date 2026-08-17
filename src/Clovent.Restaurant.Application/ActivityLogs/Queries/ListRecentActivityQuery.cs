using Clovent.Restaurant.ActivityLogs;
using Clovent.Restaurant.Application.ActivityLogs.Dtos;
using MediatR;

namespace Clovent.Restaurant.Application.ActivityLogs.Queries;

/// <summary>Retrieves the most recent activity log entries, newest first, for the Activity Log viewer screen.</summary>
public sealed record ListRecentActivityQuery(int Limit = 500) : IRequest<IReadOnlyCollection<ActivityLogEntryDto>>;

/// <summary>Handles <see cref="ListRecentActivityQuery"/>.</summary>
public sealed class ListRecentActivityQueryHandler(IActivityLogEntryRepository repository)
    : IRequestHandler<ListRecentActivityQuery, IReadOnlyCollection<ActivityLogEntryDto>>
{
    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<ActivityLogEntryDto>> Handle(ListRecentActivityQuery request, CancellationToken cancellationToken)
    {
        var entries = await repository.ListRecentAsync(request.Limit, cancellationToken);
        return [.. entries.Select(ActivityLogEntryDto.FromDomain)];
    }
}
