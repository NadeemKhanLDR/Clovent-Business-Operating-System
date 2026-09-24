using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Clovent.Restaurant.Application.QuickOrderTemplates.Commands;
using Clovent.Restaurant.Application.QuickOrderTemplates.Queries;
using Clovent.Restaurant.Application.Tests.TestSupport;
using Xunit;

namespace Clovent.Restaurant.Application.Tests.QuickOrderTemplates;

public class QuickOrderTemplateHandlerTests
{
    private readonly Guid _productId = Guid.NewGuid();
    private readonly Guid _burgerVariantId = Guid.NewGuid();
    private readonly Guid _colaVariantId = Guid.NewGuid();

    private readonly FakeQuickOrderTemplateRepository _repository = new();

    private ListActiveQuickOrderTemplatesQueryHandler CreateListHandler() =>
        new(
            _repository,
            CatalogFakes.Mediator(
                new[]
                {
                    CatalogFakes.Variant(_burgerVariantId, _productId, "Burger"),
                    CatalogFakes.Variant(_colaVariantId, Guid.NewGuid(), "Cola"),
                },
                new[] { CatalogFakes.Product(_productId, "Burger") },
                new[]
                {
                    CatalogFakes.SellingPrice(_burgerVariantId, 500m),
                    CatalogFakes.SellingPrice(_colaVariantId, 80m),
                }));

    [Fact]
    public async Task CreateAndList_PricesResolved_OverrideWins_TotalComputed()
    {
        var create = new CreateQuickOrderTemplateCommandHandler(_repository);
        var templateId = await create.Handle(
            new CreateQuickOrderTemplateCommand(
                "Combo A",
                "Burger + cola",
                DisplayOrder: 1,
                Items:
                [
                    new QuickOrderTemplateItemInput(_burgerVariantId, 2m),
                    new QuickOrderTemplateItemInput(_colaVariantId, 1m, TemplateUnitPrice: 70m),
                ]),
            CancellationToken.None);

        var listed = await CreateListHandler().Handle(new ListActiveQuickOrderTemplatesQuery(), CancellationToken.None);

        var template = Assert.Single(listed);
        Assert.Equal(templateId, template.TemplateId);
        Assert.Equal("Combo A", template.Name);
        Assert.Equal(2, template.Items.Count);

        var burger = template.Items.Single(i => i.VariantId == _burgerVariantId);
        Assert.Equal(500m, burger.UnitPrice); // current selling price
        var cola = template.Items.Single(i => i.VariantId == _colaVariantId);
        Assert.Equal(70m, cola.UnitPrice); // template override
        Assert.Equal(2m * 500m + 70m, template.TotalPrice);
    }

    [Fact]
    public async Task InactiveTemplate_IsNotListed()
    {
        var create = new CreateQuickOrderTemplateCommandHandler(_repository);
        var templateId = await create.Handle(
            new CreateQuickOrderTemplateCommand("Hidden", null, 0, [new QuickOrderTemplateItemInput(_burgerVariantId, 1m)]),
            CancellationToken.None);

        await new SetQuickOrderTemplateStatusCommandHandler(_repository).Handle(
            new SetQuickOrderTemplateStatusCommand(templateId, false), CancellationToken.None);

        var listed = await CreateListHandler().Handle(new ListActiveQuickOrderTemplatesQuery(), CancellationToken.None);
        Assert.Empty(listed);
    }

    [Fact]
    public async Task Templates_ListedInDisplayOrder()
    {
        var create = new CreateQuickOrderTemplateCommandHandler(_repository);
        await create.Handle(new CreateQuickOrderTemplateCommand("Second", null, 2, [new QuickOrderTemplateItemInput(_burgerVariantId, 1m)]), CancellationToken.None);
        await create.Handle(new CreateQuickOrderTemplateCommand("First", null, 1, [new QuickOrderTemplateItemInput(_burgerVariantId, 1m)]), CancellationToken.None);

        var listed = await CreateListHandler().Handle(new ListActiveQuickOrderTemplatesQuery(), CancellationToken.None);

        Assert.Equal(["First", "Second"], [.. listed.Select(t => t.Name)]);
    }

