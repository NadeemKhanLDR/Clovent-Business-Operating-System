using Clovent.Platform.Bootstrap;
using Clovent.Desktop.Sessions;
using Clovent.Desktop.Restaurant.SmartPos;
using Clovent.Identity.Users;
using Clovent.Identity.Users.ValueObjects;
using Clovent.Identity.Application.Authorization;
using Clovent.Restaurant.Application.SmartCombos;
using Clovent.Restaurant.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

// Explicit local-development verification tool. Never creates/publishes a deal or changes sales history.
var b = ApplicationBootstrapper.Create(basePath: Path.GetFullPath("src/Clovent.Desktop")).WithLogging().WithPlatform();
Clovent.Identity.Application.DependencyInjection.ApplicationServiceCollectionExtensions.AddApplication(b.Services,b.Configuration);
Clovent.Identity.Infrastructure.DependencyInjection.PersistenceServiceCollectionExtensions.AddPersistence(b.Services,b.Configuration);
Clovent.MasterData.Application.DependencyInjection.ApplicationServiceCollectionExtensions.AddApplication(b.Services,b.Configuration);
Clovent.MasterData.Infrastructure.DependencyInjection.PersistenceServiceCollectionExtensions.AddPersistence(b.Services,b.Configuration);
Clovent.Catalog.Application.DependencyInjection.ApplicationServiceCollectionExtensions.AddApplication(b.Services,b.Configuration);
Clovent.Catalog.Infrastructure.DependencyInjection.PersistenceServiceCollectionExtensions.AddPersistence(b.Services,b.Configuration);
Clovent.Restaurant.Application.DependencyInjection.ApplicationServiceCollectionExtensions.AddApplication(b.Services,b.Configuration);
Clovent.Restaurant.Infrastructure.DependencyInjection.PersistenceServiceCollectionExtensions.AddPersistence(b.Services,b.Configuration);
Clovent.Identity.Infrastructure.DependencyInjection.InfrastructureServiceCollectionExtensions.AddInfrastructure(b.Services,b.Configuration);
Clovent.Inventory.Application.DependencyInjection.ApplicationServiceCollectionExtensions.AddApplication(b.Services,b.Configuration);
Clovent.Inventory.Infrastructure.DependencyInjection.PersistenceServiceCollectionExtensions.AddPersistence(b.Services,b.Configuration);
Clovent.Inventory.Infrastructure.DependencyInjection.InfrastructureServiceCollectionExtensions.AddInfrastructure(b.Services,b.Configuration);
Clovent.Restaurant.Infrastructure.DependencyInjection.InfrastructureServiceCollectionExtensions.AddInfrastructure(b.Services,b.Configuration);
b.Services.AddSingleton<ICurrentSession,CurrentSession>();
b.Services.AddScoped<ISmartComboAccess,SmartComboAccess>();
b.Services.AddSingleton(new SmartComboOptions { IncludeDevelopmentSamples = true });
using var host=b.Build();
using var scope=host.Services.CreateScope();
var sp=scope.ServiceProvider;
var admin=await sp.GetRequiredService<IUserRepository>().GetByUserNameAsync(UserName.Create("admin")) ?? throw new Exception("Development admin not found.");
sp.GetRequiredService<ICurrentSession>().SignIn(admin.Id.Value,Guid.NewGuid(),"admin");
if (args.Contains("--seed-permissions"))
{
    await new Clovent.Desktop.Seed.DevelopmentAuthorizationSeedStartupTask(
        sp.GetRequiredService<IUserRepository>(), sp.GetRequiredService<Clovent.Identity.Roles.IRoleRepository>(),
        sp.GetRequiredService<Clovent.Identity.Permissions.IPermissionRepository>(), sp.GetRequiredService<Clovent.Identity.Infrastructure.Persistence.IdentityDbContext>(),
        Microsoft.Extensions.Options.Options.Create(new Clovent.Desktop.Theming.DesktopOptions { SeedDevelopmentUser=true, DefaultSkin="WXI", DefaultLanguage="en-US", SeedDevelopmentMasterData=false, SeedDevelopmentCatalogData=false, SeedDevelopmentRestaurantData=false })).ExecuteAsync();
}
var access=sp.GetRequiredService<ISmartComboAccess>();
var locations=await access.LocationsAsync(default);
if(args.Contains("--seed-development-prices"))
{
    var connection = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(b.Configuration["ConnectionStrings:Catalog"]);
    var company=await sp.GetRequiredService<Clovent.Identity.Companies.ICompanyRepository>().GetByIdAsync(admin.CompanyId!.Value);
    if(connection.DataSource!="." || connection.InitialCatalog!="Clovent_Catalog" || company?.Name.Value!="Clovent Demo Company")
        throw new InvalidOperationException("Seed is restricted to the local Clovent demo company/database.");
    var currency=await access.CurrencyAsync(locations.Single().Id,default);
    var catalog=sp.GetRequiredService<Clovent.Catalog.Infrastructure.Persistence.CatalogDbContext>();
    var products=await catalog.Products.ToListAsync(); var variants=await catalog.ProductVariants.ToListAsync();
    var repository=sp.GetRequiredService<Clovent.Catalog.Prices.IProductPriceRepository>();
    foreach(var example in new[]{("Chicken Biryani",450m),("Salad",30m),("Leechi",50m)})
    {
        var product=products.Single(p=>p.Name.Value==example.Item1);
        var variant=variants.Single(v=>v.ProductId==product.Id && v.Status.ToString()=="Active");
        if((await repository.GetByProductVariantIdAsync(variant.Id)).Any(p=>p.CurrencyId.Value==currency.Id && p.PriceType==Clovent.Catalog.Prices.PriceType.Selling && p.Status.ToString()=="Active")) continue;
        var created=await new Clovent.Catalog.Application.Prices.Commands.CreateProductPriceCommandHandler(repository).Handle(new(variant.Id.Value,Clovent.Catalog.Prices.PriceType.Selling,example.Item2,currency.Id),default);
        await catalog.SaveChangesAsync();
        File.AppendAllText("scratch/smartcombo-development-price-manifest.txt",$"{DateTimeOffset.UtcNow:u} | {example.Item1} | PriceId={created.ProductPriceId} | VariantId={variant.Id.Value} | Amount={example.Item2} | Currency={currency.Id}\n");
    }
}
if(args.Contains("--seed-development-orders"))
{
    var connection = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(b.Configuration["ConnectionStrings:Restaurant"]);
    var company=await sp.GetRequiredService<Clovent.Identity.Companies.ICompanyRepository>().GetByIdAsync(admin.CompanyId!.Value);
    if(connection.DataSource!="." || connection.InitialCatalog!="Clovent_Restaurant" || company?.Name.Value!="Clovent Demo Company") throw new InvalidOperationException("Seed restricted to local demo database.");
    var warehouse=locations.Single().Id;
    var before=await sp.GetRequiredService<SmartComboService>().AnalyzeAsync(warehouse,30,default);
    if(before.Opportunities.Count==0)
    {
        var mediator=sp.GetRequiredService<MediatR.IMediator>();
        var catalog=sp.GetRequiredService<Clovent.Catalog.Infrastructure.Persistence.CatalogDbContext>();
        var products=await catalog.Products.ToListAsync();var variants=await catalog.ProductVariants.ToListAsync();
        var ids=new[]{"Chicken Biryani","Salad","Leechi"}.Select(name=>variants.Single(v=>v.ProductId==products.Single(p=>p.Name.Value==name).Id && v.Status.ToString()=="Active").Id.Value).ToArray();
        var cash=(await sp.GetRequiredService<Clovent.Restaurant.PaymentMethods.IPaymentMethodRepository>().GetAllAsync()).Single(p=>p.Name.Value=="Cash" && p.Status.ToString()=="Active");
        var db=sp.GetRequiredService<RestaurantDbContext>();
        // Preflight all tracked stock before creating any order.
        foreach(var id in ids)
        {
            var stock=await mediator.Send(new Clovent.Inventory.Application.WarehouseStocks.Queries.GetWarehouseStockByWarehouseAndVariantQuery(warehouse,id));
            if(stock!=null && !stock.AllowNegativeStock && stock.QuantityOnHand<5) throw new InvalidOperationException("Insufficient development stock; no sample orders created.");
        }
        for(int i=1;i<=5;i++)
        {
            var note=$"DEV-SMARTCOMBO:20260921:{i}";
            var existing=await db.Orders.FirstOrDefaultAsync(o=>o.Notes==note);
            if(existing?.Status==Clovent.Restaurant.Orders.OrderStatus.Completed) continue;
            if(existing!=null) throw new InvalidOperationException("Sample already exists but is not completed; inspect it before retrying.");
            var order=await mediator.Send(new Clovent.Restaurant.Application.Orders.Commands.CreateOrderCommand(Clovent.Restaurant.Orders.OrderType.TakeAway,warehouse));
            await mediator.Send(new Clovent.Restaurant.Application.Orders.Commands.SetOrderNotesCommand(order.OrderId,note));
            foreach(var id in ids) await mediator.Send(new Clovent.Restaurant.Application.OrderLines.Commands.AddOrderLineCommand(order.OrderId,id,1));
            var lines=await mediator.Send(new Clovent.Restaurant.Application.OrderLines.Queries.ListOrderLinesByOrderQuery(order.OrderId));
            var total=Clovent.Restaurant.Application.Orders.OrderTotalsCalculator.Calculate(lines,[],[],[]).GrandTotal;
            await mediator.Send(new Clovent.Restaurant.Application.Payments.Commands.RecordPaymentCommand(order.OrderId,cash.Id.Value,total));
            var completed=await mediator.Send(new Clovent.Restaurant.Application.Orders.Commands.CompleteOrderCommand(order.OrderId));
            File.AppendAllText("scratch/smartcombo-development-order-manifest.txt",$"{note} | {completed.OrderNumber} | {order.OrderId} | Completed | Total={total}\n");
        }
    }
}
foreach(var location in locations)
{
    await access.RequireAsync("analyze",location.Id,default);
    var timer=System.Diagnostics.Stopwatch.StartNew();
    var result=await sp.GetRequiredService<SmartComboService>().AnalyzeAsync(location.Id,30,default);
    timer.Stop();
    File.WriteAllText("scratch/smartcombo-development-result.json",JsonSerializer.Serialize(new { Location=location.Name,ElapsedMilliseconds=timer.ElapsedMilliseconds,Analysis=result },new JsonSerializerOptions{WriteIndented=true}));
    if(args.Contains("--verify-conversion-rollback"))
    {
        var opportunity=result.Opportunities.First(x=>x.Items.Count==3);
        var db=sp.GetRequiredService<RestaurantDbContext>();
        var beforeTemplates=await db.QuickOrderTemplates.CountAsync();
        await using var transaction=await db.Database.BeginTransactionAsync();
        var mediator=sp.GetRequiredService<MediatR.IMediator>();
        var id=await mediator.Send(new ConvertSmartComboCommand(location.Id,30,opportunity.Signature,"Rollback verification only",opportunity.SuggestedPrice));
        var templates=await mediator.Send(new Clovent.Restaurant.Application.QuickOrderTemplates.Queries.ListActiveQuickOrderTemplatesQuery(location.Id));
        var created=templates.Single(t=>t.TemplateId==id);
        if(created.TotalPrice!=opportunity.SuggestedPrice || created.Items.Count!=3 || created.Items.Any(i=>i.Quantity!=1) || created.WarehouseId!=location.Id) throw new Exception("Canonical template mismatch.");
        var other=await mediator.Send(new Clovent.Restaurant.Application.QuickOrderTemplates.Queries.ListActiveQuickOrderTemplatesQuery(Guid.NewGuid()));
        if(other.Any(t=>t.TemplateId==id)) throw new Exception("Warehouse isolation failed.");
        await transaction.RollbackAsync();db.ChangeTracker.Clear();
        if(await db.QuickOrderTemplates.CountAsync()!=beforeTemplates) throw new Exception("Rollback did not restore template count.");
        File.WriteAllText("scratch/smartcombo-conversion-verification.txt","PASS: manager command -> canonical template -> existing scoped Quick Orders query; exact total and 3 unit components; other warehouse excluded. Transaction rolled back; no deal published.");
    }
    Console.WriteLine(JsonSerializer.Serialize(new { Location=location.Name, Analysis=result },new JsonSerializerOptions{WriteIndented=true}));
}






