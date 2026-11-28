using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Clovent.Catalog.Variants;
using Clovent.Inventory.Application.Transactions.Dtos;
using Clovent.Inventory.Application.Transactions.Queries;
using Clovent.Inventory.Application.WarehouseStocks.Commands;
using Clovent.Inventory.Application.WarehouseStocks.Dtos;
using Clovent.Inventory.Application.WarehouseStocks.Queries;
using Clovent.Inventory.Recipes;
using Clovent.Inventory.Transactions;
using Clovent.MasterData.Warehouses;
using Clovent.Restaurant.Application.Outbox.Dtos;
using Clovent.Restaurant.Application.Outbox.Handlers;
using Clovent.Restaurant.Application.Tests.TestSupport;
using Clovent.Restaurant.Outbox;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Clovent.Restaurant.Application.Tests.Outbox;

public class RecipeSalesInventoryExplosionTests
{
    private readonly FakeRecipeRepository _recipeRepo = new();
    private readonly List<InventoryTransactionDto> _recordedTransactions = [];
    private readonly List<IssueStockCommand> _recordedIssueCommands = [];
    private readonly List<OpenOrReceiveStockCommand> _recordedReceiveCommands = [];
    private readonly FakeMediator _mediator;

    public RecipeSalesInventoryExplosionTests()
    {
        _mediator = new FakeMediator(req =>
        {
            if (req is ListInventoryTransactionsByReferenceQuery q)
            {
                var matches = _recordedTransactions
                    .Where(t => t.ReferenceType == q.ReferenceType && t.ReferenceId == q.ReferenceId)
                    .ToList();
                return Task.FromResult<object?>(matches.AsReadOnly());
            }

            if (req is GetWarehouseStockByWarehouseAndVariantQuery sq)
            {
                return Task.FromResult<object?>(new WarehouseStockDto(
                    Guid.NewGuid(), sq.WarehouseId, sq.ProductVariantId, 100m, 0m, 100m, 5m, 200m, false, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));
            }

            if (req is IssueStockCommand ic)
            {
                _recordedIssueCommands.Add(ic);
                return Task.FromResult<object?>(new WarehouseStockDto(
                    ic.WarehouseStockId, Guid.NewGuid(), Guid.NewGuid(), 50m, 0m, 50m, 5m, 100m, false, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));
            }

            if (req is OpenOrReceiveStockCommand rc)
            {
                _recordedReceiveCommands.Add(rc);
                return Task.FromResult<object?>(new WarehouseStockDto(
                    Guid.NewGuid(), rc.WarehouseId, rc.ProductVariantId, 50m, 0m, 50m, 5m, 100m, false, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));
            }

            return Task.FromResult<object?>(null);
        });
    }

    [Fact]
    public async Task HandleAsync_WhenVariantHasRecipe_DeductsRecipeIngredientsProportionally()
    {
        var warehouseId = Guid.NewGuid();
        var burgerVariantId = Guid.NewGuid();
        var beefPattyVariantId = Guid.NewGuid();
        var bunVariantId = Guid.NewGuid();

        var recipe = Recipe.Create(RecipeId.New(), burgerVariantId, "Gourmet Burger", 1m, "PORTION");
        // 150g beef patty with 10% trim loss = 0.1650 KG gross
        recipe.AddOrUpdateIngredient(beefPattyVariantId, "Beef Patty", 0.1500m, "KG", 10m);
        // 1 bun with 0% loss = 1.0000 PCS gross
        recipe.AddOrUpdateIngredient(bunVariantId, "Brioche Bun", 1.0000m, "PCS", 0m);

        await _recipeRepo.AddAsync(recipe);

        var handler = new InventoryPostingOutboxHandler(
            _mediator,
            NullLogger<InventoryPostingOutboxHandler>.Instance,
            recipeRepository: _recipeRepo);

        var payload = new InventoryPostingPayload(
            Guid.NewGuid(),
            "ORD-001",
            warehouseId,
            [new InventoryPostingLineItem(burgerVariantId, "BRG-01", "Gourmet Burger", 2m)]);

        var message = OutboxMessage.Create(
            OutboxMessageType.InventoryPosting,
            "Order",
            payload.OrderId.ToString(),
            payload.OrderId.ToString(),
            JsonSerializer.Serialize(payload));

        await handler.HandleAsync(message, CancellationToken.None);

        // Expect 2 ingredient issues (Beef Patty and Brioche Bun), NOT the burger finished good
        Assert.Equal(2, _recordedIssueCommands.Count);

        // 2 portions * 0.1650 KG = 0.3300 KG
        var beefIssue = _recordedIssueCommands.FirstOrDefault(c => c.Notes != null && c.Notes.Contains("Beef Patty"));
        Assert.NotNull(beefIssue);
        Assert.Equal(0.3300m, beefIssue.Quantity);

        // 2 portions * 1.0000 PCS = 2.0000 PCS
        var bunIssue = _recordedIssueCommands.FirstOrDefault(c => c.Notes != null && c.Notes.Contains("Brioche Bun"));
        Assert.NotNull(bunIssue);
        Assert.Equal(2.0000m, bunIssue.Quantity);
    }

