using Clovent.Core.Interfaces;
using Clovent.Core.Models;
using Clovent.Core.Results;

namespace Clovent.Core.Services;

public sealed class BootstrapService : IBootstrapService
{
    public BootstrapResult Execute(BootstrapOptions options)
    {
        var result = new BootstrapResult
        {
            Success = true
        };

        result.Messages.Add("Bootstrap service started.");

        if (string.IsNullOrWhiteSpace(options.RootPath))
        {
            result.Success = false;
            result.Errors.Add("Repository path is missing.");

            return result;
        }

        if (!Directory.Exists(options.RootPath))
        {
            result.Success = false;
            result.Errors.Add($"Folder not found: {options.RootPath}");

            return result;
        }

        result.Messages.Add($"Repository found: {options.RootPath}");

        return result;
    }
}
