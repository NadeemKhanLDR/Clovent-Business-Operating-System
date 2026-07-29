using Clovent.MasterData.Currencies;
using Clovent.MasterData.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Clovent.MasterData.Infrastructure.Repositories;

/// <summary>EF Core implementation of <see cref="ICurrencyRepository"/>.</summary>
public sealed class CurrencyRepository(MasterDataDbContext dbContext) : ICurrencyRepository
{
    /// <inheritdoc/>
    public Task<Currency?> GetByIdAsync(CurrencyId id, CancellationToken cancellationToken = default) =>
        dbContext.Currencies.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    /// <inheritdoc/>
    public Task<Currency?> GetByCodeAsync(CurrencyCode code, CancellationToken cancellationToken = default) =>
        dbContext.Currencies.FirstOrDefaultAsync(c => c.Code == code, cancellationToken);

    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<Currency>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Currencies.ToListAsync(cancellationToken);

    /// <inheritdoc/>
    public async Task AddAsync(Currency currency, CancellationToken cancellationToken = default) =>
        await dbContext.Currencies.AddAsync(currency, cancellationToken);
}
