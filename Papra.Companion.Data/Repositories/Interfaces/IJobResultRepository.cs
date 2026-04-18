using Papra.Companion.Data.Entities;

namespace Papra.Companion.Data.Repositories.Interfaces;

public interface IJobResultRepository
{
    IReadOnlyList<JobResultEntity> GetRecent(int count);
    Task AddAsync(JobResultEntity entity);
}
