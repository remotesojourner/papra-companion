namespace Papra.Companion.Data.Entities;

/// <summary>Single-row IMAP attachment downloader settings — always Id = 1.</summary>
public class EmailAttachmentSettingsEntity
{
    public int Id { get; set; } = 1;
    public bool Enabled { get; set; }
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 993;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ImapFolder { get; set; } = "INBOX";
    public string SubjectRegex { get; set; } = string.Empty;
    public bool SubjectRegexIgnoreCase { get; set; } = true;
    public bool SubjectRegexMatchAnywhere { get; set; }
    public string FilenameTemplate { get; set; } = string.Empty;
    public bool UseSsl { get; set; } = true;
    public bool UseStartTls { get; set; }
    public bool DeleteAfterDownload { get; set; }
    public string DeleteCopyFolder { get; set; } = string.Empty;
    public int PollIntervalSeconds { get; set; } = 300;
}
