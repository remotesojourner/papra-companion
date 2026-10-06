using Microsoft.EntityFrameworkCore;
using Papra.Companion.Data;
using Papra.Companion.Data.Entities;
using Papra.Companion.Data.Repositories;

namespace Papra.Companion.Tests.Repositories;

public class JobResultRepositoryTests
{
    private static TestDbContextFactory CreateFactory(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new TestDbContextFactory(options);
    }

    [Fact]
    public async Task AddAsyncThenGetRecentReturnsAddedEntity()
    {
        var factory = CreateFactory(nameof(AddAsyncThenGetRecentReturnsAddedEntity));
        var repo = new JobResultRepository(factory);
        var entity = new JobResultEntity
        {
            DocumentId = "doc1",
            OrganizationId = "org1",
            Status = "Succeeded",
            StartedAt = DateTimeOffset.UtcNow,
        };

        await repo.AddAsync(entity);
        var results = repo.GetRecent(10);

        Assert.Single(results);
        Assert.Equal("doc1", results[0].DocumentId);
        Assert.Equal("Succeeded", results[0].Status);
    }

    [Fact]
    public async Task GetRecentReturnsInDescendingStartedAtOrder()
    {
        var factory = CreateFactory(nameof(GetRecentReturnsInDescendingStartedAtOrder));
        var repo = new JobResultRepository(factory);
        var now = DateTimeOffset.UtcNow;

        await repo.AddAsync(new JobResultEntity { DocumentId = "old", OrganizationId = "o", Status = "Succeeded", StartedAt = now.AddMinutes(-10) });
        await repo.AddAsync(new JobResultEntity { DocumentId = "new", OrganizationId = "o", Status = "Succeeded", StartedAt = now });

        var results = repo.GetRecent(10);

        Assert.Equal("new", results[0].DocumentId);
        Assert.Equal("old", results[1].DocumentId);
    }

    [Fact]
    public async Task GetRecentRespectsCountLimit()
    {
        var factory = CreateFactory(nameof(GetRecentRespectsCountLimit));
        var repo = new JobResultRepository(factory);
        var now = DateTimeOffset.UtcNow;

        for (var i = 0; i < 10; i++)
            await repo.AddAsync(new JobResultEntity
            {
                DocumentId = $"doc{i}",
                OrganizationId = "o",
                Status = "Succeeded",
                StartedAt = now.AddSeconds(i),
            });

        var results = repo.GetRecent(3);

        Assert.Equal(3, results.Count);
    }

    [Fact]
    public void GetRecentWhenEmptyReturnsEmptyList()
    {
        var factory = CreateFactory(nameof(GetRecentWhenEmptyReturnsEmptyList));
        var repo = new JobResultRepository(factory);

        var results = repo.GetRecent(10);

        Assert.Empty(results);
    }
}
