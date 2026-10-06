namespace Papra.Companion;

internal static partial class ProgramLogMessages
{
    [LoggerMessage(Level = LogLevel.Warning, Message = "Webhook received but pipeline is not configured")]
    internal static partial void LogWebhookNotConfigured(this ILogger logger);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to parse webhook payload")]
    internal static partial void LogWebhookPayloadInvalid(this ILogger logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Webhook payload missing organizationId or documentId")]
    internal static partial void LogWebhookPayloadIncomplete(this ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Queued document {DocumentId} from org {OrgId}")]
    internal static partial void LogQueuedDocument(this ILogger logger, string documentId, string orgId);
}
