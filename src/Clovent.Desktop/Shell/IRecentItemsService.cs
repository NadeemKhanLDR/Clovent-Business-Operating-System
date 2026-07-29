namespace Clovent.Desktop.Shell;

/// <summary>
/// The "Recent companies"/"Recent branches" deliverable. No Organization/Company/Branch
/// persistence exists anywhere in this solution (deliberately out of scope -
/// see <c>Authorization.md</c> and <c>AuthenticationIntegration.md</c>), so
/// this tracks plain display-name strings rather than referencing a domain
/// aggregate that doesn't exist yet - a future milestone that adds real
/// Organization/Company/Branch persistence would call
/// <see cref="RecordCompanySelected"/>/<see cref="RecordBranchSelected"/>
/// with the real entity's display name.
/// </summary>
public interface IRecentItemsService
{
    /// <summary>The most recently selected companies, most recent first, capped at a small fixed size.</summary>
    IReadOnlyList<string> RecentCompanies { get; }

    /// <summary>The most recently selected branches, most recent first, capped at a small fixed size.</summary>
    IReadOnlyList<string> RecentBranches { get; }

    /// <summary>Records that a company was selected, moving it to the front of <see cref="RecentCompanies"/>.</summary>
    void RecordCompanySelected(string companyName);

    /// <summary>Records that a branch was selected, moving it to the front of <see cref="RecentBranches"/>.</summary>
    void RecordBranchSelected(string branchName);
}
