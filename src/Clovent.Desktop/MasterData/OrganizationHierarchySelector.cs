using Clovent.Identity.Application.Branches.Queries;
using Clovent.Identity.Application.Companies.Queries;
using Clovent.Identity.Application.Organizations.Queries;
using DevExpress.XtraEditors;
using MediatR;

namespace Clovent.Desktop.MasterData;

/// <summary>
/// Cascading Organization -&gt; Company -&gt; Branch picker shared by every
/// screen whose entities are scoped under one of those three levels
/// (Company needs Organization; Branch/Department/Warehouse/Terminal need
/// Company; Fiscal Year/Business Settings need only Organization). Only the
/// combos a screen actually needs are shown - the others stay hidden and
/// their corresponding Selected*Id stays <see langword="null"/>.
/// </summary>
public sealed class OrganizationHierarchySelector : XtraUserControl
{
    private readonly IMediator _mediator;
    private readonly bool _showCompany;
    private readonly bool _showBranch;

    private readonly ComboBoxEdit _organizationCombo = new();
    private readonly ComboBoxEdit _companyCombo = new();
    private readonly ComboBoxEdit _branchCombo = new();

    private readonly Dictionary<string, Guid> _organizationsByDisplay = [];
    private readonly Dictionary<string, Guid> _companiesByDisplay = [];
    private readonly Dictionary<string, Guid> _branchesByDisplay = [];

    /// <summary>Raised whenever the deepest required selection level settles on a concrete value (or is cleared).</summary>
    public event EventHandler? SelectionChanged;

    /// <summary>The selected organization, or <see langword="null"/> if none is selected yet.</summary>
    public Guid? SelectedOrganizationId { get; private set; }

    /// <summary>The selected company (only meaningful when the Company combo is shown), or <see langword="null"/>.</summary>
    public Guid? SelectedCompanyId { get; private set; }

    /// <summary>The selected branch (only meaningful when the Branch combo is shown), or <see langword="null"/>.</summary>
    public Guid? SelectedBranchId { get; private set; }

    /// <summary>Builds the selector, showing only the combos the caller's screen needs.</summary>
    /// <param name="mediator">Used to query organizations/companies/branches as the user drills down.</param>
    /// <param name="showCompany">Whether to show the Company combo (implies Organization).</param>
    /// <param name="showBranch">Whether to show the Branch combo (implies Organization and Company).</param>
    public OrganizationHierarchySelector(IMediator mediator, bool showCompany, bool showBranch)
    {
        _mediator = mediator;
        _showCompany = showCompany || showBranch;
        _showBranch = showBranch;

        Dock = DockStyle.Top;
        Height = 32;

        var layout = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = false };

        _organizationCombo.Width = 220;
        _organizationCombo.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;
        layout.Controls.Add(new LabelControl { Text = "Organization:", Padding = new Padding(0, 6, 4, 0) });
        layout.Controls.Add(_organizationCombo);

        if (_showCompany)
        {
            _companyCombo.Width = 220;
            _companyCombo.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;
            layout.Controls.Add(new LabelControl { Text = "Company:", Padding = new Padding(12, 6, 4, 0) });
            layout.Controls.Add(_companyCombo);
        }

        if (_showBranch)
        {
            _branchCombo.Width = 220;
            _branchCombo.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;
            layout.Controls.Add(new LabelControl { Text = "Branch:", Padding = new Padding(12, 6, 4, 0) });
            layout.Controls.Add(_branchCombo);
        }

        Controls.Add(layout);

        _organizationCombo.SelectedIndexChanged += async (_, _) => await OnOrganizationSelectedAsync();
        _companyCombo.SelectedIndexChanged += async (_, _) => await OnCompanySelectedAsync();
        _branchCombo.SelectedIndexChanged += (_, _) => OnBranchSelected();
    }

    /// <summary>Loads every organization into the top-level combo. Call once when the hosting screen loads.</summary>
    public async Task LoadOrganizationsAsync(CancellationToken cancellationToken = default)
    {
        var organizations = await _mediator.Send(new ListOrganizationsQuery(), cancellationToken);

        _organizationsByDisplay.Clear();
        _organizationCombo.Properties.Items.Clear();
        foreach (var organization in organizations)
        {
            _organizationsByDisplay[organization.Name] = organization.OrganizationId;
            _organizationCombo.Properties.Items.Add(organization.Name);
        }

        if (_organizationCombo.Properties.Items.Count > 0)
        {
            _organizationCombo.SelectedIndex = 0;
        }
        else
        {
            await OnOrganizationSelectedAsync();
        }
    }

    private async Task OnOrganizationSelectedAsync()
    {
        SelectedOrganizationId = _organizationCombo.SelectedItem is string display && _organizationsByDisplay.TryGetValue(display, out var id)
            ? id
            : null;

        if (_showCompany)
        {
            await LoadCompaniesAsync();
        }
        else
        {
            RaiseSelectionChanged();
        }
    }

    private async Task LoadCompaniesAsync()
    {
        _companiesByDisplay.Clear();
        _companyCombo.Properties.Items.Clear();

        if (SelectedOrganizationId is { } organizationId)
        {
            var companies = await _mediator.Send(new ListCompaniesByOrganizationQuery(organizationId));
            foreach (var company in companies)
            {
                _companiesByDisplay[company.Name] = company.CompanyId;
                _companyCombo.Properties.Items.Add(company.Name);
            }
        }

        if (_companyCombo.Properties.Items.Count > 0)
        {
            _companyCombo.SelectedIndex = 0;
        }
        else
        {
            await OnCompanySelectedAsync();
        }
    }

    private async Task OnCompanySelectedAsync()
    {
        SelectedCompanyId = _companyCombo.SelectedItem is string display && _companiesByDisplay.TryGetValue(display, out var id)
            ? id
            : null;

        if (_showBranch)
        {
            await LoadBranchesAsync();
        }
        else
        {
            RaiseSelectionChanged();
        }
    }

    private async Task LoadBranchesAsync()
    {
        _branchesByDisplay.Clear();
        _branchCombo.Properties.Items.Clear();

        if (SelectedCompanyId is { } companyId)
        {
            var branches = await _mediator.Send(new ListBranchesByCompanyQuery(companyId));
            foreach (var branch in branches)
            {
                _branchesByDisplay[branch.Name] = branch.BranchId;
                _branchCombo.Properties.Items.Add(branch.Name);
            }
        }

        if (_branchCombo.Properties.Items.Count > 0)
        {
            _branchCombo.SelectedIndex = 0;
        }
        else
        {
            OnBranchSelected();
        }
    }

    private void OnBranchSelected()
    {
        SelectedBranchId = _branchCombo.SelectedItem is string display && _branchesByDisplay.TryGetValue(display, out var id)
            ? id
            : null;

        RaiseSelectionChanged();
    }

    private void RaiseSelectionChanged() => SelectionChanged?.Invoke(this, EventArgs.Empty);
}
