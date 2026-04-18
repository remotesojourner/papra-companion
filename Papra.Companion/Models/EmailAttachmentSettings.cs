namespace Papra.Companion.Models;

public class EmailAttachmentSettings
{
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

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Host) &&
        !string.IsNullOrWhiteSpace(Username) &&
        !string.IsNullOrWhiteSpace(Password);
}
