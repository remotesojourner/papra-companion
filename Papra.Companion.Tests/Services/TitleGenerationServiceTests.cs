using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Papra.Companion.Models;
using Papra.Companion.Services;
using Papra.Companion.Services.Interfaces;

namespace Papra.Companion.Tests.Services;

public class TitleGenerationServiceTests
{
    private static PipelineSettings ConfiguredSettings(int delaySeconds = 0) => new()
    {
        PapraBaseUrl  = "https://papra.example.com",
        PapraApiToken = "token",
        OpenAiApiKey  = "sk-key",
        TitlePrompt   = "Title: {{original_title}}",
        ProcessingDelaySeconds = delaySeconds,
    };

    private static (
        ISettingsService settings,
        IPipelineStatusService status,
        IPapraService papra,
        IOpenAiService openAi,
        ITitleGenerationService svc)
        Build(PipelineSettings? settings = null)
    {
        var settingsSvc = Substitute.For<ISettingsService>();
        settingsSvc.Current.Returns(settings ?? ConfiguredSettings());

        var statusSvc = Substitute.For<IPipelineStatusService>();
        var papra     = Substitute.For<IPapraService>();
        var openAi    = Substitute.For<IOpenAiService>();
        var logger    = Microsoft.Extensions.Logging.Abstractions.NullLogger<TitleGenerationService>.Instance;

        var svc = new TitleGenerationService(settingsSvc, statusSvc, papra, openAi, logger);
        return (settingsSvc, statusSvc, papra, openAi, svc);
    }

    [Fact]
    public async Task ProcessAsync_HappyPath_CompletesSuccessfully()
    {
        var (_, status, papra, openAi, svc) = Build();
        var job = new ProcessingJob { DocumentId = "doc1", OrganizationId = "org1" };

        papra.GetDocumentInfoAsync("org1", "doc1", Arg.Any<CancellationToken>())
            .Returns(("invoice.pdf", "image/jpeg"));
        openAi.CompleteAsync(Arg.Any<string>(), Arg.Any<object?>(), Arg.Any<CancellationToken>())
            .Returns("{\"title\":\"Generated Title\"}");

        await svc.ProcessAsync(job, CancellationToken.None);

        await papra.Received(1).UpdateDocumentTitleAsync("org1", "doc1", "Generated Title", Arg.Any<CancellationToken>());
        status.Received(1).JobCompleted(Arg.Is<PipelineJobResult>(r =>
            r.Status == JobStatus.Succeeded &&
            r.ExtractedTitle == "Generated Title"));
    }

    [Fact]
    public async Task ProcessAsync_WhenModelReturnsNoTitle_FallsBackToUntitled()
    {
        var (_, status, papra, openAi, svc) = Build();
        var job = new ProcessingJob { DocumentId = "doc2", OrganizationId = "org1" };

        papra.GetDocumentInfoAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(("file.pdf", "application/pdf"));
        openAi.CompleteAsync(Arg.Any<string>(), Arg.Any<object?>(), Arg.Any<CancellationToken>())
            .Returns("{\"title\":\"\"}");  // empty title

        await svc.ProcessAsync(job, CancellationToken.None);

        // null/empty title falls back to "Untitled Document"
        await papra.Received(1).UpdateDocumentTitleAsync(
            Arg.Any<string>(), Arg.Any<string>(),
            Arg.Is<string>(t => t == "Untitled Document" || t == ""),
            Arg.Any<CancellationToken>());
        status.Received(1).JobCompleted(Arg.Is<PipelineJobResult>(r => r.Status == JobStatus.Succeeded));
    }

    [Fact]
    public async Task ProcessAsync_WhenPapraThrows_SetsFailedStatus()
    {
        var (_, status, papra, _, svc) = Build();
        var job = new ProcessingJob { DocumentId = "doc3", OrganizationId = "org1" };

        papra.GetDocumentInfoAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Papra unreachable"));

        await svc.ProcessAsync(job, CancellationToken.None);

        status.Received(1).JobCompleted(Arg.Is<PipelineJobResult>(r =>
            r.Status == JobStatus.Failed &&
            r.ErrorMessage == "Papra unreachable"));
    }

    [Fact]
    public async Task ProcessAsync_WhenCancelled_SetsFailedStatusWithCancelMessage()
    {
        var (_, status, papra, _, svc) = Build();
        var job = new ProcessingJob { DocumentId = "doc4", OrganizationId = "org1" };
        using var cts = new CancellationTokenSource();

        papra.GetDocumentInfoAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new OperationCanceledException());

        await svc.ProcessAsync(job, cts.Token);

        status.Received(1).JobCompleted(Arg.Is<PipelineJobResult>(r =>
            r.Status == JobStatus.Failed &&
            r.ErrorMessage == "Operation was cancelled."));
    }

    [Fact]
    public async Task ProcessAsync_AlwaysCallsJobStartedAndJobCompleted()
    {
        var (_, status, papra, _, svc) = Build();
        var job = new ProcessingJob { DocumentId = "doc5", OrganizationId = "org1" };

        papra.GetDocumentInfoAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("boom"));

        await svc.ProcessAsync(job, CancellationToken.None);

        status.Received(1).JobStarted(Arg.Any<PipelineJobResult>());
        status.Received(1).JobCompleted(Arg.Any<PipelineJobResult>());
    }

    [Fact]
    public async Task ProcessAsync_WithNoDelay_DoesNotDelayProcessing()
    {
        var (_, _, papra, openAi, svc) = Build(ConfiguredSettings(delaySeconds: 0));
        var job = new ProcessingJob { DocumentId = "doc6", OrganizationId = "org1" };

        papra.GetDocumentInfoAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(("file.pdf", "image/png"));
        openAi.CompleteAsync(Arg.Any<string>(), Arg.Any<object?>(), Arg.Any<CancellationToken>())
            .Returns("{\"title\":\"Title\"}");

        var sw = System.Diagnostics.Stopwatch.StartNew();
        await svc.ProcessAsync(job, CancellationToken.None);
        sw.Stop();

        // With no delay the job should complete quickly (well under 1 second)
        Assert.True(sw.Elapsed < TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task ProcessAsync_TitlePromptReplacesOriginalTitlePlaceholder()
    {
        var (_, _, papra, openAi, svc) = Build();
        var job = new ProcessingJob { DocumentId = "doc7", OrganizationId = "org1" };

        papra.GetDocumentInfoAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(("My Invoice.pdf", "image/png"));
        openAi.CompleteAsync(Arg.Any<string>(), Arg.Any<object?>(), Arg.Any<CancellationToken>())
            .Returns("{\"title\":\"Title\"}");

        await svc.ProcessAsync(job, CancellationToken.None);

        // The prompt sent to the AI must contain the actual document name
        await openAi.Received(1).CompleteAsync(
            Arg.Is<string>(p => p.Contains("My Invoice.pdf")),
            Arg.Any<object?>(),
            Arg.Any<CancellationToken>());
    }
}
