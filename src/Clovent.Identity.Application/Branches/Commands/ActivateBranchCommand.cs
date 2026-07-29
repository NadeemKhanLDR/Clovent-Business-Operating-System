using Clovent.Identity.Application.Branches.Dtos;
using Clovent.Identity.Branches;
using MediatR;

namespace Clovent.Identity.Application.Branches.Commands;

/// <summary>Activates a branch.</summary>
public sealed record ActivateBranchCommand(Guid BranchId) : IRequest<BranchDto>;

/// <summary>Handles <see cref="ActivateBranchCommand"/>.</summary>
public sealed class ActivateBranchCommandHandler(IBranchRepository branchRepository)
    : IRequestHandler<ActivateBranchCommand, BranchDto>
{
    /// <inheritdoc/>
    public async Task<BranchDto> Handle(ActivateBranchCommand request, CancellationToken cancellationToken)
    {
        var branch = await branchRepository.GetByIdAsync(new BranchId(request.BranchId), cancellationToken)
            ?? throw new NotFoundException(nameof(Branch), request.BranchId);

        branch.Activate();

        return BranchDto.FromDomain(branch);
    }
}
