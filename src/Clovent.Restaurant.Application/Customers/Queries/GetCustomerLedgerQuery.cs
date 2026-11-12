using Clovent.Restaurant.Application.Customers.Dtos;
using Clovent.Restaurant.Customers;
using MediatR;

namespace Clovent.Restaurant.Application.Customers.Queries;

/// <summary>Queries the ledger transaction history for a customer.</summary>
public sealed record GetCustomerLedgerQuery(Guid CustomerId) : IRequest<IReadOnlyList<CustomerLedgerEntryDto>>, Clovent.Platform.Execution.IRequireCompanyScope;

/// <summary>Handles <see cref="GetCustomerLedgerQuery"/>.</summary>
public sealed class GetCustomerLedgerQueryHandler(ICustomerLedgerEntryRepository repository,
    ICustomerRepository customers, Clovent.Platform.Execution.IExecutionContextAccessor companyScope)
    : IRequestHandler<GetCustomerLedgerQuery, IReadOnlyList<CustomerLedgerEntryDto>>
{
    /// <inheritdoc/>
    public async Task<IReadOnlyList<CustomerLedgerEntryDto>> Handle(GetCustomerLedgerQuery request, CancellationToken cancellationToken)
    {
        var company = companyScope.Current?.CompanyId;
        if (!company.HasValue || company == Guid.Empty)
            throw new Clovent.Platform.Execution.ScopeUnresolvedException("Company scope is required for customer ledgers.");
        var customer = await customers.GetByIdAsync(new CustomerId(request.CustomerId), cancellationToken);
        if (customer?.CompanyId != company)
            throw new Clovent.Platform.Execution.ScopeMismatchException("Customer is not owned by the active company.");
        var entries = (await repository.GetByCustomerIdAsync(customer.Id, cancellationToken)).Where(e => e.CompanyId == company);
        return [.. entries.Select(CustomerLedgerEntryDto.FromDomain)];
    }
}
