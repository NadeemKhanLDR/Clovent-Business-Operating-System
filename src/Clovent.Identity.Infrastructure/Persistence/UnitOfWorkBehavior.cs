using Clovent.Identity.Application;
using MediatR;

namespace Clovent.Identity.Infrastructure.Persistence;

/// <summary>
/// Commits <see cref="IUnitOfWork.SaveChangesAsync"/> after every MediatR
/// request completes successfully. Mirrors
/// <c>Clovent.Authentication.Infrastructure.Persistence.UnitOfWorkBehavior</c>.
/// Registered as an open generic, so it applies to every command/query in
/// <c>Clovent.Identity.Application</c> without this project needing to
/// reference that assembly's concrete request types.
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
