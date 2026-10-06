using Bunit;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Papra.Companion.Components.Pages;
using Papra.Companion.Models;
using Papra.Companion.Services.Interfaces;

namespace Papra.Companion.Tests.Components;

public class SettingsHubTests : ComponentTestBase
{
    private const string TitleGeneration = "title-generation";
    private const string EmailAttachments = "email-attachments";

    private readonly ISettingsService _settingsSvc = Substitute.For<ISettingsService>();
    private readonly IEmailAttachmentSettingsService _emailSettingsSvc = Substitute.For<IEmailAttachmentSettingsService>();

    public SettingsHubTests()
    {
        _settingsSvc.Current.Returns(new PipelineSettings());
        _emailSettingsSvc.Current.Returns(new EmailAttachmentSettings());

        Services.AddSingleton(_settingsSvc);
        Services.AddSingleton(_emailSettingsSvc);
        Services.AddSingleton(Substitute.For<IPapraService>());
        Services.AddSingleton(Substitute.For<IOpenAiService>());
        Services.AddSingleton(Substitute.For<IEmailAttachmentService>());
    }

    [Fact]
    public void SettingsHubOpensPapraByDefault()
    {
        var cut = Render<SettingsHub>();

        Assert.Contains("reads documents from", cut.Markup);
        Assert.Equal("page", NavLink(cut, "Papra").GetAttribute("aria-current"));
    }

    [Theory]
    [InlineData("ai", "Title prompt")]
    [InlineData("email", "IMAP host")]
    [InlineData("EMAIL", "IMAP host")]
    [InlineData("unknown", "reads documents from")]
    public void SettingsHubOpensTheSectionInTheRoute(string section, string expected)
    {
        var cut = Render<SettingsHub>(parameters => parameters.Add(p => p.Section, section));

        Assert.Contains(expected, cut.Markup);
    }

    [Fact]
    public void SettingsHubMarksTheOpenSectionInTheNavigation()
    {
        var cut = Render<SettingsHub>(parameters => parameters.Add(p => p.Section, "ai"));

        var ai = NavLink(cut, "AI Services");
        Assert.Equal("page", ai.GetAttribute("aria-current"));
        Assert.Contains("pc-settings-link-active", ai.ClassName);
        Assert.Null(NavLink(cut, "Papra").GetAttribute("aria-current"));
    }

    [Fact]
    public void SettingsHubLinksEachSectionToItsUrl()
    {
        var cut = Render<SettingsHub>();

        Assert.Equal("settings/papra", NavLink(cut, "Papra").GetAttribute("href"));
        Assert.Equal("settings/ai", NavLink(cut, "AI Services").GetAttribute("href"));
        Assert.Equal("settings/email", NavLink(cut, "Email").GetAttribute("href"));
    }

    [Fact]
    public void SettingsHubShowsTitleGenerationOnWhenPapraAndAiAreSetUp()
    {
        _settingsSvc.Current.Returns(Pipeline(papra: true, ai: true));

        var cut = Render<SettingsHub>();

        Assert.Equal("On", StateOf(cut, TitleGeneration));
        Assert.Null(FixLink(cut, TitleGeneration));
    }

    [Theory]
    [InlineData(false, false, "Needs a Papra connection and an AI API key.", "settings/papra")]
    [InlineData(false, true, "Needs a Papra connection.", "settings/papra")]
    [InlineData(true, false, "Needs an AI API key.", "settings/ai")]
    public void SettingsHubSaysWhatTitleGenerationStillNeeds(bool papra, bool ai, string detail, string fix)
    {
        _settingsSvc.Current.Returns(Pipeline(papra, ai));

        var cut = Render<SettingsHub>();

        Assert.Equal("Needs setup", StateOf(cut, TitleGeneration));
        Assert.Contains(detail, Status(cut, TitleGeneration).TextContent);
        Assert.Equal(fix, FixLink(cut, TitleGeneration));
    }

    [Theory]
    [InlineData(true, true, "On", "Downloading new attachments from imap.example.com.")]
    [InlineData(true, false, "Off", "Set up, but not downloading attachments.")]
    [InlineData(false, true, "Needs setup", "Needs an IMAP connection.")]
    public void SettingsHubShowsWhetherEmailAttachmentsAreDownloading(bool connected, bool enabled, string state, string detail)
    {
        _emailSettingsSvc.Current.Returns(new EmailAttachmentSettings
        {
            Host = "imap.example.com",
            Username = "u",
            Password = connected ? "p" : "",
            Enabled = enabled,
        });

        var cut = Render<SettingsHub>();

        Assert.Equal(state, StateOf(cut, EmailAttachments));
        Assert.Contains(detail, Status(cut, EmailAttachments).TextContent);
        Assert.Equal(state == "On" ? null : "settings/email", FixLink(cut, EmailAttachments));
    }

    [Fact]
    public void SettingsHubUpdatesTheStatusWhenSettingsAreSaved()
    {
        var cut = Render<SettingsHub>();
        _settingsSvc.Current.Returns(Pipeline(papra: true, ai: true));

        _settingsSvc.OnChanged += Raise.Event<Action>();

        cut.WaitForAssertion(() => Assert.Equal("On", StateOf(cut, TitleGeneration)));
    }

    private static PipelineSettings Pipeline(bool papra, bool ai) => new()
    {
        PapraBaseUrl = papra ? "https://papra.example.com" : "",
        PapraApiToken = papra ? "token" : "",
        OpenAiApiKey = ai ? "sk-key" : "",
    };

    private static AngleSharp.Dom.IElement NavLink(IRenderedComponent<SettingsHub> cut, string title) =>
        cut.FindAll(".pc-settings-link").Single(link => link.QuerySelector(".pc-settings-link-label")?.TextContent == title);

    private static AngleSharp.Dom.IElement Status(IRenderedComponent<SettingsHub> cut, string feature) =>
        cut.Find($".pc-status-item[data-feature={feature}]");

    private static string StateOf(IRenderedComponent<SettingsHub> cut, string feature) =>
        Status(cut, feature).QuerySelector(".mud-chip")!.TextContent.Trim();

    private static string? FixLink(IRenderedComponent<SettingsHub> cut, string feature) =>
        Status(cut, feature).QuerySelector("a")?.GetAttribute("href");
}
