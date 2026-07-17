using Papra.Companion.Models;

namespace Papra.Companion.Services.Interfaces;

public interface ITitleGenerationService
{
    Task ProcessAsync(ProcessingJob job, CancellationToken ct);
}
