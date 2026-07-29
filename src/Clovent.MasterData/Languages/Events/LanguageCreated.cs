using Clovent.Domain;

namespace Clovent.MasterData.Languages.Events;

/// <summary>Raised when a new <see cref="Language"/> catalog entry is created.</summary>
public sealed record LanguageCreated(LanguageId LanguageId, LanguageCode Code, DateTimeOffset OccurredOnUtc) : IDomainEvent;
