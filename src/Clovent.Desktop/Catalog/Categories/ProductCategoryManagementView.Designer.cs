using Clovent.Catalog.Application.Categories.Commands;
using Clovent.Catalog.Application.Categories.Dtos;
using Clovent.Desktop.MasterData;

namespace Clovent.Desktop.Catalog.Categories;

partial class ProductCategoryManagementView
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

        _listView = new MasterDataListView<ProductCategoryDto>(
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
            OnActivate = dto => _mediator.Send(new ActivateProductCategoryCommand(dto.ProductCategoryId)),
            OnDeactivate = dto => _mediator.Send(new DeactivateProductCategoryCommand(dto.ProductCategoryId)),
        };

        Controls.Add(_listView);
        Load += ProductCategoryManagementView_Load;
    }

    #endregion

    private MasterDataListView<ProductCategoryDto> _listView;
}
