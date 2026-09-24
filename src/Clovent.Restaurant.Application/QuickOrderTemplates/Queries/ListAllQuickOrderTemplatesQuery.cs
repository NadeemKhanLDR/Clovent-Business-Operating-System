using Clovent.Restaurant.Application.QuickOrderTemplates.Dtos;
using Clovent.Restaurant.QuickOrderTemplates;
using MediatR;

namespace Clovent.Restaurant.Application.QuickOrderTemplates.Queries;

/// <summary>
/// Retrieves every quick-order template (active and inactive) for the Back
/// Office management grid, ordered by display order, with each item expanded
/// through the same catalog read model as
/// <see cref="ListActiveQuickOrderTemplatesQuery"/>.
/// </summary>
public sealed record ListAllQuickOrderTemplatesQuery : IRequest<IReadOnlyCollection<QuickOrderTemplateDto>>;

/// <summary>Handles <see cref="ListAllQuickOrderTemplatesQuery"/>.</summary>
public sealed class ListAllQuickOrderTemplatesQueryHandler(
    IQuickOrderTemplateRepository templateRepository,
    IMediator mediator) : IRequestHandler<ListAllQuickOrderTemplatesQuery, IReadOnlyCollection<QuickOrderTemplateDto>>
{
    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<QuickOrderTemplateDto>> Handle(ListAllQuickOrderTemplatesQuery request, CancellationToken cancellationToken)
    {
        var templates = await templateRepository.GetAllAsync(cancellationToken);
        if (templates.Count == 0)
        {
            return [];
        }

        var lookup = await QuickOrderTemplateProjection.CreateAsync(mediator, cancellationToken);
        return [.. templates
            .OrderBy(t => t.DisplayOrder)
            .ThenBy(t => t.Name, StringComparer.Ordinal)
            .Select(lookup.Expand)];
    }
}
