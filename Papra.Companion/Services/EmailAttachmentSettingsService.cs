using Papra.Companion.Data.Entities;
using Papra.Companion.Data.Repositories.Interfaces;
using Papra.Companion.Models;
using Papra.Companion.Services.Interfaces;

namespace Papra.Companion.Services;

public class EmailAttachmentSettingsService : IEmailAttachmentSettingsService
{
    private readonly IEmailAttachmentSettingsRepository _repository;
    private EmailAttachmentSettings _current;
    private readonly Lock _lock = new();

    public event Action? OnChanged;

    public EmailAttachmentSettings Current
    {
        get { lock (_lock) { return _current; } }
    }

    public EmailAttachmentSettingsService(IEmailAttachmentSettingsRepository repository)
    {
        _repository = repository;
        _current = ToModel(_repository.Get()) ?? new EmailAttachmentSettings();
    }

    public void Save(EmailAttachmentSettings settings)
    {
        lock (_lock) { _current = ToModel(ToEntity(settings))!; }
        Task.Run(() => _repository.UpsertAsync(ToEntity(settings)));
        OnChanged?.Invoke();
    }

    private static EmailAttachmentSettings ToModel(EmailAttachmentSettingsEntity? e) =>
        e is null ? new EmailAttachmentSettings() : new()
        {
            Enabled = e.Enabled,
            Host = e.Host,
            Port = e.Port,
            Username = e.Username,
            Password = e.Password,
            ImapFolder = e.ImapFolder,
            SubjectRegex = e.SubjectRegex,
            SubjectRegexIgnoreCase = e.SubjectRegexIgnoreCase,
            SubjectRegexMatchAnywhere = e.SubjectRegexMatchAnywhere,
            FilenameTemplate = e.FilenameTemplate,
            UseSsl = e.UseSsl,
            UseStartTls = e.UseStartTls,
            DeleteAfterDownload = e.DeleteAfterDownload,
            DeleteCopyFolder = e.DeleteCopyFolder,
            PollIntervalSeconds = e.PollIntervalSeconds,
        };

    private static EmailAttachmentSettingsEntity ToEntity(EmailAttachmentSettings m) => new()
    {
        Enabled = m.Enabled,
        Host = m.Host,
        Port = m.Port,
        Username = m.Username,
        Password = m.Password,
        ImapFolder = m.ImapFolder,
        SubjectRegex = m.SubjectRegex,
        SubjectRegexIgnoreCase = m.SubjectRegexIgnoreCase,
        SubjectRegexMatchAnywhere = m.SubjectRegexMatchAnywhere,
        FilenameTemplate = m.FilenameTemplate,
        UseSsl = m.UseSsl,
        UseStartTls = m.UseStartTls,
        DeleteAfterDownload = m.DeleteAfterDownload,
        DeleteCopyFolder = m.DeleteCopyFolder,
        PollIntervalSeconds = m.PollIntervalSeconds,
    };
}
