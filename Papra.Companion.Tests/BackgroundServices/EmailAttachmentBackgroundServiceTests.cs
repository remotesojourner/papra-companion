using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Papra.Companion.BackgroundServices;
using Papra.Companion.Models;
using Papra.Companion.Services.Interfaces;

namespace Papra.Companion.Tests.BackgroundServices;

public class EmailAttachmentBackgroundServiceTests
{
    private static IEmailAttachmentSettingsService MakeSettingsService(EmailAttachmentSettings settings)
    {
        var svc = Substitute.For<IEmailAttachmentSettingsService>();
        svc.Current.Returns(settings);
        return svc;
    }

    private static (IServiceProvider provider, IEmailAttachmentService attachmentSvc)
        BuildServiceProvider()
    {
        var attachmentSvc = Substitute.For<IEmailAttachmentService>();
        attachmentSvc.RunAsync(Arg.Any<CancellationToken>())
                     .Returns([]);
        var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        services.AddScoped<IEmailAttachmentService>(_ => attachmentSvc);
        return (services.BuildServiceProvider(), attachmentSvc);
    }

    [Fact]
    public async Task ExecuteAsync_WhenEnabledAndConfigured_CallsRunAsync()
    {
        var settings = new EmailAttachmentSettings
        {
            Enabled = true,
            Host = "imap.example.com",
            Username = "u",
            Password = "p",
            PollIntervalSeconds = 1,
        };
        var settingsSvc = MakeSettingsService(settings);
        var (provider, attachmentSvc) = BuildServiceProvider();
        var logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<EmailAttachmentBackgroundService>.Instance;

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        attachmentSvc.When(s => s.RunAsync(Arg.Any<CancellationToken>()))
                     .Do(_ => cts.Cancel());

        var bgSvc = new EmailAttachmentBackgroundService(settingsSvc, provider, logger);
        try { await bgSvc.StartAsync(cts.Token); await bgSvc.ExecuteTask!; }
        catch (OperationCanceledException) { }

        await attachmentSvc.Received(1).RunAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenDisabled_DoesNotCallRunAsync()
    {
        var settings = new EmailAttachmentSettings
        {
            Enabled = false,
            Host = "imap.example.com",
            Username = "u",
            Password = "p",
            PollIntervalSeconds = 1,
        };
        var settingsSvc = MakeSettingsService(settings);
        var (provider, attachmentSvc) = BuildServiceProvider();
        var logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<EmailAttachmentBackgroundService>.Instance;

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(150));
        var bgSvc = new EmailAttachmentBackgroundService(settingsSvc, provider, logger);
        try { await bgSvc.StartAsync(cts.Token); await bgSvc.ExecuteTask!; }
        catch (OperationCanceledException) { }

        await attachmentSvc.DidNotReceive().RunAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenNotConfigured_DoesNotCallRunAsync()
    {
        // Missing password — IsConfigured returns false
        var settings = new EmailAttachmentSettings
        {
            Enabled = true,
            Host = "imap.example.com",
            Username = "u",
            Password = string.Empty,
            PollIntervalSeconds = 1,
        };
        var settingsSvc = MakeSettingsService(settings);
        var (provider, attachmentSvc) = BuildServiceProvider();
        var logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<EmailAttachmentBackgroundService>.Instance;

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(150));
        var bgSvc = new EmailAttachmentBackgroundService(settingsSvc, provider, logger);
        try { await bgSvc.StartAsync(cts.Token); await bgSvc.ExecuteTask!; }
        catch (OperationCanceledException) { }

        await attachmentSvc.DidNotReceive().RunAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenRunAsyncThrows_ContinuesLoop()
    {
        var settings = new EmailAttachmentSettings
        {
            Enabled = true,
            Host = "imap.example.com",
            Username = "u",
            Password = "p",
            PollIntervalSeconds = 1,
        };
        var settingsSvc = MakeSettingsService(settings);
        var (provider, attachmentSvc) = BuildServiceProvider();
        var logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<EmailAttachmentBackgroundService>.Instance;

        var callCount = 0;
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        attachmentSvc.When(s => s.RunAsync(Arg.Any<CancellationToken>()))
                     .Do(_ =>
                     {
                         callCount++;
                         if (callCount == 1) throw new Exception("transient error");
                         if (callCount == 2) cts.Cancel();
                     });

        var bgSvc = new EmailAttachmentBackgroundService(settingsSvc, provider, logger);
        try { await bgSvc.StartAsync(cts.Token); await bgSvc.ExecuteTask!; }
        catch (OperationCanceledException) { }

        Assert.Equal(2, callCount);
    }

    [Fact]
    public async Task ExecuteAsync_WhenCancelled_StopsGracefully()
    {
        var settings = new EmailAttachmentSettings
        {
            Enabled = true,
            Host = "imap.example.com",
            Username = "u",
            Password = "p",
            PollIntervalSeconds = 3600, // long delay so it sits in Task.Delay
        };
        var settingsSvc = MakeSettingsService(settings);
        var (provider, _) = BuildServiceProvider();
        var logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<EmailAttachmentBackgroundService>.Instance;

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
        var bgSvc = new EmailAttachmentBackgroundService(settingsSvc, provider, logger);
        await bgSvc.StartAsync(cts.Token);

        await Task.WhenAny(bgSvc.ExecuteTask!, Task.Delay(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken));
        Assert.True(bgSvc.ExecuteTask!.IsCompleted);
    }

    // Expose ExecuteTask
    private class EmailAttachmentBackgroundService(
        IEmailAttachmentSettingsService settingsSvc,
        IServiceProvider services,
        Microsoft.Extensions.Logging.ILogger<Papra.Companion.BackgroundServices.EmailAttachmentBackgroundService> logger)
        : Papra.Companion.BackgroundServices.EmailAttachmentBackgroundService(settingsSvc, services, logger)
    {
        public new Task? ExecuteTask => base.ExecuteTask;
    }
}
