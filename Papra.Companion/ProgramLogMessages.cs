namespace Papra.Companion;

internal static partial class ProgramLogMessages
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Queued document {DocumentId} from org {OrgId}")]
    internal static partial void LogQueuedDocument(this ILogger logger, string documentId, string orgId);
}
