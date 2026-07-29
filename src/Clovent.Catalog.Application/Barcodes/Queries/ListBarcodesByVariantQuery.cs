using Clovent.Catalog.Application.Barcodes.Dtos;
using Clovent.Catalog.Barcodes;
using Clovent.Catalog.Variants;
using MediatR;

namespace Clovent.Catalog.Application.Barcodes.Queries;

/// <summary>Retrieves every barcode belonging to a variant.</summary>
public sealed record ListBarcodesByVariantQuery(Guid ProductVariantId) : IRequest<IReadOnlyCollection<BarcodeDto>>;

/// <summary>Handles <see cref="ListBarcodesByVariantQuery"/>.</summary>
public sealed class ListBarcodesByVariantQueryHandler(IBarcodeRepository repository)
    : IRequestHandler<ListBarcodesByVariantQuery, IReadOnlyCollection<BarcodeDto>>
{
    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<BarcodeDto>> Handle(ListBarcodesByVariantQuery request, CancellationToken cancellationToken)
    {
        var barcodes = await repository.GetByProductVariantIdAsync(new ProductVariantId(request.ProductVariantId), cancellationToken);
        return [.. barcodes.Select(BarcodeDto.FromDomain)];
    }
}
