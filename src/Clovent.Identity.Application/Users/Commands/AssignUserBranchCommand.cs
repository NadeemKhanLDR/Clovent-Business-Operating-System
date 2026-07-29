using Clovent.Identity.Application.Users.Dtos;
using Clovent.Identity.Branches;
using Clovent.Identity.Users;
using MediatR;

namespace Clovent.Identity.Application.Users.Commands;

/// <summary>Assigns (or reassigns) the single branch a user operates within.</summary>
public sealed record AssignUserBranchCommand(Guid UserId, Guid BranchId) : IRequest<UserDto>;

/// <summary>Handles <see cref="AssignUserBranchCommand"/>.</summary>
public sealed class AssignUserBranchCommandHandler(IUserRepository userRepository, IBranchRepository branchRepository)
    : IRequestHandler<AssignUserBranchCommand, UserDto>
{
    /// <inheritdoc/>
    public async Task<UserDto> Handle(AssignUserBranchCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(new UserId(request.UserId), cancellationToken)
            ?? throw new NotFoundException(nameof(User), request.UserId);

        var branchId = new BranchId(request.BranchId);
        _ = await branchRepository.GetByIdAsync(branchId, cancellationToken)
            ?? throw new NotFoundException(nameof(Branch), request.BranchId);

        user.AssignBranch(branchId);

        return UserDto.FromDomain(user);
    }
}
