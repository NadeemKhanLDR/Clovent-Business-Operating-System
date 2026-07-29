using Clovent.Catalog.Groups;

namespace Clovent.Catalog.Application.Tests.TestSupport;

internal sealed class FakeProductGroupRepository : IProductGroupRepository
{
    private readonly Dictionary<ProductGroupId, ProductGroup> _groups = [];

    public void Add(ProductGroup group) => _groups[group.Id] = group;

    public Task<ProductGroup?> GetByIdAsync(ProductGroupId id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_groups.GetValueOrDefault(id));

    public Task<IReadOnlyCollection<ProductGroup>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyCollection<ProductGroup>>([.. _groups.Values]);

    public Task AddAsync(ProductGroup group, CancellationToken cancellationToken = default)
    {
        _groups[group.Id] = group;
        return Task.CompletedTask;
    }
}
