using Clovent.MasterData.Infrastructure.Persistence;
using Clovent.MasterData.Languages;
using Microsoft.EntityFrameworkCore;

namespace Clovent.MasterData.Infrastructure.Repositories;

/// <summary>EF Core implementation of <see cref="ILanguageRepository"/>.</summary>
public sealed class LanguageRepository(MasterDataDbContext dbContext) : ILanguageRepository
{
    /// <inheritdoc/>
    public Task<Language?> GetByIdAsync(LanguageId id, CancellationToken cancellationToken = default) =>
        dbContext.Languages.FirstOrDefaultAsync(l => l.Id == id, cancellationToken);

    /// <inheritdoc/>
    public Task<Language?> GetByCodeAsync(LanguageCode code, CancellationToken cancellationToken = default) =>
        dbContext.Languages.FirstOrDefaultAsync(l => l.Code == code, cancellationToken);

    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<Language>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Languages.ToListAsync(cancellationToken);

    /// <inheritdoc/>
    public async Task AddAsync(Language language, CancellationToken cancellationToken = default) =>
        await dbContext.Languages.AddAsync(language, cancellationToken);
}
