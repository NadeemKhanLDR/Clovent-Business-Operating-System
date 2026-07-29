using Clovent.Identity.Application.Users.Dtos;
using Clovent.Identity.Companies;
using Clovent.Identity.Users;
using MediatR;

namespace Clovent.Identity.Application.Users.Commands;

/// <summary>Assigns (or reassigns) the single company a user operates within.</summary>
public sealed record AssignUserCompanyCommand(Guid UserId, Guid CompanyId) : IRequest<UserDto>;

/// <summary>Handles <see cref="AssignUserCompanyCommand"/>.</summary>
public sealed class AssignUserCompanyCommandHandler(IUserRepository userRepository, ICompanyRepository companyRepository)
    : IRequestHandler<AssignUserCompanyCommand, UserDto>
{
    /// <inheritdoc/>
    public async Task<UserDto> Handle(AssignUserCompanyCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(new UserId(request.UserId), cancellationToken)
            ?? throw new NotFoundException(nameof(User), request.UserId);

        var companyId = new CompanyId(request.CompanyId);
        _ = await companyRepository.GetByIdAsync(companyId, cancellationToken)
            ?? throw new NotFoundException(nameof(Company), request.CompanyId);

        user.AssignCompany(companyId);

        return UserDto.FromDomain(user);
    }
}
