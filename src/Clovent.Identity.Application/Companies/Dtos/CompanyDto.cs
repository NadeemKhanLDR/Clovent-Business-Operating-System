using Clovent.Identity.Companies;

namespace Clovent.Identity.Application.Companies.Dtos;

/// <summary>Read-model shape for a <see cref="Company"/>, safe to cross a process boundary.</summary>
public sealed record CompanyDto(
    Guid CompanyId,
    Guid OrganizationId,
    string Name,
    string? TaxId,
    string Status,
    DateTimeOffset CreatedAtUtc,
    IReadOnlyCollection<Guid> BranchIds)
{
    /// <summary>Projects a domain <see cref="Company"/> into its DTO.</summary>
    public static CompanyDto FromDomain(Company company) => new(
        company.Id.Value,
        company.OrganizationId.Value,
        company.Name.Value,
        company.TaxId?.Value,
        company.Status.ToString(),
        company.CreatedAtUtc,
        [.. company.BranchIds.Select(id => id.Value)]);
}
