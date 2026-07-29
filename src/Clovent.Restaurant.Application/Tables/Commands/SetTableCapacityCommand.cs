using Clovent.Restaurant.Application.Tables.Dtos;
using Clovent.Restaurant.Tables;
using MediatR;

namespace Clovent.Restaurant.Application.Tables.Commands;

/// <summary>Changes a table's seating capacity.</summary>
public sealed record SetTableCapacityCommand(Guid TableId, int Capacity) : IRequest<TableDto>;

/// <summary>Handles <see cref="SetTableCapacityCommand"/>.</summary>
public sealed class SetTableCapacityCommandHandler(ITableRepository repository) : IRequestHandler<SetTableCapacityCommand, TableDto>
{
    /// <inheritdoc/>
    public async Task<TableDto> Handle(SetTableCapacityCommand request, CancellationToken cancellationToken)
    {
        var table = await repository.GetByIdAsync(new TableId(request.TableId), cancellationToken)
            ?? throw new NotFoundException(nameof(Table), request.TableId);

        table.SetCapacity(request.Capacity);
        return TableDto.FromDomain(table);
    }
}
