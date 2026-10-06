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
        repo.Find().Returns((PipelineSettingsEntity?)null);
        return repo;
    }

    [Fact]
    public void CurrentWhenRepositoryEmptyReturnsDefaults()
    {
        var service = new SettingsService(EmptyRepository());

        var current = service.Current;

        Assert.Equal(string.Empty, current.PapraBaseUrl);
        Assert.Equal(string.Empty, current.OpenAiBaseUrl);
        Assert.Equal("gpt-4o-mini", current.OpenAiModel);
        Assert.Equal(PipelineSettings.DefaultTitlePrompt, current.TitlePrompt);
        Assert.Equal(0, current.ProcessingDelaySeconds);
    }

    [Fact]
    public void CurrentWhenRepositoryHasEntityMapsAllFields()
    {
        var entity = new PipelineSettingsEntity
        {
            PapraBaseUrl           = "https://papra.example.com",
            PapraApiToken          = "tok",
            OpenAiBaseUrl          = "http://localhost:11434/v1/",
            OpenAiApiKey           = "sk-key",
            OpenAiModel            = "gpt-4o",
            TitlePrompt            = "title prompt",
            ProcessingDelaySeconds = 30,
        };
        var repo = Substitute.For<IPipelineSettingsRepository>();
        repo.Find().Returns(entity);

        var service = new SettingsService(repo);
        var current = service.Current;

        Assert.Equal("https://papra.example.com", current.PapraBaseUrl);
        Assert.Equal("tok", current.PapraApiToken);
        Assert.Equal("http://localhost:11434/v1/", current.OpenAiBaseUrl);
        Assert.Equal("sk-key", current.OpenAiApiKey);
        Assert.Equal("gpt-4o", current.OpenAiModel);
        Assert.Equal("title prompt", current.TitlePrompt);
        Assert.Equal(30, current.ProcessingDelaySeconds);
    }

    [Fact]
    public void CurrentWhenRepositoryHasEmptyTitlePromptFallsBackToDefault()
    {
        var entity = new PipelineSettingsEntity { TitlePrompt = "" };
        var repo = Substitute.For<IPipelineSettingsRepository>();
        repo.Find().Returns(entity);

        var service = new SettingsService(repo);

        Assert.Equal(PipelineSettings.DefaultTitlePrompt, service.Current.TitlePrompt);
    }

    [Fact]
    public void SaveUpdatesCurrentImmediately()
    {
        var service = new SettingsService(EmptyRepository());

        service.Save(new PipelineSettings
        {
            PapraBaseUrl  = "https://new.example.com",
            OpenAiBaseUrl = "http://localhost:11434/v1/",
            OpenAiApiKey  = "sk-new",
            ProcessingDelaySeconds = 10,
        });

        Assert.Equal("https://new.example.com", service.Current.PapraBaseUrl);
        Assert.Equal("http://localhost:11434/v1/", service.Current.OpenAiBaseUrl);
        Assert.Equal("sk-new", service.Current.OpenAiApiKey);
        Assert.Equal(10, service.Current.ProcessingDelaySeconds);
    }

    [Fact]
    public void SaveRaisesOnChangedEvent()
    {
        var service = new SettingsService(EmptyRepository());
        var raised = false;
        service.OnChanged += () => raised = true;

        service.Save(new PipelineSettings());

        Assert.True(raised);
    }
}
