using Clovent.Identity.Application.Branches.Dtos;

namespace Clovent.Desktop.MasterData.Branches;

/// <summary>
/// Concrete, service-free composition of the production master-data toolbar
/// and grid. The owning view binds runtime operations after creating its scope.
/// </summary>
[System.ComponentModel.DesignerCategory("Code")]
public sealed class BranchListView : MasterDataListView<BranchDto>
{
    /// <summary>Builds the same branch columns for runtime and the containing Designer.</summary>
    public BranchListView() : base(
    [
        new MasterDataColumn("Name", "Name", 200),
        new MasterDataColumn("City", "City", 120),
        new MasterDataColumn("Country", "Country", 120),
        new MasterDataColumn("Status", "Status", 90),
        new MasterDataColumn("CreatedAtUtc", "Created (UTC)", 160),
    ]) { }
}
