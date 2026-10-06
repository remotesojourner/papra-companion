using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
using Papra.Companion.Models;
using Papra.Companion.Services.Interfaces;

namespace Papra.Companion.Tests;

public class WebhookEndpointTests : IClassFixture<TestAppFactory>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly ISettingsService _settingsServiceMock;
    private readonly IPipelineJobChannel _pipelineQueueMock;

    public WebhookEndpointTests(TestAppFactory factory)
    {
        _settingsServiceMock = Substitute.For<ISettingsService>();
        _pipelineQueueMock = Substitute.For<IPipelineJobChannel>();

        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<ISettingsService>();
                services.AddSingleton(_settingsServiceMock);

                services.RemoveAll<IPipelineJobChannel>();
                services.AddSingleton(_pipelineQueueMock);
            });
        });
    }

    [Fact]
    public async Task PostWebhookWhenNotConfiguredReturns503()
    {
        var client = _factory.CreateClient();
        _settingsServiceMock.Current.Returns(new PipelineSettings());

        var payload = new { data = new { organizationId = "org1", documentId = "doc1" } };

        var response = await client.PostAsJsonAsync("/webhook/document", payload, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [Fact]
    public async Task PostWebhookWithInvalidJsonReturns400()
    {
        var client = _factory.CreateClient();
        _settingsServiceMock.Current.Returns(new PipelineSettings 
        { 
            PapraBaseUrl = "https://example.com",
            PapraApiToken = "test",
            OpenAiApiKey = "test"
        });

        var content = new StringContent("invalid json");
        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

        var response = await client.PostAsync("/webhook/document", content, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostWebhookWithMissingDataReturns400()
    {
        var client = _factory.CreateClient();
        _settingsServiceMock.Current.Returns(new PipelineSettings 
        { 
            PapraBaseUrl = "https://example.com",
            PapraApiToken = "test",
            OpenAiApiKey = "test"
        });

        var payload = new { data = new { organizationId = "", documentId = "" } };

        var response = await client.PostAsJsonAsync("/webhook/document", payload, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostWebhookWithValidDataEnqueuesJobAndReturns202()
    {
        var client = _factory.CreateClient();
        _settingsServiceMock.Current.Returns(new PipelineSettings 
        { 
            PapraBaseUrl = "https://example.com",
            PapraApiToken = "test",
            OpenAiApiKey = "test"
        });

        var payload = new { data = new { organizationId = "org-123", documentId = "doc-456" } };

        var response = await client.PostAsJsonAsync("/webhook/document", payload, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        
        await _pipelineQueueMock.Received(1).EnqueueAsync(Arg.Is<ProcessingJob>(j => 
            j!.OrganizationId == "org-123" && j.DocumentId == "doc-456"), CancellationToken.None);
    }
}
