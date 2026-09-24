using Clovent.Restaurant.Application;
using Clovent.Restaurant.QuickOrderTemplates;
using MediatR;

namespace Clovent.Restaurant.Application.QuickOrderTemplates.Commands;

/// <summary>Activates or deactivates a quick-order template.</summary>
public sealed record SetQuickOrderTemplateStatusCommand(Guid TemplateId, bool IsActive) : IRequest;

/// <summary>Handles <see cref="SetQuickOrderTemplateStatusCommand"/>.</summary>
public sealed class SetQuickOrderTemplateStatusCommandHandler(IQuickOrderTemplateRepository repository)
    : IRequestHandler<SetQuickOrderTemplateStatusCommand>
{
    /// <inheritdoc/>
    public async Task Handle(SetQuickOrderTemplateStatusCommand request, CancellationToken cancellationToken)
    {
        var template = await repository.GetByIdAsync(new QuickOrderTemplateId(request.TemplateId), cancellationToken)
            ?? throw new NotFoundException(nameof(QuickOrderTemplate), request.TemplateId);

        template.SetStatus(request.IsActive);
        await repository.UpdateAsync(template, cancellationToken);
    }
}
