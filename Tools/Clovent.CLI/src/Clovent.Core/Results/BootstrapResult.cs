namespace Clovent.Core.Results;

public sealed class BootstrapResult
{
    public bool Success { get; set; }

    public List<string> Messages { get; } = [];

    public List<string> Errors { get; } = [];
}
