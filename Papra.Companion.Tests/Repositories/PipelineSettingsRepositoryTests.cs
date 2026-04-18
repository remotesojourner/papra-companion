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
    public void Get_WhenEmpty_ReturnsNull()
    {
        var repo = new PipelineSettingsRepository(CreateFactory(nameof(Get_WhenEmpty_ReturnsNull)));

        Assert.Null(repo.Get());
    }

    [Fact]
    public async Task UpsertAsync_WhenNoExisting_InsertsNewRow()
    {
        var repo = new PipelineSettingsRepository(CreateFactory(nameof(UpsertAsync_WhenNoExisting_InsertsNewRow)));
        var entity = new PipelineSettingsEntity
        {
            PapraBaseUrl = "https://papra.example.com",
            OpenAiApiKey = "sk-key",
            OpenAiModel = "gpt-4o",
        };

        await repo.UpsertAsync(entity);
        var result = repo.Get();

        Assert.NotNull(result);
        Assert.Equal("https://papra.example.com", result.PapraBaseUrl);
        Assert.Equal("sk-key", result.OpenAiApiKey);
        Assert.Equal("gpt-4o", result.OpenAiModel);
    }

    [Fact]
    public async Task UpsertAsync_WhenExisting_UpdatesAllFields()
    {
        var factory = CreateFactory(nameof(UpsertAsync_WhenExisting_UpdatesAllFields));
        var repo = new PipelineSettingsRepository(factory);

        await repo.UpsertAsync(new PipelineSettingsEntity
        {
            PapraBaseUrl = "https://original.com",
            OpenAiModel = "gpt-4o-mini",
        });

        await repo.UpsertAsync(new PipelineSettingsEntity
        {
            PapraBaseUrl = "https://updated.com",
            PapraApiToken = "new-token",
            OpenAiApiKey = "sk-updated",
            OpenAiModel = "gpt-4o",
            MistralApiKey = "mist",
            TitlePrompt = "new title prompt",
            TagPrompt = "new tag prompt",
            OcrPrompt = "new ocr prompt",
        });

        var result = repo.Get()!;
        Assert.Equal("https://updated.com", result.PapraBaseUrl);
        Assert.Equal("new-token", result.PapraApiToken);
        Assert.Equal("sk-updated", result.OpenAiApiKey);
        Assert.Equal("gpt-4o", result.OpenAiModel);
        Assert.Equal("mist", result.MistralApiKey);
        Assert.Equal("new title prompt", result.TitlePrompt);
    }

    [Fact]
    public async Task UpsertAsync_CalledMultipleTimes_OnlyOneRowExists()
    {
        var factory = CreateFactory(nameof(UpsertAsync_CalledMultipleTimes_OnlyOneRowExists));
        var repo = new PipelineSettingsRepository(factory);

        await repo.UpsertAsync(new PipelineSettingsEntity { PapraBaseUrl = "first" });
        await repo.UpsertAsync(new PipelineSettingsEntity { PapraBaseUrl = "second" });
        await repo.UpsertAsync(new PipelineSettingsEntity { PapraBaseUrl = "third" });

        await using var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        Assert.Equal(1, await db.PipelineSettings.CountAsync(TestContext.Current.CancellationToken));
    }
}
