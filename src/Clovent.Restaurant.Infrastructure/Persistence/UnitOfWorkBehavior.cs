using Clovent.Restaurant.Application;
using Clovent.Restaurant.Application.Payments.Commands;
using Clovent.Restaurant.Application.Payments.Dtos;
using Clovent.Restaurant.Payments;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Clovent.Restaurant.Infrastructure.Persistence;

/// <summary>Commits <see cref="IUnitOfWork.SaveChangesAsync"/> after every MediatR request completes successfully. Mirrors <c>Clovent.Catalog.Infrastructure.Persistence.UnitOfWorkBehavior</c>.</summary>
public sealed class UnitOfWorkBehavior<TRequest, TResponse>(
    IUnitOfWork unitOfWork,
    IServiceProvider? serviceProvider = null) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    /// <inheritdoc/>
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var isCommand = !typeof(TRequest).Name.EndsWith("Query", StringComparison.Ordinal);
        try
        {
            var response = await next().ConfigureAwait(false);
            if (isCommand)
            {
                await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
            return response;
        }
        catch (DbUpdateException dbEx) when (request is RecordPaymentCommand payCmd &&
                                           !string.IsNullOrWhiteSpace(payCmd.IdempotencyKey) &&
                                           IsIdempotencyKeyViolation(dbEx))
        {
            // Strict unique-conflict recovery under TASK-05:
            // Discard tainted DbContext tracked state
            if (isCommand && unitOfWork is UnitOfWork efUnitOfWork)
            {
                efUnitOfWork.DiscardTrackedState();
            }

            var paymentRepo = serviceProvider?.GetService<IPaymentRepository>();
            if (paymentRepo is not null)
            {
                var committed = await paymentRepo.GetByIdempotencyKeyAsync(payCmd.IdempotencyKey!, cancellationToken).ConfigureAwait(false);
                if (committed is not null)
                {
                    if (committed.OrderId.Value != payCmd.OrderId ||
                        committed.PaymentMethodId.Value != payCmd.PaymentMethodId ||
                        committed.Amount != payCmd.Amount)
                    {
                        throw new InvalidOperationException(
                            $"Idempotency key '{payCmd.IdempotencyKey}' was already used for a different payment (Order: {committed.OrderId.Value}, PaymentMethod: {committed.PaymentMethodId.Value}, Amount: {committed.Amount:N2}). Reusing idempotency keys with differing payloads is prohibited.");
                    }

                    var dto = PaymentDto.FromDomain(committed);
                    if (dto is TResponse typedResponse)
                    {
                        return typedResponse;
                    }
                }
            }

            throw;
        }
        catch
        {
            // Handlers can mutate tracked aggregates before throwing. Skipping
            // SaveChanges alone is insufficient when the screen reuses its scope.
            if (isCommand && unitOfWork is UnitOfWork efUnitOfWork)
                efUnitOfWork.DiscardTrackedState();
            throw;
        }
    }

    private static bool IsIdempotencyKeyViolation(DbUpdateException ex)
    {
        var text = ex.ToString();
        return text.Contains("IX_Payments_IdempotencyKey", StringComparison.OrdinalIgnoreCase) ||
               (text.Contains("Payments", StringComparison.OrdinalIgnoreCase) &&
                text.Contains("IdempotencyKey", StringComparison.OrdinalIgnoreCase) &&
                text.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase));
    }
}
