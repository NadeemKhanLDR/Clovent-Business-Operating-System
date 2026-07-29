using Clovent.Restaurant.Application.Tables.Dtos;
using Clovent.Restaurant.Tables;
using MediatR;

namespace Clovent.Restaurant.Application.Tables.Commands;

/// <summary>Deactivates a table.</summary>
public sealed record DeactivateTableCommand(Guid TableId) : IRequest<TableDto>;

/// <summary>Handles <see cref="DeactivateTableCommand"/>.</summary>
public sealed class DeactivateTableCommandHandler(ITableRepository repository) : IRequestHandler<DeactivateTableCommand, TableDto>
{
    /// <inheritdoc/>
    public async Task<TableDto> Handle(DeactivateTableCommand request, CancellationToken cancellationToken)
    {
        var table = await repository.GetByIdAsync(new TableId(request.TableId), cancellationToken)
            ?? throw new NotFoundException(nameof(Table), request.TableId);

        table.Deactivate();
        return TableDto.FromDomain(table);
    }
}
