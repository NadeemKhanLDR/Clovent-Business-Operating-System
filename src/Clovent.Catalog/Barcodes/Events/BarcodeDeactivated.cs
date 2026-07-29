using Clovent.Domain;

namespace Clovent.Catalog.Barcodes.Events;

/// <summary>Raised when a <see cref="Barcode"/> is deactivated.</summary>
public sealed record BarcodeDeactivated(BarcodeId BarcodeId, DateTimeOffset OccurredOnUtc) : IDomainEvent;
