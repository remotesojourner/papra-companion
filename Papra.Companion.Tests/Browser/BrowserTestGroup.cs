namespace Papra.Companion.Tests.Browser;

[CollectionDefinition(Name)]
public sealed class BrowserTestGroup : ICollectionFixture<Chromium>
{
    public const string Name = "Browser";
}
