using Clovent.Catalog.Barcodes.ValueObjects;
using Clovent.Catalog.Variants;
using Clovent.Domain;

namespace Clovent.Catalog.Barcodes.Events;

/// <summary>Raised when a new <see cref="Barcode"/> is created.</summary>
public sealed record BarcodeCreated(BarcodeId BarcodeId, ProductVariantId ProductVariantId, BarcodeValue Value, DateTimeOffset OccurredOnUtc) : IDomainEvent;
