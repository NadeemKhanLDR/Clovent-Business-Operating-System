using Clovent.Identity.Application.Branches.Dtos;
using Clovent.Identity.Branches;
using MediatR;

namespace Clovent.Identity.Application.Branches.Queries;

/// <summary>Retrieves a single branch by identity.</summary>
public sealed record GetBranchByIdQuery(Guid BranchId) : IRequest<BranchDto>;

/// <summary>Handles <see cref="GetBranchByIdQuery"/>.</summary>
public sealed class GetBranchByIdQueryHandler(IBranchRepository branchRepository)
    : IRequestHandler<GetBranchByIdQuery, BranchDto>
{
    /// <inheritdoc/>
    public async Task<BranchDto> Handle(GetBranchByIdQuery request, CancellationToken cancellationToken)
    {
        var branch = await branchRepository.GetByIdAsync(new BranchId(request.BranchId), cancellationToken)
            ?? throw new NotFoundException(nameof(Branch), request.BranchId);

        return BranchDto.FromDomain(branch);
    }
}
