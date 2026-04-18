namespace Papra.Companion.Services.Interfaces;

public interface IPapraService
{
    Task<(string Name, string MimeType)> GetDocumentInfoAsync(string orgId, string docId, CancellationToken ct);
    Task<byte[]> GetDocumentFileAsync(string orgId, string docId, CancellationToken ct);
    Task UpdateDocumentAsync(string orgId, string docId, string name, string content, CancellationToken ct);
    Task<List<(string Id, string Name)>> GetTagsAsync(string orgId, CancellationToken ct);
    Task AddTagToDocumentAsync(string orgId, string docId, string tagId, CancellationToken ct);
    Task<string> TestConnectionAsync(string baseUrl, string apiToken, CancellationToken ct);
}
