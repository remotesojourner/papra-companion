namespace Papra.Companion.Data.Entities;

public class JobResultEntity
{
    public int Id { get; set; }
    public string DocumentId { get; set; } = string.Empty;
    public string OrganizationId { get; set; } = string.Empty;
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ExtractedTitle { get; set; }
    public string? ErrorMessage { get; set; }
}
