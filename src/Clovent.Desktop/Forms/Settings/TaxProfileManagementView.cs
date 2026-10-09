using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Clovent.Catalog.Application.TaxProfiles.Commands;
using Clovent.Catalog.Application.TaxProfiles.Dtos;
using Clovent.Catalog.Application.TaxProfiles.Queries;
using Clovent.Catalog.TaxProfiles;
using Clovent.Desktop.Forms.Base;
using Clovent.Desktop.Forms.Base.Appearance;
using Clovent.Desktop.Sessions;
using Clovent.Identity.Application.Authorization;
using DevExpress.XtraEditors;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Clovent.Desktop.Forms.Settings;

/// <summary>
/// Administration view for configuring Pakistan statutory sales tax profiles.
/// Supports viewing active/inactive profiles, editing rates, toggling activation status,
/// and defining authority/jurisdiction parameters.
/// </summary>
[System.ComponentModel.DesignerCategory("Code")]
public sealed partial class TaxProfileManagementView : XtraUserControl
{
    private const string FeatureCode = "taxprofiles";

    private readonly IServiceScope? _scope;
    private readonly IMediator? _mediator;
    private readonly IFeatureAuthorizationPolicy? _featurePolicy;
    private readonly ICurrentSession? _currentSession;

    /// <summary>Builds the screen with DI scope.</summary>
    public TaxProfileManagementView(IServiceScopeFactory scopeFactory, ICurrentSession currentSession)
    {
        _scope = scopeFactory.CreateScope();
        _mediator = _scope.ServiceProvider.GetRequiredService<IMediator>();
        _featurePolicy = _scope.ServiceProvider.GetRequiredService<IFeatureAuthorizationPolicy>();
        _currentSession = currentSession;

        InitializeComponent();
    }

    /// <summary>Designer-only constructor.</summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public TaxProfileManagementView()
    {
        InitializeComponent();
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _scope?.Dispose();
            components?.Dispose();
        }

        base.Dispose(disposing);
    }

    private async void TaxProfileManagementView_Load(object? sender, EventArgs e)
    {
        if (DesignModeHelper.IsInDesignMode || _listView == null) return;
        await _listView.RefreshAsync();
    }

    private async Task<IReadOnlyList<TaxProfileDto>> LoadItemsAsync(CancellationToken cancellationToken)
    {
        if (_mediator == null) return [];
        return await _mediator.Send(new ListTaxProfilesQuery(), cancellationToken);
    }

    private Task<bool> CanUseFeatureAsync(string operation)
    {
        if (_currentSession?.UserId is not { } userId || _featurePolicy == null)
        {
            return Task.FromResult(false);
        }

        return _featurePolicy.CanUseFeatureAsync(userId, $"{FeatureCode}.{operation}");
    }

    private async Task CreateAsync()
    {
        if (_mediator == null) return;

        using var dialog = new TaxProfileEditDialog(isNew: true);
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            await _mediator.Send(new CreateTaxProfileCommand(
                dialog.ProfileCode,
                dialog.InvoiceDisplayName,
                dialog.Authority,
                dialog.Jurisdiction,
                dialog.TaxClassification,
                dialog.ItemClassification,
                dialog.RatePercentage,
                dialog.PricingMode,
                DateTimeOffset.UtcNow,
                null,
                dialog.ReferenceDocument,
                null,
                dialog.Notes));

            await _listView.RefreshAsync();
        }
    }

    private async Task EditSelectedProfileAsync(TaxProfileDto selected)
    {
        if (_mediator == null) return;

        var taxClass = Enum.TryParse<TaxClassification>(selected.TaxClassification, true, out var tc) ? tc : TaxClassification.Taxable;
        var itemClass = Enum.TryParse<ItemTaxClassification>(selected.ItemClassification, true, out var ic) ? ic : ItemTaxClassification.Services;
        var pricingMode = Enum.TryParse<TaxPricingMode>(selected.PricingMode, true, out var pm) ? pm : TaxPricingMode.Exclusive;

        using var dialog = new TaxProfileEditDialog(
            isNew: false,
            code: selected.Code,
            displayName: selected.InvoiceDisplayName,
            authority: selected.Authority,
            jurisdiction: selected.Jurisdiction,
            taxClassification: taxClass,
            itemClassification: itemClass,
            ratePercentage: selected.RatePercentage,
            pricingMode: pricingMode,
            referenceDocument: selected.ReferenceDocument,
            notes: selected.Notes);

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            await _mediator.Send(new UpdateTaxProfileCommand(
                selected.TaxProfileId,
                dialog.RatePercentage,
                dialog.PricingMode,
                dialog.ReferenceDocument));

            await _listView.RefreshAsync();
        }
    }
}
