using Clovent.Desktop.Forms.Base;
using Clovent.Desktop.MasterData;
using DevExpress.XtraEditors;
using DevExpress.XtraEditors.Repository;

namespace Clovent.Desktop.Restaurant.SmartPos;

/// <summary>
/// Create/edit dialog for a Smart POS recommendation rule: an optional trigger
/// product (empty = any basket), the recommended product/variant, priority,
/// activation, an optional daily time window, optional day-of-week limits and
/// free-text notes.
/// </summary>
public sealed class RecommendationRuleEditForm : MasterDataEditFormBase
{
    private readonly LookUpEdit _triggerProductEdit = new();
    private readonly LookUpEdit _recommendedVariantEdit = new();
    private readonly SpinEdit _priorityEdit = new();
    private readonly CheckEdit _activeCheck = new();
    private readonly TimeEdit _startTimeEdit = new();
    private readonly TimeEdit _endTimeEdit = new();
    private readonly FlowLayoutPanel _daysPanel = new();
    private readonly MemoEdit _notesEdit = new();
    private readonly CheckEdit[] _dayChecks;

    /// <summary>Builds the dialog for a new rule (or an existing one when its <c>existing*</c> arguments are supplied).</summary>
    public RecommendationRuleEditForm(
        string title,
        IReadOnlyList<ProductOptionRow> variantOptions,
        IReadOnlyList<ProductOptionRowSummary> productOptions,
        Guid? existingTriggerProductId = null,
        Guid? existingRecommendedVariantId = null,
        int existingPriority = 1,
        bool existingIsActive = true,
        TimeSpan? existingStartTime = null,
        TimeSpan? existingEndTime = null,
        int? existingDaysOfWeek = null,
        string? existingNotes = null) : base(title)
    {
        ConfigureTriggerProduct(productOptions, existingTriggerProductId);
        ConfigureRecommendedVariant(variantOptions, existingRecommendedVariantId ?? Guid.Empty);
        ConfigurePriority(existingPriority);
        ConfigureActive(existingIsActive);
        ConfigureTimeWindow(existingStartTime, existingEndTime);
        _dayChecks = ConfigureDays(existingDaysOfWeek);
        ConfigureNotes(existingNotes);

        AddField("Trigger Product (empty = any basket)", _triggerProductEdit);
        AddField("Recommended Product / Variant *", _recommendedVariantEdit);
        AddField("Priority (lower runs first)", _priorityEdit);
        AddField("", _activeCheck);
        AddField("Time Window (optional)", CreateTimeWindowPanel(), fixedHeight: 34);
        AddField("Days (all unchecked = every day)", _daysPanel, fixedHeight: 34);
        AddField("Notes", _notesEdit, fixedHeight: 80);
        SetFixedRowHeight(_notesEdit, 80);

        // Professional action button styling and ordering: [ Cancel ] [ Save Changes ]
        DialogOkButton.Text = "Save Changes";
        DialogOkButton.Appearance.BackColor = Color.FromArgb(13, 148, 136);
        DialogOkButton.Appearance.ForeColor = Color.White;
        DialogOkButton.Appearance.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        DialogOkButton.Appearance.Options.UseBackColor = true;
        DialogOkButton.Appearance.Options.UseForeColor = true;
        DialogOkButton.Appearance.Options.UseFont = true;

        DialogCancelButton.Text = "Cancel";

        DialogButtonPanel.Controls.SetChildIndex(DialogOkButton, 0);
        DialogButtonPanel.Controls.SetChildIndex(DialogCancelButton, 1);
    }

