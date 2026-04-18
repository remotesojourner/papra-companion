using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Papra.Companion.Models;
using Papra.Companion.Services;
using Papra.Companion.Services.Interfaces;

namespace Papra.Companion.Tests.Services;

public class DocumentPipelineServiceTests
{
    private static PipelineSettings ConfiguredSettings(bool useMistral = false) => new()
    {
        PapraBaseUrl = "https://papra.example.com",
        PapraApiToken = "token",
        OpenAiApiKey = "sk-key",
        MistralApiKey = useMistral ? "mistral-key" : string.Empty,
        TitlePrompt = "Title: {{original_title}} Content: {{content}}",
        TagPrompt = "Tags: {{available_tags}} Title: {{original_title}} Content: {{content}}",
        OcrPrompt = "Extract text",
    };

    private static (
        ISettingsService settings,
        IPipelineStatusService status,
        IPapraService papra,
        IOpenAiService openAi,
        IMistralService mistral,
        DocumentPipelineService svc)
        Build(PipelineSettings? settings = null)
    {
        var settingsSvc = Substitute.For<ISettingsService>();
        settingsSvc.Current.Returns(settings ?? ConfiguredSettings());

        var statusSvc = Substitute.For<IPipelineStatusService>();
        var papra = Substitute.For<IPapraService>();
        var openAi = Substitute.For<IOpenAiService>();
        var mistral = Substitute.For<IMistralService>();
        var logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<DocumentPipelineService>.Instance;

        var svc = new DocumentPipelineService(settingsSvc, statusSvc, papra, openAi, mistral, logger);
        return (settingsSvc, statusSvc, papra, openAi, mistral, svc);
    }

    [Fact]
    public async Task ProcessAsync_HappyPath_OpenAiOcr_CompletesSuccessfully()
    {
        var (_, status, papra, openAi, _, svc) = Build();
        var job = new ProcessingJob { DocumentId = "doc1", OrganizationId = "org1" };

        papra.GetDocumentInfoAsync("org1", "doc1", Arg.Any<CancellationToken>())
            .Returns(("invoice.pdf", "image/jpeg"));
        papra.GetDocumentFileAsync("org1", "doc1", Arg.Any<CancellationToken>())
            .Returns((byte[])[1, 2, 3]);
        openAi.ExtractTextAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("extracted text");
        openAi.CompleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("Generated Title");
        papra.GetTagsAsync("org1", Arg.Any<CancellationToken>())
            .Returns([]);

        await svc.ProcessAsync(job, CancellationToken.None);

        await papra.Received(1).UpdateDocumentAsync("org1", "doc1", "Generated Title", "extracted text", Arg.Any<CancellationToken>());
        status.Received(1).JobCompleted(Arg.Is<PipelineJobResult>(r =>
            r.Status == JobStatus.Succeeded &&
            r.ExtractedTitle == "Generated Title"));
    }

    [Fact]
    public async Task ProcessAsync_WithMistralKey_UsesMistralForOcr()
    {
        var (_, _, papra, openAi, mistral, svc) = Build(ConfiguredSettings(useMistral: true));
        var job = new ProcessingJob { DocumentId = "doc2", OrganizationId = "org1" };

        papra.GetDocumentInfoAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(("doc.pdf", "application/pdf"));
        papra.GetDocumentFileAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((byte[])[0xFF]);
        mistral.ExtractTextAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("mistral text");
        openAi.CompleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("Mistral-backed Title");
        papra.GetTagsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns([]);

        await svc.ProcessAsync(job, CancellationToken.None);

        await mistral.Received(1).ExtractTextAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await openAi.DidNotReceive().ExtractTextAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_WithMatchingTags_AppliesTagsToDocument()
    {
        var (_, _, papra, openAi, _, svc) = Build();
        var job = new ProcessingJob { DocumentId = "doc3", OrganizationId = "org1" };

        papra.GetDocumentInfoAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(("receipt.pdf", "image/png"));
        papra.GetDocumentFileAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns([1]);
        openAi.ExtractTextAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("content");
        openAi.CompleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("Title", "Finance, Invoice");
        papra.GetTagsAsync("org1", Arg.Any<CancellationToken>())
            .Returns([("tag-finance-id", "Finance"), ("tag-invoice-id", "Invoice"), ("tag-other-id", "Other")]);

        await svc.ProcessAsync(job, CancellationToken.None);

        await papra.Received(1).AddTagToDocumentAsync("org1", "doc3", "tag-finance-id", Arg.Any<CancellationToken>());
        await papra.Received(1).AddTagToDocumentAsync("org1", "doc3", "tag-invoice-id", Arg.Any<CancellationToken>());
        await papra.DidNotReceive().AddTagToDocumentAsync("org1", "doc3", "tag-other-id", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_WithNoAvailableTags_SkipsTagging()
    {
        var (_, _, papra, openAi, _, svc) = Build();
        var job = new ProcessingJob { DocumentId = "doc4", OrganizationId = "org1" };

        papra.GetDocumentInfoAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(("file.pdf", "image/png"));
        papra.GetDocumentFileAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns([1]);
        openAi.ExtractTextAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("text");
        openAi.CompleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("Title");
        papra.GetTagsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns([]);

        await svc.ProcessAsync(job, CancellationToken.None);

        await papra.DidNotReceive().AddTagToDocumentAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_WhenPapraThrows_SetsFailedStatus()
    {
        var (_, status, papra, _, _, svc) = Build();
        var job = new ProcessingJob { DocumentId = "doc5", OrganizationId = "org1" };

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
        var (_, status, papra, _, _, svc) = Build();
        var job = new ProcessingJob { DocumentId = "doc6", OrganizationId = "org1" };
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
        var (_, status, papra, _, _, svc) = Build();
        var job = new ProcessingJob { DocumentId = "doc7", OrganizationId = "org1" };

        papra.GetDocumentInfoAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("boom"));

        await svc.ProcessAsync(job, CancellationToken.None);

        status.Received(1).JobStarted(Arg.Any<PipelineJobResult>());
        status.Received(1).JobCompleted(Arg.Any<PipelineJobResult>());
    }

    [Fact]
    public async Task ProcessAsync_BuildsCorrectDataUrl()
    {
        var (_, _, papra, openAi, _, svc) = Build();
        var job = new ProcessingJob { DocumentId = "doc8", OrganizationId = "org1" };
        var fileBytes = new byte[] { 0xDE, 0xAD, 0xBE };
        var expectedBase64 = Convert.ToBase64String(fileBytes);
        var expectedDataUrl = $"data:image/png;base64,{expectedBase64}";

        papra.GetDocumentInfoAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(("file.png", "image/png"));
        papra.GetDocumentFileAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(fileBytes);
        openAi.ExtractTextAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("text");
        openAi.CompleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("Title");
        papra.GetTagsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns([]);

        await svc.ProcessAsync(job, CancellationToken.None);

        await openAi.Received(1).ExtractTextAsync(expectedDataUrl, "image/png", Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
