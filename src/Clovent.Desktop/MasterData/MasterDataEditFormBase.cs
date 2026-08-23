using System;
using System.Linq;
using System.Windows.Forms;
using System.Drawing;
using Clovent.Desktop.Forms.Base;
using DevExpress.XtraEditors;

namespace Clovent.Desktop.MasterData;

/// <summary>
/// Shared chrome for every master-data create/edit dialog: a two-column
/// label+editor content area plus OK/Cancel buttons, with a
/// <see cref="ValidateFields"/> hook a subclass overrides to block OK when
/// the entered data is invalid. Each entity has its own field set, so this
/// deliberately does not try to be generic over fields - only the chrome
/// (layout, buttons, validation gate) is shared, matching this milestone's
/// "reusable... edit-dialog pattern" ask. Control tree lives in
/// <c>MasterDataEditFormBase.Designer.cs</c>; this file holds behavior only.
/// </summary>
/// <remarks>
/// <b>Visual Studio Designer compatibility.</b> Declared as a plain (not
/// <c>abstract</c>) class with a <c>public</c> parameterless constructor
/// - never intended to be instantiated directly outside a subclass (every
/// real call site uses <c>new SomeEditForm(...)</c>), but the WinForms
/// Designer must be able to construct a class's immediate base type when
/// opening a derived form's designer surface, and it cannot do that if the
/// base is <c>abstract</c> or lacks a parameterless constructor - the same
/// reasoning already applied to <see cref="Forms.Base.BaseForm"/>.
/// </remarks>
[System.ComponentModel.DesignerCategory("Form")]
public partial class MasterDataEditFormBase : XtraForm
{
    private int _rowCount;

    /// <summary>
    /// Designer-only constructor - required for the Visual Studio WinForms
    /// Designer to construct this type when it is the immediate base class
    /// of whichever <c>*EditForm</c> is being designed. Never used at
    /// runtime: every real subclass constructor calls <c>base(title)</c>.
    /// </summary>
    public MasterDataEditFormBase()
    {
        InitializeComponent();
    }

    protected MasterDataEditFormBase(string title)
    {
        InitializeComponent();
        Text = title;
    }



    /// <summary>
    /// Whether the dialog was closed via "Save &amp; New" rather than plain
    /// OK/Save. Both close with <see cref="DialogResult.OK"/> - a caller
    /// that wants the "immediately reopen a fresh dialog for the next
    /// record" behavior (<c>MenuItemsForm</c>'s "New Menu Item") checks this
    /// after <c>ShowDialog</c> returns <see cref="DialogResult.OK"/>; every
    /// other existing caller ignores it and behaves exactly as before.
    /// </summary>
    protected bool SavedAndNew { get; private set; }

    /// <summary>
    /// Opts this dialog into a third "Save &amp; New" button, shown between
    /// Cancel and OK/Save. Not enabled by default - every existing
    /// <c>*EditForm</c> keeps its plain OK/Cancel shell unchanged unless it
    /// explicitly calls this (typically from its own constructor, after the
    /// base constructor returns). <paramref name="saveCaption"/> lets the
    /// caller relabel the OK button itself (e.g. "Save" instead of "OK") to
    /// match the "Save / Save &amp; New / Cancel" wording a modern dialog
    /// uses instead of a generic OK/Cancel pair.
    /// </summary>
    protected void EnableSaveAndNew(string saveCaption = "Save")
    {
        _okButton.Text = saveCaption;
        _buttonPanel.Controls.Add(_saveAndNewButton);
    }

