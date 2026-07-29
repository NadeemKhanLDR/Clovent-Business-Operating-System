using Clovent.Catalog.Application.Barcodes.Dtos;
using Clovent.Catalog.Barcodes;
using MediatR;

namespace Clovent.Catalog.Application.Barcodes.Commands;

/// <summary>
/// Marks a barcode as its variant's primary one, unmarking any other
/// barcode currently marked primary for that same variant - enforcing "at
/// most one primary per variant" here, in the handler, since a
/// <see cref="Barcode"/> aggregate has no visibility into its siblings (see
/// <c>Barcode</c>'s own doc comment).
/// </summary>
public sealed record MarkBarcodeAsPrimaryCommand(Guid BarcodeId) : IRequest<BarcodeDto>;

/// <summary>Handles <see cref="MarkBarcodeAsPrimaryCommand"/>.</summary>
public sealed class MarkBarcodeAsPrimaryCommandHandler(IBarcodeRepository repository)
    : IRequestHandler<MarkBarcodeAsPrimaryCommand, BarcodeDto>
{
    /// <inheritdoc/>
    public async Task<BarcodeDto> Handle(MarkBarcodeAsPrimaryCommand request, CancellationToken cancellationToken)
    {
        var barcode = await repository.GetByIdAsync(new BarcodeId(request.BarcodeId), cancellationToken)
            ?? throw new NotFoundException(nameof(Barcode), request.BarcodeId);

        var siblings = await repository.GetByProductVariantIdAsync(barcode.ProductVariantId, cancellationToken);
        foreach (var sibling in siblings.Where(b => b.Id != barcode.Id && b.IsPrimary))
        {
            sibling.UnmarkAsPrimary();
        }

        barcode.MarkAsPrimary();

        return BarcodeDto.FromDomain(barcode);
    }
}
