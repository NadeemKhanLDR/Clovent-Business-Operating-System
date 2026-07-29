using Clovent.MasterData.Currencies;

namespace Clovent.MasterData.Application.Tests.TestSupport;

internal sealed class FakeCurrencyRepository : ICurrencyRepository
{
    private readonly Dictionary<CurrencyId, Currency> _currencies = [];

    public void Add(Currency currency) => _currencies[currency.Id] = currency;

    public Task<Currency?> GetByIdAsync(CurrencyId id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_currencies.GetValueOrDefault(id));

    public Task<Currency?> GetByCodeAsync(CurrencyCode code, CancellationToken cancellationToken = default) =>
        Task.FromResult(_currencies.Values.FirstOrDefault(c => c.Code == code));

    public Task<IReadOnlyCollection<Currency>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyCollection<Currency>>([.. _currencies.Values]);

    public Task AddAsync(Currency currency, CancellationToken cancellationToken = default)
    {
        _currencies[currency.Id] = currency;
        return Task.CompletedTask;
    }
}
