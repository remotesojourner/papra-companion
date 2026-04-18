using Microsoft.EntityFrameworkCore;
using Papra.Companion.Data.Entities;
using Papra.Companion.Data.Repositories.Interfaces;

namespace Papra.Companion.Data.Repositories;

public class PipelineSettingsRepository(IDbContextFactory<AppDbContext> dbFactory) : IPipelineSettingsRepository
{
    private const int SettingsId = 1;

    public PipelineSettingsEntity? Get()
    {
        using var db = dbFactory.CreateDbContext();
        return db.PipelineSettings.AsNoTracking().FirstOrDefault(x => x.Id == SettingsId);
    }

    public async Task UpsertAsync(PipelineSettingsEntity entity)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var existing = await db.PipelineSettings.FindAsync(SettingsId);
        if (existing is null)
        {
            entity.Id = SettingsId;
            db.PipelineSettings.Add(entity);
        }
        else
        {
            existing.PapraBaseUrl = entity.PapraBaseUrl;
            existing.PapraApiToken = entity.PapraApiToken;
            existing.MistralApiKey = entity.MistralApiKey;
            existing.OpenAiApiKey = entity.OpenAiApiKey;
            existing.OpenAiModel = entity.OpenAiModel;
            existing.TitlePrompt = entity.TitlePrompt;
            existing.TagPrompt = entity.TagPrompt;
            existing.OcrPrompt = entity.OcrPrompt;
        }
        await db.SaveChangesAsync();
    }
}
