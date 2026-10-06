using Microsoft.EntityFrameworkCore;
using Papra.Companion.Data;
using Papra.Companion.Data.Entities;
using Papra.Companion.Data.Repositories;

namespace Papra.Companion.Tests.Repositories;

public class PipelineSettingsRepositoryTests
{
    private static TestDbContextFactory CreateFactory(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new TestDbContextFactory(options);
    }

    [Fact]
    public void GetWhenEmptyReturnsNull()
    {
        var repo = new PipelineSettingsRepository(CreateFactory(nameof(GetWhenEmptyReturnsNull)));

        Assert.Null(repo.Find());
    }

    [Fact]
    public async Task UpsertAsyncWhenNoExistingInsertsNewRow()
    {
        var repo = new PipelineSettingsRepository(CreateFactory(nameof(UpsertAsyncWhenNoExistingInsertsNewRow)));
        var entity = new PipelineSettingsEntity
        {
            PapraBaseUrl = "https://papra.example.com",
            OpenAiBaseUrl = "http://localhost:11434/v1/",
            OpenAiApiKey = "sk-key",
            OpenAiModel = "gpt-4o",
        };

        await repo.UpsertAsync(entity);
        var result = repo.Find();

        Assert.NotNull(result);
        Assert.Equal("https://papra.example.com", result.PapraBaseUrl);
        Assert.Equal("http://localhost:11434/v1/", result.OpenAiBaseUrl);
        Assert.Equal("sk-key", result.OpenAiApiKey);
        Assert.Equal("gpt-4o", result.OpenAiModel);
    }

    [Fact]
    public async Task UpsertAsyncWhenExistingUpdatesAllFields()
    {
        var factory = CreateFactory(nameof(UpsertAsyncWhenExistingUpdatesAllFields));
        var repo = new PipelineSettingsRepository(factory);

        await repo.UpsertAsync(new PipelineSettingsEntity
        {
            PapraBaseUrl = "https://original.com",
            OpenAiModel = "gpt-4o-mini",
        });

        await repo.UpsertAsync(new PipelineSettingsEntity
        {
            PapraBaseUrl  = "https://updated.com",
            PapraApiToken = "new-token",
            OpenAiBaseUrl = "http://localhost:11434/v1/",
            OpenAiApiKey  = "sk-updated",
            OpenAiModel   = "gpt-4o",
            TitlePrompt   = "new title prompt",
            ProcessingDelaySeconds = 15,
        });

        var result = repo.Find()!;
        Assert.Equal("https://updated.com", result.PapraBaseUrl);
        Assert.Equal("new-token", result.PapraApiToken);
        Assert.Equal("http://localhost:11434/v1/", result.OpenAiBaseUrl);
        Assert.Equal("sk-updated", result.OpenAiApiKey);
        Assert.Equal("gpt-4o", result.OpenAiModel);
        Assert.Equal("new title prompt", result.TitlePrompt);
        Assert.Equal(15, result.ProcessingDelaySeconds);
    }

    [Fact]
    public async Task UpsertAsyncCalledMultipleTimesOnlyOneRowExists()
    {
        var factory = CreateFactory(nameof(UpsertAsyncCalledMultipleTimesOnlyOneRowExists));
        var repo = new PipelineSettingsRepository(factory);

        await repo.UpsertAsync(new PipelineSettingsEntity { PapraBaseUrl = "first" });
        await repo.UpsertAsync(new PipelineSettingsEntity { PapraBaseUrl = "second" });
        await repo.UpsertAsync(new PipelineSettingsEntity { PapraBaseUrl = "third" });

        await using var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        Assert.Equal(1, await db.PipelineSettings.CountAsync(TestContext.Current.CancellationToken));
    }
}
