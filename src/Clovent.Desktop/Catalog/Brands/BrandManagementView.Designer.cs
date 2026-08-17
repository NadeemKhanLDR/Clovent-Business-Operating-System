using Clovent.Catalog.Application.Brands.Commands;
using Clovent.Catalog.Application.Brands.Dtos;
using Clovent.Desktop.MasterData;

namespace Clovent.Desktop.Catalog.Brands;

partial class BrandManagementView
{
    /// <summary>Required designer variable.</summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>Clean up any resources being used.</summary>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _scope.Dispose();
            components?.Dispose();
        }

        base.Dispose(disposing);
    }

    #region Component Designer generated code

    /// <summary>
    /// Required method for Designer support - do not modify the contents of
    /// this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        Dock = DockStyle.Fill;

        _listView = new MasterDataListView<BrandDto>(
        [
            new MasterDataColumn("Name", "Name", 220),
            new MasterDataColumn("Status", "Status", 90),
            new MasterDataColumn("CreatedAtUtc", "Created (UTC)", 160),
        ])
        {
            LoadItemsAsync = LoadItemsAsync,
            SearchTextSelector = dto => dto.Name,
            StatusSelector = dto => dto.Status,
            CanUseFeatureAsync = operation => CanUseFeatureAsync(operation),
            OnNew = CreateAsync,
            OnEdit = EditAsync,
            OnActivate = dto => _mediator.Send(new ActivateBrandCommand(dto.BrandId)),
            OnDeactivate = dto => _mediator.Send(new DeactivateBrandCommand(dto.BrandId)),
        };

        Controls.Add(_listView);
        Load += BrandManagementView_Load;
    }

    #endregion

    private MasterDataListView<BrandDto> _listView;
}
