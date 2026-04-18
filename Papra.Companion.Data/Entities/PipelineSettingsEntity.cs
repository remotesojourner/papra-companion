namespace Papra.Companion.Data.Entities;

/// <summary>Single-row settings table — always Id = 1.</summary>
public class PipelineSettingsEntity
{
    public int Id { get; set; } = 1;
    public string PapraBaseUrl { get; set; } = string.Empty;
    public string PapraApiToken { get; set; } = string.Empty;
    public string MistralApiKey { get; set; } = string.Empty;
    public string OpenAiApiKey { get; set; } = string.Empty;
    public string OpenAiModel { get; set; } = "gpt-4o-mini";
    public string TitlePrompt { get; set; } = string.Empty;
    public string TagPrompt { get; set; } = string.Empty;
    public string OcrPrompt { get; set; } = string.Empty;
}
