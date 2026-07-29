using Clovent.Identity.Application.Companies.Dtos;
using Clovent.Identity.Companies;
using Clovent.Identity.Shared.ValueObjects;
using MediatR;

namespace Clovent.Identity.Application.Companies.Commands;

/// <summary>Sets or clears a company's tax id.</summary>
public sealed record SetCompanyTaxIdCommand(Guid CompanyId, string? TaxId) : IRequest<CompanyDto>;

/// <summary>Handles <see cref="SetCompanyTaxIdCommand"/>.</summary>
public sealed class SetCompanyTaxIdCommandHandler(ICompanyRepository companyRepository)
    : IRequestHandler<SetCompanyTaxIdCommand, CompanyDto>
{
    /// <inheritdoc/>
    public async Task<CompanyDto> Handle(SetCompanyTaxIdCommand request, CancellationToken cancellationToken)
    {
        var company = await companyRepository.GetByIdAsync(new CompanyId(request.CompanyId), cancellationToken)
            ?? throw new NotFoundException(nameof(Company), request.CompanyId);

        company.SetTaxId(request.TaxId is null ? null : TaxId.Create(request.TaxId));

        return CompanyDto.FromDomain(company);
    }
}
