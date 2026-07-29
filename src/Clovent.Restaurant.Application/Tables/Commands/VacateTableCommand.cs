using Clovent.Restaurant.Application.Tables.Dtos;
using Clovent.Restaurant.Tables;
using MediatR;

namespace Clovent.Restaurant.Application.Tables.Commands;

/// <summary>Frees a table.</summary>
public sealed record VacateTableCommand(Guid TableId) : IRequest<TableDto>;

/// <summary>Handles <see cref="VacateTableCommand"/>.</summary>
public sealed class VacateTableCommandHandler(ITableRepository repository) : IRequestHandler<VacateTableCommand, TableDto>
{
    /// <inheritdoc/>
    public async Task<TableDto> Handle(VacateTableCommand request, CancellationToken cancellationToken)
    {
        var table = await repository.GetByIdAsync(new TableId(request.TableId), cancellationToken)
            ?? throw new NotFoundException(nameof(Table), request.TableId);

        table.Vacate();
        return TableDto.FromDomain(table);
    }
}
