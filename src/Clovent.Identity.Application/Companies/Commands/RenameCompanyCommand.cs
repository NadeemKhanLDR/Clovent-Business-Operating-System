using Clovent.Identity.Application.Companies.Dtos;
using Clovent.Identity.Companies;
using Clovent.Identity.Companies.ValueObjects;
using MediatR;

namespace Clovent.Identity.Application.Companies.Commands;

/// <summary>Renames an existing company.</summary>
public sealed record RenameCompanyCommand(Guid CompanyId, string Name) : IRequest<CompanyDto>;

/// <summary>Handles <see cref="RenameCompanyCommand"/>.</summary>
public sealed class RenameCompanyCommandHandler(ICompanyRepository companyRepository)
    : IRequestHandler<RenameCompanyCommand, CompanyDto>
{
    /// <inheritdoc/>
    public async Task<CompanyDto> Handle(RenameCompanyCommand request, CancellationToken cancellationToken)
    {
        var company = await companyRepository.GetByIdAsync(new CompanyId(request.CompanyId), cancellationToken)
            ?? throw new NotFoundException(nameof(Company), request.CompanyId);

        company.Rename(CompanyName.Create(request.Name));

        return CompanyDto.FromDomain(company);
    }
}
