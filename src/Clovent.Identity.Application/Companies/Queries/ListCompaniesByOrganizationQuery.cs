using Clovent.Identity.Application.Companies.Dtos;
using Clovent.Identity.Companies;
using Clovent.Identity.Organizations;
using MediatR;

namespace Clovent.Identity.Application.Companies.Queries;

/// <summary>Retrieves every company belonging to the given organization.</summary>
public sealed record ListCompaniesByOrganizationQuery(Guid OrganizationId) : IRequest<IReadOnlyCollection<CompanyDto>>;

/// <summary>Handles <see cref="ListCompaniesByOrganizationQuery"/>.</summary>
public sealed class ListCompaniesByOrganizationQueryHandler(ICompanyRepository companyRepository)
    : IRequestHandler<ListCompaniesByOrganizationQuery, IReadOnlyCollection<CompanyDto>>
{
    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<CompanyDto>> Handle(ListCompaniesByOrganizationQuery request, CancellationToken cancellationToken)
    {
        var companies = await companyRepository.GetByOrganizationIdAsync(new OrganizationId(request.OrganizationId), cancellationToken);
        return [.. companies.Select(CompanyDto.FromDomain)];
    }
}
