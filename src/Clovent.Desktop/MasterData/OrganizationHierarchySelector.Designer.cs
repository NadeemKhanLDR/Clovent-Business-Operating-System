using DevExpress.XtraEditors;

namespace Clovent.Desktop.MasterData;

partial class OrganizationHierarchySelector
{
    /// <summary>Required designer variable.</summary>
    private System.ComponentModel.IContainer components = null;

    private readonly ComboBoxEdit _organizationCombo = new();
    private readonly ComboBoxEdit _companyCombo = new();
    private readonly ComboBoxEdit _branchCombo = new();
    private readonly TableLayoutPanel _layout = new();

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
        _layout.SuspendLayout();
        SuspendLayout();
        //
        // _organizationCombo
        //
        _organizationCombo.Name = "_organizationCombo";
        _organizationCombo.Width = 260;
        _organizationCombo.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;
        _organizationCombo.Properties.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
        _organizationCombo.Properties.Appearance.Options.UseTextOptions = true;
        _organizationCombo.Properties.AppearanceDropDown.Font = new System.Drawing.Font("Segoe UI", 9.5F);
        _organizationCombo.Properties.AppearanceDropDown.Options.UseFont = true;
        _organizationCombo.Properties.DropDownRows = 10;
        _organizationCombo.SelectedIndexChanged += OrganizationCombo_SelectedIndexChanged;
        _organizationCombo.Anchor = AnchorStyles.Left;
        _organizationCombo.Margin = new Padding(0, 0, 12, 0);
        //
        // _companyCombo
        //
        _companyCombo.Name = "_companyCombo";
        _companyCombo.Width = 260;
        _companyCombo.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;
        _companyCombo.Properties.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
        _companyCombo.Properties.Appearance.Options.UseTextOptions = true;
        _companyCombo.Properties.AppearanceDropDown.Font = new System.Drawing.Font("Segoe UI", 9.5F);
        _companyCombo.Properties.AppearanceDropDown.Options.UseFont = true;
        _companyCombo.Properties.DropDownRows = 10;
        _companyCombo.SelectedIndexChanged += CompanyCombo_SelectedIndexChanged;
        _companyCombo.Anchor = AnchorStyles.Left;
        _companyCombo.Margin = new Padding(0, 0, 12, 0);
        //
        // _branchCombo
        //
        _branchCombo.Name = "_branchCombo";
        _branchCombo.Width = 260;
        _branchCombo.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;
        _branchCombo.Properties.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
        _branchCombo.Properties.Appearance.Options.UseTextOptions = true;
        _branchCombo.Properties.AppearanceDropDown.Font = new System.Drawing.Font("Segoe UI", 9.5F);
        _branchCombo.Properties.AppearanceDropDown.Options.UseFont = true;
        _branchCombo.Properties.DropDownRows = 10;
        _branchCombo.SelectedIndexChanged += BranchCombo_SelectedIndexChanged;
        _branchCombo.Anchor = AnchorStyles.Left;
        _branchCombo.Margin = Padding.Empty;
        //
        // _layout
        //
        _layout.Dock = DockStyle.Fill;
        _layout.ColumnCount = 6;
        _layout.RowCount = 1;
        _layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        _layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        _layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        _layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        _layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        _layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        _layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        _layout.AutoSize = true;
        _layout.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        _layout.Name = "_layout";
        _layout.SizeChanged += Layout_SizeChanged;

        var lblOrg = new LabelControl
        {
            Text = "Organization:",
            Anchor = AnchorStyles.Left,
            Margin = new Padding(0, 0, 4, 0),
            AutoSizeMode = LabelAutoSizeMode.Horizontal
        };
        lblOrg.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
        lblOrg.Appearance.Options.UseTextOptions = true;
        _layout.Controls.Add(lblOrg, 0, 0);
        _layout.Controls.Add(_organizationCombo, 1, 0);

        if (_showCompany)
        {
            var lblComp = new LabelControl
            {
                Text = "Company:",
                Anchor = AnchorStyles.Left,
                Margin = new Padding(0, 0, 4, 0),
                AutoSizeMode = LabelAutoSizeMode.Horizontal
            };
            lblComp.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
            lblComp.Appearance.Options.UseTextOptions = true;
            _layout.Controls.Add(lblComp, 2, 0);
            _layout.Controls.Add(_companyCombo, 3, 0);
        }

        if (_showBranch)
        {
            var lblBranch = new LabelControl
            {
                Text = "Branch:",
                Anchor = AnchorStyles.Left,
                Margin = new Padding(0, 0, 4, 0),
                AutoSizeMode = LabelAutoSizeMode.Horizontal
            };
            lblBranch.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
            lblBranch.Appearance.Options.UseTextOptions = true;
            _layout.Controls.Add(lblBranch, 4, 0);
            _layout.Controls.Add(_branchCombo, 5, 0);
        }

        //
        // OrganizationHierarchySelector
        //
        Dock = DockStyle.Top;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        Name = "OrganizationHierarchySelector";
        Controls.Add(_layout);
        _layout.ResumeLayout(false);
        _layout.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)_organizationCombo.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_companyCombo.Properties).EndInit();
        ((System.ComponentModel.ISupportInitialize)_branchCombo.Properties).EndInit();
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion
}
