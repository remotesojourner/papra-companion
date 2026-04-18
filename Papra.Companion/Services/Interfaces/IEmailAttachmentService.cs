using Papra.Companion.Models;

namespace Papra.Companion.Services.Interfaces;

public interface IEmailAttachmentService
{
    Task<IReadOnlyList<EmailAttachmentDownloadResult>> RunAsync(CancellationToken ct);
    Task<string> TestConnectionAsync(string? passwordOverride, CancellationToken ct);
    Task EnsureCopyFolderExistsAsync(CancellationToken ct);
}
