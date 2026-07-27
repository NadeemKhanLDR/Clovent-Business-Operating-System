namespace Clovent.Templates;

using System.Text;
using Clovent.Core.Models;

public sealed class TemplateEngine : ITemplateEngine
{
    public string RenderEntityClass(EntityGenerationOptions options)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"namespace Clovent.Modules.{options.ModuleName}.Domain.Entities;");
        sb.AppendLine();
        sb.AppendLine($"using Clovent.Modules.{options.ModuleName}.Domain.ValueObjects;");
        sb.AppendLine("using Clovent.Shared.Domain;");
        sb.AppendLine();
        sb.AppendLine($"public class {options.EntityName} : AggregateRoot<{options.EntityName}Id>");
        sb.AppendLine("{");
        sb.AppendLine($"    public {options.EntityName}Id Id {{ get; private set; }}");

        foreach (var prop in options.Properties)
        {
            var nullable = prop.IsRequired ? "" : "?";
            sb.AppendLine($"    public {prop.Type}{nullable} {prop.Name} {{ get; private set; }}");
        }

        sb.AppendLine("    public DateTime CreatedAtUtc { get; private set; }");
        sb.AppendLine("    public DateTime? UpdatedAtUtc { get; private set; }");
        sb.AppendLine();
        sb.AppendLine($"    private {options.EntityName}() {{ }}");
        sb.AppendLine();

        // Factory method
        var paramList = string.Join(", ", options.Properties.Select(p => $"{p.Type} {ToCamelCase(p.Name)}"));
        sb.AppendLine($"    public static {options.EntityName} Create({paramList})");
        sb.AppendLine("    {");
        sb.AppendLine($"        var entity = new {options.EntityName}");
        sb.AppendLine("        {");
        sb.AppendLine($"            Id = {options.EntityName}Id.New(),");
        foreach (var prop in options.Properties)
        {
            sb.AppendLine($"            {prop.Name} = {ToCamelCase(prop.Name)},");
        }
        sb.AppendLine("            CreatedAtUtc = DateTime.UtcNow");
        sb.AppendLine("        };");
        sb.AppendLine();
        sb.AppendLine($"        entity.AddDomainEvent(new {options.EntityName}CreatedEvent(entity.Id));");
        sb.AppendLine("        return entity;");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }

    public string RenderEntityIdValueObject(EntityGenerationOptions options)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"namespace Clovent.Modules.{options.ModuleName}.Domain.ValueObjects;");
        sb.AppendLine();
        sb.AppendLine("using Clovent.Shared.Domain;");
        sb.AppendLine();
        sb.AppendLine($"public readonly record struct {options.EntityName}Id(Guid Value)");
        sb.AppendLine("{");
        sb.AppendLine($"    public static {options.EntityName}Id New() => new(Guid.NewGuid());");
        sb.AppendLine($"    public static {options.EntityName}Id Empty => new(Guid.Empty);");
        sb.AppendLine("}");
        return sb.ToString();
    }

    public string RenderDomainEventClass(EntityGenerationOptions options, string eventName)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"namespace Clovent.Modules.{options.ModuleName}.Domain.Events;");
        sb.AppendLine();
        sb.AppendLine($"using Clovent.Modules.{options.ModuleName}.Domain.ValueObjects;");
        sb.AppendLine("using Clovent.Shared.Domain;");
        sb.AppendLine();
        sb.AppendLine($"public sealed record {options.EntityName}{eventName}Event({options.EntityName}Id {options.EntityName}Id) : IDomainEvent;");
        return sb.ToString();
    }

    public string RenderCreateCommand(EntityGenerationOptions options)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"namespace Clovent.Modules.{options.ModuleName}.Application.Commands;");
        sb.AppendLine();
        sb.AppendLine($"using Clovent.Modules.{options.ModuleName}.Domain.ValueObjects;");
        sb.AppendLine("using MediatR;");
        sb.AppendLine();
        var props = string.Join(", ", options.Properties.Select(p => $"{p.Type} {p.Name}"));
        sb.AppendLine($"public sealed record Create{options.EntityName}Command({props}) : IRequest<{options.EntityName}Id>;");
        return sb.ToString();
    }

    public string RenderCreateCommandHandler(EntityGenerationOptions options)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"namespace Clovent.Modules.{options.ModuleName}.Application.Handlers;");
        sb.AppendLine();
        sb.AppendLine($"using Clovent.Modules.{options.ModuleName}.Application.Commands;");
        sb.AppendLine($"using Clovent.Modules.{options.ModuleName}.Domain.Entities;");
        sb.AppendLine($"using Clovent.Modules.{options.ModuleName}.Domain.ValueObjects;");
        sb.AppendLine($"using Clovent.Modules.{options.ModuleName}.Infrastructure.Persistence;");
        sb.AppendLine("using MediatR;");
        sb.AppendLine();
        sb.AppendLine($"public sealed class Create{options.EntityName}CommandHandler : IRequestHandler<Create{options.EntityName}Command, {options.EntityName}Id>");
        sb.AppendLine("{");
        sb.AppendLine($"    private readonly {options.ModuleName}DbContext _dbContext;");
        sb.AppendLine();
        sb.AppendLine($"    public Create{options.EntityName}CommandHandler({options.ModuleName}DbContext dbContext)");
        sb.AppendLine("    {");
        sb.AppendLine("        _dbContext = dbContext;");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine($"    public async Task<{options.EntityName}Id> Handle(Create{options.EntityName}Command request, CancellationToken cancellationToken)");
        sb.AppendLine("    {");
        var args = string.Join(", ", options.Properties.Select(p => $"request.{p.Name}"));
        sb.AppendLine($"        var entity = {options.EntityName}.Create({args});");
        sb.AppendLine($"        await _dbContext.Set<{options.EntityName}>().AddAsync(entity, cancellationToken);");
        sb.AppendLine("        await _dbContext.SaveChangesAsync(cancellationToken);");
        sb.AppendLine("        return entity.Id;");
        sb.AppendLine("    }");
        sb.AppendLine("}");
        return sb.ToString();
    }

    public string RenderGetByIdQuery(EntityGenerationOptions options)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"namespace Clovent.Modules.{options.ModuleName}.Application.Queries;");
        sb.AppendLine();
        sb.AppendLine($"using Clovent.Modules.{options.ModuleName}.Application.DTOs;");
        sb.AppendLine($"using Clovent.Modules.{options.ModuleName}.Domain.ValueObjects;");
        sb.AppendLine("using MediatR;");
        sb.AppendLine();
        sb.AppendLine($"public sealed record Get{options.EntityName}ByIdQuery({options.EntityName}Id Id) : IRequest<{options.EntityName}Dto?>;");
        return sb.ToString();
    }

    public string RenderGetByIdQueryHandler(EntityGenerationOptions options)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"namespace Clovent.Modules.{options.ModuleName}.Application.Handlers;");
        sb.AppendLine();
        sb.AppendLine($"using Clovent.Modules.{options.ModuleName}.Application.DTOs;");
        sb.AppendLine($"using Clovent.Modules.{options.ModuleName}.Application.Queries;");
        sb.AppendLine($"using Clovent.Modules.{options.ModuleName}.Infrastructure.Persistence;");
        sb.AppendLine("using MediatR;");
        sb.AppendLine("using Microsoft.EntityFrameworkCore;");
        sb.AppendLine();
        sb.AppendLine($"public sealed class Get{options.EntityName}ByIdQueryHandler : IRequestHandler<Get{options.EntityName}ByIdQuery, {options.EntityName}Dto?>");
        sb.AppendLine("{");
        sb.AppendLine($"    private readonly {options.ModuleName}DbContext _dbContext;");
        sb.AppendLine();
        sb.AppendLine($"    public Get{options.EntityName}ByIdQueryHandler({options.ModuleName}DbContext dbContext)");
        sb.AppendLine("    {");
        sb.AppendLine("        _dbContext = dbContext;");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine($"    public async Task<{options.EntityName}Dto?> Handle(Get{options.EntityName}ByIdQuery request, CancellationToken cancellationToken)");
        sb.AppendLine("    {");
        sb.AppendLine($"        var entity = await _dbContext.Set<Domain.Entities.{options.EntityName}>()");
        sb.AppendLine("            .AsNoTracking()");
        sb.AppendLine("            .FirstOrDefaultAsync(e => e.Id == request.Id, cancellationToken);");
        sb.AppendLine();
        sb.AppendLine("        if (entity is null) return null;");
        sb.AppendLine();
        var dtoArgs = string.Join(", ", options.Properties.Select(p => $"entity.{p.Name}"));
        sb.AppendLine($"        return new {options.EntityName}Dto(entity.Id.Value, {dtoArgs});");
        sb.AppendLine("    }");
        sb.AppendLine("}");
        return sb.ToString();
    }

    public string RenderEfCoreConfiguration(EntityGenerationOptions options)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"namespace Clovent.Modules.{options.ModuleName}.Infrastructure.Persistence.Configurations;");
        sb.AppendLine();
        sb.AppendLine($"using Clovent.Modules.{options.ModuleName}.Domain.Entities;");
        sb.AppendLine($"using Clovent.Modules.{options.ModuleName}.Domain.ValueObjects;");
        sb.AppendLine("using Microsoft.EntityFrameworkCore;");
        sb.AppendLine("using Microsoft.EntityFrameworkCore.Metadata.Builders;");
        sb.AppendLine();
        sb.AppendLine($"public sealed class {options.EntityName}Configuration : IEntityTypeConfiguration<{options.EntityName}>");
        sb.AppendLine("{");
        sb.AppendLine($"    public void Configure(EntityTypeBuilder<{options.EntityName}> builder)");
        sb.AppendLine("    {");
        sb.AppendLine($"        builder.ToTable(\"{options.EntityName}s\");");
        sb.AppendLine("        builder.HasKey(e => e.Id);");
        sb.AppendLine($"        builder.Property(e => e.Id)");
        sb.AppendLine($"            .HasConversion(id => id.Value, value => new {options.EntityName}Id(value));");

        foreach (var prop in options.Properties)
        {
            var pBuilder = $"        builder.Property(e => e.{prop.Name})";
            if (prop.IsRequired) pBuilder += ".IsRequired()";
            if (prop.MaxLength.HasValue) pBuilder += $".HasMaxLength({prop.MaxLength.Value})";
            sb.AppendLine(pBuilder + ";");
        }

        sb.AppendLine("    }");
        sb.AppendLine("}");
        return sb.ToString();
    }

    public string RenderDevExpressView(EntityGenerationOptions options)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"namespace Clovent.Modules.{options.ModuleName}.UI.Views;");
        sb.AppendLine();
        sb.AppendLine("using System.Windows.Forms;");
        sb.AppendLine("using DevExpress.XtraEditors;");
        sb.AppendLine("using DevExpress.XtraGrid;");
        sb.AppendLine();
        sb.AppendLine($"public partial class {options.EntityName}View : XtraUserControl");
        sb.AppendLine("{");
        sb.AppendLine("    private GridControl _gridControl = null!;");
        sb.AppendLine();
        sb.AppendLine($"    public {options.EntityName}View()");
        sb.AppendLine("    {");
        sb.AppendLine("        InitializeComponent();");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    private void InitializeComponent()");
        sb.AppendLine("    {");
        sb.AppendLine("        _gridControl = new GridControl();");
        sb.AppendLine("        _gridControl.Dock = DockStyle.Fill;");
        sb.AppendLine("        Controls.Add(_gridControl);");
        sb.AppendLine("    }");
        sb.AppendLine("}");
        return sb.ToString();
    }

    public string RenderObsidianModuleDoc(ModuleGenerationOptions options)
    {
        var sb = new StringBuilder();
        sb.AppendLine("---");
        sb.AppendLine($"title: {options.Name} Module");
        sb.AppendLine("type: Architecture");
        sb.AppendLine("tags:");
        sb.AppendLine("  - cbos");
        sb.AppendLine("  - module");
        sb.AppendLine($"  - {options.Name.ToLowerInvariant()}");
        sb.AppendLine($"created: {DateTime.UtcNow:yyyy-MM-dd}");
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine($"# {options.Name} Module Architecture");
        sb.AppendLine();
        sb.AppendLine($"The **{options.Name}** module implements Clean Architecture and DDD principles within Clovent Business Operating System.");
        sb.AppendLine();
        sb.AppendLine("## Structure");
        sb.AppendLine("- `Domain`: Aggregates, Entities, Value Objects, Events, Specifications.");
        sb.AppendLine("- `Application`: Commands, Queries, Handlers, DTOs, Validators.");
        sb.AppendLine("- `Infrastructure`: EF Core DbContext, Configurations, Repositories.");
        sb.AppendLine("- `UI`: DevExpress WinForms User Controls.");
        return sb.ToString();
    }

    public string RenderObsidianEntityDoc(EntityGenerationOptions options)
    {
        var sb = new StringBuilder();
        sb.AppendLine("---");
        sb.AppendLine($"title: {options.EntityName} Aggregate");
        sb.AppendLine($"module: {options.ModuleName}");
        sb.AppendLine("type: DomainModel");
        sb.AppendLine($"created: {DateTime.UtcNow:yyyy-MM-dd}");
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine($"# {options.EntityName} Aggregate Root");
        sb.AppendLine();
        sb.AppendLine($"Domain model specification for `{options.EntityName}` in [[{options.ModuleName} Module]].");
        sb.AppendLine();
        sb.AppendLine("## Properties");
        sb.AppendLine("| Property | Type | Required | Max Length |");
        sb.AppendLine("| --- | --- | --- | --- |");
        foreach (var prop in options.Properties)
        {
            sb.AppendLine($"| {prop.Name} | {prop.Type} | {prop.IsRequired} | {prop.MaxLength?.ToString() ?? "N/A"} |");
        }
        return sb.ToString();
    }

    private static string ToCamelCase(string str)
    {
        if (string.IsNullOrEmpty(str) || char.IsLower(str[0])) return str;
        return char.ToLowerInvariant(str[0]) + str.Substring(1);
    }
}
