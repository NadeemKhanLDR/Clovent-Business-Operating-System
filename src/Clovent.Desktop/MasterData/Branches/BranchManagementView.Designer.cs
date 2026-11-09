namespace Clovent.Desktop.MasterData.Branches;

partial class BranchManagementView
{
    private System.ComponentModel.IContainer components = null;
    private OrganizationHierarchySelector _selector;
    private BranchListView _listView;

    private void InitializeComponent()
    {
        _selector = new OrganizationHierarchySelector();
        _listView = new BranchListView();
        SuspendLayout();
        _selector.Dock = System.Windows.Forms.DockStyle.Top;
        _selector.Name = "_selector";
        _selector.SelectionChanged += Selector_SelectionChanged;
        _listView.Dock = System.Windows.Forms.DockStyle.Fill;
        _listView.Name = "_listView";
        Controls.Add(_listView);
        Controls.Add(_selector);
        Dock = System.Windows.Forms.DockStyle.Fill;
        Name = "BranchManagementView";
        Size = new System.Drawing.Size(1000, 600);
        Load += BranchManagementView_Load;
        ResumeLayout(false);
        PerformLayout();
    }
}
