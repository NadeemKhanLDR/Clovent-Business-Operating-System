using Clovent.Identity.Application.Organizations.Queries;
using Clovent.MasterData.Application.Currencies.Queries;
using Clovent.MasterData.Application.Settings.Queries;
using MediatR;

namespace Clovent.Desktop.Forms.Base;

/// <summary>
/// Configures <see cref="CurrencyDisplay"/> from the organization's
/// <c>BusinessSettings.DefaultCurrencyId</c> (maintained in the Back Office
/// Business Settings screen), falling back to the first catalog currency when
/// no settings record exists yet. Centralized here so every screen that
/// formats money (POS, receipts, End-of-Day, Menu Items) applies the same
/// configured currency instead of each re-picking "the first currency".
/// </summary>
public static class CurrencyDisplayLoader
{
    /// <summary>Sets the process-wide currency from the organization's configured default.</summary>
    public static async Task ConfigureAsync(ISender mediator)
    {
        try
        {
            var currencies = await mediator.Send(new ListCurrenciesQuery()).ConfigureAwait(false);

            var preferredCurrencyId = await TryGetDefaultCurrencyIdAsync(mediator).ConfigureAwait(false);
            var currency = preferredCurrencyId is { } id
                ? currencies.FirstOrDefault(c => c.CurrencyId == id)
                : null;

            currency ??= currencies.FirstOrDefault();
            if (currency is not null)
            {
                CurrencyDisplay.Configure(currency.Code, currency.Symbol, currency.DecimalPlaces);
            }
            else
            {
                CurrencyDisplay.Configure("PKR", "Rs.", 2);
            }
        }
        catch
        {
            // Resilient fallback: ensure currency display is always initialized even if query fails
            CurrencyDisplay.Configure("PKR", "Rs.", 2);
        }
    }

    private static async Task<Guid?> TryGetDefaultCurrencyIdAsync(ISender mediator)
    {
        try
        {
            var organizations = await mediator.Send(new ListOrganizationsQuery()).ConfigureAwait(false);
            if (organizations.Count == 0)
            {
                return null;
            }

            var settings = await mediator.Send(new GetBusinessSettingsByOrganizationQuery(organizations.First().OrganizationId)).ConfigureAwait(false);
            return settings.DefaultCurrencyId;
        }
        catch (Exception)
        {
            // Business settings record absent or query failed - fall back to the first currency.
            return null;
        }
    }
}
