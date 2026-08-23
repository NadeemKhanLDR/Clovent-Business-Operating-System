using Clovent.MasterData.Shared.ValueObjects;
using Clovent.Restaurant.Application.Tables.Dtos;
using Clovent.Restaurant.DiningAreas;
using Clovent.Restaurant.Tables;
using MediatR;

using Clovent.Restaurant.Tables.ValueObjects;

namespace Clovent.Restaurant.Application.Tables.Commands;

/// <summary>Creates a new table under a dining area.</summary>
public sealed record CreateTableCommand(Guid DiningAreaId, string Code, string Name, int Capacity) : IRequest<TableDto>
{
    /// <summary>Backward compatibility constructor that uses the code as the table name.</summary>
    public CreateTableCommand(Guid diningAreaId, string code, int capacity)
        : this(diningAreaId, code, code, capacity)
    {
    }
}

/// <summary>Handles <see cref="CreateTableCommand"/>.</summary>
public sealed class CreateTableCommandHandler(ITableRepository repository) : IRequestHandler<CreateTableCommand, TableDto>
{
    /// <inheritdoc/>
    public async Task<TableDto> Handle(CreateTableCommand request, CancellationToken cancellationToken)
    {
        var table = Table.Create(new DiningAreaId(request.DiningAreaId), EntityCode.Create(request.Code), TableName.Create(request.Name), request.Capacity);

        await repository.AddAsync(table, cancellationToken);

        return TableDto.FromDomain(table);
    }
}
