using System;
using System.Threading;
using System.Threading.Tasks;
using Clovent.Catalog.Application.TaxProfiles.Services;
using Clovent.Catalog.Application.Tests.TestSupport;
using Clovent.Catalog.Products.ValueObjects;
using Clovent.Catalog.TaxProfiles;
using Xunit;

namespace Clovent.Catalog.Application.Tests.TaxProfiles;

public class TaxProfileResolverTests
{
    private readonly FakeTaxProfileRepository _repo = new();

    [Fact]
    public async Task ResolveAsync_ProductHasExplicitProfile_ReturnsDirectProfile()
    {
        var profile = TaxProfile.Create(
            "PK-PRA-SRV-16",
            "Punjab Sales Tax on Services",
            "PRA",
            "Punjab",
            TaxClassification.Taxable,
            ItemTaxClassification.Services,
            16.00m,
            TaxPricingMode.Exclusive);
        await _repo.AddAsync(profile);

        var resolver = new TaxProfileResolver(_repo);
        var taxConfig = TaxConfiguration.Create(16.00m, false, TaxClassification.Taxable, "PK-PRA-SRV-16", "PRA", profile.Id);

        var resolved = await resolver.ResolveAsync(taxConfig, "Punjab", ItemTaxClassification.Services, CancellationToken.None);

        Assert.Equal("PK-PRA-SRV-16", resolved.TaxCode);
        Assert.Equal(16.00m, resolved.RatePercentage);
    }

    [Fact]
    public async Task ResolveAsync_ProductMissingProfile_FallsBackToJurisdictionDefault()
    {
        var defaultProfile = TaxProfile.Create(
            "PK-PRA-DEFAULT",
            "Punjab Services Default",
            "PRA",
            "Punjab",
            TaxClassification.Taxable,
            ItemTaxClassification.Services,
            16.00m,
            TaxPricingMode.Exclusive);
        await _repo.AddAsync(defaultProfile);

        var resolver = new TaxProfileResolver(_repo);
        var taxConfig = TaxConfiguration.Create(0m, false, TaxClassification.Taxable, null, null, null);

        var resolved = await resolver.ResolveAsync(taxConfig, "Punjab", ItemTaxClassification.Services, CancellationToken.None);

        Assert.Equal("PK-PRA-DEFAULT", resolved.TaxCode);
        Assert.Equal(16.00m, resolved.RatePercentage);
    }

    [Fact]
    public async Task ResolveAsync_UnresolvableConfiguration_ThrowsCatalogDomainException()
    {
        var resolver = new TaxProfileResolver(_repo);
        var taxConfig = TaxConfiguration.Create(0m, false, TaxClassification.Taxable, null, null, null);

        // No profiles registered in repo at all
        var ex = await Assert.ThrowsAsync<CatalogDomainException>(() =>
            resolver.ResolveAsync(taxConfig, "Sindh", ItemTaxClassification.Goods, CancellationToken.None));

        Assert.Contains("has no active tax profile configured", ex.Message);
    }

    [Fact]
    public async Task ResolveAsync_MultipleActiveConflictingProfiles_ThrowsCatalogDomainException()
    {
        var profile1 = TaxProfile.Create(
            "PK-PRA-1",
            "Punjab Rule 1",
            "PRA",
            "Punjab",
            TaxClassification.Taxable,
            ItemTaxClassification.Services,
            16.00m,
            TaxPricingMode.Exclusive);

        var profile2 = TaxProfile.Create(
            "PK-PRA-2",
            "Punjab Rule 2",
            "PRA",
            "Punjab",
            TaxClassification.Taxable,
            ItemTaxClassification.Services,
            15.00m,
            TaxPricingMode.Exclusive);

        await _repo.AddAsync(profile1);
        await _repo.AddAsync(profile2);

        var resolver = new TaxProfileResolver(_repo);
        var taxConfig = TaxConfiguration.Create(0m, false, TaxClassification.Taxable, null, null, null);

        var ex = await Assert.ThrowsAsync<CatalogDomainException>(() =>
            resolver.ResolveAsync(taxConfig, "Punjab", ItemTaxClassification.Services, CancellationToken.None));

        Assert.Contains("Conflicting active tax profile detected", ex.Message);
    }

