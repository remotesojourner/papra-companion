using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Papra.Companion.Components.Pages;
using Papra.Companion.Models;
using Papra.Companion.Services.Interfaces;

namespace Papra.Companion.Tests.Components;

public class SettingsPageTests : ComponentTestBase
{
    private ISettingsService _settingsSvc = default!;
    private IEmailAttachmentSettingsService _emailSettingsSvc = default!;
    private IPapraService _papraSvc = default!;
    private IOpenAiService _openAiSvc = default!;
    private IMistralService _mistralSvc = default!;
    private IEmailAttachmentService _emailAttachmentSvc = default!;

    public SettingsPageTests()
    {
        _settingsSvc = Substitute.For<ISettingsService>();
        _emailSettingsSvc = Substitute.For<IEmailAttachmentSettingsService>();
        _papraSvc = Substitute.For<IPapraService>();
        _openAiSvc = Substitute.For<IOpenAiService>();
        _mistralSvc = Substitute.For<IMistralService>();
        _emailAttachmentSvc = Substitute.For<IEmailAttachmentService>();

        _settingsSvc.Current.Returns(new PipelineSettings());
        _emailSettingsSvc.Current.Returns(new EmailAttachmentSettings());

        Services.AddSingleton(_settingsSvc);
        Services.AddSingleton(_emailSettingsSvc);
        Services.AddSingleton(_papraSvc);
        Services.AddSingleton(_openAiSvc);
        Services.AddSingleton(_mistralSvc);
        Services.AddSingleton(_emailAttachmentSvc);

        // NavigationManager is required by the page to build _webhookUrl
        Services.AddSingleton<NavigationManager>(new FakeNavigationManager());
    }

    [Fact]
    public void Settings_WhenPipelineNotConfigured_ShowsWarningAlert()
    {
        _settingsSvc.Current.Returns(new PipelineSettings()); // not configured

        var cut = Render<Settings>();

        Assert.Contains("Not configured", cut.Markup);
    }

    [Fact]
    public void Settings_WhenPipelineConfigured_ShowsActiveAlert()
    {
        _settingsSvc.Current.Returns(new PipelineSettings
        {
            PapraBaseUrl = "https://papra.example.com",
            PapraApiToken = "token",
            OpenAiApiKey = "sk-key",
        });

        var cut = Render<Settings>();

        Assert.Contains("Active", cut.Markup);
    }

    [Fact]
    public void Settings_WhenEmailNotConfigured_ShowsEmailWarning()
    {
        _emailSettingsSvc.Current.Returns(new EmailAttachmentSettings()); // not configured

        var cut = Render<Settings>();

        Assert.Contains("Not configured", cut.Markup);
    }

    [Fact]
    public void Settings_WhenEmailConfiguredButDisabled_ShowsInfoAlert()
    {
        _emailSettingsSvc.Current.Returns(new EmailAttachmentSettings
        {
            Host = "imap.example.com", Username = "u", Password = "p", Enabled = false
        });

        var cut = Render<Settings>();

        Assert.Contains("Configured but disabled", cut.Markup);
    }

    [Fact]
    public void Settings_WhenEmailEnabledAndConfigured_ShowsActiveAlert()
    {
        _emailSettingsSvc.Current.Returns(new EmailAttachmentSettings
        {
            Host = "imap.example.com", Username = "u", Password = "p", Enabled = true
        });

        var cut = Render<Settings>();

        Assert.Contains("Active", cut.Markup);
    }

    [Fact]
    public void Settings_DefaultTabIsPapra()
    {
        var cut = Render<Settings>();

        // The Papra Connection section header is visible on the default tab
        Assert.Contains("Papra Connection", cut.Markup);
    }

    [Fact]
    public void Settings_ClickAiServicesTab_ShowsOpenAiSection()
    {
        var cut = Render<Settings>();

        var tabs = cut.FindAll("button").Where(b => b.TextContent.Contains("AI Services")).ToList();
        Assert.Single(tabs);
        tabs[0].Click();

        Assert.Contains("OpenAI", cut.Markup);
    }

