using Papra.Companion.Models;

namespace Papra.Companion.Services.Interfaces;

public interface ISettingsService
{
    PipelineSettings Current { get; }
    event Action? OnChanged;
    void Save(PipelineSettings settings);
}
