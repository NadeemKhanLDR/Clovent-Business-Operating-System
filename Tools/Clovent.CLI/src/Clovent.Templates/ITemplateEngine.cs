namespace Clovent.Templates;

using Clovent.Core.Models;

public interface ITemplateEngine
{
    string RenderEntityClass(EntityGenerationOptions options);
    string RenderEntityIdValueObject(EntityGenerationOptions options);
    string RenderDomainEventClass(EntityGenerationOptions options, string eventName);
    string RenderCreateCommand(EntityGenerationOptions options);
    string RenderCreateCommandHandler(EntityGenerationOptions options);
    string RenderGetByIdQuery(EntityGenerationOptions options);
    string RenderGetByIdQueryHandler(EntityGenerationOptions options);
    string RenderEfCoreConfiguration(EntityGenerationOptions options);
    string RenderDevExpressView(EntityGenerationOptions options);
    string RenderObsidianModuleDoc(ModuleGenerationOptions options);
    string RenderObsidianEntityDoc(EntityGenerationOptions options);
}
