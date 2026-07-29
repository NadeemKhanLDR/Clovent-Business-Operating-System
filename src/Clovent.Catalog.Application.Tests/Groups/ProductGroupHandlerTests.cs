using Clovent.Catalog.Application.Groups.Commands;
using Clovent.Catalog.Application.Groups.Queries;
using Clovent.Catalog.Application.Tests.TestSupport;
using Clovent.Catalog.Groups;
using Clovent.Catalog.Groups.ValueObjects;
using Xunit;

namespace Clovent.Catalog.Application.Tests.Groups;

public class ProductGroupHandlerTests
{
    [Fact]
    public async Task CreateProductGroupCommandHandler_ValidRequest_PersistsAndReturnsDto()
    {
        var repository = new FakeProductGroupRepository();
        var handler = new CreateProductGroupCommandHandler(repository);

        var dto = await handler.Handle(new CreateProductGroupCommand("Soft Drinks"), CancellationToken.None);

        Assert.Equal("Soft Drinks", dto.Name);
        Assert.NotNull(await repository.GetByIdAsync(new ProductGroupId(dto.ProductGroupId)));
    }

    [Fact]
    public async Task RenameProductGroupCommandHandler_UnknownGroup_Throws()
    {
        var handler = new RenameProductGroupCommandHandler(new FakeProductGroupRepository());

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new RenameProductGroupCommand(Guid.NewGuid(), "New Name"), CancellationToken.None));
    }

    [Fact]
    public async Task ActivateAndDeactivateProductGroupCommandHandlers_RoundTrip()
    {
        var repository = new FakeProductGroupRepository();
        var group = ProductGroup.Create(ProductGroupName.Create("Soft Drinks"));
        group.Deactivate();
        repository.Add(group);

        var activated = await new ActivateProductGroupCommandHandler(repository)
            .Handle(new ActivateProductGroupCommand(group.Id.Value), CancellationToken.None);
        Assert.Equal("Active", activated.Status);

        var deactivated = await new DeactivateProductGroupCommandHandler(repository)
            .Handle(new DeactivateProductGroupCommand(group.Id.Value), CancellationToken.None);
        Assert.Equal("Inactive", deactivated.Status);
    }

    [Fact]
    public async Task ListProductGroupsQueryHandler_ReturnsEveryGroup()
    {
        var repository = new FakeProductGroupRepository();
        repository.Add(ProductGroup.Create(ProductGroupName.Create("Group A")));
        repository.Add(ProductGroup.Create(ProductGroupName.Create("Group B")));
        var handler = new ListProductGroupsQueryHandler(repository);

        var result = await handler.Handle(new ListProductGroupsQuery(), CancellationToken.None);

        Assert.Equal(2, result.Count);
    }
}
