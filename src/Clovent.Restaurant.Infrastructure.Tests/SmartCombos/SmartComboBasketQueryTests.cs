using Clovent.Catalog.Variants;
using Clovent.MasterData.Warehouses;
using Clovent.Restaurant.Infrastructure.Persistence;
using Clovent.Restaurant.Infrastructure.Repositories;
using Clovent.Restaurant.Orders;
using Clovent.Restaurant.Orders.ValueObjects;
using Clovent.Restaurant.OrderLines;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Xunit;
namespace Clovent.Restaurant.Infrastructure.Tests.SmartCombos;
public class SmartComboBasketQueryTests
{
    // SQLite lacks datetimeoffset range operators: UTC ticks converter is test-only; SQL Server uses native datetimeoffset.
    public sealed class Dates(ModelCustomizerDependencies dependencies):ModelCustomizer(dependencies)
    {
        public override void Customize(ModelBuilder modelBuilder,DbContext context)
        {
            base.Customize(modelBuilder,context);
            foreach(var type in modelBuilder.Model.GetEntityTypes()) foreach(var p in type.GetProperties())
                if(p.ClrType==typeof(DateTimeOffset)) p.SetValueConverter(new ValueConverter<DateTimeOffset,long>(v=>v.UtcTicks,v=>new DateTimeOffset(v,TimeSpan.Zero)));
        }
    }
    [Theory] [InlineData(OrderStatus.Open,0)] [InlineData(OrderStatus.Held,0)] [InlineData(OrderStatus.Cancelled,0)] [InlineData(OrderStatus.Voided,0)] [InlineData(OrderStatus.Completed,1)]
    public async Task QueryIncludesOnlyCompletedSales(OrderStatus status,int expected)
    {
        using var connection=new SqliteConnection("DataSource=:memory:"); connection.Open();
        await using var db=new RestaurantDbContext(new DbContextOptionsBuilder<RestaurantDbContext>().UseSqlite(connection).ReplaceService<IModelCustomizer,Dates>().Options);
        await db.Database.EnsureCreatedAsync();
        var warehouse=Guid.NewGuid(); var order=Order.Create(OrderType.TakeAway,new WarehouseId(warehouse),null,OrderNumber.Create("SC-1"));
        var valid=OrderLine.Create(order.Id,new ProductVariantId(Guid.NewGuid()),2,100,0,false); order.AddOrderLine(valid.Id); db.Add(valid);
        var voided=OrderLine.Create(order.Id,new ProductVariantId(Guid.NewGuid()),1,100,0,false); order.AddOrderLine(voided.Id); voided.Void(); db.Add(voided);
        var removed=OrderLine.Create(order.Id,new ProductVariantId(Guid.NewGuid()),1,100,0,false); db.Add(removed);
        var zero=OrderLine.Create(order.Id,new ProductVariantId(Guid.NewGuid()),1,0,0,false); order.AddOrderLine(zero.Id); db.Add(zero);
        switch(status) { case OrderStatus.Held:order.Hold();break; case OrderStatus.Cancelled:order.Cancel("test");break;case OrderStatus.Voided:order.Void("test");break;case OrderStatus.Completed:order.Complete();break; }
        db.Add(order); await db.SaveChangesAsync(); db.ChangeTracker.Clear();
        var store=new SmartComboStore(db);var now=DateTimeOffset.UtcNow;
        var baskets=await store.ReadBasketsAsync(warehouse,now.AddDays(-1),now.AddMinutes(1),100,default);
        Assert.Equal(expected,baskets.Count);
        if(expected>0) { Assert.Equal(valid.ProductVariantId.Value,Assert.Single(baskets[0].Variants)); Assert.Equal(order.Id.Value,baskets[0].OrderId); }
        Assert.Empty(await store.ReadBasketsAsync(Guid.NewGuid(),now.AddDays(-1),now.AddMinutes(1),100,default));
        Assert.Empty(await store.ReadBasketsAsync(warehouse,now.AddDays(-3),now.AddDays(-2),100,default));
        Assert.Empty(db.ChangeTracker.Entries());
    }
}
