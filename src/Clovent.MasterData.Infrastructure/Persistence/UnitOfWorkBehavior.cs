using Clovent.MasterData.Application;
using MediatR;

namespace Clovent.MasterData.Infrastructure.Persistence;

/// <summary>
/// Commits <see cref="IUnitOfWork.SaveChangesAsync"/> after every MediatR
/// request completes successfully. Mirrors
/// <c>Clovent.Identity.Infrastructure.Persistence.UnitOfWorkBehavior</c>.
/// </summary>
public sealed class UnitOfWorkBehavior<TRequest, TResponse>(IUnitOfWork unitOfWork) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    /// <inheritdoc/>
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var response = await next();
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return response;
    }
}
