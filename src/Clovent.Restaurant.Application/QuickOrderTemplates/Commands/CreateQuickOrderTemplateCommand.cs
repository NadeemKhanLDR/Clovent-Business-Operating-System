using Clovent.Catalog.Variants;
using Clovent.Restaurant.Application.QuickOrderTemplates.Dtos;
using Clovent.Restaurant.QuickOrderTemplates;
using MediatR;

/// <summary>One line of a template being created or updated.</summary>
public sealed record QuickOrderTemplateItemInput(
    Guid VariantId,
    decimal Quantity,
    decimal? TemplateUnitPrice = null);

/// <summary>Creates a new quick-order template with its items.</summary>
public sealed record CreateQuickOrderTemplateCommand(
    string Name,
    string? Description,
    int DisplayOrder,
    IReadOnlyCollection<QuickOrderTemplateItemInput> Items, Guid? WarehouseId = null) : IRequest<Guid>;

/// <summary>Handles <see cref="CreateQuickOrderTemplateCommand"/>.</summary>
public sealed class CreateQuickOrderTemplateCommandHandler(IQuickOrderTemplateRepository repository)
    : IRequestHandler<CreateQuickOrderTemplateCommand, Guid>
{
    /// <inheritdoc/>
    public async Task<Guid> Handle(CreateQuickOrderTemplateCommand request, CancellationToken cancellationToken)
    {
        var template = QuickOrderTemplate.Create(request.Name, request.Description, request.DisplayOrder);
        template.ScopeToWarehouse(request.WarehouseId);

        foreach (var item in request.Items)
        {
            template.AddItem(new ProductVariantId(item.VariantId), item.Quantity, item.TemplateUnitPrice);
        }

        await repository.AddAsync(template, cancellationToken);
        return template.Id.Value;
    }
}

