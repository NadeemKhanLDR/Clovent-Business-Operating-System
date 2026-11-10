using Clovent.Restaurant.Application;
using MediatR;

namespace Clovent.Restaurant.Infrastructure.Persistence;

/// <summary>Commits <see cref="IUnitOfWork.SaveChangesAsync"/> after every MediatR request completes successfully. Mirrors <c>Clovent.Catalog.Infrastructure.Persistence.UnitOfWorkBehavior</c>.</summary>
public sealed class UnitOfWorkBehavior<TRequest, TResponse>(IUnitOfWork unitOfWork) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    /// <inheritdoc/>
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var isCommand = !typeof(TRequest).Name.EndsWith("Query", StringComparison.Ordinal);
        try
        {
            var response = await next().ConfigureAwait(false);
            if (isCommand)
            {
                await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
            return response;
        }
        catch
        {
            // Handlers can mutate tracked aggregates before throwing. Skipping
            // SaveChanges alone is insufficient when the screen reuses its scope.
            if (isCommand && unitOfWork is UnitOfWork efUnitOfWork)
                efUnitOfWork.DiscardTrackedState();
            throw;
        }
    }
}