    [Fact]
    public async Task Create_DuplicateVariantItems_QuantitiesSummedIntoOneItem()
    {
        var create = new CreateQuickOrderTemplateCommandHandler(_repository);
        await create.Handle(
            new CreateQuickOrderTemplateCommand("Dup", null, 0,
                [
                    new QuickOrderTemplateItemInput(_burgerVariantId, 1m),
                    new QuickOrderTemplateItemInput(_burgerVariantId, 2m),
                ]),
            CancellationToken.None);

        var listed = await CreateListHandler().Handle(new ListActiveQuickOrderTemplatesQuery(), CancellationToken.None);

        var item = Assert.Single(Assert.Single(listed).Items);
        Assert.Equal(3m, item.Quantity);
        Assert.Equal(3m * 500m, Assert.Single(listed).TotalPrice);
    }

    [Fact]
    public async Task Update_ReplacesHeaderAndItems()
    {
        var create = new CreateQuickOrderTemplateCommandHandler(_repository);
        var templateId = await create.Handle(
            new CreateQuickOrderTemplateCommand("Old", null, 0, [new QuickOrderTemplateItemInput(_burgerVariantId, 1m)]),
            CancellationToken.None);

        await new UpdateQuickOrderTemplateCommandHandler(_repository).Handle(
            new UpdateQuickOrderTemplateCommand(templateId, "New", "desc", 7, [new QuickOrderTemplateItemInput(_colaVariantId, 4m)]),
            CancellationToken.None);

        var listed = await CreateListHandler().Handle(new ListActiveQuickOrderTemplatesQuery(), CancellationToken.None);
        var template = Assert.Single(listed);
        Assert.Equal("New", template.Name);
        Assert.Equal(7, template.DisplayOrder);
        var item = Assert.Single(template.Items);
        Assert.Equal(_colaVariantId, item.VariantId);
        Assert.Equal(4m, item.Quantity);
    }

    [Fact]
    public async Task Update_MissingTemplate_ThrowsNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(() =>
            new UpdateQuickOrderTemplateCommandHandler(_repository).Handle(
                new UpdateQuickOrderTemplateCommand(Guid.NewGuid(), "X", null, 0, []),
                CancellationToken.None));
    }

    [Fact]
    public async Task ListAll_IncludesInactiveTemplates_WithStatusFlag()
    {
        var create = new CreateQuickOrderTemplateCommandHandler(_repository);
        var activeId = await create.Handle(
            new CreateQuickOrderTemplateCommand("Active One", null, 1, [new QuickOrderTemplateItemInput(_burgerVariantId, 1m)]),
            CancellationToken.None);
        var inactiveId = await create.Handle(
            new CreateQuickOrderTemplateCommand("Hidden", null, 2, [new QuickOrderTemplateItemInput(_colaVariantId, 2m)]),
            CancellationToken.None);
        await new SetQuickOrderTemplateStatusCommandHandler(_repository).Handle(
            new SetQuickOrderTemplateStatusCommand(inactiveId, false), CancellationToken.None);

        var listedAll = await new ListAllQuickOrderTemplatesQueryHandler(_repository, CatalogFakes.Mediator(
            new[]
            {
                CatalogFakes.Variant(_burgerVariantId, _productId, "Burger"),
                CatalogFakes.Variant(_colaVariantId, Guid.NewGuid(), "Cola"),
            },
            new[] { CatalogFakes.Product(_productId, "Burger") },
            new[]
            {
                CatalogFakes.SellingPrice(_burgerVariantId, 500m),
                CatalogFakes.SellingPrice(_colaVariantId, 80m),
            })).Handle(new ListAllQuickOrderTemplatesQuery(), CancellationToken.None);
        var listed = listedAll.ToList();

        Assert.Equal(2, listed.Count);
        Assert.Equal(activeId, listed[0].TemplateId);
        Assert.True(listed[0].IsActive);
        Assert.Equal(500m, listed[0].TotalPrice);
        Assert.Equal(inactiveId, listed[1].TemplateId);
        Assert.False(listed[1].IsActive);
        Assert.Equal(2m * 80m, listed[1].TotalPrice);
    }
}
