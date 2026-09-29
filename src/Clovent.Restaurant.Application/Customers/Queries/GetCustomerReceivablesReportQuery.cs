using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Clovent.Restaurant.Customers;
using Clovent.Restaurant.Orders;
using Clovent.Restaurant.PaymentMethods;
using Clovent.Restaurant.Payments;
using MediatR;

namespace Clovent.Restaurant.Application.Customers.Queries;

/// <summary>Filter options for the Customer Receivables / Aging report.</summary>
public enum CustomerReceivablesFilter
{
    /// <summary>Include all customer accounts.</summary>
    All,

    /// <summary>Include only customer accounts with an outstanding receivable > 0.</summary>
    HasBalanceOnly,

    /// <summary>Include only customer accounts that have exceeded their credit limit.</summary>
    OverLimitOnly,

    /// <summary>Include only customer accounts holding unapplied customer advance credit > 0.</summary>
    AdvanceOnly
}

/// <summary>One customer's row in the accounts receivable aging report.</summary>
public sealed record CustomerReceivableRowDto(
    Guid CustomerId,
    string CustomerCode,
    string CustomerName,
    string MobileNumber,
    decimal CreditLimit,
    decimal CurrentBalance,
    decimal AvailableCredit,
    DateTimeOffset? LastTransactionDate,
    decimal CurrentBucket,     // 0 days (today)
    decimal Days1To7Bucket,    // 1-7 days
    decimal Days8To15Bucket,   // 8-15 days
    decimal Days16To30Bucket,  // 16-30 days
    decimal Days31To60Bucket,  // 31-60 days
    decimal Days60PlusBucket,  // 60+ days
    bool IsOverCreditLimit,
    bool IsCreditAllowed,
    string Status,
    decimal Receivable = 0m,
    decimal Advance = 0m);

/// <summary>Aggregated result of the customer receivables and aging report.</summary>
public sealed record CustomerReceivablesReportDto(
    decimal TotalReceivables,
    decimal TotalOverCreditLimit,
    int ActiveAccountsWithBalanceCount,
    decimal AverageBalance,
    IReadOnlyList<CustomerReceivableRowDto> Rows,
    decimal TotalAdvances = 0m,
    decimal NetPosition = 0m);

/// <summary>Query to produce the Customer Receivables / Accounts Receivable Aging Report.</summary>
public sealed record GetCustomerReceivablesReportQuery(
    DateTimeOffset? AsOfDate = null,
    CustomerReceivablesFilter Filter = CustomerReceivablesFilter.All,
    string? SearchText = null) : IRequest<CustomerReceivablesReportDto>;

