using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
using Papra.Companion.Models;
using Papra.Companion.Services.Interfaces;

namespace Papra.Companion.Tests;

public class WebhookEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly ISettingsService _settingsServiceMock;
    private readonly IPipelineQueue _pipelineQueueMock;

    public WebhookEndpointTests(WebApplicationFactory<Program> factory)
    {
        _settingsServiceMock = Substitute.For<ISettingsService>();
        _pipelineQueueMock = Substitute.For<IPipelineQueue>();

        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<ISettingsService>();
                services.AddSingleton(_settingsServiceMock);

                services.RemoveAll<IPipelineQueue>();
                services.AddSingleton(_pipelineQueueMock);
            });
        });
    }

    [Fact]
    public async Task PostWebhook_WhenNotConfigured_Returns503()
    {
        // Arrange
        var client = _factory.CreateClient();
        _settingsServiceMock.Current.Returns(new PipelineSettings()); // IsConfigured is false

        var payload = new { data = new { organizationId = "org1", documentId = "doc1" } };

        // Act
        var response = await client.PostAsJsonAsync("/webhook/document", payload, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [Fact]
    public async Task PostWebhook_WithInvalidJson_Returns400()
    {
        // Arrange
        var client = _factory.CreateClient();
        _settingsServiceMock.Current.Returns(new PipelineSettings 
        { 
            PapraBaseUrl = "https://example.com",
            PapraApiToken = "test",
            OpenAiApiKey = "test"
        }); // IsConfigured is true

        var content = new StringContent("invalid json");
        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

        // Act
        var response = await client.PostAsync("/webhook/document", content, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostWebhook_WithMissingData_Returns400()
    {
        // Arrange
        var client = _factory.CreateClient();
        _settingsServiceMock.Current.Returns(new PipelineSettings 
        { 
            PapraBaseUrl = "https://example.com",
            PapraApiToken = "test",
            OpenAiApiKey = "test"
        }); // IsConfigured is true

        var payload = new { data = new { organizationId = "", documentId = "" } };

        // Act
        var response = await client.PostAsJsonAsync("/webhook/document", payload, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostWebhook_WithValidData_EnqueuesJobAndReturns202()
    {
        // Arrange
        var client = _factory.CreateClient();
        _settingsServiceMock.Current.Returns(new PipelineSettings 
        { 
            PapraBaseUrl = "https://example.com",
            PapraApiToken = "test",
            OpenAiApiKey = "test"
        }); // IsConfigured is true

        var payload = new { data = new { organizationId = "org-123", documentId = "doc-456" } };

        // Act
        var response = await client.PostAsJsonAsync("/webhook/document", payload, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        
        await _pipelineQueueMock.Received(1).EnqueueAsync(Arg.Is<ProcessingJob>(j => 
            j.OrganizationId == "org-123" && j.DocumentId == "doc-456"), CancellationToken.None);
    }
}
