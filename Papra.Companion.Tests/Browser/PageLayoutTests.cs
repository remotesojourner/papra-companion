using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace Papra.Companion.Tests.Browser;

[Collection(BrowserTestGroup.Name)]
[Trait("Category", "Browser")]
public sealed class PageLayoutTests : BrowserTest, IClassFixture<BrowserApp>
{
    private static readonly (string Path, string Name, string Landmark)[] _pages =
    [
        ("/", "title-generation", "Recent Activity"),
        ("/email-attachments", "email-attachments", "Recent Downloads"),
        ("/settings/papra", "settings-papra", "reads documents from"),
        ("/settings/ai", "settings-ai", "Title prompt"),
        ("/settings/email", "settings-email", "Afterwards"),
        ("/not-found", "not-found", "Page not found")
    ];

    private readonly BrowserApp _app;

    public PageLayoutTests(BrowserApp app, Chromium chromium) : base(chromium)
    {
        _app = app;
    }

    public static TheoryData<BrowserTheme, int> Views => new()
    {
        { BrowserTheme.Dark, DesktopWidth },
        { BrowserTheme.Light, DesktopWidth },
        { BrowserTheme.Dark, WideWidth },
        { BrowserTheme.Dark, PhoneWidth },
        { BrowserTheme.Light, PhoneWidth }
    };

    [Theory]
    [MemberData(nameof(Views))]
    public async Task EveryPageRendersWholeWithoutScrollingSideways(BrowserTheme theme, int width)
    {
        await using var browser = await OpenBrowserAsync(_app, theme, width);
        var page = await browser.NewPageAsync();
        Directory.CreateDirectory(ScreenshotFolder);
        var problems = new List<string>();

        foreach (var (path, name, landmark) in _pages)
        {
            await page.GotoAsync(path);
            await Expect(page.GetByText(landmark).First).ToBeVisibleAsync();
            await WaitForThemeAsync(page, theme);
            await page.ScreenshotAsync(new() { Path = Path.Combine(ScreenshotFolder, $"{name}-{theme.ToString().ToLowerInvariant()}-{width}.png"), FullPage = true, Animations = ScreenshotAnimations.Disabled });

            if (await ShowsAnErrorAsync(page)) problems.Add($"{path} shows an error");
            if (await ScrollsSidewaysAsync(page)) problems.Add($"{path} scrolls sideways");
            if (await HasTableWiderThanItsCardAsync(page)) problems.Add($"{path} has a table wider than its card");
            if (await HasSettingsOutOfLineAsync(page)) problems.Add($"{path} has a status panel that doesn't line up with the settings");
        }

        Assert.Empty(problems);
        AssertNoBrowserErrors();
    }
}
