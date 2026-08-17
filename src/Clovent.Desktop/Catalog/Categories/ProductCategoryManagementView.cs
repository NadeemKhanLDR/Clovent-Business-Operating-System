using Clovent.Catalog.Application.Categories.Commands;
using Clovent.Catalog.Application.Categories.Dtos;
using Clovent.Catalog.Application.Categories.Queries;
using Clovent.Desktop.MasterData;
using Clovent.Desktop.Sessions;
using Clovent.Identity.Application.Authorization;
using DevExpress.XtraEditors;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Clovent.Desktop.Catalog.Categories;

/// <summary>
/// Category Management screen: search, filter, CRUD, activate/deactivate
/// over the Product Category hierarchy. Feature-gated per
/// <c>categories.{create|edit|activate|deactivate}</c>.
/// </summary>
[System.ComponentModel.DesignerCategory("Code")]
public sealed partial class ProductCategoryManagementView : XtraUserControl
{
    private const string FeatureCode = "categories";

    private readonly IServiceScope _scope;
    private readonly IMediator _mediator;
    private readonly IFeatureAuthorizationPolicy _featurePolicy;
    private readonly ICurrentSession _currentSession;

    /// <summary>Builds the screen and starts its own DI scope for the Scoped services it needs.</summary>
    public ProductCategoryManagementView(IServiceScopeFactory scopeFactory, ICurrentSession currentSession)
    {
        _scope = scopeFactory.CreateScope();
        _mediator = _scope.ServiceProvider.GetRequiredService<IMediator>();
        _featurePolicy = _scope.ServiceProvider.GetRequiredService<IFeatureAuthorizationPolicy>();
        _currentSession = currentSession;

        InitializeComponent();
    }

    private async void ProductCategoryManagementView_Load(object? sender, EventArgs e) => await _listView.RefreshAsync();

    private async Task<IReadOnlyList<ProductCategoryDto>> LoadItemsAsync(CancellationToken cancellationToken)
    {
        var items = await _mediator.Send(new ListProductCategoriesQuery(), cancellationToken);
        return [.. items];
    }

    private Task<bool> CanUseFeatureAsync(string operation) =>
        _currentSession.UserId is { } userId
            ? _featurePolicy.CanUseFeatureAsync(userId, $"{FeatureCode}.{operation}")
            : Task.FromResult(false);

    private async Task<IReadOnlyList<(Guid Id, string Display)>> LoadParentOptionsAsync(Guid? excludeId)
    {
        var categories = await _mediator.Send(new ListProductCategoriesQuery());
        return [.. categories.Where(c => c.ProductCategoryId != excludeId).Select(c => (c.ProductCategoryId, c.Name))];
    }

    private async Task CreateAsync()
    {
        var parentOptions = await LoadParentOptionsAsync(excludeId: null);
        using var form = new ProductCategoryEditForm("New Category", parentOptions);
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            await _mediator.Send(new CreateProductCategoryCommand(form.NameValue, form.ParentCategoryId));
        }
    }

    private async Task EditAsync(ProductCategoryDto dto)
    {
        var parentOptions = await LoadParentOptionsAsync(excludeId: dto.ProductCategoryId);
        using var form = new ProductCategoryEditForm("Edit Category", parentOptions, dto.Name, dto.ParentCategoryId);
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            await _mediator.Send(new RenameProductCategoryCommand(dto.ProductCategoryId, form.NameValue));
            await _mediator.Send(new SetProductCategoryParentCommand(dto.ProductCategoryId, form.ParentCategoryId));
        }
    }
}
