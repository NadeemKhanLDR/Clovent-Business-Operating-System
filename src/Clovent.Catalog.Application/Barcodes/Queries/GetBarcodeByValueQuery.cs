using Clovent.Catalog.Application.Barcodes.Dtos;
using Clovent.Catalog.Barcodes;
using Clovent.Catalog.Barcodes.ValueObjects;
using MediatR;

namespace Clovent.Catalog.Application.Barcodes.Queries;

/// <summary>Retrieves a single barcode by its scanned value.</summary>
public sealed record GetBarcodeByValueQuery(string Value) : IRequest<BarcodeDto>;

/// <summary>Handles <see cref="GetBarcodeByValueQuery"/>.</summary>
public sealed class GetBarcodeByValueQueryHandler(IBarcodeRepository repository) : IRequestHandler<GetBarcodeByValueQuery, BarcodeDto>
{
    /// <inheritdoc/>
    public async Task<BarcodeDto> Handle(GetBarcodeByValueQuery request, CancellationToken cancellationToken)
    {
        var barcode = await repository.GetByValueAsync(BarcodeValue.Create(request.Value), cancellationToken)
            ?? throw new NotFoundException(nameof(Barcode), request.Value);

        return BarcodeDto.FromDomain(barcode);
    }
}
