using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
using Papra.Companion.BackgroundServices;
using Papra.Companion.Data.Entities;
using Papra.Companion.Data.Repositories.Interfaces;
using Papra.Companion.Models;
using Papra.Companion.Services.Interfaces;

namespace Papra.Companion.Tests.Browser;

public sealed class BrowserApp : TestAppFactory
{
    private static readonly DateTimeOffset _today = new(2026, 10, 6, 8, 0, 0, TimeSpan.Zero);

    public BrowserApp()
    {
        UseKestrel(0);
    }

    public Uri BaseAddress
    {
        get
        {
            StartServer();
            return new Uri(Services.GetRequiredService<IServer>().Features.GetRequiredFeature<IServerAddressesFeature>().Addresses.First());
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureServices(services =>
        {
            foreach (var service in services.Where(s => s.ImplementationType == typeof(PipelineBackgroundService) || s.ImplementationType == typeof(EmailAttachmentBackgroundService)).ToList())
                services.Remove(service);

            var settings = Substitute.For<ISettingsService>();
            settings.Current.Returns(new PipelineSettings
            {
                PapraBaseUrl = "https://papra.example.com",
                PapraApiToken = "papra-token",
                OpenAiApiKey = "sk-test",
                ProcessingDelaySeconds = 5,
            });
            services.Replace(ServiceDescriptor.Singleton(settings));

            var emailSettings = Substitute.For<IEmailAttachmentSettingsService>();
            emailSettings.Current.Returns(new EmailAttachmentSettings
            {
                Host = "imap.example.com",
                Username = "me@example.com",
                Password = "imap-password",
                SubjectRegex = "Invoice|Receipt",
                FilenameTemplate = "{{ date }}/{{ subject }}/{{ attachment_name }}",
                DeleteAfterDownload = true,
                DeleteCopyFolder = "Archived",
            });
            services.Replace(ServiceDescriptor.Singleton(emailSettings));

            var status = Substitute.For<IPipelineStatusService>();
            status.RecentJobs.Returns(Jobs());
            services.Replace(ServiceDescriptor.Singleton(status));

            var log = Substitute.For<IEmailAttachmentLogRepository>();
            log.GetRecent(Arg.Any<int>()).Returns(Downloads());
            services.Replace(ServiceDescriptor.Singleton(log));
        });
    }

    private static List<PipelineJobResult> Jobs() =>
    [
        Job("doc_m5n6b7v8c9x0z1a2s3d4f5g6", 270, JobStatus.Succeeded, "Payslip October 2026"),
        Job("doc_h3j4k5l6q7w8e9r0t1y2u3i4", 195, JobStatus.Succeeded, "Aviva Home Insurance Renewal"),
        Job("doc_z1x2c3v4b5n6m7a8s9d0f1g2", 122, JobStatus.Failed, null, "OpenAI request failed with 429: Rate limit reached for gpt-4o-mini."),
        Job("doc_p0o9i8u7y6t5r4e3w2q1a2s3", 100, JobStatus.Succeeded, "British Gas Electricity Statement September 2026"),
        Job("doc_k2l9x0b1q8w3e7r5t6y4u2i1", 12, JobStatus.Succeeded, "Council Tax Bill 2026-27")
    ];

    private static PipelineJobResult Job(string id, int minutes, JobStatus status, string? title, string? error = null) => new()
    {
        DocumentId = id,
        OrganizationId = "org_xejmmeur0dtimzy16m4qe5kg",
        StartedAt = _today.AddMinutes(minutes),
        CompletedAt = _today.AddMinutes(minutes).AddSeconds(2.4),
        Status = status,
        ExtractedTitle = title,
        ErrorMessage = error,
    };

    private static List<EmailAttachmentLogEntity> Downloads() =>
    [
        Download("statement.pdf", "Your monthly statement is ready", "noreply@bank.example.com", -1, "Access to the path is denied."),
        Download("receipt.pdf", "Receipt for your order #88341", "orders@example-shop.co.uk", -3),
        Download("invoice-10432.pdf", "Your invoice 10432 from Octopus Energy", "billing@octopus.energy", -5)
    ];

    private static EmailAttachmentLogEntity Download(string name, string subject, string from, int days, string? error = null) => new()
    {
        MessageId = $"<{name}@mail.example.com>",
        AttachmentName = name,
        SavedPath = $"/app/attachments/{name}",
        Subject = subject,
        FromEmail = from,
        MessageDate = _today.AddDays(days),
        DownloadedAt = _today.AddDays(days),
        Succeeded = error is null,
        ErrorMessage = error,
    };
}
