using Papra.Companion.Services.Interfaces;

namespace Papra.Companion.BackgroundServices;

public partial class PipelineBackgroundService(
    IPipelineJobChannel queue,
    IServiceProvider services,
    ILogger<PipelineBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        LogStarted(logger);

        await foreach (var job in queue.ReadAllAsync(stoppingToken))
        {
            try
            {
                using var scope = services.CreateScope();
                var pipeline = scope.ServiceProvider.GetRequiredService<ITitleGenerationService>();
                await pipeline.ProcessAsync(job, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                LogUnhandledJobError(logger, ex, job.DocumentId);
            }
        }

        LogStopped(logger);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Title generation background service started")]
    private static partial void LogStarted(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Title generation background service stopped")]
    private static partial void LogStopped(ILogger logger);

    [LoggerMessage(Level = LogLevel.Error, Message = "Unhandled error processing job for document {DocumentId}")]
    private static partial void LogUnhandledJobError(ILogger logger, Exception ex, string documentId);
}
