using Clovent.Restaurant.QuickOrderTemplates;

namespace Clovent.Restaurant.Application.Tests.TestSupport;

internal sealed class FakeQuickOrderTemplateRepository : IQuickOrderTemplateRepository
{
    private readonly Dictionary<QuickOrderTemplateId, QuickOrderTemplate> _templates = [];

    public void Add(QuickOrderTemplate template) => _templates[template.Id] = template;

    public Task<QuickOrderTemplate?> GetByIdAsync(QuickOrderTemplateId id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_templates.GetValueOrDefault(id));

    public Task<IReadOnlyCollection<QuickOrderTemplate>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyCollection<QuickOrderTemplate>>([.. _templates.Values]);

    public Task<IReadOnlyCollection<QuickOrderTemplate>> GetActiveAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyCollection<QuickOrderTemplate>>([.. _templates.Values.Where(t => t.IsActive)]);

    public Task AddAsync(QuickOrderTemplate template, CancellationToken cancellationToken = default)
    {
        _templates[template.Id] = template;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(QuickOrderTemplate template, CancellationToken cancellationToken = default)
    {
        _templates[template.Id] = template;
        return Task.CompletedTask;
    }
}