    [Fact]
    public void Settings_ClickAiPromptsTab_ShowsPromptFields()
    {
        var cut = Render<Settings>();

        var tab = cut.FindAll("button").First(b => b.TextContent.Contains("AI Prompts"));
        tab.Click();

        Assert.Contains("Title Extraction Prompt", cut.Markup);
        Assert.Contains("Tag Selection Prompt", cut.Markup);
    }

    [Fact]
    public void Settings_ClickEmailTab_ShowsImapSection()
    {
        var cut = Render<Settings>();

        var tab = cut.FindAll("button").First(b => b.TextContent.Contains("Email"));
        tab.Click();

        Assert.Contains("IMAP", cut.Markup);
    }

    [Fact]
    public void Settings_WithSavedPapraToken_ShowsPlaceholderHint()
    {
        _settingsSvc.Current.Returns(new PipelineSettings
        {
            PapraApiToken = "existing-token",
        });

        var cut = Render<Settings>();

        Assert.Contains("Already saved", cut.Markup);
    }

    [Fact]
    public void Settings_WithSavedOpenAiKey_ShowsPlaceholderHint()
    {
        _settingsSvc.Current.Returns(new PipelineSettings
        {
            OpenAiApiKey = "sk-existing",
        });
        var tab = Render<Settings>()
            .FindAll("button").First(b => b.TextContent.Contains("AI Services"));
        tab.Click();

        // On AI Services tab, the placeholder should indicate already saved
        var cut = Render<Settings>();
        var aiTab = cut.FindAll("button").First(b => b.TextContent.Contains("AI Services"));
        aiTab.Click();

        Assert.Contains("Already saved", cut.Markup);
    }

    [Fact]
    public async Task Settings_TestPapraConnection_CallsPapraService()
    {
        _settingsSvc.Current.Returns(new PipelineSettings
        {
            PapraBaseUrl = "https://papra.example.com",
            PapraApiToken = "tok",
            OpenAiApiKey = "sk",
        });
        _papraSvc.TestConnectionAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                 .Returns("Connected");

        var cut = Render<Settings>();
        var testBtn = cut.FindAll("button").First(b => b.TextContent.Contains("Test Connection"));
        await cut.InvokeAsync(() => testBtn.Click());

        await _papraSvc.Received(1).TestConnectionAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        Assert.Contains("Connected", cut.Markup);
    }

    [Fact]
    public async Task Settings_TestPapraConnection_WhenFails_ShowsFailedBadge()
    {
        _settingsSvc.Current.Returns(new PipelineSettings { PapraBaseUrl = "https://x.com" });
        _papraSvc.TestConnectionAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                 .ThrowsAsync(new HttpRequestException("unreachable"));

        var cut = Render<Settings>();
        var testBtn = cut.FindAll("button").First(b => b.TextContent.Contains("Test Connection"));
        await cut.InvokeAsync(() => testBtn.Click());

        Assert.Contains("Failed", cut.Markup);
    }

    [Fact]
    public async Task Settings_SaveSettings_CallsSettingsServiceSave()
    {
        _settingsSvc.Current.Returns(new PipelineSettings
        {
            PapraBaseUrl = "https://papra.example.com",
            PapraApiToken = "tok",
            OpenAiApiKey = "sk",
        });

        var cut = Render<Settings>();
        var saveBtn = cut.FindAll("button").First(b => b.TextContent.Trim() == "Save Settings");
        await cut.InvokeAsync(() => saveBtn.Click());

        _settingsSvc.Received(1).Save(Arg.Any<PipelineSettings>());
        _emailSettingsSvc.Received(1).Save(Arg.Any<EmailAttachmentSettings>());
    }

    [Fact]
    public void Settings_WebhookUrlDisplayed()
    {
        var cut = Render<Settings>();

        Assert.Contains("/webhook/document", cut.Markup);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private sealed class FakeNavigationManager : NavigationManager
    {
        public FakeNavigationManager() => Initialize("http://localhost:1003/", "http://localhost:1003/settings");
        protected override void NavigateToCore(string uri, bool forceLoad) { }
    }
}
