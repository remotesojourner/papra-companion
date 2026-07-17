using System.Net.Http.Headers;
using Papra.Companion.Constants;
using Papra.Companion.Http;
using Papra.Companion.Services.Interfaces;

namespace Papra.Companion.Services;

public class PapraService(ISettingsService settingsService) : IPapraService
{
    private HttpClient CreateClient()
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", settingsService.Current.PapraApiToken);
        return client;
    }

    private string BaseUrl => settingsService.Current.PapraBaseUrl.TrimEnd('/');

    public async Task<(string Name, string? Content)> GetDocumentInfoAsync(string orgId, string docId, CancellationToken ct)
    {
        using var client = CreateClient();
        var response = await client.GetAsync($"{BaseUrl}{string.Format(PapraConstants.DocumentsRoute, orgId, docId)}", ct);
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            throw new HttpRequestException(
                $"Papra get document info failed with {(int)response.StatusCode}: {errorBody}",
                null, response.StatusCode);
        }

        var result = await response.Content.ReadFromJsonAsync<PapraDocumentResponse>(ct);
        return (result!.Document.Name, result.Document.Content);
    }

    public async Task UpdateDocumentTitleAsync(string orgId, string docId, string name, CancellationToken ct)
    {
        using var client = CreateClient();
        var request = new HttpRequestMessage(new HttpMethod("PATCH"), $"{BaseUrl}{string.Format(PapraConstants.DocumentsRoute, orgId, docId)}")
        {
            Content = JsonContent.Create(new PapraUpdateDocumentTitleRequest(Name: name))
        };
        var response = await client.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            throw new HttpRequestException(
                $"Papra update document title failed with {(int)response.StatusCode}: {errorBody}",
                null, response.StatusCode);
        }
    }

    public async Task<string> TestConnectionAsync(string baseUrl, string apiToken, CancellationToken ct)
    {
        using var client = new HttpClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiToken);
        var response = await client.GetAsync($"{baseUrl.TrimEnd('/')}{PapraConstants.OrganizationsRoute}", ct);
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"HTTP {(int)response.StatusCode}", null, response.StatusCode);
        return "Connected";
    }
}
