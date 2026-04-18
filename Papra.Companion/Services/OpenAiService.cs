using System.Net.Http.Headers;
using Papra.Companion.Constants;
using Papra.Companion.Http;
using Papra.Companion.Services.Interfaces;

namespace Papra.Companion.Services;

public class OpenAiService(ISettingsService settingsService) : IOpenAiService
{
    private HttpClient CreateClient(bool longRunning = false)
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", settingsService.Current.OpenAiApiKey);
        if (longRunning)
            client.Timeout = System.Threading.Timeout.InfiniteTimeSpan;
        return client;
    }

    public async Task<string> CompleteAsync(string prompt, CancellationToken ct)
    {
        using var client = CreateClient();
        var request = new ChatCompletionRequest(
            Model: settingsService.Current.OpenAiModel,
            Messages: [new ChatRequestMessage(Role: "user", Content: prompt)]);

        var response = await client.PostAsJsonAsync(OpenAiConstants.ChatCompletionsEndpoint, request, ct);
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            throw new HttpRequestException(
                $"OpenAI completion request failed with {(int)response.StatusCode}: {errorBody}",
                null, response.StatusCode);
        }

        var result = await response.Content.ReadFromJsonAsync<ChatCompletionResponse>(ct);
        return result!.Choices[0].Message.Content.Trim();
    }

    public async Task<string> ExtractTextAsync(string dataUrl, string mimeType, string prompt, CancellationToken ct)
    {
        if (mimeType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            return await ExtractTextFromImageAsync(dataUrl, prompt, ct);

        return await ExtractTextFromDocumentAsync(dataUrl, mimeType, prompt, ct);
    }

    private async Task<string> ExtractTextFromImageAsync(string dataUrl, string prompt, CancellationToken ct)
    {
        using var client = CreateClient(longRunning: true);

        var request = new ChatCompletionRequest(
            Model: settingsService.Current.OpenAiModel,
            Messages:
            [
                new ChatRequestMessage(
                    Role: "user",
                    Content: (object[])
                    [
                        TextContentPart.From(prompt),
                        ImageContentPart.From(dataUrl)
                    ])
            ]);

        var response = await client.PostAsJsonAsync(OpenAiConstants.ChatCompletionsEndpoint, request, ct);
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            throw new HttpRequestException(
                $"OpenAI image OCR request failed with {(int)response.StatusCode}: {errorBody}",
                null, response.StatusCode);
        }

        var result = await response.Content.ReadFromJsonAsync<ChatCompletionResponse>(ct);
        return result!.Choices[0].Message.Content.Trim();
    }

    private async Task<string> ExtractTextFromDocumentAsync(string dataUrl, string mimeType, string prompt, CancellationToken ct)
    {
        var extension = mimeType switch
        {
            "application/pdf"                                                          => "pdf",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document" => "docx",
            "application/msword"                                                       => "doc",
            "text/plain"                                                               => "txt",
            _                                                                          => "bin"
        };

        using var client = CreateClient(longRunning: true);

        var request = new ResponsesRequest(
            Model: settingsService.Current.OpenAiModel,
            Input:
            [
                new ResponsesInputItem(
                    Role: "user",
                    Content: (object[])
                    [
                        new ResponsesTextPart(Text: prompt),
                        new ResponsesFilePart(
                            Filename: $"document.{extension}",
                            FileData: dataUrl)
                    ])
            ]);

        var response = await client.PostAsJsonAsync(OpenAiConstants.ResponsesEndpoint, request, ct);
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            throw new HttpRequestException(
                $"OpenAI OCR request failed with {(int)response.StatusCode}: {errorBody}",
                null, response.StatusCode);
        }

        var result = await response.Content.ReadFromJsonAsync<ResponsesResponse>(ct);
        var text = result!.Output
            .Where(o => o.Type == "message")
            .SelectMany(o => o.Content ?? [])
            .Where(c => c.Type == "output_text")
            .Select(c => c.Text)
            .FirstOrDefault();

        return text?.Trim() ?? string.Empty;
    }

    public async Task<string> TestConnectionAsync(string apiKey, string model, CancellationToken ct)
    {
        using var client = new HttpClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        var response = await client.GetAsync(OpenAiConstants.ModelsEndpoint, ct);
        return !response.IsSuccessStatusCode
            ? throw new HttpRequestException($"HTTP {(int)response.StatusCode}", null, response.StatusCode)
            : "Connected";
    }
}
