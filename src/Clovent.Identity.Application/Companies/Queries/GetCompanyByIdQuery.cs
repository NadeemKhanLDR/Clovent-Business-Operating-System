using Clovent.Identity.Application.Companies.Dtos;
using Clovent.Identity.Companies;
using MediatR;

namespace Clovent.Identity.Application.Companies.Queries;

/// <summary>Retrieves a single company by identity.</summary>
public sealed record GetCompanyByIdQuery(Guid CompanyId) : IRequest<CompanyDto>;

/// <summary>Handles <see cref="GetCompanyByIdQuery"/>.</summary>
public sealed class GetCompanyByIdQueryHandler(ICompanyRepository companyRepository)
    : IRequestHandler<GetCompanyByIdQuery, CompanyDto>
{
    /// <inheritdoc/>
    public async Task<CompanyDto> Handle(GetCompanyByIdQuery request, CancellationToken cancellationToken)
    {
        var company = await companyRepository.GetByIdAsync(new CompanyId(request.CompanyId), cancellationToken)
            ?? throw new NotFoundException(nameof(Company), request.CompanyId);

        return CompanyDto.FromDomain(company);
    }
}
