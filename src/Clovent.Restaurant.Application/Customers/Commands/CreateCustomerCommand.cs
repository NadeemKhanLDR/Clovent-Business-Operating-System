using Clovent.MasterData.Shared.ValueObjects;
using Clovent.Restaurant.Application.Customers.Dtos;
using Clovent.Restaurant.Customers;
using MediatR;

namespace Clovent.Restaurant.Application.Customers.Commands;

/// <summary>Creates a new Customer account.</summary>
public sealed record CreateCustomerCommand(
    string Code,
    string Name,
    string MobileNumber,
    string Address,
    string? Email,
    decimal OpeningBalance,
    decimal CreditLimit,
    string? Notes) : IRequest<CustomerDto>;

/// <summary>Handles <see cref="CreateCustomerCommand"/>.</summary>
public sealed class CreateCustomerCommandHandler(
    ICustomerRepository customerRepository,
    ICustomerLedgerEntryRepository ledgerRepository) : IRequestHandler<CreateCustomerCommand, CustomerDto>
{
    /// <inheritdoc/>
    public async Task<CustomerDto> Handle(CreateCustomerCommand request, CancellationToken cancellationToken)
    {
        var code = EntityCode.Create(request.Code);

        var existing = await customerRepository.GetByCodeAsync(code, cancellationToken);
        if (existing is not null)
        {
            throw new InvalidOperationException($"A customer with code '{request.Code}' already exists.");
        }

        var customer = Customer.Create(
            code,
            request.Name,
            request.MobileNumber,
            request.Address,
            request.Email,
            request.OpeningBalance,
            request.CreditLimit,
            request.Notes);

        await customerRepository.AddAsync(customer, cancellationToken);

        if (request.OpeningBalance > 0)
        {
            var ledgerEntry = CustomerLedgerEntry.Create(
                customer.Id,
                "OPENING",
                "Opening Balance",
                request.OpeningBalance,
                0m,
                request.OpeningBalance);
            await ledgerRepository.AddAsync(ledgerEntry, cancellationToken);
        }

        return CustomerDto.FromDomain(customer);
    }
}