    [Fact]
    public async Task HandleAsync_WhenOrderNotesContainModifier_DeductsModifierIngredient()
    {
        var warehouseId = Guid.NewGuid();
        var burgerVariantId = Guid.NewGuid();
        var beefPattyVariantId = Guid.NewGuid();
        var cheeseSliceVariantId = Guid.NewGuid();

        var recipe = Recipe.Create(RecipeId.New(), burgerVariantId, "Cheeseburger", 1m, "PORTION");
        recipe.AddOrUpdateIngredient(beefPattyVariantId, "Beef Patty", 0.1500m, "KG", 0m);
        recipe.AddModifier("Extra Cheese", cheeseSliceVariantId, "Cheddar Slice", 1m, "SLICE", 0m);

        await _recipeRepo.AddAsync(recipe);

        var handler = new InventoryPostingOutboxHandler(
            _mediator,
            NullLogger<InventoryPostingOutboxHandler>.Instance,
            recipeRepository: _recipeRepo);

        // Customer ordered 1 burger with note: "Extra Cheese; well done"
        var payload = new InventoryPostingPayload(
            Guid.NewGuid(),
            "ORD-002",
            warehouseId,
            [new InventoryPostingLineItem(burgerVariantId, "CH-BRG", "Cheeseburger", 1m, "Extra Cheese; well done")]);

        var message = OutboxMessage.Create(
            OutboxMessageType.InventoryPosting,
            "Order",
            payload.OrderId.ToString(),
            payload.OrderId.ToString(),
            JsonSerializer.Serialize(payload));

        await handler.HandleAsync(message, CancellationToken.None);

        // Expect 2 issues: base patty (0.1500 KG) and modifier cheese slice (1 SLICE)
        Assert.Equal(2, _recordedIssueCommands.Count);

        var modifierIssue = _recordedIssueCommands.FirstOrDefault(c => c.Notes != null && c.Notes.Contains("Modifier: Extra Cheese"));
        Assert.NotNull(modifierIssue);
        Assert.Equal(1m, modifierIssue.Quantity);
    }

    [Fact]
    public async Task HandleAsync_WhenVariantHasNoRecipe_DeductsFinishedGoodDirectly()
    {
        var warehouseId = Guid.NewGuid();
        var waterVariantId = Guid.NewGuid();

        var handler = new InventoryPostingOutboxHandler(
            _mediator,
            NullLogger<InventoryPostingOutboxHandler>.Instance,
            recipeRepository: _recipeRepo);

        var payload = new InventoryPostingPayload(
            Guid.NewGuid(),
            "ORD-003",
            warehouseId,
            [new InventoryPostingLineItem(waterVariantId, "WATER-500", "Mineral Water 500ml", 3m)]);

        var message = OutboxMessage.Create(
            OutboxMessageType.InventoryPosting,
            "Order",
            payload.OrderId.ToString(),
            payload.OrderId.ToString(),
            JsonSerializer.Serialize(payload));

        await handler.HandleAsync(message, CancellationToken.None);

        var singleIssue = Assert.Single(_recordedIssueCommands);
        Assert.Equal(3m, singleIssue.Quantity);
        Assert.Equal("Order ORD-003", singleIssue.Notes);
    }

    [Fact]
    public async Task HandleAsync_WhenAlreadyIssued_SkipsDuplicateDeduction()
    {
        var orderId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        var sodaVariantId = Guid.NewGuid();

        // Seed transaction indicating soda has already been issued for this order
        _recordedTransactions.Add(new InventoryTransactionDto(
            Guid.NewGuid(),
            warehouseId,
            sodaVariantId,
            nameof(InventoryTransactionType.Issue),
            1m,
            "Order",
            orderId,
            "Order ORD-004",
            DateTimeOffset.UtcNow));

        var handler = new InventoryPostingOutboxHandler(
            _mediator,
            NullLogger<InventoryPostingOutboxHandler>.Instance,
            recipeRepository: _recipeRepo);

        var payload = new InventoryPostingPayload(
            orderId,
            "ORD-004",
            warehouseId,
            [new InventoryPostingLineItem(sodaVariantId, "SODA", "Can of Soda", 1m)]);

        var message = OutboxMessage.Create(
            OutboxMessageType.InventoryPosting,
            "Order",
            orderId.ToString(),
            orderId.ToString(),
            JsonSerializer.Serialize(payload));

        await handler.HandleAsync(message, CancellationToken.None);

        // Must NOT issue again
        Assert.Empty(_recordedIssueCommands);
    }

    private sealed class FakeRecipeRepository : IRecipeRepository
    {
        private readonly ConcurrentDictionary<Guid, Recipe> _recipes = new();

        public Task<Recipe?> GetByIdAsync(RecipeId id, CancellationToken cancellationToken = default)
        {
            _recipes.TryGetValue(id.Value, out var recipe);
            return Task.FromResult(recipe);
        }

        public Task<Recipe?> GetByProductVariantIdAsync(Guid productVariantId, CancellationToken cancellationToken = default)
        {
            var recipe = _recipes.Values.FirstOrDefault(r => r.ProductVariantId == productVariantId && r.IsActive);
            return Task.FromResult(recipe);
        }

        public Task<IReadOnlyList<Recipe>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            IReadOnlyList<Recipe> list = _recipes.Values.ToList().AsReadOnly();
            return Task.FromResult(list);
        }

        public Task AddAsync(Recipe recipe, CancellationToken cancellationToken = default)
        {
            _recipes[recipe.Id.Value] = recipe;
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Recipe recipe, CancellationToken cancellationToken = default)
        {
            _recipes[recipe.Id.Value] = recipe;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(RecipeId id, CancellationToken cancellationToken = default)
        {
            _recipes.TryRemove(id.Value, out _);
            return Task.CompletedTask;
        }
    }
}
