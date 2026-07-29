using Clovent.MasterData.Application.Languages.Commands;
using Clovent.MasterData.Application.Languages.Queries;
using Clovent.MasterData.Application.Tests.TestSupport;
using Clovent.MasterData.Languages;
using Xunit;

namespace Clovent.MasterData.Application.Tests.Languages;

public class LanguageHandlerTests
{
    [Fact]
    public async Task CreateLanguageCommandHandler_ValidRequest_PersistsAndReturnsDto()
    {
        var repository = new FakeLanguageRepository();
        var handler = new CreateLanguageCommandHandler(repository);

        var dto = await handler.Handle(new CreateLanguageCommand("EN", "English", "English"), CancellationToken.None);

        Assert.Equal("en", dto.Code);
        Assert.Equal("Active", dto.Status);
        Assert.NotNull(await repository.GetByIdAsync(new LanguageId(dto.LanguageId)));
    }

    [Fact]
    public async Task ActivateAndDeactivateLanguageCommandHandlers_RoundTrip()
    {
        var repository = new FakeLanguageRepository();
        var language = Language.Create(LanguageCode.Create("en"), "English", "English");
        language.Deactivate();
        repository.Add(language);

        var activated = await new ActivateLanguageCommandHandler(repository)
            .Handle(new ActivateLanguageCommand(language.Id.Value), CancellationToken.None);
        Assert.Equal("Active", activated.Status);

        var deactivated = await new DeactivateLanguageCommandHandler(repository)
            .Handle(new DeactivateLanguageCommand(language.Id.Value), CancellationToken.None);
        Assert.Equal("Inactive", deactivated.Status);
    }

    [Fact]
    public async Task GetLanguageByIdQueryHandler_UnknownLanguage_Throws()
    {
        var handler = new GetLanguageByIdQueryHandler(new FakeLanguageRepository());

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new GetLanguageByIdQuery(Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task ListLanguagesQueryHandler_ReturnsEveryLanguage()
    {
        var repository = new FakeLanguageRepository();
        repository.Add(Language.Create(LanguageCode.Create("en"), "English", "English"));
        repository.Add(Language.Create(LanguageCode.Create("es"), "Spanish", "Español"));
        var handler = new ListLanguagesQueryHandler(repository);

        var result = await handler.Handle(new ListLanguagesQuery(), CancellationToken.None);

        Assert.Equal(2, result.Count);
    }
}
