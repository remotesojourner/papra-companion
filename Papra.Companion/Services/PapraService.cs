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

    public async Task<(string Name, string MimeType)> GetDocumentInfoAsync(string orgId, string docId, CancellationToken ct)
    {
        using var client = CreateClient();
        var response = await client.GetAsync($"{BaseUrl}{string.Format(PapraConstants.DocumentsRoute, orgId, docId)}", ct);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<PapraDocumentResponse>(ct);
        return (result!.Document.Name, result.Document.MimeType);
    }

    public async Task<byte[]> GetDocumentFileAsync(string orgId, string docId, CancellationToken ct)
    {
        using var client = CreateClient();
        var response = await client.GetAsync($"{BaseUrl}{string.Format(PapraConstants.DocumentFileRoute, orgId, docId)}", ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync(ct);
    }

    public async Task UpdateDocumentAsync(string orgId, string docId, string name, string content, CancellationToken ct)
    {
        using var client = CreateClient();
        var request = new HttpRequestMessage(new HttpMethod("PATCH"), $"{BaseUrl}{string.Format(PapraConstants.DocumentsRoute, orgId, docId)}")
        {
            Content = JsonContent.Create(new PapraUpdateDocumentRequest(Name: name, Content: content))
        };
        var response = await client.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task<List<(string Id, string Name)>> GetTagsAsync(string orgId, CancellationToken ct)
    {
        using var client = CreateClient();
        var response = await client.GetAsync($"{BaseUrl}{string.Format(PapraConstants.TagsRoute, orgId)}", ct);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<PapraTagsResponse>(ct);
        return [.. result!.Tags.Select(t => (t.Id, t.Name))];
    }

    public async Task AddTagToDocumentAsync(string orgId, string docId, string tagId, CancellationToken ct)
    {
        using var client = CreateClient();
        var response = await client.PostAsJsonAsync(
            $"{BaseUrl}{string.Format(PapraConstants.DocumentTagsRoute, orgId, docId)}",
            new PapraAddTagRequest(TagId: tagId),
            ct);
        response.EnsureSuccessStatusCode();
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
