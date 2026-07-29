using Clovent.Catalog.Barcodes;
using Clovent.Catalog.Barcodes.ValueObjects;
using Clovent.Catalog.Infrastructure.Persistence;
using Clovent.Catalog.Variants;
using Microsoft.EntityFrameworkCore;

namespace Clovent.Catalog.Infrastructure.Repositories;

/// <summary>EF Core implementation of <see cref="IBarcodeRepository"/>.</summary>
public sealed class BarcodeRepository(CatalogDbContext dbContext) : IBarcodeRepository
{
    /// <inheritdoc/>
    public Task<Barcode?> GetByIdAsync(BarcodeId id, CancellationToken cancellationToken = default) =>
        dbContext.Barcodes.FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

    /// <inheritdoc/>
    public Task<Barcode?> GetByValueAsync(BarcodeValue value, CancellationToken cancellationToken = default) =>
        dbContext.Barcodes.FirstOrDefaultAsync(b => b.Value == value, cancellationToken);

    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<Barcode>> GetByProductVariantIdAsync(ProductVariantId productVariantId, CancellationToken cancellationToken = default) =>
        await dbContext.Barcodes.Where(b => b.ProductVariantId == productVariantId).ToListAsync(cancellationToken);

    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<Barcode>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Barcodes.ToListAsync(cancellationToken);

    /// <inheritdoc/>
    public async Task AddAsync(Barcode barcode, CancellationToken cancellationToken = default) =>
        await dbContext.Barcodes.AddAsync(barcode, cancellationToken);
}
