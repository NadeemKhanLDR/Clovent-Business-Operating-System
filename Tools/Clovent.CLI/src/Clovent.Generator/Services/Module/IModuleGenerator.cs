namespace Clovent.Generator.Services.Module;

using Clovent.Core.Models;
using Clovent.Core.Results;

public interface IModuleGenerator
{
    GenerationResult Generate(ModuleGenerationOptions options);
}
