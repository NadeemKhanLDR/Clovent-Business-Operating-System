using System.Reflection;
using Clovent.Authentication.Application.DependencyInjection;
using Clovent.Authentication.Infrastructure.DependencyInjection;
using Clovent.Authentication.Infrastructure.Persistence;
using Clovent.Desktop.DependencyInjection;
using Clovent.Desktop.Restaurant.Orders;
using Clovent.Desktop.Sessions;
using Clovent.Identity.Infrastructure.Persistence;
using Clovent.Restaurant.Application.KitchenTickets.Queries;
using Clovent.Restaurant.Application.Orders.Queries;
using Clovent.Restaurant.KitchenTickets;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
using Clovent.Platform.Bootstrap;
using IHost = Microsoft.Extensions.Hosting.IHost;

namespace Clovent.Desktop.Tests;

/// <summary>
/// TEMPORARY runtime-verification harness (deleted after the verification
/// run): composes the exact production DI graph against the developer
/// machine's real databases and drives the screens' real load paths,
/// including the Kitchen Tickets orphaned-order scenario.
/// </summary>
public sealed class RuntimeVerificationTests
{
    private static readonly Lazy<IHost> Host = new(() =>
    {
        var bootstrapper = ApplicationBootstrapper
            .Create(basePath: AppContext.BaseDirectory)
            .WithLogging()
            .WithPlatform();

        bootstrapper.Services.AddApplication(bootstrapper.Configuration);
        bootstrapper.Services.AddInfrastructure(bootstrapper.Configuration);
        bootstrapper.Services.AddPersistence(bootstrapper.Configuration);
        Clovent.Identity.Application.DependencyInjection.ApplicationServiceCollectionExtensions.AddApplication(bootstrapper.Services, bootstrapper.Configuration);
        Clovent.Identity.Infrastructure.DependencyInjection.InfrastructureServiceCollectionExtensions.AddInfrastructure(bootstrapper.Services, bootstrapper.Configuration);
        Clovent.Identity.Infrastructure.DependencyInjection.PersistenceServiceCollectionExtensions.AddPersistence(bootstrapper.Services, bootstrapper.Configuration);
        Clovent.MasterData.Application.DependencyInjection.ApplicationServiceCollectionExtensions.AddApplication(bootstrapper.Services, bootstrapper.Configuration);
        Clovent.MasterData.Infrastructure.DependencyInjection.InfrastructureServiceCollectionExtensions.AddInfrastructure(bootstrapper.Services, bootstrapper.Configuration);
        Clovent.MasterData.Infrastructure.DependencyInjection.PersistenceServiceCollectionExtensions.AddPersistence(bootstrapper.Services, bootstrapper.Configuration);
        Clovent.Catalog.Application.DependencyInjection.ApplicationServiceCollectionExtensions.AddApplication(bootstrapper.Services, bootstrapper.Configuration);
        Clovent.Catalog.Infrastructure.DependencyInjection.InfrastructureServiceCollectionExtensions.AddInfrastructure(bootstrapper.Services, bootstrapper.Configuration);
        Clovent.Catalog.Infrastructure.DependencyInjection.PersistenceServiceCollectionExtensions.AddPersistence(bootstrapper.Services, bootstrapper.Configuration);
        Clovent.Inventory.Application.DependencyInjection.ApplicationServiceCollectionExtensions.AddApplication(bootstrapper.Services, bootstrapper.Configuration);
        Clovent.Inventory.Infrastructure.DependencyInjection.InfrastructureServiceCollectionExtensions.AddInfrastructure(bootstrapper.Services, bootstrapper.Configuration);
        Clovent.Inventory.Infrastructure.DependencyInjection.PersistenceServiceCollectionExtensions.AddPersistence(bootstrapper.Services, bootstrapper.Configuration);
        Clovent.Restaurant.Application.DependencyInjection.ApplicationServiceCollectionExtensions.AddApplication(bootstrapper.Services, bootstrapper.Configuration);
        Clovent.Restaurant.Infrastructure.DependencyInjection.InfrastructureServiceCollectionExtensions.AddInfrastructure(bootstrapper.Services, bootstrapper.Configuration);
        Clovent.Restaurant.Infrastructure.DependencyInjection.PersistenceServiceCollectionExtensions.AddPersistence(bootstrapper.Services, bootstrapper.Configuration);
        bootstrapper.Services.AddDesktopHost(bootstrapper.Configuration);
        global::Clovent.Desktop.Modules.DesktopModuleLoader.LoadModules(bootstrapper.Services, bootstrapper.Configuration, global::Clovent.Desktop.Modules.DesktopModuleCatalog.ModuleTypes);

        return bootstrapper.BuildAndInitializeAsync().GetAwaiter().GetResult();
    });

