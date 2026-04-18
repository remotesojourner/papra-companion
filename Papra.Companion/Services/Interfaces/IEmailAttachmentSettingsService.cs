using Papra.Companion.Models;

namespace Papra.Companion.Services.Interfaces;

public interface IEmailAttachmentSettingsService
{
    EmailAttachmentSettings Current { get; }
    event Action? OnChanged;
    void Save(EmailAttachmentSettings settings);
}
