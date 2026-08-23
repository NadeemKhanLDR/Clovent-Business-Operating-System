using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Clovent.MasterData.Languages;
using Clovent.MasterData.Infrastructure.Persistence;
using Clovent.Platform.Bootstrap;

namespace Clovent.Desktop.Seed;

/// <summary>
/// Seeds the default languages catalog (English, Spanish, Urdu) unconditionally.
/// </summary>
public sealed class WorldLanguageSeedStartupTask(
    ILanguageRepository languageRepository,
    MasterDataDbContext masterDataDbContext) : IStartupTask
{
    /// <inheritdoc/>
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var existingLanguages = await languageRepository.GetAllAsync(cancellationToken);
        var existingCodes = existingLanguages.Select(l => l.Code.Value).ToHashSet(StringComparer.OrdinalIgnoreCase);

        bool addedAny = false;

        var languagesToSeed = new (string Code, string Name, string NativeName)[]
        {
            ("en", "English", "English"),
            ("es", "Spanish", "Español"),
            ("fr", "French", "Français"),
            ("ur", "Urdu", "اردو")
        };

        foreach (var (code, name, nativeName) in languagesToSeed)
        {
            if (!existingCodes.Contains(code))
            {
                var lang = Language.Create(LanguageCode.Create(code), name, nativeName);
                await languageRepository.AddAsync(lang, cancellationToken);
                addedAny = true;
            }
        }

        if (addedAny)
        {
            await masterDataDbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
