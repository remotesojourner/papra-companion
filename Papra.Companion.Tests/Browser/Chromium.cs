using Microsoft.Playwright;

namespace Papra.Companion.Tests.Browser;

public sealed class Chromium : IAsyncLifetime
{
    public const string RequiredVariable = "PAPRA_COMPANION_BROWSER_TESTS";

    private const string InstallHint =
        "Chromium isn't installed for Playwright, so the browser tests were skipped. Build the tests, then run: " +
        "pwsh Papra.Companion.Tests/bin/Debug/net10.0/playwright.ps1 install chromium";

    private IPlaywright? _playwright;

    public IBrowser? Browser { get; private set; }

    public string Unavailable { get; private set; } = "";

    public async ValueTask InitializeAsync()
    {
        _playwright = await Playwright.CreateAsync();
        try
        {
            Browser = await _playwright.Chromium.LaunchAsync();
        }
        catch (PlaywrightException ex) when (ex.Message.Contains("Executable doesn't exist", StringComparison.Ordinal) && !Required)
        {
            Unavailable = InstallHint;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (Browser != null) await Browser.DisposeAsync();
        _playwright?.Dispose();
    }

    private static bool Required => Environment.GetEnvironmentVariable(RequiredVariable) == "required";
}
