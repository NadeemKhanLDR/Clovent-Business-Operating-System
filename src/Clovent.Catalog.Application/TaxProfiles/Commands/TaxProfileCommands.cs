using Clovent.Catalog.Application.TaxProfiles.Dtos;
using Clovent.Catalog.TaxProfiles;
using Clovent.Domain;
using MediatR;

namespace Clovent.Catalog.Application.TaxProfiles.Commands;

/// <summary>Command to create a new statutory tax profile.</summary>
public sealed record CreateTaxProfileCommand(
    string Code,
    string InvoiceDisplayName,
    string Authority,
    string Jurisdiction,
    TaxClassification TaxClassification,
    ItemTaxClassification ItemClassification,
    decimal RatePercentage,
    TaxPricingMode PricingMode,
    DateTimeOffset? EffectiveFromUtc = null,
    DateTimeOffset? EffectiveToUtc = null,
    string? ReferenceDocument = null,
    string? PaymentMethodRestriction = null,
    string? Notes = null) : IRequest<TaxProfileDto>;

/// <summary>Handles <see cref="CreateTaxProfileCommand"/>.</summary>
public sealed class CreateTaxProfileCommandHandler(ITaxProfileRepository repository)
    : IRequestHandler<CreateTaxProfileCommand, TaxProfileDto>
{
    /// <inheritdoc/>
    public async Task<TaxProfileDto> Handle(CreateTaxProfileCommand request, CancellationToken cancellationToken)
    {
        var existing = await repository.GetByCodeAsync(request.Code, cancellationToken);
        if (existing != null)
        {
            throw new InvalidOperationException($"A tax profile with statutory code '{request.Code}' already exists.");
        }

        var profile = TaxProfile.Create(
            request.Code,
            request.InvoiceDisplayName,
            request.Authority,
            request.Jurisdiction,
            request.TaxClassification,
            request.ItemClassification,
            request.RatePercentage,
            request.PricingMode,
            request.EffectiveFromUtc,
            request.EffectiveToUtc,
            request.ReferenceDocument,
            request.PaymentMethodRestriction,
            request.Notes);

        await repository.AddAsync(profile, cancellationToken);
        return TaxProfileDto.FromDomain(profile);
    }
}

/// <summary>Command to update rate, pricing mode, and reference document on an existing tax profile.</summary>
public sealed record UpdateTaxProfileCommand(
    Guid TaxProfileId,
    decimal NewRatePercentage,
    TaxPricingMode NewPricingMode,
    string? ReferenceDocument = null) : IRequest<TaxProfileDto>;

/// <summary>Handles <see cref="UpdateTaxProfileCommand"/>.</summary>
public sealed class UpdateTaxProfileCommandHandler(ITaxProfileRepository repository)
    : IRequestHandler<UpdateTaxProfileCommand, TaxProfileDto>
{
    /// <inheritdoc/>
    public async Task<TaxProfileDto> Handle(UpdateTaxProfileCommand request, CancellationToken cancellationToken)
    {
        var id = new TaxProfileId(request.TaxProfileId);
        var profile = await repository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(TaxProfile), request.TaxProfileId);

        profile.UpdateRate(request.NewRatePercentage, request.NewPricingMode, request.ReferenceDocument);
        await repository.UpdateAsync(profile, cancellationToken);

        return TaxProfileDto.FromDomain(profile);
    }
}

/// <summary>Command to toggle activation status of a tax profile.</summary>
public sealed record ToggleTaxProfileStatusCommand(Guid TaxProfileId, bool IsActive) : IRequest<TaxProfileDto>;

/// <summary>Handles <see cref="ToggleTaxProfileStatusCommand"/>.</summary>
public sealed class ToggleTaxProfileStatusCommandHandler(ITaxProfileRepository repository)
    : IRequestHandler<ToggleTaxProfileStatusCommand, TaxProfileDto>
{
    /// <inheritdoc/>
    public async Task<TaxProfileDto> Handle(ToggleTaxProfileStatusCommand request, CancellationToken cancellationToken)
    {
        var id = new TaxProfileId(request.TaxProfileId);
        var profile = await repository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(TaxProfile), request.TaxProfileId);

        if (request.IsActive)
        {
            profile.Activate();
        }
        else
        {
            profile.Deactivate();
        }

        await repository.UpdateAsync(profile, cancellationToken);
        return TaxProfileDto.FromDomain(profile);
    }
}
