using Clovent.Identity.Organizations;
using Clovent.MasterData.Application.FiscalYears.Dtos;
using Clovent.MasterData.FiscalYears;
using MediatR;

namespace Clovent.MasterData.Application.FiscalYears.Queries;

/// <summary>Retrieves every fiscal year belonging to the given organization.</summary>
public sealed record ListFiscalYearsByOrganizationQuery(Guid OrganizationId) : IRequest<IReadOnlyCollection<FiscalYearDto>>;

/// <summary>Handles <see cref="ListFiscalYearsByOrganizationQuery"/>.</summary>
public sealed class ListFiscalYearsByOrganizationQueryHandler(IFiscalYearRepository fiscalYearRepository)
    : IRequestHandler<ListFiscalYearsByOrganizationQuery, IReadOnlyCollection<FiscalYearDto>>
{
    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<FiscalYearDto>> Handle(ListFiscalYearsByOrganizationQuery request, CancellationToken cancellationToken)
    {
        var fiscalYears = await fiscalYearRepository.GetByOrganizationIdAsync(new OrganizationId(request.OrganizationId), cancellationToken);
        return [.. fiscalYears.Select(FiscalYearDto.FromDomain)];
    }
}
