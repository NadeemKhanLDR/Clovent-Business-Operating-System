using Clovent.Platform.Bootstrap;
using Clovent.Restaurant.Application.Tables.Commands;
using MediatR;

namespace Clovent.Desktop.Startup;

/// <summary>
/// Reconciles table occupancy states against legitimate active orders on application startup.
/// Ensures historical (cancelled, completed, voided) orders do not leave tables occupied,
/// while preserving occupied state for tables with legitimate active Dine-In orders.
/// </summary>
public sealed class TableOccupancyReconciliationStartupTask(IMediator mediator) : IStartupTask
{
    /// <inheritdoc/>
    public Task ExecuteAsync(CancellationToken cancellationToken = default) =>
        mediator.Send(new ReconcileTableOccupancyCommand(), cancellationToken);
}
