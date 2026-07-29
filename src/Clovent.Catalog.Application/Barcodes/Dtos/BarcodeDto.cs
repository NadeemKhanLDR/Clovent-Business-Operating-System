using Clovent.Catalog.Barcodes;

namespace Clovent.Catalog.Application.Barcodes.Dtos;

/// <summary>Read-model shape for a <see cref="Barcode"/>, safe to cross a process boundary.</summary>
public sealed record BarcodeDto(Guid BarcodeId, Guid ProductVariantId, string Value, bool IsPrimary, string Status, DateTimeOffset CreatedAtUtc)
{
    /// <summary>Projects a domain <see cref="Barcode"/> into its DTO.</summary>
    public static BarcodeDto FromDomain(Barcode barcode) => new(
        barcode.Id.Value, barcode.ProductVariantId.Value, barcode.Value.Value, barcode.IsPrimary, barcode.Status.ToString(), barcode.CreatedAtUtc);
}
