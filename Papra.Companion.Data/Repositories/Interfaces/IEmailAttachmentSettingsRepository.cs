using Papra.Companion.Data.Entities;

namespace Papra.Companion.Data.Repositories.Interfaces;

public interface IEmailAttachmentSettingsRepository
{
    EmailAttachmentSettingsEntity? Find();
    Task UpsertAsync(EmailAttachmentSettingsEntity entity);
}
