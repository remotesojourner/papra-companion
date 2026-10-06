namespace Papra.Companion.Data.Entities;

public class PipelineSettingsEntity
{
    public int Id { get; set; } = 1;
    public string PapraBaseUrl { get; set; } = string.Empty;
    public string PapraApiToken { get; set; } = string.Empty;
    public string OpenAiBaseUrl { get; set; } = string.Empty;
    public string OpenAiApiKey { get; set; } = string.Empty;
    public string OpenAiModel { get; set; } = "gpt-4o-mini";
    public string TitlePrompt { get; set; } = string.Empty;
    public int ProcessingDelaySeconds { get; set; }
}
