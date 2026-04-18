using System.Text.Json.Serialization;

namespace Papra.Companion.Http;

// ── Requests ────────────────────────────────────────────────────────────────

internal sealed record ChatCompletionRequest(
    [property: JsonPropertyName("model")]    string Model,
    [property: JsonPropertyName("messages")] ChatRequestMessage[] Messages);

internal sealed record ChatRequestMessage(
    [property: JsonPropertyName("role")]    string Role,
    [property: JsonPropertyName("content")] object Content); // string | ContentPart[]

internal abstract record ContentPart(
    [property: JsonPropertyName("type")] string Type);

internal sealed record TextContentPart(
    [property: JsonPropertyName("text")] string Text)
    : ContentPart("text");

internal sealed record ImageContentPart(
    [property: JsonPropertyName("image_url")] ImageUrlValue ImageUrl)
    : ContentPart("image_url");

internal sealed record ImageUrlValue(
    [property: JsonPropertyName("url")] string Url);

// ── Responses ───────────────────────────────────────────────────────────────

internal sealed record ChatCompletionResponse(
    [property: JsonPropertyName("choices")] ChatChoice[] Choices);

internal sealed record ChatChoice(
    [property: JsonPropertyName("message")] ChatResponseMessage Message);

internal sealed record ChatResponseMessage(
    [property: JsonPropertyName("content")] string Content);
