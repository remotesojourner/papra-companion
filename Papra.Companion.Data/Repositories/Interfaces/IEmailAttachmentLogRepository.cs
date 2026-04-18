using Papra.Companion.Data.Entities;

namespace Papra.Companion.Data.Repositories.Interfaces;

public interface IEmailAttachmentLogRepository
{
    IReadOnlyList<EmailAttachmentLogEntity> GetRecent(int count);
    bool HasBeenDownloaded(string messageId, string attachmentName);
    Task AddAsync(EmailAttachmentLogEntity entity);
}
