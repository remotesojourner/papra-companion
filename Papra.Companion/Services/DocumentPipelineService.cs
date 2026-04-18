using Papra.Companion.Models;
using Papra.Companion.Services.Interfaces;

namespace Papra.Companion.Services;

public partial class DocumentPipelineService(
    ISettingsService settingsService,
    IPipelineStatusService statusService,
    IPapraService papraService,
    IOpenAiService openAiService,
    IMistralService mistralService,
    ILogger<DocumentPipelineService> logger) : IDocumentPipelineService
{

    public async Task ProcessAsync(ProcessingJob job, CancellationToken ct)
    {
        var settings = settingsService.Current;
        var result = new PipelineJobResult
        {
            DocumentId = job.DocumentId,
            OrganizationId = job.OrganizationId,
            StartedAt = DateTimeOffset.UtcNow
        };

        statusService.JobStarted(result);

        try
        {
            LogProcessingDocument(logger, job.DocumentId, job.OrganizationId);

            var (docName, docMimeType) = await papraService.GetDocumentInfoAsync(job.OrganizationId, job.DocumentId, ct);
            LogGotDocumentInfo(logger, docName, docMimeType);

            var fileBytes = await papraService.GetDocumentFileAsync(job.OrganizationId, job.DocumentId, ct);
            LogGotDocumentFile(logger, fileBytes.Length);

            var base64 = Convert.ToBase64String(fileBytes);
            var dataUrl = $"data:{docMimeType};base64,{base64}";

            var extractedText = settings.UseOpenAiForOcr
                ? await openAiService.ExtractTextFromImageAsync(dataUrl, settings.OcrPrompt, ct)
                : await mistralService.ExtractTextAsync(dataUrl, ct);
            LogExtractedText(logger, extractedText.Length);

            var titlePrompt = settings.TitlePrompt
                .Replace("{{original_title}}", docName)
                .Replace("{{content}}", extractedText);
            var title = await openAiService.CompleteAsync(titlePrompt, ct);
            LogExtractedTitle(logger, title);
            result.ExtractedTitle = title;

            await papraService.UpdateDocumentAsync(job.OrganizationId, job.DocumentId, title, extractedText, ct);
            logger.LogInformation("Updated document content");

            var tags = await papraService.GetTagsAsync(job.OrganizationId, ct);
            LogFoundAvailableTags(logger, tags.Count);

            if (tags.Count > 0)
            {
                var tagList = string.Join(", ", tags.Select(t => t.Name));

                var tagPrompt = settings.TagPrompt
                    .Replace("{{available_tags}}", tagList)
                    .Replace("{{original_title}}", docName)
                    .Replace("{{content}}", extractedText);
                var tagResult = await openAiService.CompleteAsync(tagPrompt, ct);
                var suggestedNames = tagResult.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
                var suggestedNamesStr = string.Join(", ", suggestedNames);
                LogSuggestedTags(logger, suggestedNamesStr);

                var matchedIds = tags
                    .Where(t => suggestedNames.Any(s => string.Equals(s, t.Name, StringComparison.OrdinalIgnoreCase)))
                    .Select(t => t.Id)
                    .ToList();

                foreach (var tagId in matchedIds)
                    await papraService.AddTagToDocumentAsync(job.OrganizationId, job.DocumentId, tagId, ct);

                LogAppliedTags(logger, matchedIds.Count);
            }

            result.Status = JobStatus.Succeeded;
            LogDocumentProcessed(logger, job.DocumentId);
        }
        catch (OperationCanceledException)
        {
            result.Status = JobStatus.Failed;
            result.ErrorMessage = "Operation was cancelled.";
            LogProcessingCancelled(logger, job.DocumentId);
        }
        catch (Exception ex)
        {
            result.Status = JobStatus.Failed;
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

    [LoggerMessage(Level = LogLevel.Information, Message = "Got document info: {Name} ({MimeType})")]
    private static partial void LogGotDocumentInfo(ILogger logger, string name, string mimeType);

    [LoggerMessage(Level = LogLevel.Information, Message = "Got document file: {Size} bytes")]
    private static partial void LogGotDocumentFile(ILogger logger, int size);

    [LoggerMessage(Level = LogLevel.Information, Message = "Extracted {Chars} chars of text")]
    private static partial void LogExtractedText(ILogger logger, int chars);

    [LoggerMessage(Level = LogLevel.Information, Message = "Extracted title: {Title}")]
    private static partial void LogExtractedTitle(ILogger logger, string title);

    [LoggerMessage(Level = LogLevel.Information, Message = "Found {Count} available tags")]
    private static partial void LogFoundAvailableTags(ILogger logger, int count);

    [LoggerMessage(Level = LogLevel.Information, Message = "Suggested tags: {Tags}")]
    private static partial void LogSuggestedTags(ILogger logger, string tags);

    [LoggerMessage(Level = LogLevel.Information, Message = "Applied {Count} tags to document")]
    private static partial void LogAppliedTags(ILogger logger, int count);

    [LoggerMessage(Level = LogLevel.Information, Message = "Document {DocumentId} processed successfully")]
    private static partial void LogDocumentProcessed(ILogger logger, string documentId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Processing cancelled for document {DocumentId}")]
    private static partial void LogProcessingCancelled(ILogger logger, string documentId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to process document {DocumentId}")]
    private static partial void LogFailedDocument(ILogger logger, Exception ex, string documentId);
}
