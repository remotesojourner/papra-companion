using System.Text.Json.Serialization;

namespace Papra.Companion.Http;

internal sealed record PapraDocumentResponse(
    [property: JsonPropertyName("document")] PapraDocumentInfo Document);

internal sealed record PapraDocumentInfo(
    [property: JsonPropertyName("name")]     string Name,
    [property: JsonPropertyName("content")]  string? Content);

internal sealed record PapraUpdateDocumentTitleRequest(
    [property: JsonPropertyName("name")] string Name);
