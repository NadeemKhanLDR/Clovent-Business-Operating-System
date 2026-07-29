using Clovent.MasterData.Currencies;

namespace Clovent.MasterData.Application.Currencies.Dtos;

/// <summary>Read-model shape for a <see cref="Currency"/>, safe to cross a process boundary.</summary>
public sealed record CurrencyDto(
    Guid CurrencyId,
    string Code,
    string Name,
    string Symbol,
    int DecimalPlaces,
    string Status,
    DateTimeOffset CreatedAtUtc)
{
    /// <summary>Projects a domain <see cref="Currency"/> into its DTO.</summary>
    public static CurrencyDto FromDomain(Currency currency) => new(
        currency.Id.Value,
        currency.Code.Value,
        currency.Name,
        currency.Symbol,
        currency.DecimalPlaces,
        currency.Status.ToString(),
        currency.CreatedAtUtc);
}
