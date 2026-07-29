using Clovent.Catalog.Products.ValueObjects;
using Clovent.Domain;

namespace Clovent.Catalog.Products.Events;

/// <summary>Raised when a <see cref="Product"/>'s tax configuration is changed.</summary>
public sealed record ProductTaxConfigurationChanged(ProductId ProductId, TaxConfiguration TaxConfiguration, DateTimeOffset OccurredOnUtc) : IDomainEvent;
