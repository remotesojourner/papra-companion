namespace Papra.Companion.Services.Interfaces;

public interface IMistralService
{
    Task<string> ExtractTextAsync(string dataUrl, CancellationToken ct);
    Task<string> TestConnectionAsync(string apiKey, CancellationToken ct);
}
