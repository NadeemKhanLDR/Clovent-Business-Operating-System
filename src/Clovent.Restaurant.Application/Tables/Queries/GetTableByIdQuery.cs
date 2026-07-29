using Clovent.Restaurant.Application.Tables.Dtos;
using Clovent.Restaurant.Tables;
using MediatR;

namespace Clovent.Restaurant.Application.Tables.Queries;

/// <summary>Retrieves a table by id.</summary>
public sealed record GetTableByIdQuery(Guid TableId) : IRequest<TableDto>;

/// <summary>Handles <see cref="GetTableByIdQuery"/>.</summary>
public sealed class GetTableByIdQueryHandler(ITableRepository repository) : IRequestHandler<GetTableByIdQuery, TableDto>
{
    /// <inheritdoc/>
    public async Task<TableDto> Handle(GetTableByIdQuery request, CancellationToken cancellationToken)
    {
        var table = await repository.GetByIdAsync(new TableId(request.TableId), cancellationToken)
            ?? throw new NotFoundException(nameof(Table), request.TableId);

        return TableDto.FromDomain(table);
    }
}
