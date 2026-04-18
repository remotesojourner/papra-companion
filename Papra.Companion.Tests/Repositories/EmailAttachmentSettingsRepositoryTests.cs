using Microsoft.EntityFrameworkCore;
using Papra.Companion.Data;
using Papra.Companion.Data.Entities;
using Papra.Companion.Data.Repositories;

namespace Papra.Companion.Tests.Repositories;

public class EmailAttachmentSettingsRepositoryTests
{
    private static TestDbContextFactory CreateFactory(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new TestDbContextFactory(options);
    }

    [Fact]
    public void Get_WhenEmpty_ReturnsNull()
    {
        var repo = new EmailAttachmentSettingsRepository(
            CreateFactory(nameof(Get_WhenEmpty_ReturnsNull)));

        Assert.Null(repo.Get());
    }

    [Fact]
    public async Task UpsertAsync_WhenNoExisting_InsertsNewRow()
    {
        var factory = CreateFactory(nameof(UpsertAsync_WhenNoExisting_InsertsNewRow));
        var repo = new EmailAttachmentSettingsRepository(factory);
        var entity = new EmailAttachmentSettingsEntity
        {
            Enabled = true,
            Host = "imap.example.com",
            Port = 993,
            Username = "user@example.com",
            Password = "pass",
            ImapFolder = "INBOX",
            UseSsl = true,
            PollIntervalSeconds = 300,
        };

        await repo.UpsertAsync(entity);
        var result = repo.Get();

        Assert.NotNull(result);
        Assert.Equal("imap.example.com", result.Host);
        Assert.Equal("user@example.com", result.Username);
        Assert.True(result.Enabled);
    }

    [Fact]
    public async Task UpsertAsync_WhenExisting_UpdatesAllFields()
    {
        var factory = CreateFactory(nameof(UpsertAsync_WhenExisting_UpdatesAllFields));
        var repo = new EmailAttachmentSettingsRepository(factory);

        await repo.UpsertAsync(new EmailAttachmentSettingsEntity
        {
            Host = "old.imap.com",
            Username = "old@example.com",
            Password = "oldpass",
        });

        await repo.UpsertAsync(new EmailAttachmentSettingsEntity
        {
            Enabled = true,
            Host = "new.imap.com",
            Port = 143,
            Username = "new@example.com",
            Password = "newpass",
            ImapFolder = "Documents",
            SubjectRegex = "Invoice",
            SubjectRegexIgnoreCase = false,
            SubjectRegexMatchAnywhere = true,
            FilenameTemplate = "{{date}}-{{attachment_name}}",
            UseSsl = false,
            UseStartTls = true,
            DeleteAfterDownload = true,
            DeleteCopyFolder = "Processed",
            PollIntervalSeconds = 60,
        });

        var result = repo.Get()!;
        Assert.Equal("new.imap.com", result.Host);
        Assert.Equal(143, result.Port);
        Assert.Equal("new@example.com", result.Username);
        Assert.Equal("newpass", result.Password);
        Assert.Equal("Documents", result.ImapFolder);
        Assert.Equal("Invoice", result.SubjectRegex);
        Assert.False(result.SubjectRegexIgnoreCase);
        Assert.True(result.SubjectRegexMatchAnywhere);
        Assert.False(result.UseSsl);
        Assert.True(result.UseStartTls);
        Assert.True(result.DeleteAfterDownload);
        Assert.Equal("Processed", result.DeleteCopyFolder);
        Assert.Equal(60, result.PollIntervalSeconds);
    }

    [Fact]
    public async Task UpsertAsync_CalledMultipleTimes_OnlyOneRowExists()
    {
        var factory = CreateFactory(nameof(UpsertAsync_CalledMultipleTimes_OnlyOneRowExists));
        var repo = new EmailAttachmentSettingsRepository(factory);

        await repo.UpsertAsync(new EmailAttachmentSettingsEntity { Host = "first" });
        await repo.UpsertAsync(new EmailAttachmentSettingsEntity { Host = "second" });
        await repo.UpsertAsync(new EmailAttachmentSettingsEntity { Host = "third" });

        await using var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        Assert.Equal(1, await db.EmailAttachmentSettings.CountAsync(TestContext.Current.CancellationToken));
    }
}
