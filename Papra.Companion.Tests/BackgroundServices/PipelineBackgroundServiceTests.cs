using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Papra.Companion.Models;
using Papra.Companion.Services.Interfaces;

namespace Papra.Companion.Tests.BackgroundServices;

public class PipelineBackgroundServiceTests
{
    [Fact]
    public async Task ExecuteAsyncDequeuesJobAndCallsPipeline()
    {
        var queue = new TestPipelineJobChannel();
        var job = new ProcessingJob { DocumentId = "doc1", OrganizationId = "org1" };
        await queue.EnqueueAsync(job, TestContext.Current.CancellationToken);

        var pipeline = Substitute.For<ITitleGenerationService>();
        var services = BuildServiceProvider(pipeline);
        var logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<PipelineBackgroundService>.Instance;
        var svc = new PipelineBackgroundService(queue, services, logger);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        pipeline.When(p => p.ProcessAsync(Arg.Any<ProcessingJob>(), Arg.Any<CancellationToken>()))
                .Do(_ => cts.Cancel());

        try { await svc.StartAsync(cts.Token); await svc.ExecuteTask!; }
        catch (OperationCanceledException) { }

        await pipeline.Received(1).ProcessAsync(
            Arg.Is<ProcessingJob>(j => j!.DocumentId == "doc1"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsyncWhenPipelineThrowsContinuesProcessingNextJob()
    {
        var queue = new TestPipelineJobChannel();
        var job1 = new ProcessingJob { DocumentId = "doc1", OrganizationId = "org1" };
        var job2 = new ProcessingJob { DocumentId = "doc2", OrganizationId = "org1" };
        await queue.EnqueueAsync(job1, TestContext.Current.CancellationToken);
        await queue.EnqueueAsync(job2, TestContext.Current.CancellationToken);

        var callCount = 0;
        var pipeline = Substitute.For<ITitleGenerationService>();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

        pipeline.When(p => p.ProcessAsync(Arg.Any<ProcessingJob>(), Arg.Any<CancellationToken>()))
                .Do(_ =>
                {
                    callCount++;
                    if (callCount == 1) throw new InvalidOperationException("first job exploded");
                    if (callCount == 2) cts.Cancel();
                });

        var services = BuildServiceProvider(pipeline);
        var logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<PipelineBackgroundService>.Instance;
        var svc = new PipelineBackgroundService(queue, services, logger);

        try { await svc.StartAsync(cts.Token); await svc.ExecuteTask!; }
        catch (OperationCanceledException) { }

        await pipeline.Received(2).ProcessAsync(Arg.Any<ProcessingJob>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsyncWhenCancelledStopsGracefully()
    {
        var queue = new TestPipelineJobChannel();
        var pipeline = Substitute.For<ITitleGenerationService>();
        var services = BuildServiceProvider(pipeline);
        var logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<PipelineBackgroundService>.Instance;
        var svc = new PipelineBackgroundService(queue, services, logger);

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));
        await svc.StartAsync(cts.Token);

        await Task.WhenAny(svc.ExecuteTask!, Task.Delay(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken));
        Assert.True(svc.ExecuteTask!.IsCompleted);
    }

    private static ServiceProvider BuildServiceProvider(ITitleGenerationService pipeline)
    {
        var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        services.AddScoped<ITitleGenerationService>(_ => pipeline);
        return services.BuildServiceProvider();
    }

    private sealed class PipelineBackgroundService(
        IPipelineJobChannel queue,
        IServiceProvider services,
        Microsoft.Extensions.Logging.ILogger<Papra.Companion.BackgroundServices.PipelineBackgroundService> logger)
        : Papra.Companion.BackgroundServices.PipelineBackgroundService(queue, services, logger)
    {
        public new Task? ExecuteTask => base.ExecuteTask;
    }

    private sealed class TestPipelineJobChannel : IPipelineJobChannel
    {
        private readonly System.Threading.Channels.Channel<ProcessingJob> _ch =
            System.Threading.Channels.Channel.CreateUnbounded<ProcessingJob>();

        public async ValueTask EnqueueAsync(ProcessingJob job, CancellationToken ct = default)
            => await _ch.Writer.WriteAsync(job, ct);

        public IAsyncEnumerable<ProcessingJob> ReadAllAsync(CancellationToken ct = default)
            => _ch.Reader.ReadAllAsync(ct);
    }
}
