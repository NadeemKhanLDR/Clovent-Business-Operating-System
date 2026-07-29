using Clovent.Identity.Application.Branches.Dtos;
using Clovent.Identity.Branches;
using Clovent.Identity.Shared.ValueObjects;
using MediatR;

namespace Clovent.Identity.Application.Branches.Commands;

/// <summary>Sets or clears a branch's address. Pass every field <see langword="null"/> to clear.</summary>
public sealed record SetBranchAddressCommand(
    Guid BranchId,
    string? Street,
    string? City,
    string? State,
    string? PostalCode,
    string? Country) : IRequest<BranchDto>;

/// <summary>Handles <see cref="SetBranchAddressCommand"/>.</summary>
public sealed class SetBranchAddressCommandHandler(IBranchRepository branchRepository)
    : IRequestHandler<SetBranchAddressCommand, BranchDto>
{
    /// <inheritdoc/>
    public async Task<BranchDto> Handle(SetBranchAddressCommand request, CancellationToken cancellationToken)
    {
        var branch = await branchRepository.GetByIdAsync(new BranchId(request.BranchId), cancellationToken)
            ?? throw new NotFoundException(nameof(Branch), request.BranchId);

        var clearing = request.Street is null && request.City is null && request.State is null && request.PostalCode is null && request.Country is null;
        branch.SetAddress(clearing
            ? null
            : Address.Create(request.Street ?? "", request.City ?? "", request.State ?? "", request.PostalCode ?? "", request.Country ?? ""));

        return BranchDto.FromDomain(branch);
    }
}
