namespace Papra.Companion.Services.Interfaces;

public interface IOpenAiService
{
    Task<string> ExtractTextAsync(string dataUrl, string mimeType, string prompt, CancellationToken ct);
    Task<string> CompleteAsync(string prompt, CancellationToken ct);
    Task<string> TestConnectionAsync(string baseUrl, string apiKey, string model, CancellationToken ct);
}
