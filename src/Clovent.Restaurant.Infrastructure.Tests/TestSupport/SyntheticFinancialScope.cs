using Clovent.Restaurant.Application.Security;

namespace Clovent.Restaurant.Infrastructure.Tests.TestSupport;

// Explicit test-only authorization for single-company transaction/rollback fixtures.
// These suites qualify persistence/locking, not tenant authorization. Real scope is tested separately.
internal sealed class SyntheticFinancialScope : IFinancialCompanyScope
{
    public Task<Guid> AuthorizeOrderAsync(Guid order, Guid? branch, CancellationToken ct, Guid? shiftId = null, Guid? organizationId = null) => Task.FromResult(branch ?? Guid.Empty);
    public Task AuthorizeCollectionAsync(Guid customer, Guid? shift, CancellationToken ct) => Task.CompletedTask;
    public Task AuthorizeWarehouseAsync(Guid warehouse, CancellationToken ct) => Task.CompletedTask;
}
