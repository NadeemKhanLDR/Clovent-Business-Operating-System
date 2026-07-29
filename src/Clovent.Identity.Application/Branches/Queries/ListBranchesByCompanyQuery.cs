using Clovent.Identity.Application.Branches.Dtos;
using Clovent.Identity.Branches;
using Clovent.Identity.Companies;
using MediatR;

namespace Clovent.Identity.Application.Branches.Queries;

/// <summary>Retrieves every branch belonging to the given company.</summary>
public sealed record ListBranchesByCompanyQuery(Guid CompanyId) : IRequest<IReadOnlyCollection<BranchDto>>;

/// <summary>Handles <see cref="ListBranchesByCompanyQuery"/>.</summary>
public sealed class ListBranchesByCompanyQueryHandler(IBranchRepository branchRepository)
    : IRequestHandler<ListBranchesByCompanyQuery, IReadOnlyCollection<BranchDto>>
{
    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<BranchDto>> Handle(ListBranchesByCompanyQuery request, CancellationToken cancellationToken)
    {
        var branches = await branchRepository.GetByCompanyIdAsync(new CompanyId(request.CompanyId), cancellationToken);
        return [.. branches.Select(BranchDto.FromDomain)];
    }
}
