using System.Text.Json.Serialization;

namespace Papra.Companion.Http;

// ── Responses ───────────────────────────────────────────────────────────────

internal sealed record PapraDocumentResponse(
    [property: JsonPropertyName("document")] PapraDocumentInfo Document);

internal sealed record PapraDocumentInfo(
    [property: JsonPropertyName("name")]     string Name,
    [property: JsonPropertyName("mimeType")] string MimeType);

internal sealed record PapraTagsResponse(
    [property: JsonPropertyName("tags")] PapraTag[] Tags);

internal sealed record PapraTag(
    [property: JsonPropertyName("id")]   string Id,
    [property: JsonPropertyName("name")] string Name);

// ── Requests ────────────────────────────────────────────────────────────────

internal sealed record PapraUpdateDocumentRequest(
    [property: JsonPropertyName("name")]    string Name,
    [property: JsonPropertyName("content")] string Content);

internal sealed record PapraAddTagRequest(
    [property: JsonPropertyName("tagId")] string TagId);
