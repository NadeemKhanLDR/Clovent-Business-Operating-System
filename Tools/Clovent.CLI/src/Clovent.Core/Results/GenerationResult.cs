namespace Clovent.Core.Results;

public sealed class GenerationResult
{
    public bool Success { get; set; }
    public List<string> GeneratedFiles { get; } = new();
    public List<string> Messages { get; } = new();
    public List<string> Errors { get; } = new();

    public static GenerationResult Ok(IEnumerable<string>? files = null, string? message = null)
    {
        var result = new GenerationResult { Success = true };
        if (files != null) result.GeneratedFiles.AddRange(files);
        if (!string.IsNullOrEmpty(message)) result.Messages.Add(message);
        return result;
    }

    public static GenerationResult Fail(string error)
    {
        var result = new GenerationResult { Success = false };
        result.Errors.Add(error);
        return result;
    }
}
