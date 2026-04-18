using Papra.Companion.Data.Entities;

namespace Papra.Companion.Data.Repositories.Interfaces;

public interface IEmailAttachmentSettingsRepository
{
    EmailAttachmentSettingsEntity? Get();
    Task UpsertAsync(EmailAttachmentSettingsEntity entity);
}
