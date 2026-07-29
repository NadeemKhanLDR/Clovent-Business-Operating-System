using Clovent.Catalog.Brands;

namespace Clovent.Catalog.Application.Tests.TestSupport;

internal sealed class FakeBrandRepository : IBrandRepository
{
    private readonly Dictionary<BrandId, Brand> _brands = [];

    public void Add(Brand brand) => _brands[brand.Id] = brand;

    public Task<Brand?> GetByIdAsync(BrandId id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_brands.GetValueOrDefault(id));

    public Task<IReadOnlyCollection<Brand>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyCollection<Brand>>([.. _brands.Values]);

    public Task AddAsync(Brand brand, CancellationToken cancellationToken = default)
    {
        _brands[brand.Id] = brand;
        return Task.CompletedTask;
    }
}
