using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;
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

builder.Services.AddMudServices();
builder.Services.AddScoped<BrowserInteropService>();

var dbPath = Path.Combine(builder.Environment.ContentRootPath, AppPaths.DataFolder, AppPaths.DatabaseFileName);
Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
builder.Services.AddDbContextFactory<AppDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

Directory.CreateDirectory(Path.Combine(builder.Environment.ContentRootPath, AppPaths.AttachmentsFolder));

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor
                             | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});
var keysPath = Path.Combine(builder.Environment.ContentRootPath, AppPaths.DataFolder, AppPaths.KeysFolder);
Directory.CreateDirectory(keysPath);
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(keysPath))
    .SetApplicationName("Papra.Companion");

var oidcIssuer = Environment.GetEnvironmentVariable("OIDC_ISSUER");
var oidcEnabled = !string.IsNullOrWhiteSpace(oidcIssuer);

builder.Services.AddSingleton(new OidcOptions(oidcEnabled));

if (oidcEnabled)
{
    var oidcClientId = Environment.GetEnvironmentVariable("OIDC_CLIENT_ID") ?? throw new InvalidOperationException("OIDC_CLIENT_ID environment variable is required when OIDC_ISSUER is set.");
    var oidcClientSecret = Environment.GetEnvironmentVariable("OIDC_CLIENT_SECRET") ?? throw new InvalidOperationException("OIDC_CLIENT_SECRET environment variable is required when OIDC_ISSUER is set.");

    builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
    })
    .AddCookie(options =>
    {
        options.Cookie.SameSite = SameSiteMode.None;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    })
    .AddOpenIdConnect(options =>
    {
        options.Authority = oidcIssuer;
        options.ClientId = oidcClientId;
        options.ClientSecret = oidcClientSecret;
        options.ResponseType = "code";
        options.SaveTokens = true;
        options.GetClaimsFromUserInfoEndpoint = true;
        options.Scope.Add("openid");
        options.Scope.Add("profile");
        options.Scope.Add("email");
        options.MapInboundClaims = false;
        options.TokenValidationParameters.NameClaimType = "name";
    });
}
else
{
    builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
        .AddCookie();
}

builder.Services.AddAuthorization(options =>
{
    if (oidcEnabled)
    {
        options.FallbackPolicy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .Build();
    }
});
builder.Services.AddCascadingAuthenticationState();

builder.Services.AddSingleton<IJobResultRepository, JobResultRepository>();
builder.Services.AddSingleton<IPipelineSettingsRepository, PipelineSettingsRepository>();
builder.Services.AddSingleton<ISettingsService, SettingsService>();
builder.Services.AddSingleton<IPipelineStatusService, PipelineStatusService>();
builder.Services.AddSingleton<IPipelineJobChannel, PipelineJobChannel>();
builder.Services.AddScoped<IPapraService, PapraService>();
builder.Services.AddScoped<IOpenAiService, OpenAiService>();
builder.Services.AddScoped<ITitleGenerationService, TitleGenerationService>();
builder.Services.AddHostedService<PipelineBackgroundService>();

builder.Services.AddSingleton<IEmailAttachmentSettingsRepository, EmailAttachmentSettingsRepository>();
builder.Services.AddSingleton<IEmailAttachmentLogRepository, EmailAttachmentLogRepository>();
builder.Services.AddSingleton<IEmailAttachmentSettingsService, EmailAttachmentSettingsService>();
builder.Services.AddScoped<IEmailAttachmentService, EmailAttachmentService>();
builder.Services.AddHostedService<EmailAttachmentBackgroundService>();

var app = builder.Build();

app.UseForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();
app.MapStaticAssets();

if (oidcEnabled)
{
    app.MapGet("/auth/login", () => Results.Challenge(
        new AuthenticationProperties { RedirectUri = "/" },
        [OpenIdConnectDefaults.AuthenticationScheme]));

    app.MapPost("/auth/logout", async (HttpContext context, IAntiforgery antiforgery) =>
    {
        if (!await antiforgery.IsRequestValidAsync(context))
            return Results.Problem("The sign-out request has no valid antiforgery token.", statusCode: StatusCodes.Status400BadRequest);

        await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        await context.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme,
            new AuthenticationProperties { RedirectUri = "/" });
        return Results.Empty;
    }).AllowAnonymous();
}

app.MapGet("/api/stats", (IPipelineStatusService pipelineStatusService, IEmailAttachmentLogRepository emailAttachmentLogRepository) =>
{
    var uptime = DateTimeOffset.UtcNow - appStartTime;
    var recentEmailDownloads = emailAttachmentLogRepository.GetRecent(100);
    var totalEmailDownloads = recentEmailDownloads.Count;

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
}).AllowAnonymous();

app.MapPost("/webhook/document", async (HttpContext context,
    ISettingsService settingsService,
    IPipelineJobChannel queue,
    ILogger<Program> logger) =>
{
    var settings = settingsService.Current;
    if (!settings.IsConfigured)
    {
        logger.LogWebhookNotConfigured();
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
        logger.LogWebhookPayloadInvalid(ex);
        return Results.BadRequest("Invalid JSON payload.");
    }

    var orgId = payload?["data"]?["organizationId"]?.GetValue<string>();
    var docId = payload?["data"]?["documentId"]?.GetValue<string>();

    if (string.IsNullOrWhiteSpace(orgId) || string.IsNullOrWhiteSpace(docId))
    {
        logger.LogWebhookPayloadIncomplete();
        return Results.BadRequest("Missing organizationId or documentId in payload.");
    }

    await queue.EnqueueAsync(new ProcessingJob
    {
        OrganizationId = orgId,
        DocumentId = docId
    });

    logger.LogQueuedDocument(docId, orgId);
    return Results.Accepted();
}).AllowAnonymous();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>().CreateDbContext();
    await db.Database.MigrateAsync();
}

app.Run();

record OidcOptions(bool Enabled);
