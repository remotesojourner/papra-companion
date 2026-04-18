using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Papra.Companion.Components.Pages;
using Papra.Companion.Data.Entities;
using Papra.Companion.Data.Repositories.Interfaces;
using Papra.Companion.Models;
using Papra.Companion.Services.Interfaces;

namespace Papra.Companion.Tests.Components;

public class EmailAttachmentsPageTests : ComponentTestBase
{
    private readonly IEmailAttachmentSettingsService _settingsSvc = default!;
    private readonly IEmailAttachmentLogRepository _logRepo = default!;

    public EmailAttachmentsPageTests()
    {
        _settingsSvc = Substitute.For<IEmailAttachmentSettingsService>();
        _logRepo = Substitute.For<IEmailAttachmentLogRepository>();
        _logRepo.GetRecent(Arg.Any<int>()).Returns([]);

        Services.AddSingleton(_settingsSvc);
        Services.AddSingleton(_logRepo);
    }

    [Fact]
    public void EmailAttachments_WhenNotConfigured_ShowsWarning()
    {
        _settingsSvc.Current.Returns(new EmailAttachmentSettings()); // not configured

        var cut = Render<EmailAttachments>();

        Assert.Contains("Not configured", cut.Markup);
    }

    [Fact]
    public void EmailAttachments_WhenConfiguredButDisabled_ShowsDisabledInfo()
    {
        _settingsSvc.Current.Returns(new EmailAttachmentSettings
        {
            Host = "imap.example.com", Username = "u", Password = "p", Enabled = false
        });

        var cut = Render<EmailAttachments>();

        Assert.Contains("Disabled", cut.Markup);
    }

    [Fact]
    public void EmailAttachments_WhenConfiguredAndEnabled_ShowsNoWarnings()
    {
        _settingsSvc.Current.Returns(new EmailAttachmentSettings
        {
            Host = "imap.example.com", Username = "u", Password = "p", Enabled = true
        });

        var cut = Render<EmailAttachments>();

        Assert.DoesNotContain("Configure your IMAP credentials", cut.Markup);
        Assert.DoesNotContain("configured but currently disabled", cut.Markup);
    }

    [Fact]
    public void EmailAttachments_WithNoLogs_ShowsEmptyState()
    {
        _settingsSvc.Current.Returns(new EmailAttachmentSettings
        {
            Host = "h", Username = "u", Password = "p", Enabled = true
        });
        _logRepo.GetRecent(Arg.Any<int>()).Returns([]);

        var cut = Render<EmailAttachments>();

        Assert.Contains("No attachments downloaded yet", cut.Markup);
    }

    [Fact]
    public void EmailAttachments_WithLogs_ShowsHistoryTable()
    {
        _settingsSvc.Current.Returns(new EmailAttachmentSettings
        {
            Host = "h", Username = "u", Password = "p", Enabled = true
        });
        _logRepo.GetRecent(Arg.Any<int>()).Returns(
        [
            new EmailAttachmentLogEntity
            {
                MessageId = "msg1",
                AttachmentName = "invoice.pdf",
                Subject = "Your Invoice",
                FromEmail = "billing@example.com",
                SavedPath = "/app/attachments/invoice.pdf",
                Succeeded = true,
                DownloadedAt = DateTimeOffset.UtcNow,
                MessageDate = DateTimeOffset.UtcNow,
            }
        ]);

        var cut = Render<EmailAttachments>();

        Assert.Contains("invoice.pdf", cut.Markup);
        Assert.Contains("Your Invoice", cut.Markup);
        Assert.Contains("billing@example.com", cut.Markup);
        Assert.Contains("Downloaded", cut.Markup);
    }

    [Fact]
    public void EmailAttachments_WithFailedLog_ShowsFailedBadge()
    {
        _settingsSvc.Current.Returns(new EmailAttachmentSettings
        {
            Host = "h", Username = "u", Password = "p", Enabled = true
        });
        _logRepo.GetRecent(Arg.Any<int>()).Returns(
        [
            new EmailAttachmentLogEntity
            {
                MessageId = "msg2",
                AttachmentName = "broken.pdf",
                Subject = "Sub",
                FromEmail = "a@b.com",
                Succeeded = false,
                ErrorMessage = "disk full",
                DownloadedAt = DateTimeOffset.UtcNow,
                MessageDate = DateTimeOffset.UtcNow,
            }
        ]);

        var cut = Render<EmailAttachments>();

        Assert.Contains("broken.pdf", cut.Markup);
        Assert.Contains("Failed", cut.Markup);
    }

    [Fact]
    public void EmailAttachments_StatsReflectLogCounts()
    {
        _settingsSvc.Current.Returns(new EmailAttachmentSettings
        {
            Host = "h", Username = "u", Password = "p", Enabled = true
        });
        _logRepo.GetRecent(Arg.Any<int>()).Returns(
        [
            new EmailAttachmentLogEntity { MessageId = "m1", AttachmentName = "a.pdf", Succeeded = true,  DownloadedAt = DateTimeOffset.UtcNow, MessageDate = DateTimeOffset.UtcNow, Subject = "", FromEmail = "" },
            new EmailAttachmentLogEntity { MessageId = "m2", AttachmentName = "b.pdf", Succeeded = true,  DownloadedAt = DateTimeOffset.UtcNow, MessageDate = DateTimeOffset.UtcNow, Subject = "", FromEmail = "" },
            new EmailAttachmentLogEntity { MessageId = "m3", AttachmentName = "c.pdf", Succeeded = false, DownloadedAt = DateTimeOffset.UtcNow, MessageDate = DateTimeOffset.UtcNow, Subject = "", FromEmail = "" },
        ]);

        var cut = Render<EmailAttachments>();
        var markup = cut.Markup;

        // Total=3, Succeeded=2, Failed=1
        Assert.Contains(">3<", markup);
        Assert.Contains(">2<", markup);
        Assert.Contains(">1<", markup);
    }
}
