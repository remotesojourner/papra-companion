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
            PapraBaseUrl           = e.PapraBaseUrl,
            PapraApiToken          = e.PapraApiToken,
            OpenAiBaseUrl          = e.OpenAiBaseUrl,
            OpenAiApiKey           = e.OpenAiApiKey,
            OpenAiModel            = e.OpenAiModel,
            TitlePrompt            = string.IsNullOrWhiteSpace(e.TitlePrompt) ? PipelineSettings.DefaultTitlePrompt : e.TitlePrompt,
            ProcessingDelaySeconds = e.ProcessingDelaySeconds,
        };

    private static PipelineSettingsEntity ToEntity(PipelineSettings m) => new()
    {
        PapraBaseUrl           = m.PapraBaseUrl,
        PapraApiToken          = m.PapraApiToken,
        OpenAiBaseUrl          = m.OpenAiBaseUrl,
        OpenAiApiKey           = m.OpenAiApiKey,
        OpenAiModel            = m.OpenAiModel,
        TitlePrompt            = m.TitlePrompt,
        ProcessingDelaySeconds = m.ProcessingDelaySeconds,
    };
}
