using System.Threading.Channels;
using Papra.Companion.Models;
using Papra.Companion.Services.Interfaces;

namespace Papra.Companion.Services;

public class PipelineQueue : IPipelineQueue
{
    private readonly Channel<ProcessingJob> _channel = Channel.CreateBounded<ProcessingJob>(
        new BoundedChannelOptions(100) { FullMode = BoundedChannelFullMode.Wait });

    public async ValueTask EnqueueAsync(ProcessingJob job, CancellationToken ct = default)
        => await _channel.Writer.WriteAsync(job, ct);

    public IAsyncEnumerable<ProcessingJob> ReadAllAsync(CancellationToken ct = default)
        => _channel.Reader.ReadAllAsync(ct);
}
