using Clovent.Catalog.Application.Barcodes.Dtos;
using Clovent.Catalog.Barcodes;
using Clovent.Catalog.Barcodes.ValueObjects;
using Clovent.Catalog.Variants;
using MediatR;

namespace Clovent.Catalog.Application.Barcodes.Commands;

/// <summary>Creates a new barcode for an existing product variant.</summary>
public sealed record CreateBarcodeCommand(Guid ProductVariantId, string Value, bool IsPrimary = false) : IRequest<BarcodeDto>;

/// <summary>Handles <see cref="CreateBarcodeCommand"/>.</summary>
public sealed class CreateBarcodeCommandHandler(IBarcodeRepository repository) : IRequestHandler<CreateBarcodeCommand, BarcodeDto>
{
    /// <inheritdoc/>
    public async Task<BarcodeDto> Handle(CreateBarcodeCommand request, CancellationToken cancellationToken)
    {
        var variantId = new ProductVariantId(request.ProductVariantId);

        if (request.IsPrimary)
        {
            await UnmarkExistingPrimaryAsync(variantId, cancellationToken);
        }

        var barcode = Barcode.Create(variantId, BarcodeValue.Create(request.Value), request.IsPrimary);
        await repository.AddAsync(barcode, cancellationToken);

        return BarcodeDto.FromDomain(barcode);
    }

    private async Task UnmarkExistingPrimaryAsync(ProductVariantId variantId, CancellationToken cancellationToken)
    {
        var siblings = await repository.GetByProductVariantIdAsync(variantId, cancellationToken);
        foreach (var sibling in siblings.Where(b => b.IsPrimary))
        {
            sibling.UnmarkAsPrimary();
        }
    }
}
