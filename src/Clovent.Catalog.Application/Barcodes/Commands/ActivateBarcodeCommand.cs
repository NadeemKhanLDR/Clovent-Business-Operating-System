using Clovent.Catalog.Application.Barcodes.Dtos;
using Clovent.Catalog.Barcodes;
using MediatR;

namespace Clovent.Catalog.Application.Barcodes.Commands;

/// <summary>Activates a barcode.</summary>
public sealed record ActivateBarcodeCommand(Guid BarcodeId) : IRequest<BarcodeDto>;

/// <summary>Handles <see cref="ActivateBarcodeCommand"/>.</summary>
public sealed class ActivateBarcodeCommandHandler(IBarcodeRepository repository) : IRequestHandler<ActivateBarcodeCommand, BarcodeDto>
{
    /// <inheritdoc/>
    public async Task<BarcodeDto> Handle(ActivateBarcodeCommand request, CancellationToken cancellationToken)
    {
        var barcode = await repository.GetByIdAsync(new BarcodeId(request.BarcodeId), cancellationToken)
            ?? throw new NotFoundException(nameof(Barcode), request.BarcodeId);

        barcode.Activate();
        return BarcodeDto.FromDomain(barcode);
    }
}
