using Clovent.Catalog.Application.Barcodes.Dtos;
using Clovent.Catalog.Barcodes;
using MediatR;

namespace Clovent.Catalog.Application.Barcodes.Queries;

/// <summary>Retrieves a single barcode by identity.</summary>
public sealed record GetBarcodeByIdQuery(Guid BarcodeId) : IRequest<BarcodeDto>;

/// <summary>Handles <see cref="GetBarcodeByIdQuery"/>.</summary>
public sealed class GetBarcodeByIdQueryHandler(IBarcodeRepository repository) : IRequestHandler<GetBarcodeByIdQuery, BarcodeDto>
{
    /// <inheritdoc/>
    public async Task<BarcodeDto> Handle(GetBarcodeByIdQuery request, CancellationToken cancellationToken)
    {
        var barcode = await repository.GetByIdAsync(new BarcodeId(request.BarcodeId), cancellationToken)
            ?? throw new NotFoundException(nameof(Barcode), request.BarcodeId);

        return BarcodeDto.FromDomain(barcode);
    }
}
