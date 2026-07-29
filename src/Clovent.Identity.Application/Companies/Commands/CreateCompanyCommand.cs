using Clovent.Identity.Application.Companies.Dtos;
using Clovent.Identity.Companies;
using Clovent.Identity.Companies.ValueObjects;
using Clovent.Identity.Organizations;
using Clovent.Identity.Shared.ValueObjects;
using MediatR;

namespace Clovent.Identity.Application.Companies.Commands;

/// <summary>Creates a new company under an existing organization.</summary>
public sealed record CreateCompanyCommand(Guid OrganizationId, string Name, string? TaxId = null) : IRequest<CompanyDto>;

/// <summary>Handles <see cref="CreateCompanyCommand"/>.</summary>
public sealed class CreateCompanyCommandHandler(IOrganizationRepository organizationRepository, ICompanyRepository companyRepository)
    : IRequestHandler<CreateCompanyCommand, CompanyDto>
{
    /// <inheritdoc/>
    public async Task<CompanyDto> Handle(CreateCompanyCommand request, CancellationToken cancellationToken)
    {
        var organizationId = new OrganizationId(request.OrganizationId);
        var organization = await organizationRepository.GetByIdAsync(organizationId, cancellationToken)
            ?? throw new NotFoundException(nameof(Organization), request.OrganizationId);

        var taxId = request.TaxId is null ? null : TaxId.Create(request.TaxId);
        var company = Company.Create(organizationId, CompanyName.Create(request.Name), taxId);
        organization.AddCompany(company.Id);

        await companyRepository.AddAsync(company, cancellationToken);

        return CompanyDto.FromDomain(company);
    }
}
