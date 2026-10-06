namespace Papra.Companion.Constants;

internal static class PapraConstants
{
    internal const string OrganizationsRoute = "/api/organizations";
    internal static string DocumentRoute(string orgId, string docId) => $"{OrganizationsRoute}/{orgId}/documents/{docId}";
}
