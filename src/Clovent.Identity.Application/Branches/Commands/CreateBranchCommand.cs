using Clovent.Identity.Application.Branches.Dtos;
using Clovent.Identity.Branches;
using Clovent.Identity.Branches.ValueObjects;
using Clovent.Identity.Companies;
using Clovent.Identity.Shared.ValueObjects;
using MediatR;

namespace Clovent.Identity.Application.Branches.Commands;

/// <summary>Creates a new branch under an existing company. Address fields are optional but, if any is supplied, all must be.</summary>
public sealed record CreateBranchCommand(
    Guid CompanyId,
    string Name,
    string? Street = null,
    string? City = null,
    string? State = null,
    string? PostalCode = null,
    string? Country = null) : IRequest<BranchDto>;

/// <summary>Handles <see cref="CreateBranchCommand"/>.</summary>
public sealed class CreateBranchCommandHandler(ICompanyRepository companyRepository, IBranchRepository branchRepository)
    : IRequestHandler<CreateBranchCommand, BranchDto>
{
    /// <inheritdoc/>
    public async Task<BranchDto> Handle(CreateBranchCommand request, CancellationToken cancellationToken)
    {
        var companyId = new CompanyId(request.CompanyId);
        var company = await companyRepository.GetByIdAsync(companyId, cancellationToken)
            ?? throw new NotFoundException(nameof(Company), request.CompanyId);

        var address = BuildAddress(request);
        var branch = Branch.Create(companyId, BranchName.Create(request.Name), address);
        company.AddBranch(branch.Id);

        await branchRepository.AddAsync(branch, cancellationToken);

        return BranchDto.FromDomain(branch);
    }

    private static Address? BuildAddress(CreateBranchCommand request)
    {
        if (request.Street is null && request.City is null && request.State is null && request.PostalCode is null && request.Country is null)
            return null;

        return Address.Create(request.Street ?? "", request.City ?? "", request.State ?? "", request.PostalCode ?? "", request.Country ?? "");
    }
}
