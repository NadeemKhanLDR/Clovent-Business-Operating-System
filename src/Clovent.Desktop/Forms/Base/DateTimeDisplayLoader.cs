using System;
using System.Linq;
using System.Threading.Tasks;
using Clovent.Identity.Application.Organizations.Queries;
using Clovent.MasterData.Application.Settings.Queries;
using Clovent.MasterData.Application.TimeZones.Queries;
using Clovent.Restaurant.Application.Shifts.Services;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Clovent.Desktop.Forms.Base;

/// <summary>
/// Configures DateTimeDisplay from the organization's business settings.
/// </summary>
public static class DateTimeDisplayLoader
{
    /// <summary>Loads active timezone and date/time format configurations.</summary>
    public static async Task ConfigureAsync(ISender mediator, IServiceProvider? serviceProvider = null)
    {
        try
        {
            var organizations = await mediator.Send(new ListOrganizationsQuery()).ConfigureAwait(false);
            if (organizations.Count == 0)
            {
                var fallbackSettings = CompanyDisplaySettingsStore.Load();
                ApplyConfiguration(TimeZoneInfo.Utc, fallbackSettings.DateFormat, fallbackSettings.TimeFormat, fallbackSettings.QuantityPrecision, serviceProvider);
                return;
            }

            var settings = await mediator.Send(new GetBusinessSettingsByOrganizationQuery(organizations.First().OrganizationId)).ConfigureAwait(false);
            var tz = await mediator.Send(new GetTimeZoneEntryByIdQuery(settings.DefaultTimeZoneId)).ConfigureAwait(false);
            
            TimeZoneInfo? timeZoneInfo = null;
            try
            {
                timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(tz.IanaId);
            }
            catch (TimeZoneNotFoundException)
            {
                string targetId = tz.IanaId;
                if (targetId.Equals("Asia/Karachi", StringComparison.OrdinalIgnoreCase) || 
                    targetId.Equals("Pakistan Standard Time", StringComparison.OrdinalIgnoreCase))
                {
                    try { timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById("Pakistan Standard Time"); } catch {}
                    try { timeZoneInfo ??= TimeZoneInfo.FindSystemTimeZoneById("Asia/Karachi"); } catch {}
                }
                else if (targetId.Equals("America/New_York", StringComparison.OrdinalIgnoreCase) || 
                         targetId.Equals("Eastern Standard Time", StringComparison.OrdinalIgnoreCase))
                {
                    try { timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time"); } catch {}
                    try { timeZoneInfo ??= TimeZoneInfo.FindSystemTimeZoneById("America/New_York"); } catch {}
                }
                else if (targetId.Equals("UTC", StringComparison.OrdinalIgnoreCase) || 
                         targetId.Equals("Coordinated Universal Time", StringComparison.OrdinalIgnoreCase))
                {
                    timeZoneInfo = TimeZoneInfo.Utc;
                }

                if (timeZoneInfo == null)
                {
                    timeZoneInfo = TimeZoneInfo.GetSystemTimeZones().FirstOrDefault(x =>
                        x.Id.Equals(targetId, StringComparison.OrdinalIgnoreCase) ||
                        x.StandardName.Equals(targetId, StringComparison.OrdinalIgnoreCase) ||
                        x.DisplayName.Contains(targetId, StringComparison.OrdinalIgnoreCase))
                        ?? TimeZoneInfo.Utc;
                }
            }

            timeZoneInfo ??= TimeZoneInfo.Utc;
            var companySettings = CompanyDisplaySettingsStore.Load();
            ApplyConfiguration(timeZoneInfo, settings.DateFormat, companySettings.TimeFormat, companySettings.QuantityPrecision, serviceProvider);
        }
        catch (Exception)
        {
            var companySettings = CompanyDisplaySettingsStore.Load();
            ApplyConfiguration(TimeZoneInfo.Utc, "dd-MMM-yyyy", companySettings.TimeFormat, companySettings.QuantityPrecision, serviceProvider);
        }
    }

    private static void ApplyConfiguration(TimeZoneInfo timeZone, string dateFormat, string? timeFormat, int quantityPrecision, IServiceProvider? serviceProvider)
    {
        QuantityDisplay.Configure(quantityPrecision);
        DateTimeDisplay.Configure(timeZone, dateFormat, timeFormat);

        if (serviceProvider != null)
        {
            try
            {
                var dtService = serviceProvider.GetService<IBusinessDateTimeService>();
                dtService?.Configure(timeZone, dateFormat, timeFormat);

                var dateProvider = serviceProvider.GetService<IBusinessDateProvider>() as BusinessDateProvider;
                dateProvider?.SetBusinessTimeZone(timeZone);
            }
            catch
            {
                // Best effort provider synchronization
            }
        }
    }
}
