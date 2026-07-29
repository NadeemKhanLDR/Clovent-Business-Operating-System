using Clovent.Catalog.Categories;

namespace Clovent.Catalog.Application.Tests.TestSupport;

internal sealed class FakeProductCategoryRepository : IProductCategoryRepository
{
    private readonly Dictionary<ProductCategoryId, ProductCategory> _categories = [];

    public void Add(ProductCategory category) => _categories[category.Id] = category;

    public Task<ProductCategory?> GetByIdAsync(ProductCategoryId id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_categories.GetValueOrDefault(id));

    public Task<IReadOnlyCollection<ProductCategory>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyCollection<ProductCategory>>([.. _categories.Values]);

    public Task AddAsync(ProductCategory category, CancellationToken cancellationToken = default)
    {
        _categories[category.Id] = category;
        return Task.CompletedTask;
    }
}
