using Papra.Companion.Data.Entities;
using Papra.Companion.Data.Repositories.Interfaces;
using Papra.Companion.Models;
using Papra.Companion.Services.Interfaces;

namespace Papra.Companion.Services;

public class PipelineStatusService : IPipelineStatusService
{
    private const int MaxJobs = 20;

    private readonly IJobResultRepository _repository;
    private readonly List<PipelineJobResult> _recentJobs;
    private PipelineJobResult? _currentJob;
    private readonly Lock _lock = new();

    public event Action? OnChanged;

    public PipelineStatusService(IJobResultRepository repository)
    {
        _repository = repository;
        _recentJobs = [.. _repository.GetRecent(MaxJobs).Select(ToModel)];
    }

    public PipelineJobResult? CurrentJob
    {
        get { lock (_lock) { return _currentJob; } }
    }

    public IReadOnlyList<PipelineJobResult> RecentJobs
    {
        get { lock (_lock) { return [.. _recentJobs]; } }
    }

    public void JobStarted(PipelineJobResult job)
    {
        lock (_lock)
        {
            job.Status = JobStatus.Processing;
            _currentJob = job;
        }
        OnChanged?.Invoke();
    }

    public void JobCompleted(PipelineJobResult job)
    {
        lock (_lock)
        {
            _currentJob = null;
            _recentJobs.Insert(0, job);
            if (_recentJobs.Count > MaxJobs)
                _recentJobs.RemoveAt(_recentJobs.Count - 1);
        }

        Task.Run(() => _repository.AddAsync(ToEntity(job)));

        OnChanged?.Invoke();
    }

    private static PipelineJobResult ToModel(JobResultEntity e) => new()
    {
        DocumentId = e.DocumentId,
        OrganizationId = e.OrganizationId,
        StartedAt = e.StartedAt,
        CompletedAt = e.CompletedAt,
        Status = Enum.TryParse<JobStatus>(e.Status, out var s) ? s : JobStatus.Failed,
        ExtractedTitle = e.ExtractedTitle,
        ErrorMessage = e.ErrorMessage
    };

    private static JobResultEntity ToEntity(PipelineJobResult m) => new()
    {
        DocumentId = m.DocumentId,
        OrganizationId = m.OrganizationId,
        StartedAt = m.StartedAt,
        CompletedAt = m.CompletedAt,
        Status = m.Status.ToString(),
        ExtractedTitle = m.ExtractedTitle,
        ErrorMessage = m.ErrorMessage
    };
}
