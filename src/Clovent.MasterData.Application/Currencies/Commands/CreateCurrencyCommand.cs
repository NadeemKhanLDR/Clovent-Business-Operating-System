using Clovent.MasterData.Application.Currencies.Dtos;
using Clovent.MasterData.Currencies;
using MediatR;

namespace Clovent.MasterData.Application.Currencies.Commands;

/// <summary>Creates a new currency catalog entry.</summary>
public sealed record CreateCurrencyCommand(string Code, string Name, string Symbol, int DecimalPlaces) : IRequest<CurrencyDto>;

/// <summary>Handles <see cref="CreateCurrencyCommand"/>.</summary>
public sealed class CreateCurrencyCommandHandler(ICurrencyRepository currencyRepository)
    : IRequestHandler<CreateCurrencyCommand, CurrencyDto>
{
    /// <inheritdoc/>
    public async Task<CurrencyDto> Handle(CreateCurrencyCommand request, CancellationToken cancellationToken)
    {
        var currency = Currency.Create(CurrencyCode.Create(request.Code), request.Name, request.Symbol, request.DecimalPlaces);

        await currencyRepository.AddAsync(currency, cancellationToken);

        return CurrencyDto.FromDomain(currency);
    }
}
