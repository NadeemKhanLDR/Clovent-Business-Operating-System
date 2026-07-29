using Clovent.Catalog.Barcodes;
using Clovent.Catalog.Barcodes.ValueObjects;
using Clovent.Catalog.Variants;

namespace Clovent.Catalog.Application.Tests.TestSupport;

internal sealed class FakeBarcodeRepository : IBarcodeRepository
{
    private readonly Dictionary<BarcodeId, Barcode> _barcodes = [];

    public void Add(Barcode barcode) => _barcodes[barcode.Id] = barcode;

    public Task<Barcode?> GetByIdAsync(BarcodeId id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_barcodes.GetValueOrDefault(id));

    public Task<Barcode?> GetByValueAsync(BarcodeValue value, CancellationToken cancellationToken = default) =>
        Task.FromResult(_barcodes.Values.FirstOrDefault(b => b.Value == value));

    public Task<IReadOnlyCollection<Barcode>> GetByProductVariantIdAsync(ProductVariantId productVariantId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyCollection<Barcode>>([.. _barcodes.Values.Where(b => b.ProductVariantId == productVariantId)]);

    public Task<IReadOnlyCollection<Barcode>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyCollection<Barcode>>([.. _barcodes.Values]);

    public Task AddAsync(Barcode barcode, CancellationToken cancellationToken = default)
    {
        _barcodes[barcode.Id] = barcode;
        return Task.CompletedTask;
    }
}
