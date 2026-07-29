using Clovent.Identity.Application.Branches.Dtos;
using Clovent.Identity.Branches;
using Clovent.Identity.Branches.ValueObjects;
using MediatR;

namespace Clovent.Identity.Application.Branches.Commands;

/// <summary>Renames an existing branch.</summary>
public sealed record RenameBranchCommand(Guid BranchId, string Name) : IRequest<BranchDto>;

/// <summary>Handles <see cref="RenameBranchCommand"/>.</summary>
public sealed class RenameBranchCommandHandler(IBranchRepository branchRepository)
    : IRequestHandler<RenameBranchCommand, BranchDto>
{
    /// <inheritdoc/>
    public async Task<BranchDto> Handle(RenameBranchCommand request, CancellationToken cancellationToken)
    {
        var branch = await branchRepository.GetByIdAsync(new BranchId(request.BranchId), cancellationToken)
            ?? throw new NotFoundException(nameof(Branch), request.BranchId);

        branch.Rename(BranchName.Create(request.Name));

        return BranchDto.FromDomain(branch);
    }
}
