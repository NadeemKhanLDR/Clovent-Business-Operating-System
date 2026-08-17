using DevExpress.XtraEditors;

namespace Clovent.Desktop.MasterData;

partial class OrganizationHierarchySelector
{
    /// <summary>Required designer variable.</summary>
    private System.ComponentModel.IContainer components = null;

    private readonly ComboBoxEdit _organizationCombo = new();
    private readonly ComboBoxEdit _companyCombo = new();
    private readonly ComboBoxEdit _branchCombo = new();
    private readonly FlowLayoutPanel _layout = new();

    /// <summary>Clean up any resources being used.</summary>
    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }

        base.Dispose(disposing);
    }

    #region Component Designer generated code

    /// <summary>
    /// Required method for Designer support - do not modify the contents of
    /// this method with the code editor. Which combos get added below
    /// depends on <see cref="_showCompany"/>/<see cref="_showBranch"/> - both
    /// already set by the constructor before this runs - since a WinForms
    /// Designer surface has no way to express "only some of these controls
    /// exist depending on how the caller constructed this control".
    /// </summary>
    private void InitializeComponent()
    {
        ((System.ComponentModel.ISupportInitialize)_organizationCombo.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_companyCombo.Properties).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_branchCombo.Properties).BeginInit();
        SuspendLayout();
        //
        // _organizationCombo
        //
        _organizationCombo.Name = "_organizationCombo";
        _organizationCombo.Width = 220;
        _organizationCombo.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;
        _organizationCombo.SelectedIndexChanged += OrganizationCombo_SelectedIndexChanged;
        //
        // _companyCombo
        //
        _companyCombo.Name = "_companyCombo";
        _companyCombo.Width = 220;
        _companyCombo.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;
        _companyCombo.SelectedIndexChanged += CompanyCombo_SelectedIndexChanged;
        //
        // _branchCombo
        //
        _branchCombo.Name = "_branchCombo";
        _branchCombo.Width = 220;
        _branchCombo.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;
        _branchCombo.SelectedIndexChanged += BranchCombo_SelectedIndexChanged;
        //
        // _layout
        //
        _layout.Dock = DockStyle.Fill;
        _layout.FlowDirection = FlowDirection.LeftToRight;
        _layout.WrapContents = false;
        _layout.AutoSize = true;
        _layout.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        _layout.Name = "_layout";
        _layout.SizeChanged += Layout_SizeChanged;

        _layout.Controls.Add(new LabelControl { Text = "Organization:", Padding = new Padding(0, 6, 4, 0) });
        _layout.Controls.Add(_organizationCombo);

        if (_showCompany)
        {
            _layout.Controls.Add(new LabelControl { Text = "Company:", Padding = new Padding(12, 6, 4, 0) });
            _layout.Controls.Add(_companyCombo);
        }

        if (_showBranch)
        {
            _layout.Controls.Add(new LabelControl { Text = "Branch:", Padding = new Padding(12, 6, 4, 0) });
            _layout.Controls.Add(_branchCombo);
        }

        //
        // OrganizationHierarchySelector
        //
        // AutoSize (not a fixed Height) so this control always fits its
        // combos'/labels' actual rendered height - a fixed Height=32 was,
        // like EntityPicker's and MasterDataListView's toolbar's own fixed
        // heights before them, measured too short for a DevExpress
        // ComboBoxEdit's real height under some DPI/skin combinations,
        // clipping/overlapping whatever is docked below it.
        Dock = DockStyle.Top;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        Name = "OrganizationHierarchySelector";
        Controls.Add(_layout);
        ((System.ComponentModel.ISupportInitialize)_organizationCombo.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_companyCombo.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_branchCombo.Properties).EndInit();
        ResumeLayout(false);
    }

    #endregion
}
