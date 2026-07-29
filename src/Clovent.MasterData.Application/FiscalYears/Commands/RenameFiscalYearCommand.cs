using Clovent.MasterData.Application.FiscalYears.Dtos;
using Clovent.MasterData.FiscalYears;
using Clovent.MasterData.FiscalYears.ValueObjects;
using MediatR;

namespace Clovent.MasterData.Application.FiscalYears.Commands;

/// <summary>Renames an existing fiscal year.</summary>
public sealed record RenameFiscalYearCommand(Guid FiscalYearId, string Name) : IRequest<FiscalYearDto>;

/// <summary>Handles <see cref="RenameFiscalYearCommand"/>.</summary>
public sealed class RenameFiscalYearCommandHandler(IFiscalYearRepository fiscalYearRepository)
    : IRequestHandler<RenameFiscalYearCommand, FiscalYearDto>
{
    /// <inheritdoc/>
    public async Task<FiscalYearDto> Handle(RenameFiscalYearCommand request, CancellationToken cancellationToken)
    {
        var fiscalYear = await fiscalYearRepository.GetByIdAsync(new FiscalYearId(request.FiscalYearId), cancellationToken)
            ?? throw new NotFoundException(nameof(FiscalYear), request.FiscalYearId);

        fiscalYear.Rename(FiscalYearName.Create(request.Name));

        return FiscalYearDto.FromDomain(fiscalYear);
    }
}
