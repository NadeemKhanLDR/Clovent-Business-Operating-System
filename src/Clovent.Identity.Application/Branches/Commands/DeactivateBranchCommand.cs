using Clovent.Identity.Application.Branches.Dtos;
using Clovent.Identity.Branches;
using MediatR;

namespace Clovent.Identity.Application.Branches.Commands;

/// <summary>Deactivates a branch.</summary>
public sealed record DeactivateBranchCommand(Guid BranchId) : IRequest<BranchDto>;

/// <summary>Handles <see cref="DeactivateBranchCommand"/>.</summary>
public sealed class DeactivateBranchCommandHandler(IBranchRepository branchRepository)
    : IRequestHandler<DeactivateBranchCommand, BranchDto>
{
    /// <inheritdoc/>
    public async Task<BranchDto> Handle(DeactivateBranchCommand request, CancellationToken cancellationToken)
    {
        var branch = await branchRepository.GetByIdAsync(new BranchId(request.BranchId), cancellationToken)
            ?? throw new NotFoundException(nameof(Branch), request.BranchId);

        branch.Deactivate();

        return BranchDto.FromDomain(branch);
    }
}
