using Clovent.Desktop.MasterData;

namespace Clovent.Desktop.MasterData.FiscalYears;

/// <summary>
/// Create/edit dialog for a Fiscal Year - name plus, for new fiscal years
/// only, start/end dates (immutable after creation - a fiscal year's period
/// cannot change once created, only its display name and open/closed state).
/// </summary>
public sealed partial class FiscalYearEditForm : MasterDataEditFormBase
{
    /// <summary>Design-time-only constructor for the Visual Studio WinForms Designer - never used at runtime.</summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [Obsolete("Designer only", true)]
    public FiscalYearEditForm() : base("Edit Fiscal Year")
    {
        InitializeComponent();
        }

    /// <summary>
    /// Builds the dialog. <paramref name="title"/> is the dialog's caption;
    /// <paramref name="name"/> pre-populates the display name. Pass
    /// <paramref name="startDate"/>/<paramref name="endDate"/> when editing so
    /// the (disabled) fields still show them - both are only enabled when
    /// <paramref name="isNew"/> is <see langword="true"/>, since a fiscal
    /// year's period is immutable after creation.
    /// </summary>
    public FiscalYearEditForm(string title, string? name = null, DateOnly? startDate = null, DateOnly? endDate = null, bool isNew = true) : base(title)
    {
        InitializeComponent();
        if (Clovent.Desktop.Forms.Base.DesignModeHelper.IsInDesignMode)
            return;

        _nameEdit.Text = name ?? string.Empty;
        _startDateEdit.DateTime = (startDate ?? DateOnly.FromDateTime(DateTime.Today)).ToDateTime(TimeOnly.MinValue);
        _endDateEdit.DateTime = (endDate ?? DateOnly.FromDateTime(DateTime.Today).AddYears(1)).ToDateTime(TimeOnly.MinValue);
        _startDateEdit.Enabled = isNew;
        _endDateEdit.Enabled = isNew;
    }



    /// <summary>The entered fiscal year name/label.</summary>
    public string FiscalYearNameValue => _nameEdit.Text.Trim();

    /// <summary>The entered start date (only meaningful when creating).</summary>
    public DateOnly StartDate => DateOnly.FromDateTime(_startDateEdit.DateTime);

    /// <summary>The entered end date (only meaningful when creating).</summary>
    public DateOnly EndDate => DateOnly.FromDateTime(_endDateEdit.DateTime);

    /// <inheritdoc/>
    protected override bool ValidateFields(out string error)
    {
        if (string.IsNullOrWhiteSpace(_nameEdit.Text))
        {
            error = "Name is required.";
            return false;
        }

        if (_startDateEdit.Enabled && EndDate <= StartDate)
        {
            error = "End date must be after start date.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    }
