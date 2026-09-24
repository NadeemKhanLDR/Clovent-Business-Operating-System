using Clovent.Catalog.Variants;
using Clovent.Restaurant.SmartRecommendations;
using MediatR;

namespace Clovent.Restaurant.Application.SmartRecommendations.Commands;

/// <summary>
/// Appends one suggestion analytics fact from the POS. Fire-and-forget by
/// design - the caller never awaits it as part of checkout.
/// </summary>
public sealed record RecordSuggestionEventCommand(
    Guid OrderId,
    Guid VariantId,
    Guid? TriggerVariantId,
    SuggestionEventKind Kind,
    Guid? OrderLineId = null,
    decimal AcceptedQuantity = 0m,
    decimal AcceptedUnitAmount = 0m) : IRequest;

/// <summary>Handles <see cref="RecordSuggestionEventCommand"/>.</summary>
public sealed class RecordSuggestionEventCommandHandler(ISuggestionEventRepository repository)
    : IRequestHandler<RecordSuggestionEventCommand>
{
    /// <inheritdoc/>
    public Task Handle(RecordSuggestionEventCommand request, CancellationToken cancellationToken)
    {
        SuggestionEvent suggestionEvent = request.Kind switch
        {
            SuggestionEventKind.Offered => SuggestionEvent.Offered(request.OrderId, new ProductVariantId(request.VariantId), request.TriggerVariantId),
            SuggestionEventKind.Accepted => SuggestionEvent.Accepted(request.OrderId, new ProductVariantId(request.VariantId), request.TriggerVariantId, request.OrderLineId, request.AcceptedQuantity, request.AcceptedUnitAmount),
            SuggestionEventKind.Dismissed => SuggestionEvent.Dismissed(request.OrderId, new ProductVariantId(request.VariantId), request.TriggerVariantId),
            _ => throw new ArgumentOutOfRangeException(nameof(request.Kind), request.Kind, "Unknown suggestion event kind.")
        };

        return repository.AddAsync(suggestionEvent, cancellationToken);
    }
}
