using NSubstitute;
using Papra.Companion.Data.Entities;
using Papra.Companion.Data.Repositories.Interfaces;
using Papra.Companion.Models;
using Papra.Companion.Services;

namespace Papra.Companion.Tests.Services;

public class SettingsServiceTests
{
    private static IPipelineSettingsRepository EmptyRepository()
    {
        var repo = Substitute.For<IPipelineSettingsRepository>();
        repo.Get().Returns((PipelineSettingsEntity?)null);
        return repo;
    }

    [Fact]
    public void Current_WhenRepositoryEmpty_ReturnsDefaults()
    {
        var service = new SettingsService(EmptyRepository());

        var current = service.Current;

        Assert.Equal(string.Empty, current.PapraBaseUrl);
        Assert.Equal("gpt-4o-mini", current.OpenAiModel);
        Assert.Equal(PipelineSettings.DefaultTitlePrompt, current.TitlePrompt);
        Assert.Equal(PipelineSettings.DefaultTagPrompt, current.TagPrompt);
        Assert.Equal(PipelineSettings.DefaultOcrPrompt, current.OcrPrompt);
    }

    [Fact]
    public void Current_WhenRepositoryHasEntity_MapsAllFields()
    {
        var entity = new PipelineSettingsEntity
        {
            PapraBaseUrl = "https://papra.example.com",
            PapraApiToken = "tok",
            OpenAiApiKey = "sk-key",
            OpenAiModel = "gpt-4o",
            MistralApiKey = "mist",
            TitlePrompt = "title prompt",
            TagPrompt = "tag prompt",
            OcrPrompt = "ocr prompt",
        };
        var repo = Substitute.For<IPipelineSettingsRepository>();
        repo.Get().Returns(entity);

        var service = new SettingsService(repo);
        var current = service.Current;

        Assert.Equal("https://papra.example.com", current.PapraBaseUrl);
        Assert.Equal("tok", current.PapraApiToken);
        Assert.Equal("sk-key", current.OpenAiApiKey);
        Assert.Equal("gpt-4o", current.OpenAiModel);
        Assert.Equal("mist", current.MistralApiKey);
        Assert.Equal("title prompt", current.TitlePrompt);
        Assert.Equal("tag prompt", current.TagPrompt);
        Assert.Equal("ocr prompt", current.OcrPrompt);
    }

    [Fact]
    public void Current_WhenRepositoryHasEmptyPrompts_FallsBackToDefaults()
    {
        var entity = new PipelineSettingsEntity { TitlePrompt = "", TagPrompt = "  ", OcrPrompt = "" };
        var repo = Substitute.For<IPipelineSettingsRepository>();
        repo.Get().Returns(entity);

        var service = new SettingsService(repo);

        Assert.Equal(PipelineSettings.DefaultTitlePrompt, service.Current.TitlePrompt);
        Assert.Equal(PipelineSettings.DefaultTagPrompt, service.Current.TagPrompt);
        Assert.Equal(PipelineSettings.DefaultOcrPrompt, service.Current.OcrPrompt);
    }

    [Fact]
    public void Save_UpdatesCurrentImmediately()
    {
        var service = new SettingsService(EmptyRepository());

        service.Save(new PipelineSettings
        {
            PapraBaseUrl = "https://new.example.com",
            OpenAiApiKey = "sk-new"
        });

        Assert.Equal("https://new.example.com", service.Current.PapraBaseUrl);
        Assert.Equal("sk-new", service.Current.OpenAiApiKey);
    }

    [Fact]
    public void Save_RaisesOnChangedEvent()
    {
        var service = new SettingsService(EmptyRepository());
        var raised = false;
        service.OnChanged += () => raised = true;

        service.Save(new PipelineSettings());

        Assert.True(raised);
    }
}
