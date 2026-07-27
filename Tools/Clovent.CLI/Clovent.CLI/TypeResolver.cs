using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;

namespace Clovent.CLI;

public sealed class TypeResolver : ITypeResolver, IDisposable
{
    private readonly ServiceProvider _provider;

    public TypeResolver(ServiceProvider provider)
    {
        _provider = provider;
    }

    public object? Resolve(Type? type)
    {
        if (type is null)
            return null;

        return _provider.GetService(type);
    }

    public void Dispose()
    {
        _provider.Dispose();
    }
}
