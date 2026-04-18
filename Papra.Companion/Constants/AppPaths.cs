namespace Papra.Companion.Constants;

internal static class AppPaths
{
    /// <summary>Subdirectory (relative to content root) where persistent data is stored.</summary>
    internal const string DataFolder = "data";

    /// <summary>SQLite database filename inside <see cref="DataFolder"/>.</summary>
    internal const string DatabaseFileName = "papra-companion.db";

    /// <summary>Subdirectory inside <see cref="DataFolder"/> where Data Protection keys are persisted.</summary>
    internal const string KeysFolder = "keys";

    /// <summary>Subdirectory (relative to content root) where downloaded email attachments are written.</summary>
    internal const string AttachmentsFolder = "attachments";
}
