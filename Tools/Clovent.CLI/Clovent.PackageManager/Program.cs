using Clovent.PackageManager.Abstractions;
using Clovent.PackageManager.Core.Builder;
using Clovent.PackageManager.Core.Services;

IPackageInstaller installer = new PackageInstaller();

if (args.Length == 0)
{
    Console.WriteLine("Commands:");
    Console.WriteLine("  pack <sourceFolder> <output.cbospkg>");
    Console.WriteLine("  install <package.cbospkg>");
    Console.WriteLine("  uninstall <packageId>");
    Console.WriteLine("  verify");
    return;
}

switch (args[0].ToLowerInvariant())
{
    case "pack":

        if (args.Length < 3)
        {
            Console.WriteLine("Usage: pack <sourceFolder> <output.cbospkg>");
            return;
        }

        new PackageBuilder().Build(args[1], args[2]);
        break;

    case "install":

        if (args.Length < 2)
        {
            Console.WriteLine("Package file required.");
            return;
        }

        await installer.InstallAsync(args[1]);
        break;

    case "uninstall":

        if (args.Length < 2)
        {
            Console.WriteLine("Package Id required.");
            return;
        }

        await installer.UninstallAsync(args[1]);
        break;

    case "verify":

        await installer.VerifyAsync();
        break;

    default:

        Console.WriteLine("Unknown command.");
        break;
}
