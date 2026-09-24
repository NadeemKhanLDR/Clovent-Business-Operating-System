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
        var diningAreaId = new DiningAreaId(request.DiningAreaId);
        var code = EntityCode.Create(request.Code);

        var existing = await repository.GetByCodeAsync(diningAreaId, code, cancellationToken);
        if (existing is not null)
        {
            throw RestaurantDomainException.TableCodeAlreadyExists(code);
        }

        var table = Table.Create(diningAreaId, code, TableName.Create(request.Name), request.Capacity);

        await repository.AddAsync(table, cancellationToken);

        return TableDto.FromDomain(table);
    }
}
