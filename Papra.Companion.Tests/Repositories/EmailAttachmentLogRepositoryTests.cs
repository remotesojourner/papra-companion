using Microsoft.EntityFrameworkCore;
using Papra.Companion.Data;
using Papra.Companion.Data.Entities;
using Papra.Companion.Data.Repositories;

namespace Papra.Companion.Tests.Repositories;

public class EmailAttachmentLogRepositoryTests
{
    private static IDbContextFactory<AppDbContext> CreateFactory(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new TestDbContextFactory(options);
    }

    private static EmailAttachmentLogEntity MakeLog(
        string messageId = "msg1",
        string attachmentName = "file.pdf",
        bool succeeded = true,
        DateTimeOffset? downloadedAt = null) => new()
    {
        MessageId = messageId,
        AttachmentName = attachmentName,
        SavedPath = $"/app/attachments/{attachmentName}",
        Subject = "Test subject",
        FromEmail = "from@example.com",
        MessageDate = DateTimeOffset.UtcNow.AddHours(-1),
        DownloadedAt = downloadedAt ?? DateTimeOffset.UtcNow,
        Succeeded = succeeded,
    };

    [Fact]
    public async Task AddAsync_ThenGetRecent_ReturnsEntry()
    {
        var factory = CreateFactory(nameof(AddAsync_ThenGetRecent_ReturnsEntry));
        var repo = new EmailAttachmentLogRepository(factory);

        await repo.AddAsync(MakeLog("msg1", "invoice.pdf"));
        var results = repo.GetRecent(10);

        Assert.Single(results);
        Assert.Equal("msg1", results[0].MessageId);
        Assert.Equal("invoice.pdf", results[0].AttachmentName);
    }

    [Fact]
    public async Task GetRecent_ReturnsInDescendingDownloadedAtOrder()
    {
        var factory = CreateFactory(nameof(GetRecent_ReturnsInDescendingDownloadedAtOrder));
        var repo = new EmailAttachmentLogRepository(factory);
        var now = DateTimeOffset.UtcNow;

        await repo.AddAsync(MakeLog("msg1", "old.pdf", downloadedAt: now.AddMinutes(-10)));
        await repo.AddAsync(MakeLog("msg2", "new.pdf", downloadedAt: now));

        var results = repo.GetRecent(10);

        Assert.Equal("new.pdf", results[0].AttachmentName);
        Assert.Equal("old.pdf", results[1].AttachmentName);
    }

    [Fact]
    public async Task GetRecent_RespectsCountLimit()
    {
        var factory = CreateFactory(nameof(GetRecent_RespectsCountLimit));
        var repo = new EmailAttachmentLogRepository(factory);
        var now = DateTimeOffset.UtcNow;

        for (var i = 0; i < 8; i++)
            await repo.AddAsync(MakeLog($"msg{i}", $"file{i}.pdf", downloadedAt: now.AddSeconds(i)));

        Assert.Equal(3, repo.GetRecent(3).Count);
    }

    [Fact]
    public async Task HasBeenDownloaded_WhenEntryExists_ReturnsTrue()
    {
        var factory = CreateFactory(nameof(HasBeenDownloaded_WhenEntryExists_ReturnsTrue));
        var repo = new EmailAttachmentLogRepository(factory);
        await repo.AddAsync(MakeLog("msg1", "invoice.pdf"));

        Assert.True(repo.HasBeenDownloaded("msg1", "invoice.pdf"));
    }

    [Fact]
    public async Task HasBeenDownloaded_WhenEntryAbsent_ReturnsFalse()
    {
        var factory = CreateFactory(nameof(HasBeenDownloaded_WhenEntryAbsent_ReturnsFalse));
        var repo = new EmailAttachmentLogRepository(factory);
        await repo.AddAsync(MakeLog("msg1", "invoice.pdf"));

        Assert.False(repo.HasBeenDownloaded("msg1", "other.pdf"));
        Assert.False(repo.HasBeenDownloaded("msg99", "invoice.pdf"));
    }

    [Fact]
    public void HasBeenDownloaded_WhenEmpty_ReturnsFalse()
    {
        var factory = CreateFactory(nameof(HasBeenDownloaded_WhenEmpty_ReturnsFalse));
        var repo = new EmailAttachmentLogRepository(factory);

        Assert.False(repo.HasBeenDownloaded("any", "any.pdf"));
    }

    [Fact]
    public async Task GetRecent_WhenEmpty_ReturnsEmptyList()
    {
        var factory = CreateFactory(nameof(GetRecent_WhenEmpty_ReturnsEmptyList));
        var repo = new EmailAttachmentLogRepository(factory);

        Assert.Empty(repo.GetRecent(10));
    }
}
