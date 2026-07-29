using Clovent.Restaurant.Application.Tables.Dtos;
using Clovent.Restaurant.Tables;
using MediatR;

namespace Clovent.Restaurant.Application.Tables.Commands;

/// <summary>Returns an out-of-service table to available.</summary>
public sealed record ReturnTableToServiceCommand(Guid TableId) : IRequest<TableDto>;

/// <summary>Handles <see cref="ReturnTableToServiceCommand"/>.</summary>
public sealed class ReturnTableToServiceCommandHandler(ITableRepository repository) : IRequestHandler<ReturnTableToServiceCommand, TableDto>
{
    /// <inheritdoc/>
    public async Task<TableDto> Handle(ReturnTableToServiceCommand request, CancellationToken cancellationToken)
    {
        var table = await repository.GetByIdAsync(new TableId(request.TableId), cancellationToken)
            ?? throw new NotFoundException(nameof(Table), request.TableId);

        table.ReturnToService();
        return TableDto.FromDomain(table);
    }
}
