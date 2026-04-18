using Papra.Companion.Models;

namespace Papra.Companion.Services.Interfaces;

public interface IPipelineStatusService
{
    PipelineJobResult? CurrentJob { get; }
    IReadOnlyList<PipelineJobResult> RecentJobs { get; }
    event Action? OnChanged;
    void JobStarted(PipelineJobResult job);
    void JobCompleted(PipelineJobResult job);
}
