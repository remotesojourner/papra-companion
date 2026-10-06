using System.Net;

namespace Papra.Companion.Tests;

[CollectionDefinition(nameof(OidcEnvironmentGroup), DisableParallelization = true)]
public class OidcEnvironmentGroup;

[Collection(nameof(OidcEnvironmentGroup))]
public sealed class SignOutEndpointTests : IDisposable
{
    private static readonly (string Name, string Value)[] _oidcVariables =
    [
        ("OIDC_ISSUER", "https://auth.invalid/application/o/papra-companion/"),
        ("OIDC_CLIENT_ID", "papra-companion"),
        ("OIDC_CLIENT_SECRET", "secret")
    ];

    private readonly TestAppFactory _factory;

    public SignOutEndpointTests()
    {
        foreach (var (name, value) in _oidcVariables) Environment.SetEnvironmentVariable(name, value);
        _factory = new TestAppFactory();
    }

    [Fact]
    public async Task SignOutWithoutAntiforgeryTokenIsRejected()
    {
        var client = _factory.CreateClient(new() { AllowAutoRedirect = false });

        var response = await client.PostAsync("/auth/logout", new FormUrlEncodedContent([]), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.False(response.Headers.Contains("Set-Cookie"));
    }

    public void Dispose()
    {
        _factory.Dispose();
        foreach (var (name, _) in _oidcVariables) Environment.SetEnvironmentVariable(name, null);
    }
}
