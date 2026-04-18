namespace Papra.Companion.Models;

public class ProcessingJob
{
    public string OrganizationId { get; set; } = string.Empty;
    public string DocumentId { get; set; } = string.Empty;
    public DateTimeOffset ReceivedAt { get; set; } = DateTimeOffset.UtcNow;
}
