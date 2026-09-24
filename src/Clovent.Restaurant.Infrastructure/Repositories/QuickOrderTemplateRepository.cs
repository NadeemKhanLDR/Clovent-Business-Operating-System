using Clovent.Restaurant.Infrastructure.Persistence;
using Clovent.Restaurant.QuickOrderTemplates;
using Microsoft.EntityFrameworkCore;

namespace Clovent.Restaurant.Infrastructure.Repositories;

/// <summary>EF Core implementation of <see cref="IQuickOrderTemplateRepository"/>.</summary>
public sealed class QuickOrderTemplateRepository(RestaurantDbContext dbContext) : IQuickOrderTemplateRepository
{
    /// <inheritdoc/>
    public async Task<QuickOrderTemplate?> GetByIdAsync(QuickOrderTemplateId id, CancellationToken cancellationToken = default) =>
        await dbContext.QuickOrderTemplates
            .Include(t => t.Items)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken)
            .ConfigureAwait(false);

    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<QuickOrderTemplate>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await dbContext.QuickOrderTemplates
            .AsNoTracking()
            .Include(t => t.Items)
            .OrderBy(t => t.DisplayOrder)
            .ThenBy(t => t.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<QuickOrderTemplate>> GetActiveAsync(CancellationToken cancellationToken = default) =>
        await dbContext.QuickOrderTemplates
            .AsNoTracking()
            .Include(t => t.Items)
            .Where(t => t.IsActive)
            .OrderBy(t => t.DisplayOrder)
            .ThenBy(t => t.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    /// <inheritdoc/>
    public async Task AddAsync(QuickOrderTemplate template, CancellationToken cancellationToken = default) =>
        await dbContext.QuickOrderTemplates.AddAsync(template, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc/>
    public Task UpdateAsync(QuickOrderTemplate template, CancellationToken cancellationToken = default)
    {
        if (dbContext.Entry(template).State == EntityState.Detached)
        {
            dbContext.QuickOrderTemplates.Attach(template).State = EntityState.Modified;
        }

        return Task.CompletedTask;
    }
}
