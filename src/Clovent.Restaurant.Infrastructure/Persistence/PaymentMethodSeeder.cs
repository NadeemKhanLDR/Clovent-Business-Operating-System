using Clovent.Restaurant.PaymentMethods;
using Clovent.Restaurant.PaymentMethods.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Clovent.Restaurant.Infrastructure.Persistence;

/// <summary>
/// Ensures core required restaurant payment methods ("Cash", "Card", "On Account")
/// exist idempotently across both fresh installations and existing database upgrades.
/// </summary>
public static class PaymentMethodSeeder
{
    /// <summary>
    /// Core payment method names required for standard POS and restaurant operations.
    /// </summary>
    public static readonly IReadOnlyList<string> RequiredCoreMethods = ["Cash", "Card", "On Account"];

    /// <summary>
    /// Checks the database and idempotently inserts any missing core payment methods.
    /// Deduplicates duplicate core methods if present from legacy or multiple imports.
    /// Existing records and custom payment methods are strictly preserved and never overwritten.
    /// </summary>
    /// <param name="dbContext">The target restaurant database context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of new core payment methods seeded.</returns>
    public static async Task<int> EnsureCorePaymentMethodsAsync(
        RestaurantDbContext dbContext,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dbContext);

        var existing = await dbContext.PaymentMethods
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        // Deduplicate any duplicate payment method names if present
        var duplicates = existing
            .GroupBy(m => m.Name.Value, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .SelectMany(g => g.OrderBy(m => m.CreatedAtUtc).Skip(1))
            .ToList();

        var duplicatesRemoved = 0;
        if (duplicates.Count > 0)
        {
            dbContext.PaymentMethods.RemoveRange(duplicates);
            duplicatesRemoved = duplicates.Count;
        }

        var existingNames = existing
            .Except(duplicates)
            .Select(m => m.Name.Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var insertedCount = 0;
        foreach (var requiredName in RequiredCoreMethods)
        {
            if (!existingNames.Contains(requiredName))
            {
                var newMethod = PaymentMethod.Create(PaymentMethodName.Create(requiredName));
                await dbContext.PaymentMethods.AddAsync(newMethod, cancellationToken).ConfigureAwait(false);
                existingNames.Add(requiredName);
                insertedCount++;
            }
        }

        if (insertedCount > 0 || duplicatesRemoved > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return insertedCount;
    }
}
