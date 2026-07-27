namespace Clovent.Generator.Services.Entity;

using Clovent.Core.Models;
using Clovent.Core.Results;

public interface IEntityGenerator
{
    GenerationResult Generate(EntityGenerationOptions options);
}
