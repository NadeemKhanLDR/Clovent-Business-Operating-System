using Clovent.Identity.Organizations;

namespace Clovent.Identity.Application.Organizations.Dtos;

/// <summary>Read-model shape for an <see cref="Organization"/>, safe to cross a process boundary.</summary>
public sealed record OrganizationDto(
    Guid OrganizationId,
    string Name,
    string? TaxId,
    string Status,
    DateTimeOffset CreatedAtUtc,
    IReadOnlyCollection<Guid> CompanyIds)
{
    /// <summary>Projects a domain <see cref="Organization"/> into its DTO.</summary>
    public static OrganizationDto FromDomain(Organization organization) => new(
        organization.Id.Value,
        organization.Name.Value,
        organization.TaxId?.Value,
        organization.Status.ToString(),
        organization.CreatedAtUtc,
        [.. organization.CompanyIds.Select(id => id.Value)]);
}
