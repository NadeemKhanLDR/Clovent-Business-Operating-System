using Clovent.Identity.Branches;

namespace Clovent.Identity.Application.Branches.Dtos;

/// <summary>Read-model shape for a <see cref="Branch"/>, safe to cross a process boundary.</summary>
public sealed record BranchDto(
    Guid BranchId,
    Guid CompanyId,
    string Name,
    string? Street,
    string? City,
    string? State,
    string? PostalCode,
    string? Country,
    string Status,
    DateTimeOffset CreatedAtUtc)
{
    /// <summary>Projects a domain <see cref="Branch"/> into its DTO.</summary>
    public static BranchDto FromDomain(Branch branch) => new(
        branch.Id.Value,
        branch.CompanyId.Value,
        branch.Name.Value,
        branch.Address?.Street,
        branch.Address?.City,
        branch.Address?.State,
        branch.Address?.PostalCode,
        branch.Address?.Country,
        branch.Status.ToString(),
        branch.CreatedAtUtc);
}
