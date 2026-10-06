using Bunit;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Papra.Companion.Components.Pages.Settings;
using Papra.Companion.Models;
using Papra.Companion.Services.Interfaces;

namespace Papra.Companion.Tests.Components;

public class SettingsAiTests : ComponentTestBase
{
    private readonly ISettingsService _settingsSvc = Substitute.For<ISettingsService>();
    private readonly IOpenAiService _openAiSvc = Substitute.For<IOpenAiService>();

    public SettingsAiTests()
    {
        _settingsSvc.Current.Returns(new PipelineSettings
        {
            PapraBaseUrl = "https://papra.example.com",
            PapraApiToken = "papra-token",
            OpenAiApiKey = "sk-saved",
            OpenAiModel = "my-model",
            TitlePrompt = "Custom prompt",
            ProcessingDelaySeconds = 5,
        });

        Services.AddSingleton(_settingsSvc);
        Services.AddSingleton(_openAiSvc);
    }

    [Fact]
    public void SettingsAiShowsApiPromptAndDelayRows()
    {
        var cut = Render<SettingsAi>();

        Assert.NotNull(Row(cut, "API"));
        Assert.NotNull(Row(cut, "Title prompt"));
        Assert.NotNull(Row(cut, "Delay"));
    }

    [Fact]
    public void SettingsAiWithSavedKeyShowsPlaceholderHintWithoutTheKey()
    {
        var cut = Render<SettingsAi>();

        Assert.Contains("Saved — type to replace", cut.Markup);
        Assert.DoesNotContain("sk-saved", cut.Markup);
    }

    [Fact]
    public void SettingsAiEnablesSaveOnlyAfterAChange()
    {
        var cut = Render<SettingsAi>();
        Assert.True(Button(cut, "Save").HasAttribute("disabled"));

        Field(cut, "Seconds before processing").Input("30");

        Assert.False(Button(cut, "Save").HasAttribute("disabled"));
    }

    [Fact]
    public async Task SettingsAiTestConnectionUsesSavedKeyAndModel()
    {
        _openAiSvc.TestConnectionAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns("Connected");
        var cut = Render<SettingsAi>();

        await cut.InvokeAsync(() => Button(cut, "Test Connection").Click());

        await _openAiSvc.Received(1).TestConnectionAsync("", "sk-saved", "my-model", Arg.Any<CancellationToken>());
        Assert.Contains("Connected", cut.Markup);
    }

    [Fact]
    public async Task SettingsAiResetToDefaultSavesTheDefaultPrompt()
    {
        var cut = Render<SettingsAi>();

        await cut.InvokeAsync(() => Button(cut, "Reset to default").Click());
        await cut.InvokeAsync(() => Button(cut, "Save").Click());

        _settingsSvc.Received(1).Save(Arg.Is<PipelineSettings>(s => s.TitlePrompt == PipelineSettings.DefaultTitlePrompt));
    }

    [Fact]
    public async Task SettingsAiSaveWritesEveryAiSettingAndKeepsPapraAndTheSavedKey()
    {
        var cut = Render<SettingsAi>();
        Field(cut, "Model").Input("other-model");
        Field(cut, "Prompt").Input("New prompt");
        Field(cut, "Seconds before processing").Input("30");

        await cut.InvokeAsync(() => Button(cut, "Save").Click());

        _settingsSvc.Received(1).Save(Arg.Is<PipelineSettings>(s =>
            s.OpenAiModel == "other-model" &&
            s.TitlePrompt == "New prompt" &&
            s.ProcessingDelaySeconds == 30 &&
            s.OpenAiApiKey == "sk-saved" &&
            s.PapraBaseUrl == "https://papra.example.com" &&
            s.PapraApiToken == "papra-token"));
    }
}
