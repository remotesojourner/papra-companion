namespace Papra.Companion.Constants;

internal static class PapraConstants
{
    // Route templates — use string.Format / interpolation with (orgId, docId)
    internal const string OrganizationsRoute = "/api/organizations";
    internal const string DocumentsRoute     = "/api/organizations/{0}/documents/{1}";
    internal const string DocumentFileRoute  = "/api/organizations/{0}/documents/{1}/file";
    internal const string DocumentTagsRoute  = "/api/organizations/{0}/documents/{1}/tags";
    internal const string TagsRoute          = "/api/organizations/{0}/tags";
}
