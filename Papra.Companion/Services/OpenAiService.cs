using System.Net.Http.Headers;
using System.Net.Http.Json;
using Papra.Companion.Constants;
using Papra.Companion.Http;
using Papra.Companion.Services.Interfaces;

namespace Papra.Companion.Services;

public class OpenAiService(ISettingsService settingsService) : IOpenAiService
{
    private HttpClient CreateClient()
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", settingsService.Current.OpenAiApiKey);
        return client;
    }

    public async Task<string> CompleteAsync(string prompt, CancellationToken ct)
    {
        using var client = CreateClient();
        var request = new ChatCompletionRequest(
            Model: settingsService.Current.OpenAiModel,
            Messages: [new ChatRequestMessage(Role: "user", Content: prompt)]);

        var response = await client.PostAsJsonAsync(OpenAiConstants.ChatCompletionsEndpoint, request, ct);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<ChatCompletionResponse>(ct);
        return result!.Choices[0].Message.Content.Trim();
    }

    public async Task<string> ExtractTextFromImageAsync(string dataUrl, string prompt, CancellationToken ct)
    {
        using var client = CreateClient();
        var request = new ChatCompletionRequest(
            Model: settingsService.Current.OpenAiModel,
            Messages:
            [
                new ChatRequestMessage(
                    Role: "user",
                    Content: (ContentPart[])
                    [
                        new TextContentPart(Text: prompt),
                        new ImageContentPart(ImageUrl: new ImageUrlValue(Url: dataUrl))
                    ])
            ]);

        var response = await client.PostAsJsonAsync(OpenAiConstants.ChatCompletionsEndpoint, request, ct);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<ChatCompletionResponse>(ct);
        return result!.Choices[0].Message.Content.Trim();
    }

    public async Task<string> TestConnectionAsync(string apiKey, string model, CancellationToken ct)
    {
        using var client = new HttpClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        var response = await client.GetAsync(OpenAiConstants.ModelsEndpoint, ct);
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"HTTP {(int)response.StatusCode}", null, response.StatusCode);
        return "Connected";
    }
}
