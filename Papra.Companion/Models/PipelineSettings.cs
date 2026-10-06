namespace Papra.Companion.Models;

public class PipelineSettings
{
    public string PapraBaseUrl { get; set; } = string.Empty;
    public string PapraApiToken { get; set; } = string.Empty;

    public string OpenAiBaseUrl { get; set; } = string.Empty;
    public string OpenAiApiKey { get; set; } = string.Empty;
    public string OpenAiModel { get; set; } = "gpt-4o-mini";

    public string TitlePrompt { get; set; } = DefaultTitlePrompt;

    public int ProcessingDelaySeconds { get; set; }

    public bool IsPapraConfigured =>
        !string.IsNullOrWhiteSpace(PapraBaseUrl) &&
        !string.IsNullOrWhiteSpace(PapraApiToken);

    public bool IsAiConfigured => !string.IsNullOrWhiteSpace(OpenAiApiKey);

    public bool IsConfigured => IsPapraConfigured && IsAiConfigured;

    public PipelineSettings Clone() => (PipelineSettings)MemberwiseClone();

    public const string DefaultTitlePrompt =
        """
        I will provide you with the name and extracted text content of a document.
        Your task is to find a suitable document title that I can use as the title in my document management system.
        If the original title is already adding value and not just a technical filename you can use it as a base.
        Respond only with the title, without any additional information.

        The data will be provided using an XML-like format for clarity:

        <original_title>{{original_title}}</original_title>
        <content>
        {{content}}
        </content>
        """;
}
