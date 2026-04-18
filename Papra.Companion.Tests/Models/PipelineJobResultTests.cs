using Papra.Companion.Models;

namespace Papra.Companion.Tests.Models;

public class PipelineJobResultTests
{
    [Fact]
    public void Duration_WhenCompletedAtIsNull_ReturnsNull()
    {
        var result = new PipelineJobResult
        {
            StartedAt = DateTimeOffset.UtcNow,
            CompletedAt = null
        };

        Assert.Null(result.Duration);
    }

    [Fact]
    public void Duration_WhenCompletedAtIsSet_ReturnsCorrectTimeSpan()
    {
        var start = new DateTimeOffset(2024, 1, 1, 12, 0, 0, TimeSpan.Zero);
        var end = start.AddSeconds(4.5);
        var result = new PipelineJobResult { StartedAt = start, CompletedAt = end };

        Assert.Equal(TimeSpan.FromSeconds(4.5), result.Duration);
    }

    [Fact]
    public void Status_DefaultsToQueued()
    {
        var result = new PipelineJobResult();
        Assert.Equal(JobStatus.Queued, result.Status);
    }
}
