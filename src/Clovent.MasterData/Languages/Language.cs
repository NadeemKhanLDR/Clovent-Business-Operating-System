using Clovent.Domain;
using Clovent.MasterData.Languages.Events;
using Clovent.MasterData.Shared;

namespace Clovent.MasterData.Languages;

/// <summary>A language catalog entry (e.g. English, Spanish) - reference data shared across every organization.</summary>
public sealed class Language : AggregateRoot<LanguageId>
{
    private const int MaxNameLength = 100;

    /// <summary>The ISO 639-1 code.</summary>
    public LanguageCode Code { get; }

    /// <summary>The language's name in the current UI language (e.g. "Spanish").</summary>
    public string Name { get; private set; }

    /// <summary>The language's name in itself (e.g. "Español").</summary>
    public string NativeName { get; private set; }

    /// <summary>The language's current lifecycle state.</summary>
    public MasterDataStatus Status { get; private set; }

    /// <summary>UTC instant this language was created.</summary>
    public DateTimeOffset CreatedAtUtc { get; }

    /// <summary>Takes every persisted field explicitly so this is the single, unambiguous constructor an EF Core Infrastructure implementation can bind to.</summary>
    private Language(LanguageId id, LanguageCode code, string name, string nativeName, MasterDataStatus status, DateTimeOffset createdAtUtc)
    {
        Id = id;
        Code = code;
        Name = name;
        NativeName = nativeName;
        Status = status;
        CreatedAtUtc = createdAtUtc;
    }

    /// <summary>Creates a new, active language catalog entry.</summary>
    /// <exception cref="ArgumentException"><paramref name="name"/>/<paramref name="nativeName"/> are empty or too long.</exception>
    public static Language Create(LanguageCode code, string name, string nativeName)
    {
        ArgumentNullException.ThrowIfNull(code);
        name = RequireField(name, nameof(name));
        nativeName = RequireField(nativeName, nameof(nativeName));

        var now = DateTimeOffset.UtcNow;
        var language = new Language(LanguageId.New(), code, name, nativeName, MasterDataStatus.Active, now);
        language.AddDomainEvent(new LanguageCreated(language.Id, language.Code, now));
        return language;
    }

    /// <summary>Activates the language.</summary>
    /// <exception cref="MasterDataDomainException">The language is already active.</exception>
    public void Activate()
    {
        if (Status == MasterDataStatus.Active)
            throw MasterDataDomainException.LanguageAlreadyActive(Id);

        Status = MasterDataStatus.Active;
    }

    /// <summary>Deactivates the language.</summary>
    /// <exception cref="MasterDataDomainException">The language is not active.</exception>
    public void Deactivate()
    {
        if (Status != MasterDataStatus.Active)
            throw MasterDataDomainException.LanguageNotActive(Id);

        Status = MasterDataStatus.Inactive;
    }

    private static string RequireField(string value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException($"{fieldName} is required.", fieldName);

        value = value.Trim();

        if (value.Length > MaxNameLength)
            throw new ArgumentException($"{fieldName} cannot exceed {MaxNameLength} characters.", fieldName);

        return value;
    }
}
