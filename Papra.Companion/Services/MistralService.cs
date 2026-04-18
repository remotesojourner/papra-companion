using System.Net.Http.Headers;
using Papra.Companion.Constants;
using Papra.Companion.Http;
using Papra.Companion.Services.Interfaces;

namespace Papra.Companion.Services;

public class MistralService(ISettingsService settingsService) : IMistralService
{
    public async Task<string> ExtractTextAsync(string dataUrl, CancellationToken ct)
    {
        using var client = new HttpClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", settingsService.Current.MistralApiKey);

        var mimeType = dataUrl.StartsWith("data:") ? dataUrl[5..dataUrl.IndexOf(';')] : string.Empty;
        var isImage = mimeType.StartsWith("image/");

        MistralDocument document = isImage
            ? new MistralImageDocument(ImageUrl: dataUrl)
            : new MistralDocumentUrlDoc(DocumentUrl: dataUrl);

        var request = new MistralOcrRequest(
            Model: MistralConstants.OcrModel,
            Document: document,
            ImageLimit: 0);

        var response = await client.PostAsJsonAsync(MistralConstants.OcrEndpoint, request, ct);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            throw new HttpRequestException(
                $"Mistral OCR request failed with {(int)response.StatusCode}: {errorBody}",
                null, response.StatusCode);
        }

        var result = await response.Content.ReadFromJsonAsync<MistralOcrResponse>(ct);
        return string.Join("\n\n", result!.Pages.Select(p => p.Markdown));
    }

    public async Task<string> TestConnectionAsync(string apiKey, CancellationToken ct)
    {
        using var client = new HttpClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        var response = await client.GetAsync(MistralConstants.ModelsEndpoint, ct);
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"HTTP {(int)response.StatusCode}", null, response.StatusCode);
        return "Connected";
    }
}
