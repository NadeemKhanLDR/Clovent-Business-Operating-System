using Clovent.Desktop.Forms.Base.Appearance;
using Clovent.Desktop.MasterData;
using Clovent.Restaurant.Application.PaymentMethods.Dtos;

namespace Clovent.Desktop.Forms.Restaurant.Setup;

partial class PaymentMethodsView
{
    /// <summary>Required designer variable.</summary>
    private System.ComponentModel.IContainer components = null;

    private MasterDataListView<PaymentMethodDto> _listView;

    #region Component Designer generated code

    /// <summary>
    /// Required method for Designer support - do not modify the contents of
    /// this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        SuspendLayout();

        Dock = DockStyle.Fill;

        _listView = new MasterDataListView<PaymentMethodDto>(
        [
            new MasterDataColumn("Name", "Name", 220),
            new MasterDataColumn("Status", "Status", 90),
            new MasterDataColumn("CreatedAtUtc", "Created (UTC)", 160),
        ])
        {
            LoadItemsAsync = LoadItemsAsync,
            SearchTextSelector = dto => dto.Name,
            StatusSelector = dto => dto.Status,
            CanUseFeatureAsync = CanUseFeatureAsync,
            OnNew = CreateAsync,
            OnEdit = EditAsync,
            OnActivate = ActivateAsync,
            OnDeactivate = DeactivateAsync,
        };
        _listView.Name = "_listView";

        //
        // PaymentMethodsView
        //
        Controls.Add(_listView);
        Name = "PaymentMethodsView";

        AppearanceManager.Changed += AppearanceManager_Changed;
        Load += PaymentMethodsView_Load;

        ResumeLayout(false);
    }

    #endregion
}
