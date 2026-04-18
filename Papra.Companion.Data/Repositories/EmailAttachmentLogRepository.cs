using Microsoft.EntityFrameworkCore;
using Papra.Companion.Data.Entities;
using Papra.Companion.Data.Repositories.Interfaces;

namespace Papra.Companion.Data.Repositories;

public class EmailAttachmentLogRepository(IDbContextFactory<AppDbContext> dbFactory)
    : IEmailAttachmentLogRepository
{
    public IReadOnlyList<EmailAttachmentLogEntity> GetRecent(int count)
    {
        using var db = dbFactory.CreateDbContext();
        return [.. db.EmailAttachmentLog
            .AsNoTracking()
            .AsEnumerable()
            .OrderByDescending(e => e.DownloadedAt)
            .Take(count)];
    }

    public bool HasBeenDownloaded(string messageId, string attachmentName)
    {
        using var db = dbFactory.CreateDbContext();
        return db.EmailAttachmentLog
            .AsNoTracking()
            .Any(e => e.MessageId == messageId && e.AttachmentName == attachmentName);
    }

    public async Task AddAsync(EmailAttachmentLogEntity entity)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        db.EmailAttachmentLog.Add(entity);
        await db.SaveChangesAsync();
    }
}
