using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Clovent.MasterData.Infrastructure.Persistence;
using Clovent.MasterData.TimeZones;
using Clovent.Platform.Bootstrap;

namespace Clovent.Desktop.Seed;

/// <summary>
/// Seeds all available system time zones from .NET TimeZoneInfo into the database.
/// Run unconditionally at startup to guarantee Windows and IANA system timezone listings are always available.
/// </summary>
public sealed class WorldTimeZoneSeedStartupTask(
    ITimeZoneRepository timeZoneRepository,
    MasterDataDbContext masterDataDbContext) : IStartupTask
{
    /// <inheritdoc/>
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var existingZones = await timeZoneRepository.GetAllAsync(cancellationToken);
        var existingIds = existingZones.Select(z => z.IanaId.Value).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var systemZones = TimeZoneInfo.GetSystemTimeZones();
        bool addedAny = false;

        foreach (var zone in systemZones)
        {
            if (!existingIds.Contains(zone.Id))
            {
                var offsetMinutes = (int)zone.BaseUtcOffset.TotalMinutes;
                var displayName = zone.DisplayName;
                if (displayName.Length > 100)
                {
                    displayName = displayName.Substring(0, 100);
                }

                var entry = TimeZoneEntry.Create(IanaId.Create(zone.Id), displayName, offsetMinutes);
                await timeZoneRepository.AddAsync(entry, cancellationToken);
                addedAny = true;
            }
        }

        if (addedAny)
        {
            await masterDataDbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
