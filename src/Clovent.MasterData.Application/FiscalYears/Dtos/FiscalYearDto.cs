using Clovent.MasterData.FiscalYears;

namespace Clovent.MasterData.Application.FiscalYears.Dtos;

/// <summary>Read-model shape for a <see cref="FiscalYear"/>, safe to cross a process boundary.</summary>
public sealed record FiscalYearDto(
    Guid FiscalYearId,
    Guid OrganizationId,
    string Name,
    DateOnly StartDate,
    DateOnly EndDate,
    string Status,
    DateTimeOffset CreatedAtUtc)
{
    /// <summary>Projects a domain <see cref="FiscalYear"/> into its DTO.</summary>
    public static FiscalYearDto FromDomain(FiscalYear fiscalYear) => new(
        fiscalYear.Id.Value,
        fiscalYear.OrganizationId.Value,
        fiscalYear.Name.Value,
        fiscalYear.StartDate,
        fiscalYear.EndDate,
        fiscalYear.Status.ToString(),
        fiscalYear.CreatedAtUtc);
}
