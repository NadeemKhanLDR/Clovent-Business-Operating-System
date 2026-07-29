using Clovent.MasterData.Application.FiscalYears.Dtos;
using Clovent.MasterData.FiscalYears;
using MediatR;

namespace Clovent.MasterData.Application.FiscalYears.Queries;

/// <summary>Retrieves a single fiscal year by identity.</summary>
public sealed record GetFiscalYearByIdQuery(Guid FiscalYearId) : IRequest<FiscalYearDto>;

/// <summary>Handles <see cref="GetFiscalYearByIdQuery"/>.</summary>
public sealed class GetFiscalYearByIdQueryHandler(IFiscalYearRepository fiscalYearRepository)
    : IRequestHandler<GetFiscalYearByIdQuery, FiscalYearDto>
{
    /// <inheritdoc/>
    public async Task<FiscalYearDto> Handle(GetFiscalYearByIdQuery request, CancellationToken cancellationToken)
    {
        var fiscalYear = await fiscalYearRepository.GetByIdAsync(new FiscalYearId(request.FiscalYearId), cancellationToken)
            ?? throw new NotFoundException(nameof(FiscalYear), request.FiscalYearId);

        return FiscalYearDto.FromDomain(fiscalYear);
    }
}
