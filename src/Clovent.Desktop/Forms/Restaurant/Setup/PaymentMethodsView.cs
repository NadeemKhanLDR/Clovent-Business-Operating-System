using Clovent.Desktop.Forms.Base;
using Clovent.Desktop.Forms.Base.Appearance;
using Clovent.Desktop.MasterData;
using Clovent.Desktop.Sessions;
using Clovent.Identity.Application.Authorization;
using Clovent.Restaurant.Application.ActivityLogs.Commands;
using Clovent.Restaurant.Application.PaymentMethods.Commands;
using Clovent.Restaurant.Application.PaymentMethods.Dtos;
using Clovent.Restaurant.Application.PaymentMethods.Queries;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Clovent.Desktop.Forms.Restaurant.Setup;

/// <summary>
/// Restaurant Setup: lets an owner define which tenders the POS Payment
/// screen offers - Cash, Debit Card, Credit Card, Bank Transfer, EasyPaisa,
/// JazzCash, Google Pay, Apple Pay, or anything else, each just a name. A
/// search/CRUD/activate-deactivate grid over the existing
/// <c>Clovent.Restaurant</c> <c>PaymentMethod</c> aggregate (already used by
/// <c>RecordPaymentCommand</c>/<c>PaymentPanel</c> - this screen adds no new
/// domain/application code, only the missing management UI), the same
/// <c>MasterDataListView&lt;TDto&gt;</c> shape <c>BrandManagementView</c>
/// already uses for an identically-shaped name-only aggregate. Deactivating a
/// method here removes it from <c>PaymentPanel</c>'s method buttons
/// immediately (that screen only offers Active methods) without affecting
/// any payment already recorded through it. Feature-gated per
/// <c>paymentmethods.{create|edit|activate|deactivate}</c>. Control tree
/// lives in <c>PaymentMethodsView.Designer.cs</c>; this file holds behavior
/// only.
/// </summary>
[System.ComponentModel.DesignerCategory("Code")]
public sealed partial class PaymentMethodsView : DevExpress.XtraEditors.XtraUserControl
{
    private const string FeatureCode = "paymentmethods";

    private readonly IServiceScope _scope;
    private readonly ScreenOperationGate _gate = new();
    private readonly IMediator _mediator;
    private readonly IFeatureAuthorizationPolicy _featurePolicy;
    private readonly ICurrentSession _currentSession;

    /// <summary>Builds the screen and starts its own DI scope for the Scoped services it needs.</summary>
    public PaymentMethodsView(IServiceScopeFactory scopeFactory, ICurrentSession currentSession)
    {
        _scope = scopeFactory.CreateScope();
        _mediator = new SerializedMediator(_scope.ServiceProvider.GetRequiredService<IMediator>(), _gate);
        _featurePolicy = new SerializedFeatureAuthorizationPolicy(_scope.ServiceProvider.GetRequiredService<IFeatureAuthorizationPolicy>(), _gate);
        _currentSession = currentSession;

        InitializeComponent();
    }

    private void AppearanceManager_Changed(object? sender, EventArgs e) => AppearanceManager.Apply(this, "Restaurant", nameof(PaymentMethodsView));

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            AppearanceManager.Changed -= AppearanceManager_Changed;
            _scope.Dispose();
            _gate.Dispose();
            components?.Dispose();
        }

        base.Dispose(disposing);
    }

    private async void PaymentMethodsView_Load(object? sender, EventArgs e)
    {
        AppearanceManager.Apply(this, "Restaurant", nameof(PaymentMethodsView));
        await _listView.RefreshAsync();
    }

    private async Task<IReadOnlyList<PaymentMethodDto>> LoadItemsAsync(CancellationToken cancellationToken)
    {
        var items = await _mediator.Send(new ListPaymentMethodsQuery(), cancellationToken);
        return [.. items];
    }

    private Task<bool> CanUseFeatureAsync(string operation) =>
        _currentSession.UserId is { } userId
            ? _featurePolicy.CanUseFeatureAsync(userId, $"{FeatureCode}.{operation}")
            : Task.FromResult(false);

    private async Task CreateAsync()
    {
        using var form = new PaymentMethodEditForm("New Payment Method");
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            await _mediator.Send(new CreatePaymentMethodCommand(form.NameValue));
            await LogActivityAsync($"Added payment method \"{form.NameValue}\"");
        }
    }

    private async Task EditAsync(PaymentMethodDto dto)
    {
        using var form = new PaymentMethodEditForm("Edit Payment Method", dto.Name);
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            await _mediator.Send(new RenamePaymentMethodCommand(dto.PaymentMethodId, form.NameValue));
            await LogActivityAsync($"Renamed payment method \"{dto.Name}\" to \"{form.NameValue}\"");
        }
    }

    private async Task ActivateAsync(PaymentMethodDto dto)
    {
        await _mediator.Send(new ActivatePaymentMethodCommand(dto.PaymentMethodId));
        await LogActivityAsync($"Activated payment method \"{dto.Name}\"");
    }

    private async Task DeactivateAsync(PaymentMethodDto dto)
    {
        await _mediator.Send(new DeactivatePaymentMethodCommand(dto.PaymentMethodId));
        await LogActivityAsync($"Deactivated payment method \"{dto.Name}\"");
    }

    /// <summary>Records one "Setup Changes" activity log entry - see <c>RestaurantPosView.LogActivityAsync</c>'s identical reasoning for why failures here are deliberately swallowed rather than surfaced.</summary>
    private async Task LogActivityAsync(string details)
    {
        try
        {
            await _mediator.Send(new RecordActivityCommand("Setup Changes", details, _currentSession.DisplayName ?? "Unknown", Environment.MachineName));
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
        }
    }
}
