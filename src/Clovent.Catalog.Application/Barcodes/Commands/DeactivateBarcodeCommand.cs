using Clovent.Catalog.Application.Barcodes.Dtos;
using Clovent.Catalog.Barcodes;
using MediatR;

namespace Clovent.Catalog.Application.Barcodes.Commands;

/// <summary>Deactivates a barcode.</summary>
public sealed record DeactivateBarcodeCommand(Guid BarcodeId) : IRequest<BarcodeDto>;

/// <summary>Handles <see cref="DeactivateBarcodeCommand"/>.</summary>
public sealed class DeactivateBarcodeCommandHandler(IBarcodeRepository repository) : IRequestHandler<DeactivateBarcodeCommand, BarcodeDto>
{
    /// <inheritdoc/>
    public async Task<BarcodeDto> Handle(DeactivateBarcodeCommand request, CancellationToken cancellationToken)
    {
        var barcode = await repository.GetByIdAsync(new BarcodeId(request.BarcodeId), cancellationToken)
            ?? throw new NotFoundException(nameof(Barcode), request.BarcodeId);

        barcode.Deactivate();
        return BarcodeDto.FromDomain(barcode);
    }
}
