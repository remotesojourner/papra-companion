using Papra.Companion.Data.Entities;
using Papra.Companion.Data.Repositories.Interfaces;
using Papra.Companion.Models;
using Papra.Companion.Services.Interfaces;

namespace Papra.Companion.Services;

public class SettingsService : ISettingsService
{
    private readonly IPipelineSettingsRepository _repository;
    private PipelineSettings _current;
    private readonly Lock _lock = new();

    public event Action? OnChanged;

    public PipelineSettings Current
    {
        get { lock (_lock) { return _current; } }
    }

    public SettingsService(IPipelineSettingsRepository repository)
    {
        _repository = repository;
        _current = ToModel(_repository.Get()) ?? new PipelineSettings();
    }

    public void Save(PipelineSettings settings)
    {
        lock (_lock)
        {
            _current = ToModel(ToEntity(settings))!;
        }

        Task.Run(() => _repository.UpsertAsync(ToEntity(settings)));

        OnChanged?.Invoke();
    }

    private static PipelineSettings? ToModel(PipelineSettingsEntity? e) =>
        e is null ? null : new()
        {
            PapraBaseUrl = e.PapraBaseUrl,
            PapraApiToken = e.PapraApiToken,
            MistralApiKey = e.MistralApiKey,
            OpenAiApiKey = e.OpenAiApiKey,
            OpenAiModel = e.OpenAiModel,
            TitlePrompt = string.IsNullOrWhiteSpace(e.TitlePrompt) ? PipelineSettings.DefaultTitlePrompt : e.TitlePrompt,
            TagPrompt = string.IsNullOrWhiteSpace(e.TagPrompt) ? PipelineSettings.DefaultTagPrompt : e.TagPrompt,
            OcrPrompt = string.IsNullOrWhiteSpace(e.OcrPrompt) ? PipelineSettings.DefaultOcrPrompt : e.OcrPrompt,
        };

    private static PipelineSettingsEntity ToEntity(PipelineSettings m) => new()
    {
        PapraBaseUrl = m.PapraBaseUrl,
        PapraApiToken = m.PapraApiToken,
        MistralApiKey = m.MistralApiKey,
        OpenAiApiKey = m.OpenAiApiKey,
        OpenAiModel = m.OpenAiModel,
        TitlePrompt = m.TitlePrompt,
        TagPrompt = m.TagPrompt,
        OcrPrompt = m.OcrPrompt,
    };
}
