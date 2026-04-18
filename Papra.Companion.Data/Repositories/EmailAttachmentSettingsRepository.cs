using Microsoft.EntityFrameworkCore;
using Papra.Companion.Data.Entities;
using Papra.Companion.Data.Repositories.Interfaces;

namespace Papra.Companion.Data.Repositories;

public class EmailAttachmentSettingsRepository(IDbContextFactory<AppDbContext> dbFactory)
    : IEmailAttachmentSettingsRepository
{
    public EmailAttachmentSettingsEntity? Get()
    {
        using var db = dbFactory.CreateDbContext();
        return db.EmailAttachmentSettings.Find(1);
    }

    public async Task UpsertAsync(EmailAttachmentSettingsEntity entity)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var existing = await db.EmailAttachmentSettings.FindAsync(1);
        if (existing is null)
        {
            entity.Id = 1;
            db.EmailAttachmentSettings.Add(entity);
        }
        else
        {
            existing.Enabled = entity.Enabled;
            existing.Host = entity.Host;
            existing.Port = entity.Port;
            existing.Username = entity.Username;
            existing.Password = entity.Password;
            existing.ImapFolder = entity.ImapFolder;
            existing.SubjectRegex = entity.SubjectRegex;
            existing.SubjectRegexIgnoreCase = entity.SubjectRegexIgnoreCase;
            existing.SubjectRegexMatchAnywhere = entity.SubjectRegexMatchAnywhere;
            existing.FilenameTemplate = entity.FilenameTemplate;
            existing.UseSsl = entity.UseSsl;
            existing.UseStartTls = entity.UseStartTls;
            existing.DeleteAfterDownload = entity.DeleteAfterDownload;
            existing.DeleteCopyFolder = entity.DeleteCopyFolder;
            existing.PollIntervalSeconds = entity.PollIntervalSeconds;
        }
        await db.SaveChangesAsync();
    }
}