    [Fact]
    public async Task ResolveProfileAsync_ProductInheritsCategoryDefaultTaxProfile()
    {
        var categoryProfile = TaxProfile.Create(
            "PK-PRA-CAT-16",
            "Category Default Tax",
            "PRA",
            "Punjab",
            TaxClassification.Taxable,
            ItemTaxClassification.Services,
            16.00m,
            TaxPricingMode.Exclusive);
        await _repo.AddAsync(categoryProfile);

        var catRepo = new FakeProductCategoryRepository();
        var category = Clovent.Catalog.Categories.ProductCategory.Create(
            Clovent.Catalog.Categories.ValueObjects.ProductCategoryName.Create("Beverages"),
            defaultTaxProfileId: categoryProfile.Id);
        catRepo.Add(category);

        var prodRepo = new FakeProductRepository();
        var product = Clovent.Catalog.Products.Product.Create(
            Clovent.Catalog.Products.ValueObjects.ProductName.Create("Iced Tea"),
            Clovent.Catalog.Shared.ValueObjects.Sku.Create("BEV-TEA-01"),
            Clovent.Catalog.UnitsOfMeasure.UnitOfMeasureId.New(),
            TaxConfiguration.Create(0m, false, TaxClassification.Taxable, null, null, null), // Taxable item with no explicit profile on product
            categoryId: category.Id);
        prodRepo.Add(product);

        var resolver = new TaxProfileResolver(_repo, prodRepo, catRepo);

        var result = await resolver.ResolveProfileAsync(
            product.Id.Value,
            category.Id.Value,
            "Prepared",
            "Punjab",
            DateTimeOffset.UtcNow,
            cancellationToken: CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("PK-PRA-CAT-16", result.TaxCode);
        Assert.Equal(16.00m, result.RatePercentage);
    }

    [Fact]
    public async Task ResolveProfileAsync_ProductExplicitOverride_PreemptsCategoryDefault()
    {
        var categoryProfile = TaxProfile.Create(
            "PK-PRA-CAT-16",
            "Category Default Tax",
            "PRA",
            "Punjab",
            TaxClassification.Taxable,
            ItemTaxClassification.Services,
            16.00m,
            TaxPricingMode.Exclusive);
        await _repo.AddAsync(categoryProfile);

        var productExplicitProfile = TaxProfile.Create(
            "PK-PRA-OVERRIDE-5",
            "Reduced Rate Item Override",
            "PRA",
            "Punjab",
            TaxClassification.Taxable,
            ItemTaxClassification.Services,
            5.00m,
            TaxPricingMode.Inclusive);
        await _repo.AddAsync(productExplicitProfile);

        var catRepo = new FakeProductCategoryRepository();
        var category = Clovent.Catalog.Categories.ProductCategory.Create(
            Clovent.Catalog.Categories.ValueObjects.ProductCategoryName.Create("Specials"),
            defaultTaxProfileId: categoryProfile.Id);
        catRepo.Add(category);

        var prodRepo = new FakeProductRepository();
        var product = Clovent.Catalog.Products.Product.Create(
            Clovent.Catalog.Products.ValueObjects.ProductName.Create("Special Dish"),
            Clovent.Catalog.Shared.ValueObjects.Sku.Create("SPEC-01"),
            Clovent.Catalog.UnitsOfMeasure.UnitOfMeasureId.New(),
            TaxConfiguration.FromProfile(productExplicitProfile), // Explicit override on product
            categoryId: category.Id);
        prodRepo.Add(product);

        var resolver = new TaxProfileResolver(_repo, prodRepo, catRepo);

        var result = await resolver.ResolveProfileAsync(
            product.Id.Value,
            category.Id.Value,
            "Prepared",
            "Punjab",
            DateTimeOffset.UtcNow,
            cancellationToken: CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("PK-PRA-OVERRIDE-5", result.TaxCode);
        Assert.Equal(5.00m, result.RatePercentage);
        Assert.True(result.IsInclusive);
    }
}
