namespace Clovent.Generator.Tests;

using Clovent.Core.Models;
using Clovent.Templates;
using Xunit;

public class TemplateEngineTests
{
    private readonly TemplateEngine _sut = new();

    [Fact]
    public void RenderEntityClass_ShouldGenerateValidCSharpCode()
    {
        var options = new EntityGenerationOptions
        {
            ModuleName = "Ordering",
            EntityName = "Order",
            Properties = new()
            {
                new EntityPropertyDefinition { Name = "CustomerName", Type = "string", IsRequired = true, MaxLength = 100 },
                new EntityPropertyDefinition { Name = "TotalAmount", Type = "decimal", IsRequired = true }
            }
        };

        var code = _sut.RenderEntityClass(options);

        Assert.Contains("namespace Clovent.Modules.Ordering.Domain.Entities;", code);
        Assert.Contains("public class Order : AggregateRoot<OrderId>", code);
        Assert.Contains("public string CustomerName { get; private set; }", code);
        Assert.Contains("public decimal TotalAmount { get; private set; }", code);
        Assert.Contains("public static Order Create(string customerName, decimal totalAmount)", code);
    }

    [Fact]
    public void RenderEntityIdValueObject_ShouldGenerateTypedId()
    {
        var options = new EntityGenerationOptions
        {
            ModuleName = "Ordering",
            EntityName = "Order"
        };

        var code = _sut.RenderEntityIdValueObject(options);

        Assert.Contains("namespace Clovent.Modules.Ordering.Domain.ValueObjects;", code);
        Assert.Contains("public readonly record struct OrderId(Guid Value)", code);
    }
}
