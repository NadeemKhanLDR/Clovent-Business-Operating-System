using Clovent.Identity.Application.Companies.Dtos;
using Clovent.Identity.Companies;
using MediatR;

namespace Clovent.Identity.Application.Companies.Commands;

/// <summary>Activates a company.</summary>
public sealed record ActivateCompanyCommand(Guid CompanyId) : IRequest<CompanyDto>;

/// <summary>Handles <see cref="ActivateCompanyCommand"/>.</summary>
public sealed class ActivateCompanyCommandHandler(ICompanyRepository companyRepository)
    : IRequestHandler<ActivateCompanyCommand, CompanyDto>
{
    /// <inheritdoc/>
    public async Task<CompanyDto> Handle(ActivateCompanyCommand request, CancellationToken cancellationToken)
    {
        var company = await companyRepository.GetByIdAsync(new CompanyId(request.CompanyId), cancellationToken)
            ?? throw new NotFoundException(nameof(Company), request.CompanyId);

        company.Activate();

        return CompanyDto.FromDomain(company);
    }
}
