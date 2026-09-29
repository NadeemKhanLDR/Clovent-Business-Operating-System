using Clovent.Catalog.Variants;
using MediatR;

namespace Clovent.Catalog.Application.Variants.Commands;

/// <summary>
/// Bulk updates availability (Available vs Sold Out) for multiple product variants in a single transaction.
/// </summary>
public sealed record BulkUpdateProductVariantsAvailabilityCommand(
    IReadOnlyList<Guid> VariantIds,
    bool IsAvailable) : IRequest<int>;

/// <summary>Handles <see cref="BulkUpdateProductVariantsAvailabilityCommand"/>.</summary>
public sealed class BulkUpdateProductVariantsAvailabilityCommandHandler(
    IProductVariantRepository variantRepository) : IRequestHandler<BulkUpdateProductVariantsAvailabilityCommand, int>
{
    /// <inheritdoc/>
    public async Task<int> Handle(BulkUpdateProductVariantsAvailabilityCommand request, CancellationToken cancellationToken)
    {
        if (request.VariantIds.Count == 0)
        {
            return 0;
        }

        int affectedCount = 0;
        foreach (var id in request.VariantIds)
        {
            var variant = await variantRepository.GetByIdAsync(new ProductVariantId(id), cancellationToken);
            if (variant is null) continue;

            if (variant.IsAvailable != request.IsAvailable)
            {
                variant.SetAvailability(request.IsAvailable);
                affectedCount++;
            }
        }

        return affectedCount;
    }
}
