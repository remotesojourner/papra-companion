using System.Text.Json;
using Papra.Companion.Models;
using Papra.Companion.Services.Interfaces;

namespace Papra.Companion.Services;

public partial class TitleGenerationService(
    ISettingsService settingsService,
    IPipelineStatusService statusService,
    IPapraService papraService,
    IOpenAiService openAiService,
    ILogger<TitleGenerationService> logger) : ITitleGenerationService
{
    public async Task ProcessAsync(ProcessingJob job, CancellationToken ct)
    {
        var settings = settingsService.Current;
        var result = new PipelineJobResult
        {
            DocumentId     = job.DocumentId,
            OrganizationId = job.OrganizationId,
            StartedAt      = DateTimeOffset.UtcNow
        };

        statusService.JobStarted(result);

        try
        {
            LogProcessingDocument(logger, job.DocumentId, job.OrganizationId);

            // Optional delay before processing (gives Papra time to finish indexing)
            if (settings.ProcessingDelaySeconds > 0)
            {
                LogDelayingProcessing(logger, settings.ProcessingDelaySeconds);
                await Task.Delay(TimeSpan.FromSeconds(settings.ProcessingDelaySeconds), ct);
            }

            var (docName, _) = await papraService.GetDocumentInfoAsync(job.OrganizationId, job.DocumentId, ct);
            LogGotDocumentInfo(logger, docName);

            var titlePrompt = settings.TitlePrompt
                .Replace("{{original_title}}", docName);

            var titleSchema = new
            {
                type = "json_schema",
                json_schema = new
                {
                    name   = "title_extraction",
                    strict = true,
                    schema = new
                    {
                        type       = "object",
                        properties = new { title = new { type = "string" } },
                        required   = new[] { "title" },
                        additionalProperties = false
                    }
                }
            };

            var titleJsonStr  = await openAiService.CompleteAsync(titlePrompt, titleSchema, ct);
            var titleResponse = JsonSerializer.Deserialize<TitleExtractionResponse>(titleJsonStr);
            var title         = titleResponse?.Title ?? "Untitled Document";

            LogExtractedTitle(logger, title);
            result.ExtractedTitle = title;

            await papraService.UpdateDocumentTitleAsync(job.OrganizationId, job.DocumentId, title, ct);
            logger.LogInformation("Updated document title");

            result.Status = JobStatus.Succeeded;
            LogDocumentProcessed(logger, job.DocumentId);
        }
        catch (OperationCanceledException)
        {
            result.Status       = JobStatus.Failed;
            result.ErrorMessage = "Operation was cancelled.";
            LogProcessingCancelled(logger, job.DocumentId);
        }
        catch (Exception ex)
        {
            result.Status       = JobStatus.Failed;
            result.ErrorMessage = ex.Message;
            LogFailedDocument(logger, ex, job.DocumentId);
        }
        finally
        {
            result.CompletedAt = DateTimeOffset.UtcNow;
            statusService.JobCompleted(result);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Processing document {DocumentId} for org {OrgId}")]
    private static partial void LogProcessingDocument(ILogger logger, string documentId, string orgId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Delaying processing for {Seconds} second(s)")]
    private static partial void LogDelayingProcessing(ILogger logger, int seconds);

    [LoggerMessage(Level = LogLevel.Information, Message = "Got document info: {Name}")]
    private static partial void LogGotDocumentInfo(ILogger logger, string name);

    [LoggerMessage(Level = LogLevel.Information, Message = "Extracted title: {Title}")]
    private static partial void LogExtractedTitle(ILogger logger, string title);

    [LoggerMessage(Level = LogLevel.Information, Message = "Document {DocumentId} processed successfully")]
    private static partial void LogDocumentProcessed(ILogger logger, string documentId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Processing cancelled for document {DocumentId}")]
    private static partial void LogProcessingCancelled(ILogger logger, string documentId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to process document {DocumentId}")]
    private static partial void LogFailedDocument(ILogger logger, Exception ex, string documentId);
}

internal sealed record TitleExtractionResponse(
    [property: System.Text.Json.Serialization.JsonPropertyName("title")] string Title);
