using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
using Papra.Companion.Models;
using Papra.Companion.Services.Interfaces;
using Papra.Companion.Data.Repositories.Interfaces;

namespace Papra.Companion.Tests;

public class StatsEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly IPipelineStatusService _pipelineStatusServiceMock;
    private readonly IEmailAttachmentLogRepository _emailAttachmentLogRepositoryMock;

    public StatsEndpointTests(WebApplicationFactory<Program> factory)
    {
        _pipelineStatusServiceMock = Substitute.For<IPipelineStatusService>();
        _emailAttachmentLogRepositoryMock = Substitute.For<IEmailAttachmentLogRepository>();

        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IPipelineStatusService>();
                services.AddSingleton(_pipelineStatusServiceMock);

                services.RemoveAll<IEmailAttachmentLogRepository>();
                services.AddSingleton(_emailAttachmentLogRepositoryMock);
            });
        });
    }

    [Fact]
    public async Task GetStats_ReturnsExpectedData()
    {
        // Arrange
        var client = _factory.CreateClient();

        _pipelineStatusServiceMock.RecentJobs.Returns(
        [
            new() { Status = JobStatus.Succeeded },
            new() { Status = JobStatus.Failed },
            new() { Status = JobStatus.Succeeded }
        ]);

        _emailAttachmentLogRepositoryMock.GetRecent(100).Returns(
        [
            new() { Succeeded = true, DownloadedAt = DateTimeOffset.UtcNow },
            new() { Succeeded = false, DownloadedAt = DateTimeOffset.UtcNow.AddMinutes(-5) }
        ]);

        // Act
        var response = await client.GetAsync("/api/stats", TestContext.Current.CancellationToken);

        // Assert
        response.EnsureSuccessStatusCode();
        var stats = await response.Content.ReadFromJsonAsync<JsonNode>(
            TestContext.Current.CancellationToken);
        
        Assert.NotNull(stats);
        Assert.Equal(3, stats["totalRecentDocumentsProcessed"]?.GetValue<int>());
        Assert.Equal(2, stats["totalRecentDocumentsSucceeded"]?.GetValue<int>());
        Assert.Equal(1, stats["totalRecentDocumentsFailed"]?.GetValue<int>());
        Assert.Equal(2, stats["totalRecentDownloads"]?.GetValue<int>());
        Assert.Equal(1, stats["totalRecentSucceeded"]?.GetValue<int>());
        Assert.Equal(1, stats["totalRecentFailed"]?.GetValue<int>());
    }
}
