namespace Papra.Companion.Data.Entities;

/// <summary>One row per downloaded attachment — used for deduplication and history.</summary>
public class EmailAttachmentLogEntity
{
    public int Id { get; set; }
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
