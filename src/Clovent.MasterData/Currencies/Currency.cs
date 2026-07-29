using Clovent.Domain;
using Clovent.MasterData.Currencies.Events;
using Clovent.MasterData.Shared;

namespace Clovent.MasterData.Currencies;

/// <summary>
/// A currency catalog entry (e.g. USD, EUR) - reference data shared across
/// every organization, not owned by any one of them. <see cref="Code"/> is
/// the stable identifier other aggregates reference
/// (<see cref="Settings.BusinessSettings.DefaultCurrencyId"/>); never
/// computes an exchange rate or performs currency conversion - that is
/// explicitly a future module's concern.
/// </summary>
public sealed class Currency : AggregateRoot<CurrencyId>
{
    private const int MaxNameLength = 100;
    private const int MaxSymbolLength = 10;

    /// <summary>The ISO 4217 code.</summary>
    public CurrencyCode Code { get; }

    /// <summary>The currency's display name (e.g. "US Dollar").</summary>
    public string Name { get; private set; }

    /// <summary>The currency's symbol (e.g. "$").</summary>
    public string Symbol { get; private set; }

    /// <summary>The number of decimal places conventionally used for this currency (e.g. 2 for USD, 0 for JPY).</summary>
    public int DecimalPlaces { get; }

    /// <summary>The currency's current lifecycle state.</summary>
    public MasterDataStatus Status { get; private set; }

    /// <summary>UTC instant this currency was created.</summary>
    public DateTimeOffset CreatedAtUtc { get; }

    /// <summary>Takes every persisted field explicitly so this is the single, unambiguous constructor an EF Core Infrastructure implementation can bind to.</summary>
    private Currency(CurrencyId id, CurrencyCode code, string name, string symbol, int decimalPlaces, MasterDataStatus status, DateTimeOffset createdAtUtc)
    {
        Id = id;
        Code = code;
        Name = name;
        Symbol = symbol;
        DecimalPlaces = decimalPlaces;
        Status = status;
        CreatedAtUtc = createdAtUtc;
    }

    /// <summary>Creates a new, active currency catalog entry.</summary>
    /// <exception cref="ArgumentException"><paramref name="name"/>/<paramref name="symbol"/> are empty or too long.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="decimalPlaces"/> is negative or greater than 4.</exception>
    public static Currency Create(CurrencyCode code, string name, string symbol, int decimalPlaces)
    {
        ArgumentNullException.ThrowIfNull(code);
        name = RequireField(name, nameof(name), MaxNameLength);
        symbol = RequireField(symbol, nameof(symbol), MaxSymbolLength);

        if (decimalPlaces is < 0 or > 4)
            throw new ArgumentOutOfRangeException(nameof(decimalPlaces), decimalPlaces, "Decimal places must be between 0 and 4.");

        var now = DateTimeOffset.UtcNow;
        var currency = new Currency(CurrencyId.New(), code, name, symbol, decimalPlaces, MasterDataStatus.Active, now);
        currency.AddDomainEvent(new CurrencyCreated(currency.Id, currency.Code, now));
        return currency;
    }

    /// <summary>Activates the currency.</summary>
    /// <exception cref="MasterDataDomainException">The currency is already active.</exception>
    public void Activate()
    {
        if (Status == MasterDataStatus.Active)
            throw MasterDataDomainException.CurrencyAlreadyActive(Id);

        Status = MasterDataStatus.Active;
    }

    /// <summary>Deactivates the currency.</summary>
    /// <exception cref="MasterDataDomainException">The currency is not active.</exception>
    public void Deactivate()
    {
        if (Status != MasterDataStatus.Active)
            throw MasterDataDomainException.CurrencyNotActive(Id);

        Status = MasterDataStatus.Inactive;
    }

    private static string RequireField(string value, string fieldName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException($"{fieldName} is required.", fieldName);

        value = value.Trim();

        if (value.Length > maxLength)
            throw new ArgumentException($"{fieldName} cannot exceed {maxLength} characters.", fieldName);

        return value;
    }
}
