using Clovent.Restaurant.Application.Tables.Dtos;
using Clovent.Restaurant.Tables;
using MediatR;

namespace Clovent.Restaurant.Application.Tables.Commands;

/// <summary>Seats a table.</summary>
public sealed record OccupyTableCommand(Guid TableId) : IRequest<TableDto>;

/// <summary>Handles <see cref="OccupyTableCommand"/>.</summary>
public sealed class OccupyTableCommandHandler(ITableRepository repository) : IRequestHandler<OccupyTableCommand, TableDto>
{
    /// <inheritdoc/>
    public async Task<TableDto> Handle(OccupyTableCommand request, CancellationToken cancellationToken)
    {
        var table = await repository.GetByIdAsync(new TableId(request.TableId), cancellationToken)
            ?? throw new NotFoundException(nameof(Table), request.TableId);

        table.Occupy();
        return TableDto.FromDomain(table);
    }
}
