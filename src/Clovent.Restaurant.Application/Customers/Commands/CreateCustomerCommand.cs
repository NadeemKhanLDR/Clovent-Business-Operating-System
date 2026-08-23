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
    string? Notes,
    string? ShopNo = null,
    string? Mobile2 = null,
    string? Phone = null) : IRequest<CustomerDto>;

/// <summary>Handles <see cref="CreateCustomerCommand"/>.</summary>
public sealed class CreateCustomerCommandHandler(
    ICustomerRepository customerRepository,
    ICustomerLedgerEntryRepository ledgerRepository) : IRequestHandler<CreateCustomerCommand, CustomerDto>
{
    /// <inheritdoc/>
    public async Task<CustomerDto> Handle(CreateCustomerCommand request, CancellationToken cancellationToken)
    {
        string codeStr = request.Code;
        EntityCode code = EntityCode.Create(codeStr);

        var existing = await customerRepository.GetByCodeAsync(code, cancellationToken);
        if (existing is not null)
        {
            var allCustomers = await customerRepository.GetAllAsync(cancellationToken);
            var nextNumber = 1;
            foreach (var c in allCustomers)
            {
                var cCode = c.Code.Value;
                if (cCode.StartsWith("C", StringComparison.OrdinalIgnoreCase) && 
                    int.TryParse(cCode.Substring(1), out var num))
                {
                    if (num >= nextNumber)
                    {
                        nextNumber = num + 1;
                    }
                }
            }
            codeStr = $"C{nextNumber:D3}";
            code = EntityCode.Create(codeStr);

            int attempts = 0;
            while (attempts < 100)
            {
                var secondCheck = await customerRepository.GetByCodeAsync(code, cancellationToken);
                if (secondCheck is null)
                {
                    break;
                }
                nextNumber++;
                codeStr = $"C{nextNumber:D3}";
                code = EntityCode.Create(codeStr);
                attempts++;
            }
        }

        var customer = Customer.Create(
            code,
            request.Name,
            request.MobileNumber,
            request.Address,
            request.Email,
            request.OpeningBalance,
            request.CreditLimit,
            request.Notes,
            request.ShopNo,
            request.Mobile2,
            request.Phone);

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
