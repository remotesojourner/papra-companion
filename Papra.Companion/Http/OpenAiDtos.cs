using System.Text.Json.Serialization;

namespace Papra.Companion.Http;

// ── Chat Completions request ─────────────────────────────────────────────────

internal sealed record ChatCompletionRequest(
    [property: JsonPropertyName("model")]    string Model,
    [property: JsonPropertyName("messages")] ChatRequestMessage[] Messages);

internal sealed record ChatRequestMessage(
    [property: JsonPropertyName("role")]    string Role,
    [property: JsonPropertyName("content")] object Content); // string | object[]

// Vision content parts (used when sending images via Chat Completions)
internal sealed record TextContentPart(
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("text")] string Text)
{
    internal static TextContentPart From(string text) => new("text", text);
}

internal sealed record ImageContentPart(
    [property: JsonPropertyName("type")]      string Type,
    [property: JsonPropertyName("image_url")] ImageUrlValue ImageUrl)
{
    internal static ImageContentPart From(string dataUrl) => new("image_url", new ImageUrlValue(dataUrl));
}

internal sealed record ImageUrlValue(
    [property: JsonPropertyName("url")] string Url);

// ── Responses API (file-based OCR) ───────────────────────────────────────────

internal sealed record ResponsesRequest(
    [property: JsonPropertyName("model")]  string Model,
    [property: JsonPropertyName("input")] ResponsesInputItem[] Input);

internal sealed record ResponsesInputItem(
    [property: JsonPropertyName("role")]    string Role,
    [property: JsonPropertyName("content")] object[] Content); // ResponsesContentPart[]

internal abstract record ResponsesContentPart(
    [property: JsonPropertyName("type")] string Type);

internal sealed record ResponsesTextPart(
    [property: JsonPropertyName("text")] string Text)
    : ResponsesContentPart("input_text");

// Inline file — filename and file_data sit directly on the content part (no wrapper object)
internal sealed record ResponsesFilePart(
    [property: JsonPropertyName("filename")]  string Filename,
    [property: JsonPropertyName("file_data")] string FileData)
    : ResponsesContentPart("input_file");

internal sealed record ResponsesResponse(
    [property: JsonPropertyName("output")] ResponsesOutputItem[] Output);

internal sealed record ResponsesOutputItem(
    [property: JsonPropertyName("type")]    string Type,
    [property: JsonPropertyName("content")] ResponsesOutputContent[]? Content);

internal sealed record ResponsesOutputContent(
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("text")] string? Text);

// ── Chat Completions response ────────────────────────────────────────────────

internal sealed record ChatCompletionResponse(
    [property: JsonPropertyName("choices")] ChatChoice[] Choices);

internal sealed record ChatChoice(
    [property: JsonPropertyName("message")] ChatResponseMessage Message);

internal sealed record ChatResponseMessage(
    [property: JsonPropertyName("content")] string Content);
