using NSubstitute;
using Papra.Companion.Data.Entities;
using Papra.Companion.Data.Repositories.Interfaces;
using Papra.Companion.Models;
using Papra.Companion.Services;

namespace Papra.Companion.Tests.Services;

public class PipelineStatusServiceTests
{
    private static IJobResultRepository EmptyRepository()
    {
        var repo = Substitute.For<IJobResultRepository>();
        repo.GetRecent(Arg.Any<int>()).Returns([]);
        return repo;
    }

    [Fact]
    public void CurrentJob_InitiallyNull()
    {
        var service = new PipelineStatusService(EmptyRepository());
        Assert.Null(service.CurrentJob);
    }

    [Fact]
    public void RecentJobs_InitiallyEmpty()
    {
        var service = new PipelineStatusService(EmptyRepository());
        Assert.Empty(service.RecentJobs);
    }

    [Fact]
    public void JobStarted_SetsCurrentJobAndStatus()
    {
        var service = new PipelineStatusService(EmptyRepository());
        var job = new PipelineJobResult { DocumentId = "doc1", OrganizationId = "org1" };

        service.JobStarted(job);

        Assert.Equal(job, service.CurrentJob);
        Assert.Equal(JobStatus.Processing, service.CurrentJob!.Status);
    }

    [Fact]
    public void JobStarted_RaisesOnChangedEvent()
    {
        var service = new PipelineStatusService(EmptyRepository());
        var raised = false;
        service.OnChanged += () => raised = true;

        service.JobStarted(new PipelineJobResult());

        Assert.True(raised);
    }

    [Fact]
    public void JobCompleted_ClearsCurrentJob()
    {
        var service = new PipelineStatusService(EmptyRepository());
        var job = new PipelineJobResult { DocumentId = "doc1" };
        service.JobStarted(job);

        service.JobCompleted(job);

        Assert.Null(service.CurrentJob);
    }

    [Fact]
    public void JobCompleted_AddsToRecentJobs()
    {
        var service = new PipelineStatusService(EmptyRepository());
        var job = new PipelineJobResult { DocumentId = "doc1" };

        service.JobCompleted(job);

        Assert.Single(service.RecentJobs);
        Assert.Equal("doc1", service.RecentJobs[0].DocumentId);
    }

    [Fact]
    public void JobCompleted_InsertsAtFront()
    {
        var service = new PipelineStatusService(EmptyRepository());
        var first = new PipelineJobResult { DocumentId = "first" };
        var second = new PipelineJobResult { DocumentId = "second" };

        service.JobCompleted(first);
        service.JobCompleted(second);

        Assert.Equal("second", service.RecentJobs[0].DocumentId);
        Assert.Equal("first", service.RecentJobs[1].DocumentId);
    }

    [Fact]
    public void JobCompleted_CapsAtHundredJobs()
    {
        var service = new PipelineStatusService(EmptyRepository());

        for (var i = 0; i < 125; i++)
            service.JobCompleted(new PipelineJobResult { DocumentId = $"doc{i}" });

        Assert.Equal(100, service.RecentJobs.Count);
    }

    [Fact]
    public void RecentJobs_LoadedFromRepositoryOnConstruction()
    {
        var repo = Substitute.For<IJobResultRepository>();
        repo.GetRecent(Arg.Any<int>()).Returns(
        [
            new JobResultEntity { DocumentId = "doc-from-db", OrganizationId = "org1", Status = "Succeeded" }
        ]);

        var service = new PipelineStatusService(repo);

        Assert.Single(service.RecentJobs);
        Assert.Equal("doc-from-db", service.RecentJobs[0].DocumentId);
        Assert.Equal(JobStatus.Succeeded, service.RecentJobs[0].Status);
    }

    [Fact]
    public void JobCompleted_RaisesOnChangedEvent()
    {
        var service = new PipelineStatusService(EmptyRepository());
        var raised = false;
        service.OnChanged += () => raised = true;

        service.JobCompleted(new PipelineJobResult());

        Assert.True(raised);
    }
}
