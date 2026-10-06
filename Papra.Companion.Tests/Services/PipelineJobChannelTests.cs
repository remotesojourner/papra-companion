using Papra.Companion.Models;
using Papra.Companion.Services;

namespace Papra.Companion.Tests.Services;

public class PipelineJobChannelTests
{
    [Fact]
    public async Task EnqueueAsyncThenReadAllAsyncYieldsEnqueuedJob()
    {
        var queue = new PipelineJobChannel();
        var job = new ProcessingJob { DocumentId = "doc1", OrganizationId = "org1" };

        await queue.EnqueueAsync(job, TestContext.Current.CancellationToken);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        var received = await queue.ReadAllAsync(cts.Token).FirstAsync(TestContext.Current.CancellationToken);

        Assert.Equal("doc1", received.DocumentId);
        Assert.Equal("org1", received.OrganizationId);
    }

    [Fact]
    public async Task EnqueueAsyncMultipleJobsPreservesOrder()
    {
        var queue = new PipelineJobChannel();
        var jobs = Enumerable.Range(1, 5)
            .Select(i => new ProcessingJob { DocumentId = $"doc{i}" })
            .ToList();

        foreach (var job in jobs)
            await queue.EnqueueAsync(job, TestContext.Current.CancellationToken);

        var results = new List<ProcessingJob>();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        try
        {
            await foreach (var j in queue.ReadAllAsync(cts.Token))
            {
                results.Add(j);
                if (results.Count == jobs.Count)
                    break;
            }
        }
        catch (OperationCanceledException) { }

        Assert.Equal(jobs.Select(j => j.DocumentId), results.Select(r => r.DocumentId));
    }
}
