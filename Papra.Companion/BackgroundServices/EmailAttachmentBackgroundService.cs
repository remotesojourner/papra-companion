using Papra.Companion.Services;
using Papra.Companion.Services.Interfaces;

namespace Papra.Companion.BackgroundServices;

public partial class EmailAttachmentBackgroundService(
    IEmailAttachmentSettingsService settingsService,
    IServiceProvider services,
    ILogger<EmailAttachmentBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Email attachment downloader background service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            var settings = settingsService.Current;
            var delaySeconds = settings.PollIntervalSeconds > 0 ? settings.PollIntervalSeconds : 300;

            if (settings.Enabled && settings.IsConfigured)
            {
                try
                {
                    using var scope = services.CreateScope();
                    var svc = scope.ServiceProvider.GetRequiredService<IEmailAttachmentService>();
                    var results = await svc.RunAsync(stoppingToken);
                    LogRunComplete(logger, results.Count);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Unhandled error in email attachment downloader");
                }
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(delaySeconds), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        logger.LogInformation("Email attachment downloader background service stopped");
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Email attachment run complete: {Count} attachments processed")]
    private static partial void LogRunComplete(ILogger logger, int count);
}
