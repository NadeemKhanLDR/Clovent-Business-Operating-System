using Clovent.Restaurant.Application.Tables.Dtos;
using Clovent.Restaurant.Tables;
using MediatR;

namespace Clovent.Restaurant.Application.Tables.Commands;

/// <summary>Activates a table.</summary>
public sealed record ActivateTableCommand(Guid TableId) : IRequest<TableDto>;

/// <summary>Handles <see cref="ActivateTableCommand"/>.</summary>
public sealed class ActivateTableCommandHandler(ITableRepository repository) : IRequestHandler<ActivateTableCommand, TableDto>
{
    /// <inheritdoc/>
    public async Task<TableDto> Handle(ActivateTableCommand request, CancellationToken cancellationToken)
    {
        var table = await repository.GetByIdAsync(new TableId(request.TableId), cancellationToken)
            ?? throw new NotFoundException(nameof(Table), request.TableId);

        table.Activate();
        return TableDto.FromDomain(table);
    }
}
