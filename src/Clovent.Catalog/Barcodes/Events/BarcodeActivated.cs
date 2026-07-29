using Clovent.Domain;

namespace Clovent.Catalog.Barcodes.Events;

/// <summary>Raised when a <see cref="Barcode"/> is (re)activated.</summary>
public sealed record BarcodeActivated(BarcodeId BarcodeId, DateTimeOffset OccurredOnUtc) : IDomainEvent;
