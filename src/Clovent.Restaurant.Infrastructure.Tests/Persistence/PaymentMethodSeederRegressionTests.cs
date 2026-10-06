using Clovent.Restaurant.Infrastructure.Persistence;
using Clovent.Restaurant.Infrastructure.Tests.TestSupport;
using Clovent.Restaurant.PaymentMethods;
using Clovent.Restaurant.PaymentMethods.ValueObjects;
using Clovent.Restaurant.Shared;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Clovent.Restaurant.Infrastructure.Tests.Persistence;

/// <summary>
/// Regression tests verifying the payment-method seeding and upgrade behavior
/// for fresh installations, existing client upgrades, and idempotency guarantees.
/// </summary>
public class PaymentMethodSeederRegressionTests : SqliteTestBase
{
    [Fact]
    public async Task OldDatabase_MissingCashAndCard_BecomesUsableAfterUpgrade()
    {
        // 1. Arrange: Simulate an existing database created by an earlier release (contains only "On Account")
        await using (var writeContext = CreateContext())
        {
            var onAccount = PaymentMethod.Create(PaymentMethodName.Create("On Account"));
            await writeContext.PaymentMethods.AddAsync(onAccount);
            await writeContext.SaveChangesAsync();
        }

        // Verify pre-condition: Only "On Account" exists, Cash and Card are missing
        await using (var verifyPreContext = CreateContext())
        {
            var initialMethods = await verifyPreContext.PaymentMethods.ToListAsync();
            Assert.Single(initialMethods);
            Assert.Equal("On Account", initialMethods[0].Name.Value);
            Assert.DoesNotContain(initialMethods, m => string.Equals(m.Name.Value, "Cash", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(initialMethods, m => string.Equals(m.Name.Value, "Card", StringComparison.OrdinalIgnoreCase));
        }

        // 2. Act: Perform upgrade via PaymentMethodSeeder
        await using (var upgradeContext = CreateContext())
        {
            var inserted = await PaymentMethodSeeder.EnsureCorePaymentMethodsAsync(upgradeContext);
            Assert.Equal(2, inserted); // Cash and Card should be inserted
        }

        // 3. Assert: All three core methods now exist and are Active
        await using (var verifyPostContext = CreateContext())
        {
            var upgradedMethods = await verifyPostContext.PaymentMethods.ToListAsync();
            Assert.Equal(3, upgradedMethods.Count);

            var cash = upgradedMethods.FirstOrDefault(m => string.Equals(m.Name.Value, "Cash", StringComparison.OrdinalIgnoreCase));
            var card = upgradedMethods.FirstOrDefault(m => string.Equals(m.Name.Value, "Card", StringComparison.OrdinalIgnoreCase));
            var onAccount = upgradedMethods.FirstOrDefault(m => string.Equals(m.Name.Value, "On Account", StringComparison.OrdinalIgnoreCase));

            Assert.NotNull(cash);
            Assert.Equal(RestaurantStatus.Active, cash.Status);

            Assert.NotNull(card);
            Assert.Equal(RestaurantStatus.Active, card.Status);

            Assert.NotNull(onAccount);
            Assert.Equal(RestaurantStatus.Active, onAccount.Status);
        }
    }

    [Fact]
    public async Task ExistingCashAndCard_AreStrictlyPreserved()
    {
        // 1. Arrange: Pre-populate existing Cash, Card, and custom payment method
        var existingCash = PaymentMethod.Create(PaymentMethodName.Create("Cash"));
        var existingCard = PaymentMethod.Create(PaymentMethodName.Create("Card"));
        var customMethod = PaymentMethod.Create(PaymentMethodName.Create("JazzCash"));

        await using (var writeContext = CreateContext())
        {
            await writeContext.PaymentMethods.AddRangeAsync(existingCash, existingCard, customMethod);
            await writeContext.SaveChangesAsync();
        }

        // 2. Act: Run upgrade seeder
        await using (var upgradeContext = CreateContext())
        {
            var inserted = await PaymentMethodSeeder.EnsureCorePaymentMethodsAsync(upgradeContext);
            Assert.Equal(1, inserted); // Only "On Account" should be newly inserted
        }

        // 3. Assert: Existing records are preserved with identical IDs and timestamps
        await using (var readContext = CreateContext())
        {
            var all = await readContext.PaymentMethods.ToListAsync();
            Assert.Equal(4, all.Count);

            var reloadedCash = all.First(m => m.Id == existingCash.Id);
            Assert.Equal("Cash", reloadedCash.Name.Value);
            Assert.Equal(existingCash.CreatedAtUtc, reloadedCash.CreatedAtUtc);

            var reloadedCard = all.First(m => m.Id == existingCard.Id);
            Assert.Equal("Card", reloadedCard.Name.Value);
            Assert.Equal(existingCard.CreatedAtUtc, reloadedCard.CreatedAtUtc);

            var reloadedCustom = all.First(m => m.Id == customMethod.Id);
            Assert.Equal("JazzCash", reloadedCustom.Name.Value);
        }
    }

    [Fact]
    public async Task RerunningUpgrade_CreatesNoDuplicates()
    {
        // 1. Arrange: Empty DB, seed once
        await using (var context1 = CreateContext())
        {
            await PaymentMethodSeeder.EnsureCorePaymentMethodsAsync(context1);
        }

        // 2. Act: Rerun upgrade multiple times
        await using (var context2 = CreateContext())
        {
            var inserted2 = await PaymentMethodSeeder.EnsureCorePaymentMethodsAsync(context2);
            Assert.Equal(0, inserted2);
        }

        await using (var context3 = CreateContext())
        {
            var inserted3 = await PaymentMethodSeeder.EnsureCorePaymentMethodsAsync(context3);
            Assert.Equal(0, inserted3);
        }

        // 3. Assert: Exactly 3 records remain
        await using (var readContext = CreateContext())
        {
            var all = await readContext.PaymentMethods.ToListAsync();
            Assert.Equal(3, all.Count);

            Assert.Single(all, m => string.Equals(m.Name.Value, "Cash", StringComparison.OrdinalIgnoreCase));
            Assert.Single(all, m => string.Equals(m.Name.Value, "Card", StringComparison.OrdinalIgnoreCase));
            Assert.Single(all, m => string.Equals(m.Name.Value, "On Account", StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact]
    public async Task ExistingDuplicates_AreCleanedUpToSingleActiveRecord()
    {
        // 1. Arrange: Pre-populate duplicate Cash and On Account records
        var cash1 = PaymentMethod.Create(PaymentMethodName.Create("Cash"));
        var cash2 = PaymentMethod.Create(PaymentMethodName.Create("Cash"));
        var onAccount1 = PaymentMethod.Create(PaymentMethodName.Create("On Account"));
        var onAccount2 = PaymentMethod.Create(PaymentMethodName.Create("On Account"));
        var card = PaymentMethod.Create(PaymentMethodName.Create("Card"));

        await using (var writeContext = CreateContext())
        {
            await writeContext.PaymentMethods.AddRangeAsync(cash1, cash2, onAccount1, onAccount2, card);
            await writeContext.SaveChangesAsync();
        }

        // 2. Act: Run seeder
        await using (var upgradeContext = CreateContext())
        {
            await PaymentMethodSeeder.EnsureCorePaymentMethodsAsync(upgradeContext);
        }

        // 3. Assert: Exactly 3 records remain, exactly one of each
        await using (var readContext = CreateContext())
        {
            var all = await readContext.PaymentMethods.ToListAsync();
            Assert.Equal(3, all.Count);

            Assert.Single(all, m => string.Equals(m.Name.Value, "Cash", StringComparison.OrdinalIgnoreCase));
            Assert.Single(all, m => string.Equals(m.Name.Value, "Card", StringComparison.OrdinalIgnoreCase));
            Assert.Single(all, m => string.Equals(m.Name.Value, "On Account", StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact]
    public async Task FreshInstallation_ReceivesAllRequiredCorePaymentMethods()
    {
        // 1. Arrange: Fresh database with 0 payment methods
        await using (var checkContext = CreateContext())
        {
            var count = await checkContext.PaymentMethods.CountAsync();
            Assert.Equal(0, count);
        }

        // 2. Act: Run seeder (as executed during First-Run Commissioning)
        await using (var seedContext = CreateContext())
        {
            var inserted = await PaymentMethodSeeder.EnsureCorePaymentMethodsAsync(seedContext);
            Assert.Equal(3, inserted);
        }

        // 3. Assert: All 3 core methods are populated
        await using (var verifyContext = CreateContext())
        {
            var all = await verifyContext.PaymentMethods.ToListAsync();
            Assert.Equal(3, all.Count);

            var names = all.Select(m => m.Name.Value).ToList();
            Assert.Contains("Cash", names);
            Assert.Contains("Card", names);
            Assert.Contains("On Account", names);

            Assert.All(all, m => Assert.Equal(RestaurantStatus.Active, m.Status));
        }
    }

    [Fact]
    public async Task RestaurantPersistenceInitializer_ExecutesIdempotently()
    {
        // 1. Arrange & Act: Run RestaurantPersistenceInitializer against context
        await using (var initContext = CreateContext())
        {
            var initializer = new RestaurantPersistenceInitializer(initContext);
            await initializer.InitializeAsync();
        }

        // 2. Assert: Core payment methods exist
        await using (var readContext = CreateContext())
        {
            var all = await readContext.PaymentMethods.ToListAsync();
            Assert.Equal(3, all.Count);
        }

        // 3. Act again: Run initializer a second time
        await using (var reinitContext = CreateContext())
        {
            var initializer = new RestaurantPersistenceInitializer(reinitContext);
            await initializer.InitializeAsync();
        }

        // 4. Assert: Still exactly 3 payment methods
        await using (var finalContext = CreateContext())
        {
            var all = await finalContext.PaymentMethods.ToListAsync();
            Assert.Equal(3, all.Count);
        }
    }
}
