using Clovent.Domain;

namespace Clovent.Catalog.Barcodes.Events;

/// <summary>Raised when a <see cref="Barcode"/>'s primary flag is changed.</summary>
public sealed record BarcodePrimaryChanged(BarcodeId BarcodeId, bool IsPrimary, DateTimeOffset OccurredOnUtc) : IDomainEvent;
