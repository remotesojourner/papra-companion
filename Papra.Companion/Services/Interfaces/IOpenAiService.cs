namespace Papra.Companion.Services.Interfaces;

public interface IOpenAiService
{
    Task<string> ExtractTextFromImageAsync(string dataUrl, string prompt, CancellationToken ct);
    Task<string> CompleteAsync(string prompt, CancellationToken ct);
    Task<string> TestConnectionAsync(string apiKey, string model, CancellationToken ct);
}
