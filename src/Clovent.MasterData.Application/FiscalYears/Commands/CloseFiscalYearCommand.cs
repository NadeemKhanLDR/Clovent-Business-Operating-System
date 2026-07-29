using Clovent.MasterData.Application.FiscalYears.Dtos;
using Clovent.MasterData.FiscalYears;
using MediatR;

namespace Clovent.MasterData.Application.FiscalYears.Commands;

/// <summary>Closes a fiscal year. This is a one-way transition.</summary>
public sealed record CloseFiscalYearCommand(Guid FiscalYearId) : IRequest<FiscalYearDto>;

/// <summary>Handles <see cref="CloseFiscalYearCommand"/>.</summary>
public sealed class CloseFiscalYearCommandHandler(IFiscalYearRepository fiscalYearRepository)
    : IRequestHandler<CloseFiscalYearCommand, FiscalYearDto>
{
    /// <inheritdoc/>
    public async Task<FiscalYearDto> Handle(CloseFiscalYearCommand request, CancellationToken cancellationToken)
    {
        var fiscalYear = await fiscalYearRepository.GetByIdAsync(new FiscalYearId(request.FiscalYearId), cancellationToken)
            ?? throw new NotFoundException(nameof(FiscalYear), request.FiscalYearId);

        fiscalYear.Close();

        return FiscalYearDto.FromDomain(fiscalYear);
    }
}