    private void TryClose(bool savedAndNew)
    {
        if (ValidateFields(out var error))
        {
            SavedAndNew = savedAndNew;
            DialogResult = DialogResult.OK;
            Close();
        }
        else
        {
            XtraMessageBox.Show(this, error, "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void OkButton_Click(object? sender, EventArgs e) => TryClose(savedAndNew: false);

    private void SaveAndNewButton_Click(object? sender, EventArgs e) => TryClose(savedAndNew: true);

    /// <summary>
    /// Adds a labelled field row to the content area. <paramref name="fixedHeight"/>
    /// forces the row to an explicit height instead of the default AutoSize -
    /// required for any editor (e.g. a <c>CheckedListBoxControl</c> role/permission
    /// checklist) whose own <c>GetPreferredSize</c> doesn't reflect the
    /// <c>Height</c> the caller set on it: <see cref="TableLayoutPanel"/>'s
    /// AutoSize rows measure via <c>GetPreferredSize</c>, not the control's
    /// <c>Height</c> property, so an AutoSize row silently collapses such a
    /// control to a sliver regardless of what <c>Height</c> was assigned -
    /// discovered via manual verification collapsing <c>UserEditForm</c>'s
    /// Roles field and <c>RoleEditForm</c>'s Permissions field to nothing.
    /// </summary>
    protected void AddField(string label, Control editor, int? fixedHeight = null)
    {
        _contentPanel.RowCount = _rowCount + 1;
        _contentPanel.RowStyles.Add(fixedHeight is { } height ? new RowStyle(SizeType.Absolute, height) : new RowStyle(SizeType.AutoSize));

        var labelControl = new LabelControl { Text = label, Padding = new Padding(0, 6, 8, 0) };
        // CheckEdit's own caption (not the row's label, usually empty for a
        // checkbox field) needs room for its actual text, not the fixed
        // 260px every other editor uses - a live screenshot showed
        // "Tax-inclusive pricing" clipped at that width, and CheckEdit's
        // own AutoSize (tried first) clipped it even further, so this
        // measures the real caption text instead of guessing.
        if (editor is CheckEdit checkEdit)
        {
            var textWidth = TextRenderer.MeasureText(checkEdit.Text, checkEdit.Font).Width;
            checkEdit.Width = textWidth + 40;
        }
        else
        {
            editor.Width = 260;
        }

        editor.Margin = new Padding(0, 3, 0, 3);
        if (fixedHeight is { } fixedH)
        {
            editor.Height = fixedH - editor.Margin.Vertical;
            editor.Dock = DockStyle.Fill;
        }

        _contentPanel.Controls.Add(labelControl, 0, _rowCount);
        _contentPanel.Controls.Add(editor, 1, _rowCount);
        _rowCount++;
    }

    /// <summary>
    /// Converts the row containing <paramref name="editor"/> from AutoSize to
    /// an absolute <paramref name="height"/> - the runtime-layout twin of
    /// <see cref="AddField(string, Control, int?)"/>'s <c>fixedHeight</c>
    /// argument, for rows whose editors were added in a Designer file. Needed
    /// for editors whose <c>GetPreferredSize</c> ignores their assigned
    /// <c>Height</c> (<c>CheckedListBoxControl</c>, tall <c>MemoEdit</c>s):
    /// an AutoSize row measures via <c>GetPreferredSize</c> and collapses
    /// such a control to a sliver.
    /// </summary>
    protected void SetFixedRowHeight(Control editor, int height)
    {
        var row = _contentPanel.GetRow(editor);
        if (row < 0 || row >= _contentPanel.RowStyles.Count)
        {
            return;
        }

        _contentPanel.RowStyles[row] = new RowStyle(SizeType.Absolute, height);
        editor.Height = height - editor.Margin.Vertical;
        editor.Dock = DockStyle.Fill;
    }

    /// <summary>Validates every field before OK is allowed to close the dialog. The default accepts anything.</summary>
    /// <param name="error">The message to show the user when validation fails.</param>
    protected virtual bool ValidateFields(out string error)
    {
        error = string.Empty;
        return true;
    }

    /// <summary>
    /// Recomputes this dialog's real size from its subclass's actual field
    /// content - see this class's constructor for why the InitializeComponent
    /// placeholder Width/Height alone are not enough.
    /// </summary>
    private void MasterDataEditFormBase_Load(object? sender, EventArgs e)
    {
        if (Clovent.Desktop.Forms.Base.DesignModeHelper.IsInDesignMode) return;

        Localization.LocalizationHelper.LocalizeControl(this);

        // Ensure proper Z-order docking layout so Dock=Fill content takes remaining space correctly.
        _contentPanel.SendToBack();
        _buttonPanel.BringToFront();
        foreach (Control c in Controls.Cast<Control>().ToList())
        {
            if (c != _contentPanel && c != _buttonPanel && c.Dock is DockStyle.Top or DockStyle.Bottom)
            {
                c.BringToFront();
            }
        }

        // Convert any default percent-based row styles to AutoSize so fields don't stretch/align poorly,
        // but ignore empty spacer rows (rows that have no controls at all in any column).
        for (int i = 0; i < _contentPanel.RowStyles.Count; i++)
        {
            var style = _contentPanel.RowStyles[i];
            if (style.SizeType == SizeType.Percent)
            {
                bool isRowEmpty = true;
                for (int col = 0; col < _contentPanel.ColumnCount; col++)
                {
                    if (_contentPanel.GetControlFromPosition(col, i) != null)
                    {
                        isRowEmpty = false;
                        break;
                    }
                }

                if (!isRowEmpty)
                {
                    style.SizeType = SizeType.AutoSize;
                }
            }
        }

        // Force layout initialization before sizing calculations
        _contentPanel.PerformLayout();
        _buttonPanel.PerformLayout();
        this.PerformLayout();

        var labelColumnWidth = 0;
        var maxEditorWidth = 260;
        var maxSpan2Width = 0;

        foreach (Control control in _contentPanel.Controls)
        {
            var col = _contentPanel.GetColumn(control);
            var colSpan = _contentPanel.GetColumnSpan(control);
            var prefSize = control.GetPreferredSize(Size.Empty);

            if (colSpan == 2)
            {
                maxSpan2Width = Math.Max(maxSpan2Width, prefSize.Width);
            }
            else if (col == 0)
            {
                labelColumnWidth = Math.Max(labelColumnWidth, prefSize.Width);
            }
            else if (col == 1)
            {
                maxEditorWidth = Math.Max(maxEditorWidth, prefSize.Width);
            }
        }

        var wantedClientWidth = Math.Max(labelColumnWidth + maxEditorWidth, maxSpan2Width) + _contentPanel.Padding.Horizontal + 48;

        // The button strip's own preferred width (three AutoSize buttons plus
        // its padding) is a floor too - a narrow field set must not size the
        // dialog narrower than its own buttons ("Save & New"/Save/Cancel
        // squeezed against the edges, confirmed in the Menu Item Edit
        // screenshot).
        var buttonRowWidth = _buttonPanel.GetPreferredSize(Size.Empty).Width + 16;
        wantedClientWidth = Math.Max(wantedClientWidth, buttonRowWidth);

        // Ensure title bar text is fully visible (not truncated)
        int titleWidth = 0;
        try
        {
            titleWidth = TextRenderer.MeasureText(Text, SystemFonts.CaptionFont).Width + 120;
        }
        catch
        {
            titleWidth = TextRenderer.MeasureText(Text, Font).Width + 120;
        }
        wantedClientWidth = Math.Max(wantedClientWidth, titleWidth);

        // Comfortable click targets: AutoSize grows buttons to their caption,
        // but a DPI-scaled minimum keeps OK/Cancel/Save & New from rendering
        // as thin slivers on short captions at above-100% DPI.
        var minButtonWidth = Clovent.Desktop.Forms.Base.DesktopDpi.Scale(96, this);
        foreach (var button in new[] { _okButton, _cancelButton, _saveAndNewButton })
        {
            button.MinimumSize = new Size(minButtonWidth, 0);
            button.Padding = new Padding(10, 4, 10, 4);
        }

        var contentHeight = _contentPanel.GetPreferredSize(new Size(wantedClientWidth - _contentPanel.Padding.Horizontal, 0)).Height;
        var buttonHeight = _buttonPanel.GetPreferredSize(new Size(wantedClientWidth, 0)).Height;

        // Any extra Top/Bottom-docked chrome a subclass adds after this
        // constructor returns (e.g. MenuItemEditForm's centered "Menu
        // Item" heading) needs its height counted here too, or this
        // dialog sizes itself short by exactly that much and clips the
        // last field row - the same class of bug this whole recompute
        // was added to fix in the first place.
        var extraChromeHeight = Controls.OfType<Control>()
            .Where(c => c != _contentPanel && c != _buttonPanel && c.Dock is DockStyle.Top or DockStyle.Bottom)
            .Sum(c => {
                var prefHeight = c.GetPreferredSize(new Size(wantedClientWidth, 0)).Height;
                return prefHeight > 0 ? prefHeight : c.Height;
            });
        var wantedClientHeight = contentHeight + buttonHeight + extraChromeHeight;

        ClientSize = new Size(
            Math.Max(ClientSize.Width, wantedClientWidth),
            Math.Max(ClientSize.Height, wantedClientHeight));

        // The dialog can now be resized/maximized, so it needs a real
        // floor: whatever this subclass's own fields actually need,
        // computed above from real content the exact same way ClientSize
        // already is - a user can shrink the window down to (but never
        // below) what its own fields require, instead of an arbitrary
        // guessed minimum.
        MinimumSize = Size;

        // Keyed by the concrete subclass's own type name, so
        // PriceOverrideDialog/DiscountDialog/PaymentMethodEditForm/...
        // each remember their own size/position independently, with no
        // per-subclass code needed - restoring here (after MinimumSize
        // is set) clamps a saved size up to this dialog's real minimum
        // rather than risking violating it.
        WindowPlacementStore.Restore(this, GetType().Name);
    }

    private void MasterDataEditFormBase_FormClosed(object? sender, FormClosedEventArgs e) =>
        WindowPlacementStore.Save(this, GetType().Name);
}
