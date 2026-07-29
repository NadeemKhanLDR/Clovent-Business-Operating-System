using Clovent.Restaurant.Application.Tables.Dtos;
using Clovent.Restaurant.Tables;
using MediatR;

namespace Clovent.Restaurant.Application.Tables.Commands;

/// <summary>Takes a table out of service.</summary>
public sealed record SetTableOutOfServiceCommand(Guid TableId) : IRequest<TableDto>;

/// <summary>Handles <see cref="SetTableOutOfServiceCommand"/>.</summary>
public sealed class SetTableOutOfServiceCommandHandler(ITableRepository repository) : IRequestHandler<SetTableOutOfServiceCommand, TableDto>
{
    /// <inheritdoc/>
    public async Task<TableDto> Handle(SetTableOutOfServiceCommand request, CancellationToken cancellationToken)
    {
        var table = await repository.GetByIdAsync(new TableId(request.TableId), cancellationToken)
            ?? throw new NotFoundException(nameof(Table), request.TableId);

        table.SetOutOfService();
        return TableDto.FromDomain(table);
    }
}
