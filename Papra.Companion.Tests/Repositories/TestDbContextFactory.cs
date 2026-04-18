using Microsoft.EntityFrameworkCore;
using Papra.Companion.Data;

namespace Papra.Companion.Tests.Repositories;

/// <summary>
/// Simple IDbContextFactory implementation for tests, backed by EF Core In-Memory database.
/// </summary>
internal sealed class TestDbContextFactory(DbContextOptions<AppDbContext> options)
    : IDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext() => new(options);
    public Task<AppDbContext> CreateDbContextAsync(CancellationToken _ = default)
        => Task.FromResult(new AppDbContext(options));
}
