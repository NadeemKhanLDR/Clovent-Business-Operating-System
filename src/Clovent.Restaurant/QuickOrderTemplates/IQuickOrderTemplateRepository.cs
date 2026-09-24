namespace Clovent.Restaurant.QuickOrderTemplates;

/// <summary>Persistence contract for <see cref="QuickOrderTemplate"/> aggregates (including their <see cref="QuickOrderTemplateItem"/> children).</summary>
public interface IQuickOrderTemplateRepository
{
    /// <summary>Retrieves a template by identity (with its items), or <see langword="null"/> if none exists.</summary>
    Task<QuickOrderTemplate?> GetByIdAsync(QuickOrderTemplateId id, CancellationToken cancellationToken = default);

    /// <summary>Retrieves every template (with its items), regardless of status.</summary>
    Task<IReadOnlyCollection<QuickOrderTemplate>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Retrieves every currently-active template (with its items).</summary>
    Task<IReadOnlyCollection<QuickOrderTemplate>> GetActiveAsync(CancellationToken cancellationToken = default);

    /// <summary>Adds a newly-created template.</summary>
    Task AddAsync(QuickOrderTemplate template, CancellationToken cancellationToken = default);

    /// <summary>Updates an existing template.</summary>
    Task UpdateAsync(QuickOrderTemplate template, CancellationToken cancellationToken = default);
}
