using Clovent.Identity.Organizations;
using Clovent.MasterData.Application.FiscalYears.Dtos;
using Clovent.MasterData.FiscalYears;
using Clovent.MasterData.FiscalYears.ValueObjects;
using MediatR;

namespace Clovent.MasterData.Application.FiscalYears.Commands;

/// <summary>Creates a new fiscal year for an existing organization.</summary>
public sealed record CreateFiscalYearCommand(Guid OrganizationId, string Name, DateOnly StartDate, DateOnly EndDate) : IRequest<FiscalYearDto>;

/// <summary>Handles <see cref="CreateFiscalYearCommand"/>.</summary>
public sealed class CreateFiscalYearCommandHandler(IFiscalYearRepository fiscalYearRepository)
    : IRequestHandler<CreateFiscalYearCommand, FiscalYearDto>
{
    /// <inheritdoc/>
    public async Task<FiscalYearDto> Handle(CreateFiscalYearCommand request, CancellationToken cancellationToken)
    {
        var fiscalYear = FiscalYear.Create(
            new OrganizationId(request.OrganizationId),
            FiscalYearName.Create(request.Name),
            request.StartDate,
            request.EndDate);

        await fiscalYearRepository.AddAsync(fiscalYear, cancellationToken);

        return FiscalYearDto.FromDomain(fiscalYear);
    }
}
