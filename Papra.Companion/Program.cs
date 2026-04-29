using System.Text.Json.Nodes;
using Flowbite.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Papra.Companion;
using Papra.Companion.BackgroundServices;
using Papra.Companion.Components;
using Papra.Companion.Constants;
using Papra.Companion.Data;
using Papra.Companion.Data.Repositories;
using Papra.Companion.Data.Repositories.Interfaces;
using Papra.Companion.Models;
using Papra.Companion.Services;
using Papra.Companion.Services.Interfaces;


var appStartTime = DateTimeOffset.UtcNow;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddHttpClient();

// Flowbite services (TailwindMerge, FloatingService, etc.)
builder.Services.AddFlowbite();

// SQLite database via EF Core
var dbPath = Path.Combine(builder.Environment.ContentRootPath, AppPaths.DataFolder, AppPaths.DatabaseFileName);
Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
builder.Services.AddDbContextFactory<AppDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

// Pre-create attachments folder under content root
Directory.CreateDirectory(Path.Combine(builder.Environment.ContentRootPath, AppPaths.AttachmentsFolder));

// Persist Data Protection keys to the data folder so antiforgery tokens survive container restarts
var keysPath = Path.Combine(builder.Environment.ContentRootPath, AppPaths.DataFolder, AppPaths.KeysFolder);
Directory.CreateDirectory(keysPath);
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(keysPath))
    .SetApplicationName("Papra.Companion");

// Pipeline services
builder.Services.AddSingleton<IJobResultRepository, JobResultRepository>();
builder.Services.AddSingleton<IPipelineSettingsRepository, PipelineSettingsRepository>();
builder.Services.AddSingleton<ISettingsService, SettingsService>();
builder.Services.AddSingleton<IPipelineStatusService, PipelineStatusService>();
builder.Services.AddSingleton<IPipelineQueue, PipelineQueue>();
builder.Services.AddScoped<IPapraService, PapraService>();
builder.Services.AddScoped<IOpenAiService, OpenAiService>();
builder.Services.AddScoped<IMistralService, MistralService>();
builder.Services.AddScoped<IDocumentPipelineService, DocumentPipelineService>();
builder.Services.AddHostedService<PipelineBackgroundService>();

// Email attachment downloader services
builder.Services.AddSingleton<IEmailAttachmentSettingsRepository, EmailAttachmentSettingsRepository>();
builder.Services.AddSingleton<IEmailAttachmentLogRepository, EmailAttachmentLogRepository>();
builder.Services.AddSingleton<IEmailAttachmentSettingsService, EmailAttachmentSettingsService>();
builder.Services.AddScoped<IEmailAttachmentService, EmailAttachmentService>();
builder.Services.AddHostedService<EmailAttachmentBackgroundService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseAntiforgery();
app.MapStaticAssets();

// Minimal API endpoint for application stats
app.MapGet("/api/stats", (IPipelineStatusService pipelineStatusService, IEmailAttachmentLogRepository emailAttachmentLogRepository) =>
{
    var uptime = DateTimeOffset.UtcNow - appStartTime;
    var recentEmailDownloads = emailAttachmentLogRepository.GetRecent(100);
    var totalEmailDownloads = recentEmailDownloads.Count;
    var mostRecentDownload = recentEmailDownloads.OrderByDescending(e => e.DownloadedAt).FirstOrDefault();

    var stats = new
    {
        version = typeof(Program).Assembly.GetName().Version?.ToString() ?? "unknown",
        uptimeSeconds = (long)uptime.TotalSeconds,
        totalRecentDocumentsProcessed = pipelineStatusService.RecentJobs.Count,
        totalRecentDocumentsSucceeded = pipelineStatusService.RecentJobs.Count(j => j.Status == JobStatus.Succeeded),
        totalRecentDocumentsFailed = pipelineStatusService.RecentJobs.Count(j => j.Status == JobStatus.Failed),
        totalRecentDownloads = totalEmailDownloads,
        totalRecentSucceeded = recentEmailDownloads.Count(d => d.Succeeded),
        totalRecentFailed = recentEmailDownloads.Count(d => !d.Succeeded)
    };
    return Results.Json(stats);
});

// Webhook endpoint - Papra calls this when a document is uploaded
app.MapPost("/webhook/document", async (HttpContext context,
    ISettingsService settingsService,
    IPipelineQueue queue,
    ILogger<Program> logger) =>
{
    var settings = settingsService.Current;
    if (!settings.IsConfigured)
    {
        logger.LogWarning("Webhook received but pipeline is not configured");
        return Results.Problem("Pipeline is not configured.", statusCode: 503);
    }

    JsonNode? payload;
    try
    {
        using var reader = new StreamReader(context.Request.Body);
        var body = await reader.ReadToEndAsync();
        payload = JsonNode.Parse(body);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to parse webhook payload");
        return Results.BadRequest("Invalid JSON payload.");
    }

    var orgId = payload?["data"]?["organizationId"]?.GetValue<string>();
    var docId = payload?["data"]?["documentId"]?.GetValue<string>();

    if (string.IsNullOrWhiteSpace(orgId) || string.IsNullOrWhiteSpace(docId))
    {
        logger.LogWarning("Webhook payload missing organizationId or documentId");
        return Results.BadRequest("Missing organizationId or documentId in payload.");
    }

    await queue.EnqueueAsync(new ProcessingJob
    {
        OrganizationId = orgId,
        DocumentId = docId
    });

    logger.LogQueuedDocument(docId, orgId);
    return Results.Accepted();
});

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Apply any pending EF Core migrations on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>().CreateDbContext();
    await db.Database.MigrateAsync();
}

app.Run();
