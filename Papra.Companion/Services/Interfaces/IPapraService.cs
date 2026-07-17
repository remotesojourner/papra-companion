namespace Papra.Companion.Services.Interfaces;

public interface IPapraService
{
    Task<(string Name, string? Content)> GetDocumentInfoAsync(string orgId, string docId, CancellationToken ct);
    Task UpdateDocumentTitleAsync(string orgId, string docId, string name, CancellationToken ct);
    Task<string> TestConnectionAsync(string baseUrl, string apiToken, CancellationToken ct);
}
