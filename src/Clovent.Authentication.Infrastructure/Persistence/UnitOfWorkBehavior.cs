using Clovent.Authentication.Application;
using MediatR;

namespace Clovent.Authentication.Infrastructure.Persistence;

/// <summary>
/// Commits <see cref="IUnitOfWork.SaveChangesAsync"/> after every MediatR
/// request completes successfully. Resolves the open question
/// <c>AuthenticationInfrastructure.md</c> Section 8 left unanswered ("nothing
/// currently calls <see cref="IUnitOfWork.SaveChangesAsync"/>") the moment a
/// real host (Milestone 9's <c>Clovent.Desktop</c>) needs command handlers'
/// mutations to actually persist. Registered as an open generic
/// (<c>typeof(IPipelineBehavior&lt;,&gt;)</c>), so it applies to every
/// command/query in <c>Clovent.Authentication.Application</c> without this
/// project needing to reference that assembly's concrete request types.
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
