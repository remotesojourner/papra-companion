using Papra.Companion.Models;

namespace Papra.Companion.Services.Interfaces;

public interface IPipelineJobChannel
{
    ValueTask EnqueueAsync(ProcessingJob job, CancellationToken ct = default);
    IAsyncEnumerable<ProcessingJob> ReadAllAsync(CancellationToken ct = default);
}
