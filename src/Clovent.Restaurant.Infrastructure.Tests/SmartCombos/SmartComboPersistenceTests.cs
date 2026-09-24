using Clovent.Restaurant.Infrastructure.Tests.TestSupport;
using Clovent.Restaurant.Infrastructure.Repositories;
using Clovent.Restaurant.SmartCombos;
using Clovent.Restaurant.QuickOrderTemplates;
using Microsoft.EntityFrameworkCore;
using Xunit;
namespace Clovent.Restaurant.Infrastructure.Tests.SmartCombos;
public class SmartComboPersistenceTests : SqliteTestBase
{
    [Fact] public async Task Decisions_RoundTripAndIsolateLocations()
    {
        var warehouse=Guid.NewGuid(); var user=Guid.NewGuid();
        await using(var db=CreateContext()) { var store=new SmartComboStore(db); var decision=await store.DecisionAsync(warehouse,"a|b",default); decision.Dismiss(user,"Seasonal",30); await db.SaveChangesAsync(); }
        await using(var db=CreateContext()) { var store=new SmartComboStore(db); var decision=Assert.Single(await store.DecisionsAsync(warehouse,default)); Assert.Equal(user,decision.UserId); Assert.Equal("Seasonal",decision.Reason); Assert.True(decision.Suppresses(DateTimeOffset.UtcNow)); Assert.Empty(await store.DecisionsAsync(Guid.NewGuid(),default)); }
    }
    [Fact] public async Task TemplateWarehouse_RoundTripsWithoutChangingLegacyTemplates()
    {
        var warehouse=Guid.NewGuid();
        await using(var db=CreateContext()) { var scoped=QuickOrderTemplate.Create("Scoped"); scoped.ScopeToWarehouse(warehouse); db.Add(scoped); db.Add(QuickOrderTemplate.Create("Legacy")); await db.SaveChangesAsync(); }
        await using(var db=CreateContext()) { var all=await db.QuickOrderTemplates.ToListAsync(); Assert.Equal(warehouse,all.Single(x=>x.Name=="Scoped").WarehouseId); Assert.Null(all.Single(x=>x.Name=="Legacy").WarehouseId); }
    }
    [Fact] public async Task ConcurrentDecisionUpdate_IsRejected()
    {
        var warehouse=Guid.NewGuid();
        await using(var db=CreateContext()) { var decision=await new SmartComboStore(db).DecisionAsync(warehouse,"a|b",default); decision.Dismiss(Guid.NewGuid(),null,1); await db.SaveChangesAsync(); }
        await using var first=CreateContext(); await using var second=CreateContext();
        var a=await new SmartComboStore(first).DecisionAsync(warehouse,"a|b",default); var b=await new SmartComboStore(second).DecisionAsync(warehouse,"a|b",default);
        a.Convert(Guid.NewGuid(),Guid.NewGuid()); b.Convert(Guid.NewGuid(),Guid.NewGuid()); await first.SaveChangesAsync();
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(()=>second.SaveChangesAsync());
    }
    [Fact] public async Task DuplicateConversionRollsBackItsCanonicalTemplate()
    {
        var warehouse=Guid.NewGuid();
        await using var first=CreateContext();await using var second=CreateContext();
        var a=await new SmartComboStore(first).DecisionAsync(warehouse,"a|b",default);
        var b=await new SmartComboStore(second).DecisionAsync(warehouse,"a|b",default);
        var firstId=await new CreateQuickOrderTemplateCommandHandler(new QuickOrderTemplateRepository(first)).Handle(new("Approved",null,0,[new(Guid.NewGuid(),1,10)],warehouse),default);
        var secondId=await new CreateQuickOrderTemplateCommandHandler(new QuickOrderTemplateRepository(second)).Handle(new("Duplicate",null,0,[new(Guid.NewGuid(),1,10)],warehouse),default);
        a.Convert(Guid.NewGuid(),firstId);b.Convert(Guid.NewGuid(),secondId);
        await first.SaveChangesAsync();await Assert.ThrowsAsync<DbUpdateException>(()=>second.SaveChangesAsync());
        await using var verify=CreateContext();Assert.Equal("Approved",Assert.Single(await verify.QuickOrderTemplates.ToListAsync()).Name);
        Assert.Equal(firstId,Assert.Single(await verify.Set<ComboDecision>().ToListAsync()).TemplateId);
    }}

