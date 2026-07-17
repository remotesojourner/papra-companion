namespace Papra.Companion.Models;

public class PipelineSettings
{
    // ── Papra connection ──────────────────────────────────────────────────────
    public string PapraBaseUrl { get; set; } = string.Empty;
    public string PapraApiToken { get; set; } = string.Empty;

    // ── AI services ───────────────────────────────────────────────────────────
    public string OpenAiBaseUrl { get; set; } = string.Empty;
    public string OpenAiApiKey { get; set; } = string.Empty;
    public string OpenAiModel { get; set; } = "gpt-4o-mini";

    // ── Prompts ───────────────────────────────────────────────────────────────
    public string TitlePrompt { get; set; } = DefaultTitlePrompt;

    // ── Pipeline behaviour ────────────────────────────────────────────────────
    /// <summary>Seconds to wait after receiving a webhook before processing the document.</summary>
    public int ProcessingDelaySeconds { get; set; } = 0;

    // ── Computed ──────────────────────────────────────────────────────────────
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(PapraBaseUrl) &&
        !string.IsNullOrWhiteSpace(PapraApiToken) &&
        !string.IsNullOrWhiteSpace(OpenAiApiKey);

    // ── Default prompts ───────────────────────────────────────────────────────
    public const string DefaultTitlePrompt =
        """
        I will provide you with the name of a document.
        Your task is to find a suitable document title that I can use as the title in my document management system.
        If the original title is already adding value and not just a technical filename you can use it as a base.
        Respond only with the title, without any additional information.

        The data will be provided using an XML-like format for clarity:

        <original_title>{{original_title}}</original_title>
        """;
}