/// <summary>Handles <see cref="GetCustomerReceivablesReportQuery"/>.</summary>
public sealed class GetCustomerReceivablesReportQueryHandler(
    ICustomerRepository customerRepository,
    ICustomerLedgerEntryRepository ledgerRepository,
    IOrderRepository orderRepository,
    IPaymentRepository paymentRepository,
    IPaymentMethodRepository paymentMethodRepository,
    ICustomerPaymentAllocationRepository allocationRepository)
    : IRequestHandler<GetCustomerReceivablesReportQuery, CustomerReceivablesReportDto>
{
    /// <inheritdoc/>
    public async Task<CustomerReceivablesReportDto> Handle(GetCustomerReceivablesReportQuery request, CancellationToken cancellationToken)
    {
        var asOf = request.AsOfDate ?? DateTimeOffset.UtcNow;
        var customers = await customerRepository.GetAllAsync(cancellationToken);
        var lastDates = await ledgerRepository.GetLastTransactionDatesAsync(cancellationToken);
        var allAllocations = await allocationRepository.GetAllAsync(cancellationToken);
        var allocationsUpToAsOf = allAllocations.Where(a => a.AllocatedAtUtc <= asOf).ToList();
        var allocationsByOrder = allocationsUpToAsOf.GroupBy(a => a.OrderId).ToDictionary(g => g.Key, g => g.Sum(a => a.Amount));

        var paymentMethods = await paymentMethodRepository.GetAllAsync(cancellationToken);
        var onAccountMethodIds = paymentMethods
            .Where(m => string.Equals(m.Name.Value, "On Account", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(m.Name.Value, "Customer Account", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(m.Name.Value, "Credit", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(m.Name.Value, "Customer Credit", StringComparison.OrdinalIgnoreCase))
            .Select(m => m.Id)
            .ToHashSet();

        var allOrders = await orderRepository.GetAllAsync(cancellationToken);
        var ordersByCustomer = allOrders
            .Where(o => o.CustomerId.HasValue && o.CreatedAtUtc <= asOf && o.Status != OrderStatus.Voided && o.Status != OrderStatus.Cancelled)
            .GroupBy(o => o.CustomerId!.Value)
            .ToDictionary(g => g.Key, g => g.OrderBy(o => o.CreatedAtUtc).ToList());

        var allPayments = await paymentRepository.GetAllAsync(cancellationToken);
        var paymentsByOrder = allPayments
            .Where(p => !p.IsVoided && p.CreatedAtUtc <= asOf && onAccountMethodIds.Contains(p.PaymentMethodId))
            .GroupBy(p => p.OrderId)
            .ToDictionary(g => g.Key, g => g.Sum(p => p.Amount));

        var rows = new List<CustomerReceivableRowDto>();

        foreach (var customer in customers)
        {
            var customerLedger = await ledgerRepository.GetByCustomerIdAsync(customer.Id, cancellationToken);
            var entriesUpToAsOf = customerLedger.Where(e => e.Date <= asOf).OrderBy(e => e.Date).ToList();

            decimal netBal;
            if (entriesUpToAsOf.Count > 0)
            {
                netBal = entriesUpToAsOf.Last().RunningBalance;
            }
            else if (customer.CreatedAtUtc <= asOf)
            {
                netBal = customer.OpeningBalance;
            }
            else
            {
                netBal = 0m;
            }

            var receivable = Math.Max(0m, netBal);
            var advance = Math.Max(0m, -netBal);

            // Canonical Rule: Credit Limit = 0 means Zero Credit Limit.
            // Any customer with receivable > CreditLimit is over limit.
            var isOverLimit = receivable > customer.CreditLimit;
            var availableCredit = customer.IsCreditAllowed ? Math.Max(0m, customer.CreditLimit - receivable) : 0m;
            var lastDate = lastDates.GetValueOrDefault(customer.Id);

            decimal current = 0m;
            decimal d1to7 = 0m;
            decimal d8to15 = 0m;
            decimal d16to30 = 0m;
            decimal d31to60 = 0m;
            decimal d60plus = 0m;

            // Age ONLY receivable portions (never advances, never fully paid portions)
            if (receivable > 0)
            {
                var custOrders = ordersByCustomer.GetValueOrDefault(customer.Id, []);
                var unallocatedPieces = new List<(decimal Amount, DateTimeOffset Date)>();

                foreach (var order in custOrders)
                {
                    var onAccountAmount = paymentsByOrder.GetValueOrDefault(order.Id, 0m);
                    if (onAccountAmount <= 0) continue;

                    var allocated = allocationsByOrder.GetValueOrDefault(order.Id, 0m);
                    var remaining = Math.Max(0m, onAccountAmount - allocated);
                    if (remaining > 0)
                    {
                        unallocatedPieces.Add((remaining, order.CreatedAtUtc));
                    }
                }

                var totalPieces = unallocatedPieces.Sum(p => p.Amount);
                if (totalPieces < receivable)
                {
                    // Balance exceeds identified open orders; remainder belongs to opening balance
                    unallocatedPieces.Add((receivable - totalPieces, customer.CreatedAtUtc));
                }

                var scale = totalPieces > receivable && totalPieces > 0 ? receivable / totalPieces : 1m;

                foreach (var piece in unallocatedPieces)
                {
                    var amount = piece.Amount * scale;
                    var days = (asOf - piece.Date).TotalDays;

                    if (days < 1)
                        current += amount;
                    else if (days < 8)
                        d1to7 += amount;
                    else if (days < 16)
                        d8to15 += amount;
                    else if (days < 31)
                        d16to30 += amount;
                    else if (days < 61)
                        d31to60 += amount;
                    else
                        d60plus += amount;
                }
            }

            string statusText;
            if (advance > 0)
            {
                statusText = "Advance";
            }
            else if (receivable == 0)
            {
                statusText = "No Balance";
            }
            else if (isOverLimit)
            {
                statusText = "Over Limit";
            }
            else if ((d16to30 + d31to60 + d60plus) > 0)
            {
                statusText = "Overdue";
            }
            else
            {
                statusText = "Current";
            }

            var row = new CustomerReceivableRowDto(
                customer.Id.Value,
                customer.Code.Value,
                customer.Name,
                customer.MobileNumber,
                customer.CreditLimit,
                netBal,
                availableCredit,
                lastDate == default ? null : lastDate,
                Math.Round(current, 2),
                Math.Round(d1to7, 2),
                Math.Round(d8to15, 2),
                Math.Round(d16to30, 2),
                Math.Round(d31to60, 2),
                Math.Round(d60plus, 2),
                isOverLimit,
                customer.IsCreditAllowed,
                statusText,
                receivable,
                advance);

            // Filter check
            if (request.Filter == CustomerReceivablesFilter.HasBalanceOnly && row.Receivable <= 0)
                continue;
            if (request.Filter == CustomerReceivablesFilter.OverLimitOnly && !row.IsOverCreditLimit)
                continue;
            if (request.Filter == CustomerReceivablesFilter.AdvanceOnly && row.Advance <= 0)
                continue;

            if (!string.IsNullOrWhiteSpace(request.SearchText))
            {
                var s = request.SearchText.Trim();
                var matches = row.CustomerCode.Contains(s, StringComparison.OrdinalIgnoreCase) ||
                              row.CustomerName.Contains(s, StringComparison.OrdinalIgnoreCase) ||
                              row.MobileNumber.Contains(s, StringComparison.OrdinalIgnoreCase);
                if (!matches) continue;
            }

            rows.Add(row);
        }

        // Summary totals: keep Receivables and Advances separated (do NOT net them in header totals)
        decimal totalReceivables = rows.Sum(r => r.Receivable);
        decimal totalAdvances = rows.Sum(r => r.Advance);
        decimal netPosition = totalReceivables - totalAdvances;
        decimal totalOverLimit = rows.Where(r => r.IsOverCreditLimit).Sum(r => Math.Max(0m, r.Receivable - r.CreditLimit));
        int activeWithBalance = rows.Count(r => r.Receivable > 0);
        decimal avgBalance = activeWithBalance > 0 ? totalReceivables / activeWithBalance : 0m;

        return new CustomerReceivablesReportDto(
            totalReceivables,
            totalOverLimit,
            activeWithBalance,
            avgBalance,
            [.. rows.OrderByDescending(r => r.Receivable).ThenByDescending(r => r.Advance)],
            totalAdvances,
            netPosition);
    }
}
