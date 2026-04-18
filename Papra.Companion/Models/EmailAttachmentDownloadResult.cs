namespace Papra.Companion.Models;

public class EmailAttachmentDownloadResult
{
    public string MessageId { get; set; } = string.Empty;
    public string AttachmentName { get; set; } = string.Empty;
    public string SavedPath { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public DateTimeOffset MessageDate { get; set; }
    public DateTimeOffset DownloadedAt { get; set; }
    public bool Succeeded { get; set; }
    public string? ErrorMessage { get; set; }
}
