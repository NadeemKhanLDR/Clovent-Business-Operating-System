using Clovent.Domain;

namespace Clovent.MasterData.Currencies.Events;

/// <summary>Raised when a new <see cref="Currency"/> catalog entry is created.</summary>
public sealed record CurrencyCreated(CurrencyId CurrencyId, CurrencyCode Code, DateTimeOffset OccurredOnUtc) : IDomainEvent;
