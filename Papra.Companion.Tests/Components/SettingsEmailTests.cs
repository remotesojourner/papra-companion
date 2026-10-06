using Bunit;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Papra.Companion.Components.Pages.Settings;
using Papra.Companion.Models;
using Papra.Companion.Services.Interfaces;

namespace Papra.Companion.Tests.Components;

public class SettingsEmailTests : ComponentTestBase
{
    private readonly IEmailAttachmentSettingsService _emailSettingsSvc = Substitute.For<IEmailAttachmentSettingsService>();
    private readonly IEmailAttachmentService _emailAttachmentSvc = Substitute.For<IEmailAttachmentService>();

    public SettingsEmailTests()
    {
        _emailSettingsSvc.Current.Returns(Configured());

        Services.AddSingleton(_emailSettingsSvc);
        Services.AddSingleton(_emailAttachmentSvc);
    }

    [Fact]
    public void SettingsEmailShowsEveryRow()
    {
        var cut = Render<SettingsEmail>();

        foreach (var label in new[] { "Server", "Filter", "Saving", "Afterwards" })
            Assert.NotNull(Row(cut, label));
    }

    [Fact]
    public void SettingsEmailWithSavedPasswordShowsPlaceholderHintWithoutThePassword()
    {
        var cut = Render<SettingsEmail>();

        Assert.Contains("Saved — type to replace", cut.Markup);
        Assert.DoesNotContain("saved-password", cut.Markup);
    }

    [Fact]
    public async Task SettingsEmailTestConnectionUsesSavedPasswordWithoutShowingIt()
    {
        string? passwordSent = null;
        _emailAttachmentSvc.TestConnectionAsync(Arg.Any<EmailAttachmentSettings>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                passwordSent = call.Arg<EmailAttachmentSettings>().Password;
                call.Arg<EmailAttachmentSettings>().Password = "filled-in-by-service";
                return "Connected";
            });
        var cut = Render<SettingsEmail>();

        await cut.InvokeAsync(() => Button(cut, "Test Connection").Click());
        cut.Render();

        Assert.Equal("saved-password", passwordSent);
        Assert.Contains("Connected", cut.Markup);
        Assert.DoesNotContain("filled-in-by-service", cut.Markup);
        Assert.DoesNotContain("saved-password", cut.Markup);
    }

    [Fact]
    public void SettingsEmailSwitchSavesOnlyTheSwitchStraightAway()
    {
        var cut = Render<SettingsEmail>();
        Field(cut, "IMAP host").Input("unsaved.example.com");

        cut.Find(".pc-header-switch input").Change(true);

        _emailSettingsSvc.Received(1).Save(Arg.Is<EmailAttachmentSettings>(s =>
            s.Enabled && s.Host == "imap.example.com" && s.Password == "saved-password"));
        Assert.Equal("unsaved.example.com", Field(cut, "IMAP host").GetAttribute("value"));
    }

    [Fact]
    public void SettingsEmailSwitchWaitsForAConnection()
    {
        _emailSettingsSvc.Current.Returns(new EmailAttachmentSettings());

        var cut = Render<SettingsEmail>();

        Assert.True(cut.Find(".pc-header-switch input").HasAttribute("disabled"));
        Assert.Contains("Save a server connection to turn on automatic downloads.", cut.Markup);
    }

    [Fact]
    public void SettingsEmailEnablesSaveOnlyAfterAChange()
    {
        var cut = Render<SettingsEmail>();
        Assert.True(Button(cut, "Save").HasAttribute("disabled"));

        Field(cut, "Folder").Input("Invoices");

        Assert.False(Button(cut, "Save").HasAttribute("disabled"));
    }

    [Fact]
    public async Task SettingsEmailSaveKeepsSavedPasswordAndTheSwitch()
    {
        var settings = Configured();
        settings.Enabled = true;
        _emailSettingsSvc.Current.Returns(settings);
        var cut = Render<SettingsEmail>();
        Field(cut, "IMAP host").Input("mail.example.com");
        Field(cut, "Folder").Input("Invoices");

        await cut.InvokeAsync(() => Button(cut, "Save").Click());

        _emailSettingsSvc.Received(1).Save(Arg.Is<EmailAttachmentSettings>(s =>
            s.Host == "mail.example.com" && s.ImapFolder == "Invoices" && s.Password == "saved-password" && s.Enabled));
    }

    [Fact]
    public async Task SettingsEmailSaveWhenCopyFolderCheckFailsStillSaves()
    {
        var settings = Configured();
        settings.DeleteAfterDownload = true;
        settings.DeleteCopyFolder = "Archived";
        _emailSettingsSvc.Current.Returns(settings);
        _emailAttachmentSvc.EnsureCopyFolderExistsAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("server unreachable"));
        var cut = Render<SettingsEmail>();
        Field(cut, "Copy to folder first").Input("Old mail");

        await cut.InvokeAsync(() => Button(cut, "Save").Click());

        _emailSettingsSvc.Received(1).Save(Arg.Is<EmailAttachmentSettings>(s => s.DeleteCopyFolder == "Old mail"));
        await _emailAttachmentSvc.Received(1).EnsureCopyFolderExistsAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public void SettingsEmailCopyFolderFieldShowsOnlyWhenDeletingAfterDownload()
    {
        var cut = Render<SettingsEmail>();

        Assert.DoesNotContain("Copy to folder first", cut.Markup);
    }

    private static EmailAttachmentSettings Configured() => new()
    {
        Host = "imap.example.com",
        Username = "user@example.com",
        Password = "saved-password",
    };
}
