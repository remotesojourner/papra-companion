namespace Papra.Companion.Models;

public class PipelineSettings
{
    // ── Papra connection ──────────────────────────────────────────────────────
    public string PapraBaseUrl { get; set; } = string.Empty;
    public string PapraApiToken { get; set; } = string.Empty;

    // ── AI services ───────────────────────────────────────────────────────────
    public string MistralApiKey { get; set; } = string.Empty;
    public string OpenAiApiKey { get; set; } = string.Empty;
    public string OpenAiModel { get; set; } = "gpt-4o-mini";

    // ── Prompts ───────────────────────────────────────────────────────────────
    public string TitlePrompt { get; set; } = DefaultTitlePrompt;
    public string TagPrompt { get; set; } = DefaultTagPrompt;
    public string OcrPrompt { get; set; } = DefaultOcrPrompt;

    // ── Computed ──────────────────────────────────────────────────────────────
    public bool UseOpenAiForOcr => string.IsNullOrWhiteSpace(MistralApiKey);
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(PapraBaseUrl) &&
        !string.IsNullOrWhiteSpace(PapraApiToken) &&
        !string.IsNullOrWhiteSpace(OpenAiApiKey);

    // ── Default prompts ───────────────────────────────────────────────────────
    public const string DefaultTitlePrompt =
        """
        I will provide you with the content of a document that has been partially read by OCR (so it may contain errors).
        Your task is to find a suitable document title that I can use as the title in my document management system.
        If the original title is already adding value and not just a technical filename you can use it as extra information to enhance your suggestion.
        Respond only with the title, without any additional information.

        The data will be provided using an XML-like format for clarity:

        <original_title>{{original_title}}</original_title>
        <content>
        {{content}}
        </content>
        """;

    public const string DefaultTagPrompt =
        """
        I will provide you with the content and the title of a document.
        Your task is to select appropriate tags for the document from the list of available tags I will provide. Only select tags from the provided list. Respond only with the selected tags as a comma-separated list, without any additional information.

        The data will be provided using an XML-like format for clarity:

        <available_tags>
        {{available_tags}}
        </available_tags>
        <original_title>{{original_title}}</original_title>
        <content>
        {{content}}
        </content>

        Please concisely select the tags from the list above that best describe the document.
        Be very selective and only choose the most relevant tags since too many tags will make the document less discoverable.
        """;

    public const string DefaultOcrPrompt =
        "Extract and return all the text from this document as plain text, preserving the logical structure. Return only the extracted text without any commentary.";
}
