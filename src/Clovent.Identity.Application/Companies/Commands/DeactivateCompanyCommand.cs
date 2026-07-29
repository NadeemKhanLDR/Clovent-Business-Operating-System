using Clovent.Identity.Application.Companies.Dtos;
using Clovent.Identity.Companies;
using MediatR;

namespace Clovent.Identity.Application.Companies.Commands;

/// <summary>Deactivates a company.</summary>
public sealed record DeactivateCompanyCommand(Guid CompanyId) : IRequest<CompanyDto>;

/// <summary>Handles <see cref="DeactivateCompanyCommand"/>.</summary>
public sealed class DeactivateCompanyCommandHandler(ICompanyRepository companyRepository)
    : IRequestHandler<DeactivateCompanyCommand, CompanyDto>
{
    /// <inheritdoc/>
    public async Task<CompanyDto> Handle(DeactivateCompanyCommand request, CancellationToken cancellationToken)
    {
        var company = await companyRepository.GetByIdAsync(new CompanyId(request.CompanyId), cancellationToken)
            ?? throw new NotFoundException(nameof(Company), request.CompanyId);

        company.Deactivate();

        return CompanyDto.FromDomain(company);
    }
}
