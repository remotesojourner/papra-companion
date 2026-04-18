using NSubstitute;
using Papra.Companion.Data.Entities;
using Papra.Companion.Data.Repositories.Interfaces;
using Papra.Companion.Models;
using Papra.Companion.Services;

namespace Papra.Companion.Tests.Services;

public class EmailAttachmentSettingsServiceTests
{
    private static IEmailAttachmentSettingsRepository EmptyRepository()
    {
        var repo = Substitute.For<IEmailAttachmentSettingsRepository>();
        repo.Get().Returns((EmailAttachmentSettingsEntity?)null);
        return repo;
    }

    [Fact]
    public void Current_WhenRepositoryEmpty_ReturnsDefaults()
    {
        var service = new EmailAttachmentSettingsService(EmptyRepository());

        var current = service.Current;

        Assert.Equal(993, current.Port);
        Assert.Equal("INBOX", current.ImapFolder);
        Assert.True(current.SubjectRegexIgnoreCase);
        Assert.True(current.UseSsl);
        Assert.Equal(300, current.PollIntervalSeconds);
    }

    [Fact]
    public void Current_WhenRepositoryHasEntity_MapsAllFields()
    {
        var entity = new EmailAttachmentSettingsEntity
        {
            Enabled = true,
            Host = "imap.example.com",
            Port = 143,
            Username = "user@example.com",
            Password = "secret",
            ImapFolder = "Documents",
            SubjectRegex = "Invoice.*",
            SubjectRegexIgnoreCase = false,
            SubjectRegexMatchAnywhere = true,
            FilenameTemplate = "{{date}}-{{attachment_name}}",
            UseSsl = false,
            UseStartTls = true,
            DeleteAfterDownload = true,
            DeleteCopyFolder = "Processed",
            PollIntervalSeconds = 60,
        };
        var repo = Substitute.For<IEmailAttachmentSettingsRepository>();
        repo.Get().Returns(entity);

        var service = new EmailAttachmentSettingsService(repo);
        var current = service.Current;

        Assert.True(current.Enabled);
        Assert.Equal("imap.example.com", current.Host);
        Assert.Equal(143, current.Port);
        Assert.Equal("user@example.com", current.Username);
        Assert.Equal("secret", current.Password);
        Assert.Equal("Documents", current.ImapFolder);
        Assert.Equal("Invoice.*", current.SubjectRegex);
        Assert.False(current.SubjectRegexIgnoreCase);
        Assert.True(current.SubjectRegexMatchAnywhere);
        Assert.Equal("{{date}}-{{attachment_name}}", current.FilenameTemplate);
        Assert.False(current.UseSsl);
        Assert.True(current.UseStartTls);
        Assert.True(current.DeleteAfterDownload);
        Assert.Equal("Processed", current.DeleteCopyFolder);
        Assert.Equal(60, current.PollIntervalSeconds);
    }

    [Fact]
    public void Save_UpdatesCurrentImmediately()
    {
        var service = new EmailAttachmentSettingsService(EmptyRepository());

        service.Save(new EmailAttachmentSettings
        {
            Host = "imap.new.com",
            Port = 993,
            Username = "new@example.com",
        });

        Assert.Equal("imap.new.com", service.Current.Host);
        Assert.Equal("new@example.com", service.Current.Username);
    }

    [Fact]
    public void Save_RaisesOnChangedEvent()
    {
        var service = new EmailAttachmentSettingsService(EmptyRepository());
        var raised = false;
        service.OnChanged += () => raised = true;

        service.Save(new EmailAttachmentSettings());

        Assert.True(raised);
    }

    [Fact]
    public void IsConfigured_WhenAllRequiredFieldsPresent_ReturnsTrue()
    {
        var settings = new EmailAttachmentSettings
        {
            Host = "imap.example.com",
            Username = "user",
            Password = "pass"
        };
        Assert.True(settings.IsConfigured);
    }

    [Theory]
    [InlineData("", "user", "pass")]
    [InlineData("host", "", "pass")]
    [InlineData("host", "user", "")]
    public void IsConfigured_WhenAnyRequiredFieldMissing_ReturnsFalse(
        string host, string username, string password)
    {
        var settings = new EmailAttachmentSettings
        {
            Host = host,
            Username = username,
            Password = password
        };
        Assert.False(settings.IsConfigured);
    }
}