    /// <inheritdoc/>
    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        DesktopDialogSizing.Apply(this, 640, 580, 540, 480, null, true);
    }

    /// <summary>The chosen trigger product id, or <see langword="null"/> for "any basket".</summary>
    public Guid? TriggerProductIdValue =>
        _triggerProductEdit.EditValue is Guid id && id != Guid.Empty ? id : null;

    /// <summary>The chosen recommended variant id (never empty - validation enforces it).</summary>
    public Guid RecommendedVariantIdValue => (Guid)_recommendedVariantEdit.EditValue!;

    /// <summary>The entered priority (1 or higher).</summary>
    public int PriorityValue => (int)_priorityEdit.Value;

    /// <summary>Whether the rule is active.</summary>
    public bool IsActiveValue => _activeCheck.Checked;

    /// <summary>The optional daily window start, or <see langword="null"/>.</summary>
    public TimeSpan? StartTimeValue => ToTimeSpan(_startTimeEdit);

    /// <summary>The optional daily window end, or <see langword="null"/>.</summary>
    public TimeSpan? EndTimeValue => ToTimeSpan(_endTimeEdit);

    /// <summary>The day-of-week bitmask (bit <c>d</c> = <see cref="DayOfWeek"/> value <c>d</c>), or <see langword="null"/> for every day.</summary>
    public int? DaysOfWeekValue
    {
        get
        {
            var mask = 0;
            for (var i = 0; i < _dayChecks.Length; i++)
            {
                if (_dayChecks[i].Checked)
                {
                    mask |= 1 << i;
                }
            }

            return mask == AllDaysMask ? null : mask;
        }
    }

    /// <summary>The optional free-text notes.</summary>
    public string? NotesValue => string.IsNullOrWhiteSpace(_notesEdit.Text) ? null : _notesEdit.Text.Trim();

    private const int AllDaysMask = 0b0111_1111;

    private static TimeSpan? ToTimeSpan(TimeEdit editor) =>
        editor.EditValue is DateTime time ? time.TimeOfDay : null;

    private void ConfigureTriggerProduct(IReadOnlyList<ProductOptionRowSummary> productOptions, Guid? existing)
    {
        ConfigurePicker(_triggerProductEdit, productOptions, "ProductName", "ProductId");
        _triggerProductEdit.EditValue = existing ?? Guid.Empty;
    }

    private void ConfigureRecommendedVariant(IReadOnlyList<ProductOptionRow> variantOptions, Guid existing)
    {
        ConfigurePicker(_recommendedVariantEdit, variantOptions, "Display", "VariantId");
        _recommendedVariantEdit.EditValue = existing;
    }

    private static void ConfigurePicker(LookUpEdit editor, object dataSource, string displayMember, string valueMember)
    {
        editor.Properties.DataSource = dataSource;
        editor.Properties.DisplayMember = displayMember;
        editor.Properties.ValueMember = valueMember;
        editor.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;
        editor.Properties.NullText = "(None)";
        editor.Properties.AllowNullInput = DevExpress.Utils.DefaultBoolean.False;
        editor.Dock = System.Windows.Forms.DockStyle.Fill;
        editor.Font = new Font("Segoe UI", 9.5F);
    }

    private void ConfigurePriority(int existing)
    {
        _priorityEdit.Properties.IsFloatValue = false;
        _priorityEdit.Properties.MinValue = 1;
        _priorityEdit.Properties.MaxValue = 9999;
        _priorityEdit.EditValue = existing;
        _priorityEdit.Dock = System.Windows.Forms.DockStyle.Fill;
        _priorityEdit.Font = new Font("Segoe UI", 9.5F);
    }

    private void ConfigureActive(bool existing)
    {
        _activeCheck.Text = "Active";
        _activeCheck.Checked = existing;
        _activeCheck.Font = new Font("Segoe UI", 9.5F);
    }

    private void ConfigureTimeWindow(TimeSpan? start, TimeSpan? end)
    {
        ConfigureTimeEditor(_startTimeEdit, start);
        ConfigureTimeEditor(_endTimeEdit, end);
    }

    private static void ConfigureTimeEditor(TimeEdit editor, TimeSpan? value)
    {
        editor.Properties.AllowNullInput = DevExpress.Utils.DefaultBoolean.True;
        editor.Properties.NullText = "(none)";
        editor.Properties.Mask.UseMaskAsDisplayFormat = true;
        editor.EditValue = value is { } time ? (DateTime?)DateTime.Today.Add(time) : null;
        editor.Dock = System.Windows.Forms.DockStyle.Fill;
        editor.Font = new Font("Segoe UI", 9.5F);
    }

    private System.Windows.Forms.Control CreateTimeWindowPanel()
    {
        var panel = new System.Windows.Forms.TableLayoutPanel
        {
            Dock = System.Windows.Forms.DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 1,
            Margin = new Padding(0)
        };
        panel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.AutoSize));
        panel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
        panel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.AutoSize));
        panel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));

        var lblFrom = new LabelControl
        {
            Text = "From",
            Padding = new Padding(0, 4, 8, 0),
            Appearance = { Font = new Font("Segoe UI", 9F) }
        };
        var lblTo = new LabelControl
        {
            Text = "To",
            Padding = new Padding(12, 4, 8, 0),
            Appearance = { Font = new Font("Segoe UI", 9F) }
        };

        panel.Controls.Add(lblFrom, 0, 0);
        panel.Controls.Add(_startTimeEdit, 1, 0);
        panel.Controls.Add(lblTo, 2, 0);
        panel.Controls.Add(_endTimeEdit, 3, 0);
        return panel;
    }

    private CheckEdit[] ConfigureDays(int? existingDays)
    {
        var names = new[] { "Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat" };
        var checks = new CheckEdit[7];

        var table = new System.Windows.Forms.TableLayoutPanel
        {
            Dock = System.Windows.Forms.DockStyle.Fill,
            ColumnCount = 7,
            RowCount = 1,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };
        for (var i = 0; i < 7; i++)
        {
            table.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 14.28F));
        }

        for (var i = 0; i < 7; i++)
        {
            checks[i] = new CheckEdit
            {
                Text = names[i],
                Checked = existingDays is null || (existingDays.Value & (1 << i)) != 0,
                Font = new Font("Segoe UI", 9F),
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 2, 4, 2)
            };
            table.Controls.Add(checks[i], i, 0);
        }

        _daysPanel.Controls.Clear();
        _daysPanel.Controls.Add(table);
        _daysPanel.Dock = System.Windows.Forms.DockStyle.Fill;
        table.Dock = System.Windows.Forms.DockStyle.Fill;

        return checks;
    }

    private void ConfigureNotes(string? existing)
    {
        _notesEdit.Text = existing ?? string.Empty;
        _notesEdit.Dock = System.Windows.Forms.DockStyle.Fill;
        _notesEdit.Font = new Font("Segoe UI", 9.5F);
        _notesEdit.Properties.ScrollBars = ScrollBars.Vertical;
        _notesEdit.Properties.WordWrap = true;
    }

    /// <inheritdoc/>
    protected override bool ValidateFields(out string error)
    {
        if (_recommendedVariantEdit.EditValue is not Guid recommended || recommended == Guid.Empty)
        {
            error = "A recommended product/variant is required.";
            return false;
        }

        if (_priorityEdit.Value < 1)
        {
            error = "Priority must be 1 or higher.";
            return false;
        }

        var start = StartTimeValue;
        var end = EndTimeValue;
        if ((start is null) != (end is null))
        {
            error = "Set both the start and end of the time window, or leave both empty.";
            return false;
        }

        if (start is { } s && end is { } e && s >= e)
        {
            error = "The time window's start must be earlier than its end.";
            return false;
        }

        error = string.Empty;
        return true;
    }
}
