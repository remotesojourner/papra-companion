using Microsoft.EntityFrameworkCore;
using Papra.Companion.Data.Entities;
using Papra.Companion.Data.Repositories.Interfaces;

namespace Papra.Companion.Data.Repositories;

public class JobResultRepository(IDbContextFactory<AppDbContext> dbFactory) : IJobResultRepository
{
    public IReadOnlyList<JobResultEntity> GetRecent(int count)
    {
        using var db = dbFactory.CreateDbContext();
        return [.. db.JobResults
            .AsNoTracking()
            .AsEnumerable()
            .OrderByDescending(e => e.StartedAt)
            .Take(count)];
    }

    public async Task AddAsync(JobResultEntity entity)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        db.JobResults.Add(entity);
        await db.SaveChangesAsync();
    }
}
