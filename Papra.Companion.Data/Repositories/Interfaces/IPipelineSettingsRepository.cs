using Papra.Companion.Data.Entities;

namespace Papra.Companion.Data.Repositories.Interfaces;

public interface IPipelineSettingsRepository
{
    PipelineSettingsEntity? Get();
    Task UpsertAsync(PipelineSettingsEntity entity);
}
