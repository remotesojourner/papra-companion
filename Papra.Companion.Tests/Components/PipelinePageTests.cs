using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Papra.Companion.Components.Pages;
using Papra.Companion.Models;
using Papra.Companion.Services.Interfaces;

namespace Papra.Companion.Tests.Components;

public class PipelinePageTests : ComponentTestBase
{
    private readonly IPipelineStatusService _statusSvc;
    private readonly ISettingsService _settingsSvc;

    public PipelinePageTests()
    {
        _statusSvc = Substitute.For<IPipelineStatusService>();
        _settingsSvc = Substitute.For<ISettingsService>();
        _settingsSvc.Current.Returns(new PipelineSettings
        {
            PapraBaseUrl = "https://papra.example.com",
            PapraApiToken = "token",
            OpenAiApiKey = "sk-key",
        });
        _statusSvc.CurrentJob.Returns((PipelineJobResult?)null);
        _statusSvc.RecentJobs.Returns([]);

        Services.AddSingleton(_statusSvc);
        Services.AddSingleton(_settingsSvc);
    }

    [Fact]
    public void PipelineWhenNotConfiguredShowsWarningAlert()
    {
        _settingsSvc.Current.Returns(new PipelineSettings());

        var cut = Render<Pipeline>();

        Assert.Contains("Not configured", cut.Markup);
    }

    [Fact]
    public void PipelineWhenConfiguredDoesNotShowWarning()
    {
        var cut = Render<Pipeline>();

        Assert.DoesNotContain("Configure your Papra connection", cut.Markup);
    }

    [Fact]
    public void PipelineWhenIdleShowsIdleState()
    {
        var cut = Render<Pipeline>();

        Assert.Contains("Idle", cut.Markup);
    }

    [Fact]
    public void PipelineWhenJobProcessingShowsProcessingState()
    {
        _statusSvc.CurrentJob.Returns(new PipelineJobResult
        {
            DocumentId = "doc-abc",
            Status = JobStatus.Processing
        });

        var cut = Render<Pipeline>();

        Assert.Contains("Processing", cut.Markup);
        Assert.Contains("doc-abc", cut.Markup);
    }

    [Fact]
    public void PipelineWithRecentJobsShowsJobTable()
    {
        _statusSvc.RecentJobs.Returns(
        [
            new PipelineJobResult
            {
                DocumentId = "doc-xyz",
                Status = JobStatus.Succeeded,
                ExtractedTitle = "My Invoice",
                StartedAt = DateTimeOffset.UtcNow,
                CompletedAt = DateTimeOffset.UtcNow.AddSeconds(3),
            }
        ]);

        var cut = Render<Pipeline>();

        Assert.Contains("doc-xyz", cut.Markup);
        Assert.Contains("My Invoice", cut.Markup);
        Assert.Contains("Succeeded", cut.Markup);
    }

    [Fact]
    public void PipelineWithFailedJobShowsFailedBadge()
    {
        _statusSvc.RecentJobs.Returns(
        [
            new PipelineJobResult
            {
                DocumentId = "doc-bad",
                Status = JobStatus.Failed,
                ErrorMessage = "Something went wrong",
                StartedAt = DateTimeOffset.UtcNow,
            }
        ]);

        var cut = Render<Pipeline>();

        Assert.Contains("Failed", cut.Markup);
    }

    [Fact]
    public void PipelineWithNoJobsShowsEmptyState()
    {
        _statusSvc.RecentJobs.Returns([]);

        var cut = Render<Pipeline>();

        Assert.Contains("No documents processed yet", cut.Markup);
    }

    [Fact]
    public void PipelineShowsCorrectStats()
    {
        _statusSvc.RecentJobs.Returns(
        [
            new PipelineJobResult { DocumentId = "d1", Status = JobStatus.Succeeded, StartedAt = DateTimeOffset.UtcNow },
            new PipelineJobResult { DocumentId = "d2", Status = JobStatus.Succeeded, StartedAt = DateTimeOffset.UtcNow },
            new PipelineJobResult { DocumentId = "d3", Status = JobStatus.Failed,    StartedAt = DateTimeOffset.UtcNow },
        ]);

        var cut = Render<Pipeline>();
        var markup = cut.Markup;

        Assert.Contains(">3<", markup);
        Assert.Contains(">2<", markup);
        Assert.Contains(">1<", markup);
    }

    [Fact]
    public void PipelineDisposesEventHandlerOnDispose()
    {
        var cut = Render<Pipeline>();
        cut.Instance.Dispose();

        _statusSvc.OnChanged += Raise.Event<Action>();
    }
}
