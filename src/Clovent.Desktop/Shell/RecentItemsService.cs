namespace Clovent.Desktop.Shell;

/// <summary><see cref="IRecentItemsService"/> implementation - a process-wide singleton, most-recent-first, capped, de-duplicated.</summary>
public sealed class RecentItemsService : IRecentItemsService
{
    private const int MaxItems = 5;

    private readonly List<string> _recentCompanies = [];
    private readonly List<string> _recentBranches = [];

    /// <inheritdoc/>
    public IReadOnlyList<string> RecentCompanies => _recentCompanies.AsReadOnly();

    /// <inheritdoc/>
    public IReadOnlyList<string> RecentBranches => _recentBranches.AsReadOnly();

    /// <inheritdoc/>
    public void RecordCompanySelected(string companyName) => Record(_recentCompanies, companyName);

    /// <inheritdoc/>
    public void RecordBranchSelected(string branchName) => Record(_recentBranches, branchName);

    private static void Record(List<string> list, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        list.RemoveAll(existing => string.Equals(existing, name, StringComparison.OrdinalIgnoreCase));
        list.Insert(0, name);

        if (list.Count > MaxItems)
        {
            list.RemoveRange(MaxItems, list.Count - MaxItems);
        }
    }
}
