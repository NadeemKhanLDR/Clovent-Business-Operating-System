using Clovent.Restaurant.Application.Tables.Dtos;
using Clovent.Restaurant.Tables;
using Clovent.Restaurant.Tables.ValueObjects;
using MediatR;

namespace Clovent.Restaurant.Application.Tables.Commands;

/// <summary>Updates a table's name and seating capacity.</summary>
public sealed record UpdateTableCommand(Guid TableId, string Name, int Capacity) : IRequest<TableDto>;

/// <summary>Handles <see cref="UpdateTableCommand"/>.</summary>
public sealed class UpdateTableCommandHandler(ITableRepository repository) : IRequestHandler<UpdateTableCommand, TableDto>
{
    /// <inheritdoc/>
    public async Task<TableDto> Handle(UpdateTableCommand request, CancellationToken cancellationToken)
    {
        var table = await repository.GetByIdAsync(new TableId(request.TableId), cancellationToken)
            ?? throw new NotFoundException(nameof(Table), request.TableId);

        table.Rename(TableName.Create(request.Name));
        table.SetCapacity(request.Capacity);

        return TableDto.FromDomain(table);
    }
}
