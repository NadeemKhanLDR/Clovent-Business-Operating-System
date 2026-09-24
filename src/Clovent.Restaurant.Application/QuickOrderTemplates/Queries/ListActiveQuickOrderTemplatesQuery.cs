using Clovent.Restaurant.Application.QuickOrderTemplates.Dtos;
using Clovent.Restaurant.QuickOrderTemplates;
using MediatR;

namespace Clovent.Restaurant.Application.QuickOrderTemplates.Queries;

/// <summary>
/// Retrieves every active quick-order template for the POS quick-order bar,
/// ordered by display order, with each item's unit price resolved to its
/// <c>TemplateUnitPrice</c> override when set and otherwise to the variant's
/// current catalog selling price - the same price read model the POS product
/// wall resolves through.
/// </summary>
public sealed record ListActiveQuickOrderTemplatesQuery(Guid? WarehouseId = null) : IRequest<IReadOnlyCollection<QuickOrderTemplateDto>>;

/// <summary>Handles <see cref="ListActiveQuickOrderTemplatesQuery"/>.</summary>
public sealed class ListActiveQuickOrderTemplatesQueryHandler(
    IQuickOrderTemplateRepository templateRepository,
    IMediator mediator) : IRequestHandler<ListActiveQuickOrderTemplatesQuery, IReadOnlyCollection<QuickOrderTemplateDto>>
{
    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<QuickOrderTemplateDto>> Handle(ListActiveQuickOrderTemplatesQuery request, CancellationToken cancellationToken)
    {
        var templates = await templateRepository.GetActiveAsync(cancellationToken);
        if (templates.Count == 0)
        {
            return [];
        }

        var lookup = await QuickOrderTemplateProjection.CreateAsync(mediator, cancellationToken);
        return [.. templates
            .Where(t => t.WarehouseId == null || t.WarehouseId == request.WarehouseId)
            .OrderBy(t => t.DisplayOrder)
            .ThenBy(t => t.Name, StringComparer.Ordinal)
            .Select(lookup.Expand)];
    }
}

