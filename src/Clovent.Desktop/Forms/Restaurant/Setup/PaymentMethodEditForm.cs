using Clovent.Desktop.MasterData;

namespace Clovent.Desktop.Forms.Restaurant.Setup;

/// <summary>Create/edit dialog for a Payment Method - just a name (e.g. "Cash", "Debit Card", "EasyPaisa"). Control tree lives in <c>PaymentMethodEditForm.Designer.cs</c>; this file holds behavior only.</summary>
public sealed partial class PaymentMethodEditForm : MasterDataEditFormBase
{
    /// <summary>Builds the dialog.</summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [Obsolete("Designer only", true)]
    public PaymentMethodEditForm() : base("Edit Payment Method")
    {
        InitializeComponent();
        }

    /// <summary>Builds the dialog. <paramref name="title"/> is the dialog's caption; <paramref name="name"/> pre-populates the name field when editing an existing payment method.</summary>
    public PaymentMethodEditForm(string title, string? name = null) : base(title)
    {
        InitializeComponent();
        if (Clovent.Desktop.Forms.Base.DesignModeHelper.IsInDesignMode)
            return;

        _nameEdit.Text = name ?? string.Empty;
    }

    /// <summary>The entered payment method name.</summary>
    public string NameValue => _nameEdit.Text.Trim();

    /// <inheritdoc/>
    protected override bool ValidateFields(out string error)
    {
        if (string.IsNullOrWhiteSpace(_nameEdit.Text))
        {
            error = "Enter a name for this payment method (e.g. \"Cash\", \"EasyPaisa\").";
            return false;
        }

        error = string.Empty;
        return true;
    }

    }
