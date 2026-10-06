using Papra.Companion.Data.Entities;

namespace Papra.Companion.Data.Repositories.Interfaces;

public interface IPipelineSettingsRepository
{
    PipelineSettingsEntity? Find();
    Task UpsertAsync(PipelineSettingsEntity entity);
}
