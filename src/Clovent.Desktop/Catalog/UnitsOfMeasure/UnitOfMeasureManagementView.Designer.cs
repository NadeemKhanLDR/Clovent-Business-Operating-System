using Clovent.Catalog.Application.UnitsOfMeasure.Commands;
using Clovent.Catalog.Application.UnitsOfMeasure.Dtos;
using Clovent.Desktop.MasterData;

namespace Clovent.Desktop.Catalog.UnitsOfMeasure;

partial class UnitOfMeasureManagementView
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

        _listView = new MasterDataListView<UnitOfMeasureDto>(
        [
            new MasterDataColumn("Code", "Code", 80),
            new MasterDataColumn("Name", "Name", 200),
            new MasterDataColumn("Status", "Status", 90),
            new MasterDataColumn("CreatedAtUtc", "Created (UTC)", 160),
        ])
        {
            LoadItemsAsync = LoadItemsAsync,
            SearchTextSelector = dto => $"{dto.Code} {dto.Name}",
            StatusSelector = dto => dto.Status,
            CanUseFeatureAsync = operation => CanUseFeatureAsync(operation),
            OnNew = CreateAsync,
            OnEdit = EditAsync,
            OnActivate = dto => _mediator.Send(new ActivateUnitOfMeasureCommand(dto.UnitOfMeasureId)),
            OnDeactivate = dto => _mediator.Send(new DeactivateUnitOfMeasureCommand(dto.UnitOfMeasureId)),
        };

        Controls.Add(_listView);
        Load += UnitOfMeasureManagementView_Load;
    }

    #endregion

    private MasterDataListView<UnitOfMeasureDto> _listView;
}
