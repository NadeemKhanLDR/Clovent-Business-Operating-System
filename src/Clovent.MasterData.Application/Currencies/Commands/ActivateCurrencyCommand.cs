using Clovent.MasterData.Application.Currencies.Dtos;
using Clovent.MasterData.Currencies;
using MediatR;

namespace Clovent.MasterData.Application.Currencies.Commands;

/// <summary>Activates a currency.</summary>
public sealed record ActivateCurrencyCommand(Guid CurrencyId) : IRequest<CurrencyDto>;

/// <summary>Handles <see cref="ActivateCurrencyCommand"/>.</summary>
public sealed class ActivateCurrencyCommandHandler(ICurrencyRepository currencyRepository)
    : IRequestHandler<ActivateCurrencyCommand, CurrencyDto>
{
    /// <inheritdoc/>
    public async Task<CurrencyDto> Handle(ActivateCurrencyCommand request, CancellationToken cancellationToken)
    {
        var currency = await currencyRepository.GetByIdAsync(new CurrencyId(request.CurrencyId), cancellationToken)
            ?? throw new NotFoundException(nameof(Currency), request.CurrencyId);

        currency.Activate();

        return CurrencyDto.FromDomain(currency);
    }
}
