using Clovent.CLI;
using Clovent.CLI.Commands.Bootstrap;
using Clovent.CLI.Commands.Entity;
using Clovent.CLI.Commands.New;
using Clovent.CLI.Commands.Shared;
using Clovent.CLI.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;

var services = new ServiceCollection();
services.AddCloventServices();

var registrar = new TypeRegistrar(services);
var app = new CommandApp(registrar);

app.Configure(config =>
{
    config.SetApplicationName("clovent");

    config.AddCommand<DoctorCommand>("doctor")
        .WithDescription("Validate development environment.");

    config.AddCommand<BootstrapCommand>("bootstrap")
        .WithDescription("Run bootstrap process.");

    config.AddBranch("new", newBranch =>
    {
        newBranch.AddCommand<NewModuleCommand>("module")
            .WithDescription("Create a new DDD Clean Architecture module.");

        newBranch.AddCommand<NewEntityCommand>("entity")
            .WithDescription("Create a new Entity / Aggregate Root with CQRS, EF Core, WinForms View, and Obsidian Docs.");
    });
});

return app.Run(args);