    private static async Task<(IMediator Mediator, ICurrentSession Session)> ConnectAsAdminAsync()
    {
        var host = Host.Value;
        var session = host.Services.GetRequiredService<ICurrentSession>();
        using (var scope = host.Services.CreateScope())
        {
            var identityDb = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            var admin = identityDb.Users.Local.Count != 0
                ? identityDb.Users.Local.Single()
                : identityDb.Users.Single(u => u.Id == new Clovent.Identity.Users.UserId(Guid.Parse("BFE39055-F8A8-491A-90F8-676C667BE2EF")));
            session.SignIn(admin.Id.Value, Guid.NewGuid(), admin.UserName.Value);
        }

        var mediator = host.Services.GetRequiredService<IMediator>();
        return (mediator, session);
    }

    [Fact]
    public async Task KitchenTickets_RealDatabase_OrphanedTicketShowsUnavailableNotCrash()
    {
        var (mediator, _) = await ConnectAsAdminAsync();

        var tickets = await mediator.Send(new ListActiveKitchenTicketsQuery());
        Assert.NotEmpty(tickets);

        // The exact row that crashed the screen before the fix.
        var orphan = tickets.Single(t => t.OrderId == Guid.Parse("AE963CD6-9DEC-491B-979D-A58499B10D14"));
        Assert.Equal("New", orphan.Status);

        var rows = await KitchenTicketViewerView.BuildRowsAsync(mediator, tickets, CancellationToken.None);
        var orphanRow = rows.Single(r => r.KitchenTicketId == orphan.KitchenTicketId);
        Assert.Equal("(order unavailable)", orphanRow.OrderNumber);

        // Every other ticket still resolves its real order number.
        foreach (var row in rows.Where(r => r.KitchenTicketId != orphan.KitchenTicketId))
        {
            Assert.NotEqual("(order unavailable)", row.OrderNumber);
        }

        // Refresh path re-runs the same load without throwing.
        var again = await mediator.Send(new ListActiveKitchenTicketsQuery());
        Assert.Equal(tickets.Count, again.Count);
    }

    [Fact]
    public async Task MenuItems_RealDatabase_LoadsRowsAndPrices()
    {
        var (mediator, _) = await ConnectAsAdminAsync();
        var variants = await mediator.Send(new Clovent.Catalog.Application.Variants.Queries.ListProductVariantsQuery());
        Assert.NotEmpty(variants);
        var prices = await mediator.Send(new Clovent.Catalog.Application.Prices.Queries.ListActiveProductPricesByTypeQuery(Clovent.Catalog.Prices.PriceType.Selling));
        Assert.NotEmpty(prices);
        var categories = await mediator.Send(new Clovent.Catalog.Application.Categories.Queries.ListProductCategoriesQuery());
        Assert.NotEmpty(categories);
    }

    [Fact]
    public void DevExpressIcons_EveryUriUsed_ResolvesToAnActualImage()
    {
        var uris = new[]
        {
            Clovent.Desktop.Forms.Base.DesktopIcons.Add,
            Clovent.Desktop.Forms.Base.DesktopIcons.Edit,
            Clovent.Desktop.Forms.Base.DesktopIcons.ActivateIcon,
            Clovent.Desktop.Forms.Base.DesktopIcons.CancelIcon,
            Clovent.Desktop.Forms.Base.DesktopIcons.Refresh,
            Clovent.Desktop.Forms.Base.DesktopIcons.Save,
            Clovent.Desktop.Forms.Base.DesktopIcons.Search,
            Clovent.Desktop.Forms.Base.DesktopIcons.Up,
            Clovent.Desktop.Forms.Base.DesktopIcons.Down,
        };

        var cache = DevExpress.Images.ImageResourceCache.Default;
        foreach (var uri in uris)
        {
            var svg = cache.GetSvgImage(uri);
            Assert.True(svg is not null, $"Icon URI '{uri}' did not resolve to an SVG image.");
        }
    }
}