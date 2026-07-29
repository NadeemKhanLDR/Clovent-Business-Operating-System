using Clovent.MasterData;
using Clovent.MasterData.Languages;
using Clovent.MasterData.Languages.Events;
using Clovent.MasterData.Shared;
using Xunit;

namespace Clovent.MasterData.Tests.Languages;

public class LanguageTests
{
    [Fact]
    public void Create_Valid_ActiveByDefault_RaisesLanguageCreated()
    {
        var language = Language.Create(LanguageCode.Create("EN"), "English", "English");

        Assert.Equal("en", language.Code.Value);
        Assert.Equal(MasterDataStatus.Active, language.Status);
        Assert.IsType<LanguageCreated>(Assert.Single(language.DomainEvents));
    }

    [Fact]
    public void Deactivate_AlreadyInactive_Throws()
    {
        var language = Language.Create(LanguageCode.Create("en"), "English", "English");
        language.Deactivate();

        Assert.Throws<MasterDataDomainException>(() => language.Deactivate());
    }
}

public class LanguageCodeTests
{
    [Theory]
    [InlineData("")]
    [InlineData("e")]
    [InlineData("eng")]
    [InlineData("E1")]
    public void Create_Invalid_Throws(string value)
    {
        Assert.Throws<ArgumentException>(() => LanguageCode.Create(value));
    }
}
