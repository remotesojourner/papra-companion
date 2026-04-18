using System.Text.RegularExpressions;
using MailKit;
using Papra.Companion.Constants;
using MailKit.Net.Imap;
using MailKit.Search;
using MailKit.Security;
using MimeKit;
using Papra.Companion.Data.Entities;
using Papra.Companion.Data.Repositories.Interfaces;
using Papra.Companion.Models;
using Papra.Companion.Services.Interfaces;

namespace Papra.Companion.Services;

public partial class EmailAttachmentService(
    IEmailAttachmentSettingsService settingsService,
    IEmailAttachmentLogRepository logRepository,
    IWebHostEnvironment env,
    ILogger<EmailAttachmentService> logger) : IEmailAttachmentService
{
    public async Task<IReadOnlyList<EmailAttachmentDownloadResult>> RunAsync(CancellationToken ct)
    {
        var settings = settingsService.Current;
        var results = new List<EmailAttachmentDownloadResult>();

        if (!settings.IsConfigured)
        {
            logger.LogWarning("Email attachment downloader is not configured — skipping run");
            return results;
        }

        using var client = new ImapClient();
        await ConnectAsync(client, settings, ct);

        try
        {
            var folder = await OpenFolderAsync(client, settings.ImapFolder, ct);

            var uids = await folder.SearchAsync(SearchQuery.All, ct);
            LogFoundMessages(logger, uids.Count, settings.ImapFolder);

            var subjectFilter = BuildSubjectRegex(settings);

            var toDelete = new List<UniqueId>();

            foreach (var uid in uids)
            {
                ct.ThrowIfCancellationRequested();

                var message = await folder.GetMessageAsync(uid, ct);
                var messageId = message.MessageId ?? uid.ToString();
                var subject = message.Subject ?? string.Empty;
                var from = message.From.Mailboxes.FirstOrDefault()?.Address ?? string.Empty;
                var date = message.Date;

                if (subjectFilter is not null && !subjectFilter.IsMatch(subject))
                    continue;

                var anyAttachment = false;

                foreach (var attachment in message.Attachments.OfType<MimePart>())
                {
                    ct.ThrowIfCancellationRequested();
                    var attachmentName = attachment.FileName ?? $"attachment_{Guid.NewGuid()}";

                    if (logRepository.HasBeenDownloaded(messageId, attachmentName))
                    {
                        LogSkippingAlreadyDownloaded(logger, attachmentName, messageId);
                        continue;
                    }

                    anyAttachment = true;
                    var result = new EmailAttachmentDownloadResult
                    {
                        MessageId = messageId,
                        AttachmentName = attachmentName,
                        Subject = subject,
                        FromEmail = from,
                        MessageDate = date,
                        DownloadedAt = DateTimeOffset.UtcNow,
                    };

                    try
                    {
                        var savePath = BuildSavePath(settings, env.ContentRootPath, messageId, attachmentName, subject, from, date);
                        Directory.CreateDirectory(Path.GetDirectoryName(savePath)!);

                        await using var stream = File.Create(savePath);
                        if (attachment.Content is not null)
                            await attachment.Content.DecodeToAsync(stream, ct);

                        result.SavedPath = savePath;
                        result.Succeeded = true;
                        LogDownloadedAttachment(logger, attachmentName, savePath);
                    }
                    catch (Exception ex)
                    {
                        result.Succeeded = false;
                        result.ErrorMessage = ex.Message;
                        LogFailedToSaveAttachment(logger, ex, attachmentName);
                    }

                    results.Add(result);
                    await logRepository.AddAsync(new EmailAttachmentLogEntity
                    {
                        MessageId = result.MessageId,
                        AttachmentName = result.AttachmentName,
                        SavedPath = result.SavedPath,
                        Subject = result.Subject,
                        FromEmail = result.FromEmail,
                        MessageDate = result.MessageDate,
                        DownloadedAt = result.DownloadedAt,
                        Succeeded = result.Succeeded,
                        ErrorMessage = result.ErrorMessage,
                    });
                }

                if (anyAttachment && settings.DeleteAfterDownload)
                {
                    if (!string.IsNullOrWhiteSpace(settings.DeleteCopyFolder))
                    {
                        var copyDest = await OpenFolderAsync(client, settings.DeleteCopyFolder, ct);
                        // Re-open the source folder — some IMAP servers close it when a second folder is opened.
                        if (!folder.IsOpen)
                            await folder.OpenAsync(FolderAccess.ReadWrite, ct);
                        await folder.CopyToAsync(uid, copyDest, ct);
                    }
                    toDelete.Add(uid);
                }
            }

            if (toDelete.Count > 0)
            {
                if (!folder.IsOpen)
                    await folder.OpenAsync(FolderAccess.ReadWrite, ct);
                foreach (var uid in toDelete)
                    await folder.AddFlagsAsync(uid, MessageFlags.Deleted, true, ct);
                await folder.ExpungeAsync(ct);
                LogDeletedMessages(logger, toDelete.Count);
            }
        }
        finally
        {
            await client.DisconnectAsync(true, ct);
        }

        return results;
    }

    private static async Task ConnectAsync(ImapClient client, EmailAttachmentSettings settings, CancellationToken ct)
    {
        var socketOptions = settings.UseStartTls
            ? SecureSocketOptions.StartTls
            : settings.UseSsl
                ? SecureSocketOptions.SslOnConnect
                : SecureSocketOptions.None;

        await client.ConnectAsync(settings.Host, settings.Port, socketOptions, ct);
        await client.AuthenticateAsync(settings.Username, settings.Password, ct);
    }

    private static async Task<IMailFolder> OpenFolderAsync(ImapClient client, string folderName, CancellationToken ct)
    {
        var folder = string.IsNullOrWhiteSpace(folderName)
            ? client.Inbox!
            : await client.GetFolderAsync(folderName, ct)
              ?? throw new InvalidOperationException($"IMAP folder '{folderName}' was not found.");
        await folder.OpenAsync(FolderAccess.ReadWrite, ct);
        return folder;
    }

    internal static Regex? BuildSubjectRegex(EmailAttachmentSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.SubjectRegex))
            return null;

        var options = settings.SubjectRegexIgnoreCase ? RegexOptions.IgnoreCase : RegexOptions.None;
        var pattern = settings.SubjectRegexMatchAnywhere
            ? settings.SubjectRegex
            : $"^{settings.SubjectRegex}";

        return new Regex(pattern, options);
    }

    internal static string BuildSavePath(
        EmailAttachmentSettings settings,
        string contentRootPath,
        string messageId, string attachmentName, string subject, string fromEmail, DateTimeOffset date)
    {
        string filename;

        if (!string.IsNullOrWhiteSpace(settings.FilenameTemplate))
        {
            var safeSubject = SanitizePath(subject);
            var safeFrom   = SanitizePath(fromEmail);
            var safeName   = SanitizePath(attachmentName);
            var safeMsgId  = SanitizePath(messageId);

            filename = settings.FilenameTemplate
                .Replace("{{ message_id }}", safeMsgId)
                .Replace("{{message_id}}", safeMsgId)
                .Replace("{{ attachment_name }}", safeName)
                .Replace("{{attachment_name}}", safeName)
                .Replace("{{ subject }}", safeSubject)
                .Replace("{{subject}}", safeSubject)
                .Replace("{{ from_email }}", safeFrom)
                .Replace("{{from_email}}", safeFrom)
                .Replace("{{ date }}", date.ToString("yyyy-MM-dd"))
                .Replace("{{date}}", date.ToString("yyyy-MM-dd"));
        }
        else
        {
            filename = SanitizePath(attachmentName);
        }

        return Path.Combine(contentRootPath, AppPaths.AttachmentsFolder, filename);
    }

    private static readonly char[] InvalidFileNameChars =
        [.. Path.GetInvalidFileNameChars().Union([':', '*', '?', '"', '<', '>', '|', '\\', '/'])];

    internal static string SanitizePath(string value)
    {
        foreach (var c in InvalidFileNameChars)
            value = value.Replace(c, '_');
        return value;
    }

    public async Task<string> TestConnectionAsync(EmailAttachmentSettings settings, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(settings.Password))
            settings.Password = settingsService.Current.Password;
        using var client = new ImapClient();
        await ConnectAsync(client, settings, ct);
        try
        {
            await OpenFolderAsync(client, settings.ImapFolder, ct);
            return "Connected";
        }
        finally
        {
            await client.DisconnectAsync(true, ct);
        }
    }

    public async Task EnsureCopyFolderExistsAsync(CancellationToken ct)
    {
        var settings = settingsService.Current;
        if (!settings.DeleteAfterDownload || string.IsNullOrWhiteSpace(settings.DeleteCopyFolder))
            return;

        using var client = new ImapClient();
        await ConnectAsync(client, settings, ct);
        try
        {
            var found = false;
            try
            {
                await client.GetFolderAsync(settings.DeleteCopyFolder, ct);
                found = true;
            }
            catch (FolderNotFoundException) { }

            if (!found)
            {
                var personal = client.GetFolder(client.PersonalNamespaces[0]);
                await personal.CreateAsync(settings.DeleteCopyFolder, true, ct);
                LogCreatedImapFolder(logger, settings.DeleteCopyFolder);
            }
        }
        finally
        {
            await client.DisconnectAsync(true, ct);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Found {Count} messages in {Folder}")]
    private static partial void LogFoundMessages(ILogger logger, int count, string folder);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Skipping already-downloaded {AttachmentName} from {MessageId}")]
    private static partial void LogSkippingAlreadyDownloaded(ILogger logger, string attachmentName, string messageId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Downloaded {AttachmentName} → {SavedPath}")]
    private static partial void LogDownloadedAttachment(ILogger logger, string attachmentName, string savedPath);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to save attachment {AttachmentName}")]
    private static partial void LogFailedToSaveAttachment(ILogger logger, Exception ex, string attachmentName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Deleted {Count} messages from mailbox")]
    private static partial void LogDeletedMessages(ILogger logger, int count);

    [LoggerMessage(Level = LogLevel.Information, Message = "Created IMAP folder '{Folder}'")]
    private static partial void LogCreatedImapFolder(ILogger logger, string folder);
}
