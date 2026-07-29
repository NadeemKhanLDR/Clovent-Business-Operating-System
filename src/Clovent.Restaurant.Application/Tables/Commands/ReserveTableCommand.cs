using Clovent.Restaurant.Application.Tables.Dtos;
using Clovent.Restaurant.Tables;
using MediatR;

namespace Clovent.Restaurant.Application.Tables.Commands;

/// <summary>Reserves a table for an upcoming party.</summary>
public sealed record ReserveTableCommand(Guid TableId) : IRequest<TableDto>;

/// <summary>Handles <see cref="ReserveTableCommand"/>.</summary>
public sealed class ReserveTableCommandHandler(ITableRepository repository) : IRequestHandler<ReserveTableCommand, TableDto>
{
    /// <inheritdoc/>
    public async Task<TableDto> Handle(ReserveTableCommand request, CancellationToken cancellationToken)
    {
        var table = await repository.GetByIdAsync(new TableId(request.TableId), cancellationToken)
            ?? throw new NotFoundException(nameof(Table), request.TableId);

        table.Reserve();
        return TableDto.FromDomain(table);
    }
}
