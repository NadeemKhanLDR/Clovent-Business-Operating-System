using Clovent.MasterData.Application.Currencies.Dtos;
using Clovent.MasterData.Currencies;
using MediatR;

namespace Clovent.MasterData.Application.Currencies.Queries;

/// <summary>Retrieves a single currency by identity.</summary>
public sealed record GetCurrencyByIdQuery(Guid CurrencyId) : IRequest<CurrencyDto>;

/// <summary>Handles <see cref="GetCurrencyByIdQuery"/>.</summary>
public sealed class GetCurrencyByIdQueryHandler(ICurrencyRepository currencyRepository)
    : IRequestHandler<GetCurrencyByIdQuery, CurrencyDto>
{
    /// <inheritdoc/>
    public async Task<CurrencyDto> Handle(GetCurrencyByIdQuery request, CancellationToken cancellationToken)
    {
        var currency = await currencyRepository.GetByIdAsync(new CurrencyId(request.CurrencyId), cancellationToken)
            ?? throw new NotFoundException(nameof(Currency), request.CurrencyId);

        return CurrencyDto.FromDomain(currency);
    }
}
