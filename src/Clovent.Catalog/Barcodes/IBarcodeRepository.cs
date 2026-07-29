using Clovent.Catalog.Barcodes.ValueObjects;
using Clovent.Catalog.Variants;

namespace Clovent.Catalog.Barcodes;

/// <summary>Persistence contract for <see cref="Barcode"/> aggregates.</summary>
public interface IBarcodeRepository
{
    /// <summary>Retrieves a barcode by identity, or <see langword="null"/> if none exists.</summary>
    Task<Barcode?> GetByIdAsync(BarcodeId id, CancellationToken cancellationToken = default);

    /// <summary>Retrieves a barcode by its scanned value, or <see langword="null"/> if none exists.</summary>
    Task<Barcode?> GetByValueAsync(BarcodeValue value, CancellationToken cancellationToken = default);

    /// <summary>Retrieves every barcode belonging to a variant.</summary>
    Task<IReadOnlyCollection<Barcode>> GetByProductVariantIdAsync(ProductVariantId productVariantId, CancellationToken cancellationToken = default);

    /// <summary>Retrieves every barcode in the catalog.</summary>
    Task<IReadOnlyCollection<Barcode>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Adds a newly-created barcode.</summary>
    Task AddAsync(Barcode barcode, CancellationToken cancellationToken = default);
}
