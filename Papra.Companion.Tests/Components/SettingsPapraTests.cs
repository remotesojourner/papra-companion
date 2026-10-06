using Bunit;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Papra.Companion.Components.Pages.Settings;
using Papra.Companion.Models;
using Papra.Companion.Services.Interfaces;

namespace Papra.Companion.Tests.Components;

public class SettingsPapraTests : ComponentTestBase
{
    private readonly ISettingsService _settingsSvc = Substitute.For<ISettingsService>();
    private readonly IPapraService _papraSvc = Substitute.For<IPapraService>();

    public SettingsPapraTests()
    {
        _settingsSvc.Current.Returns(new PipelineSettings
        {
            PapraBaseUrl = "https://papra.example.com",
            PapraApiToken = "saved-token",
            OpenAiApiKey = "sk-saved",
            OpenAiModel = "my-model",
        });

        Services.AddSingleton(_settingsSvc);
        Services.AddSingleton(_papraSvc);
    }

    [Fact]
    public void SettingsPapraShowsConnectionAndWebhookRows()
    {
        var cut = Render<SettingsPapra>();

        Assert.NotNull(Row(cut, "Connection"));
        Assert.Contains("/webhook/document", Row(cut, "Webhook").InnerHtml);
    }

    [Fact]
    public void SettingsPapraWithSavedTokenShowsPlaceholderHintWithoutTheToken()
    {
        var cut = Render<SettingsPapra>();

        Assert.Contains("Saved — type to replace", cut.Markup);
        Assert.DoesNotContain("saved-token", cut.Markup);
    }

    [Fact]
    public void SettingsPapraWithoutSavedTokenShowsTokenPlaceholder()
    {
        _settingsSvc.Current.Returns(new PipelineSettings());

        var cut = Render<SettingsPapra>();

        Assert.DoesNotContain("Saved — type to replace", cut.Markup);
        Assert.Contains("Bearer token from Papra settings", cut.Markup);
    }

    [Fact]
    public void SettingsPapraEnablesSaveOnlyAfterAChange()
    {
        var cut = Render<SettingsPapra>();
        Assert.True(Button(cut, "Save").HasAttribute("disabled"));
        Assert.DoesNotContain("Unsaved changes", cut.Markup);

        Field(cut, "Base URL").Input("https://other.example.com");

        Assert.False(Button(cut, "Save").HasAttribute("disabled"));
        Assert.Contains("Unsaved changes", cut.Markup);
    }

    [Fact]
    public async Task SettingsPapraTestConnectionUsesTypedUrlAndSavedToken()
    {
        _papraSvc.TestConnectionAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns("Connected");
        var cut = Render<SettingsPapra>();
        Field(cut, "Base URL").Input("https://other.example.com/");

        await cut.InvokeAsync(() => Button(cut, "Test Connection").Click());

        await _papraSvc.Received(1).TestConnectionAsync("https://other.example.com", "saved-token", Arg.Any<CancellationToken>());
        Assert.Contains("Connected", cut.Markup);
    }

    [Fact]
    public async Task SettingsPapraTestConnectionWhenFailsShowsTheReason()
    {
        _papraSvc.TestConnectionAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                 .ThrowsAsync(new HttpRequestException("unreachable"));
        var cut = Render<SettingsPapra>();

        await cut.InvokeAsync(() => Button(cut, "Test Connection").Click());

        Assert.Contains("Failed: unreachable", cut.Markup);
    }

    [Fact]
    public async Task SettingsPapraSaveKeepsSavedTokenAndAiSettings()
    {
        var cut = Render<SettingsPapra>();
        Field(cut, "Base URL").Input("https://new.example.com/");

        await cut.InvokeAsync(() => Button(cut, "Save").Click());

        _settingsSvc.Received(1).Save(Arg.Is<PipelineSettings>(s =>
            s.PapraBaseUrl == "https://new.example.com" &&
            s.PapraApiToken == "saved-token" &&
            s.OpenAiApiKey == "sk-saved" &&
            s.OpenAiModel == "my-model"));
    }

    [Fact]
    public async Task SettingsPapraSaveWithNewTokenReplacesTheToken()
    {
        var cut = Render<SettingsPapra>();
        Field(cut, "API Token").Input("new-token");

        await cut.InvokeAsync(() => Button(cut, "Save").Click());

        _settingsSvc.Received(1).Save(Arg.Is<PipelineSettings>(s => s.PapraApiToken == "new-token"));
    }
}
