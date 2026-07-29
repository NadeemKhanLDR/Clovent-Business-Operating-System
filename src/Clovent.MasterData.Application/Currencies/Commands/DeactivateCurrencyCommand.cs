using Clovent.MasterData.Application.Currencies.Dtos;
using Clovent.MasterData.Currencies;
using MediatR;

namespace Clovent.MasterData.Application.Currencies.Commands;

/// <summary>Deactivates a currency.</summary>
public sealed record DeactivateCurrencyCommand(Guid CurrencyId) : IRequest<CurrencyDto>;

/// <summary>Handles <see cref="DeactivateCurrencyCommand"/>.</summary>
public sealed class DeactivateCurrencyCommandHandler(ICurrencyRepository currencyRepository)
    : IRequestHandler<DeactivateCurrencyCommand, CurrencyDto>
{
    /// <inheritdoc/>
    public async Task<CurrencyDto> Handle(DeactivateCurrencyCommand request, CancellationToken cancellationToken)
    {
        var currency = await currencyRepository.GetByIdAsync(new CurrencyId(request.CurrencyId), cancellationToken)
            ?? throw new NotFoundException(nameof(Currency), request.CurrencyId);

        currency.Deactivate();

        return CurrencyDto.FromDomain(currency);
    }
}
