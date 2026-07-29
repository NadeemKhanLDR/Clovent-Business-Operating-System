using Clovent.Identity.Organizations;

namespace Clovent.Identity.Application.Tests.TestSupport;

internal sealed class FakeOrganizationRepository : IOrganizationRepository
{
    private readonly Dictionary<OrganizationId, Organization> _organizations = [];

    public void Add(Organization organization) => _organizations[organization.Id] = organization;

    public Task<Organization?> GetByIdAsync(OrganizationId id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_organizations.GetValueOrDefault(id));

    public Task<IReadOnlyCollection<Organization>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyCollection<Organization>>([.. _organizations.Values]);

    public Task AddAsync(Organization organization, CancellationToken cancellationToken = default)
    {
        _organizations[organization.Id] = organization;
        return Task.CompletedTask;
    }
}
