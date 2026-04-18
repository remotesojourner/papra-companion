using Papra.Companion.Models;

namespace Papra.Companion.Services.Interfaces;

public interface IDocumentPipelineService
{
    Task ProcessAsync(ProcessingJob job, CancellationToken ct);
}
