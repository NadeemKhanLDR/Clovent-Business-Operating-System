using Clovent.Platform.Execution;

namespace Clovent.MasterData.Application.Tests.TestSupport;

public sealed class FakeExecutionContextAccessor(IExecutionContext? current = null) : IExecutionContextAccessor
{
    public IExecutionContext? Current { get; set; } = current;
}
