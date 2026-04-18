using System.Text.Json.Serialization;

namespace Papra.Companion.Models;

public class PipelineJobResult
{
    public string DocumentId { get; set; } = string.Empty;
    public string OrganizationId { get; set; } = string.Empty;
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public JobStatus Status { get; set; } = JobStatus.Queued;
    public string? ExtractedTitle { get; set; }
    public string? ErrorMessage { get; set; }

    [JsonIgnore]
    public TimeSpan? Duration => CompletedAt.HasValue ? CompletedAt.Value - StartedAt : null;
}
