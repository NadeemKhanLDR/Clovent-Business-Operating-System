namespace Clovent.Core.Interfaces;

using Clovent.Core.Models;
using Clovent.Core.Results;

public interface IBootstrapService
{
    BootstrapResult Execute(BootstrapOptions options);
}
