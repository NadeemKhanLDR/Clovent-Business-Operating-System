using Clovent.Catalog.Variants;
using Clovent.Restaurant.Application;
using Clovent.Restaurant.QuickOrderTemplates;
using MediatR;

namespace Clovent.Restaurant.Application.QuickOrderTemplates.Commands;

/// <summary>Updates a quick-order template's header and replaces its item list wholesale.</summary>
public sealed record UpdateQuickOrderTemplateCommand(
    Guid TemplateId,
    string Name,
    string? Description,
    int DisplayOrder,
    IReadOnlyCollection<QuickOrderTemplateItemInput> Items) : IRequest;

/// <summary>Handles <see cref="UpdateQuickOrderTemplateCommand"/>.</summary>
public sealed class UpdateQuickOrderTemplateCommandHandler(IQuickOrderTemplateRepository repository)
    : IRequestHandler<UpdateQuickOrderTemplateCommand>
{
    /// <inheritdoc/>
    public async Task Handle(UpdateQuickOrderTemplateCommand request, CancellationToken cancellationToken)
    {
        var template = await repository.GetByIdAsync(new QuickOrderTemplateId(request.TemplateId), cancellationToken)
            ?? throw new NotFoundException(nameof(QuickOrderTemplate), request.TemplateId);

        template.Update(request.Name, request.Description, request.DisplayOrder);

        foreach (var existing in template.Items.ToList())
        {
            template.RemoveItem(existing.VariantId);
        }

        foreach (var item in request.Items)
        {
            template.AddItem(new ProductVariantId(item.VariantId), item.Quantity, item.TemplateUnitPrice);
        }

        await repository.UpdateAsync(template, cancellationToken);
    }
}
