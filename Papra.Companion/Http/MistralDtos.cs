using System.Text.Json.Serialization;

namespace Papra.Companion.Http;

// ── Requests ────────────────────────────────────────────────────────────────

internal sealed record MistralOcrRequest(
    [property: JsonPropertyName("model")]       string Model,
    [property: JsonPropertyName("document")]    MistralDocument Document,
    [property: JsonPropertyName("image_limit")] int ImageLimit);

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(MistralImageDocument),    typeDiscriminator: "image_url")]
[JsonDerivedType(typeof(MistralDocumentUrlDoc),   typeDiscriminator: "document_url")]
internal abstract record MistralDocument();

internal sealed record MistralImageDocument(
    [property: JsonPropertyName("image_url")] string ImageUrl)
    : MistralDocument();

internal sealed record MistralDocumentUrlDoc(
    [property: JsonPropertyName("document_url")] string DocumentUrl)
    : MistralDocument();

// ── Responses ───────────────────────────────────────────────────────────────

internal sealed record MistralOcrResponse(
    [property: JsonPropertyName("pages")] MistralOcrPage[] Pages);

internal sealed record MistralOcrPage(
    [property: JsonPropertyName("markdown")] string Markdown);
