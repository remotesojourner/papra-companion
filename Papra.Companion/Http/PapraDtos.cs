using System.Text.Json.Serialization;

namespace Papra.Companion.Http;

// ── Responses ───────────────────────────────────────────────────────────────

internal sealed record PapraDocumentResponse(
    [property: JsonPropertyName("document")] PapraDocumentInfo Document);

internal sealed record PapraDocumentInfo(
    [property: JsonPropertyName("name")]     string Name,
    [property: JsonPropertyName("mimeType")] string MimeType);

// ── Requests ────────────────────────────────────────────────────────────────

internal sealed record PapraUpdateDocumentTitleRequest(
    [property: JsonPropertyName("name")] string Name);
