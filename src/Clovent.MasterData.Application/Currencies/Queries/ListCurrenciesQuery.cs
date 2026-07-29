using Clovent.MasterData.Application.Currencies.Dtos;
using Clovent.MasterData.Currencies;
using MediatR;

namespace Clovent.MasterData.Application.Currencies.Queries;

/// <summary>Retrieves every currency in the catalog.</summary>
public sealed record ListCurrenciesQuery : IRequest<IReadOnlyCollection<CurrencyDto>>;

/// <summary>Handles <see cref="ListCurrenciesQuery"/>.</summary>
public sealed class ListCurrenciesQueryHandler(ICurrencyRepository currencyRepository)
    : IRequestHandler<ListCurrenciesQuery, IReadOnlyCollection<CurrencyDto>>
{
    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<CurrencyDto>> Handle(ListCurrenciesQuery request, CancellationToken cancellationToken)
    {
        var currencies = await currencyRepository.GetAllAsync(cancellationToken);
        return [.. currencies.Select(CurrencyDto.FromDomain)];
    }
}
