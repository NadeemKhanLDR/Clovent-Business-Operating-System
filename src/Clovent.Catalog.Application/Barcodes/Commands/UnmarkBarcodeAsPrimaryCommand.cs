using Clovent.Catalog.Application.Barcodes.Dtos;
using Clovent.Catalog.Barcodes;
using MediatR;

namespace Clovent.Catalog.Application.Barcodes.Commands;

/// <summary>Unmarks a barcode as its variant's primary one.</summary>
public sealed record UnmarkBarcodeAsPrimaryCommand(Guid BarcodeId) : IRequest<BarcodeDto>;

/// <summary>Handles <see cref="UnmarkBarcodeAsPrimaryCommand"/>.</summary>
public sealed class UnmarkBarcodeAsPrimaryCommandHandler(IBarcodeRepository repository)
    : IRequestHandler<UnmarkBarcodeAsPrimaryCommand, BarcodeDto>
{
    /// <inheritdoc/>
    public async Task<BarcodeDto> Handle(UnmarkBarcodeAsPrimaryCommand request, CancellationToken cancellationToken)
    {
        var barcode = await repository.GetByIdAsync(new BarcodeId(request.BarcodeId), cancellationToken)
            ?? throw new NotFoundException(nameof(Barcode), request.BarcodeId);

        barcode.UnmarkAsPrimary();

        return BarcodeDto.FromDomain(barcode);
    }
}
